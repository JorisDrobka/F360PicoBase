using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Utility.Config
{


    public class CustomConfigReader<TData, TContext, TValues> : ICustomReader
                                    where TData : class, IDataTarget<TContext>, new()
                                    where TContext : Enum
                                    where TValues : Enum
    {
        public readonly string Name;
        public readonly CustomConfigSetup<TData, TContext, TValues> Setup;

        public int lineIndexer { get; private set; } = 0;
        public bool logging { get; set; }
        public bool logValueError { get; set; } = false;

        public event Action<TData> DataParseEvent; 


        readonly TermSolver<TData, TContext> Solver;

        TData dataTarget;
        TContext currentContext;
        string currentFile;
        List<Term> termBuffer;
        List<TData> dataBuffer;

        Dictionary<string, List<IContextData>> contextDataBuffer;     //  inner data objects
        
        
        public CustomConfigReader(string name, 
                                  CustomConfigSetup<TData, TContext, TValues> setup,
                                  TermSolver<TData, TContext> solver=null)
        {
            this.Name = name;
            this.Setup = setup;
            this.Solver = solver;
            this.termBuffer = new List<Term>();
            this.dataBuffer = new List<TData>();
            this.contextDataBuffer = new Dictionary<string, List<IContextData>>();
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  SINGLE FILE INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------


        ICustomIOSetup ICustomReader.Setup { get { return Setup; } }
        bool ICustomReader.ParseStream(string file, StreamReader reader, ref int lineIndex, ref string line, out IDataTarget data)
        {
            currentFile = file;
            lineIndexer = lineIndex;
            dataTarget = new TData();
            var b = new System.Text.StringBuilder();
            if(logging) b.Append(RichText.darkMagenta("Parse Stream") + "<" + typeof(TData).Name + ", " + typeof(TContext).Name + ">");

            line = parseInternal(reader, stopReaderOnParseError: true, b: b);
            pushContextData();
            lineIndex += lineIndexer;

            if(logging)
            {
                Debug.Log(b.ToString());
                Debug.Log(RichText.orange("After Parse Stream") + "<" + typeof(TData).Name + ", " + typeof(TContext).Name + "> valid data? " + dataTarget.isValid() + "\n" + dataTarget.Readable(true));
            }
            
            data = dataTarget;
            return data != null;
            /*if(dataTarget.isValid())
            {
                data = dataTarget;
                return true;
            }
            else
            {
                data = default(TData);
                return false;
            }*/
        }


        /// @brief
        /// loads runtime data from metafile
        ///
        public bool TryParse(string pathToFile, out TData data, bool debug=false)
        {
            logging = debug;
            data = null;
            try
            {
                if(File.Exists(pathToFile))
                {
                    currentFile = pathToFile;
                    dataTarget = new TData();
                    contextDataBuffer.Clear();
                    //clipBuffer.Clear();
                    //termBuffer.Clear();
                    lineIndexer = 0;

                    var b = new System.Text.StringBuilder();
                    using(var stream = new FileStream(pathToFile, FileMode.OpenOrCreate))
                    {
                        using(var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                        {
                            parseInternal(reader, false, b);
                            reader.Close();
                        }
                        stream.Close();
                    }


                    Setup.EmptyLine(currentContext, lineIndexer);
                    pushContextData();

                    if(logging)
                    {
                        Debug.Log(RichText.darkMagenta("ConfigReader AFTER Parse::")
                                + RichText.italic(pathToFile) 
                                + "\nlineindex: " + lineIndexer + "\n" + b.ToString());
                        Debug.Log("Written DataTarget<" + typeof(TData).Name + ">:\n" + dataTarget.Readable(deep: true));
                    }
                    data = dataTarget;
                    DataParseEvent?.Invoke(data);
                    return data.isValid();
                }
                return false;
            }
            catch(IOException ex)
            {
                Debug.LogError(ex.Message);
                return false;    
            }
        }   


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  PACKING INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------


        /// @brief
        /// parses a file with multiple clips
        ///
        public bool TryPackParsing(string pathToFile, out TData[] data, bool debug=false)
        {
            if(!Setup.HasPackingRules())
            {
                Debug.LogWarning(Name + ":: no packing rules defined in setup!\n\t@file: " + RichText.italic(pathToFile));
                data = new TData[0];
                return false;
            }

            logging = debug;
            try
            {
                if(File.Exists(pathToFile))
                {
                    //contextDataBuffer.Clear();
                    currentFile = pathToFile;
                    dataBuffer.Clear();
                    using(var stream = new FileStream(pathToFile, FileMode.OpenOrCreate))
                    {
                        using(var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                        {
                            lineIndexer = 0;
                            string line = "";
                            int currVideoID = -1;
                            while(tryParseUntilNextPack(pathToFile, reader, ref line, ref currVideoID))
                            {
                                //contextDataBuffer.Clear();
                                if(logging) Debug.Log("PackParse.. [" + currVideoID + "]\n\tline={" + line + "}");
                            }
                            reader.Close();
                        }
                        stream.Close();
                    }

                    if(dataBuffer.Count > 0)
                    {
                        data = dataBuffer.ToArray();
                        dataBuffer.Clear();
                        if(DataParseEvent != null)
                        {
                            for(int i = 0; i < data.Length; i++) DataParseEvent(data[i]);
                        }
                        return true;
                    }
                    else if(lineIndexer > 0)
                    {
                        //  try single read
                        TData single;
                        if(TryParse(pathToFile, out single, debug))
                        {
                            data = new TData[] { single };
                            DataParseEvent?.Invoke(single);
                            return true;
                        }
                    }
                }
            }
            catch(IOException ex)
            {
                Debug.LogError(ex.Message);
            }

            data = new TData[0];
            return false;
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  CONTEXT DATA OBJECTS
        //
        //-----------------------------------------------------------------------------------------------------------------

        /// @returns current inner context data, or null
        /// this can be called during custom TermSolver() calls to set/manipulate inner data.
        ///
        public IContextData GetActiveContextData()
        {
            if(!currentContext.Equals(Setup.NullContext))
            {
                if(hasContextData(currentContext))
                {
                    var cdata = contextDataBuffer[currentContext.ToString()].Last();
                    if(!cdata.isClosed)
                    {
                        return cdata;
                    }
                }
            }
            return null;
        }


        bool hasContextData(TContext context)
        {   
            string ctx = context.ToString();
            return contextDataBuffer.ContainsKey(ctx) && contextDataBuffer[ctx].Count > 0;
        }

        bool hasActiveContextData(TContext context)
        {
            string ctx = context.ToString();
            return contextDataBuffer.ContainsKey(ctx) 
                && contextDataBuffer[ctx].Count > 0 
                && !contextDataBuffer[ctx].Last().isClosed;
        }

        void addToContextData(TContext context, IContextData data)
        {
            var ctx = context.ToString();
            if(!contextDataBuffer.ContainsKey(ctx)) contextDataBuffer.Add(ctx, new List<IContextData>());
            contextDataBuffer[ctx].Add(data);
        }

        void closeContextData(TContext context)
        {
            var ctx = context.ToString();
            if(contextDataBuffer.ContainsKey(ctx) && contextDataBuffer[ctx].Count > 0)
            {
                contextDataBuffer[ctx].Last().isClosed = true;
            }
        }

        void pushContextData()
        {
            foreach(var ctx in contextDataBuffer.Keys)
            {
                //Debug.Log("push[" + ctx + "] ? " + (contextDataBuffer[ctx].Count) + " " + (contextDataBuffer[ctx].Count > 0 ? contextDataBuffer[ctx][0].Context.ToString() : " no context"));
                if(contextDataBuffer[ctx].Count > 0 
                    && contextDataBuffer[ctx][0].Context == ctx)
                {
                    dataTarget.PushContextData(Setup.ContextFromName(ctx), contextDataBuffer[ctx].ToArray());
                }
            }
            contextDataBuffer.Clear();
        }

    

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  PARSING
        //
        //-----------------------------------------------------------------------------------------------------------------

        /// @returns true if line contains nothing to parse
        ///
        static bool filterLine(string line)
        {
            if(!string.IsNullOrEmpty(line))
            {
                if(line.Length >= 2)
                {
                    var begin = line.Substring(0, 2);
                    return begin == "//"
                        || begin == "--"
                        || begin == "__";
                }
                return false;
            }
            return true;
        }

        string correctLineFormatError(string line)
        {
            line = line.Trim();
            if(line.EndsWith(":")) line = line.Substring(0, line.Length-1);
            return line;
        }


        string parseInternal(StreamReader reader, bool stopReaderOnParseError=false, System.Text.StringBuilder b=null)
        {
            termBuffer.Clear();
            
            var bb = new System.Text.StringBuilder(RichText.darkMagenta("Run Parser") + "<" + typeof(TData).Name + ", " + typeof(TContext).Name + ">...");
            var line = "";
            var originalLine = "";
            while((line = reader.ReadLine()) != null)
            {
                originalLine = line;
                if(!parseSingleLine(reader, ref line, bb))
                {
                    if(stopReaderOnParseError)
                    {
                        if(logging) Debug.Log(RichText.darkOrange("stop internalParse on parseError!"));
                        break;
                    }
                }
            }

            if(logging)
            {
                Debug.Log(bb);
                Debug.Log("END Parsing [" + currentContext + "]\ntype: " + typeof(TData).Name);
            }

            if(!currentContext.Equals(Setup.NullContext))
            {
                solve();
                currentContext = Setup.NullContext;
            }
            if(b != null && bb != null)
            {
                b.Append("\n" + bb.ToString());
            }
            return originalLine;
        }

        
    
        bool parseSingleLine(StreamReader reader, ref string line, System.Text.StringBuilder bb=null)
        {
            bool log = logging && bb != null;
            if(log)
            {
                var s = "\n" + lineIndexer.ToString() + ":[" + currentContext.ToString() + "]\t" + RichText.darkGreen(line.Trim());
                bb.Append(s);
            }


            string ctxIndex = "";       //  optional multi-context index
            int ctxID=-1;

            if(Setup.GetContextIndexer(ref line, out ctxIndex))
            {
                //Debug.Log(RichText.emph("Context Indicer:: ") + ctxIndex);
                if(log) bb.Append("[indexer: " + ctxIndex + "]");
            }

            line = correctLineFormatError(line);

            ICustomConfigIO childIO = null;
            if(line.Contains(Setup.PackPrefix))
            {
                //  stop - encountered next packed video meta
                if(log) bb.Append("\tBreak; Encountered Pack Prefix!");
                return false;
            }
            else if(filterLine(line))
            {
                if(log) bb.Append("\t- filtered Line!");
                if(!currentContext.Equals(Setup.NullContext))
                {
                    if(log) bb.Append("\t- Solve after filtered Line!");
                    //  context closed, solve term buffer
                    solve();
                    currentContext = Setup.NullContext;
                }
                return true;
            }
            else if(Setup.CheckChildContext(line, out childIO, out ctxID))
            {
                //  parse with child io
                var newCtx = Setup.ContextFromID(ctxID);
                var creader = childIO.GetReader();

                if(log) bb.Append("\t- Found Child Context! [" + newCtx + "] " + (creader != null));

                if(creader != null)
                {
                    int lineID = lineIndexer;
                    IDataTarget innerData;
                    creader.logging = log;
                    if(creader.ParseStream(currentFile, reader, ref lineID, ref line, out innerData))
                    {
                        var cdata = innerData as IContextData;
                        //innerData.Index = ctxIndexer;
                        if(log)
                        {
                            bb.Append("\n\t" + RichText.darkGreen("Parsed Child Context ") 
                                + "data: " + (cdata != null ? ("(" + cdata.GetType().Name + "-" + cdata.Context.ToString() + ")") : "NULL") 
                                + "\n\toldCtx: " + currentContext
                                + "\n\tnewCtx: " + newCtx 
                                + "\n\tlast line: " + line);    
                        }
                        addToContextData(newCtx, cdata);
                        pushContextData();
                        currentContext = Setup.NullContext;

                        if(lineIndexer != lineID)
                        {
                            lineIndexer = lineID;
                            return parseSingleLine(reader, ref line, bb);
                        }
                        else
                        {
                            Debug.LogWarning("CustomReader:: parsed stream, but no movement of line reader detected. Abort to prevent looping\n\t@file: " + RichText.italic(currentFile));
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("CustomReader:: error parsing child stream [" + newCtx + "]\n\t@file: " + RichText.italic(currentFile));
                        lineIndexer = lineID;
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning("CustomReader:: found child context, but no appropriate reader registered in Setup!\n\t@file: " + RichText.italic(currentFile));
                    return false;
                }
            }
            else if(Setup.CheckNewContext(line, out ctxID))
            {
                //  try open new context
                var newCtx = Setup.ContextFromID(ctxID);
                if(logging) bb.Append("\t- New Context ! [" + newCtx + "]");

                if(!currentContext.Equals(newCtx)
                    && !currentContext.Equals(Setup.NullContext))
                {
                    //  new context found, solve old
                    solve();
                }
                currentContext = newCtx;
                if(!currentContext.Equals(Setup.NullContext))
                {
                    //  check if new context data has to be created
                    var rule = Setup.GetContextRule(newCtx);
                    if(rule != null)
                    {
                        if(rule.Mode == ContextMode.Multiple)
                        {
                            if(rule.Creator != null)
                            {
                                //  multi context data
                                var mdata = rule.Creator();
                                rule.TryParseIndex(mdata, ctxIndex);
                                addToContextData(newCtx, mdata);
                                //  set context index
                            }
                            else
                            {
                                Debug.LogWarning(Name + ":: no ContextData creator set for context=[" + newCtx.ToString() + "]\n\t@file: " + RichText.italic(currentFile));
                            }
                        }
                        else if(rule.Creator != null)
                        {
                            //  singleton context data
                            addToContextData(newCtx, rule.Creator());
                        }
                    }
                }
                return true;
            }
            else
            {
                //  try parse line
                if(log) bb.Append("\t- parse line with context=[" + currentContext + "]...");

                Term term;
                if(Setup.TryReadLine(currentContext, line, lineIndexer, out term))
                {
                    termBuffer.Add(term);
                    lineIndexer++;
                    return true;
                }
                else 
                {
                    if(!currentContext.Equals(Setup.NullContext))
                    {
                        if(logging) 
                            Debug.LogWarning("Unable to read line:: " + RichText.italic(line) + "\ncontext: " + currentContext 
                                            + "\nreader: " + this.GetType().FullName
                                            + "\n@file: " + RichText.italic(currentFile));
                    }
                    return false;
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        bool tryParseUntilNextPack(string pathToFile, StreamReader reader, ref string currentLine, ref int currVideoID)
        {
            string line = currentLine;
            if(string.IsNullOrEmpty(line))
            {
                line = reader.ReadLine();
            }
            do
            {
                if(!string.IsNullOrEmpty(line))
                {
                    int vID;
                    if(Setup.GetPackingContext(line, out vID))
                    {
                        if(logging)
                            Debug.Log("NEW PACKING Context={" + vID + "}");
                        currVideoID = vID;
                        dataTarget = new TData();
                        currentLine = parseInternal(reader, true);

                        Setup.EmptyLine(currentContext, lineIndexer);
                        pushContextData();
                        //dataTarget.SetContextData(contextDataBuffer);
                        //dataTarget.clips = clipBuffer.ToArray();

                        if(logging)
                        {
                            Debug.Log("PARSED:\n" + dataTarget.Readable(deep: true));
                        }

                        if(dataTarget.Index >= 0)
                        {
                            //dataTarget.media = MediaData.CreateVideo(pathToFile, dataTarget.durationMs);
                            if(dataTarget.isValid())
                            {
                                dataBuffer.Add(dataTarget);
                            }
                        }
                        return true;
                    }
                    else
                    {
                        lineIndexer++;
                    }
                }
            }
            while((line = reader.ReadLine()) != null);
            return false;       
        }





        //-----------------------------------------------------------------------------------------------------------------
        //
        //  SOLVER
        //
        //-----------------------------------------------------------------------------------------------------------------

        //  solve is called as soon as an active context ends
        //  termbuffer is read and its contents are written to respective data targets  
        //
        void solve()
        {
            if(logging)
            {
                var b = new System.Text.StringBuilder(RichText.darkGreen("Resolve Context:: ") + currentContext.ToString() + " termbuffer: " + termBuffer.Count.ToString());
                for(int i = 0; i < termBuffer.Count; i++)
                {
                    b.Append("\n\t" + termBuffer[i].rawLine + " [" + termBuffer[i].descriptor + ":" + termBuffer[i].content + "]");
                }
                Debug.Log(b);
            }

            if(!currentContext.Equals(Setup.NullContext))
            {
                if(Solver == null || !Solver(currentContext, dataTarget, termBuffer))
                {
                    var state = DefaultSolver(currentContext, dataTarget, termBuffer);
                    if(state.hasError())
                    {
                        if(state == ValueState.Invalid)
                        {
                            if(logValueError) Debug.LogWarning(Name + ":: data[" + dataTarget.Index + "]: Value error occured while solving context=[" + currentContext + "]\n\t@file: " + RichText.italic(currentFile));
                        }
                        else
                        {
                            if(logging) Debug.LogWarning(Name + ":: data[" + dataTarget.Index + "]: error while solving context=[" + currentContext + "]\n\t@file: " + RichText.italic(currentFile));
                        }
                        
                    }
                }
                closeContextData(currentContext);
            }
            termBuffer.Clear();
        }


        /// @brief
        /// reads out termbuffer and writes contents to active targets 
        ///
        public ValueState DefaultSolver(TContext context, TData target, List<Term> termBuffer)
        {
            //  set meta/clipdata by descriptor
            ValueState state = ValueState.Undefined;
            for(int i = 0; i < termBuffer.Count; i++)
            {
                if(Setup.HasDescriptor(termBuffer[i].descriptor))
                {
                    var descr = Setup.GetDescriptor(termBuffer[i].descriptor, context);
                    if(descr.hasSetter())
                    {
                        object val;
                        if(isUndefinedContent(termBuffer[i].content))
                        {
                            //  set default value
                            val = Setup.GetDefaultValue(descr.valueType);
                            state = setValue(descr, val);
                        }
                        else if(Setup.TryParseValue(termBuffer[i].content, descr.valueType, out val))
                        {
                            state = setValue(descr, val);
                        }
                        else
                        {
                            //  parse error
                            if(logging) Debug.LogWarning(Name + ":: Error solving <" + context.ToString() + "/" + descr.descriptor 
                                                        + ">\n\treason: unable to parse value=[" + termBuffer[i].content + "]"
                                                        + "\n\t@file: " + RichText.italic(currentFile));
                            state = ValueState.DataError;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("descriptor[" + termBuffer[i].content + "->" + context.ToString() + "/" + termBuffer[i].descriptor + "] has no setter func!\n\t@file: " + RichText.italic(currentFile));
                    }
                }
                else
                {
                    //  missing descriptor error
                    if(logging) Debug.LogWarning(Name + ":: Error solving term=[" + i + "]: " + termBuffer[i].rawLine + "\n\treason: missing descriptor.\n\t@file: " + RichText.italic(currentFile));
                    state = ValueState.DescriptorError;
                }
            }
            return state;
        }

        bool isUndefinedContent(string content)
        {
            content = content.Trim().ToLower();
            return content == "undefined"
                || content.StartsWith("xx")
                || content == "x"
                || content == "x-x";
        }

        ValueState setValue(IDescriptor<TData, TContext, TValues> descr, object value)
        {
            if(!Setup.ValidateValue(descr.valueType, value))
            {
                if(logValueError) Debug.LogWarning(Name + ":: Valuetype mismatch! context=[" + descr.context + "]<" + descr.descriptor + "> --> " + (value != null ? value.GetType().Name : "null"));
                return ValueState.DataError;
            }

            var state = ValueState.Undefined;
            
            if(Setup.hasContextRule(currentContext.ToString()))
            {
                var rule = Setup.GetContextRule(currentContext.ToString());
                switch(rule.Mode)
                {
                    case ContextMode.Singular: 
                        if(rule.Creator != null 
                            && hasActiveContextData(currentContext))
                        {
                            var cdata = GetActiveContextData();
                            state = descr.Set(contextDataBuffer[currentContext.ToString()].Last(), value);
                            switch(state)
                            {
                                case ValueState.DataError:
                                    if(logging) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "]<" + descr.descriptor + "> singleton contextData[" + cdata.GetType().Name + "]"
                                                            + "\n\treason: unable to set value"
                                                            + "\n\t@file: " + RichText.italic(currentFile));
                                    break;

                                case ValueState.Invalid:
                                    if(logValueError) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "]<" + descr.descriptor + "> singleton contextData[" + cdata.GetType().Name + "]"
                                                            + "\n\treason: invalid value"
                                                            + "\n\t@file: " + RichText.italic(currentFile));
                                    break;
                            }
                        }
                        else
                        {
                            state = descr.Set(dataTarget, value);
                            switch(state)
                            {
                                case ValueState.DataError:
                                    if(logging) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "]<" + descr.descriptor + ">"
                                                            + "\n\treason: unable to set value"
                                                            + "\n\t@file: " + RichText.italic(currentFile));
                                    break;
                                case ValueState.Invalid:
                                    if(logValueError) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "] <" + descr.descriptor + ">"
                                                            + "\n\treason: invalid value"
                                                            + "\n\t@file: " + RichText.italic(currentFile));
                                    break;
                            }
                        }
                        break;

                    case ContextMode.Multiple:

                        if(hasActiveContextData(currentContext))
                        {
                            var cdata = contextDataBuffer[currentContext.ToString()].Last();
                            state = descr.Set(cdata, value);
                            switch(state)
                            {
                                case ValueState.DataError:
                                    if(logging) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "]<" + descr.descriptor + "> multi contextData[" + cdata.GetType().Name + "]"
                                                            + "\n\treason: unable to set value"
                                                            + "\n\t@file: " + RichText.italic(currentFile));
                                    break;

                                case ValueState.Invalid:
                                    if(logValueError) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "]<" + descr.descriptor + "> multi contextData[" + cdata.GetType().Name + "]"
                                                            + "\n\treason: invalid value"
                                                            + "\n\t@file: " + RichText.italic(currentFile));
                                    break;
                            }
                        }
                        break;
                }
            }
            else
            {
                state = descr.Set(dataTarget, value);
                switch(state)
                {
                    case ValueState.DataError:
                        if(logging) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "]<" + descr.descriptor + ">"
                                                + "\n\treason: unable to set value"
                                                + "\n\t@file: " + RichText.italic(currentFile));
                        break;
                    case ValueState.Invalid:
                        if(logValueError) Debug.LogWarning(Name + ":: Error solving [" + descr.context + "] <" + descr.descriptor + ">"
                                                + "\n\treason: invalid value"
                                                + "\n\t@file: " + RichText.italic(currentFile));
                        break;
                }
            }
            return state;
        }

    }

}