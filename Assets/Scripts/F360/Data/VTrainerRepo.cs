using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace F360.Data.Beta
{

    /// @brief
    /// Access to all Virtual Trainer lectures.
    /// A single lecture contains a number of sequences and rated tasks.
    /// Multiple lectures are arranged in groups & displayed together in the UI
    ///
    ///
    public class VTrainerRepo
    {
        
        public static VTrainerRepo Current
        {
            get {
                if(_instance == null) _instance = new VTrainerRepo();
                return _instance;
            }
        }
        static VTrainerRepo _instance;


        Dictionary<int, VTrainerChapter> cache;

        Dictionary<int, VTrainerGroupData> groups;

        private VTrainerRepo()
        {
            cache = new Dictionary<int, VTrainerChapter>();
            groups = new Dictionary<int, VTrainerGroupData>();
        }


        //-----------------------------------------------------------------------------------------------------------------

        public void AddGroup(params VTrainerGroupData[] data)
        {
            for(int i = 0; i < data.Length; i++)
            {
                addGroupInternal(data[i]);
            }
        }

        public void Add(params VTrainerData[] data)
        {
            for(int i = 0; i < data.Length; i++)
            {
                addInternal(data[i]);
            }
        }

        
        //-----------------------------------------------------------------------------------------------------------------


        public int GroupCount
        {
            get { return cache.Count; }
        }

        public int LectureCount
        {
            get; private set;
        }


        public VTrainerChapter Get(int groupID)
        {
            //Debug.Log("VTrainerRepo::Get(" + groupID + ") ? " + cache.ContainsKey(groupID));
            if(cache.ContainsKey(groupID)) return cache[groupID];
            return null;
        }

        public VTrainerData GetLecture(int index)
        {
            foreach(var groupID in cache.Keys)
            {
                for(int i = 0; i < cache[groupID].Count; i++)
                {
                    var id = cache[groupID].lectures[i].index;
                    if(id == index)
                    {
                        return cache[groupID].lectures[i];
                    }
                    else if(id > index)
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        public VTrainerChapter Find(System.Func<VTrainerChapter, bool> predicate)
        {
            foreach(var groupID in cache.Keys)
            {
                if(predicate(cache[groupID]))
                {
                    return cache[groupID];
                }
            }
            return null;
        }
        public VTrainerData FindLecture(System.Func<VTrainerData, bool> predicate)
        {
            foreach(var groupID in cache.Keys)
            {
                foreach(var lecture in cache[groupID].lectures)
                {
                    if(predicate(lecture)) return lecture;
                }
            }
            return null;
        }

        public IEnumerable<VTrainerData> QueryLectures(System.Func<VTrainerData, bool> predicate)
        {
            foreach(var groupID in cache.Keys)
            {
                foreach(var lecture in cache[groupID].lectures)
                {
                    if(predicate(lecture)) yield return lecture;
                }
            }
        }

        

        //-----------------------------------------------------------------------------------------------------------------

        //  internal

        bool addGroupInternal(VTrainerGroupData group)
        {
            if(group != null && group.isValid())
            {
                if(!groups.ContainsKey(group.index))
                {
                    //Debug.Log("VTrainerRepo:: " + RichText.darkGreen("added lecture group[" + group.index.ToString() + "] data!"));
                    groups.Add(group.index, group);
                    foreach(var lec in cache.Values)
                    {
                        if(lec.lectureGroupID == group.index)
                        {
                            lec.SetGroupInfo(group);
                        }
                    }
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("VTrainerRepo:: error adding lecturegroup.\nreason: " + (group == null ? "is null" : (" data invalid!\n" + group.Readable(true))));
            }
            return false;
        }

        bool addInternal(VTrainerData lecture)
        {
//            Debug.Log(RichText.emph("VTrainerRepo:: add") + " [" + lecture.group + "/" + lecture.index + "]");
            if(lecture != null && lecture.isValid())
            {
                if(!cache.ContainsKey(lecture.group))
                {
                    var lec = new VTrainerChapter(lecture.group);
                    cache.Add(lecture.group, lec);
                    if(groups.ContainsKey(lecture.group))
                    {
                        lec.SetGroupInfo(groups[lecture.group]);
                    }
                }
                if(!cache[lecture.group].lectures.Contains(lecture))
                {
                    cache[lecture.group].lectures.Add(lecture);
                    LectureCount++;
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("VTrainerRepo:: error adding lecture.\nreason: " + (lecture == null ? "is null" : (" data invalid!\n" + lecture.Readable(true))));
            }
            return false;
        }



    }


}



