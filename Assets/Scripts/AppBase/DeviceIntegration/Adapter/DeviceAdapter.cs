using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DeviceBridge
{
    /// @brief
    /// DeviceAdapter provides a generic high-level interface to a plugin for any device
    /// this app is running on. 
    ///
    /// @details
    /// @see @ref dbridge_main
    /// 
    ///
    public abstract class DeviceAdapter
    {
        //  startup

        /// @brief
        /// Check if bridge is ready and conditions (i.e. device permissions) are met.
        ///
        public abstract bool isReady();

        /// @brief
        /// Start bridge activity.
        ///
        /// @details
        /// Initializes bridge activity. Call this sometime after Awake().
        /// @attention Make sure Host does not call StopAllCoroutines() from within.
        ///
        /// @param host The Monobehaviour the bridge can run coroutines on.
        /// @param whenDone (optional) Callback when bridge ready.
        public abstract void Start(System.Action whenDone=null);

        //  commands
        
        /// @brief
        /// Send a command to the plugin and receive a response
        ///
        /// @details
        /// Send a single command token to the plugin. Internally, a IDeviceCommand is created, serialized & send via the bridge.
        /// @see @ref IDeviceCommand
        /// 
        /// @param cmd A standardized command token known to both app and plugin.
        /// @param onSuccess A callback fired when operation is successful.
        /// @param onFailure A callback fired when operation fails. If this is null, the bridge will receive a generic error response.
        ///
        public abstract void SendCommand(string cmd, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null);
        /// @brief
        /// Send a command to the plugin and receive a response.
        /// 
        /// @details
        /// Send a command token and additional metadata to the plugin. Metadata can be either plain string or a json object that is mirrored in the plugin.
        /// Internally, a IDeviceCommand is created, serialized & send via the bridge.
        /// @see @ref IDeviceCommand
        /// 
        /// @param cmd A standardized command token known to both app and plugin.
        /// @param meta Additional data send along with the command.
        /// @param onSuccess A callback fired when operation is successful.
        /// @param onFailure A callback fired when operation fails. If this is null, the bridge will receive a generic error response.
        ///
        public abstract void SendCommand(string cmd, string meta, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null);

        //  bridges
        

        public abstract IPlayerBridge Player { get; }
        /// @brief
        /// Bridge to device's Storage capabilities.
        /// 
        public abstract IDeviceStorageBridge Storage { get; }
        /// @brief
        /// Bridge to device's Wifi capabilities.
        ///
        public abstract IWifiBridge Wifi { get; }
        /// @brief
        /// Bridge to device's screen mirroring/casting capabilities.
        ///
        public abstract IScreencastBridge Screencast { get; }


        //  activity access
        
        /// @brief
        /// Returns wether the application is paused device-wise (sleep mode on mobiles).
        ///
        public abstract bool isPaused();
        /// @brief
        /// Returns the battery level in percent, if any.
        ///
        public abstract int GetBatteryLevel();


        //  listeners

        /// @brief
        /// Start to listen to a certain feature of the device.
        ///
        public abstract void AddListener<TListener>(TListener listener) where TListener : class, Internal.IDeviceBridgeListener;
        /// @brief
        /// Stop listening to a device feature.
        ///
        public abstract void RemoveListener(Internal.IDeviceBridgeListener listener);


        protected MonoBehaviour coroutineHost { 
            get { 
                if(_host == null)
                    _host = CoroutineHost.Instance;
                return _host;
            } 
            set { 
                if(value != null)
                    _host = value;
            }
        }
        private MonoBehaviour _host;

        //-----------------------------------------------------------------------------------------------------------------

        //  singleton access
        public static DeviceAdapter Instance { get { return _instance; } }
        private static DeviceAdapter _instance;
 

        public DeviceAdapter(MonoBehaviour host)
        {
            this.coroutineHost = host;
            if(_instance != null)
            {
                Debug.LogError("cannot have two DeviceAdapter instances.. current=[" + _instance.GetType() + "] new=[" + GetType() + "]. discarding this latter...");
            }
            else
            {
                _instance = this;
            }
        }
        
        
    }

}

