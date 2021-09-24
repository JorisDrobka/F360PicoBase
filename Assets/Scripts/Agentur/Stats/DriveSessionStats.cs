using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using F360.Data;



 /* ######### ILUME ##########
    
    Muss serialisiert & synchronisiert werden
    Nach Deserialisierung bitte OnAfterDeserialize() aufrufen.

*/


namespace F360.Users.Stats
{


    /// @brief
    /// all data & ratings of a single driving session (DriveVR and Exam/Prüfungs-Simulation)
    ///
    /// after deserialization, runtime data is loaded for querying.
    ///
    ///
    public class DriveSessionStats : ISynchable
    {

        //-----------------------------------------------------------------------------------------------
        //
        //  interface

        public DateTime Time_Created { get { return create_T; } }

        public int VideoID { get { return v_id; } }

        public string SynchUri { get { return s_uri; } }

        public bool hasHazards()
        {
            return h_ratings != null && h_ratings.Length > 0;
        }

        public int TotalRating
        {
            get {
                if(ratingMap == null)
                {
                    Debug.LogWarning("DriveSessionStats was not properly deserialized.");
                    return Constants.RATING_NONE;
                }
                return ratingMap.Total;
            }
        }

        public RatingMap Ratings
        {
            get {
                if(ratingMap == null)
                {
                    Debug.LogWarning("DriveSessionStats was not properly deserialized.");
                    return null;
                }
                return ratingMap;
            }
        }

        public bool isDirty { get; set; }                   ///< flag marking this object for serialization

        //-----------------------------------------------------------------------------------------------

        //  maps


        /// @brief
        /// call this to write look data of this session to target LookEventMap
        ///
        public LookEventMap WriteLookMap(LookEventMap target)
        {
            if(l_ratings != null) {
                return StatUtil.WriteLookMap(target, lookArray);
            }
            else {
                Debug.LogWarning("DriveSessionStats:: cannot write look map, not properly deserialized.");
                return target;
            }
        }


        /// @brief
        /// call this to write maneuver data of this session to target LookEventMap
        ///
        public LookEventMap WriteManeuverMap(LookEventMap target, ManeuverType type)
        {
            if(l_ratings != null) {
                return StatUtil.WriteManeuverMap(target, type, lookArray);
            }
            else {
                Debug.LogWarning("DriveSessionStats:: cannot write maneuver map, not properly deserialized.");
                return target;
            }
        }


        public IEnumerable<RatedLookEvent> GetLookEvents()
        {
            return lookArray;
        }

        public IEnumerable<RatedHazardEvent> GetHazardEvents()
        {
            return hazardArray;
        }

    
        //-----------------------------------------------------------------------------------------------
        //
        //  serialized data


        /// ###   SERIALIZE & SYNCH   ### 

        int v_id;
        DateTime create_T;
        string s_uri;
        internal int[] l_ratings { get; private set; }
        internal int[] h_ratings { get; private set; }

        public void ReadSerializedData(SerializedDriveSession data)
        {
            v_id = data.video;
            s_uri = data.uri;
            StatSerializationUtil.TryParseTime(data.created, ref create_T);
            l_ratings = StatSerializationUtil.ConvertToIntArray(data.looks);
            h_ratings = StatSerializationUtil.ConvertToIntArray(data.hazards);
        }

        ISynchedTerm ISynchable.GetSynchTerm()
        {
            return new SerializedDriveSession(this);
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  runtime data

        List<RatedLookEvent> lookArray;
        List<RatedHazardEvent> hazardArray;
        RatingMap ratingMap;


        //-----------------------------------------------------------------------------------------------
        //
        //  constructor

        public DriveSessionStats(SerializedDriveSession data)
        {
            ReadSerializedData(data);
            OnAfterDeserialize();
        }
        public DriveSessionStats(string synchURI, int videoID, IEnumerable<IRatedEvent> events)
        {
            this.s_uri = synchURI;
            this.v_id = videoID;
            this.create_T = Backend.TimeUtil.ServerTime;
            var looks = events.Where(x=> x is RatedLookEvent).Cast<RatedLookEvent>();
            var hazards = events.Where(x=> x is RatedHazardEvent).Cast<RatedHazardEvent>();
            writeStatsInternal(looks, hazards);
        }

        public DriveSessionStats(string synchURI, int videoID, IEnumerable<RatedLookEvent> looks, IEnumerable<RatedHazardEvent> hazards=null)
        {
            this.s_uri = synchURI;
            this.v_id = videoID;
            this.create_T = Backend.TimeUtil.ServerTime;
            writeStatsInternal(looks, hazards);
        }

        //-----------------------------------------------------------------------------------------------

        /// @brief
        /// overrides content of this session.
        ///
        public void SetData(string synchUri, int videoID, IEnumerable<RatedLookEvent> looks, IEnumerable<RatedHazardEvent> hazards=null)
        {
            this.s_uri = synchUri;
            this.v_id = videoID;
            this.create_T = Backend.TimeUtil.ServerTime;
            writeStatsInternal(looks, hazards);    
        }


        //-----------------------------------------------------------------------------------------------


        /// @brief
        /// loads runtime datastructures after deserialization
        ///
        void OnAfterDeserialize()
        {
            if(VideoRepo.Current == null)
            {
                Debug.LogError("No VideoRepo found while unpacking DriveSession[" + SynchUri + "]!");
            }
            else
            {
                VideoMetaData data;
                if(VideoRepo.Current.GetMetadata(VideoID, out data))
                {
                    if(l_ratings != null && data.hasLookData())
                    {
                        if(data.looks.EventCount != l_ratings.Length)
                        {
                            Debug.LogError("LookEvent count mismatch! DriveSession[" + SynchUri + "]\n\tVideoID=" + VideoID + "\n\tEvents: " + l_ratings.Length + " Saved Data: " + data.LookCount);
                        }
                        else
                        {
                            //  load look events
                            lookArray = new List<RatedLookEvent>();
                            for(int i = 0; i < data.looks.EventCount; i++)
                            {
                                var e = data.looks.GetEventAt(i);
                                lookArray.Add(new RatedLookEvent(
                                    i,
                                    e.type,
                                    e.maneuverID,
                                    data.looks.GetManeuverTypeAt(e.maneuverID),
                                    l_ratings[i]
                                ));
                            }
                        }
                        
                    }
                    if(h_ratings != null && data.hasHazards())
                    {
                        if(data.hazards.EventCount != h_ratings.Length)
                        {
                            Debug.LogError("HazardEvent count mismatch! DriveSession[" + SynchUri + "]\n\tVideoID=" + VideoID + "\n\tEvents: " + h_ratings.Length + " Saved Data: " + data.HazardCount);
                        }
                        else
                        {
                            //  load hazard events
                            hazardArray = new List<RatedHazardEvent>();
                            for(int i = 0; i < data.hazards.EventCount; i++)
                            {
                                var e = data.hazards.GetEventAt(i);
                                hazardArray.Add(new RatedHazardEvent(i, e.type, h_ratings[i]));
                            }
                        }
                    }

                    //  calc rating map
                    this.ratingMap = StatUtil.CreateRatingMap(lookArray, hazardArray);
                }
                else
                {
                    Debug.LogError("No VideoMetaData found while unpacking DriveSession[" + SynchUri + "]\n\tVideoID=" + VideoID);
                }
            }
        }


        //-----------------------------------------------------------------------------------------------

        void writeStatsInternal(IEnumerable<RatedLookEvent> looks, IEnumerable<RatedHazardEvent> hazards)
        {
            int lC = looks != null ? looks.Count() : 0;
            int hC = hazards != null ? hazards.Count() : 0;
            this.l_ratings = new int[lC];
            this.h_ratings = new int[hC];
            if(lC > 0)
            {
                int c = 0;
                foreach(var l in looks)
                {
                    this.l_ratings[c] = l.Rating;
                    c++;
                }
                if(lookArray != null) {
                    lookArray.Clear();
                    lookArray.AddRange(looks);
                }
                else {
                    lookArray = new List<RatedLookEvent>(looks);
                }
            }
            else
            {
                if(lookArray != null) lookArray.Clear();
                else lookArray = new List<RatedLookEvent>();
            }
            if(hC > 0)
            {
                int c = 0;
                foreach(var h in hazards)
                {
                    this.h_ratings[c] = h.Rating;
                    c++;
                }
                if(hazardArray != null) {
                    hazardArray.Clear();
                    hazardArray.AddRange(hazards);
                }
                else {
                    hazardArray = new List<RatedHazardEvent>(hazards);
                }
            }
            else
            {
                if(hazardArray != null) hazardArray.Clear();
                else hazardArray = new List<RatedHazardEvent>();
            }

            this.ratingMap = StatUtil.CreateRatingMap(looks, hazards);
        }

    }


}


