


using System.Collections.Generic;
using UnityEngine;


namespace F360.Data.Runtime
{


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  EVENT
    //
    //-----------------------------------------------------------------------------------------------------------------


    public struct Event
    {

        public int index;                           ///< event sequence id
        public int sharedIndex;                     ///< shared sequence id - completing one will automatically complete all others
        public RangeInt timing;                     ///< event time range
        public int[] tresholdOverride;              ///< perception tresholds used to rate user input - by default, these are at thirds of the time range. 
        public bool isQuickEvent;                   ///< no thresholds
        public int fixedRating;                     ///< rating user receives for completing this event (only for QuickEvents)

        public string descriptor;                   ///< readable name/type of hazard


        public static Event Create(int index)
        {   
            var e = new Event();
            e.index = index;
            return e;
        }
        public static Event Empty()
        {
            var e = new Event();
            e.index = -1;
            return e;
        }

        public bool isValid(int duration)
        {
            return timing.start >= 0 && timing.end <= duration;
        }

        public bool CheckTimeRange(int timeMs)
        {
            return timeMs >= timing.start && timeMs <= timing.end;
        }
        public bool CheckTimeRange(RangeInt range)
        {
            return CheckTimeRange(range.start) || CheckTimeRange(range.end);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(obj, null))
            {
                return false;
            }
            else if(obj is Event)
            {
                return Equals((Event)obj);
            }
            return false;
        }
        public bool Equals(Event other)
        {
            return other.timing.Equals(timing);
        }
        public override int GetHashCode()
        {
            return timing.GetHashCode();
        }

        public static bool operator==(Event a, Event b)
        {
            return a.Equals(b);
        }
        public static bool operator!=(Event a, Event b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            var b = new System.Text.StringBuilder();
            if(!string.IsNullOrEmpty(descriptor)) b.Append(descriptor + ": ");
            b.Append(isQuickEvent ? "QHazard[" : "Hazard[");
            b.Append("\n\t" + timing.start.ToString() + "-" + timing.end.ToString());
            if(sharedIndex != -1) b.Append("\n\tsharedID: " + sharedIndex.ToString());
            if(fixedRating != -1) b.Append("\n\tfixedRating: " + fixedRating.ToString());
            b.Append("\n]");
            return b.ToString();
        }

        public string ReadableList()
        {
            var b = new System.Text.StringBuilder();
            if(!string.IsNullOrEmpty(descriptor)) b.Append(descriptor + ":\t");
            b.Append(isQuickEvent ? "QHazard[" : "Hazard[");
            b.Append(timing.start.ToString() + "-" + timing.end.ToString());
            b.Append("]");
            return b.ToString();
        }
    }


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  DATA
    //
    //-----------------------------------------------------------------------------------------------------------------


    public class HazardPerceptionData
    {

        public const int NUM_SCORE_TRESHOLDS = 2;

        const char SYMBOL_TRESHOLD_START = '[';
        const char SYMBOL_TRESHOLD_END = ']';

        const char SYMBOL_TRESHOLD_SEQENCE_SEPARATOR = '+';

        const char SYMBOL_COMMENT_START = '(';
        const char SYMBOL_COMMENT_END = ')';

        const char SYMBOL_SHARED_ID_START = '{';
        const char SYMBOL_SHARED_ID_END = '}';

        static IEnumerable<char> AllSymbols() 
        { 
            yield return SYMBOL_TRESHOLD_START;
            yield return SYMBOL_TRESHOLD_END;
            yield return SYMBOL_TRESHOLD_SEQENCE_SEPARATOR;
            yield return SYMBOL_COMMENT_START;
            yield return SYMBOL_COMMENT_END;
            yield return SYMBOL_SHARED_ID_START;
            yield return SYMBOL_SHARED_ID_END;
        }


        static bool debug = false;


        //-----------------------------------------------------------------------------------------------------------------

        List<Event> entries;


        public int EventCount { get { return entries.Count; } }


        public Event GetEventAt(int index)
        {
            if(index >= 0 && index < entries.Count) return entries[index];
            else return Event.Empty();
        }
        public IEnumerable<int> GetEventIndicesAtTime(int timeMs)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(timeMs >= entries[i].timing.start && timeMs <= entries[i].timing.end)
                {
                    yield return i;
                }
            }
        }
        public IEnumerable<int> GetEventIndicesAfterTime(int timeMs)
        {
            for(int i = 0; i < entries.Count; i++)
            {
                if(timeMs < entries[i].timing.end)
                {
                    yield return i;
                }
            }
        }

        public RangeInt GetTiming(int index)
        {
            if(index >= 0 && index < EventCount)
            {
                return entries[index].timing;
            }
            return new RangeInt();
        }


       
        //-----------------------------------------------------------------------------------------------------------------

        public string Print()
        {
            var b = new System.Text.StringBuilder("[HazardPerceptionData]");
            if(entries != null)
            {
                var i = 1;
                foreach(var e in entries)
                {
                    b.Append("\n\t" + i.ToString() + ": " + e.ToString());
                    i++;
                }
                return b.ToString();
            }
            else
            {
                b.Append(" no data!");
                return b.ToString();
            }
        }



        //-----------------------------------------------------------------------------------------------------------------

        //  TSV PARSE

        /// @brief
        /// parse HZ dataset from csv. Each term is interpreted as an event.
        /// descriptors term can be optionally set. Each descriptor in the term is separated by a comma.
        /// number of descriptors must match number of HZ terms
        ///
        public static bool TryParseWithDescriptors(out HazardPerceptionData result, string descriptors, params string[] terms)
        {
            var descr = descriptors.Split(',');
            var descrSeq = 0;
            var valid = false;
            result = new HazardPerceptionData();
            for(int i = 0; i < terms.Length; i++)
            {
                Event e;
                int index = result.entries != null ? result.entries.Count : 0;
                if(tryParseTerm(index, terms[i], out e))
                {
                    valid = true;
                    if(descr.Length > 0 && descrSeq < descr.Length)
                    {
                        if(string.IsNullOrEmpty(e.descriptor))
                        {
                            e.descriptor = descr[descrSeq];
                        } 
                        descrSeq++;
                    }
                    if(result.entries == null) result.entries = new List<Event>();
                    result.entries.Add(e);
                }
            }
            return valid;
        }   

        /// @brief
        /// parse HZ dataset from csv. Each term is interpreted as an event.
        ///
        public static bool TryParse(out HazardPerceptionData result, params string[] terms)
        {
            var valid = false;
            result = new HazardPerceptionData();
            for(int i = 0; i < terms.Length; i++)
            {
                Event e;
                int index = result.entries != null ? result.entries.Count : 0;
                if(tryParseTerm(index, terms[i], out e))
                {
                    if(result.entries == null) result.entries = new List<Event>();
                    result.entries.Add(e);
                    valid = true;
                }
            }
            return valid;
        }

        public static bool TryParseDescriptor(ref string term, out string descriptor)
        {
            descriptor = "";
            int id = term.IndexOf(':');
            if(id != -1)
            {
                for(int i = 0; i < id; i++)
                {
                    if(System.Char.IsDigit(term[i]))
                    {
                        return false;
                    }
                    else if(i == id-1 && term[i] == ' ')
                    {
                        return false;
                    }
                    else
                    {
                        foreach(var s in AllSymbols()) 
                        {
                            if(s == term[i])
                            {
                                return false;
                            }
                        }
                    }
                }
                descriptor = term.Substring(0, id).Trim();
                term = term.Substring(id+1, term.Length-id-1).Trim();
                return true;
            }
            return false;
        }

        static bool tryParseTerm(int index, string term, out Event result)
        {
            if(!string.IsNullOrEmpty(term))
            {
                if(debug)
                {
                    Debug.Log("\t[" + index + "]term=[ " + term + " ]\n\n");
                }
                removeCommentary(ref term);
                int[] tresholds;
                int fixedRating;
                int sharedID;
                string descriptor;
                TryParseDescriptor(ref term, out descriptor);
                bool parsedTresholds = tryParseTresholdOverrides(ref term, out tresholds, out fixedRating);
                bool isQuickEvent = parsedTresholds && tresholds.Length == 0;
                tryParseSharedIndex(ref term, out sharedID);

                RangeInt timing;
                if(F360FileSystem.ParseTimeRangeFromSecondsDataset(term, out timing))
                {
//                    Debug.Log("TIminig:: " + timing.start + " - " + timing.end);
                    result = Event.Create(index);
                    result.timing = timing;
                    result.sharedIndex = sharedID;
                    result.fixedRating = fixedRating;
                    result.isQuickEvent = isQuickEvent;
                    result.tresholdOverride = tresholds;
                    result.descriptor = descriptor;
                    return true;
                }
                else if(debug)
                {
                    Debug.LogWarning("HazardPerceptionData:: could not parse term<" + term + ">");
                }
            }
            result = Event.Empty();
            return false;
        }

        

        static bool tryParseTresholdOverrides(ref string term, out int[] tresholds, out int fixedRating)
        {
            tresholds = null;
            fixedRating = -1;
            if(term.Length > 0)
            {
                int left=0, right=0;
                bool parsed = false;
                do
                {
                    left = term.IndexOf(SYMBOL_TRESHOLD_START);
                    right = term.IndexOf(SYMBOL_TRESHOLD_END);

                    if(left != -1 && right != -1 && left < right)
                    {
                        string s = term.Substring(left+1, right-left-1);

                        //  check quick event term [Q], [Q1,2,3]
                        if(s.ToLower().StartsWith("q"))
                        {
                            tresholds = new int[0];
                            if(s.Length > 1)
                            {
                                F360FileSystem.ParseIntegerFromDataset(s.Substring(1), out fixedRating);
                            }
                            string _ls = term.Substring(0, left);
                            string _rs = term.Substring(right+1, term.Length-right-1);
                            term = _ls + _rs;
                            return true;
                        }
                        else
                        {
                            if(tresholds == null)
                            {
                                tresholds = new int[NUM_SCORE_TRESHOLDS];
                                for(int i = 0; i < NUM_SCORE_TRESHOLDS; i++)
                                {
                                    tresholds[i] = -1;
                                }
                            }
                            
                            //  check individual override
                            string prefix = "";
                            if(parseTermPrefix(ref s, out prefix))
                            {
                                int millis;
                                if(F360FileSystem.ParseTimeFromSecondsDataset(s, out millis))
                                {
                                    switch(prefix.ToLower()[0])
                                    {
                                        case 'a':  tresholds[0] = millis; parsed = true; break;
                                        case 'b':  tresholds[1] = millis; parsed = true; break;
                                        default:    
                                            Debug.LogWarning("HazardPerceptData has erroron parsing treshold override term=[\"" + term + "\"]"); 
                                            break;
                                    }
                                }
                            }
                            //  parse in sequence
                            else
                            {
                                string[] terms = s.Split(SYMBOL_TRESHOLD_SEQENCE_SEPARATOR);
                                int index = 0;
                                for(int i = 0; i < terms.Length; i++)
                                {
                                    int millis;
                                    if(F360FileSystem.ParseTimeFromSecondsDataset(terms[i], out millis))
                                    {
                                        tresholds[index]++;
                                        index++;
                                        parsed = true;
                                        if(index >= NUM_SCORE_TRESHOLDS) break;
                                    }
                                }
                            }
                        }
                        
                        //  remove treshold override from base term
                        string ls = term.Substring(0, left);
                        string rs = term.Substring(right+1, term.Length-right-1);
                        term = ls + rs;
                    }
                    else
                    {
                        break;
                    }
                }
                while(left != -1 && right != -1);
                return parsed;
            }
            return false;
        }

        static bool parseTermPrefix(ref string term, out string prefix)
        {
            prefix = "";
            for(int i = 0; i < term.Length; i++)
            {
                if(System.Char.IsDigit(term[i]))
                {
                    prefix = term.Substring(0, i);
                    term = term.Substring(i);
                    return i > 0;
                }
            }
            return false;
        }

        static bool tryParseSharedIndex(ref string term, out int sharedID)
        {
            sharedID = -1;
            int left = term.IndexOf(SYMBOL_SHARED_ID_START);
            int right = term.IndexOf(SYMBOL_SHARED_ID_END);
            if(left != -1 && right != -1 && left < right)
            {
                string s = term.Substring(left+1, right-left-1);
                string ls = term.Substring(0, left);
                string rs = term.Substring(right+1, term.Length-right-1);
                term = ls + rs;
                if(F360FileSystem.ParseIntegerFromDataset(s, out sharedID))
                {
                    return true;
                }
            }
            return false;
        }

        static void removeCommentary(ref string term)
        {
            int left = term.IndexOf(SYMBOL_COMMENT_START);
            int right = term.IndexOf(SYMBOL_COMMENT_END);
            if(left != -1 && right != -1 && left < right)
            {
                string ls = term.Substring(0, left);
                string rs = term.Substring(right+1, term.Length-right-1);
                term = ls + rs;
            }
        }

        /*static bool tryParseTimingOverride(ref string term, out int looktime)
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
        }*/

        //-----------------------------------------------------------------------------------------------------------------


    }


}


