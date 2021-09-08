using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Utility.Config
{


    


    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Generic helper class to write a runtime dataset (TData) into a config file line-by-line.
    ///
    /// The writer receives a matching CustomConfigSetup and needs a corresponding CustomConfigReader with same setup for packing actions.
    /// 
    /// The file itself is written via contexts (TContext) and matching descriptors holding specific values (TValue).
    /// A context indicates the start of a specific section of the file, allowing certain descriptors to be applied if present in the dataset.
    /// Contexts and descriptors are later used to parse the file and recreate the runtime dataset.
    ///
    /// The descriptors must be registered in the setup and are shared with the corresponding reader.
    ///
    public class CustomConfigWriter<TData, TContext, TValues> : ICustomWriter
                                where TData : class, IDataTarget<TContext>, new()
                                where TContext : Enum
                                where TValues : Enum
    {
        public readonly string Name;
        public readonly CustomConfigSetup<TData, TContext, TValues> Setup;

        public int lineIndexer { get; private set; } = 0;
        public bool logging { get; set; }


        readonly CustomConfigReader<TData, TContext, TValues> Reader;
        readonly TermBufferWriter<TData, TContext> BufferWriter;
        readonly TermFormatter<TContext> Formatter;

        TData dataTarget;
        TContext currentContext;

        List<Term> termBuffer;
        List<TData> dataBuffer;
        System.Text.StringBuilder stringBuffer;



        public CustomConfigWriter(string name,
                                  CustomConfigSetup<TData, TContext, TValues> setup, 
                                  CustomConfigReader<TData, TContext, TValues> reader,
                                  TermBufferWriter<TData, TContext> bufferWriter,
                                  TermFormatter<TContext> formatter=null)
        {
            this.Name = name;
            this.Setup = setup;
            this.Reader = reader;
            this.BufferWriter = bufferWriter;
            this.Formatter = formatter;
            termBuffer = new List<Term>();
            dataBuffer = new List<TData>();
            stringBuffer = new System.Text.StringBuilder();
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  SINGLE FILE INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------

        ICustomIOSetup ICustomWriter.Setup { get { return Setup; } }
        bool ICustomWriter.WriteStream(StreamWriter writer, IDataTarget data)
        {
            dataTarget = data as TData;
            if(dataTarget != null)
            {
                fillTermBuffer(null, null);
                if(termBuffer.Count > 0)
                {
                    writeTermBufferToFile(writer);
                    return true;
                } 
            }  
            return false;
        }


        /// @brief  
        /// create a metafile from loaded runtime data
        ///
        public bool TryWrite(string pathToFile, TData data, bool debug=false)
        {
            logging = debug;
            dataTarget = data;
            try 
            {
                if(File.Exists(pathToFile))
                {
                    File.Delete(pathToFile);
                }
                using(var stream = new FileStream(pathToFile, FileMode.OpenOrCreate))
                {
                    using(var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                    {
                        fillTermBuffer(null, null);
                        writeTermBufferToFile(writer);
                        writer.Close();
                    }
                    stream.Close();
                }
                return true;
            }
            catch(IOException ex)
            {
                Debug.LogError(ex.Message);
            }
            return false;
        }


        /// @brief  
        /// create/override a metafile from loaded runtime data, but only write certain contexts
        ///
        public bool TryWrite(string pathToFile, TData data, IWriteConstraint<TContext> constraint, bool debug=false)
        {
            logging = debug;
            try 
            {
                TData fallback = null;
                if(File.Exists(pathToFile))
                {
                    if(!Reader.TryParse(pathToFile, out fallback))
                    {
                        Debug.LogWarning("Could not constraint-write VideoMeta[" + data.Index + "], file is corrupt!");
                        constraint = null;
                    }
                    else
                    {
                        File.Delete(pathToFile);
                    }
                }
                else
                {
                    constraint = null;
                }

                dataTarget = data;
                using(var stream = new FileStream(pathToFile, FileMode.OpenOrCreate))
                {
                    using(var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                    {
                        fillTermBuffer(constraint, fallback);
                        writeTermBufferToFile(writer);
                        writer.Close();
                    }
                    stream.Close();
                }
                return true;
            }
            catch(IOException ex)
            {
                Debug.LogError(ex.Message);
            }
            return false;
        }




        //-----------------------------------------------------------------------------------------------------------------
        //
        //  PACKING INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------


        /// @brief
        /// writes multiple video meta infos into one file.
        /// 
        /// @param pathToFile Full target folderPath
        /// @param packing ID range that is fitted into one file (10 equals 001-010 be packed into first file)
        /// @param packID Packing index (n-th file with defined packing)
        /// @param data Dataset to be written; video-IDs must match given packing range & index
        ///
        public bool TryPackWrite(string pathToFolder, int packing, int packID, List<TData> data, bool debug=false)
        {
            return TryPackWrite(pathToFolder, packing, packID, data, null);
        }
        
        

        /// @brief
        /// writes multiple video meta infos into one file.
        /// 
        /// @param pathToFile Full target folderPath
        /// @param packing ID range that is fitted into one file (10 equals 001-010 be packed into first file)
        /// @param packID Packing index (n-th file with defined packing)
        /// @param data Dataset to be written; video-IDs must match given packing range & index
        /// @param constraint Additional instruction on what to write
        ///
        public bool TryPackWrite(string pathToFolder, int packing, int packID, List<TData> data, IWriteConstraint<TContext> constraint, bool debug=false)
        {
            logging = debug;
            
            if(!Setup.HasPackingRules())
            {
                Debug.LogWarning(Name + ":: no packing rules defined in setup!");
                return false;
            }
            else if(!CustomConfigIO.ValidatePackingIndices(packing, packID, data))
            {
                Debug.LogWarning("VideoMetaDataReader:: invalid packing indices!");
                return false;
            }

            //  load existing data
            
            string fileName = Setup.GetPackedFileName(packing, packID);
            string pathToFile = "";
            if(pathToFolder.EndsWith(fileName))
            {
                pathToFile = pathToFolder;
                pathToFolder = FileSystemUtil.RemoveFileFromPath(pathToFolder);
            }
            else
            {
                if(!pathToFolder.EndsWith("/")) pathToFolder = pathToFolder + "/";
                pathToFile = pathToFolder + fileName;
            }
            try
            {
                dataBuffer.Clear();
                if(File.Exists(pathToFile))
                {   
                    //  load all to list, override by id & sort buffer
                    //  write old files to dataBuffer as fallback
                    TData[] oldData;
                    if(Reader.TryPackParsing(pathToFile, out oldData, debug))
                    {
                        Debug.Log("OLD data exists! " + oldData.Length);
                        fillPackBuffers(data, oldData);
                    }
                    File.Delete(pathToFile);
                }

                if(dataBuffer.Count == 0)
                {
                    Debug.Log("OLD data does not exist!");
                    dataBuffer.Clear();
                    for(int i = 0; i < data.Count; i++) dataBuffer.Add(null);
                }

                Debug.Log("DATA: " + data.Count + " fallback buffer: " + dataBuffer.Count);

                //  write file
                using(var stream = new FileStream(pathToFile, FileMode.OpenOrCreate))
                {
                    using(var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                    {
                        for(int i = 0; i < data.Count; i++)
                        {
                            dataTarget = data[i];
                            writer.Write(Setup.PackPrefix + Setup.FormatPackingIndex(dataTarget) + Setup.PackSuffix);
                            //writer.WriteLine(Setup.PackPrefix + Setup.PackIndexWriter(dataTarget) + Setup.PackSuffix);
                            fillTermBuffer(constraint, dataBuffer[i]);
                            writeTermBufferToFile(writer);
                            for(int k = 0; k < Setup.PackLineSpacing; k++) writer.WriteLine();
                        }
                        writer.Close();
                    }
                    stream.Close();
                }
                return data.Count > 0;
            }
            catch(IOException ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }



        /// @brief
        /// reformats existing file for consistent readability
        ///
        public bool ReformatTextContent(string pathToFile)
        {
            try
            {
                if(File.Exists(pathToFile))
                {
                    var currentContext = Setup.NullContext;
                    var b = new System.Text.StringBuilder();
                    foreach(var l in File.ReadLines(pathToFile))
                    {
                        string line = l.Trim();
                        if(!string.IsNullOrEmpty(line))
                        {
                            TContext next;
                            if(Setup.CheckNewContext(line, out next))
                            {
                                if(!next.Equals(currentContext))
                                {   
                                    Debug.Log("[CONTEXT] <" + line + ">\n" + line.Length + "\n");

                                    b.Append("\n\n" + line);
                                    currentContext = next;
                                }
                            }
                            else if(!string.IsNullOrEmpty(line))
                            {
                                Debug.Log("[LINE] <" + line + ">\n" + line.Length + "\n");
                                b.Append("\n\t" + line);
                            }
                        }
                    }

                    var text = b.ToString();
                    Debug.Log("[Reformat]::\n" + text);
                    File.Delete(pathToFile);
                    File.WriteAllText(pathToFile, text, System.Text.Encoding.UTF8);
                    return true;
                }
                return false;
            }
            catch(IOException ex)
            {
                Debug.LogError(ex);
                return false;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  BUFFER WRITING
        //
        //-----------------------------------------------------------------------------------------------------------------

        

        void writeTermBufferToFile(StreamWriter writer)
        {
            //orderTermBuffer();

            stringBuffer.Clear();
            currentContext = Setup.NullContext;
            var b = new System.Text.StringBuilder("[WRITE TERM BUFFER]\n");
            for(int i = 0; i < termBuffer.Count; i++)
            {
                var tContext = ContextFromTerm(termBuffer[i]);
                b.Append("\n[" + i + "] " + termBuffer[i].rawLine + "  -> " + currentContext);
                if(!tContext.Equals(currentContext))
                {
                    //  new context found
                    if(logging) Debug.Log(RichText.emph("new context - ") + termBuffer[i].context + " - <" + termBuffer[i].rawLine + ">");
                    currentContext = tContext;
                    writer.WriteLine();
                    writer.WriteLine(termBuffer[i].rawLine);
                }
                else
                {
                    //  write line in context
                    string term = termBuffer[i].rawLine;
                    if(Formatter != null)
                    {
                        term = Formatter(currentContext, term);
                    }
                    stringBuffer.Clear();
                    stringBuffer.Append("\t");
                    stringBuffer.Append(term);
                    writer.WriteLine(stringBuffer.ToString());
                }
            }
            if(logging) Debug.Log(b);
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  TERM BUFFER
        //
        //-----------------------------------------------------------------------------------------------------------------

        void fillTermBuffer(IWriteConstraint<TContext> constraint, TData fallback=null)
        {
            termBuffer.Clear();
            lineIndexer = 0;

            foreach(var ctx in Setup.GetWriteableContexts())
            {
                if(constraint == null || constraint.hasContext(ctx))
                {
                    lineIndexer += BufferWriter(ctx, dataTarget, termBuffer, constraint);
                }
                else if(fallback != null)
                {
                    lineIndexer += BufferWriter(ctx, fallback, termBuffer, constraint);
                }
            }
        }   


        void fillPackBuffers(List<TData> data, IEnumerable<TData> oldData)
        {
            foreach(var old in oldData)
            {
                if(data.Exists(x=> x.Index == old.Index))
                {
                    int id = data.FindIndex(x=> x.Index > old.Index);
                    if(id != -1) data.Insert(id, old);
                    else data.Add(old);
                }
            }
            dataBuffer.Clear();
            for(int j = 0; j < data.Count; j++)
            {
                bool oldVersionExists = false;
                foreach(var o in oldData) {
                    if(o.Index == data[j].Index) {
                        dataBuffer.Add(o);
                        oldVersionExists = true;
                        break;
                    }
                }
                if(!oldVersionExists)
                {
                    dataBuffer.Add(null);
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  util

        TContext ContextFromTerm(Term t)
        {
            return Setup.ContextFromName(t.context);
        }

        
    }

}