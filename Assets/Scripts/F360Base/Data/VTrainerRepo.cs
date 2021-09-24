using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace F360.Data
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

        Dictionary<int, VTrainerChapterData> chapterData;

        private VTrainerRepo()
        {
            cache = new Dictionary<int, VTrainerChapter>();
            chapterData = new Dictionary<int, VTrainerChapterData>();
        }


        //-----------------------------------------------------------------------------------------------------------------

        public void AddGroup(params VTrainerChapterData[] data)
        {
            for(int i = 0; i < data.Length; i++)
            {
                addChapterInternal(data[i]);
            }
        }

        public void Add(params VTrainerLecture[] data)
        {
            for(int i = 0; i < data.Length; i++)
            {
                addLectureInternal(data[i]);
            }
        }

        
        //-----------------------------------------------------------------------------------------------------------------


        public int ChapterCount
        {
            get { return cache.Count; }
        }

        public int LectureCount
        {
            get; private set;
        }


        public VTrainerChapter Get(int chapterID)
        {
            //Debug.Log("VTrainerRepo::Get(" + groupID + ") ? " + cache.ContainsKey(groupID));
            if(cache.ContainsKey(chapterID)) return cache[chapterID];
            return null;
        }

        public VTrainerLecture GetLecture(int lectureID)
        {
            foreach(var chapterID in cache.Keys)
            {
                for(int i = 0; i < cache[chapterID].Count; i++)
                {
                    var id = cache[chapterID].lectures[i].index;
                    if(id == lectureID)
                    {
                        return cache[chapterID].lectures[i];
                    }
                    else if(id > lectureID)
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        public VTrainerChapter Find(System.Func<VTrainerChapter, bool> predicate)
        {
            foreach(var chapterID in cache.Keys)
            {
                if(predicate(cache[chapterID]))
                {
                    return cache[chapterID];
                }
            }
            return null;
        }
        public VTrainerLecture FindLecture(System.Func<VTrainerLecture, bool> predicate)
        {
            foreach(var chapterID in cache.Keys)
            {
                foreach(var lecture in cache[chapterID].lectures)
                {
                    if(predicate(lecture)) return lecture;
                }
            }
            return null;
        }

        public IEnumerable<VTrainerLecture> QueryLectures(System.Func<VTrainerLecture, bool> predicate)
        {
            foreach(var chapterID in cache.Keys)
            {
                foreach(var lecture in cache[chapterID].lectures)
                {
                    if(predicate(lecture)) yield return lecture;
                }
            }
        }

        

        //-----------------------------------------------------------------------------------------------------------------

        //  internal

        bool addChapterInternal(VTrainerChapterData chapter)
        {
            if(chapter != null && chapter.isValid())
            {
                if(!chapterData.ContainsKey(chapter.index))
                {
                    //Debug.Log("VTrainerRepo:: " + RichText.darkGreen("added lecture group[" + group.index.ToString() + "] data!"));
                    chapterData.Add(chapter.index, chapter);
                    foreach(var lec in cache.Values)
                    {
                        if(lec.lectureGroupID == chapter.index)
                        {
                            lec.SetGroupInfo(chapter);
                        }
                    }
                    return true;
                }
            }
            else
            {
                Debug.LogWarning("VTrainerRepo:: error adding lecturegroup.\nreason: " + (chapter == null ? "is null" : (" data invalid!\n" + chapter.Readable(true))));
            }
            return false;
        }

        bool addLectureInternal(VTrainerLecture lecture)
        {
//            Debug.Log(RichText.emph("VTrainerRepo:: add") + " [" + lecture.group + "/" + lecture.index + "]");
            if(lecture != null && lecture.isValid())
            {
                if(!cache.ContainsKey(lecture.group))
                {
                    var lec = new VTrainerChapter(lecture.group);
                    cache.Add(lecture.group, lec);
                    if(chapterData.ContainsKey(lecture.group))
                    {
                        lec.SetGroupInfo(chapterData[lecture.group]);
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



