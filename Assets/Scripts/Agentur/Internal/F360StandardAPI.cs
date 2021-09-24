using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using F360.Data;
using F360.Users;
using F360.Users.Stats;
using F360.Backend;
using F360.Backend.Messages;

using DeviceBridge;

namespace F360
{

    public class StandardAPI : MonoBehaviour, 
                                    IStandardAPI, IMenuAPI, IDataAPI, IBackendAPI, IDeviceAPI
    {

    
        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Interface

        public static IStandardAPI GetAPI() { return GetInstance(); }

        public static IMenuAPI Menu { get { return GetInstance().menu; } }
        public static IDataAPI Data { get { return GetInstance().data; } }
        public static IBackendAPI Backend { get { return GetInstance().backend; } }
        public static IDeviceAPI Device { get { return GetInstance().device; } }


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Menu

        void IMenuAPI.RunLecture(int lectureID)
        {
            Debug.Log(RichText.emph("Menu Endpoint") + ": run lecture=[" + RichText.emph(lectureID) + "]");
        }     
        void IMenuAPI.RunDriveVR(int videoID)
        {
            Debug.Log(RichText.emph("Menu Endpoint") + ": run driveVR=[" + RichText.emph(videoID) + "]");
        }
        void IMenuAPI.RunExam(ExamLevel level)
        {
            Debug.Log(RichText.emph("Menu Endpoint") + ": run exam=[" + RichText.emph(level.ToString()) + "]");
        }

        void IMenuAPI.OnChangedMenu(AppLocation location)
        {
            Debug.Log(RichText.emph("Menu Callback") + ": changed location=[" + location + "]");
        }


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Data

        VTrainerChapter IDataAPI.GetVTrainerChapter(int chapterID)
        {
            return VTrainerRepo.Current.Get(chapterID);
        }
        VTrainerLecture IDataAPI.GetVTrainerLecture(int lectureID)
        {
            return VTrainerRepo.Current.GetLecture(lectureID);
        }
        VideoMetaData IDataAPI.GetVideoData(int videoID)
        {
            VideoMetaData meta;
            if(VideoRepo.Current.GetMetadata(videoID, out meta))
            {
                return meta;
            }
            return null;
        }
        F360Student IDataAPI.GetCurrentUser()
        {
            return ActiveUser.Current as F360Student;
        }
        IUserStats IDataAPI.GetUserStats()
        {
            var student = ActiveUser.Current as F360Student;
            if(student != null)
            {
                return student.Stats;
            }
            return null;
        }


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Backend

        void IBackendAPI.Login(string user, System.Action<LoginState, SC_UserLoginInfo> callback)
        {
            
        }      
        void IBackendAPI.Login(int userID, System.Action<LoginState, SC_UserLoginInfo> callback)
        {

        }

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Device

        bool IDeviceAPI.hasWifiConnection()
        {
            UnityEngine.Assertions.Assert.IsNotNull(wifiBridge, "No Wifi Adapter found in scene!");
            return wifiBridge.isConnected();
        }
        int IDeviceAPI.GetBatteryState()
        {
            UnityEngine.Assertions.Assert.IsNotNull(playerBridge, "No DevicePlayer Adapter found in scene!");
            return playerBridge.GetBatteryLevel();
        }
        bool IDeviceAPI.GotoWifiMenu()
        {
            UnityEngine.Assertions.Assert.IsNotNull(playerBridge, "No DevicePlayer Adapter found in scene!");
            return playerBridge.GotoWifiSettings();
        }
        bool IDeviceAPI.GotoScreenshareMenu()
        {
            UnityEngine.Assertions.Assert.IsNotNull(playerBridge, "No DevicePlayer Adapter found in scene!");
            return playerBridge.GotoScreenshareSettings();
        }





        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Internal

        IMenuAPI IStandardAPI.menu { get { return this; } }
        IDataAPI IStandardAPI.data { get { return this; } }
        IBackendAPI IStandardAPI.backend { get { return this; } }
        IDeviceAPI IStandardAPI.device { get { return this; } }

        IWifiBridge wifiBridge { get { return DeviceAdapter.Instance.Wifi; } }
        IPlayerBridge playerBridge { get { return DeviceAdapter.Instance.Player; } }
        F360Client client;

        void Awake()
        {
            __instance = this;
        }   

        void onLoaderReady()
        {
            client = F360Backend.GetClient<F360Client>();
        }


        static IStandardAPI GetInstance()
        {
            if(__instance == null) {
                GameObject go = new GameObject("StandardAPI");
                __instance = go.AddComponent<StandardAPI>();
            }
            return __instance;
        }
        static IStandardAPI __instance;
    }

}

