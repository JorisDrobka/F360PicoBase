using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeviceBridge.Files;

namespace DeviceBridge.Android
{
    /// @brief
    /// Runtime interface to Android OS.
    ///
    /// @details
    /// The adapter grants access to all common android device features 
    /// such as Wifi, Internet access or writing to storage.
    /// The adapter sets up the bridge automatically, provided the scene contains the AndroidBridge object 
    /// with all needed FeatureBridges.
    ///
    public class AndroidAdapter : DeviceAdapter
    {
        public override IPlayerBridge Player { get { return player; } }
        public override IDeviceStorageBridge Storage { get { return storage; } }
        public override IWifiBridge Wifi 
        { 
            get {
                return wifi;
            } 
        }
        public override IScreencastBridge Screencast { get { return screencast; } }

        AndroidStorageBridge storage;
        IWifiBridge wifi;
        IScreencastBridge screencast;
        IPlayerBridge player;


        /// @cond PRIVATE
        protected IAndroidBridge bridge 
        {
            get {
                return AndroidBridge.Instance;
            }
        }
        

        //-----------------------------------------------------------------------------------------------------------------

        protected readonly string activityName;
        protected readonly DeviceFeature features;
        protected readonly IPermissionsRationale permissionHandler;
        System.Action bridgeStartCallback=null;

        //-----------------------------------------------------------------------------------------------------------------
        /// @endcond



        //  init

        public AndroidAdapter(MonoBehaviour coroutineHost, DeviceFeature features, string activityName=AndroidHelper.UnityActivityName, IPermissionsRationale permissionHandler=null) 
                            : base(coroutineHost)
        {
            this.activityName = activityName;
            this.features = features;
            this.permissionHandler = permissionHandler;
        }

        public override void Start(System.Action whenDone)
        {
            bridgeStartCallback = whenDone;
            storage = new AndroidStorageBridge(this, coroutineHost);
            
            storage.TryAccessExternalStorage(this, onStorageAvailable);
            bridge.StartBridge(features, activityName, permissionHandler, onBridgeStarted);
            player = AndroidBridge.GetBridge<IAndroidPlayerBridge>();
            wifi = AndroidBridge.GetBridge<IAndroidWifiBridge>();
            screencast = AndroidBridge.GetBridge<IAndroidScreencastBridge>();
        }

        //  start sequence

        void onBridgeStarted()
        {
            coroutineHost.StartCoroutine(waitForStorageAccess());
        }
        bool _attemptedStorageAccess = false;

        IEnumerator waitForStorageAccess()
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                while(!_attemptedStorageAccess)
                {
                    yield return null;
                }
            }
            else 
            { 
                yield return null;
            }
            yield return null;
            bridgeStartCallback?.Invoke();
            bridgeStartCallback = null;
        }

        void onStorageAvailable()
        {
            if(storage.isFeatureSupported())
            {   
                Debug.Log("access to android storage granted! storagePath=[" + storage.GetStoragePath(StorageLocation.Device_External) + "]");
            }
            else
            {
                Debug.LogWarning("AndroidAdapter:: Storage unavailable!");
            }
            _attemptedStorageAccess = true;
        }
        

        //-----------------------------------------------------------------------------------------------------------------

        //  interface

        public override bool isReady()
        {
            return bridge != null && bridge.isReady;
        }

        
        public override void SendCommand(string cmd, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null)
        {
            SendCommand(cmd, "", onSuccess, onFailure);
        }
        public override void SendCommand(string cmd, string meta, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null)
        {   
            if(bridge != null)
            {
                AndroidCommandCallback cb = null;
                if(onSuccess != null || onFailure != null)
                {
                     cb = AndroidCommandCallback.Get(onSuccess, onFailure);
                }
                bridge.SendCommand(new Command(cmd, meta), cb);
            }
        }

        //  listeners
        public override void AddListener<TListener>(TListener listener)
        {
//            Debug.Log("-------> Bridge AddListener of type=[" + typeof(TListener) + "]  bridge? " + (bridge != null));
            bridge?.AddListener<TListener>(listener);
        }
        public override void RemoveListener(DeviceBridge.Internal.IDeviceBridgeListener listener)
        {
            bridge?.RemoveListener(listener);
        }


        //  activity access
        public override bool isPaused()
        {
            return player != null ? player.isPaused() : !Application.isPlaying;
        }
        public override int GetBatteryLevel()
        {
            return player != null ? player.GetBatteryLevel() : 100;
        }



        //-----------------------------------------------------------------------------------------------------------------


        //  storage bridge mockup


        class AndroidStorageBridge : DeviceStorageBridge 
        {
            string _externalStoragePath = "";
            string _externalStoragePathOverride = "";
            string _internalStoragePathOverride = "";

            System.Action accessExtStorageCallback;


            protected override string deviceExternalPath 
            {
                get {
                    if(string.IsNullOrEmpty(_externalStoragePathOverride)) return _externalStoragePath;
                    else return _externalStoragePathOverride;
                }
                set { _externalStoragePathOverride = value; }
            }
            protected override string deviceInternalPath 
            {
                get {
                    if(string.IsNullOrEmpty(_internalStoragePathOverride)) return Application.persistentDataPath + "/";
                    else return _internalStoragePathOverride; 
                }
                set { _internalStoragePathOverride = value; }
            }

            AndroidAdapter context;

            public AndroidStorageBridge(AndroidAdapter context, MonoBehaviour host=null) : base(context, host)
            {
                this.context = context;
            }

            public void TryAccessExternalStorage(AndroidAdapter context, System.Action whenDone=null)
            {
                accessExtStorageCallback = null;
                if(string.IsNullOrEmpty(_externalStoragePath))
                {
                    if(Application.isEditor)
                    {
                        //  route to project path
                        _externalStoragePath = Application.dataPath.Remove(Application.dataPath.Length-"Assets/".Length);
                        mState = DeviceStorageState.NoPermission;       
                        whenDone?.Invoke();
                    }
                    else
                    {
                        accessExtStorageCallback = whenDone;
                        context.SendCommand(Commands.GET_SDCARD_PATH, onStoragePathAvailable, onStoragePathUnavailable);
                    }
                }
                else
                {
                    whenDone?.Invoke();
                }
            }

            public override FileLoader GetFileLoader(StorageLocation location)
            {
                if(location == StorageLocation.Device_Internal)
                {
                    return new CustomFileLoader(this);
                }
                else
                {
                    return base.GetFileLoader(location);
                }
            }


            void onStoragePathAvailable(IDeviceResponse response)
            {
                _externalStoragePath = response.content;
                mState = DeviceStorageState.ReadAndWrite;
                accessExtStorageCallback?.Invoke();
                accessExtStorageCallback = null;
            }
            void onStoragePathUnavailable(IDeviceResponse response)
            {
                Debug.Log("AndroidAdapter.DeviceBridge:: Could not access External Device Storage!");

                mState = DeviceStorageState.ReadAndWrite;       //  tmp: set set expectations about access
                accessExtStorageCallback?.Invoke();
                accessExtStorageCallback = null;
            }   


            //  custom loader to handle async private file read
            class CustomFileLoader : FileLoader
            {
                new AndroidStorageBridge context;
                public CustomFileLoader(AndroidStorageBridge context) : base(context) 
                {
                    this.context = context;
                }

                public override bool canLoadImmediate(string folderpath, string filename, string filetype, StorageLocation location)
                {
                    return location != StorageLocation.Device_Internal;
                }

                protected override void handleAsyncLoad(AsyncFileLoadWorker worker)
                {
                    if(worker.location == StorageLocation.Device_Internal)
                    {
                        //  change path as android private app storage does not allow for subdirectories
                        //  use .txt to read/write and convert to json from text.
                        fullPath = worker.fileName + ".txt";     
                        this.context.context.SendCommand(Commands.READ_PRIVATE_FILE, fullPath, onReadPrivateFile, onFailToReadPrivateFile);
                    }
                }

                void onReadPrivateFile(IDeviceResponse response)
                {   
                    Debug.Log("read private file... worker? " + (currentWorker != null));
                    mState = FileAccessState.Success;
                    if(currentWorker != null)
                    {
                        Debug.Log("RESPONSE?? [" + response.content + "]");
                        currentWorker.SetJsonResult(response.content, mState);
                    }
                }

                void onFailToReadPrivateFile(IDeviceResponse response)
                {
                    Debug.LogWarning("AndroidStorageBridge:: Failed to read private file");
                    mState = FileAccessState.File_not_found;
                    if(currentWorker != null)
                    {
                        currentWorker.SetJsonResult(null, mState);
                    }
                }
            }

        } 
    }

}

