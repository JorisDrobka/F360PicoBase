using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace F360.Users.Stats
{


    public interface ISynchable
    {
        string SynchUri { get; }
        DateTime Time_Created { get; }
        ISynchedTerm GetSynchTerm();
    }


    public interface ISynchedTerm
    {
        string SynchUri { get; }
        DateTime GetTimeCreated();
        string ToYaml();
    }



    /// @brief
    /// Defines unique synch ids to identify saved ratings.
    ///
    public static class SynchURI
    {

        public const string NO_SYNCH = "nosynch";

        public const string USER_META = "usermeta";
        public const string EXAM_BRONZE = PREFIX_EXAM + "bronze";
        public const string EXAM_SILVER = PREFIX_EXAM + "silver";
        public const string EXAM_GOLD = PREFIX_EXAM + "gold";



        const string PREFIX_VTRAINER = "vtr_";
        const string PREFIX_DRIVEVR = "dvr_";
        const string PREFIX_EXAM = "ex_";

        

        public static string CreateVTrainerChapter(int chapterID)
        {
            return PREFIX_VTRAINER + chapterID.ToString();
        }

        public static string CreateDriveVRSession(int videoID, int attempt)
        {
            return PREFIX_DRIVEVR + F360FileSystem.FormatVideoID(videoID) + "-" + attempt.ToString();
        }

        public static string CreateExam(ExamLevel level)
        {
            switch(level)
            {
                case ExamLevel.Bronze:  return EXAM_BRONZE;
                case ExamLevel.Silver:  return EXAM_SILVER;
                case ExamLevel.Gold:    return EXAM_GOLD;
                default:                return "Undefined";
            }
        }

        public static TrainingContext GetContext(string uri)
        {
            if(uri.StartsWith(PREFIX_VTRAINER))
            {
                return TrainingContext.VRTrainer;
            }
            else if(uri.StartsWith(PREFIX_DRIVEVR))
            {
                return TrainingContext.DriveVR;
            }
            else if(uri.StartsWith(PREFIX_EXAM))
            {
                return TrainingContext.Exam;
            }
            else
            {
                return TrainingContext.None;
            }
        }


    }


}


