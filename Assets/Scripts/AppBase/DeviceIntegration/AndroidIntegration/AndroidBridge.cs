using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using Utility.ManagedObservers;

using DeviceBridge.Internal;
using DeviceBridge.Android.Internal;

namespace DeviceBridge.Android
{
    public class AndroidBridge : MonoBehaviour, IAndroidBridge, IObservable
    {

        protected const string MAINACTIVITY_SINGLETON_ACCESS = "currentActivity";
        protected const string MAINACTIVITY_BRIDGE_ACCESS = "GetBridge";
        protected const string FUNC_RECEIVE_COMMAND = "OnReceiveCommand";
        protected const string FUNC_ENABLE_FEATURE = "EnableFeature";
        protected const string FUNC_DISABLE_FEATURE = "DisableFeature";


        //-----------------------------------------------------------------------------------------------------------------

        public bool debug = false;
        public bool debugPlugin = false;
        public bool debugPermissions;

        public AndroidJavaObject mainActivity { get; private set; }
        public AndroidJavaObject bridgeObject { get; private set; }
        
        public virtual bool isReady { get { return _bridgebaseReady; } }

        public bool isWaitingForPermissions { get; private set; }

        protected bool _bridgebaseReady { get; private set; }

        protected bool wasStarted { get; private set; }

        public DeviceFeature allFeatures { get; private set; }
        public DeviceFeature enabledFeatures { get; private set; }

        List<IDeviceBridgeListener> listeners;

        List<CachedCommand> commandCache = new List<CachedCommand>();

        bool firstActivation = false;
        bool readyForCommands = false;


        //-----------------------------------------------------------------------------------------------------------------

        //  Bridges access

        //  feature bridges are monobehaviours residing on this gameObject 
        private IAndroidWifiBridge wifiBridge;
        private IAndroidPlayerBridge playerBridge;
        private IAndroidScreencastBridge castBridge;

        private static List<IAndroidFeatureBridge> bridges;

        /// @brief
        /// access any available bridge
        ///
        public static TBridge GetBridge<TBridge>() where TBridge : class, IAndroidFeatureBridge
        {
            if(bridges != null)
            {
                foreach(var bridge in bridges)
                {
                    TBridge t = bridge as TBridge;
                    if(t != null)
                        return t;
                }
            }
            return null;
        }
        public static IAndroidFeatureBridge GetBridge(DeviceFeature feature)
        {
            foreach(var bridge in bridges)
            {
                if(bridge.mFeature.hasFeature(feature))
                {
                    return bridge;
                }
            }
            return null;
        }

        public static IAndroidWifiBridge Wifi 
        {
            get { 
                return _instance != null ? _instance.wifiBridge : null;
            }
        }
        public static IAndroidPlayerBridge Player 
        {
            get { 
                return _instance != null ? _instance.playerBridge : null;
            }
        }
        public static IAndroidScreencastBridge Screencast 
        {
            get { 
                if(_instance == null) Debug.LogError("BRIDGE IS NULL!");
                return _instance != null ? _instance.castBridge : null;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  Singleton

        public static IAndroidBridge Instance { get { return _instance; } }
        private static AndroidBridge _instance;

        protected virtual void Awake()
        {
            if(Instance != null)
            {
                Component.Destroy(this);
            }
            else
            {
                _instance = this;
            }
        }

        protected virtual void Start() {}
        

        //-----------------------------------------------------------------------------------------------------------------

        //  Init

        public virtual void StartBridge(DeviceFeature capabilities, string fullActivityName=AndroidHelper.UnityActivityName, IPermissionsRationale permissionsRationale=null, System.Action whenReady=null)
        {
            if(!wasStarted)
            {
                _initBridge(capabilities, fullActivityName);
                wasStarted = true;

                ActivateFeature(capabilities, permissionsRationale, whenReady);
            }
        }
        
        //-----------------------------------------------------------------------------------------------------------------

        //  Feature Management

        public bool isFeatureProvided(DeviceFeature feature)
        {
            return allFeatures.hasFeature(feature);
        }

        public bool isFeatureEnabled(DeviceFeature feature)
        {
            return enabledFeatures.hasFeature(feature);
        }

        public bool hasFeaturePermissions(DeviceFeature features)
        {
            var permissionsNeeded = new HashSet<string>();
            foreach(var feature in features.GetAll())
            {
                foreach(var permission in feature.GetNeededPermissions())
                {
                    permissionsNeeded.Add(permission);
                }
            }
            foreach(var permission in permissionsNeeded)
            {
                if(!Permissions.hasPermission(permission))
                {
                    return false;
                }
            }
            return true;
        }

        /// @brief
        /// activate a certain/multiple features defined at the start of the bridge. 
        /// This may trigger permission requests which can be handled via the provided IPermissionsRationale interface.
        ///
        public virtual void ActivateFeature(DeviceFeature feature, IPermissionsRationale rationale=null, System.Action whenDone=null)
        {
            feature |= ~DeviceFeature.SCREEN_CAST;
            if(debug)
            {
                Debug.Log("AndroidBridge enable feature=[" + feature.GetName() + "]");
            }
            DeviceFeature toEnable = DeviceFeature.None;
            foreach(var f in feature.GetAll())
            {
                if(allFeatures.hasFeature(f) && !enabledFeatures.hasFeature(f))
                {
                    toEnable |= f;
                }
            }
            
            if(toEnable != DeviceFeature.None)
            {
                StartCoroutine(_activateFeatureInternal(toEnable, rationale, whenDone));
            }
        }
        /// @brief
        /// deactivate a certain/multiple features of the bridge if it is no longer needed.
        ///
        public virtual void DeactivateFeature(DeviceFeature feature)
        {
            foreach(var f in feature.GetAll())
            {
                if(allFeatures.hasFeature(f) && enabledFeatures.hasFeature(f))
                {
                    Command cmd = new Command(Commands.DISABLE_FEATURE, f.GetName());
                    SendCommand(cmd);
                    enabledFeatures |= ~f;
                }
            }
        }


        private IEnumerator _activateFeatureInternal(DeviceFeature features, IPermissionsRationale rationale, System.Action whenDone)
        {   
            DeviceFeature toEnable = features;
            foreach(var permission in features.GetNeededPermissions())
            {
                if(debugPermissions)
                {
                    Debug.Log("AndroidBridge::checkPermission=[" + permission + "]  already granted?=" + (Permissions.hasPermission(permission)));
                }

                if(!Permissions.hasPermission(permission))
                {
                    isWaitingForPermissions = true;
                    yield return StartCoroutine(Permissions.requestPermission(permission, null, rationale));
                    isWaitingForPermissions = false;
                    
                    if(!Permissions.hasPermission(permission))
                    {
                        //  not granted, disable all features that need this permission
                        Debug.LogWarning("AndroidBridge:: permission=[ " + permission + "] not granted! Disable features=[" + toEnable.FilterByPermission(permission).GetName(true) + "]");
                        toEnable &= ~toEnable.FilterByPermission(permission);
                        if(toEnable == DeviceFeature.None)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if(debugPermissions)
                        {
                            Debug.Log("AndroidBridge:: now has permission=[" + permission + "]");
                        }
                    }
                }
            }

            foreach(var feature in toEnable.GetAll())
            {
                if(debug)
                {
                    Debug.Log("AndroidBridge enable feature NOW=[" + feature.GetName() + "]");
                }
                Command cmd = new Command(Commands.ENABLE_FEATURE, feature.GetName());
                SendCommand(cmd);

                enabledFeatures |= feature;
            }
            firstActivation = true;

            //  callback
            whenDone?.Invoke();
        }



        //-----------------------------------------------------------------------------------------------------------------

        //  Commands

        /// @brief
        /// send custom command data to the android-side of the application
        ///
        public void SendCommand(IDeviceCommand cmd, AndroidCommandCallback callback=null)
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                if(cmd != null && !string.IsNullOrEmpty(cmd.CMD))
                {
                    bool routed = false;
                    if(playerBridge != null && playerBridge.RouteCommand(cmd, callback))
                    {
                        routed = true;
                    }

                    if(!routed)
                    {
                        if(debug)
                        {
                            Debug.Log("AndroidBridge.SendCommand<" + cmd.UID + ">[" + cmd.CMD + " {" + cmd.content + "}]  ready? " + readyForCommands);
                        }
                        
                        if(readyForCommands)
                        {
                            string json = cmd.ToJson();
                            bridgeObject.Call(FUNC_RECEIVE_COMMAND, json, callback);
                        }
                        else if(!commandCache.Exists(x=> x.cmd==cmd))
                        {
                            commandCache.Add(new CachedCommand(cmd, callback));
                        }
                    }
                    else
                    {
                        Debug.Log("AndroidBridge.SendCommand<" + cmd.UID + ">[" + cmd.CMD + "] ROUTED by PlayerBridge!");
                    }
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  listeners

        /// @brief
        /// start to listen to a certain feature of the bridge.
        ///
        public void AddListener<TListener>(TListener listener) where TListener : class, IDeviceBridgeListener
        {
            ObserverManager.AddObserver<TListener>(listener, this);
        }

        /// @brief
        /// stop listening to a certain feature of the bridge.
        ///
        public void RemoveListener(IDeviceBridgeListener listener)
        {
            ObserverManager.RemoveObserver(listener, this);
        }

        public void AddObserver<TObserver>(TObserver o) where TObserver: class, IManagedObserver
        {
            ObserverManager.AddObserver<TObserver>(o, this);
        }
        public void RemoveObserver<TObserver>(TObserver o) where TObserver: class, IManagedObserver
        {
            ObserverManager.RemoveObserver<TObserver>(o, this);
        }

        /// @brief
        /// called by feature bridges to message registered observers.
        ///
        public void MessageObservers<TListener>(System.Action<TListener> action) 
            where TListener : class, IDeviceBridgeListener
        {
            var manager = ObserverManager.Get<TListener>();
            if(manager != null)
            {
                manager.FireEvent(this, action);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  init

        private void _initBridge(DeviceFeature capabilities, string fullActivityName)
        {
            listeners = new List<IDeviceBridgeListener>();


            //  get main activity  
            if(Application.platform == RuntimePlatform.Android)
            {
                var mainActivityClass = new AndroidJavaClass(fullActivityName);
                Assert.IsNotNull(mainActivityClass, "AndroidBridge: activity of type=[" + fullActivityName + "] cannot be found, check your android plugin classes!");

                mainActivity = mainActivityClass.GetStatic<AndroidJavaObject>(MAINACTIVITY_SINGLETON_ACCESS);
                Assert.IsNotNull(mainActivity, "AndroidBridge: main activity must have public static instance member named <" + MAINACTIVITY_SINGLETON_ACCESS + ">");

                bridgeObject = mainActivity.Call<AndroidJavaObject>(MAINACTIVITY_BRIDGE_ACCESS);
                Assert.IsNotNull(bridgeObject, "AndroidBridge: main activity must return a valid UnityBridge AndroidObject, instead returned null!");
            }

            //  set features
            allFeatures = capabilities;
            enabledFeatures = DeviceFeature.None;


            //  get feature bridges
            bridges = new List<IAndroidFeatureBridge>();
            var b = GetComponents<FeatureBridge>();
            foreach(var script in b)
            {
                var bridge = script as IAndroidFeatureBridge;
                if(debug)
                {
                    Debug.Log("FeatureBridge<" + script.name + ">..." + (bridge != null ? bridge.GetType().ToString() : "(null)"));
                }

                if(bridge != null)
                {
                    if(bridge is IAndroidWifiBridge)
                    {
                        wifiBridge = bridge as IAndroidWifiBridge;
                    }
                    if(bridge is IAndroidScreencastBridge)
                    {
                        castBridge = bridge as IAndroidScreencastBridge;
                    }
                    if(bridge is IAndroidPlayerBridge)
                    {
                        playerBridge = bridge as IAndroidPlayerBridge;
                    }

                    script.Init(this);
                    bridges.Add(bridge);
                }
            }

            //  check features
            if(wifiBridge == null)
            {
                allFeatures &= ~(DeviceFeature.WIFI_ACCESS | DeviceFeature.WIFI_SCAN_ACCESS);
            }
            if(castBridge == null)
            {
                allFeatures &= ~DeviceFeature.SCREEN_CAST;
            }
            _bridgebaseReady = true;


            //  set android messaging object to own GameObject
            if(Application.platform == RuntimePlatform.Android)
            {   
                Debug.Log("AndroidBridge:: SET MESSAGE OBJ");
                Command cmd = new Command(Commands.INTERNAL_SET_MESSAGE_OBJECT, transform.name);
                bridgeObject.Call(FUNC_RECEIVE_COMMAND, cmd.ToJson());

                //  plugin debug flag command
                if(debugPlugin)
                {
                    commandCache.Add(new CachedCommand(new Command(Commands.INTERNAL_ENABLE_BRIDGE_DEBUG), null));
                }
                else
                {
                    commandCache.Add(new CachedCommand(new Command(Commands.INTERNAL_DISABLE_BRIDGE_DEBUG), null));
                }

                //  wifi status update command
                commandCache.Add(new CachedCommand(new Command(Commands.WIFI_STATUS_UPDATE), null));
                StartCoroutine("waitUntilReadyForCommandCache");
            }
        }


        IEnumerator waitUntilReadyForCommandCache()
        {
            while(!wasStarted && !isReady && !firstActivation)
            {
                yield return null;
                if(debug)
                    Debug.Log("waitUntilReadyForCommandCache()...");
            }

            if(debug)
                Debug.Log("AndroidBridge ----> SEND CACHED MESSAGES");
            
            readyForCommands = true;

            //  send cached messages
            while(commandCache.Count > 0)
            {
                SendCommand(commandCache[0].cmd, commandCache[0].callback);
                commandCache.RemoveAt(0);
            }
        }




        private struct CachedCommand
        {
            public IDeviceCommand cmd;
            public AndroidCommandCallback callback;
            public CachedCommand(IDeviceCommand cmd, AndroidCommandCallback callback)
            {
                this.cmd = cmd;
                this.callback = callback;
            }
        }

    }

}

