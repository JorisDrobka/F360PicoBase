using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using F360.Data;




/* ######### ILUME ##########
    
    Muss serialisiert & synchronisiert werden
    Nach Deserialisierung bitte OnAfterDeserialize() aufrufen.

*/

namespace F360.Users.Stats
{

    /// @brief
    /// saved ratings of a VTrainer Chapter
    ///
    /// after deserialization, runtime data is loaded for querying.
    ///
    ///
    public class VTrainerStats : ISynchable
    {


        //-----------------------------------------------------------------------------------------------
        //
        //  serialized data


        /// ###   SERIALIZE & SYNCH   ### 

        private int c_id;
        private DateTime create_T;
        internal int[] lectureRatings { get; private set; }


        public void ReadSerializedData(SerializedVTrainerChapter data)
        {
            c_id = data.chapter;
            StatSerializationUtil.TryParseTime(data.created, ref create_T);
            lectureRatings = StatSerializationUtil.ConvertToIntArray(data.ratings);
        }

        ISynchedTerm ISynchable.GetSynchTerm()
        {
            return new SerializedVTrainerChapter(this);
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  runtime data

        public string SynchUri { get { return SynchURI.CreateVTrainerChapter(ChapterID); } }

        private VTrainerChapter dataset;


        //-----------------------------------------------------------------------------------------------
        //
        //  constructor

        public VTrainerStats(SerializedVTrainerChapter data)
        {
            ReadSerializedData(data);
            OnAfterDeserialize();
        }

        public VTrainerStats(VTrainerChapter data, int[] ratings)
        {
            this.ChapterID = data.lectureGroupID;
            this.Time_Created = Backend.TimeUtil.ServerTime;
            this.dataset = data;
            this.lectureRatings = ratings;
            this.isCompleted = check_completed();
            calc_TotalRating();
        }

        public void UpdateFromServer(VTrainerStats other)
        {
            if(other.c_id == c_id)
            {
                create_T = other.create_T;
                lectureRatings = new int[other.lectureRatings.Length];
                Array.Copy(other.lectureRatings, lectureRatings, lectureRatings.Length);
            }
        }   


        //-----------------------------------------------------------------------------------------------
        //
        //  interface

        public int ChapterID 
        { 
            get { return c_id; } 
            private set { c_id = value; } 
        }
        public DateTime Time_Created 
        { 
            get { return create_T; }
            private set { create_T = value; } 
        }

        public int TotalRating { get; private set; } = Constants.RATING_NONE;

        public bool isCompleted { get; private set; }

        public int LectureCount { get { return lectureRatings.Length; } }

        public int this[int lectureIndex]
        {
            get {
                return GetRatingByLectureID(lectureIndex);
            }
        }

        public int GetRatingByLectureID(int lectureIndex)
        {
            if(dataset == null)
            {
                Debug.LogWarning("VTrainerStats:: was not properly deserialized.");
            }
            else if(dataset.ContainsLectureID(lectureIndex))
            {
                int position = dataset.GetLecturePositionIndex(lectureIndex);
                return lectureRatings[position];
            }
            Debug.LogWarning("VTrainerStats:: Chapter[" + ChapterID + "] does not contain lectureIndex=" + lectureIndex);
            return Constants.RATING_NONE;
        }

        public int GetRatingByLecturePosition(int position)
        {
            if(dataset == null)
            {
                Debug.LogWarning("VTrainerStats:: was not properly deserialized.");
            }
            else if(position >= 0 && position < lectureRatings.Length)
            {
                return lectureRatings[position];
            }
            return Constants.RATING_NONE;
        }


        //-----------------------------------------------------------------------------------------------

        public void OnAfterDeserialize()
        {
            if(VTrainerRepo.Current == null)
            {
                Debug.LogError("No VTrainerRepo found while unpacking VTrainerChapter[" + ChapterID + "]!");
            }
            else
            {
                var data = VTrainerRepo.Current.Get(ChapterID);
                if(data != null)
                {
                    if(data.Count != lectureRatings.Length)
                    {
                        Debug.LogError("VTrainerChapter count mismatch! Chapter[" + ChapterID + "]" + "\nlectures: " + data.Count + " Saved Data: " + lectureRatings.Length);
                    }
                    else
                    {
                        this.dataset = data;
                        this.isCompleted = check_completed();
                        calc_TotalRating();
                    }
                }
                else
                {
                    Debug.LogError("No VTrainerChapterData found while unpacking Chapter[" + ChapterID + "]");
                }
            }
        }

        void calc_TotalRating()
        {
            float sum = 0f; int num = 0;
            for(int i = 0; i < lectureRatings.Length; i++)
            {
                if(lectureRatings[i] >= Constants.RATING_MIN)
                {
                    sum += lectureRatings[i];
                    num++;
                }
            }
            if(num > 0)
            {
                this.TotalRating = StatUtil.Clamp(Mathf.RoundToInt(sum / num));
            }
            else
            {
                this.TotalRating = Constants.RATING_NONE;
            }
        }

        bool check_completed()
        {
            for(int i = 0; i < lectureRatings.Length; i++)
            {
                if(lectureRatings[i] < Constants.LECTURE_COMPLETION_RATING)
                {
                    return false;
                }
            }
            return true;
        }

    }



}

