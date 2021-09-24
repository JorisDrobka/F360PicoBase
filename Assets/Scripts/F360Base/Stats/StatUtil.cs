using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using F360.Data;

namespace F360.Users.Stats
{

    /// @brief
    /// helper functions to generate stats
    ///
    public static class StatUtil
    {

        public const int RATING_NONE = Constants.RATING_NONE;
        public const int RATING_MAX = Constants.RATING_MAX;
        public const int RATING_MIN = Constants.RATING_MIN;
        
        static List<RatedLookEvent> lookBuffer;
        static List<RatedHazardEvent> hazardBuffer;


        //-----------------------------------------------------------------------------------------------
        //  
        //      INTERFACE - RATINGS
        //
        //-----------------------------------------------------------------------------------------------


        public static int Clamp(int rating) 
        {
            if(rating >= RATING_MIN) 
                return Mathf.Clamp(rating, RATING_MIN, RATING_MAX); 
            else
                return RATING_NONE;
        }

        /// @brief
        /// Generates all displayable ratings from a sequence of generic rated events.
        /// 
        /// @returns A datastructure storing all ratings by type
        ///
        public static RatingMap CreateRatingMap(IEnumerable<IRatedEvent> e)
        {
            var looks = e.Where(x=> x is RatedLookEvent).Cast<RatedLookEvent>();
            var hazards = e.Where(x=> x is RatedHazardEvent).Cast<RatedHazardEvent>();
            return CreateRatingMap(looks, hazards);
        }

        /// @brief
        /// Generates all displayable ratings from a sequence of generic rated events.
        /// 
        /// @returns A datastructure storing all ratings by type
        ///
        public static RatingMap CreateRatingMap(IEnumerable<RatedLookEvent> looks, IEnumerable<RatedHazardEvent> hazards)
        {
            var map = new RatingMap();
            map.Maneuvers = calc_ManeuverRating(looks);
            map.Awareness = calc_AwarenessRating(looks);
            map.Attention = calc_AttentionRating(looks);
            map.Hazards = calc_HazardRating(hazards);
            map.Anticipation = calc_AnticipationRating(hazards);
            map.Total = GetTotalRating(map);
            return map;
        }

        //-----------------------------------------------------------------------------------------------

        public static int GetTotalRating(RatingMap map)
        {
            return calc_totalRating(map.Maneuvers, map.Awareness, map.Attention, map.Hazards, map.Anticipation);
        }
        public static int GetTotalRating(int maneuvers, int awareness, int attention, int hazards, int anticipation)
        {
            return calc_totalRating(maneuvers, awareness, attention, hazards, anticipation);
        }

        public static int GetManeuverRating(IEnumerable<RatedLookEvent> e)
        {
            return calc_ManeuverRating(e);
        }

        public static int GetAttentionRating(IEnumerable<RatedLookEvent> e)
        {
            return calc_AttentionRating(e);
        }

        public static int GetHazardRating(IEnumerable<RatedHazardEvent> e)
        {
            return calc_HazardRating(e);
        }

        public static int GetAnticipationRating(IEnumerable<RatedHazardEvent> e)
        {
            return calc_AnticipationRating(e);
        }

        public static int GetAwarenessRating(IEnumerable<RatedLookEvent> e)
        {
            return calc_AwarenessRating(e);
        }


        //-----------------------------------------------------------------------------------------------
        //  
        //      INTERFACE - MAPS
        //
        //-----------------------------------------------------------------------------------------------

        public static LookEventMap WriteLookMap(LookEventMap target, IEnumerable<RatedLookEvent> e)
        {
            if(target == null)
            {
                return create_LookMap(e);
            }
            else
            {
                return write_LookMap(target, e);
            }
        }

        public static LookEventMap WriteManeuverMap(LookEventMap target, ManeuverType type, IEnumerable<RatedLookEvent> e)
        {
            if(target == null)
            {
                return create_ManeuverMap(type, e);
            }   
            else
            {
                return write_ManeuverMap(target, type, e);
            }
        }


        //-----------------------------------------------------------------------------------------------
        //  
        //      INTERFACE - DRIVE SESSION OPERATIONS
        //
        //-----------------------------------------------------------------------------------------------

        /// @returns start date of considered DriveVR sessions - older sessions are considered as archived.
        ///
        public static DateTime GetDriveVRRatingBegin(DateTime lastSynchTime)
        {
            return lastSynchTime.AddDays(-Constants.DRIVEVR_DAYS_RATED);
        }

        /// @returns all DriveVR sessions created within the considered time frame.
        ///
        public static IEnumerable<DriveSessionStats> GetNonArchivedDriveSessions(DateTime lastSynchTime, IEnumerable<DriveSessionStats> list)
        {
            var cap = GetDriveVRRatingBegin(lastSynchTime);
            return list.Where(x=> x.Time_Created >= cap);
        }


        /// @brief
        /// Adds up all look & hazard ratings of a set of sessions with same video ID.
        /// The resulting data reflects the average of all sessions taken.
        ///
        public static DriveSessionStats MergeDriveSessions(IEnumerable<DriveSessionStats> sessions, DriveSessionStats container=null)
        {
            return op_MergeDriveSessionSame(sessions, container);
        }

        /// @brief
        /// Adds up all look & hazard ratings of a given set of sessions to show summed up statistics.
        ///
        public static DriveSessionStats MergeDriveSessionsAny(IEnumerable<DriveSessionStats> sessions, DriveSessionStats container=null)
        {
            return op_MergeDriveSessionsAny(sessions, container);
        }   

        //-----------------------------------------------------------------------------------------------

        //  buffer

        public static List<RatedLookEvent> GetLookBuffer()
        {
            if(lookBuffer == null) {
                lookBuffer = new List<RatedLookEvent>(120);
            }
            else {
                lookBuffer.Clear();
            }
            return lookBuffer;
        }

        public static List<RatedHazardEvent> GetHazardBuffer()
        {
            if(hazardBuffer == null) {
                hazardBuffer = new List<RatedHazardEvent>(120);
            }
            else {
                hazardBuffer.Clear();
            }
            return hazardBuffer;
        }



        //-----------------------------------------------------------------------------------------------
        //  
        //      INTERNAL
        //
        //-----------------------------------------------------------------------------------------------


        //  ratings calculation

        static int calc_totalRating(int maneuvers, int awareness, int attention, int hazards, int anticipation)
        {
            float sum = 0f; int num = 0;
            if(maneuvers >= RATING_MIN)     { sum += Mathf.Min(maneuvers, RATING_MAX); num++; }
            if(awareness >= RATING_MIN)     { sum += Mathf.Min(awareness, RATING_MAX); num++; }
            if(attention >= RATING_MIN)     { sum += Mathf.Min(attention, RATING_MAX); num++; }
            if(hazards >= RATING_MIN)       { sum += Mathf.Min(hazards, RATING_MAX); num++; }
            if(anticipation >= RATING_MIN)  { sum += Mathf.Min(anticipation, RATING_MAX); num++; }
            if(num > 0)
            {
                return Clamp(Mathf.RoundToInt(sum / num));
            }
            else
            {
                return RATING_NONE;
            }
        }

        static int calc_ManeuverRating(IEnumerable<RatedLookEvent> e)
        {
            if(e != null)
            {
                float sum = 0f; float sum2 = 0f; 
                int num = 0; 
                int mID = -1;
                foreach(var look in e)
                {
                    if(look.isPartOfManeuver())
                    {
                        if(look.ManeuverIndex > mID)
                        {
                            mID = look.ManeuverIndex;
                            sum += sum2;
                            sum2 = look.Rating;
                            num++;
                        }
                        else
                        {
                            sum2 += look.Rating;
                        }
                    }
                }
                if(sum2 > 0)
                {
                    sum += sum2;
                    num++;
                }
                if(num > 0)
                {
                    return Clamp(Mathf.RoundToInt(sum / num));
                }
            }
            return RATING_NONE;
        }

        static int calc_AwarenessRating(IEnumerable<RatedLookEvent> e)
        {
            if(e != null)
            {
                float sum = 0f; int num = 0;
                foreach(var look in e)
                {
                    if(!look.isPartOfManeuver())
                    {
                        if(look.MatchType( LookEventType.Check_Direction,
                                        LookEventType.Check_Direction_L,
                                        LookEventType.Check_Direction_R,
                                        LookEventType.Observe_Direction,
                                        LookEventType.Observe_Direction_L,
                                        LookEventType.Observe_Direction_R, 
                                        LookEventType.RearViewMirror, 
                                        LookEventType.Follow_Marker ))
                        {
                            sum += look.Rating;
                            num++;
                        }
                    }
                }
                if(num > 0)
                {
                    return Clamp(Mathf.RoundToInt(sum / num));
                }
            }
            return RATING_NONE;
        }   

        static int calc_AttentionRating(IEnumerable<RatedLookEvent> e)
        {
            if(e != null)
            {
                float sum = 0f; int num = 0;
                foreach(var look in e)
                {
                    if(!look.isPartOfManeuver())
                    {
                        if(look.MatchType( LookEventType.Observe_Situation ))
                        {
                            sum += look.Rating;
                            num++;
                        }
                    }
                }
                if(num > 0)
                {
                    return Clamp(Mathf.RoundToInt(sum / num));
                }
            }
            return RATING_NONE;
        }

        static int calc_HazardRating(IEnumerable<RatedHazardEvent> e)
        {
            if(e != null)
            {
                float sum = 0f; int num = 0;
                foreach(var hz in e)
                {
                    if(hz.Type == HazardEventType.Quick)
                    {
                        sum += hz.Rating;
                        num++;
                    }
                }
                if(num > 0)
                {
                    return Clamp(Mathf.RoundToInt(sum / num));
                }
            }
            return RATING_NONE;
        }

        static int calc_AnticipationRating(IEnumerable<RatedHazardEvent> e)
        {
            if(e != null)
            {
                float sum = 0f; int num = 0;
                foreach(var hz in e)
                {
                    if(hz.Type == HazardEventType.Anticipated)
                    {
                        sum += hz.Rating;
                        num++;
                    }
                }
                if(num > 0)
                {
                    return Clamp(Mathf.RoundToInt(sum / num));
                }
            }
            return RATING_NONE;
        }


        //-----------------------------------------------------------------------------------------------

        //  maps


        static LookEventMap create_LookMap(IEnumerable<RatedLookEvent> e)
        {
            var map = new LookEventMap();
            return write_LookMap(map, e);
        }

        static LookEventMap write_LookMap(LookEventMap target, IEnumerable<RatedLookEvent> e)
        {
            var keys = target.GetSavedKeys();
            target.Reset();
            foreach(var look in keys)
            {
                target.SetRating(look, calc_lookAverage(look, e));
            }
            return target;
        }

        static LookEventMap create_ManeuverMap(ManeuverType type, IEnumerable<RatedLookEvent> e)
        {
            var map = new LookEventMap();
            return write_ManeuverMap(map, type, e);
        }

        static LookEventMap write_ManeuverMap(LookEventMap target, ManeuverType type, IEnumerable<RatedLookEvent> e)
        {
            var constraint = ManeuverUtil.GetLookSequenceUnique(type);
            target.Reset();
            foreach(var look in constraint)
            {
                target.SetRating(look, calc_lookAverage(look, e, type));
            }
            return target;
        }


        static int calc_lookAverage(LookEventType type, IEnumerable<RatedLookEvent> events, ManeuverType constraint=ManeuverType.Undefined)
        {
            float sum = 0f; int num = 0;
            foreach(var look in events)
            {
                if(look.Type == type && look.ManeuverType == constraint)
                {
                    sum += look.Rating;
                    num++;
                }
            }
            if(num > 0)
            {
                return Clamp(Mathf.RoundToInt(sum / num));
            }
            else
            {
                return RATING_NONE;
            }
        }


        //-----------------------------------------------------------------------------------------------

        //  drive sessions

        static DriveSessionStats op_MergeDriveSessionSame(IEnumerable<DriveSessionStats> sessions, DriveSessionStats container=null)
        {
            var lbuffer = GetLookBuffer();
            var hbuffer = GetHazardBuffer();
            int vID = -1;
            foreach(var session in sessions)
            {
                if(vID == -1)
                {
                    vID = session.VideoID;
                }
                else if(vID != session.VideoID)
                {
                    Debug.LogError("Cannot merge sessions with different video IDs!");
                    return container;
                }

                lbuffer.AddRange(session.GetLookEvents());
                hbuffer.AddRange(session.GetHazardEvents());
            }
            int sessionCount = sessions.Count();
            int lstep = lbuffer.Count / sessionCount;
            if(lstep > 0)
            {
                int val = 0;
                for(int i = 0; i < lstep; i++)
                {
                    for(int j = 0; j < lbuffer.Count; j += lstep)
                    {
                        val += lbuffer[j].Rating;
                    }
                    val = val / lstep;
                    lbuffer[i] = new RatedLookEvent(lbuffer[i], val);
                }
                lbuffer.RemoveRange(lstep, lbuffer.Count-lstep);
            }
            int hstep = hbuffer.Count / sessionCount;
            if(hstep > 0)
            {
                int val = 0;
                for(int i = 0; i < hstep; i++)
                {
                    for(int j = 0; j < hbuffer.Count; j += hstep)
                    {
                        val += hbuffer[j].Rating;
                    }
                    val = val / hstep;
                    hbuffer[i] = new RatedHazardEvent(hbuffer[i], val);
                }
                hbuffer.RemoveRange(hstep, hbuffer.Count-hstep);
            }
            if(container == null)
            {
                container = new DriveSessionStats(SynchURI.NO_SYNCH, -1, lbuffer, hbuffer);
            }
            else
            {
                container.SetData(SynchURI.NO_SYNCH, -1, lbuffer, hbuffer);
            }
            return container;
        }

        

        static DriveSessionStats op_MergeDriveSessionsAny(IEnumerable<DriveSessionStats> sessions, DriveSessionStats container=null)
        {
            var lbuffer = GetLookBuffer();
            var hbuffer = GetHazardBuffer();
            foreach(var session in sessions)
            {
                lbuffer.AddRange(session.GetLookEvents());
                hbuffer.AddRange(session.GetHazardEvents());
            }
            if(container == null) 
            {
                container = new DriveSessionStats(SynchURI.NO_SYNCH, -1, lbuffer, hbuffer);
            }
            else
            {
                container.SetData(SynchURI.NO_SYNCH, -1, lbuffer, hbuffer);
            }
            return container;
        }


        
    }



}

