


#define SAVE_RAW_TERMS



using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace F360.Data
{


    public enum LookEventType
    {
        None=0,
        RearViewMirror=1,       //  DefaultMode=[ MinGaze, 0,5s ] Marker=[ icon ]
        SideMirror_L,           //  
        SideMirror_R,           //
        SideMirror_Any,         //
        ShoulderLook_L,         //  
        ShoulderLook_R,         //  


        FrontWindow,            //  DefaultMode=[ LookAway, 2s ] Marker=[ "Vorne" ]
        RearWindow,             //  DefaultMode=[ LookAway, 2s ] Marker=[ "Hinten" ]

        
        Check_Direction,        //  User must look in a direction, DefaultMode=[ MinGaze, 0,5s ] Marker=[ "Einsehen" ]
        Check_Direction_L,      //  
        Check_Direction_R,      //  
        Observe_Direction,      //  User must observe a direction. DefaultMode=[ LookAway, 2s ] Marker=[ "Einsehen" ]
        Observe_Direction_L,    //
        Observe_Direction_R,    //
        Observe_Situation,      //  User must observe the traffic. DefaultMode=[ LookAway, 2s ] Marker=[ "Beobachten" ]
        Follow_Marker,          //  User must follow marker. DefaultMode=[ LookAway, 2s ] Marker=[ "Folgen" ] 

        Console,                //  Car console
        Wheel,


        //  define maneuver types for serialization purposes 

        M_Spurwechsel_L,
        M_Spurwechsel_R,
        M_Uberholen,
        M_Abbiegen_L,
        M_Abbiegen_R,
        M_Vorbeifahren
    }


    public enum LookEventMode
    {
        None=0,
        MinGaze,        //  user completes event by looking for minimum gaze time
        LookAway        //  user fails event by looking away for given gaze time
    }

    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// contains all look events within a video
    ///
    public class LookData
    {

        /// @brief
        /// representation of a single look event
        ///
        public class Event
        {
            public LookEventMode mode;          ///< mode defines how event is resolved
            public LookEventType type;          ///< type of event
            public RangeInt timing;             ///< timing in milliseconds
            public int maneuverID;              ///< index of maneuver this event is part of,  or -1
            public string customLabel;          ///< marker label override
            public string customSubLabel;
            public int customLookTimeMs;        ///< mingaze/lookaway time override
            public Direction fixedDirection;    ///< 8-way direction marker can point to target
            public bool enableFixedDirection;   ///< marker will stay in fixedDirection
            public bool immediateMode;          ///< immediately start event, even if previous event isn't finished

            public int alignment;               ///< negative: left | 0: centered | positive: right


            public bool isValid(int duration)
            {
                return type != LookEventType.None && timing.start >= 0 && timing.start < duration;
            }

            public string GetMarkerLabel()
            {
                if(!string.IsNullOrEmpty(customLabel))
                {
                    return customLabel;
                }
                else
                {
                    return type.Readable(showAlignment: false);
                }
            }

            /// @returns minimum gaze/lookaway time in seconds
            public float GetGazeTime()
            {
                if(customLookTimeMs > 0)
                {
                    return customLookTimeMs / 1000f;
                }
                else
                {
                    return type.GetDefaultGazeTime(mode);
                }
            }

            public bool CheckTimeRange(int timeMs)
            {
                return timeMs >= timing.start && timeMs <= timing.end;
            }
            public bool CheckTimeRange(RangeInt range)
            {
                return CheckTimeRange(range.start) || CheckTimeRange(range.end);
            }

            /// @param lookdir 0 - forward, 1 - left, 2 - right
            /// @returns available error entries for this event.
            public IEnumerable<string> GetRecordKeys(int lookdir=0)
            {
                var byType = this.type.GetRecordKeys();
                foreach(var key in byType) yield return key;
                if(lookdir != 0)
                {
                    if(byType.Contains(LookEventUtil.KEY_CROSSING))
                    {
                        if(lookdir == 1) yield return LookEventUtil.KEY_CROSSING_LEFT;
                        else if(lookdir == 2) yield return LookEventUtil.KEY_CROSSING_RIGHT;
                    }
                }
            }

            public override string ToString()
            {
                var b = new System.Text.StringBuilder();
                b.Append("[" + mode.ToString() + "]-->" + type.ToString() + " [" + timing.start.ToString() + "-" + timing.end.ToString() + "]");
                if(!string.IsNullOrEmpty(customLabel))
                {
                    b.Append(" marker<" + customLabel + ">");
                }
                if(customLookTimeMs > 0)
                {
                    b.Append(" look[" + customLookTimeMs.ToString() + "]");
                }
                return b.ToString();
            }
            
        }




        //-----------------------------------------------------------------------------------------------------------------
        //
        //  PARSING
        //
        //-----------------------------------------------------------------------------------------------------------------



        static bool debug = false;

        public static IEnumerable<string> GetDescriptors()
        {
            foreach(var type in LookEventUtil.GetAllTypes())
            {
                yield return LookEventUtil.ReadableCode(type);
            }
            yield return SYMBOL_IMMEDIATE_MODE;
        }



        //  SYMBOLS

        public const string SYMBOL_IMMEDIATE_MODE = "[<<]";

        const char SYMBOL_TIMING_TERM = ':';
        const char SYMBOL_MARKER_OVERRIDE_START = '<';
        const char SYMBOL_MARKER_OVERRIDE_END = '>';
        const char SYMBOL_MARKER_TIME_START = '[';
        const char SYMBOL_MARKER_TIME_END = ']';
        const char SYMBOL_MARKER_DIRECTION_START = '{';
        const char SYMBOL_MARKER_DIRECTION_END = '}';
        const char SYMBOL_COMMENT_START = '(';
        const char SYMBOL_COMMENT_END = ')';

        const char SYMBOL_MANEUVER = '@';               //  next two characters indicate maneuver type & id


        public bool NotAvailableForSelection { get; set; } = false;

        List<Event> entries;
        List<Maneuver> maneuvers;

        #if SAVE_RAW_TERMS
        List<string> rawTerms = new List<string>();
        #endif

        public IEnumerable<string> GetRawTerms()
        {
            #if SAVE_RAW_TERMS
            return rawTerms;
            #else
            Debug.LogWarning("LookData:: RAW terms are not saved!");
            return new string[0];
            #endif
        }


        public int EventCount { get { return entries.Count; } }
        public int ManeuverCount { get { return maneuvers.Count; } }
        public int NonManeuverCount { get { return entries.Count(x=> x.maneuverID != -1); } }

        public Event GetEventAt(int index)
        {
            if(index >= 0 && index < entries.Count) return entries[index];
            else return new Event();
        }

        public bool HasEventOfType(LookEventType type)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(entries[i].type == type) return true;
            }
            return false;
        }
        public bool HasEventOfType(IEnumerable<LookEventType> types)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(types.Contains(entries[i].type)) return true;
            }
            return false;
        }
        public bool IsLookEventOfType(int index, LookEventType type)
        {
            return index >= 0 && index < entries.Count && entries[index].type == type;
        }
        public bool IsLookEventOfType(int index, IEnumerable<LookEventType> types)
        {
            return index >= 0 && index < entries.Count && types.Contains(entries[index].type);
        }
        public LookEventType GetTypeAt(int index)
        {
            return index >= 0 && index < entries.Count ? entries[index].type : LookEventType.None;
        }
        public IEnumerable<LookEventType> GetLookEventTypes()
        {
            foreach(var t in LookEventUtil.GetAllTypes())
            {
                for(int i = 0; i < entries.Count; i++)
                {
                    if(entries[i].type == t) {
                        yield return t;
                        break;
                    }
                }
            }
        }

        public Maneuver GetManeuver(int index)
        {
            if(maneuvers != null && index >= 0 && index < maneuvers.Count)
            {
                return maneuvers[index];
            }
            return null;
        }
        public bool IsManeuverOfType(int index, ManeuverType type)
        {
            return index >= 0 && index < ManeuverCount && maneuvers[index].type == type;
        }
        public bool IsManeuverOfType(int index, IEnumerable<ManeuverType> types)
        {
            return index >= 0 && index < ManeuverCount && types.Contains(maneuvers[index].type);
        }
        public ManeuverType GetManeuverTypeAt(int index)
        {
            return index >= 0 && index < ManeuverCount ? maneuvers[index].type : ManeuverType.Undefined;
        }
        public IEnumerable<Maneuver> GetAllManeuvers()
        {
            if(maneuvers != null) return maneuvers;
            else return new Maneuver[0];
        }
        public int GetManeuverIndexAtTime(float timeMs)
        {
            if(maneuvers != null && timeMs >= 0)
            {
                for(int i = 0; i < maneuvers.Count; i++) {
                    if(timeMs >= maneuvers[i].timing.start && timeMs <= maneuvers[i].timing.end) {
                        return i;
                    }
                }
            }
            return -1;
        }
        public int GetManeuverIndexFromEvent(int eventID)
        {
            if(maneuvers != null && eventID >= 0 && eventID < EventCount)
            {
                for(int i = 0; i < maneuvers.Count; i++) {
                    if(maneuvers[i].eventIDs.Contains(eventID)) return maneuvers[i].maneuverID;
                }
            }
            return -1;
        }

        public bool HasManeuverOfType(ManeuverType type)
        {
            return maneuvers != null && maneuvers.Exists(x=> x.type==type);
        }
        public bool HasManeuverOfType(IEnumerable<ManeuverType> types)
        {
            if(maneuvers != null) {
                foreach(var t in types) {
                    if(maneuvers.Exists(x=> x.type == t)) return true;
                }
            }
            return false;
        }
        public IEnumerable<Maneuver> GetManeuversOfType(ManeuverType type)
        {
            if(maneuvers != null) {
                foreach(var m in maneuvers) {
                    if(m.type == type) yield return m;
                }
            }
        }
        public IEnumerable<Maneuver> GetManeuversOfType(IEnumerable<ManeuverType> types)
        {
            if(maneuvers != null) {
                foreach(var m in maneuvers) {
                    if(types.Contains(m.type)) yield return m;
                }
            }
        }

        public IEnumerable<int> GetManeuverIndices(int maneuverID)
        {
            if(maneuverID >= 0 && maneuverID < ManeuverCount)
            {
                foreach(int id in maneuvers[maneuverID].eventIDs) yield return id;
            }
        }
        public IEnumerable<int> GetNonManeuverIndices()
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(entries[i].maneuverID < 0) yield return i;
            }
        }

        public int GetIndexAtTime(float timeMs)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(entries[i].CheckTimeRange((int)timeMs))// timeMs >= entries[i].timing.start && timeMs <= entries[i].timing.end)
                {
                    return i;
                }
            }
            return -1;
        }

        /// @returns wether event index is active at given time range
        ///
        public bool CheckIndexAtTime(int eventID, int timeMs)
        {
            if(eventID >= 0 && eventID < entries.Count)
            {
                return timeMs >= entries[eventID].timing.start 
                    && timeMs <= entries[eventID].timing.end;
            }
            return false;
        }
        public bool CheckIndexAtTimeRange(int eventID, RangeInt timeRangeMs)
        {
            if(eventID >= 0 && eventID < entries.Count)
            {
                return (timeRangeMs.start >= entries[eventID].timing.start 
                        && timeRangeMs.start <= entries[eventID].timing.end)
                    || (timeRangeMs.end >= entries[eventID].timing.start
                        && timeRangeMs.end <= entries[eventID].timing.end);
            }
            return false;
        }

        /// @returns wether maneuver is active at given time range
        ///
        public bool CheckManeuverAtTime(int maneuverID, int timeMs)
        {
            if(maneuverID >= 0 && maneuverID < ManeuverCount)
            {
                return timeMs >= maneuvers[maneuverID].timing.start
                    && timeMs <= maneuvers[maneuverID].timing.end;
            }
            return false;
        }
        public bool CheckManeuverAtTimeRange(int maneuverID, RangeInt timeRangeMs)
        {
            if(maneuverID >= 0 && maneuverID < ManeuverCount)
            {
                return (timeRangeMs.start >= maneuvers[maneuverID].timing.start
                        && timeRangeMs.start <= maneuvers[maneuverID].timing.end)
                    || (timeRangeMs.end >= maneuvers[maneuverID].timing.start
                        && timeRangeMs.end <= maneuvers[maneuverID].timing.end);
            }
            return false;
        }

        public string Print()
        {
            var b = new System.Text.StringBuilder("[LookEventData]");
            var i = 1;
            foreach(var e in entries)
            {
                b.Append("\n\t" + i.ToString() + ": " + e.ToString());
                i++;
            }
            return b.ToString();
        }


        //-----------------------------------------------------------------------------------------------------------------


        public static LookData Parse(int taskID, params string[] terms)
        {
            var result = new LookData();
            result.entries = new List<Event>();
            result.maneuvers = new List<Maneuver>();

            if(terms.Length > 0)
            {
                #if SAVE_RAW_TERMS                
                if(terms[0].ToLower().StartsWith("!is"))
                {
                    result.rawTerms.Add("!IS");
                }
                #endif

                if(debug)
                {
                    var bb = new System.Text.StringBuilder(RichText.emph("Looks[" + taskID + "] input:"));
                    for(int i = 0; i < terms.Length; i++) {
                        bb.Append("\n\t" + terms[i]);
                    }
                    Debug.Log(bb);
                }

                var b = new System.Text.StringBuilder(RichText.emph("[" + taskID.ToString() + "]") + " Parse LookEventData::");
                Maneuver currentManeuver = null;
                int lastManeuverID=-1;
                int maneuverStartT = -1;
                int maneuverEndT = -1;
                for(int i = 0; i < terms.Length; i++)
                {
                    terms[i] = terms[i].Trim();

                    var t = terms[i];
                    if(debug && !string.IsNullOrEmpty(t)) b.Append("\n\n\tterm=" + t);
                    
                    Event[] e;
                    ManeuverType maneuverType;
                    ManeuverType rawType;
                    int maneuverID;
                    tryParseManeuverID(ref t, out rawType, out maneuverID);
                    if(TryParseTerm(t, maneuverID, out e))
                    {
                        if(debug) b.Append(" parsed [" + e.Length + "]");
                        foreach(var ee in e)
                        {
                            if(!rawType.HasAlignment())
                            {
                                maneuverType = rawType.GetAlignedType(ee.type);
                            }
                            else
                            {
                                maneuverType = rawType;
                            }

                            result.entries.Add(ee);
                            if(maneuverID != -1 && maneuverType != ManeuverType.Undefined)
                            {
                                if(currentManeuver == null) 
                                {
                                    currentManeuver = new Maneuver(taskID, maneuverType, result.maneuvers.Count, ee.alignment);
                                    result.maneuvers.Add(currentManeuver);
                                    currentManeuver.AddEvent(result.entries.Count-1);
                                    lastManeuverID = maneuverID;
                                    maneuverStartT = ee.timing.start;
                                    maneuverEndT = ee.timing.end;
                 //                   b.Append("   |  first [" + maneuverID + "/" + maneuverType + "]  [" + maneuverStartT + " -> " + maneuverEndT + "]");
                                }
                                else if(currentManeuver.type.MatchUnalignedType(maneuverType)
                                    && maneuverID == lastManeuverID) 
                                {
                                    currentManeuver.AddEvent(result.entries.Count-1);
                                    maneuverEndT = ee.timing.end;
                 //                   b.Append("   |  append [" + maneuverID + "/" + maneuverType + "] [" + maneuverStartT + " -> " + maneuverEndT + "]");
                                }
                                else 
                                {
                 //                   b.Append("   |  next [" + maneuverID + "/" + maneuverType + "] last= " + currentManeuver.type + " / " + lastManeuverID + " [" + maneuverStartT + " -> " + maneuverEndT + "]");
                                    currentManeuver.timing = new RangeInt(maneuverStartT, maneuverEndT-maneuverStartT);

                                    currentManeuver = new Maneuver(taskID, maneuverType, result.maneuvers.Count, ee.alignment);
                                    result.maneuvers.Add(currentManeuver);
                                    currentManeuver.AddEvent(result.entries.Count-1);
                                    lastManeuverID = maneuverID;
                                    maneuverStartT = ee.timing.start;
                                    maneuverEndT = ee.timing.end;
                                }
                            }
                            if(debug) b.Append("\n\n\t\t--> added! {" + ee.ToString() + "}");
                        }
                        if(currentManeuver != null)
                        {
                            currentManeuver.timing = new RangeInt(maneuverStartT, maneuverEndT-maneuverStartT);
                        }

                        #if SAVE_RAW_TERMS
                        result.rawTerms.Add(terms[i]);
                        #endif
                    }
                }

                //  add IS event at start
                if(result.entries.Count > 0 
                    && (result.entries[0].type != LookEventType.RearViewMirror || result.entries[0].timing.start > 5) 
                    && !terms[0].ToLower().StartsWith("!is"))
                {
                    var e = new Event();
                    e.mode = LookEventMode.MinGaze;
                    e.type = LookEventType.RearViewMirror;
                    e.timing = new RangeInt(0, 4000);
                    result.entries.Insert(0, e);

                    if(result.maneuvers != null) {
                        foreach(var m in result.maneuvers) {
                            for(int i = 0; i < m.eventIDs.Count; i++) {
                                m.eventIDs[i]++;
                            }
                        }
                    }
                    if(result.entries.Count > 1 && result.entries[1].timing.start < 4000)
                    {
                        result.entries[1].immediateMode = true;
                        if(result.entries[1].timing.end <= 4000)
                        {
                            var t = result.entries[1].timing;
                            result.entries[1].timing = new RangeInt(t.start + 2000, t.length);      //  WORKAROUND::: Offset first entry position by 2 seconds if IS is put in front
                        }
                    }
                    
                }
                if(debug)
                {
                    if(result.maneuvers.Count > 0)
                    {
                        b.Append("\n%Maneuvers%");
                        foreach(var m in result.maneuvers)
                        {
                            b.Append("\n\t[" + m.maneuverID + "]: " + m.type.ToString());
                            foreach(var id in m.eventIDs) {
                                b.Append("\n\t\t" + id.ToString() + " - " + result.GetEventAt(id).type.ToString());
                            }
                        }
                    }
                    Debug.Log(b);
                }
            }
            return result;
        }


        static bool TryParseTerm(string term, int maneuverID, out Event[] entries)
        {
            if(term.Contains(SYMBOL_TIMING_TERM))
            {
                int lookTimeOverride = -1;
                string markerLabel = "";
                string sublabel = "";
                string comment = "";
                Direction direction;
                bool immediate = tryParseImmediateMode(ref term);
                tryParseComment(ref term, out comment);
                tryParseTimingOverride(ref term, out lookTimeOverride);
                tryParseMarkerOverride(ref term, out markerLabel, out sublabel);
                bool fixedDir = tryParseFixedDirection(ref term, out direction);

                /*if(fixedDir)
                {
                    Debug.Log(RichText.emph("Parsed fixed Direction") + " (" + direction + ")");
                }*/

                var timerSplit = new string[2];// = term.Split(SYMBOL_TIMING_TERM);
                var id = term.IndexOf(SYMBOL_TIMING_TERM);
                if(id != -1)
                {
                    timerSplit[0] = term.Substring(0, id);
                    timerSplit[1] = term.Substring(id+1, term.Length-id-1);

//                    Debug.Log("timersplit of term <" + term + "> :: " + timerSplit[0] + " || " + timerSplit[1]);
                }
                else
                {
                    entries = new Event[0];
                    return false;
                }

                if(timerSplit.Length == 2)
                {
                    LookEventType[] types;
                    RangeInt timing;
                    if(LookEventUtil.TryParse(timerSplit[0], out types, debug) && F360FileSystem.ParseTimeRangeFromSecondsDataset(timerSplit[1], out timing))
                    {
                        entries = new Event[types.Length];
                        for(int i = 0; i < types.Length; i++)
                        {
                            LookEventMode mode = types[i].GetDefaultMode();
                            tryParseModeOverride(timerSplit[0], ref mode);

                            entries[i] = new Event();
                            entries[i].mode = mode;
                            entries[i].type = types[i];
                            entries[i].timing = timing;
                            entries[i].maneuverID = maneuverID;
                            entries[i].customLabel = markerLabel;
                            entries[i].customSubLabel = sublabel;
                            entries[i].customLookTimeMs = lookTimeOverride;
                            entries[i].immediateMode = immediate;
                            entries[i].alignment = types[i].GetAlignment(comment);

                            if(entries[i].alignment != 0)
                            {
                                //  get aligned type
                                entries[i].type = types[i].GetAlignedType(entries[i].alignment);
                            }

                            if(false && entries[i].type == LookEventType.FrontWindow)
                            {
                                entries[i].enableFixedDirection = true;
                                entries[i].fixedDirection = Direction.north;
                            }
                            else
                            {
                                entries[i].enableFixedDirection = fixedDir;
                                entries[i].fixedDirection = direction;
                            }
                        }
                        return true;
                    }
                }
            }            
            entries = new Event[0];
            return false;
        }

        static bool tryParseManeuverID(ref string term, out ManeuverType type, out int mID)
        {
            mID = -1;
            type = ManeuverType.Undefined;

            int index = term.IndexOf(SYMBOL_MANEUVER);
            if(index != -1) {
                if(term.Length > index + 2) 
                {
                    string m = term.Substring(index+1, 2);
                    if(!(Maneuver.TryParse(m[0], out type) && System.Int32.TryParse(m.Substring(1), out mID)))
                    {
                        Debug.Log("Error parsing Maneuver from term=[" + term + "]");
                    }
                    string ls = term.Substring(0, index);
                    string rs = term.Substring(index+3);
                    term = ls + rs;
                    return true;
                }
            }
            return false;
        }

        static bool tryParseTimingOverride(ref string term, out int looktime)
        {
            int left = term.IndexOf(SYMBOL_MARKER_TIME_START);
            int right = term.IndexOf(SYMBOL_MARKER_TIME_END);
            if(left != -1 && right != -1 && left < right)
            {
                int millis;
                string s = term.Substring(left+1, right-left-1);
                if(F360FileSystem.ParseTimeFromSecondsDataset(s, out millis))
                {
                    looktime = millis;
                    
                    string ls = term.Substring(0, left);
                    string rs = term.Substring(right+1, term.Length-right-1);
                    term = ls + rs;
                    return true;
                }
            }

            looktime = -1;
            return false;
        }

        static bool tryParseFixedDirection(ref string term, out Direction dir)
        {
            int left = term.IndexOf(SYMBOL_MARKER_DIRECTION_START);
            int right = term.IndexOf(SYMBOL_MARKER_DIRECTION_END);
            if(left != -1 && right != -1 && left < right)
            {
                string s = term.Substring(left+1, right-left-1).ToLower();      
                switch(s)
                {
                    case "n":   dir = Direction.north; return true;
                    case "ne":  dir = Direction.northEast; return true;
                    case "e":   dir = Direction.east; return true;
                    case "se":  dir = Direction.southEast; return true;
                    case "s":   dir = Direction.south; return true;
                    case "sw":  dir = Direction.southWest; return true;
                    case "w":   dir = Direction.west; return true;
                    case "nw":  dir = Direction.northWest; return true;
                }
            }
            dir = Direction.north;
            return false;
        }

        static bool tryParseComment(ref string term, out string comment)
        {
            int left = term.IndexOf(SYMBOL_COMMENT_START);
            int right = term.IndexOf(SYMBOL_COMMENT_END);
            if(left != -1 && right != -1 && left < right)
            {
                string label = term.Substring(left+1, right-left-1);
                if(!string.IsNullOrEmpty(label))
                {
                    comment = label;
                    
                    string ls = term.Substring(0, left);
                    string rs = term.Substring(right+1, term.Length-right-1);
                    term = ls + rs;
                    return true;
                }
            }
            comment = "";
            return false;
        }

        static bool tryParseMarkerOverride(ref string term, out string markerlabel, out string sublabel)
        {
            int left = term.IndexOf(SYMBOL_MARKER_OVERRIDE_START);
            int right = term.IndexOf(SYMBOL_MARKER_OVERRIDE_END);
            if(left != -1 && right != -1 && left < right)
            {
                string label = term.Substring(left+1, right-left-1);
                if(!string.IsNullOrEmpty(label))
                {
                    string[] split = label.Split(';');
                    markerlabel = split[0];
                    sublabel = split.Length > 1 ? split[1] : "";
                    
                    string ls = term.Substring(0, left);
                    string rs = term.Substring(right+1, term.Length-right-1);
                    term = ls + rs;
                    return true;
                }
            }
            markerlabel = "";
            sublabel = "";
            return false;
        }

        static bool tryParseModeOverride(string term, ref LookEventMode mode)
        {
            string typeTerm = LookEventUtil.GetTypeTerm(term);
            term = term.Substring(typeTerm.Length, term.Length-typeTerm.Length).Trim();
//            Debug.Log("tryParseModeOverride:: [" + typeTerm + "] [" + term + "]");

            if(term.StartsWith("+"))
            {
                mode = LookEventMode.MinGaze;
                return true;
            }
            else if(term.StartsWith("-"))
            {
                mode = LookEventMode.LookAway;
                return true;
            }
            return false;
        }

        static bool tryParseImmediateMode(ref string term)
        {
            term = term.Trim();
            if(term.StartsWith(SYMBOL_IMMEDIATE_MODE))
            {
                term = term.Substring(SYMBOL_IMMEDIATE_MODE.Length);
                return true;
            }
            else
            {
                return false;
            }
        }

    }


    //-----------------------------------------------------------------------------------------------------------------

    public static class LookEventUtil
    {

        static HashSet<LookEventType> __ALL_TYPES;
        static HashSet<LookEventType> __SAVED_TYPES;

        static LookEventUtil()
        {
            __ALL_TYPES = new HashSet<LookEventType>();
            __SAVED_TYPES = new HashSet<LookEventType>();
            foreach(var val in System.Enum.GetValues(typeof(LookEventType)))
            {
                var t = (LookEventType)val;
                if(t != LookEventType.None)
                {
                    if(!t.isManeuver())
                    {
                        __ALL_TYPES.Add((LookEventType)val);
                    }
                    if(__isSaved(t))
                    {
                        __SAVED_TYPES.Add(t);
                    }
                }
            }
        }
        static bool __isSaved(LookEventType type)
        {
            switch(type) {
                case LookEventType.None:
        //        case LookEventType.Observe_Direction:
                case LookEventType.SideMirror_Any:
                case LookEventType.Wheel:
                case LookEventType.Console:             return false;
        //         case LookEventType.Follow_Marker:       return false;
                default:                                return true;
            }
        }


        public static IEnumerable<LookEventType> GetAllTypes()
        {
            return __ALL_TYPES;
        }
        public static IEnumerable<LookEventType> GetSerializableTypes()
        {
            return __SAVED_TYPES;
        }

        public static bool isSerializable(this LookEventType type)
        {
            return __SAVED_TYPES.Contains(type);
        }


        ///     TMP WORKAROUND - display observe & front somehow?

        public static bool isDisplayedInStats(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:
                case LookEventType.Check_Direction_L:
                case LookEventType.Check_Direction_R:
                case LookEventType.ShoulderLook_L:
                case LookEventType.ShoulderLook_R:
                case LookEventType.SideMirror_L:
                case LookEventType.SideMirror_R:
                case LookEventType.RearWindow:      return true;
                default:                            return false;
            }
        }
        
    
        public static LookEventMode GetDefaultMode(this LookEventType type)
        {
            if(type.isManeuver()) 
            {
                return LookEventMode.None;
            }
            switch(type)
            {
                case LookEventType.FrontWindow:
                case LookEventType.RearWindow:
                case LookEventType.Observe_Direction:
                case LookEventType.Observe_Direction_L:
                case LookEventType.Observe_Direction_R:
                case LookEventType.Observe_Situation:
                case LookEventType.Follow_Marker:       return LookEventMode.LookAway;
                default:                                return LookEventMode.MinGaze;
            }
        }

        public static float GetDefaultGazeTime(this LookEventType type, LookEventMode mode)
        {
            if(mode == LookEventMode.MinGaze)
            {
                if(isCarMirror(type))
                {
                    return 0.2f;
                }
                else if(isShoulder(type))
                {
                    return 0.1f;
                }
                else
                {
                    return 0.5f;
                }
            }
            else
            {
                return 2f;
            }
        }


        public static bool isCarMirror(this LookEventType type)
        {
            switch(type)    
            {
                case LookEventType.RearViewMirror:
                case LookEventType.SideMirror_L:
                case LookEventType.SideMirror_R:    
                case LookEventType.SideMirror_Any:      return true;
                default:                                return false;
            }
        }

        public static bool isCarWindow(this LookEventType type)
        {
            return type == LookEventType.FrontWindow || type == LookEventType.RearWindow;
        }

        public static bool isShoulder(this LookEventType type)
        {
            return type == LookEventType.ShoulderLook_L || type == LookEventType.ShoulderLook_R;
        }

        public static bool isPartOfCar(this LookEventType type)
        {
            if(isCarMirror(type)) { return true; }
            else
            {
                switch(type)
                {
                    case LookEventType.FrontWindow:
                    case LookEventType.RearWindow:
                    case LookEventType.ShoulderLook_L:
                    case LookEventType.ShoulderLook_R:  return true;
                    default:                            return false;
                }
            }
        }

        public static bool isManeuver(this LookEventType type)
        {   
            return type.ToManeuver() != ManeuverType.Undefined;
        }

        /// @brief
        /// check if two events are part of same group of events
        ///
        public static bool SharesGroupWith(this LookEventType type, LookEventType other)
        {
            if(type == other)
            {
                return true;
            }
            else
            {
                return type.GetGroupCode() == other.GetGroupCode();
            }
        }

        /// @brief
        /// Group code can be used to compare event types
        ///
        public static int GetGroupCode(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:      return 1;

                case LookEventType.SideMirror_Any:
                case LookEventType.SideMirror_L:
                case LookEventType.SideMirror_R:        return 2;   

                case LookEventType.ShoulderLook_L:
                case LookEventType.ShoulderLook_R:      return 3;
                
                case LookEventType.FrontWindow:         return 4;
                case LookEventType.RearWindow:          return 5;
                
                case LookEventType.Check_Direction:
                case LookEventType.Check_Direction_L:
                case LookEventType.Check_Direction_R:   return 6;

                case LookEventType.Observe_Direction:
                case LookEventType.Observe_Direction_L:
                case LookEventType.Observe_Direction_R: return 7;

                case LookEventType.Follow_Marker:       return 8;
                case LookEventType.Observe_Situation:   return 9;

                case LookEventType.Console:             return 99;
                default:                                return -1;
            }
        }

        public static int GetAlignment(this LookEventType type)
        {
            switch(type)
            {                
                case LookEventType.SideMirror_L:
                case LookEventType.ShoulderLook_L:
                case LookEventType.Check_Direction_L:
                case LookEventType.Observe_Direction_L: return -1;

                case LookEventType.SideMirror_R:
                case LookEventType.ShoulderLook_R:
                case LookEventType.Check_Direction_R:
                case LookEventType.Observe_Direction_R: return 1;
                default:                                return 0;
            }
        }

        public static bool AlwaysShowMarker(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.FrontWindow:
                case LookEventType.RearWindow:

                case LookEventType.Follow_Marker:
                case LookEventType.Observe_Situation:    
          //      case LookEventType.Check_Direction:  
          //      case LookEventType.Check_Direction_L:
          //      case LookEventType.Check_Direction_R:  
                case LookEventType.Observe_Direction:   
                case LookEventType.Observe_Direction_L:
                case LookEventType.Observe_Direction_R: return true;
                default:                                return false;
            }
        }

        public static bool AlwaysShowMarkerText(this LookEventType type)
        {
            return !isCarMirror(type);
        }

        public static string Readable(this LookEventType type, bool showAlignment=true)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:      return "Innenspiegel";
                case LookEventType.SideMirror_Any:      return "Außenspiegel";

                case LookEventType.SideMirror_L:        return showAlignment ? "Spiegel links" : "Spiegel";
                case LookEventType.SideMirror_R:        return showAlignment ? "Spiegel rechts" : "Spiegel";   

                case LookEventType.ShoulderLook_L:
                case LookEventType.ShoulderLook_R:      return "Schulter";
                
                case LookEventType.FrontWindow:         return "Vorne";
                case LookEventType.RearWindow:          return "Hinten";
                case LookEventType.Console:             return "Konsole";
                
                case LookEventType.Check_Direction:     return "Einsehen";
                case LookEventType.Check_Direction_L:   return showAlignment ? "Einsehen links" : "Einsehen";
                case LookEventType.Check_Direction_R:   return showAlignment ? "Einsehen rechts" : "Einsehen";
                case LookEventType.Observe_Direction:   return "Einsehen";
                case LookEventType.Observe_Direction_L: return showAlignment ? "Einsehen links" : "Einsehen";
                case LookEventType.Observe_Direction_R: return showAlignment ? "Einsehen rechts" : "Einsehen";
                case LookEventType.Follow_Marker:       return "Folgen";
                case LookEventType.Observe_Situation:   return "Beobachten";
                default:                                return "";
            }
        }

        public static void Readable2(this LookEventType type, out string descriptor, out string direction)
        {
            descriptor = type.Readable().Split(' ').First();
            direction = "";
            switch(type)
            {
                case LookEventType.SideMirror_L:        direction = "links"; break;
                case LookEventType.SideMirror_R:        direction = "rechts"; break;
                case LookEventType.ShoulderLook_L:      direction = "links"; break;
                case LookEventType.ShoulderLook_R:      direction = "rechts"; break;
                case LookEventType.Check_Direction_L:   direction = "links"; break;
                case LookEventType.Check_Direction_R:   direction = "rechts"; break;
                case LookEventType.Observe_Direction_L: direction = "links"; break;
                case LookEventType.Observe_Direction_R: direction = "rechts"; break;
            }
        }

        public static string ReadableShort(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:      return "Innenspiegel";
                case LookEventType.SideMirror_Any:      return "AußenSpiegel";

                case LookEventType.SideMirror_L:        return "Spiegel L";
                case LookEventType.SideMirror_R:        return "Spiegel R";   

                case LookEventType.ShoulderLook_L:      return "Schulter L";
                case LookEventType.ShoulderLook_R:      return "Schulter R";
                
                case LookEventType.FrontWindow:         return "Vorne";
                case LookEventType.RearWindow:          return "Hinten";
                case LookEventType.Console:             return "Konsole";
                
                case LookEventType.Check_Direction:     return "Einsehen";
                case LookEventType.Check_Direction_L:   return "Einsehen L";
                case LookEventType.Check_Direction_R:   return "Einsehen R";
                case LookEventType.Observe_Direction:   return "Einsehen";
                case LookEventType.Observe_Direction_L: return "Einsehen L";
                case LookEventType.Observe_Direction_R: return "Einsehen R";
                case LookEventType.Follow_Marker:       return "Folgen";
                case LookEventType.Observe_Situation:   return "Beobachten";
                default:                                return "";
            }
        }

        public static string ToString(this LookEventType type)
        {
            return ReadableCode(type);
        }

        public static string ReadableCode(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:      return "IS";
                case LookEventType.SideMirror_Any:      return "A";

                case LookEventType.SideMirror_L:        return "AL";
                case LookEventType.SideMirror_R:        return "AR";   

                case LookEventType.ShoulderLook_L:      return "SL";
                case LookEventType.ShoulderLook_R:      return "SR";
                
                case LookEventType.FrontWindow:         return "V";
                case LookEventType.RearWindow:          return "H";
                case LookEventType.Console:             return "K";
                
                case LookEventType.Check_Direction:     return "E";
                case LookEventType.Check_Direction_L:   return "EL";
                case LookEventType.Check_Direction_R:   return "ER";
                case LookEventType.Observe_Direction:   return "E";
                case LookEventType.Observe_Direction_L: return "EL";
                case LookEventType.Observe_Direction_R: return "ER";
                case LookEventType.Follow_Marker:       return "F";
                case LookEventType.Observe_Situation:   return "B";
                default:                                return "";
            }
        }

        public static int GetAlignment(this LookEventType type, string comment)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:      return 0;
                case LookEventType.SideMirror_L:        return -1;
                case LookEventType.SideMirror_R:        return 1;   

                case LookEventType.ShoulderLook_L:      return -1;
                case LookEventType.ShoulderLook_R:      return 1;
                
                case LookEventType.FrontWindow:         return 0;
                case LookEventType.RearWindow:          return 0;
                case LookEventType.Console:             return 0;

                case LookEventType.Check_Direction_L:   return -1;
                case LookEventType.Check_Direction_R:   return 1;

                case LookEventType.Observe_Direction_L: return -1;
                case LookEventType.Observe_Direction_R: return 1;
                
                default:
                    if(!string.IsNullOrEmpty(comment)) {
                        comment = comment.ToLower();
                        if(comment.Contains("links")) return -1;
                        else if(comment.Contains("rechts")) return 1;
                    }
                    return 0;
            }
        }

        public static LookEventType GetAlignedType(this LookEventType type, int alignment)
        {
            switch(type)
            {
                case LookEventType.SideMirror_Any:      return __resolveAlignment(alignment, type, LookEventType.SideMirror_L, LookEventType.SideMirror_R);
                case LookEventType.Check_Direction:     return __resolveAlignment(alignment, type, LookEventType.Check_Direction_L, LookEventType.Check_Direction_R);
                case LookEventType.Observe_Direction:   return __resolveAlignment(alignment, type, LookEventType.Observe_Direction_L, LookEventType.Observe_Direction_R);
                default:                                return type;
            }
        }
        static LookEventType __resolveAlignment(int alignment, LookEventType center, LookEventType left, LookEventType right)
        {
            if(alignment < 0) return left;
            else if(alignment > 0) return right;
            else return center;
        }


        public static bool TryParse(string s, out LookEventType[] t, bool logErrors=false)
        {
            t = new LookEventType[1];
            t[0] = LookEventType.None;
            if(string.IsNullOrEmpty(s))
            {
                return false;
            }
            else
            {
                string typeTerm = GetTypeTerm(s);;
                
                //Debug.Log("try parse:: [" + typeTerm + "] original=[" + s + "]");
                if(s.Length >= 1)
                {
                    switch(typeTerm)
                    {
                        //  frontscheibe
                        case "v":   t[0] = LookEventType.FrontWindow; break;

                        //  heckscheibe
                        case "h":   t[0] = LookEventType.RearWindow; break;

                        //  innenspiegel
                        case "is":  t[0] = LookEventType.RearViewMirror; break;
                        
                        //  seitenspiegel
                        case "a":   t[0] = LookEventType.SideMirror_Any; break;
                        case "al":  t[0] = LookEventType.SideMirror_L; break;
                        case "ar":  t[0] = LookEventType.SideMirror_R; break;
                        case "arl": 
                            t = new LookEventType[2];
                            t[0] = LookEventType.SideMirror_R; 
                            t[1] = LookEventType.SideMirror_L;
                            break;
                        case "alr": 
                            t = new LookEventType[2];
                            t[0] = LookEventType.SideMirror_L;
                            t[1] = LookEventType.SideMirror_R; 
                            break;

                        //  schultern
                        case "sl":  t[0] = LookEventType.ShoulderLook_L; break;
                        case "sr":  t[0] = LookEventType.ShoulderLook_R; break;

                        //  beobachten
                        case "b":   t[0] = LookEventType.Observe_Situation; break;

                        //  folgen
                        case "f":   t[0] = LookEventType.Follow_Marker; break;

                        //  einsehen
                        case "e":   t[0] = LookEventType.Observe_Direction; break;

                        //  richtung checken
                        case "r":   t[0] = LookEventType.Check_Direction; break;
                        case "rl":  t[0] = LookEventType.Check_Direction_L; break;
                        case "rr":  t[0] = LookEventType.Check_Direction_R; break;
                    }
                }
                if(t.Length > 0)
                {
                    if(t[0] != LookEventType.None)
                    {
                        return true;
                    }
                    else if(logErrors)
                    {
                        Debug.LogWarning("Term \"" + s + "\" cannot be parsed!");
                    }
                }
                return false;
            }
        }

        public static bool TryParseFromString(string raw, out LookEventType t)
        {
            t = LookEventType.None;
            if(string.IsNullOrEmpty(raw))
            {
                return false;
            }
            else
            {                
                switch(raw.ToLower())
                {
                    //  frontscheibe
                    case "v":   t = LookEventType.FrontWindow; break;

                    //  heckscheibe
                    case "h":   t = LookEventType.RearWindow; break;

                    //  innenspiegel
                    case "is":  t = LookEventType.RearViewMirror; break;
                    
                    //  seitenspiegel
                    case "a":   t = LookEventType.SideMirror_Any; break;
                    case "al":  t = LookEventType.SideMirror_L; break;
                    case "ar":  t = LookEventType.SideMirror_R; break;
                    case "arl": t = LookEventType.SideMirror_Any; break;

                    //  schultern
                    case "sl":  t = LookEventType.ShoulderLook_L; break;
                    case "sr":  t = LookEventType.ShoulderLook_R; break;

                    //  beobachten
                    case "b":   t = LookEventType.Observe_Situation; break;

                    //  folgen
                    case "f":   t = LookEventType.Follow_Marker; break;

                    //  einsehen
                    case "e":   t = LookEventType.Observe_Direction; break;

                    //  richtung checken
                    case "r":   t = LookEventType.Check_Direction; break;
                    case "rl":  t = LookEventType.Check_Direction_L; break;
                    case "rr":  t = LookEventType.Check_Direction_R; break;
                }
                return t != LookEventType.None;
            }
        }


        public static string GetTypeTerm(string s)
        {
            //  only parse type term until a non-letter is found
            s = s.ToLower().Trim();
            for(int i = 0; i < s.Length; i++)
            {
                if(!System.Char.IsLetter(s[i]))
                {
                    return s.Substring(0, i);
                }
            }
            return s;
        }



        //  RECORD KEYS

        public const string KEY_MIRROR_REAR = "is";
        public const string KEY_MIRROR_SIDES = "a";
        public const string KEY_MIRROR_LEFT = "ar";
        public const string KEY_MIRROR_RIGHT = "al";
        public const string KEY_SHOULDERS = "s";
        public const string KEY_SHOULDER_LEFT = "ls";
        public const string KEY_SHOULDER_RIGHT = "rs";
        public const string KEY_BACK = "b";
        public const string KEY_CROSSING = "c";
        public const string KEY_CROSSING_LEFT = "cl";
        public const string KEY_CROSSING_RIGHT = "cr";

        public static IEnumerable<string> GetAllRecordKeys()
        {
            yield return KEY_MIRROR_REAR;
            yield return KEY_MIRROR_SIDES;
            yield return KEY_MIRROR_LEFT;
            yield return KEY_MIRROR_RIGHT;
            yield return KEY_SHOULDERS;
            yield return KEY_SHOULDER_LEFT;
            yield return KEY_SHOULDER_RIGHT;
            yield return KEY_BACK;
            yield return KEY_CROSSING;
            yield return KEY_CROSSING_LEFT;
            yield return KEY_CROSSING_RIGHT;
        }

        public static string ReadableRecordKey(string key)
        {
            switch(key)
            {   
                case KEY_MIRROR_REAR:       return Readable(LookEventType.RearViewMirror);
                case KEY_MIRROR_SIDES:      return Readable(LookEventType.SideMirror_Any);
                case KEY_MIRROR_LEFT:       return Readable(LookEventType.SideMirror_L);
                case KEY_MIRROR_RIGHT:      return Readable(LookEventType.SideMirror_R);
                case KEY_SHOULDERS:         return "Schultern";
                case KEY_SHOULDER_LEFT:     return Readable(LookEventType.ShoulderLook_L);
                case KEY_SHOULDER_RIGHT:    return Readable(LookEventType.ShoulderLook_R);
                case KEY_BACK:              return Readable(LookEventType.RearWindow);
                case KEY_CROSSING:          return "Vorfahrt";
                case KEY_CROSSING_LEFT:     return "Links einsehen";
                case KEY_CROSSING_RIGHT:    return "Rechts einsehen";
                default:                    return "";
            }
        }

        public static IEnumerable<string> GetRecordKeys(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.RearViewMirror:      yield return KEY_MIRROR_REAR; break;
                case LookEventType.SideMirror_Any:      yield return KEY_MIRROR_SIDES; break;
                case LookEventType.SideMirror_L:        yield return KEY_MIRROR_SIDES; yield return KEY_MIRROR_LEFT; break;
                case LookEventType.SideMirror_R:        yield return KEY_MIRROR_SIDES; yield return KEY_MIRROR_RIGHT; break;   

                case LookEventType.ShoulderLook_L:      yield return KEY_SHOULDERS; yield return KEY_SHOULDER_LEFT; break;
                case LookEventType.ShoulderLook_R:      yield return KEY_SHOULDERS; yield return KEY_SHOULDER_RIGHT; break;
                
                case LookEventType.RearWindow:          yield return KEY_BACK; break;
                
                case LookEventType.Check_Direction_L:   yield return KEY_CROSSING_LEFT; break;
                case LookEventType.Check_Direction_R:   yield return KEY_CROSSING_RIGHT; break;
                case LookEventType.Check_Direction:     yield return KEY_CROSSING; break;

                case LookEventType.Observe_Direction_L: yield return KEY_CROSSING_LEFT; break;
                case LookEventType.Observe_Direction_R: yield return KEY_CROSSING_RIGHT; break;
                case LookEventType.Observe_Direction:   yield return KEY_CROSSING; break;


                case LookEventType.FrontWindow:         
                case LookEventType.Console:
                case LookEventType.Observe_Situation:   break;
            }
        }
    }


}


