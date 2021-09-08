using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Utility.Config;


namespace F360.Data.Beta
{

    public class VTrainerGroupData
    {
        public int index;
        public string title;
        public string description;

        public bool isValid() { return index > 0; }
        public string Readable(bool full)
        {
            var b = new System.Text.StringBuilder("[VTrainerGroup " + index + "] : ");
            b.Append(title);
            if(full) b.Append("\n\tdescription: " + description);
            return b.ToString();
        }
    }





    /// @brief
    /// VirtualTrainer lectures are organized in groups of 2, 3, 4, 5 or 6 assignments,
    /// which are rated individually and as a group. Once the user has completed each assignment
    /// successfully, the next group is unlocked.
    ///
    public class VTrainerData
    {
        public int group;           ///< index of group
        public int index;           ///< index of task within lecture

        /// @brief
        /// unique ID to identify/fetch a single task
        ///
        public int lectureID { get { return group*100 + index; } }

        public string title;
        public int difficulty;


//        public SequenceData[] sequences;

    

        public bool isValid()
        {
            return true;
        }



        public string Readable(bool deep=false)
        {
            var b = new System.Text.StringBuilder("[LectureData ");
            b.Append(group);
            b.Append("/");
            b.Append(index);
            b.Append("]");
            b.Append("\n\ttitle: " + title);
            //if(sequences != null)
            //{
            //    b.Append("\n\tsequences: " + sequences.Length.ToString());
            //    if(deep)
            //    {
            //        foreach(var s in sequences)
            //        {
            //            b.Append("\n" + s.Readable(deep));
            //            b.Append("\n");
            //        }
            //    }
            //}
            //else
            //{
                b.Append("\n\tsequences: " + RichText.darkRed("Missing"));
            //}
            return b.ToString();
        }
    }


}