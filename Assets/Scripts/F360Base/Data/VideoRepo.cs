using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


using VRIntegration.Video;


namespace F360.Data
{

    public enum VideoType
    {
        Undefined=0,
        VR,
        Tutorial,
        MenuBG
    }





    /// @brief
    /// Provides access to all local videos, including drive sequences,
    /// menu dome backgrounds and tutorials.
    ///
    /// The repo is set up to handle different types of videos at different locations,
    /// and ensures all videos have their proper data (like a unique internal ID)
    ///
    ///
    public class VideoRepo
    {

        public const string VIDEO_EXTENSION = ".mp4";


        const int ID_TUTORIAL_START = 1000;
        const int ID_MENU_START = 10000;
        
        const int MIN_FILE_SIZE = 1000000;      // 1MB

        static int sequencer_menuID = 0;


        
        public static VideoRepo Current 
        {   
            get {
                if(_instance == null) _instance = new VideoRepo(); 
                return _instance; 
            }
        }

        public VideoRepo()
        {
            _instance = this;
            paths = new Dictionary<VideoType, string>();
            cache = new Dictionary<int, CachedVideo>();
            metadata = new Dictionary<int, VideoMetaData>();
            namePointerCache = new Dictionary<string, int>();
        }

        static VideoRepo _instance;
        Dictionary<int, CachedVideo> cache;
        Dictionary<int, VideoMetaData> metadata;
        Dictionary<string, int> namePointerCache;
        Dictionary<VideoType, string> paths;
        
        //-----------------------------------------------------------------------------------------------------------------

        //  getter

        public int Count { get { return cache.Count; } }


        public bool CheckAccess(string video)
        {
            return ExistsLocally(video);
        }

        /// @returns wether given video filepath/filename exists in repo.
        ///
        public bool ExistsLocally(string video)
        {
            video = F360FileSystem.RemoveFolderFromFilePath(video);
            video = F360FileSystem.RemoveFileExtension(video);
            return namePointerCache.ContainsKey(video) && cache.ContainsKey(namePointerCache[video]);
        }   

        /// @returns internal video id of given video filepath/filename, or -1
        ///
        public int GetVideoID(string video)
        {
            video = F360FileSystem.RemoveFolderFromFilePath(video);
            video = F360FileSystem.RemoveFileExtension(video);
            if(namePointerCache.ContainsKey(video)) return namePointerCache[video];
            return -1;
        }

        public int GetVideoIDFromClipname(string clipname)
        {
            foreach (var meta in metadata.Values)
            {
                if(meta.hasClip(clipname)) return meta.videoID;
            }
            return -1;
        }

        /// @brief
        /// access a video via internal ID 
        ///
        /// @returns true if videodata exists in repo
        ///
        public bool Get(int videoID, out MediaData videodata)
        {
            if(cache.ContainsKey(videoID))
            {
                videodata = cache[videoID].Media;
                return true;
            }
            videodata = new MediaData();
            return false;
        }   

        /// @brief
        /// access metadata of a video via internal ID
        ///
        /// @returns true if metadata exists in repo
        ///
        public bool GetMetadata(int videoID, out VideoMetaData metadata)
        {
            if(this.metadata.ContainsKey(videoID))
            {
                metadata = this.metadata[videoID];
                return true;
            }
            metadata = null;
            return false;
        }

        /// @brief
        /// access a video by filepath/filename
        ///
        /// @returns true if videodata exists in repo
        ///
        public bool Get(string video, out MediaData videodata)
        {
            video = F360FileSystem.RemoveFolderFromFilePath(video);
            video = F360FileSystem.RemoveFileExtension(video);
            if(namePointerCache.ContainsKey(video) && cache.ContainsKey(namePointerCache[video]))
            {
                videodata = cache[namePointerCache[video]].Media;
                return true;
            }
            else
            {
                videodata = new MediaData();
                return false;
            }
        }

        /// @brief
        /// search for a local video of a certain type
        ///
        /// @returns true if videodata exists in repo
        ///
        public bool Find(VideoType type, System.Func<CachedVideo, bool> predicate, out MediaData media)
        {
            foreach(var v in cache.Values)
            {
                if(v.Type == type && predicate(v))
                {
                    media = v.Media;
                    return true;
                }
            }
            media = default(MediaData);
            return false;
        }

        /// @returns all local videos of given type, with an optional filter function
        ///
        public IEnumerable<CachedVideo> Query(VideoType type, System.Func<CachedVideo, bool> predicate=null)
        {
            foreach(var v in cache.Values)
            {
                if(v.Type == type && (predicate == null || predicate(v)))
                {
                    yield return v;
                }
            }
        }

        /// @returns type of given video path/name
        ///
        public VideoType GetTypeOfVideo(string video)
        {
            var path = F360FileSystem.RemoveFileFromPath(video);
            if(!string.IsNullOrEmpty(path))
            {
                //  check cached paths
                foreach(var p in paths)
                {
                    if(p.Value.Contains(path) || path.Contains(p.Value))
                    {
                        return p.Key;
                    }
                }
            }
            video = F360FileSystem.RemoveFolderFromFilePath(video);
            video = F360FileSystem.RemoveFileExtension(video);
            if(namePointerCache.ContainsKey(video))
            {
                return cache[namePointerCache[video]].Type;
            }
            else
            {
                return VideoType.Undefined;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  setter

        public enum AutoLoad
        {
            Off,
            Folder,
            Hierarchy
        }


        /// @brief
        /// Add a local video folder to this repo. This will happen automatically when adding a new video,
        /// but it is advised to set paths in advance for more control (in case there are subfolders)
        ///
        /// @param type The category of videos contained in this folder
        /// @param path The folder path
        /// @param autoload Automatically load all files in folder to repo
        ///
        public bool AddVideoFolder(VideoType type, string folder, AutoLoad auto=AutoLoad.Off)
        {
            return __addVideoFolderInternal(type, folder, auto);
        }


        /// @brief
        /// add a VR training video to the repo
        ///
        public bool AddLocalTrainingVideo(string filename, string filepath, int durationMs=0)
        {
            return __addTrainingVideoInternal(filename, filepath, durationMs);
        }

        /// @brief
        /// add a VR training video to the repo
        ///
        public bool AddLocalTrainingVideo(VideoMetaData meta)
        {
            return __addTrainingVideoInternal(meta.media.fileName, meta.media.videoPath, data: meta);
        }


        /// @brief
        /// adds VR metadata to an existing video
        ///
        public bool AddVideoMetadata(int videoID, VideoMetaData data)
        {
            if(data.videoID != videoID)
            {
                Debug.LogWarning("VideoRepo:: malfitting video Id given! " + videoID + " != " + data.videoID);
                return false;
            }
            else if(!cache.ContainsKey(videoID))
            {
                Debug.LogWarning("VideoRepo:: tried adding video metadata, but no video with id=[" + videoID + "] could not be found!");
                return false;
            }
            else 
            {
                if(!metadata.ContainsKey(videoID)) metadata.Add(videoID, data);
                else metadata[videoID] = data;
                return true;
            }
        }

        /// @brief
        /// add a VR training video to the repo
        ///
        public bool AddLocalTrainingVideo(int videoID, MediaData video)
        {
            return __addLocalVideoInternal(videoID, VideoType.VR, video);
        }


        /// @brief
        /// add a video of given type to the repo
        ///
        public bool AddLocalVideo(VideoType type, MediaData video)
        {
            return __addLocalVideoInternal(type, video);
        }

        /// @brief
        /// add a video at given path without knowing the type.
        /// caution: only works if video folders are set up properly
        ///
        public bool AddLocalVideo(string filepath)
        {
            return __tryAddLocalVideo(filepath);
        }


        /// @brief
        /// loads all videos from given folder if previously added via AddVideoPath()
        ///
        /// @returns number of video loaded or -1 if an error occured
        ///
        public int AutoloadFromFolder(string folder, bool includeSubfolders=false)
        {
            return __autoLoadInternal(folder, includeSubfolders);
        }

        

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  INTERNAL
        //
        //-----------------------------------------------------------------------------------------------------------------


        //  util

        bool isValidFolder(string folder)
        {
            foreach(var p in paths)
            {
                if(p.Value.Contains(folder) || folder.Contains(p.Value))
                {
                    return true;
                }
            }
            return false;
        }

        bool isValidFileName(string video)
        {
            video = F360FileSystem.RemoveFolderFromFilePath(video);
            video = F360FileSystem.RemoveFileExtension(video);
            return namePointerCache.ContainsKey(video) && cache.ContainsKey(namePointerCache[video]);
        }       


        /// @returns videoID from filename/path or -1 if there was an error while parsing
        ///
        public static int TryParseVideoIndex(string videoFile)
        {
            var name = F360FileSystem.RemoveFolderFromFilePath(videoFile);
            name = F360FileSystem.RemoveFileExtension(name);
            var splt = name.Split('_');
            var index = -1;
            if(splt[0].StartsWith("00"))
            {
                splt[0] = splt[0].Substring(2);
            }
            else if(splt[0].StartsWith("0"))
            {
                splt[0] = splt[0].Substring(1);
            }
            if(System.Int32.TryParse(splt[0], out index))
            {
                return index;
            }
            return -1;
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  setter

        bool __addVideoFolderInternal(VideoType type, string folder, AutoLoad auto)
        {
            try
            {
                if(!F360FileSystem.isFolder(folder))
                {
                    Debug.LogWarning("VideoRepo:: added path is not a folder!\n\tpath: " + RichText.italic(folder));
                    return false;
                }
                else if(!Directory.Exists(folder))
                {
                    Debug.LogWarning("VideoRepo:: could not add path, directory does not exist: " + RichText.italic(folder));
                    return false;
                }
                if(!paths.ContainsKey(type))
                {
                    paths.Add(type, folder);
                }
                else
                {
                    if(paths[type].Contains(folder))
                    {
                        //  take shorter path
                        paths[type] = folder;
                    }
                    else if(!folder.Contains(paths[type]))
                    {
                        Debug.LogWarning("VideoRepo:: failed trying to relocate videofolder of type={" + type + "}"
                                        + "\n\tcurrent: " + RichText.italic(paths[type])
                                        + "\n\trelocate: " + RichText.italic(folder));
                        return false;
                    }
                }

                if(auto != AutoLoad.Off)
                {
                    AutoloadFromFolder(folder, auto == AutoLoad.Hierarchy);
                }
                return true;
            }
            catch(IOException ex)
            {
                Debug.LogWarning("VideoRepo:: could not add path: " + RichText.italic(folder) + "\n" + ex.Message);
                return false;
            }
        }


        bool __tryAddLocalVideo(string filepath)
        {
            var type = GetTypeOfVideo(filepath);
            if(type != VideoType.Undefined)
            {
                var media = MediaData.CreateVideo(filepath);
                switch(type)
                {
                    case VideoType.VR:  return AddLocalTrainingVideo(media.fileName, filepath);
                    default:            return AddLocalVideo(type, media);    
                }
            }
            return false;
        }

        bool __addTrainingVideoInternal(string filename, string filepath, int durationMs=0, VideoMetaData data=null)
        {
            MediaData media;
            if(data != null)
            {
                media = data.media;
                if(media.fileName != filename || media.videoPath != filepath)
                {
                    Debug.LogWarning("VideoRepo:: mismatching paths while adding training video via metadata!" 
                                        + "\n\t[Media]name: " + RichText.italic(filename) + " @path: " + RichText.italic(filepath)
                                        + "\n\t[Meta]name: " + RichText.italic(media.fileName) + " @path: " + RichText.italic(media.videoPath));
                    return false;
                }
                else
                {
                    if(__addLocalVideoInternal(data.videoID, VideoType.VR, media))
                    {
                        return AddVideoMetadata(data.videoID, data);
                    }
                    return false;
                }
            }
            else
            {
                filename = F360FileSystem.RemoveFolderFromFilePath(filename);
                filename = F360FileSystem.RemoveFileExtension(filename);
                
                int videoID;
                if(F360FileSystem.ParseVideoID(filename, out videoID))
                {
                    media = new MediaData();
                    media.videoPath = filepath;
                    media.fileName = filename;
                    media.durationMs = durationMs;
                    return __addLocalVideoInternal(videoID, VideoType.VR, media);
                }
                else
                {
                    Debug.LogWarning("VideoRepo:: could not parse VideoID [" + filename + "] of training video at path: " + RichText.italic(filepath));
                    return false;
                }
            }            
        }


        bool __addLocalVideoInternal(VideoType type, MediaData video)
        {
            if(type == VideoType.VR)
            {
                Debug.LogWarning("VideoRepo:: Use AddLocalTrainingVideo when adding videos of type {" + type + "}");
                return AddLocalTrainingVideo(video.fileName, video.videoPath, video.durationMs);
            }
            else
            {
                int videoID = -1;
                switch(type)
                {
                    case VideoType.Tutorial:    
                        if(F360FileSystem.ParseVideoID(video.fileName, out videoID))
                        {
                            videoID += ID_TUTORIAL_START;
                        }
                        break;

                    case VideoType.MenuBG:      
                        videoID = ID_MENU_START + (++sequencer_menuID);
                        break;
                }

                if(videoID != -1)
                {
                    return __addLocalVideoInternal(videoID, type, video);
                }
                else
                {
                    Debug.LogWarning("Could not determine videoID of video{" + type + "} at path: " + RichText.italic(video.videoPath));
                    return false;
                }
            }
        }

        bool __addLocalVideoInternal(int videoID, VideoType type, MediaData media, bool addPath=true)
        {
            if(!media.Exists())
            {
                Debug.LogWarning("VideoRepo:: tried to add video[" + type + "/" + videoID + "] without file at path: " + RichText.italic(media.videoPath));
                return false;
            }
            else if(!media.isVideo())
            {
                Debug.LogWarning("VideoRepo:: tried to add non-video[" + type + "/" + videoID + "] at paths:\n\t" + RichText.italic(media.videoPath) 
                                + "\n\t" + RichText.italic(media.imagePath) );
                return false;
            }


            CachedVideo c = new CachedVideo(videoID, type, media);
            if(!cache.ContainsKey(videoID))
            {
                cache.Add(videoID, c);
            }
            else
            {   
                cache[videoID] = c;
            }

            if(!namePointerCache.ContainsKey(media.fileName))
            {
                namePointerCache.Add(media.fileName, videoID);
            }
            else
            {
                namePointerCache[media.fileName] = videoID;
            }
            
            if(addPath && !paths.ContainsKey(type))
            {
                string folder = F360FileSystem.RemoveFileFromPath(media.videoPath);
                AddVideoFolder(type, folder);
            }
            return true;
        }

        //-----------------------------------------------------------------------------------------------------------------

        int __autoLoadInternal(string folder, bool includeSubfolders)
        {
            if(!isValidFolder(folder))
            {
                Debug.LogWarning("VideoRepo:: given path is not a known video folder! path: " + RichText.italic(folder));
                return -1;
            }
            else
            {
                var type = GetTypeOfVideo(folder);
                if(type == VideoType.Undefined)
                {
                    Debug.LogWarning("VideoRepo:: given path has no video type set! path: " + RichText.italic(folder));
                    return -1;
                }
                else
                {
                    int addedVideos = 0;
                    try
                    {
                        foreach(var file in Directory.EnumerateFiles(folder))
                        {
                            var size = new FileInfo(file).Length;
                            if(file.EndsWith(VIDEO_EXTENSION) && size > MIN_FILE_SIZE)       //  5 MB
                            {   
                                var name = F360FileSystem.FormatName(file, false, false, false);
                                if(!namePointerCache.ContainsKey(name))
                                {       
                                    switch(type)
                                    {
                                        case VideoType.VR:
                                            if(AddLocalTrainingVideo(name, folder))
                                            {
                                                addedVideos++;
                                            }
                                            else
                                            {
                                                Debug.LogWarning("VideoRepo:: error auto-adding video<" + RichText.italic(name) 
                                                                    + ">{" + type + "} in folder: " + RichText.italic(folder));
                                            }
                                            break;

                                        case VideoType.Tutorial:
                                        case VideoType.MenuBG:
                                            
                                            var media = MediaData.CreateVideo(folder + file);
                                            if(AddLocalVideo(type, media))
                                            {
                                                addedVideos++;
                                            }
                                            else
                                            {
                                                Debug.LogWarning("VideoRepo:: error auto-adding video<" + RichText.italic(name) 
                                                                    + ">{" + type + "} in folder: " +  RichText.italic(folder));
                                            }
                                            break;
                                    }
                                }                                
                            }
                        }
                    }
                    catch(IOException ex)
                    {
                        Debug.LogWarning("VideoRepo:: error autoloading from folder: " + RichText.italic(folder) + "\n" + ex.Message);
                        return -1;
                    }

                    return addedVideos;
                }
            }
        }
        

        

        //-----------------------------------------------------------------------------------------------------------------



        /*
        /// refresh duration
        public void onStartVideo(string video, int durationMs)
        {
            if(isValidFileName(video))
            {
                cache[pointerCache[video]] = new MediaData(cache[pointerCache[video]], durationMs);
            }
            else
            {
                Debug.LogWarning("VideoRepo:: file played not in repository file=[" + video + "]");
            }
        }*/



        //-----------------------------------------------------------------------------------------------------------------


        public struct CachedVideo
        {
            public VideoType Type;
            public int VideoID;         ///< sequencer ID
            public MediaData Media;

            public CachedVideo(int id, VideoType type, MediaData media)
            {
                this.VideoID = id;
                this.Type = type;
                this.Media = media;
            }

            public string Name
            {
                get { return Media.fileName; }
            }
            public string FilePath 
            {
                get { return Media.videoPath; }
            }
            public int DurationMs
            {
                get { return Media.durationMs; }
            }
        }




    }


}