using UnityEngine;

using F360.Users.Stats;


using Utility.Config;

namespace F360.Data.IO.Video
{



    /// definitions for Metadata I/O


    public enum Context     //  in order of appearance in meta file
    {
        None=0,
        Meta, 
        Intent,
        BA,
        HZ,
        Clip,
    }


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  META DESCRIPTORS
    //
    //-----------------------------------------------------------------------------------------------------------------


    internal static class Meta
    {
        public const string CONTEXT_TERM = "meta";      
        public const string VIDEO_ID = "id";            
        public const string LOCATION = "location";          static string[] TYPE_ALIASES = new string[] { "ort", "type", "typ" };
        public const string LECTION = "lecture";            static string[] LECTION_ALIASES = new string[] { "lektion", "lection" };
        public const string DURATION = "duration";          static string[] DURATION_ALIASES = new string[] { "dauer" };
        public const string DIFFICULTY = "difficulty";      static string[] DIFFICULTY_ALIASES = new string[] { "schwierigkeit", "schwere" };
        public const string COORDS = "coordinates";         static string[] COORD_ALIASES = new string[] { "coords", "koordinaten" };


        public static void CreateDescriptors(CustomConfigSetup<VideoMetaData, Context, ValueType> setup)
        {
            //  new Config system
            setup.AddContextDescriptor(CONTEXT_TERM, Context.Meta, ValueType.Context);
            setup.AddDescriptor<int>(VIDEO_ID, Context.Meta, ValueType.Number, setID);
            setup.AddDescriptor<int>(LECTION, Context.Meta, ValueType.Number, setLection, LECTION_ALIASES);
            setup.AddDescriptor<DriveLocation>(LOCATION, Context.Meta, ValueType.Location, setLoc, TYPE_ALIASES);
            setup.AddDescriptor<int>(DURATION, Context.Meta, ValueType.Time, setDuration, DURATION_ALIASES);
            setup.AddDescriptor<int>(DIFFICULTY, Context.Meta, ValueType.Number, setDifficulty, DIFFICULTY_ALIASES);
            setup.AddDescriptor<string>(COORDS, Context.Meta, ValueType.Text, setCoords, COORD_ALIASES);
        }

        public static void CreateDescriptors()
        {
            VideoMetaDataUtil.addDescriptor(CONTEXT_TERM, Context.Meta, ValueType.Context);
            VideoMetaDataUtil.addDescriptor(VIDEO_ID, Context.Meta, ValueType.Number, setID2);
            VideoMetaDataUtil.addDescriptor(LOCATION, Context.Meta, ValueType.Location, setLoc2);
            VideoMetaDataUtil.addDescriptor(DURATION, Context.Meta, ValueType.Time, setDuration2);
            VideoMetaDataUtil.addDescriptor(DIFFICULTY, Context.Meta, ValueType.Number, setDifficulty2);
            VideoMetaDataUtil.addDescriptor(COORDS, Context.Meta, ValueType.Text, setCoords2);
        }

        //  setter NEW

        static bool setID(VideoMetaData data, int id) { data.videoID = id; return data.videoID > 0; }
        static bool setLoc(VideoMetaData data, DriveLocation loc) { data.location = loc; return data.location != DriveLocation.Undefined; }
        static bool setLection(VideoMetaData data, int id) { data.lectureID = id; return data.lectureID > 0; }
        static bool setDuration(VideoMetaData data, int durationMs) { data.durationMs = durationMs; return data.durationMs > 100; }
        static bool setDifficulty(VideoMetaData data, int difficulty) { data.difficulty = difficulty; return data.difficulty >= 0; }
        static bool setCoords(VideoMetaData data, string coords) { data.geoCoordinates = coords; return string.IsNullOrEmpty(data.geoCoordinates); }

        //  setter OLD
        static bool setID2(VideoMetaData data, object id) 
        { 
            if(id is int) {
                data.videoID = (int)id;
            } 
            return false; 
        }
        static bool setLoc2(VideoMetaData data, object loc) 
        { 
            if(loc is DriveLocation) {
                data.location = (DriveLocation)loc;
                return true;
            } 
            return false; 
        }
        static bool setLection2(VideoMetaData data, object id) 
        { 
            if(id is int) {
                data.lectureID = (int)id;
                return true;
            } 
            return false; 
        }
        static bool setDuration2(VideoMetaData data, object durationMs) 
        { 
            if(durationMs is int) {
                data.durationMs = (int)durationMs;
                return true;
            } 
            return false; 
        }
        static bool setDifficulty2(VideoMetaData data, object difficulty) 
        {
            if(difficulty is int) {
                data.difficulty = (int)difficulty; 
                return true;
            }
            return false; 
        }
        static bool setCoords2(VideoMetaData data, object coords) 
        { 
            if(coords is string) {
                data.geoCoordinates = (string)coords;
            } 
            return false; 
        }
    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  CLIP DESCRIPTORS
    //
    //-----------------------------------------------------------------------------------------------------------------


    internal static class Clip
    {
        public const string CONTEXT_TERM = "clip";
        public const string CLIP_ID = "id";
        public const string CLIP_NAME = "name";
        public const string CATEGORY = "category";          static string[] CATEGORY_ALIASES = new string[] { "kategorie" };
        public const string TAGS = "tags";                  static string[] TAG_ALIASES = new string[] { "tag", "tagging" };
        public const string TIMING = "timing";              static string[] TIMING_ALIASES = new string[] { "zeit", "time" };
        public const string DESCRIPTION = "description";    static string[] DESCR_ALIASES = new string[] { "beschreibung", "descr" };
        public const string SIGN = "sign";                  static string[] SIGN_ALIASES = new string[] { "zeichen", "verkehrszeichen" };
        public const string ROW = "row";                    static string[] ROW_ALIASES = new string[] { "vorfahrt", "rightofway" };
        public const string QUESTION = "question";          static string[] QUESTION_ALIASES = new string[] { "frage" };



        public static void CreateDescriptors(CustomConfigSetup<VideoMetaData, Context, ValueType> setup)
        {
            //  new Config system
            setup.AddContextDescriptor(CONTEXT_TERM, Context.Clip, ValueType.Context);
            setup.AddDescriptor<VideoClip, int>(CLIP_ID, Context.Clip, ValueType.Number, setClipID);
            setup.AddDescriptor<VideoClip, string>(CLIP_NAME, Context.Clip, ValueType.Text, setClipName);
            setup.AddDescriptor<VideoClip, Category>(CATEGORY, Context.Clip, ValueType.Category, setCategory, CATEGORY_ALIASES);
            setup.AddDescriptor<VideoClip, string[]>(TAGS, Context.Clip, ValueType.TextMultiple, setTags, TAG_ALIASES);
            setup.AddDescriptor<VideoClip, RangeInt>(TIMING, Context.Clip, ValueType.TimeRange, setTiming, TIMING_ALIASES);
            setup.AddDescriptor<VideoClip, string>(DESCRIPTION, Context.Clip, ValueType.Text, setDescription, DESCR_ALIASES);
            setup.AddDescriptor<VideoClip, string>(SIGN, Context.Clip, ValueType.Text, setTrafficSign, SIGN_ALIASES);
            setup.AddDescriptor<VideoClip, string>(ROW, Context.Clip, ValueType.Text, setRow, ROW_ALIASES);
        }

        internal static void CreateDescriptors()
        {
            VideoMetaDataUtil.addDescriptor(CONTEXT_TERM, Context.Clip, ValueType.Context);
            VideoMetaDataUtil.addDescriptor(CATEGORY, Context.Clip, ValueType.Category, setCategory2);
            VideoMetaDataUtil.addDescriptor(TAGS, Context.Clip, ValueType.TextMultiple, setTags2);
            VideoMetaDataUtil.addDescriptor(TIMING, Context.Clip, ValueType.TimeRange, setTiming2);
            VideoMetaDataUtil.addDescriptor(DESCRIPTION, Context.Clip, ValueType.Text, setDescription2);
            VideoMetaDataUtil.addDescriptor(SIGN, Context.Clip, ValueType.Text, setTrafficSign2);
            VideoMetaDataUtil.addDescriptor(ROW, Context.Clip, ValueType.Text, setRow2);
        }


        //  NEW setters

        static bool setClipID(VideoClip target, int id) { target.clipID = id; return target.clipID >= 0; }
        static bool setClipName(VideoClip target, string name) { target.name = (string)name; return !string.IsNullOrEmpty(target.name); }

        static bool setCategory(VideoClip target, Category category) { target.category = category; return target.category != null; }
        static bool setTags(VideoClip target, string[] tags) { target.tags = tags; return target.tags != null && target.tags.Length > 0; }
        static bool setTiming(VideoClip target, RangeInt timing) { target.timing = timing; return target.timing.length > 0; }
        static bool setDescription(VideoClip target, string description) { target.description = description; return !string.IsNullOrEmpty(target.description); }
        static bool setTrafficSign(VideoClip target, string signCode) { target.signCode = signCode; return !string.IsNullOrEmpty(target.signCode); }
        static bool setRow(VideoClip target, string rowCode) { target.rowCode = rowCode; return !string.IsNullOrEmpty(target.rowCode); }


        //  OLD setters
        static bool setCategory2(VideoClip target, object category) 
        {
            if(category is Category) target.category = (Category) category;
            return target.category != null;
        }
        static bool setTags2(VideoClip target, object tags) 
        {
            if(tags is string[]) target.tags = (string[]) tags;
            return target.tags != null && target.tags.Length > 0;
        }
        static bool setTiming2(VideoClip target, object timing) 
        {
            if(timing is RangeInt) target.timing = (RangeInt) timing;
            return target.timing.length > 0;
        }
        static bool setDescription2(VideoClip target, object description) 
        {
            if(description is string) target.description = (string) description;
            return !string.IsNullOrEmpty(target.description);
        }
        static bool setTrafficSign2(VideoClip target, object signCode) 
        {
            if(signCode is string) target.signCode = (string) signCode;
            return !string.IsNullOrEmpty(target.signCode);
        }
        static bool setRow2(VideoClip target, object rowCode) 
        {
            if(rowCode is string) target.rowCode = (string) rowCode;
            return !string.IsNullOrEmpty(target.rowCode);
        }

    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  BA DESCRIPTORS
    //
    //-----------------------------------------------------------------------------------------------------------------


    internal static class BA
    {
        public const string CONTEXT_TERM = "blickanalyse";      static string[] TERM_ALIASES = new string[] { "ba" };

        public static void CreateDescriptors(CustomConfigSetup<VideoMetaData, Context, ValueType> setup)
        {
            //  new Config system
            setup.AddContextDescriptor(CONTEXT_TERM, Context.BA, ValueType.Context, TERM_ALIASES);
            foreach(var descr in LookData.GetDescriptors())
            {
                setup.AddDescriptor(descr, Context.BA, ValueType.BATerm);
            }
        }

        internal static void CreateDescriptors()
        {
            VideoMetaDataUtil.addDescriptor(CONTEXT_TERM, Context.BA, ValueType.Context);
            foreach(var descr in LookData.GetDescriptors())
            {
                VideoMetaDataUtil.addDescriptor(descr, Context.BA, ValueType.BATerm);
            }
        }
    }


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  HZ DESCRIPTORS
    //
    //-----------------------------------------------------------------------------------------------------------------


    internal static class HZ
    {
        public const string CONTEXT_TERM = "gefahren";  static string[] TERM_ALIASES = new string[] { "hz" };

        public static void CreateDescriptors(CustomConfigSetup<VideoMetaData, Context, ValueType> setup)
        {
            //  new Config system
            setup.AddContextDescriptor(CONTEXT_TERM, Context.HZ, ValueType.Context, TERM_ALIASES);
        }

        internal static void CreateDescriptors()
        {
            VideoMetaDataUtil.addDescriptor(CONTEXT_TERM, Context.HZ, ValueType.Context);
        }
    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  INTENT DESCRIPTORS
    //
    //-----------------------------------------------------------------------------------------------------------------


    internal static class Intent
    {
        public const string CONTEXT_TERM = "intention";
        public const string TERM = "in";


        public static void CreateDescriptors(CustomConfigSetup<VideoMetaData, Context, ValueType> setup)
        {
            //  new Config system
            setup.AddContextDescriptor(CONTEXT_TERM, Context.Intent, ValueType.Context);
            foreach(var descr in DriverIntentData.GetDescriptors())
            {
                setup.AddDescriptor(descr, Context.Intent, ValueType.IntentTerm);
            }
        }

        internal static void CreateDescriptors()
        {
            VideoMetaDataUtil.addDescriptor(CONTEXT_TERM, Context.Intent, ValueType.Context);
            foreach(var descr in DriverIntentData.GetDescriptors())
            {
                VideoMetaDataUtil.addDescriptor(descr, Context.Intent, ValueType.IntentTerm);
            }

        }
    }

}