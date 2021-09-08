using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace F360.Data.Runtime
{


    public enum ManeuverType
    {
        Undefined = 0,
        Vorbeifahren,
        Uberholen,

        Spurenwechsel_L,
        Spurenwechsel_R,
        
        Abbiegen_L,
        Abbiegen_R,

        Spurenwechsel_Any,
        Abbiegen_Any
    }

    public static class ManeuverUtil
    {
        static short[] __maneuverKeys;
        static Dictionary<ManeuverType, LookEventType[]> __sequences;

        static ManeuverUtil()
        {
            __maneuverKeys = GetAllTypes().Select(x=> (short)x).ToArray();
            __sequences = new Dictionary<ManeuverType, LookEventType[]>();
            __sequences.Add(ManeuverType.Vorbeifahren, 
                            new LookEventType[] { 
                                LookEventType.SideMirror_L, 
                                LookEventType.ShoulderLook_L, 
                                LookEventType.RearViewMirror });

            __sequences.Add(ManeuverType.Uberholen, 
                            new LookEventType[] { 
                                LookEventType.SideMirror_L, 
                                LookEventType.ShoulderLook_L, 
                                LookEventType.RearViewMirror,
                                LookEventType.SideMirror_R,
                                LookEventType.ShoulderLook_R,
                                LookEventType.RearViewMirror });

            __sequences.Add(ManeuverType.Spurenwechsel_L, __sequences[ManeuverType.Vorbeifahren]);
            __sequences.Add(ManeuverType.Spurenwechsel_R, 
                            new LookEventType[] { 
                                LookEventType.SideMirror_R, 
                                LookEventType.ShoulderLook_R, 
                                LookEventType.RearViewMirror });

            __sequences.Add(ManeuverType.Abbiegen_L, __sequences[ManeuverType.Spurenwechsel_L]);
            __sequences.Add(ManeuverType.Abbiegen_R, __sequences[ManeuverType.Spurenwechsel_R]);
        }

        public static short[] GetDictKeys()
        {
            return __maneuverKeys;
        }

        public static IEnumerable<ManeuverType> GetAllTypes()
        {
            yield return ManeuverType.Abbiegen_L;
            yield return ManeuverType.Abbiegen_R;
            yield return ManeuverType.Spurenwechsel_L;
            yield return ManeuverType.Spurenwechsel_R;
            yield return ManeuverType.Uberholen;
            yield return ManeuverType.Vorbeifahren;
        }

        public static string Readable(this ManeuverType type, bool showAlignment=true)
        {
            switch(type)
            {
                case ManeuverType.Spurenwechsel_L:      return "Spurwechsel" + (showAlignment ? " L" : "");
                case ManeuverType.Spurenwechsel_R:      return "Spurwechsel" + (showAlignment ? " R" : "");;
                case ManeuverType.Spurenwechsel_Any:    return "Spurwechsel";
                case ManeuverType.Uberholen:            return "Überholen";
                case ManeuverType.Abbiegen_L:           return "Abbiegen" + (showAlignment ? " L" : "");;
                case ManeuverType.Abbiegen_R:           return "Abbiegen" + (showAlignment ? " R" : "");;
                case ManeuverType.Abbiegen_Any:         return "Abbiegen";
                case ManeuverType.Vorbeifahren:         return "Vorbeifahren";
                default:                                return "Undefined";
            }
        }

        public static ManeuverType ToManeuver(this LookEventType type)
        {
            switch(type)
            {
                case LookEventType.M_Spurwechsel_L: return ManeuverType.Spurenwechsel_L;
                case LookEventType.M_Spurwechsel_R: return ManeuverType.Spurenwechsel_R;
                case LookEventType.M_Uberholen:     return ManeuverType.Uberholen;
                case LookEventType.M_Abbiegen_L:    return ManeuverType.Abbiegen_L;
                case LookEventType.M_Abbiegen_R:    return ManeuverType.Abbiegen_R;
                case LookEventType.M_Vorbeifahren:  return ManeuverType.Vorbeifahren;
                default:                            return ManeuverType.Undefined;
            }
        }
        public static LookEventType ToLookEvent(this ManeuverType type)
        {
            switch(type)
            {
                case ManeuverType.Spurenwechsel_L:  return LookEventType.M_Spurwechsel_L;
                case ManeuverType.Spurenwechsel_R:  return LookEventType.M_Spurwechsel_R;
                case ManeuverType.Uberholen:        return LookEventType.M_Uberholen;
                case ManeuverType.Abbiegen_L:       return LookEventType.M_Abbiegen_L;
                case ManeuverType.Abbiegen_R:       return LookEventType.M_Abbiegen_R;
                case ManeuverType.Vorbeifahren:     return LookEventType.M_Vorbeifahren;
                default:                            return LookEventType.None;
            }
        }


        public static LookEventType[] GetLookSequence(this ManeuverType type)
        {
            if(!__sequences.ContainsKey(type))
            {
                return new LookEventType[0];
            }
            else
            {
                return __sequences[type];
            }
        }

        public static ManeuverType GetAlignedType(this ManeuverType type, LookEventType look)
        {
            switch(type)
            {
                case ManeuverType.Abbiegen_Any:

                    int align = look.GetAlignment();   
                    if(align < 0) return ManeuverType.Abbiegen_L;
                    else if(align > 0) return ManeuverType.Abbiegen_R;
                    break;  

                case ManeuverType.Spurenwechsel_Any:

                    int align2 = look.GetAlignment();   
                    if(align2 < 0) return ManeuverType.Spurenwechsel_L;
                    else if(align2 > 0) return ManeuverType.Spurenwechsel_R;
                    break;    
            }
            return type;
        }

        public static int GetAlignment(this ManeuverType type)
        {
            switch(type)
            {
                case ManeuverType.Spurenwechsel_L:
                case ManeuverType.Abbiegen_L:           return -1;

                case ManeuverType.Spurenwechsel_R:
                case ManeuverType.Abbiegen_R:           return 1;
                default:                                return 0;
            }
        }

        public static bool HasAlignment(this ManeuverType type)
        {
            return type != ManeuverType.Abbiegen_Any && type != ManeuverType.Spurenwechsel_Any;
        }

        public static bool MatchUnalignedType(this ManeuverType type, ManeuverType other)
        {
            switch(type)
            {
                case ManeuverType.Spurenwechsel_Any:
                case ManeuverType.Spurenwechsel_L:
                case ManeuverType.Spurenwechsel_R:      
                    return other == ManeuverType.Spurenwechsel_Any
                        || other == ManeuverType.Spurenwechsel_L
                        || other == ManeuverType.Spurenwechsel_R;
                case ManeuverType.Abbiegen_Any:
                case ManeuverType.Abbiegen_L:
                case ManeuverType.Abbiegen_R:           
                    return other == ManeuverType.Abbiegen_Any
                        || other == ManeuverType.Abbiegen_L
                        || other == ManeuverType.Abbiegen_R;
                default:
                    return false;
            }
        }
    }


    /// @brief
    /// maneuver consists of a sequence of LookEvents
    ///
    ///
    public class Maneuver
    {
        public readonly int taskID;
        public readonly int maneuverID;
        public readonly ManeuverType type;
        public List<int> eventIDs;
        public RangeInt timing;
        public int alignment;       //  -1: left  | 1: right

        public int EventCount { get { return eventIDs != null ? eventIDs.Count : 0; } }

        public Maneuver(int taskID, ManeuverType type, int maneuverID, int align=0)
        {
            this.taskID = taskID;
            this.type = type;
            this.maneuverID = maneuverID;
            this.alignment = align;
            eventIDs = new List<int>();
        }

        public void AddEvent(int index)
        {
            eventIDs.Add(index);
        }

        public int GetSequenceID(int eventID)
        {
            if(EventCount > 0 && eventID > 0)
            {
                for(int i = 0; i < eventIDs.Count; i++)
                {
                    if(eventIDs[i] == eventID) return i;
                }
            }
            return -1;
        }

        /// @returns wether given event index is first of this maneuver
        ///
        public bool CheckFirstEvent(int eventID)
        {
            if(EventCount > 0 && eventID > 0)
            {
                return eventIDs[0] == eventID;
            }
            return false;
        }

        /// @returns wether given event index between first and last event of this maneuver
        ///
        public bool CheckInnerEvent(int eventID)
        {
            if(EventCount > 0 && eventID > 0)
            {
                for(int i = 1; i < EventCount-1; i++) 
                    if(eventIDs[i] == eventID)
                        return true;
            }
            return false;
        }

        /// @returns wether given event index is last of this maneuver
        ///
        public bool CheckLastEvent(int eventID)
        {
            if(EventCount > 0 && eventID > 0)
            {
                return eventIDs[EventCount-1] == eventID;
            }
            return false;
        }

        public bool CheckTimeRange(int timeMs)
        {
            return timeMs >= timing.start && timeMs <= timing.end;
        }
        public bool CheckTimeRange(RangeInt range)
        {
            return CheckTimeRange(range.start) || CheckTimeRange(range.end);
        }


        public static bool TryParse(string s, out ManeuverType type)
        {
            if(s.Length > 0)
            {
                return TryParse(s[0], out type);
            }
            type = ManeuverType.Undefined;
            return false;
        }

        public static bool TryParse(char s, out ManeuverType type)
        {
            switch(s) {
                case 'A':
                case 'a':   type = ManeuverType.Abbiegen_Any; return true;
                case 'V':
                case 'v':   type = ManeuverType.Vorbeifahren; return true;
                case 'S':
                case 's':   type = ManeuverType.Spurenwechsel_Any; return true;
                case 'U':
                case 'u':   type = ManeuverType.Uberholen; return true;
                default:    type = ManeuverType.Undefined; return false;
            }
        }
    }

}