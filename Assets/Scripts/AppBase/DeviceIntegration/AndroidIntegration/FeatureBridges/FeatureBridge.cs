using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeviceBridge.Internal;

namespace DeviceBridge.Android.Internal
{

    //  RECEIVER
    //
    //  all functions are called from Android via the Message system,
    //  so Interfaces must be implemented by a Monobehaviour residing
    //  on a GameObject named "AndroidBridge".


    /// @brief 
    /// Android state messages send by Android Player Activity
    ///
    ///
    public interface IAndroidPlayerBridgeInternal : IAndroidPlayerBridge
    {
        void OnApplicationSleep();
        void OnApplicationWake();
        /// @brief
        /// Called when application is stopped android-wise. parameter=[ int ]
        ///
        void OnApplicationStop();

        /// @brief
        /// Receive a Command json object to interpret. parameter=[ Android.Command ]
        ///
        void OnReceiveCommand(string command);
        /// @brief
        /// Receive a response from plugin, following a command. parameter=[ Android.Response ]
        ///
        void OnReceiveResponse(string response);
        ///<summary>
        /// called when battery level changes parameter=[ int ]
        ///</summary>
        void OnUpdatedBatteryLevel(string level);
    }



    /// @brief
    /// Wifi state messages send by Android Plugin
    ///
    ///
    public interface IAndroidWifiBridgeInternal : IAndroidWifiBridge
    {
        ///
        /// Wifi service becomes enabled / disabled parameter=[ bool ]
        ///
        void OnWifiStateChange(string state);
        ///
        /// Connected to a new Wifi.
        ///
        void OnConnectedToWifi(string ssid);
        ///
        /// Disconnected from current Wifi. parameter=[ int ]
        ///
        void OnDisconnectedFromWifi(string reason);
        ///
        /// The WifiList was updated. parameter=[ DeviceBridge.WifiListInfo ]
        ///
        void UpdatedWifiList(string list);
        ///
        /// Receive the current wifi signal strength. parameter=[ int ]
        ///
        void UpdateRSSI(string level);
    }

    public interface IAndroidScreencastBridgeInternal : IAndroidScreencastBridge
    {
        void OnChangedWifiSettings(string state);

        /// @brief
        /// called when connection to cast-ready display is established
        ///
        void OnConnectedDisplay(string displayID);

        /// @brief
        /// called when connection to display ends
        ///
        void OnDisconnectedDisplay(string displayID);

        /// @brief
        /// called when cast routes have changed - parameter is of type Wifi.ScreencastState
        ///
        void OnRefreshAvailableDisplays(string json);

        
        //void OnDisplayStatusUpdate();
    }

    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// Base class for Android feature implementations that reside
    /// on the bridge object.
    ///
    /// @details
    /// Derive from this class to implement an android feature. 
    /// The bridge must implement one of the feature interfaces above 
    /// and also define a capability / android feature it implements.
    /// 
    /// @see @ref dbridge_s1
    ///
    public abstract class FeatureBridge : MonoBehaviour, IAndroidFeatureBridge
    {        
        public bool debug = false;

        /// @brief
        /// The feature/features that are accessed via this bridge. 
        ///
        public abstract DeviceFeature mFeature { get; }

        /// @brief
        /// Check wether the feature is supported in current environment.
        ///
        public bool isFeatureSupported()
        {
            return isInitialized;
        }

        /// @cond PRIVATE
        protected AndroidBridge mainBridge { get; private set; }

        protected bool isInitialized { get; private set; }


        public void Init(AndroidBridge main)
        {
            if(!isInitialized)
            {
                isInitialized = true;
                mainBridge = main;
                OnInitializeBridge(main);
            }
        }   
        protected virtual void Awake() {}
        protected virtual void Start() {}
        
        protected virtual void OnInitializeBridge(AndroidBridge main) {}
        /// @endcond


        /// @brief
        /// Call a specific function on all interested observers registered
        /// in the main bridge.
        ///
        protected void SendMessageToObservers<TObserver>(System.Action<TObserver> e) where TObserver : class, IDeviceBridgeListener
        {
            if(isInitialized)
            {
                mainBridge.MessageObservers(e);
            }
        }

        /// @brief
        /// Creates an error response on this side of the bridge.
        ///
        protected void formatErrorResponse(DeviceCommandCallback callback, string error, string content="")
        {
            if(callback != null)
            {
                callback(Response.CreateWithRandomUID(error, content));
            }
        }
        /// @brief
        /// Creates an error response on this side of the bridge.
        ///
        protected void formatErrorResponseWithUID(DeviceCommandCallback callback, string uid, string error, string content="")
        {
            if(callback != null)
            {
                callback(Response.Create(uid, error, content));
            }
        }


        protected bool parseBoolean(string val, out bool result)
        {
            if(System.Boolean.TryParse(val, out result))
            {
                return true;
            }
            else
            {
     //           Debug.LogWarning("error while parsing boolean");
                return false;
            }
        }
        protected bool parseInteger(string val, out int result)
        {
            if(System.Int32.TryParse(val, out result))
            {
                return true;
            }
            else
            {
        //        Debug.LogWarning("error while parsing integer");
                return false;
            }
        }




    }

}

