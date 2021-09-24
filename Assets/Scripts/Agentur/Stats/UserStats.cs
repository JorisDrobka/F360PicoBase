using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using F360.Data;
using F360.Users.Stats;

namespace F360.Users
{


    


    /// @brief  
    /// Runtime access to all stats of a user.
    /// Consists of a number of serializable & synchable datastructures implementing ISynchable interface.
    ///
    /// Loaded & managed via StatRepo.
    ///
    public class UserStats : IUserStats
    {

        //  synchronized fields

        UserMeta meta;

        DriveSessionStats ex_bronze;
        DriveSessionStats ex_silver;
        DriveSessionStats ex_gold;
        Dictionary<int, VTrainerStats> vTrainerStats;
        List<DriveSessionStats> driveVRSessions;



        //-----------------------------------------------------------------------------------------------
        //
        //  Constructor


        public readonly int UserID;

        public UserStats(int userID)
        {
            UserID = userID;
            meta = new UserMeta();
            OverallRatings = new RatingMap();
            vTrainerStats = new Dictionary<int, VTrainerStats>();
            driveVRSessions = new List<DriveSessionStats>();
        }

        int IUserStats.UserID { get { return UserID; } }



        //-----------------------------------------------------------------------------------------------
        //
        //  Meta

        public UserMeta Meta { get { return meta; } }


        /// @returns wether a driveVR session was already seen by user in menu
        ///
        public bool hasVisitedDriveVR(int videoID)
        {
            return meta.hasVisitedDriveVRSession(videoID);
        }

        /// @brief
        /// Call to mark a driveVR session as visited by user
        ///
        public void MarkDriveVRVisited(int videoID)
        {
            meta.AddVisitedDriveVRTask(videoID);
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  Progress

        public RatingMap OverallRatings { get; private set; }


        /// @brief
        /// total progress of user in percent
        ///
        public int Progress_Overall { get; private set; } = Constants.RATING_NONE;

        public int Progress_VTrainer { get; private set; } = Constants.RATING_NONE;

        public int Progress_DriveVR { get; private set; } = Constants.RATING_NONE;

        public int Progress_Exam { get; private set; } = Constants.RATING_NONE;


        public void RecalcProgress()
        {
            recalc();
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  VTrainer

        public int Rating_VTrainer { get; private set; } = Constants.RATING_NONE;

        public VTrainerStats GetVTrainerStats(int chapterID)
        {
            if(vTrainerStats.ContainsKey(chapterID)) { return vTrainerStats[chapterID]; }
            else { 
                Debug.LogWarning("UserStats: VTrainerChapter[" + chapterID + "] does not exist");
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------
        //
        //  DriveVR

        public int Rating_DriveVR { get; private set; } = Constants.RATING_NONE;

        /// @brief
        /// All rated sessions combined to read out overall stats
        ///
        public DriveSessionStats AddedDriveVRSessions { get; private set; }


        /// @returns the averaged stats of an individual driveVR video for display.
        ///
        public DriveSessionStats GetDriveVRSessionAveraged(int videoID, DriveSessionStats buffer=null, bool includeArchived=false)
        {
            return StatUtil.MergeDriveSessions(GetDriveVRSessions(videoID, includeArchived), buffer);
        }

        /// @returns data of all sessions of an individual driveVR video.
        ///
        public IEnumerable<DriveSessionStats> GetDriveVRSessions(int videoID, bool includeArchived=false)
        {
            if(includeArchived)
            {
                return driveVRSessions.Where(x=> x.VideoID == videoID);
            }
            else
            {
                var cap = getDriveVRMinRatingDate();
                return driveVRSessions.Where(x=> x.VideoID == videoID && x.Time_Created >= cap);
            }
        }

        /// @returns all driveVR sessions of this user
        ///
        public IEnumerable<DriveSessionStats> GetDriveVRSessionsAll(bool includeArchived=false)
        {
            if(includeArchived)
            {
                return driveVRSessions;
            }
            else
            {
                var cap = getDriveVRMinRatingDate();
                return driveVRSessions.Where(x=> x.Time_Created >= cap);
            }
        }

        

        //-----------------------------------------------------------------------------------------------
        //
        //  Exam

        public int Rating_Exam { get; private set; } = Constants.RATING_NONE;

        public DriveSessionStats Exam_Bronze 
        { 
            get { return ex_bronze; } 
        }
        
        public DriveSessionStats Exam_Silver 
        { 
            get { return ex_silver; } 
        }
        
        public DriveSessionStats Exam_Gold 
        { 
            get { return ex_gold; }
        }

        public bool hasCompletedExam(ExamLevel level)
        {
            switch(level)
            {
                case ExamLevel.Bronze:  return Exam_Bronze != null;
                case ExamLevel.Silver:  return Exam_Silver != null;
                case ExamLevel.Gold:    return Exam_Gold != null;
                default:                return false;
            }
        }


        public int GetTimeSecondsUntilExamUnlock(ExamLevel level)
        {
            switch(level)
            {
                case ExamLevel.Bronze:  return calc_TimeSecondsUntilExamUnlock(Exam_Bronze);
                case ExamLevel.Silver:  return calc_TimeSecondsUntilExamUnlock(Exam_Silver);
                case ExamLevel.Gold:    return calc_TimeSecondsUntilExamUnlock(Exam_Gold);
                default:                return 0;
            }
        }



        //-----------------------------------------------------------------------------------------------
        //
        //  Synch Interface


        /// @returns wether stats have new data to be synchronized
        ///
        public bool isReadyToSynch()
        {
            if(meta.LastChangedTime > meta.LastSynchTime) return true;
            for(int i = 0; i < driveVRSessions.Count; i++) {
                if(driveVRSessions[i].Time_Created > meta.LastSynchTime) return true;
            }
            if(ex_bronze != null && ex_bronze.Time_Created > meta.LastSynchTime) return true;
            if(ex_silver != null && ex_silver.Time_Created > meta.LastSynchTime) return true;
            if(ex_gold != null && ex_gold.Time_Created > meta.LastSynchTime) return true;
            return false;
        }

        /// @returns all synchable terms
        ///
        public IEnumerable<ISynchedTerm> GetChangedValues()
        {
            //  meta
            if(meta.LastChangedTime > meta.LastSynchTime)
            {
                yield return new SerializedUserMetaData(meta);
            }

            //  drive vr
            foreach(var session in driveVRSessions)
            {
                if(session.Time_Created > meta.LastSynchTime)
                {
                    yield return new SerializedDriveSession(session);
                }
            }

            //  vtrainer
            foreach(var chapter in vTrainerStats.Values)
            {
                if(chapter.Time_Created > meta.LastSynchTime)
                {
                    yield return new SerializedVTrainerChapter(chapter);
                }
            }

            //  exam
            if(Exam_Bronze != null && Exam_Bronze.Time_Created > meta.LastSynchTime)
            {
                yield return new SerializedDriveSession(Exam_Bronze);
            }
            if(Exam_Silver != null && Exam_Silver.Time_Created > meta.LastSynchTime)
            {
                yield return new SerializedDriveSession(Exam_Silver);
            }
            if(Exam_Gold != null && Exam_Gold.Time_Created > meta.LastSynchTime)
            {
                yield return new SerializedDriveSession(Exam_Gold);
            }
        }


        /// @brief
        /// update values from server
        ///
        public int UpdateFromServer(IEnumerable<ISynchedTerm> terms)
        {
            int count = 0;
            foreach(var t in terms)
            {
                if(t.GetTimeCreated() > meta.LastSynchTime)
                {
                    if(t is SerializedUserMetaData)
                    {
                        this.meta.UpdateFromServer((SerializedUserMetaData)t);
                        count++;
                    }
                    else if(t is SerializedDriveSession)
                    {
                        var dvr = (SerializedDriveSession)t;
                        if(SynchURI.GetContext(dvr.uri) == TrainingContext.DriveVR)
                        {
                            DriveSessionStats session;
                            int id = getDriveSessionIndexByUri(dvr.uri);
                            if(id == -1)
                            {
                                session = new DriveSessionStats(dvr);
                                driveVRSessions.Add(session);
                            }
                            else
                            {
                                session = driveVRSessions[id];
                                session.ReadSerializedData(dvr);
                            }
                            session.isDirty = true;
                            count++;
                        }
                        else 
                        {
                            switch(dvr.uri)
                            {
                                case SynchURI.EXAM_BRONZE:      
                                    setExam(ExamLevel.Bronze, dvr);
                                    count++;
                                    break;
                                case SynchURI.EXAM_SILVER:      
                                    setExam(ExamLevel.Silver, dvr);
                                    count++;
                                    break;
                                case SynchURI.EXAM_GOLD:        
                                    setExam(ExamLevel.Gold, dvr);
                                    count++;
                                    break;
                            } 
                        }
                    }
                    else if(t is VTrainerStats)
                    {
                        var vtr = t as VTrainerStats;
                        if(!vTrainerStats.ContainsKey(vtr.ChapterID))
                        {
                            vTrainerStats.Add(vtr.ChapterID, vtr);
                        }
                        else
                        {
                            vTrainerStats[vtr.ChapterID].UpdateFromServer(vtr);
                        }
                        count++;
                    }
                }
            }

            if(count > 0)
            {
                meta.LastSynchTime = Backend.TimeUtil.ServerTime;
                recalc();
            }
            return count;
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  Serialization


        /// @brief
        /// Time stats where loaded by repo
        ///
        public DateTime SerializationTime { get; set; }


        /// @returns wether stats need to be saved to disk
        ///
        public bool isDirty()
        {
            if(meta.LastChangedTime > SerializationTime) return true; 
            for(int i = 0; i < driveVRSessions.Count; i++) {
                if(driveVRSessions[i].isDirty) return true;
            }
            if(ex_bronze != null && ex_bronze.isDirty) return true;
            if(ex_silver != null && ex_silver.isDirty) return true;
            if(ex_gold != null && ex_gold.isDirty) return true;
            return false;
        }

        public void LoadFromSerializedData(SavedUserStats data)
        {
            //  Meta
            meta.ReadSerializedData(data.meta);
            
            //  VTrainer
            for(int i = 0; i < data.vTrainer.Length; i++)
            {
                if(vTrainerStats.ContainsKey(data.vTrainer[i].chapter)) {
                    vTrainerStats[data.vTrainer[i].chapter].ReadSerializedData(data.vTrainer[i]);
                }
                else {
                    vTrainerStats.Add(data.vTrainer[i].chapter, new VTrainerStats(data.vTrainer[i]));
                }
            }

            //  DriveVR
            for(int i = 0; i < data.driveVR.Length; i++)
            {
                int id = getDriveSessionIndexByUri(data.driveVR[i].uri);
                if(id != -1){
                    driveVRSessions[id].ReadSerializedData(data.driveVR[i]);
                }
                else if(data.driveVR[i].isValid()) {
                    driveVRSessions.Add(new DriveSessionStats(data.driveVR[i]));
                }
            }

            //  Exam
            if(data.bronze.isValid())
            {
                if(ex_bronze != null)
                {
                    ex_bronze.ReadSerializedData(data.bronze);
                }
                else
                {
                    ex_bronze = new DriveSessionStats(data.bronze);
                }
            }
            if(data.silver.isValid())
            {
                if(ex_silver != null)
                {
                    ex_silver.ReadSerializedData(data.silver);
                }
                else
                {
                    ex_silver = new DriveSessionStats(data.silver);
                }
            }
            if(data.gold.isValid())
            {
                if(ex_gold != null)
                {
                    ex_gold.ReadSerializedData(data.gold);
                }
                else
                {
                    ex_gold = new DriveSessionStats(data.gold);
                }
            }

            recalc();
            SerializationTime = Backend.TimeUtil.ServerTime;
        }
        


        //-----------------------------------------------------------------------------------------------
        //
        //  Progress calc


        void recalc()
        {
            OverallRatings.Clear();           //  add all drive session ratings up

            //  VTrainer - 20%
            calc_VTrainerProgress();

            //  DriveVR  - 50%
            calc_DriveVRProgress();

            //  Exams    - 30%
            calc_ExamProgress();

            //  overall
            Progress_Overall = 
                StatUtil.Clamp(
                    Mathf.CeilToInt(Progress_VTrainer * Constants.PROGRESS_WEIGHT_VTRAINER) +
                    Mathf.CeilToInt(Progress_DriveVR * Constants.PROGRESS_WEIGHT_DRIVEVR) +
                    Mathf.CeilToInt(Progress_Exam * Constants.PROGRESS_WEIGHT_EXAM));

            OverallRatings.Bake();

            //  merge driveVR sessions
            AddedDriveVRSessions = 
                        StatUtil.MergeDriveSessionsAny(
                                    GetDriveVRSessionsAll(), 
                                    AddedDriveVRSessions );
        }

        void calc_VTrainerProgress()
        {
            int count = VTrainerRepo.Current.ChapterCount;
            float sum = 0f;
            float rsum = 0f;
            int rnum = 0;
            foreach(var chapter in vTrainerStats.Keys)
            {
                if(vTrainerStats[chapter].isCompleted)
                {
                    sum += Constants.RATING_MAX;
                }
                if(vTrainerStats[chapter].TotalRating >= Constants.RATING_MIN)
                {
                    rsum += vTrainerStats[chapter].TotalRating;
                    rnum++;
                }
            }
            Progress_VTrainer = StatUtil.Clamp(Mathf.RoundToInt(sum / count));
            Rating_VTrainer = StatUtil.Clamp(Mathf.RoundToInt(rsum / rnum));
        }
        void calc_DriveVRProgress()
        {
            float sum = 0f; int num = 0;
            DateTime cap = getDriveVRMinRatingDate();
            foreach(var session in driveVRSessions)
            {
                if(session.Time_Created >= cap)
                {
                    OverallRatings.AddMap(session.Ratings);
                    sum += session.TotalRating;
                    num++;
                }   
            }
            if(num > 0)
            {
                float avg = sum / num;
                float w = Utility.Math.MapF(avg, Constants.RATING_MIN, Constants.RATING_MAX, Constants.RATING_MIN, Constants.PROGRESS_DRIVEVR_TARGET_RATING);

                //  weight with minimum number of sessions!!
                float sw = Mathf.Clamp01(num / Constants.DRIVEVR_MINIMUM_SESSIONS);

                Progress_DriveVR = Mathf.RoundToInt(w * sw);
                Rating_DriveVR = Mathf.RoundToInt(avg);
            }
            else
            {
                Progress_DriveVR = Constants.RATING_NONE;
            }
        }
        void calc_ExamProgress()
        {
            float sum = 0f;
            float rsum = 0f;
            if(Exam_Bronze != null)
            {
                if(Exam_Bronze.TotalRating >= Constants.EXAM_COMPLETION_RATING)
                {
                    sum += 100;
                }
                OverallRatings.AddMap(Exam_Bronze.Ratings);
                rsum += Exam_Bronze.TotalRating;
            }
            if(Exam_Silver != null)
            {
                if(Exam_Silver.TotalRating >= Constants.EXAM_COMPLETION_RATING)
                {
                    sum += 100;
                }
                OverallRatings.AddMap(Exam_Bronze.Ratings);
                rsum += Exam_Silver.TotalRating;
            }
            if(Exam_Gold != null)
            {
                if(Exam_Gold.TotalRating >= Constants.EXAM_COMPLETION_RATING)
                {
                    sum += 100;
                }
                OverallRatings.AddMap(Exam_Bronze.Ratings);
                rsum += Exam_Gold.TotalRating;
            }
            Progress_Exam = Mathf.RoundToInt(sum / 3);
            Rating_Exam += StatUtil.Clamp(Mathf.RoundToInt(rsum / 3));
        }



        //-----------------------------------------------------------------------------------------------
        //
        //  helper


        int calc_TimeSecondsUntilExamUnlock(DriveSessionStats stats)
        {
            if(stats != null)
            {
                if(stats.Ratings.Total < Constants.RATING_MAX)
                {
                    var t = Backend.TimeUtil.ServerTime;
                    var d = t - stats.Time_Created;
                    return d.Seconds;
                }
            }
            return 0;
        }
        

        int getDriveSessionIndexByUri(string synchUri)
        {
            for(int i = 0; i < driveVRSessions.Count; i++)
            {
                if(driveVRSessions[i].SynchUri == synchUri)
                {
                    return i;
                }
            }
            return -1;
        }

        DateTime getDriveVRMinRatingDate()
        {
            return StatUtil.GetDriveVRRatingBegin(meta.LastSynchTime);
        }


        void setExam(ExamLevel level, DriveSessionStats stats)
        {
            switch(level)
            {
                case ExamLevel.Bronze:  ex_bronze = stats; break;
                case ExamLevel.Silver:  ex_silver = stats; break;
                case ExamLevel.Gold:    ex_gold = stats; break;
            }
            if(stats != null)
            {
                stats.isDirty = true;
            }
        }
        void setExam(ExamLevel level, SerializedDriveSession data)
        {
            switch(level)
            {
                case ExamLevel.Bronze:  
                    if(ex_bronze != null) ex_bronze.ReadSerializedData(data);
                    else ex_bronze = new DriveSessionStats(data); 
                    ex_bronze.isDirty = true;
                    break;
                case ExamLevel.Silver:  
                    if(ex_silver != null) ex_silver.ReadSerializedData(data);
                    else ex_silver = new DriveSessionStats(data); 
                    ex_silver.isDirty = true;
                    break;
                case ExamLevel.Gold:    
                    if(ex_gold != null) ex_gold.ReadSerializedData(data);
                    else ex_gold = new DriveSessionStats(data);
                    ex_gold.isDirty = true;
                    break;
            }
        }
    }


}

