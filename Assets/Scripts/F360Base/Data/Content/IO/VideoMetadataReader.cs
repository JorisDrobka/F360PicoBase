using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using System.IO;

using VRIntegration.Video;

using F360.Users.Stats;
using Term = F360.Data.IO.Video.VideoMetaDataUtil.Term;


namespace F360.Data.IO.Video
{

    /// @brief
    /// interface for reading VideoMetaData
    ///
    public static class VideoMetaDataReader
    {
        static bool logging = false;


        static VideoMetaData dataTarget;
        static Context currentContext;
        static int lineIndexer;
        static List<Term> termBuffer;
        static List<VideoClip> clipBuffer;
        static List<VideoMetaData> dataBuffer;
        

        static VideoMetaDataReader()
        {
            Prepare();
        }

        internal static void Prepare()
        {
            termBuffer = new List<Term>();
            clipBuffer = new List<VideoClip>();
            dataBuffer = new List<VideoMetaData>();
        }

        internal static void Flush()
        {
            termBuffer = null;
            clipBuffer = null;
            dataBuffer = null;
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  LOADER INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------


        /// @brief
        /// parses VideoMetaData from a .vmeta file, either single or multiple
        ///
        public static bool TryParseMetaFile(string pathToFile, out VideoMetaData[] data, bool debug=false)
        {
            string fileName = F360FileSystem.RemoveFolderFromFilePath(pathToFile);
            if(VideoMetaDataUtil.isPackedFile(fileName))
            {
                if(debug) Debug.Log("Parse Metafile<" + fileName + "> ---> PACKED FILE");
                return TryPackParsing(pathToFile, out data, debug);
            }
            else
            {
                if(debug) Debug.Log("Parse Metafile<" + fileName + "> ---> Single File");
                VideoMetaData single;
                if(TryParse(pathToFile, out single, debug))
                {
                    data = new VideoMetaData[] { single };
                    return true;
                }
            }
            data = new VideoMetaData[0];
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  SINGLE FILE INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------


        /// @brief
        /// loads runtime data from metafile
        ///
        public static bool TryParse(string pathToFile, out VideoMetaData data, bool debug=false)
        {
            logging = debug;
            data = null;
            try
            {
                if(File.Exists(pathToFile))
                {
                    dataTarget = new VideoMetaData();
                    //clipBuffer.Clear();
                    //termBuffer.Clear();
                    lineIndexer = 0;

                    var b = new System.Text.StringBuilder();
                    using(var stream = new FileStream(pathToFile, FileMode.OpenOrCreate))
                    {
                        using(var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                        {
                            parseInternal(reader);
                            reader.Close();
                        }
                        stream.Close();
                    }

                    
                    /*foreach(var line in File.ReadLines(pathToFile))
                    {
                        b.Append("\n" + line);
                        tryReadLine(line);
                        lineIndexer++;
                    }*/


                    tryReadLine("");
                    dataTarget.clips = clipBuffer.ToArray();

                    if(logging)
                    {
                        Debug.Log("VideoMetaDataReader AFTER Parse:: " + lineIndexer + "\n" + b.ToString());
                        Debug.Log("PARSED:\n" + dataTarget.Readable(true, true, true));
                    }

                    if(dataTarget.videoID >= 0)
                    {
                        dataTarget.media = MediaData.CreateVideo(pathToFile, dataTarget.durationMs);
                        data = dataTarget;
                        return data.isValid();
                    }
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


        /// @brief
        /// reformats existing file for consistent readability
        ///
        public static bool ReformatTextContent(string pathToFile)
        {
            return VideoMetaDataUtil.ReformatTextContent(pathToFile);
        }


        public static string GetPackedFileName(int packing, int packID, string ext = VideoMetaDataUtil.FILE_EXTENSION)
        {
            return VideoMetaDataUtil.GetPackedFileName(packing, packID, ext);
        }

        public static bool isPackedFile(string fileName)
        {
            return VideoMetaDataUtil.isPackedFile(fileName);
        }

        public static bool UnpackFile(string fileName, out int packing, out int packID)
        {
            return VideoMetaDataUtil.UnpackFile(fileName, out packing, out packID);
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  PACKING INTERFACE
        //
        //-----------------------------------------------------------------------------------------------------------------


        /// @brief
        /// parses a file with multiple clips
        ///
        public static bool TryPackParsing(string pathToFile, out VideoMetaData[] data, bool debug=false)
        {
            logging = debug;
            try
            {
                if(File.Exists(pathToFile))
                {
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
                                if(logging) Debug.Log("PackParse.. [" + currVideoID + "]");
                            }
                            reader.Close();
                        }
                        stream.Close();
                    }
                    if(dataBuffer.Count > 0)
                    {
                        data = dataBuffer.ToArray();
                        dataBuffer.Clear();
                        return true;
                    }
                    else if(lineIndexer > 0)
                    {
                        //  try single read
                        VideoMetaData single;
                        if(TryParse(pathToFile, out single, debug))
                        {
                            data = new VideoMetaData[] { single };
                            return true;
                        }
                    }
                }
            }
            catch(IOException ex)
            {
                Debug.LogError(ex.Message);
            }

            data = new VideoMetaData[0];
            return false;
        }


        static bool tryParseUntilNextPack(string pathToFile, StreamReader reader, ref string currentLine, ref int currVideoID)
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
                    if(VideoMetaDataUtil.GetPackingContext(line, out vID))
                    {
                        currVideoID = vID;
                        dataTarget = new VideoMetaData();
                        currentLine = parseInternal(reader);
                        tryReadLine("");
                        dataTarget.clips = clipBuffer.ToArray();

                        /*if(logging)
                        {
                            Debug.Log("PARSED:\n" + dataTarget.Readable(true, true, true));
                        }*/

                        if(dataTarget.videoID >= 0)
                        {
                            dataTarget.media = MediaData.CreateVideo(pathToFile, dataTarget.durationMs);
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
        //  READ
        //
        //-----------------------------------------------------------------------------------------------------------------


        static string parseInternal(StreamReader reader, System.Text.StringBuilder b=null)
        {
            clipBuffer.Clear();
            termBuffer.Clear();

            string line = "";
            while((line = reader.ReadLine()) != null)
            {
                if(line.Contains(VideoMetaDataUtil.TOKEN_PACKED_META_PREFIX))
                {
                    //  encountered next packed video meta
                    break;
                }
                else
                {
                    if(tryReadLine(line))
                    {
                        lineIndexer++;
                    }
                    if(b != null)
                    {
                        b.Append("\n");
                        b.Append(line);
                    }
                }
            }
            return line;
        }

        static bool filterLine(string line)
        {
            if(!string.IsNullOrEmpty(line))
            {
                if(line.Length >= 2)
                {
                    var begin = line.Substring(0, 2);
                    return begin != "//"
                        && begin != "--"
                        && begin != "__";
                }
                return true;
            }
            return false;
        }

        static bool tryReadLine(string line)
        {
            if(filterLine(line))
            {
                Context ctx;
                if(VideoMetaDataUtil.CheckNewContext(line, out ctx))
                {
                    if(logging) Debug.Log(RichText.emph("New Context:: ") + ctx);
                    if(currentContext != Context.None)
                    {
                        resolveTerms();
                    }

                    //  begin new context
                    termBuffer.Clear();
                    currentContext = ctx;
                    if(ctx == Context.Clip)
                    {
                        //  new video clip
                        clipBuffer.Add(new VideoClip(dataTarget));
                    }
                    return true;
                }
                else
                {
                    string descr, content;
                    if(VideoMetaDataUtil.GetDescriptor(line, currentContext, out descr, out content))
                    {
                        if(VideoMetaDataUtil.LineMatchesContext(descr, currentContext))
                        {
                            termBuffer.Add(
                                new Term(currentContext, descr, content, lineIndexer)
                            );
                            return true;
                        }
                        else
                        {
                            //  line format error
                        }
                    }
                    else if(VideoMetaDataUtil.LineMatchesContext(line, currentContext))
                    {
                        termBuffer.Add(
                            new Term(currentContext, line, lineIndexer)
                        );
                        return true;
                    }
                    else
                    {   
                        //  line format error
                    }
                }
            }
            else
            {
                if(currentContext != Context.None)
                {
                    resolveTerms();
                    termBuffer.Clear();
                    currentContext = Context.None;
                }
            }
            return false;
        }

        static bool resolveTerms()
        {
            if(logging)
            {
                var b = new System.Text.StringBuilder(RichText.darkRed("RESOLVE CONTEXT:: ") + currentContext.ToString() + " termbuffer: " + termBuffer.Count.ToString());
                for(int i = 0; i < termBuffer.Count; i++)
                {
                    b.Append("\n\t" + termBuffer[i].rawLine + " [" + termBuffer[i].descriptor + ":" + termBuffer[i].content + "]");
                }
                Debug.Log(b);
            }
            

            if(currentContext == Context.BA)
            {
                //  parse awareness data from termbuffer
                dataTarget.looks = LookData.Parse(dataTarget.videoID, termBuffer.Select(x=> x.rawLine).ToArray());
                if(dataTarget.looks != null)
                {
                    return true;
                }
                else
                {
                    Debug.LogWarning("MetadataReader:: Error reading [ba] context!");
                    return false;
                }
            }
            else if(currentContext == Context.HZ)
            {
                //  parse hazard data from termbuffer
                HazardPerceptionData data;
                if(HazardPerceptionData.TryParse(out data, termBuffer.Select(x=> x.rawLine).ToArray()))
                {
                    dataTarget.hazards = data;
                    return true;
                }
                else
                {
                    Debug.LogWarning("MetadataReader:: Error reading [hazard] context!");
                }
                return false;
            }
            else if(currentContext == Context.Intent)
            {
                //  parse intent data from termbuffer
                DriverIntentData data;
                if(DriverIntentData.TryParse(out data, termBuffer.Select(x=> x.rawLine).ToArray()))
                {
                    dataTarget.intents = data;
                    return true;
                }
                else
                {
                    Debug.LogWarning("MetadataReader:: Error reading [intent] context!");
                }
                return false;
            }
            else if(currentContext != Context.None)
            {
                //  set meta/clipdata by descriptor
                bool error = false;
                for(int i = 0; i < termBuffer.Count; i++)
                {
                    if(VideoMetaDataUtil.HasDescriptor(termBuffer[i].descriptor))
                    {
                        var descr = VideoMetaDataUtil.GetDescriptor(termBuffer[i].descriptor);
                        if(descr.hasSetter())
                        {
                            object val;
                            if(parseValue(termBuffer[i].content, descr.type, out val))
                            {
                                
                                switch(currentContext)
                                {
                                    case Context.Meta:
                                        if(!descr.Set(dataTarget, val))
                                        {
                                            error = true;
                                        } 
                                        break;

                                    case Context.Clip:
                                        if(!descr.Set(clipBuffer[clipBuffer.Count-1], val))
                                        {
                                            error = true;
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                //  parse error
                                error = true;
                            }
                        }
                    }
                    else
                    {
                        //  missing descriptor error
                        error = true;
                    }
                }
                return !error;
            }
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------


        static bool parseValue(string raw, ValueType type, out object val)
        {
            switch(type)
            {
                case ValueType.Context:

                    Context ctx;
                    if(VideoMetaDataUtil.CheckNewContext(raw, out ctx))
                    {
                        val = ctx;
                        return true;
                    }
                    break;

                case ValueType.TextMultiple:
                    
                    string[] txt;
                    if(parseMultipleText(raw, out txt))
                    {
                        val = txt;
                        return true;
                    }
                    break;

                case ValueType.Number:

                    int num;
                    if(parseNumber(raw, out num))
                    {
                        val = num;
                        return true;
                    }
                    break;

                case ValueType.Time:

                    int timeMs;
                    if(parseTimeMs(raw, out timeMs))
                    {
                        val = timeMs;
                        return true;
                    }
                    break;

                case ValueType.TimeRange:

                    RangeInt timing;
                    if(parseTimeRange(raw, out timing))
                    {
                        val = timing;
                        return true;
                    }
                    break;

                case ValueType.Category:

                    Category c;
                    if(parseCategory(raw, out c))
                    {
                        val = c;
                        return true;
                    }
                    break;

                case ValueType.Location:
                    DriveLocation loc;
                    if(parseLocation(raw, out loc))
                    {
                        val = loc;
                        return true;
                    }
                    break;

                case ValueType.Text:
                case ValueType.BATerm:
                case ValueType.HZTerm:
                case ValueType.IntentTerm:  
                    
                    val = raw;              //  just keep terms as string
                    return true;
            }

            val = null;             
            return false;
        }

        static bool parseMultipleText(string raw, out string[] txt)
        {
            txt = raw.Split(';');
            for(int i = 0; i < txt.Length; i++)
            {
                txt[i] = txt[i].Trim();
            }
            return txt.Length > 0;
        }

        static bool parseNumber(string raw, out int num)
        {
            return F360FileSystem.ParseIntegerFromDataset(raw, out num);
        }

        static bool parseTimeMs(string raw, out int timeMs)
        {
            return F360FileSystem.ParseTimeFromSecondsDataset(raw, out timeMs);
        }

        static bool parseTimeRange(string raw, out RangeInt timing)
        {
            return F360FileSystem.ParseTimeRangeFromSecondsDataset(raw, out timing);
        }

        static bool parseCategory(string raw, out Category c)
        {
            var id = raw.IndexOf(".");
            if(id != -1)
            {
                var splt = raw.Split('.');
                if(splt.Length == 2)
                {
                    int main, sub;
                    if(parseNumber(splt[0], out main)
                        && parseNumber(splt[1], out sub))
                    {
                        c = MainCategories.GetByGroup(main, sub);   
                        return c != null;
                    }
                }
            }
            c = null;
            return false;
        }

        static bool parseLocation(string raw, out DriveLocation loc)
        {
            return TrafficHelper.ParseLocation(raw, out loc);
        }


        


    }




}

