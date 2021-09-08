using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using F360.Data.Runtime;
using F360.Data.Beta;
using F360.Users.Stats;

using Utility.Config;

namespace F360.Data.IO
{
    

    //-----------------------------------------------------------------------------------------------------------------
    //
    //  TYPE DEFINES
    //
    //-----------------------------------------------------------------------------------------------------------------



    public enum ValueType
    {
        None=0,
        Context,
        Text,
        TextMultiple,
        Number,
        Bool,
        Time,
        TimeRange,



        Category,
        Location,

        BATerm,
        HZTerm,
        IntentTerm,

        LookType,
        LookTypeCollection,

        SequenceMode,
        SequenceCondition,
    }


    public static class IOHelper
    {

        public static object GetDefaultValue<TData, TContext, TValues>(CustomConfigSetup<TData, TContext, TValues> setup, ValueType type)
                                                where TData : class, IDataTarget<TContext>, new()
                                                where TContext : System.Enum
                                                where TValues : System.Enum
        {
            object result = null;
            switch(type)
            {
                case ValueType.Context:         result = setup.NullContext;         break;     
                case ValueType.TextMultiple:    result = new string[0];             break;
                case ValueType.Number:          result = -1;                        break;
                case ValueType.Bool:            result = false;                     break;
                case ValueType.Time:            result = -1;                        break;
                case ValueType.TimeRange:       result = new RangeInt();            break;
                case ValueType.Category:        result = null;                      break;
                case ValueType.Location:        result = DriveLocation.Undefined;    break;
                case ValueType.Text:            result = "";                        break;
                case ValueType.BATerm:          result = "";                        break;
                case ValueType.HZTerm:          result = "";                        break;
                case ValueType.IntentTerm:      result = "";                        break;
                //case ValueType.SequenceMode:    result = SequenceLoadType.Undefined;break;
                //case ValueType.SequenceCondition: result = SequenceCondition.None;  break;
                case ValueType.LookType:        result = LookEventType.None;        break;
                case ValueType.LookTypeCollection: result = new LookEventType[0];   break;
            }
            return result;
        }

        public static bool ValueValidator<TContext>(ValueType type, object o) where TContext : System.Enum
        {
            switch(type)
            {
                case ValueType.Context:             return o is TContext;  
                case ValueType.TextMultiple:        return o is string[];
                case ValueType.Number:              return o is int;
                case ValueType.Bool:                return o is bool;
                case ValueType.Time:                return o is int;
                case ValueType.TimeRange:           return o is RangeInt;
                case ValueType.Category:            return o is Category;
                case ValueType.Location:            return o is DriveLocation;
                case ValueType.Text:                return o is string;
                case ValueType.BATerm:              return o is string;
                case ValueType.HZTerm:              return o is string;
                case ValueType.IntentTerm:          return o is string;
                //case ValueType.SequenceMode:        return o is SequenceLoadType;
                //case ValueType.SequenceCondition:   return o is SequenceCondition;
                case ValueType.LookType:            return o is LookEventType;  
                case ValueType.LookTypeCollection:  return o is LookEventType[];
                default:                            return false;
            }
        }



        public static bool ValueParser<TData, TContext, TValues>(CustomConfigSetup<TData, TContext, TValues> setup, ValueType type, string raw, out object val) 
                                        where TData : class, IDataTarget<TContext>, new()
                                        where TContext : System.Enum
                                        where TValues : System.Enum
        {
            switch(type)
            {
                case ValueType.Context:

                    TContext ctx;
                    if(setup.CheckNewContext(raw, out ctx))
                   // if(VideoMetaDataUtil.CheckNewContext(raw, out ctx))
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

                case ValueType.Bool:
                    bool b;
                    if(parseBool(raw, out b))
                    {
                        val = b;
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

                /*case ValueType.SequenceMode:
                    SequenceLoadType loadType;
                    if(parseSequenceMode(raw, out loadType))
                    {
                        val = loadType;
                        return true;
                    }
                    break;

                case ValueType.SequenceCondition:
                    SequenceCondition cond;
                    if(parseSequenceCondition(raw, out cond))
                    {
                        val = cond;
                        return true;
                    }
                    break;*/

                case ValueType.LookType:
                    LookEventType look;
                    if(parseLookEvent(raw, out look))
                    {
                        val = look;
                        return true;
                    }
                    break;

                case ValueType.LookTypeCollection:
                    LookEventType[] looks;
                    if(parseLookEventCollection(raw, out looks))
                    {
                        val = looks;
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


        public static string FormatTextArray(string[] txt)
        {
            var b = new System.Text.StringBuilder();
            for(int i = 0; i < txt.Length; i++)
            {
                if(i > 0) b.Append(",");
                b.Append(txt[i]);
            }
            return b.ToString();
        }

        public static string FormatEnumArray<TEnum>(TEnum[] txt) where TEnum : System.Enum
        {
            var b = new System.Text.StringBuilder();
            for(int i = 0; i < txt.Length; i++)
            {
                if(i > 0) b.Append(",");
                b.Append(txt[i].ToString());
            }
            return b.ToString();
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

        static bool parseBool(string raw, out bool b)
        {
            raw = raw.ToLower();
            b = (raw == "true" || raw == "1" || raw == "ja");
            return true;
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


        /*static bool parseSequenceMode(string raw, out SequenceLoadType mode)
        {
            try
            {
                return System.Enum.TryParse<SequenceLoadType>(raw, true, out mode);
            }
            catch
            {
                mode = SequenceLoadType.Undefined;
                return false;
            }
        }


        static bool parseSequenceCondition(string raw, out SequenceCondition cond)
        {
            try
            {
                return System.Enum.TryParse<SequenceCondition>(raw, true, out cond);
            }
            catch
            {
                cond = SequenceCondition.None;
                return false;
            }
        }*/


        static bool parseLookEvent(string raw, out LookEventType type)
        {
            return LookEventUtil.TryParseFromString(raw, out type);
        }

        static bool parseLookEventCollection(string raw, out LookEventType[] looks)
        {   
            var splt = raw.Split(',');
            var bf = new List<LookEventType>();
            foreach(var s in splt)
            {
                LookEventType t;
                if(parseLookEvent(s.Trim(), out t))
                {
                    bf.Add(t);
                }
            }
            looks = bf.ToArray();
            return looks.Length > 0;
        }



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  FORMATTING
        //
        //-----------------------------------------------------------------------------------------------------------------

        public static string ReformatTerm(string term, int tabIndent=2, bool debug=false)
        {
            int id = term.IndexOf(':');
            if(id != -1 && id < term.Length-1)
            {
                term = ReformatTabbing(term, id+1, tabIndent, debug);
            }
            return term;
        }

        public static string ReformatTimeRange(RangeInt timing)
        {
            return F360FileSystem.FormatTimeRange(timing, showMillis: true, showSuffix: false);
        }
        
        public static string ReformatTime(int timeMs)
        {
            return F360FileSystem.FormatTime(timeMs, showMillis: true, showSuffix: false);
        }

        public static string ReformatTabbing(string term, int insertAt=-1, int tabSpacing=CustomConfigIO.TAB_DEFAULT, bool debug=false)
        {
            return CustomConfigIO.ReformatTabbing(term, insertAt, tabSpacing, debug);
        }

        

    }
}