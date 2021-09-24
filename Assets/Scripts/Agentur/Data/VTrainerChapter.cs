using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace F360.Data
{


    /// @brief
    /// Lecture summarizes a number of assignments into a thematic block rated both individually and as a whole.
    /// Once the user completed a lecture, the next one is unlocked
    /// This is the runtime data structure the menu GUI uses to display lecture content.
    ///
    public class VTrainerChapter
    {

        public List<VTrainerLecture> lectures;

        //  meta
        public readonly int lectureGroupID;
        public string title;
        public string description;
        public Texture2D previewImg;


        public int Count
        {
            get { return lectures.Count; }
        }

        /// @returns lecture by internal position
        ///
        public VTrainerLecture this[int index]
        {
            get {
                if(index >= 0 && index < lectures.Count) return lectures[index];
                return null;
            }
        }


        public VTrainerChapter(int groupID)
        {
            this.lectureGroupID = groupID;
            lectures = new List<VTrainerLecture>();
        }


        public bool isValid()
        {
            return lectures.Count > 0;
        }

        public bool ContainsLectureID(int lectureID)
        {
            return getLectureByID(lectureID) != -1;
        }

        public VTrainerLecture GetLectureByID(int lectureID)
        {
            int index = getLectureByID(lectureID);
            if(index != -1)
            {
                return lectures[index];
            }
            return null;
        }

        public int GetLecturePositionIndex(int lectureID)
        {
            return getLectureByID(lectureID);
        }

        public void SetGroupInfo(VTrainerChapterData data)
        {
            //Debug.Log(lectureGroupID + " >> set GroupInfo::\n\ttitle: " + data.title + "\ndescr: " + data.description);
            this.title = data.title;
            this.description = data.description;
        }




        //  util

        int getLectureByID(int lectureID)
        {
            for(int i = 0; i < lectures.Count; i++)
            {
                if(lectures[i].lectureID == lectureID) return i;
            }
            return -1;
        }

    }



    

}