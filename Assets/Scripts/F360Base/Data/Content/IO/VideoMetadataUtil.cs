using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

using VRIntegration.Video;
using F360.Users.Stats;


namespace F360.Data.IO.Video
{


    public delegate bool DataSetter(VideoMetaData target, object val);
    public delegate bool ClipDataSetter(VideoClip target, object val);
    

    //-----------------------------------------------------------------------------------------------------------------
    //
    //  CONSTRAINT
    //
    //-----------------------------------------------------------------------------------------------------------------


    /// @brief  
    /// allows to only write specific contexts
    ///
    [System.Serializable]
    public struct WriteConstraint : Utility.Config.IWriteConstraint<Context>
    {   
        public static WriteConstraint All   { get; private set; } = new WriteConstraint(true, true, true, true, true);


        [Tooltip("write/override video information")]
        public bool Write_Meta;
        [Tooltip("write/override intent event")]
        public bool Write_Intents;
        [Tooltip("write/override awareness data")]
        public bool Write_BA;
        [Tooltip("write/override hazard data")]
        public bool Write_HZ;
        [Tooltip("write/override clips")]
        public bool Write_Clips;

        public WriteConstraint(bool meta, bool intent, bool ba, bool hz, bool clip)
        {
            this.Write_Meta = meta;
            this.Write_Intents = intent;
            this.Write_BA = ba;
            this.Write_HZ = hz;
            this.Write_Clips = clip;
        }

        public bool hasContext(Context ctx)
        {
            switch(ctx)
            {
                case Context.Meta:      return Write_Meta;
                case Context.Intent:    return Write_Intents;
                case Context.BA:        return Write_BA;
                case Context.HZ:        return Write_HZ;
                case Context.Clip:      return Write_Clips;
                default:                return false;
            }
        }
    }


    //-----------------------------------------------------------------------------------------------------------------



    public static class VideoMetaDataUtil
    {


        public const string FILE_EXTENSION = ".vmeta";
        public const string TOKEN_PACKED_META_PREFIX = "[ ";
        public const string TOKEN_PACKED_META_SUFFIX = " ]";


        public const int PACK_LINE_SPACING = 5;
        public const string TOKEN_UNDEFINED = "UNDEFINED";


        //-----------------------------------------------------------------------------------------------------------------



        /// @brief
        /// reformats existing file for consistent readability
        ///
        public static bool ReformatTextContent(string pathToFile)
        {
            try
            {
                if(File.Exists(pathToFile))
                {
                    var currentContext = Context.None;
                    var b = new System.Text.StringBuilder();
                    foreach(var l in File.ReadLines(pathToFile))
                    {
                        string line = l.Trim();
                        if(!string.IsNullOrEmpty(line))
                        {
                            Context next;
                            if(VideoMetaDataUtil.CheckNewContext(line, out next))
                            {
                                if(next != currentContext)
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
        //  DESCRIPTORS
        //
        //-----------------------------------------------------------------------------------------------------------------

        /// @brief
        /// registered value entries
        ///
        static Dictionary<string, Entry> descriptors;


        static VideoMetaDataUtil()
        {
            descriptors = new Dictionary<string, Entry>();
            Meta.CreateDescriptors();
            Clip.CreateDescriptors();
            Intent.CreateDescriptors();
            BA.CreateDescriptors();
            HZ.CreateDescriptors();
        }

        internal static bool HasDescriptor(string descr)
        {
            return descriptors.ContainsKey(descr);
        }

        internal static Entry GetDescriptor(string descr)
        {
            if(descriptors.ContainsKey(descr)) return descriptors[descr];
            else return default(Entry);
        }

        internal static void addDescriptor(string descriptor, Context context, ValueType type)
        {
            if(!descriptors.ContainsKey(descriptor))
            {
                descriptors.Add(descriptor, new Entry(context, descriptor, type));
            }
        }
        internal static void addDescriptor(string descriptor, Context context, ValueType type, DataSetter setter)
        {
            if(!descriptors.ContainsKey(descriptor))
            {
                descriptors.Add(descriptor, new Entry(context, descriptor, type, setter));
            }
        }
        internal static void addDescriptor(string descriptor, Context context, ValueType type, ClipDataSetter setter)
        {
            if(!descriptors.ContainsKey(descriptor))
            {
                descriptors.Add(descriptor, new Entry(context, descriptor, type, setter));
            }
        }




        //-----------------------------------------------------------------------------------------------------------------
        //
        //  INTERNAL
        //
        //-----------------------------------------------------------------------------------------------------------------


        internal static bool LineMatchesContext(string descriptor, Context context)
        {
            switch(context)
            {
                case Context.Meta:  
                case Context.Clip:
                case Context.Intent:  
                
                    return descriptors.ContainsKey(descriptor) && descriptors[descriptor].context == context;

                case Context.BA:
                case Context.HZ:    
                
                    return true;        //  validate ba/lookdata in respective datasets      
                
                default:            
                
                    return false;
            } 
        }

        internal static bool CheckNewContext(string line, out Context ctx)
        {
            switch(line)
            {
                case Meta.CONTEXT_TERM:
                    ctx = Context.Meta;
                    return true;
                case Clip.CONTEXT_TERM:
                    ctx = Context.Clip;
                    return true;
                case BA.CONTEXT_TERM: 
                    ctx = Context.BA;
                    return true;
                case HZ.CONTEXT_TERM:
                    ctx = Context.HZ;
                    return true;
                case Intent.CONTEXT_TERM:
                    ctx = Context.Intent;
                    return true;
                default:
                    ctx = Context.None;
                    return false;
            }
        }

        internal static bool GetDescriptor(string line, Context context, out string descriptor, out string content)
        {
            descriptor = "";
            content = "";

            int id = line.IndexOf(":");
            if(id != -1)
            {
                descriptor = line.Substring(0, id);
                descriptor = descriptor.Trim();
                content = line.Substring(id+1, line.Length-id-1);
                if(descriptors.ContainsKey(descriptor))
                {   
                    content = content.Trim();
                    return true;
                }
            }
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  PACKING
        //
        //-----------------------------------------------------------------------------------------------------------------


        public static string GetPackedFileName(int packing, int packID, string ext = VideoMetaDataUtil.FILE_EXTENSION)
        {
            int start = (packID * packing) + 1;
            int end = start + packing - 1;
            return F360FileSystem.FormatVideoID(start) + "-" + F360FileSystem.FormatVideoID(end) + ext;
        }

        public static bool isPackedFile(string fileName)
        {
            int p1, p2;
            return UnpackFile(fileName, out p1, out p2);
        }

        public static bool UnpackFile(string fileName, out int packing, out int packID)
        {
            if(fileName.EndsWith(VideoMetaDataUtil.FILE_EXTENSION))
            {
                fileName = F360FileSystem.RemoveFolderFromFilePath(fileName);
                fileName = F360FileSystem.RemoveFileExtension(fileName);
                string[] splt = fileName.Split('-');
                if(splt.Length == 2)
                {
                    int n1, n2;
                    if(F360FileSystem.ParseVideoID(splt[0].Trim(), out n1)
                        && F360FileSystem.ParseVideoID(splt[1].Trim(), out n2))
                    {
                        packID = n1;
                        packing = n2-n1;
                        return true;   
                    }
                }
            }
            packing = -1;
            packID = -1;
            return false;
        }

        internal static bool GetPackingContext(string line, out int videoID)
        {
            string inner;
            if(F360FileSystem.TryFormatString(ref line, VideoMetaDataUtil.TOKEN_PACKED_META_PREFIX, VideoMetaDataUtil.TOKEN_PACKED_META_SUFFIX, out inner, addTokensToInnerTerm: false))
            {
                inner = inner.Trim();
                return F360FileSystem.ParseVideoID(inner, out videoID);
            }
            videoID = -1;
            return false;
        }

        

        internal static bool ValidatePackingIndices(int packing, int packID, IEnumerable<VideoMetaData> data)
        {
            foreach(var d in data)
            {
                if(!ValidatePackingIndex(packing, packID, d.videoID)) return false;
            }
            return true;
        }

        internal static bool ValidatePackingIndex(int packing, int packID, int videoID)
        {
            if(packing > 1)
                return (videoID-1) / packing == packID;
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------------



        internal struct Entry
        {
            public string descriptor;
            public Context context;
            public ValueType type;
            DataSetter dataSetter;
            ClipDataSetter clipSetter;

            public Entry(Context context, string descr, ValueType type)
            {
                this.descriptor = descr;
                this.context = context;
                this.type = type;
                this.dataSetter = null;
                this.clipSetter = null;
            }
            public Entry(Context context, string descr, ValueType type, DataSetter setter)
            {
                this.descriptor = descr;
                this.context = context;
                this.type = type;
                this.dataSetter = setter;
                this.clipSetter = null;
            }
            public Entry(Context context, string descr, ValueType type, ClipDataSetter setter)
            {
                this.descriptor = descr;
                this.context = context;
                this.type = type;
                this.dataSetter = null;
                this.clipSetter = setter;
            }

            public bool hasSetter()
            {
                return dataSetter != null || clipSetter != null;
            }

            public bool Set(VideoMetaData target, object val)
            {
                if(dataSetter != null) {
                    return dataSetter(target, val);
                }
                return false;
            }
            public bool Set(VideoClip target, object val)
            {
                if(clipSetter != null) {
                    return clipSetter(target, val);
                }
                return false;
            }
        }



        internal struct Term
        {
            public Context context;
            public string descriptor;
            public string content;
            
            public string rawLine;
            public int index;               ///< index within context (0 if line opens new context)

            public Term(Context context, string line, int index)
            {
                this.rawLine = line;
                this.context = context;
                this.index = index;
                
                string dc, c;
                if(GetDescriptor(line, context, out dc, out c))
                {
                    descriptor = dc;
                    content = c;
                }
                else 
                {
                    descriptor = "";
                    content = line;
                }
            }

            public Term(Context context, string descriptor, string content, int index)
            {
                this.rawLine = descriptor + ": " + content;
                this.context = context;
                this.descriptor = descriptor;
                this.content = content;
                this.index = index;
            }

            public bool isNewContext()
            {
                return index == 0;
            }

            public bool hasDescriptor()
            {
                return !string.IsNullOrEmpty(descriptor);
            }

            public bool isValid()
            {
                return context != Context.None && !string.IsNullOrEmpty(content);
            }
        }


    }


}