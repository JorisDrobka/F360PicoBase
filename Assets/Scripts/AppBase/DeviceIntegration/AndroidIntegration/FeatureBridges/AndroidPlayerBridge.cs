using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeviceBridge.Wifi;

namespace DeviceBridge.Android.Internal
{
    /// @brief
    /// Provides basic access to android activity state as and receives commands from the android side.
    ///
    public class AndroidPlayerBridge : FeatureBridge, IAndroidPlayerBridgeInternal
    {        
        public override DeviceFeature mFeature { get { return DeviceFeature.ACTIVITY_ACCESS; } }

        protected bool applicationPause { get; private set; } = false;

        private List<IDeviceCommand> perInitCmds = new List<IDeviceCommand>();

        protected override void Awake()
        {
            base.Awake();

            //  register base callback types
            Utility.ManagedObservers.ObserverManager.RegisterObserverType<ICommandListener>();
            Utility.ManagedObservers.ObserverManager.RegisterObserverType<IAppStateListener>();
            Utility.ManagedObservers.ObserverManager.RegisterObserverType<IBatteryStateListener>();
        }

        #if UNITY_EDITOR
        void OnDestroy()
        {
            SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationStop(0));
            SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationDestroy());
        }
        #endif

        //-----------------------------------------------------------------------------------------------------------------

        protected override void OnInitializeBridge(AndroidBridge main)
        {
            //  resolve all commands send before init
            foreach(var cmd in perInitCmds)
            {
                SendMessageToObservers<ICommandListener>(x=> x.OnReceiveCommand(cmd));
            }
            if(applicationPause)
            {
                SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationSleep());
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  frontend

        public bool isPaused()
        {
            return applicationPause;
        }

        public virtual int GetBatteryLevel()
        {
            return 0;  //  TODO: implement default method for battery level request on android
        }

        public virtual bool hasSystemLevelPermission() 
        { 
            return false; 
        }

        public virtual void Exec_Recenter()
        {
            
        }

        public virtual void Exec_Shutdown()
        {
            
        }

        public virtual void Exec_LockScreen()
        {
            //  implement android plugin: KeepLockScreenOn (see: https://stackoverflow.com/questions/5331152/correct-method-for-setkeepscreenon-flag-keep-screen-on)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        public virtual void Exec_UnlockScreen()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        public virtual bool Exec_AquireWakeLock()
        {
            return false;
        }
        public virtual void Exec_ReleaseWakeLock()
        {

        }

        public virtual bool Exec_SilentInstall(string pathToFile)
        {
            return false;
        }

        public virtual bool GotoActivity(string package, string activity)
        {
            return false;
        }
        public virtual bool GotoWifiSettings()
        {
            return false;
        }
        public virtual bool GotoScreenshareSettings()
        {
            return false;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  backend

        public virtual void OnApplicationSleep()
        {
            if(debug)
            {
                Debug.Log("AndroidPlayerBridge.OnApplicationSleep()  already paused? " + applicationPause);
            }

            if(!applicationPause)
            {
                applicationPause = true;
                if(isInitialized)
                {
                    SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationSleep());   
                }
            }
        }

        public virtual void OnApplicationWake()
        {
            if(debug)
            {
                Debug.Log("AndroidPlayerBridge.OnApplicationWake() paused? " + applicationPause);
            }
            if(applicationPause)
            {
                applicationPause = false;
                if(isInitialized)
                {
                    SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationWake());
                }
            }
        }

        public virtual void OnApplicationStop(/*string reason>*/)
        {   
            applicationPause = true;
            if(isInitialized)
            {
                SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationStop(0));
            }
            /*int parsed;
            if(parseInteger(reason, out parsed))
            {
                if(debug)
                {
                    Debug.Log("AndroidPlayerBridge.OnApplicationStop() reason=[" + WifiConstants.Readable(parsed) + "]");
                }
                applicationPause = true;
                if(isInitialized)
                {
                    SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationStop(parsed));
                }
            }
            else
            {
                Debug.LogError("Cannot parse reason=[" + reason + "]");
            }*/
        }

        public virtual void OnApplicationDestroy()
        {
            if(isInitialized)
            {
                SendMessageToObservers<IAppStateListener>(x=> x.OnApplicationDestroy());
            }
        }

        public virtual void OnUpdatedBatteryLevel(string level)
        {
            int parsed;
            if(parseInteger(level, out parsed))
            {
                if(isInitialized)
                {
                    SendMessageToObservers<IBatteryStateListener>(x=> x.OnUpdatedBatteryLevel(parsed));
                }
            }
        }


        public virtual void OnReceiveCommand(string json)
        {
            //  unpack command
            var c = Command.Parse(json) as IDeviceCommand;
            if(c != null)
            {
                if(debug)
                {
                    Debug.Log("AndroidPlayerBridge.OnReceiveCommand=[" + c.CMD + (!string.IsNullOrEmpty(c.content) ? " {" + c.content + "}]" : ""));
                }
                if(isInitialized)
                {
                    SendMessageToObservers<ICommandListener>(x=> x.OnReceiveCommand(c));
                }
                else
                {
                    perInitCmds.Add(c);
                }
            }
        }

        //  receive responses of commands without callback set
        public virtual void OnReceiveResponse(string response)
        {
            //  unpack response
            Response r = Response.Parse(response);

            if(debug)
            {
                Debug.Log("AndroidPlayerBridge.OnReceiveResponse: id=[" + r.uid + "]Token=[" + r.TOKEN + "] content={" + (r.isJson() ? "(" + r.jsonType + ")" : r.content) + "}");
            }

        }

        public virtual bool RouteCommand(IDeviceCommand cmd, AndroidCommandCallback callback=null)
        {
            return false;
        }
        
    }
}

