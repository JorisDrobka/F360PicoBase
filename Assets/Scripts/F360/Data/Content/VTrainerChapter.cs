using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace F360.Data.Beta
{


    /// @brief
    /// Lecture summarizes a number of assignments into a thematic block rated both individually and as a whole.
    /// Once the user completed a lecture, the next one is unlocked
    /// This is the runtime data structure the menu GUI uses to display lecture content.
    ///
    public class VTrainerChapter
    {

        public List<VTrainerData> lectures;

        //  meta
        public readonly int lectureGroupID;
        public string title;
        public string description;


        public int Count
        {
            get { return lectures.Count; }
        }

        public VTrainerData this[int index]
        {
            get {
                if(index >= 0 && index < lectures.Count) return lectures[index];
                return null;
            }
        }


        public VTrainerChapter(int groupID)
        {
            this.lectureGroupID = groupID;
            lectures = new List<VTrainerData>();
        }


        public bool isValid()
        {
            return lectures.Count > 0;
        }


        public void SetGroupInfo(VTrainerGroupData data)
        {
            //Debug.Log(lectureGroupID + " >> set GroupInfo::\n\ttitle: " + data.title + "\ndescr: " + data.description);
            this.title = data.title;
            this.description = data.description;
        }

    }



    

}