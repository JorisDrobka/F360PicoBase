using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

//using F360.Manage;

namespace F360
{


    /// VR Application Config File

    [DataContract]
    public class AppConfig : AppConfigBase
    {

        //  internal defines

        public const string FILE_PATCHMANIFEST = "patchlevel";
        const string filepath_patchmanifest
        #if UNITY_EDITOR
         = "<project>/.patchtest/";
        #else
         = "<appdata>/";
        #endif

        //-----------------------------------------------------------------------------------------------------------------

        //  public F360 Path Defines

        public const string PATH_MENU_VIDEOS = "p_menu_videos";
        public const string PATH_EXAM_VIDEOS = "p_exam_videos";
        public const string PATH_DRIVE_VR_VIDEOS = "p_drivevr_videos";
        public const string PATH_TUTORIAL_VIDEOS = "p_tutorial_videos";
        public const string PATH_EXAM_DATA = "p_exam_data";

        public const string PATH_TMP_DOWNLOAD = "p_tmp_download";
        public const string PATH_WORKING_BUILD = "p_working_build";


        //-----------------------------------------------------------------------------------------------------------------

        //  public Preset Keys

        public const string KEY_DEVICE_NAME = "k_deviceName";


        //-----------------------------------------------------------------------------------------------------------------

        /*public override IPatchLevelManifest patchManifest 
        { 
            get { return m_PatchManifest; } 
            set { m_PatchManifest = value as PatchlevelManifest; } 
        }*/
        public override string SDRootPath  { get { return m_RootPath; } protected set { m_RootPath = value; } } 
        public override string DataFolder  { get { return m_DataPath; } protected set { m_DataPath = value; } } 
        
        public override string DefaultDeviceName { get { return device_defaultName; } }

        public string Backend_URL { get { return backend_address; } }

        /*public override string GetPatchLevel()
        {
            if(m_PatchManifest != null) return m_PatchManifest.patch_level;
            else return "";
        } */

        //-----------------------------------------------------------------------------------------------------------------

        //  constructor

        public static new AppConfig Current { get; private set; }

        AppConfig() : base() 
        {
            Current = this;
            addPathInternal(PATH_DRIVE_VR_VIDEOS,   Define_PathToDriveVRVideos);
            addPathInternal(PATH_TUTORIAL_VIDEOS,   Define_PathToTutorialVideos);
            addPathInternal(PATH_MENU_VIDEOS,       Define_PathToMenuVideos);
            addPathInternal(PATH_EXAM_VIDEOS,       Define_PathToExamVideos);
            addPathInternal(PATH_EXAM_DATA,         Define_PathToExamData);
            addPathInternal(PATH_TMP_DOWNLOAD,      Define_PathToTmpDownloadFolder);
            addPathInternal(PATH_WORKING_BUILD,     Define_PathToWorkingBuildBackup);
        }

        //-----------------------------------------------------------------------------------------------------------------

        
        protected override string Define_PathToUserData(bool absolute)   
        { 
            return FormatDataPath(userDataCacheDir, PathFormat.RelativeToDataFolder, absolute); 
        }
        protected override string Define_PathToPatchManifest(bool absolute) 
        { 
 //           Debug.Log("======================> GET PATCHMANIFEST path=[" + filepath_patchmanifest + "] abs? " + absolute + " " + userDataCacheDir);
            return FormatDataPath(filepath_patchmanifest, PathFormat.Absolute, absolute) + FILE_PATCHMANIFEST; 
        }
        protected override string Define_PathToBundleData(bool absolute) 
        { 
            if(false && Application.isEditor && absolute)
            {   
                var path = Application.dataPath;
                var l = "Assets".Length;
                path = path.Remove(path.Length - l, l);
                return path + "/" + bundleDataDir;
            }
            else
            {
                return FormatDataPath(bundleDataDir, Application.isEditor ? PathFormat.Absolute : PathFormat.RelativeToDataFolder, absolute);   
            } 
        }
        protected override string Define_PathToCurrentBuildData(bool absolute)
        {
            return FormatDataPath(currentBuildDir, PathFormat.Absolute, absolute);
        }


        string Define_PathToWorkingBuildBackup(bool absolute)
        {
            return FormatDataPath(workingBuildBackupDir, PathFormat.Absolute, absolute);
        }
        string Define_PathToTmpDownloadFolder(bool absolute)
        {
            return FormatDataPath(tmpDownloadDir, PathFormat.Absolute, absolute);
        }

        string Define_PathToDriveVRVideos(bool absolute) 
        { 
            return FormatDataPath(driveVrVideosDir, /*Application.isEditor ? */PathFormat.Absolute /*: PathFormat.RelativeToDataFolder*/, absolute); 
        }
        string Define_PathToTutorialVideos(bool absolute) 
        { 
            return FormatDataPath(tutorialVideosDir, /*Application.isEditor ? */PathFormat.Absolute /*: PathFormat.RelativeToDataFolder*/, absolute); 
        }
        string Define_PathToMenuVideos(bool absolute) 
        { 
            return FormatDataPath(menuVideosDir, /*Application.isEditor ? */PathFormat.Absolute /*: PathFormat.RelativeToDataFolder*/, absolute); 
        }
        string Define_PathToExamVideos(bool absolute) 
        { 
            return FormatDataPath(examVideosDir, /*Application.isEditor ? */PathFormat.Absolute /*: PathFormat.RelativeToDataFolder*/, absolute); 
        }
        string Define_PathToExamData(bool absolute) 
        { 
            return FormatDataPath(examDataDir, Application.isEditor ? PathFormat.Absolute : PathFormat.RelativeToDataFolder, absolute); 
        }
        
        


        //-----------------------------------------------------------------------------------------------------------------

        //  runtime fields

        string m_RootPath = "";
        string m_DataPath = "";

        //PatchlevelManifest m_PatchManifest;

        //-----------------------------------------------------------------------------------------------------------------

        //  serialized fields

        #pragma warning disable

        [DataMember]
        public string remoteId;
        [DataMember]
        public string patchLevelId;
        [DataMember]
        public int updatePollInterval;
        [DataMember]
        public string updateServerUrl;

        [DataMember]
        public string workingBuildBackupDir;
        [DataMember]
        public string tmpDownloadDir;
        [DataMember]
        public string currentBuildDir;

        [DataMember]
        public string userDataCacheDir;
        [DataMember]
        public string bundleDataDir;
        [DataMember]
        public string examDataDir;


        [DataMember]
        public string examVideosDir;
        [DataMember]
        public string driveVrVideosDir;
        [DataMember]
        public string tutorialVideosDir;
        [DataMember] 
        public string menuVideosDir;
        

        [DataMember]
        public string settings_package;
        [DataMember]
        public string settings_screencast_activity;
        [DataMember]
        public string settings_wifi_activity;

        [DataMember]
        string backend_address;

        [DataMember]
        public string device_defaultName;       //  optional


        #pragma warning restore
    }

}
