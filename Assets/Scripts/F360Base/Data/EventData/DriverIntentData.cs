
#define SAVE_RAW_TERMS


using System.Collections.Generic;
using UnityEngine;


namespace F360.Data
{

    public enum DriverIntentType
    {
        None=0,
        Forward,
        Left,
        Right,
        Back
    }

    public class DriverIntentData
    {
        

        public const int INTENT_DISPLAY_TIME_MS = 4000;
        public const int MIN_START_TIME = 500;


        const char SYMBOL_MARKER_TIMING_START = '[';
        const char SYMBOL_MARKER_TIMING_END = ']';

        public static IEnumerable<string> GetDescriptors()
        {
            yield return "L";
            yield return "R";
        }


        static bool debug = false;


        public struct DriverIntent
        {
            public DriverIntentType type;
            public RangeInt timing;                     ///< intent time range

            public override string ToString()
            {
                var b = new System.Text.StringBuilder("[DriverIntent] ");
                b.Append(type.ToString());
                b.Append(" <" + timing.start.ToString());
                b.Append("-" + timing.end.ToString());
                b.Append(">");
                return b.ToString();
            }
            public bool isValid() { return type != DriverIntentType.None && timing.length > 0; }

        }


        List<DriverIntent> intents;

        #if SAVE_RAW_TERMS
        List<string> rawTerms = new List<string>();
        #endif

        public IEnumerable<string> GetRawTerms()
        {
            #if SAVE_RAW_TERMS
            return rawTerms;
            #else
            Debug.LogWarning("IntentData:: RAW terms are not saved!");
            return new string[0];
            #endif
        }

        public int Count { get { return intents != null ? intents.Count : 0; } }

        public DriverIntent Get(int index)
        {
            if(index >= 0 && index < Count)
            {
                return intents[index];
            }
            return new DriverIntent();
        }


        public override string ToString()
        {
            if(intents == null)
            {
                return "[IntentData] NULLData";
            }
            var b = new System.Text.StringBuilder("[IntentData] " + intents.Count.ToString());
        //    b.Append("\nsource={" + sourceTerm + "}");
            for(int i = 0; i < intents.Count; i++)
            {
                b.Append("\n\t" + intents[i].ToString());
            }
            return b.ToString();
        }



        //-----------------------------------------------------------------------------------------------------------------

        //  TSV PARSE

        public static bool TryParse(out DriverIntentData result, string[] terms)
        {
   //         Debug.Log("-----------------------------------------------\n\n");

            var valid = false;
            result = new DriverIntentData();
            for(int i = 0; i < terms.Length; i++)
            {
            //    Debug.Log("driverIntent term[" + i + "/" + terms.Length + "] : <" + terms[i] + ">");
                DriverIntent dI;
                int index = result.intents != null ? result.intents.Count : 0;
                if(tryParseTerm(index, terms[i], out dI))
                {
                    if(result.intents == null) result.intents = new List<DriverIntent>();
                    result.intents.Add(dI);
                    if(debug)
                    {
                        Debug.Log("added intent: " + dI.ToString());
                    }
                    valid = true;

                    #if SAVE_RAW_TERMS
                    result.rawTerms.Add(terms[i]);
                    #endif
                }
            }
            return valid;
        }

        public static bool tryParseTerm(int index, string term, out DriverIntent result, bool preventMinStartClamp=false)
        {
            term = term.Trim();
            if(!string.IsNullOrEmpty(term))
            {
                if(debug)
                {
                    Debug.Log("\tterm=[ " + term + " ]\n\n");
                }
                DriverIntentType type;
                if(tryParseIntent(ref term, out type))
                {   
//                    Debug.Log("\tintent=[" + type + "] remainder=[" + term + "]");

                    int duration = INTENT_DISPLAY_TIME_MS;
                    tryParseDurationOverride(ref term, out duration);               
                    
                    int intentTime;
                    if(F360FileSystem.ParseTimeFromSecondsDataset(term, out intentTime))
                    {
                        result = new DriverIntent();
                        result.type = type;
                        result.timing = new RangeInt(
                            preventMinStartClamp 
                            ? Mathf.Max(0, intentTime-duration) 
                            : Mathf.Max(MIN_START_TIME, intentTime - duration),
                            duration
                        );
                        return true;
                    }
                    else Debug.LogWarning("failed to parsed timing! [" + term + "]");
                }
            }
            result = new DriverIntent();
            return false;
        }

        static bool tryParseIntent(ref string term, out DriverIntentType type)
        {
            term = term.ToLower();
            switch(term[0])
            {
                case DriverIntentUtil.CHAR_LEFT:
                    type = DriverIntentType.Left;
                    term = term.Substring(1);
                    return true;

                case DriverIntentUtil.CHAR_RIGHT:
                    type = DriverIntentType.Right;
                    term = term.Substring(1);
                    return true;

                default:
                    type = DriverIntentType.None;
                    return false;
            }
        }

        static bool tryParseDurationOverride(ref string term, out int duration)
        {
            int left = term.IndexOf(SYMBOL_MARKER_TIMING_START);
            int right = term.IndexOf(SYMBOL_MARKER_TIMING_END);
            if(left != -1 && right != -1 && left < right)
            {
                int millis;
                string s = term.Substring(left+1, right-left-1);
                if(F360FileSystem.ParseTimeFromSecondsDataset(s, out millis))
                {
                    duration = millis;                    
                    string ls = term.Substring(0, left);
                    string rs = term.Substring(right+1, term.Length-right-1);
                    term = ls + rs;
                    return true;
                }
            }
            duration = INTENT_DISPLAY_TIME_MS;
            return false;
        }

    }


    

    public static class DriverIntentUtil
    {
        public const char CHAR_LEFT = 'l';
        public const char CHAR_RIGHT = 'r';
        public const char CHAR_FWD = 'f';
        public const char CHAR_BACK = 'b';

        public const string CODE_LEFT = "L";
        public const string CODE_RIGHT = "R";
        public const string CODE_FWD = "F";
        public const string CODE_BACK = "B"; 

        public static string ReadableCode(this DriverIntentType type)
        {
            switch(type)
            {
                case DriverIntentType.Left:     return CODE_LEFT;
                case DriverIntentType.Right:    return CODE_RIGHT;
                case DriverIntentType.Forward:  return CODE_FWD;
                case DriverIntentType.Back:     return CODE_BACK;
                default:                        return "";
            }
        }   
    }
}