using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utility.ManagedObservers;

using System.IO;

namespace VRIntegration.Video
{


    public enum VPlayerState
    {
        Stopped=0,
        Loading,
        Ready,
        Playing,
        Paused,
        Seeking
    }

    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// VideoPlayer interface for the application.
    ///
    public interface IVRPlayer : IObservable
    {
        VPlayerState mState { get; }        ///< State of the player.
        string videoPath { get; }           ///< Currently played/loaded/loading video.
        int durationMs { get; }           ///< Playback duration in milliseconds.
        int currentTimeMs { get; }        ///< Playback time in milliseconds.
        float normalizedTime { get; }       ///< Normalized playback time
        bool loop { get; set; }             ///< Loop the current or next video.
        

        bool blendFrameEnabled { get; }     ///< Returns wether a still frame is displayed at the moment.


        /// @brief
        /// Returns wether this player has a video loaded/loading.
        ///
        bool hasVideo();

        /// @brief
        /// Load a video at provided path.
        ///
        /// @param filePath The absolute path to the video file to be played.
        /// @param callback Ad-hoc callback when player is done loading video.
        /// @param autoPlay Play the video as soon as player is done loading.
        ///
        bool Prepare(MediaData media, System.Action callback=null, bool autoPlay=false);

        /// @brief
        /// Load a video at provided path.
        ///
        /// @param clip The absolute path to the video file to be played.
        /// @param meta Additional metadata for VR playback.
        /// @param callback Ad-hoc callback when player is with preparation.
        /// @param autoPlay Play the video as soon as player is done loading.
        ///
        bool Prepare(MediaData media, VRPlaybackData meta, System.Action callback=null, bool autoPlay=false);

        /*
        /// @brief
        /// Load a video at provided path.
        ///
        /// @param filePath The absolute path to the video file to be played.
        /// @param callback Ad-hoc callback when player is done loading video.
        /// @param autoPlay Play the video as soon as player is done loading.
        ///
        bool Prepare(string filePath, System.Action callback=null, bool autoPlay=false);

        /// @brief
        /// Load a video at provided path.
        ///
        /// @param clip The absolute path to the video file to be played.
        /// @param meta Additional metadata for VR playback.
        /// @param callback Ad-hoc callback when player is with preparation.
        /// @param autoPlay Play the video as soon as player is done loading.
        ///
        bool Prepare(string filePath, VRPlaybackData meta, System.Action callback=null, bool autoPlay=false);*/

        /// @brief
        /// Play the currently loaded video.
        ///
        /// @summary
        /// Can only be called when player state is VPlayerState.Ready or VPlayerState.Paused
        ///
        /// @param delay An additional delay in seconds before video is played.
        ///
        bool Play(float delay=0f);

        /// @brief
        /// Pause the current playback. Call Play() to resume. 
        ///
        void Pause();
        
        /// @brief
        /// Stops the current playback
        ///
        void Stop();

        /// @brief
        /// Seek given timestamp of current video.
        ///
        /// @param timeS video time in milliseconds
        /// @param pause If checked, playback will pause after completed seeking.
        /// 
        void Seek(int timeMs, bool pause);

        void SeekPrecise(int timeMs, bool pause);

        /// @brief
        /// Set video back to start. 
        ///
        /// @param pause If checked, playback will pause after completed seeking.
        ///
        void Rewind(bool pause);

        /// @brief
        /// display a 360° still frame in order to blend between videos or save performance during GUI.
        ///
        /// @param overlay Keep video texture active in background
        ///
        bool ShowBlendFrame(UnityEngine.Texture2D frame, bool overlay=false, float blendT=0f, bool stereo=false);
        bool ShowBlendFrame(UnityEngine.Texture2D frame, Vector2 offset, bool overlay=false, float blendT=0f, bool stereo=false);
        void HideBlendFrame(float blendT=0f);
        void SetPlaybackSpeed(float speed);

        float GetVolume();
        void SetVolume(float volume=1f);

        void Mute(bool b);
    }
    


    /// @brief
    /// Used by Training.TrainingController to fade videoplayer in/out.
    ///
    public interface IVRPlayerFader
    {
        void Prepare();
        void Cleanup();

        bool state { get; }
        bool darkened { get; }

        void fadeIn(float duration);
        void fadeOut(float duration);
        IEnumerator fadeVideoIn(System.Func<bool> videoReady, bool fastBlend);
        IEnumerator fadeVideoOut(bool fastBlend);

        /// @brief 
        /// overlay video with transpharent dark sphere
        ///
        void darkenVideo(bool b);
    }



    /// @brief
    /// Receive callbacks from the current videoplayer.
    ///
    /// @details
    /// Implement the interface and register the observer with
    /// @code Utility.ManagedObservers.ObserverManager.RegisterObserverType<IVRPlayerObserver>(); @endcode
    ///
    public interface IVRPlayerObserver : IManagedObserver
    {
        void OnVideoLoaded(IVRPlayer player);
        void OnStartPlayback(IVRPlayer player);
        void OnPausePlayback(IVRPlayer player);
        void OnResumePlayback(IVRPlayer player);
        void OnStopPlayback(IVRPlayer player);
        void OnReachedLoopPoint(IVRPlayer player);
        void OnStartSeeking(IVRPlayer player, float timeMs);
        void OnFinishedSeeking(IVRPlayer player);
        void OnStalled(IVRPlayer player);
        void OnUnstalled(IVRPlayer player);
        void OnVideoError(IVRPlayer player, string error);
    }

    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// High-level controller interface for playback functionality
    ///
    public interface IVideoControls
    {
        IVRPlayer player { get; }
        
        bool hasVideo();
        bool canChangeVideo();
        bool canSeek();
        bool canPlay();
        bool canPause();
        bool canStop();

        bool Play();
        bool Pause();
        bool Stop();
        bool Seek(int timeMs, bool pause);    
        bool JumpForward(bool pause);
        bool JumpBack(bool pause);    

        bool TryPlayMedia(MediaData media);
        
    }

    public interface IPlaylistController : IVideoControls
    {
        bool SetPlaylist(IPlaylist list);
        bool TryPlayMediaFromPlaylist(int index);
    }

    /// @brief
    /// Basic playlist functionality
    ///
    public interface IPlaylist
    {
        int Count { get; }
        int ActiveCount { get; }
        int IndexOf(string file);
        bool isActive(int index);
        float GetFullDurationMs();
        bool Get(int index, out MediaData data);
        void SetActive(int index, bool state);
        void SetMetadata(int index, string meta);
        bool GetMetadata(int index, out string meta);
        IEnumerable<MediaData> GetAll(bool includeInactive=true);
    }


    

    //-----------------------------------------------------------------------------------------------------------------
    
    /// @brief
    /// Datapaths to streamable video media
    ///
    [System.Serializable]
    public struct MediaData
    {
        /// @brief
        /// name of the file w/o extension
        ///
        public string fileName;

        /// @brief
        /// absolute path to video file
        ///
        public string videoPath;

        /// @brief
        /// absolute path to an additional image file (freeze frame)
        ///
        public string imagePath;

        public int durationMs;

        public RangeInt timeRange;

        public bool Exists()
        {
            return !string.IsNullOrEmpty(videoPath) || !string.IsNullOrEmpty(imagePath);
        }

        public bool isVideo() 
        {
            return !string.IsNullOrEmpty(videoPath);
        }

        public bool isImage()
        {
            return !string.IsNullOrEmpty(imagePath);
        } 

        public bool hasTimeRange()
        {
            return timeRange.length > 0;
        }

        public static MediaData Create(string imagePath, string videoPath, int durationMs=0)
        {
            return new MediaData(imagePath, videoPath, durationMs);
        }
        public static MediaData CreateImage(string path)
        {
            return new MediaData(path, "", 0);
        }
        public static MediaData CreateVideo(string path, int durationMs=0)
        {
            return new MediaData("", path, durationMs);
        }
        public static MediaData CreateVideo(string path, int durationMs, RangeInt timeRange)
        {
            if(timeRange.start >= 0 && timeRange.start < durationMs && timeRange.end <= durationMs)
            {
                var media = CreateVideo(path, durationMs);
                media.timeRange = timeRange;
                return media;
            }
            else
            {
                Debug.LogWarning("MediaData:: invalid timerange given! range[" + timeRange.start + "-" + timeRange.end +"]  durationMs=" + durationMs);
                return CreateVideo(path, durationMs);
            }
        }

        private MediaData(string imagePath, string videoPath, int durationMs)
        {
            this.imagePath = "";
            this.videoPath = videoPath;
            this.durationMs = durationMs;
            this.fileName = formatFileName(videoPath, imagePath);
            this.timeRange = new RangeInt();
        }
        public MediaData(MediaData data, int durationMs)
        {
            this.imagePath = data.imagePath;
            this.videoPath = data.videoPath;
            this.durationMs = durationMs;
            this.fileName = formatFileName(videoPath, imagePath);
            this.timeRange = new RangeInt();
        }
        public static string formatFileName(string videoPath, string imagePath)
        {
            string path = "";
            if(!string.IsNullOrEmpty(videoPath))
            {
                path = videoPath;
            }
            else
            {
                path = imagePath;
            }
            if(!string.IsNullOrEmpty(path))
            {
                var split = path.Split('/');
                var file = split[split.Length-1];
                var index = file.IndexOf('.');
                if(index != -1)
                {
                    return file.Substring(0, index);
                }
                else
                {
                    return file;
                }
            }
            else
            {
                return "Unknown File";
            }
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = !string.IsNullOrEmpty(videoPath) ? videoPath.GetHashCode() ^ 17 : 131;
                hash += !string.IsNullOrEmpty(imagePath) ? imagePath.GetHashCode() ^ 7 : 0;
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null))
            {
                if(obj is MediaData)
                {
                    return Equals((MediaData) obj);
                }
            }
            return false;
        }
        public bool Equals(MediaData data)
        {
            bool vMatch = (string.IsNullOrEmpty(videoPath) && string.IsNullOrEmpty(data.videoPath)) || data.videoPath == videoPath;
            bool iMatch = (string.IsNullOrEmpty(imagePath) && string.IsNullOrEmpty(data.imagePath)) || data.imagePath == imagePath;
            return vMatch && iMatch;
        }

        public static bool operator==(MediaData a, MediaData b)
        {
            return a.Equals(b);
        }
        public static bool operator!=(MediaData a, MediaData b)
        {
            return !a.Equals(b);
        }


        public bool LoadImageFromDisk(out Texture2D img)
        {
            if(!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    if(File.Exists(imagePath))
                    {
                        //  try load from disc
                        byte[] data = File.ReadAllBytes(imagePath);
                        img = new Texture2D(2,2);
                        img.LoadImage(data);
                        return true;
                    }
                    else
                    {
                        //  try load from resources
                        img = Resources.Load<Texture2D>(imagePath);
                        return img != null;
                    }
                }
                catch(System.Exception ex) {
                    Debug.LogError("Unable to load image file at path<" + imagePath + ">\n" + ex.Message);
                }
                
            }   
            img = null;
            return false;    
        }
    }

    //-----------------------------------------------------------------------------------------------------------------
    
    /// @brief
    /// Additional VR playback data
    ///
    [System.Serializable]
    public struct VRPlaybackData
    {
        public string fileName;
        public UnityEngine.Texture2D blendFrame;
        public bool stereo;
        public bool setBlendframeAfterPlayback;

        public VRPlaybackData(string filename, UnityEngine.Texture2D blendframe, bool stereo=false, bool afterPlayback=false)
        {
            this.fileName = filename;
            this.blendFrame = blendframe;
            this.stereo = stereo;
            this.setBlendframeAfterPlayback = true;
        }
    }


    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// Customizable generic playback controller.
    /// Validates user input & handles playlists.
    ///
    public class VRPlayerController : IPlaylistController
    {
        IVideoControls _parentControls;
        IPlaylistController _parentPlaylistControls;
        VRPlaybackListener _listener;
        IVRPlayer _player;
        bool hasParent { get { return _parentControls != null; } }
        bool hasParentPlaylist { get { return _parentPlaylistControls != null; } }

        
        public int frame_Jump = 12;
        public System.Func<bool> checkCanChange;
        public System.Func<bool> checkCanPlay;
        public System.Func<bool> checkCanPause;
        public System.Func<bool> checkCanStop;
        public System.Func<bool> checkCanSeek;



        public IVideoControls parent {
            get {
                return _parentControls;
            }   
            set {
                _parentControls = value;
                _parentPlaylistControls = value as IPlaylistController;
            }
        }
        

        public IVRPlayer player { 
            get { 
                return _player != null ? _player : VRPlayer.Player; 
            } 
            set { _player = value; }
        }

        public VRPlaybackListener listener {
            get {
                if(_listener == null) {
                    _listener = new VRPlaybackListener(player);
                    _listener.EnableListener();
                }
                return _listener;
            }
        }

        public IPlaylist playlist { get; private set; } 

        public VPlayerState state { get { return player.mState; } }

        public int timeMs { get { return Mathf.FloorToInt(player.currentTimeMs); } }
        public int durationMs { get { return Mathf.FloorToInt(player.durationMs); }  }


        public bool SetPlaylist(IPlaylist list)
        {
            if(hasParentPlaylist)
            {
                if(!_parentPlaylistControls.SetPlaylist(list))
                {
                    return false;
                }
            }
            if(canChangeVideo())
            {
                playlist = list;
                return true;
            }
            return false;
        }

        public bool hasVideo()
        {
            if(hasParent)
            {
                return parent.hasVideo();
            }
            else
            {
                return player.hasVideo();
            }
        }
        public bool canChangeVideo()
        {
            if(hasParent)
            {
                return parent.canChangeVideo();
            }
            else if(checkCanChange != null)
            {
                return checkCanChange();
            }
            else
            {
                return true;
            }
        }
        public bool canSeek()
        {
            if(hasParent)
            {
                return parent.canChangeVideo();
            }
            else if(checkCanSeek != null)
            {
                return checkCanSeek();
            }
            else
            {
                return true;
            }
        }
        public bool canPlay()
        {
            if(hasParent)
            {
                return parent.canPlay();
            }
            else
            {
                return hasVideo()
                    && (checkCanPlay == null || checkCanPlay())
                    && (state == VPlayerState.Ready 
                    || state == VPlayerState.Paused 
                    || state == VPlayerState.Stopped);
            }
        }
        public bool canPause()
        {
            if(hasParent)
            {
                return parent.canPause();
            }
            else
            {
                return (checkCanPause == null || checkCanPause()) 
                        && state == VPlayerState.Playing;
            }
        }
        public bool canStop()
        {
            if(hasParent)
            {
                return parent.canStop();
            }
            else
            {
                return (checkCanStop == null || checkCanStop()) 
                        && state != VPlayerState.Stopped;
            }
        }

        public bool Play()
        {
            if(canPlay())
            {
                if(hasParent)
                {
                    return parent.Play();
                }
                else
                {
                    return player.Play();;
                }  
            } 
            return false;
        }
        public bool Pause()
        {
            if(canPause())
            {
                if(hasParent)
                {
                    return parent.Pause();
                }
                else
                {
                    player.Pause();
                    return true;
                } 
            }
            return false;
        }
        public bool Stop()
        {
            if(canStop())
            {
                if(hasParent)
                {
                    return parent.Stop();
                }
                else
                {
                    player.Stop();
                } 
            }
            return false;
        }
        public bool Seek(int timeMs, bool pause)
        {
            if(canSeek())
            {
                if(hasParent)
                {
                    return parent.Seek(timeMs, pause);
                }
                else
                {
                    player.Seek(timeMs, pause);
                    return false;
                }  
            }
            return false;
        }    
        public bool JumpForward(bool pause)
        {
            if(hasParent)
            {
                return parent.JumpForward(pause);
            }
            else if(frame_Jump > 0)
            {
                return Seek(Mathf.Min(player.currentTimeMs+frame_Jump, player.durationMs), pause);
            } 
            return false;
        }
        public bool JumpBack(bool pause)
        {
            if(hasParent)
            {
                return parent.JumpBack(pause);
            }
            else if(frame_Jump > 0)
            {
                return Seek(Mathf.Max(player.currentTimeMs-frame_Jump, 0), pause);
            } 
            return false;
        }    

        public bool TryPlayMedia(MediaData media)
        {
            if(hasParent)
            {
                return parent.TryPlayMedia(media);
            }
            else if(media.Exists())
            {
                player.Stop();
                if(player.Prepare(media, null, true))
                {
                    return true;
                }
                else
                {
                    Debug.LogWarning("VRPlayerController:: Could not load media from path=[" + media.videoPath + "]");
                }
            }
            else
            {
                Debug.LogWarning("VRPlayerController:: media at path=[" + media.videoPath + "] does not exist.");
            }
            return false;
        }

        

        public bool TryPlayMediaFromPlaylist(int index)
        {
            if(hasParentPlaylist)
            {   
                return _parentPlaylistControls.TryPlayMediaFromPlaylist(index);
            }
            else if(playlist != null && index >= 0 && index < playlist.Count)
            {
                MediaData media;
                if(playlist.Get(index, out media))
                {
                    if(media.Exists())
                    {
                        player.Stop();
                        if(player.Prepare(media, null, true))
                        {
                            return true;
                        }
                        else
                        {
                            Debug.LogWarning("VRPlayerController:: Could not load media from path=[" + media.videoPath + "]");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("VRPlayerController:: media at path=[" + media.videoPath + "] does not exist.");
                    }
                }
                else
                {
                    Debug.LogWarning("VRPlayerController:: media at path=[" + media.videoPath + "] does not exist.");
                }
            }
            return false;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// Generic playback listener
    ///
    public class VRPlaybackListener : IVRPlayerObserver
    {
        public readonly IVRPlayer player;

        public event System.Action videoLoaded;
        public event System.Action startedPlayback;
        public event System.Action pausedPlayback;
        public event System.Action resumedPlayback;
        public event System.Action stoppedPlayback;
        public event System.Action reachedLoopPoint;
        public event System.Action<float> startedSeeking;
        public event System.Action finishedSeeking;
        public event System.Action stalled;
        public event System.Action unstalled;
        public event System.Action<string> error;

        private bool subscribed = false;

        public VRPlaybackListener(IVRPlayer player)
        {
            this.player = player;
            EnableListener();
        }

        public void EnableListener()
        {
            if(!subscribed)
            {
                player.AddObserver<IVRPlayerObserver>(this);
                subscribed = true;
            }
        }
        public void DisableListener()
        {
            if(subscribed)
            {
                player.RemoveObserver<IVRPlayerObserver>(this);
                subscribed = false;
            }
        }

        void IVRPlayerObserver.OnVideoLoaded(IVRPlayer player) 
        {
            videoLoaded?.Invoke();
        }
        void IVRPlayerObserver.OnStartPlayback(IVRPlayer player) 
        {
            startedPlayback?.Invoke();
        }
        void IVRPlayerObserver.OnPausePlayback(IVRPlayer player) 
        {
            pausedPlayback?.Invoke();
        }
        void IVRPlayerObserver.OnResumePlayback(IVRPlayer player) 
        {
            resumedPlayback?.Invoke();
        }
        void IVRPlayerObserver.OnStopPlayback(IVRPlayer player) 
        {
            stoppedPlayback?.Invoke();
        }
        void IVRPlayerObserver.OnReachedLoopPoint(IVRPlayer player) 
        {
            reachedLoopPoint?.Invoke();
        }
        void IVRPlayerObserver.OnStartSeeking(IVRPlayer player, float timeMs)
        {
            startedSeeking?.Invoke(timeMs);
        }
        void IVRPlayerObserver.OnFinishedSeeking(IVRPlayer player)
        {
            finishedSeeking?.Invoke();
        }

        void IVRPlayerObserver.OnStalled(IVRPlayer player)
        {
            stalled?.Invoke();
        }
        void IVRPlayerObserver.OnUnstalled(IVRPlayer player)
        {
            unstalled?.Invoke();
        }
        void IVRPlayerObserver.OnVideoError(IVRPlayer player, string e)
        {
            error?.Invoke(e);
        }
    }
}

