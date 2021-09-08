using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Utility.Config;
using VRIntegration.Video;
using F360.Users.Stats;
using F360.Data.IO.Video;

namespace F360.Data
{


    /// @brief
    /// additional data for each video sequence
    ///
    public class VideoMetaData : IDataTarget<Context>
    {
        public int videoID { get; internal set; } = -1;
        public int lectureID { get; internal set; } = -1;
        public MediaData media { get; internal set; }
        public DriveLocation location { get; internal set; }

        public VideoClip[] clips { get; internal set; }
        public int ClipCount { get { return clips != null ? clips.Length : 0; } }

        public DriverIntentData intents { get; internal set; }
        public LookData looks { get; internal set; }
        public HazardPerceptionData hazards { get; internal set; }

        //public VideoSceneData sceneData { get; set; }

        public int durationMs { get; internal set; } = -1;

        public int difficulty { get; internal set; } = -1;

        public string geoCoordinates { get; internal set; }


        public VideoMetaData()
        {
            
        }

        #if UNITY_EDITOR
        public void Clear()
        {
            videoID = -1;
            lectureID = -1;
            media = new MediaData();
            location = DriveLocation.Undefined;
            clips = null;
            intents = null;
            looks = null;
            hazards = null;
            durationMs = -1;
            difficulty = -1;
            geoCoordinates = "";
        }
        #endif

        public bool isValid()
        {
            return videoID >= 0;
        }

        //  getter

        public int LookCount { get { return looks != null ? looks.EventCount : 0; } }
        public int HazardCount { get { return hazards != null ? hazards.EventCount : 0; } }
        public int IntentCount { get { return intents != null ? intents.Count : 0; } }

        public bool hasIntents() { return intents != null && intents.Count > 0; }
        public bool hasLookData() { return looks != null && looks.EventCount > 0; }
        public bool hasHazards() { return hazards != null && hazards.EventCount > 0; }
        //public bool hasSceneData() { return sceneData != null && sceneData.GetElementCount() > 0; }



        public bool hasClip(int clipID)
        {
            return clips != null && clipID >= 0 && clipID < clips.Length;
        }

        public bool hasClip(string clipname)
        {
            if(clips != null) {
                clipname = clipname.ToLower();
                for(int i = 0; i < clips.Length; i++) {
                    if(clips[i].name.ToLower() == clipname) {
                        return true;
                    }
                }
            }
            return false;
        }


        public bool hasClip(System.Func<VideoClip, bool> predicate)
        {
            if(clips != null)
            {
                for(int i = 0; i < clips.Length; i++)
                    if(predicate(clips[i]))
                        return true;
            }
            return false;
        }


        public VideoClip GetClip(int clipID)
        {
            if(clips != null && clipID >= 0 && clipID < clips.Length) {
                return clips[clipID];
            }
            return null;
        }

        public VideoClip GetClip(string clipname)
        {
            if(clips != null) {
                clipname = clipname.ToLower();
                for(int i = 0; i < clips.Length; i++) {
                    if(clips[i].name == clipname) return clips[i];
                }
            }
            return null;
        }

        public VideoClip FindClip(System.Func<VideoClip, bool> predicate)
        {
            if(clips != null)
            {
                for(int i = 0; i < clips.Length; i++)
                    if(predicate(clips[i]))
                        return clips[i];
            }   
            return null;
        }

        public IEnumerable<VideoClip> QueryClips(System.Func<VideoClip, bool> predicate)
        {
            if(clips != null)
            {
                for(int i = 0; i < clips.Length; i++)
                {
                    if(predicate(clips[i]))
                    {
                        yield return clips[i];
                    }
                }
            }
        }

        public int IndexOfClip(VideoClip clip)
        {
            if(clips != null)
            {
                return System.Array.IndexOf(clips, clip);
            }
            return -1;
        }


        public string Readable( bool printBA=false, 
                                bool printHZ=false, 
                                bool printIntents=false)
        {
            var b = new System.Text.StringBuilder("[VideoMeta::" + RichText.emph(F360FileSystem.FormatVideoID(videoID)) + "]\n");
            b.Append("\nlecture-ID:\t" + lectureID.ToString());
            b.Append("\nlocation:\t" + location.Readable());
            b.Append("\nduration:\t" + durationMs.ToString());// F360FileSystem.FormatTimeSeconds(durationMs));
            b.Append("\ndifficulty:\t" + difficulty.ToString());
            b.Append("\nlooks:\t" + LookCount.ToString());
            if(printBA)
            {
                for(int i = 0; i < LookCount; i++) b.Append("\n\t" + looks.GetEventAt(i).ToString());
                b.Append("\n");
            }
            b.Append("\nhazards:\t" + HazardCount.ToString());
            if(printHZ)
            {
                for(int i = 0; i < HazardCount; i++) b.Append("\n\t" + hazards.GetEventAt(i).ReadableList());
                b.Append("\n");
            }
            b.Append("\nintents:\t" + IntentCount.ToString());
            if(printIntents)
            {
                for(int i = 0; i < IntentCount; i++) {
                    var intent = intents.Get(i);
                    b.Append("\n\t" + intent.type.ToString() + " " + intent.timing.end.ToString());
                }
                b.Append("\n");
            }
            if(clips != null && clips.Length > 0)
            {     
                for(int i = 0; i < clips.Length; i++)
                {
                    b.Append("\n");
                    b.Append(clips[i].Readable());
                }
            }
            return b.ToString();
        }



        int IDataTarget.Index { get { return videoID; } }

        string IDataTarget.Readable(bool deep) { return Readable(deep, deep, deep); }


        void IDataTarget<Context>.PushContextData(Context context, IContextData[] data)
        {
            if(context == Context.Clip)
            {
                List<VideoClip> buffer = new List<VideoClip>();
                foreach(var d in data)
                {
                    var vc = d as VideoClip;
                    if(vc != null)
                    {
                        buffer.Add(vc);
                    }
                }
                if(buffer.Count != data.Length)
                {
                    Debug.LogWarning("VideoMetaData:: error while pushing context data<" + context + ">\n\tbuffer: " + buffer.Count + "\n\tpushed: " + data.Length);
                }
                this.clips = buffer.ToArray();
            }

        }


    }


    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// clip or full video with descriptive metadata
    ///
    public class VideoClip : IContextData
    {
        public VideoMetaData parentData { get; private set; }

        public int clipID;
        public string name;
        public MediaData media;
        public RangeInt timing;
        
        public string[] tags;
        public Category category;
        public string description;


        public string signCode;
        public string rowCode;
        public int questionID;

        public int VideoID { get { return parentData.videoID; } }
        public int DurationMs { get { return timing.length; } }
        public bool isFullVideo { get { return media.durationMs == timing.length; } }


        public VideoClip() {}
        public VideoClip(VideoMetaData meta)
        {
            this.parentData = meta;
        }
        

        string IContextData.Context { get { return Context.Clip.ToString(); } }

        IDataTarget IContextData.Parent 
        { 
            get { return parentData; }
            set {
                this.parentData = value as VideoMetaData;
            } 
        }

        bool IContextData.isClosed { get; set; }


        public string Readable()
        {
            var b = new System.Text.StringBuilder();
            if(!string.IsNullOrEmpty(name)) b.Append("[clip:" + name + "]"); 
            else b.Append("[clip]");
            b.Append("\n\ttiming:\t" + F360FileSystem.FormatTimeRangeSeconds(timing));
            b.Append("\n\tcategory:\t" + (category != null ? category.GroupLabel : "none"));
            if(tags != null && tags.Length > 0)
            {
                b.Append("\n\ttags:");
                for(int i = 0; i < tags.Length; i++) b.Append("\n\t\t" + tags[i]);
            }
            if(!string.IsNullOrEmpty(description))
                b.Append("\n\tdescription: " + description);
            if(!string.IsNullOrEmpty(signCode))
                b.Append("\n\tsign:\t" + signCode);
            if(!string.IsNullOrEmpty(rowCode))
                b.Append("\n\trow:\t" + rowCode);

            return b.ToString();
        }

    }   


}