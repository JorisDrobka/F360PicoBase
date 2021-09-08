using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeviceBridge.Android
{


    /// @brief
    /// Specific implementation of Pico player features such as battery & special activities provided by Pico SDK
    ///
    public class AndroidPicoPlayerBridge : Android.Internal.AndroidPlayerBridge
    {

        const string ACTIVITY_POWER_MANAGER = "com.picovr.picovrpowermanager.PicoVRPowerManagerActivity";
        const string PACKAGE_POWER_MANAGER = "com.picovr.picovrpowermanager";


        const string ACTIVITY_DISPLAY_SETTINGS = "com.android.settings.picovr.PvrWifiDisplaySettings";
        const string PACKAGE_SETTINGS = "com.android.settings";


        const string ACTIVITY_SHORTCUT = "com.pvr.shortcut.MainActivity";
        const string PACKAGE_SHORTCUT = "com.pvr.shortcut";


        const string PACKAGE_PICO_CONTROLLER = "com.picovr.hummingbirdsvc";
        const string ACTIVITY_CONTROLLER_AUTOCONNECT = PACKAGE_PICO_CONTROLLER + ".AutoConnectService";
        const string ACTIVITY_CONTROLLER_WIZARD = PACKAGE_PICO_CONTROLLER + ".TranslucentActivity";


        //  see http://static.appstore.picovr.com/docs/PowerManager/chapter_two.html
        const string CMD_RECENTER = "pm_recenter";
        const string CMD_ANDROID_SHUTDOWN = "pm_shutdown"; //"androidShutDown";
        const string CMD_ANDROID_REBOOT = "pm_reboot"; //"androidReBoot";
        const string CMD_ANDROID_LOCK_SCREEN = "pm_lock_screen"; //"androidLockScreen";
        const string CMD_ANDROID_UNLOCK_SCREEN = "pm_unlock_screen";
        const string CMD_ACQUIRE_WAKELOCK = "pm_aquire_wakelock";
        const string CMD_RELEASE_WAKELOCK = "pm_release_wakelock";
        //const string CMD_SET_PROP_SLEEP = "setpropSleep";
        //const string CMD_SET_PROP_LOCK_SCREEN = "setPropLockScreen";
        const string CMD_SILENT_INSTALL = "pm_silent_install"; //"silentinstall";
        //const string CMD_SILENT_UNINSTALL = "silentUninstall";
        const string CMD_GOTO_ACTIVITY = "pm_goto_activity"; //"goToActivity";


        /*string actionForSettings = "pui.settings.action.SETTINGS";
        string actionForBluetooth = "pui.settings.action.BLUETOOTH_SETTINGS";
        string actionForWifi = "pui.settings.action.WIFI_SETTINGS";*/

        //-----------------------------------------------------------------------------------------------------------------

        public bool TMP_FLAG_SYSLEVEL_BUILD;

        AndroidJavaObject powerManagerActivity;

        int lastBatteryLevel = -1;

        bool screenLocked = false;

        //-----------------------------------------------------------------------------------------------------------------
        
        //  init

        protected override void Start () 
        {
            base.Start();

            if(Application.platform == RuntimePlatform.Android)
            {
                StartCoroutine("UpdateBattery");
            }
        }

        protected override void OnInitializeBridge(AndroidBridge main)
        {
            if(debug)
            {
                Debug.Log("Initialize PicoBridge... batteryLevel=[" + lastBatteryLevel + "]");
            }

            //  try read battery level
            if(lastBatteryLevel > 0)
            {
                SendMessageToObservers<IBatteryStateListener>(x=> x.OnUpdatedBatteryLevel(lastBatteryLevel));
            }
        }


        //-----------------------------------------------------------------------------------------------------------------
        
        public override int GetBatteryLevel()
        {
            return lastBatteryLevel;
        }


        //  system-level access functionality

        public override bool hasSystemLevelPermission()
        {
            return TMP_FLAG_SYSLEVEL_BUILD;                 //  TODO: ask plugin for access level
        }

        public override void Exec_Recenter()
        {
            var cmd = new Command(CMD_RECENTER);
            this.mainBridge.SendCommand(cmd);
        }

        public override void Exec_Shutdown()
        {
            if(hasSystemLevelPermission())
            {
                var cmd = new Command(CMD_ANDROID_SHUTDOWN);
                this.mainBridge.SendCommand(cmd);
            }
        }
        public void Exec_Reboot()
        {
            if(hasSystemLevelPermission())
            {
                var cmd = new Command(CMD_ANDROID_REBOOT);
                this.mainBridge.SendCommand(cmd);
            }
        }
        public override void Exec_LockScreen()
        {
            if(hasSystemLevelPermission() && !screenLocked)
            {
                var cmd = new Command(CMD_ANDROID_LOCK_SCREEN);
                this.mainBridge.SendCommand(cmd);
                screenLocked = true;
            }
        }
        public override void Exec_UnlockScreen()
        {
            if(hasSystemLevelPermission() && screenLocked)
            {
                var cmd = new Command(CMD_ANDROID_UNLOCK_SCREEN);
                this.mainBridge.SendCommand(cmd);
            }
        }
        public override bool Exec_AquireWakeLock()
        {
            if(hasSystemLevelPermission())
            {
                var cmd = new Command(CMD_ACQUIRE_WAKELOCK);
                this.mainBridge.SendCommand(cmd);
                return true;
            }
            return false;
        }
        public override void Exec_ReleaseWakeLock()
        {
            if(hasSystemLevelPermission())
            {
                var cmd = new Command(CMD_RELEASE_WAKELOCK);
                this.mainBridge.SendCommand(cmd);
            }
        }
        public override bool Exec_SilentInstall(string pathToApk)
        {
            if(!string.IsNullOrEmpty(pathToApk) && hasSystemLevelPermission())
            {
                var cmd = new Command(CMD_SILENT_INSTALL, pathToApk);
                this.mainBridge.SendCommand(cmd);
                return true;
            }
            return false;
        }
        public override bool GotoActivity(string packageName, string activityName)
        {
            return _gotoActivity(packageName, activityName);
        }

        public override bool GotoWifiSettings()
        {
            return _gotoActivity(F360.AppConfig.Current.settings_package, F360.AppConfig.Current.settings_wifi_activity);
        }
        public override bool GotoScreenshareSettings()
        {
            Debug.Log("PicoBridge.GotoScreenshareSettings.. " + F360.AppConfig.Current.settings_package + " " + F360.AppConfig.Current.settings_screencast_activity);
            return _gotoActivity(F360.AppConfig.Current.settings_package, F360.AppConfig.Current.settings_screencast_activity);
        }

        public bool RunControllerConnectActivity(System.Action<bool> callback=null)
        {
            Debug.Log("PicoBridge.TryFindController..");
            return _gotoActivity(PACKAGE_PICO_CONTROLLER, ACTIVITY_CONTROLLER_AUTOCONNECT);
        }       



        /*public override bool RouteCommand(IDeviceCommand cmd, AndroidCommandCallback callback=null)
        {
            if(cmd.CMD == Commands.GOTO_ACTIVITY)
            {
                if(cmd.content == Commands.ACTIVITY_DISPLAY_SETTINGS)
                {
                    return GotoActivity(PACKAGE_SETTINGS, ACTIVITY_DISPLAY_SETTINGS);
                }
                else if(cmd.content == Commands.ACTIVITY_WIFI_SELECTION)
                {
                    gotoWifiSettings();
                    return true;
                }
                else if(cmd.content == Commands.ACTIVITY_SCREEN_SHARE)
                {
                    gotoScreenshareSettings();
                    return true;
                }
            }
            return false;
        }*/

        //-----------------------------------------------------------------------------------------------------------------

        //  extended pico access

        bool _gotoActivity(string package, string activity)
        {
       /*     if(!hasSystemLevelPermission())
            {
                Debug.LogWarning("AndroidPlayerBridge:: Misses SystemLevel Access to change Activity!");
                return false;
            }*/

            if(debug)
            {
                Debug.Log("PicoBridge.GotoActivity  activity=[" + activity + "]  package=[" + package + "]");
            }

            var cmd = new Command(CMD_GOTO_ACTIVITY, package + "/" + activity);
            this.mainBridge.SendCommand(cmd);
            return true;


            /*if(powerManagerActivity != null)
            {
                try 
                {
                    powerManagerActivity.Call(CMD_GOTO_ACTIVITY, new object[] { package, activity });
                    return true;
                } 
                catch (AndroidJavaException e) 
                {
                    Debug.LogError ("Exception calling method " + name + ": " + e);
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("PicoBridge.GotoActivity:: no power manager plugin missing");
                return false;
            }*/
            
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  battery

        IEnumerator UpdateBattery()
        {
            while (true)
            {
                int level = _getBatteryLevel();
                if(true || level != lastBatteryLevel)
                {
                    if(debug)
                    {
           //             Debug.Log("AndroidPicoPlayerBridge.UpdateBattery=[" + level + "]");
                    } 
                    SendMessageToObservers<IBatteryStateListener>(x=> x.OnUpdatedBatteryLevel(level));
                    lastBatteryLevel = level;
                }
                yield return new WaitForSeconds(10f);
            }
        }

        int _getBatteryLevel()
        {
            try
            {
                const string filestring = "/sys/class/power_supply/battery/capacity";
                
                string CapacityString = System.IO.File.ReadAllText(filestring);
                return int.Parse(CapacityString);
            }
            catch (System.Exception e)
            {
                Debug.Log("Failed to read battery power; " + e.Message);
            }
            return -1;
        }


        //-----------------------------------------------------------------------------------------------------------------

        
    }
}


