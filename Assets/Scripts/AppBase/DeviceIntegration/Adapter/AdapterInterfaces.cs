using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeviceBridge.Files;



/// @page doc_device_bridge DeviceBridge Documentation
///
/// [TOC]
///
/// The device bridge allows a Unity App to communicate with a custom plugin for the running OS / platform.
/// It is designed as a high-level api to decouple application logic from specific device.
///
///
/// @section dbridge_main Using the Devicebridge
///
/// @subsection dbridge_s1 Bridge Structure
/// The bridge consists of a number of frontend interfaces available to the Unity application in the package \a DeviceBridge
/// as well as a number of backend interfaces needed for the bridge to work within \a DeviceBridge.Internal.
/// When developing a plugin for a certain OS or device, the internal interfaces represent available endpoints in the unity application,
/// which can easily be expanded on (see @ref s4)
/// The application communicates via the DeviceAdapter class with the bridge, decoupling it from the actual platform. The adapter
/// provides basic functionality expected from a mobile device, but must provide a fallback or error handling when a certain feature is missing or disabled.
///     
/// App, bridge and plugin must be aligned in terms of available features - meaning all three entities must have an implementation/represenation and fallback/errorhandling at hand.
///     
///
/// @subsection dbridge_s2 Bridge-Plugin Communication
///
/// @subsubsection dbridge_s2_1 Device Callbacks
/// The plugin knowns about certain features of the device representation of the Unity App (for example UnityPlayerActivity on Android) as well as 
/// how Unity handles features of the target OS. All enabled features of the plugin are able to invoke predefined callbacks which are send to 
/// a single Unity GameObject hosting the bridge scripts. The object is set with the Commands.INTERNAL_SET_MESSAGE_OBJECT command when the bridge initializes first.
/// Each part of the bridge residing on this GameObject can then implement a callback interface
/// found within the DeviceBridge.Internal package to receive plugin messages.     
///
/// @subsubsection dbridge_s2_2 Commands
/// IDeviceCommand provides a flexible method to send commands in the form of a string token
/// known to both the Unity app and the plugin as well as string metadata (either plain text or as json).
/// A command send should always yield a IDeviceResponse object to ensure error handling. 
/// The bridge handles unresolved commands (due to parsing errors or missing features in the plugin) generically
/// by catching resulting error responses, though the start point of the command may implement its own handling by sending a callback along.
///
/// 
///
/// @subsection dbridge_s3 Bridge-App Communication
/// The bridge itself consists of implementations of the provided plugin interfaces in the DeviceBridge package, but it does not communicate directly with the app.
/// instead, a DeviceAdapter is used to mediate between both, making the app independent of target OS and running device.
/// The adapter provides basic functionality such as commands and exposes additional feature bridges, for example IWifiBridge or IDeviceStorageBridge.
/// Even if a feature isn't present or enabled on the device, the bridge objects should still be accessible and handle requests within a reasonable range,
/// for example re-routing storage commands to the Unity resource folder when testing inside the editor etc.
/// 
/// Additionally, scripts can implement several listener interfaces to get notified by the plugin's features.
/// This may look as follows:
///     @code
///     
///     using Utility.ManagedObservers;
///
///     public class Example : MonoBehaviour, IBatteryStateListener
///     {
///         void Start() {
///             ObserverManager.AddObserver<IBatteryStateListener>(this);
///         }
///         void IBatteryStateListener.OnUpdatedBatteryLevel(int level) {
///             //...
///         }
///     }
///
///     @endcode
///
/// @subsection dbridge_s4 Implementing additional Features
/// By design, the DeviceAdapter used to communicate with the bridge is restricted to a basic set of functionalities expected from a target device/operating system.
/// To add additional features, you can either define new command tokens & responses that are accepted by your plugin or you can write new callback interfaces 
/// that are addressed within the plugin and send to the bridge object.
///
///
///
namespace DeviceBridge
{

    //  bridges

    //-----------------------------------------------------------------------------------------------------------------

    //  Activity

    /// @brief
    /// Front access to device/app activity state.
    /// 
    public interface IPlayerBridge : Internal.IDeviceBridge
    {
        bool isPaused();
        int GetBatteryLevel();          //  in percent

        bool hasSystemLevelPermission();
        bool GotoActivity(string package, string activity);
        bool GotoWifiSettings();
        bool GotoScreenshareSettings();
        
        bool Exec_SilentInstall(string pathToInstall);
        void Exec_LockScreen();
        void Exec_UnlockScreen();
        bool Exec_AquireWakeLock();
        void Exec_ReleaseWakeLock();
        void Exec_Recenter();
        void Exec_Shutdown();
    }


    /// @brief
    /// A direct callback received from the device plugin.
    ///
    /// @details
    /// Delegate that can be mirrored and invoked within the plugin, allowing easy
    /// direct callbacks from anywhere in the system.
    /// This feature may be a costly implementation depending on target OS, on Android it's free using Unity's [AndroidJavaProxy class](https://docs.unity3d.com/ScriptReference/AndroidJavaProxy.html).
    ///
    public delegate void DeviceCommandCallback(IDeviceResponse response);

    
    /// @brief
    /// Command pattern base to communicate with the device plugin.
    ///  
    /// @details
    /// Command can be any \a json -parseable datastructure and is used
    /// the call individual functions or share data with the device plugin.
    /// Each command must be identifiable by a unique String that is used to 
    /// track the command's lifetime and aquire the correct response.
    ///
    /// The plugin and/or bridge mus also ensure a response is given even
    /// if the command cannot be resolved.
    /// @see @ref  dbridge_s2_2 "DeviceDridge Commands"
    ///
    public interface IDeviceCommand
    {
        string UID { get; }         ///< The unique identifier for each created command.
        
        string CMD { get; }         ///< Standardized command token known to both bridge & plugin.

        /// @brief 
        /// command metadata, either json data or direct string content.   
        /// @details 
        /// Both command creator and endpoint must have knowledge regarding the type of content. 
        /// Json representations must exist on both sides. If the type isn't found, a Commands.ERR_COMMAND_INVALID response is returned.
        string content { get; }     
                
        string ToJson();    ///< Serializes the command to json.
    }

    /// @brief
    /// A response object following a command.
    ///
    /// @details
    /// Each IDeviceCommand must generate a response datastructure with the same unique identifier
    /// to ensure plugin communication remains stable and exceptions are handled.
    /// @see @ref dbridge_s2_2
    ///
    public interface IDeviceResponse
    {
        bool success { get; }   ///< true if command was executed successfully

        string UID { get; }     ///< The unique identifier of the parent command.
        
        string TOKEN { get; }   ///< Standardized response token (similiar to IDeviceCommand.CMD) known to both bridge & plugin.

        string content { get; } ///< Response content, usually an error message a user-defined json content.

        bool isJson();          ///< Check if response content is json-formatted.
    }

    

    //-----------------------------------------------------------------------------------------------------------------

    //  Storage
    
    /// @brief
    /// Frontend access to the running device's internal storage.
    ///
    /// @details
    /// The storage bridge provides functionality to write files into public (SD card) and private (internal storage) locations on the target device
    /// and allows to cache sensible data like local account information and internal app states.
    ///
    public interface IDeviceStorageBridge : Internal.IDeviceBridge
    {
        /// @brief
        /// Current read/write permission state.
        DeviceStorageState GetState();                      

        /// @brief
        /// Absolute storage path to specified location
        ///
        string GetStoragePath(StorageLocation location); 
        /// @brief
        /// Change storage location folders - only works for device_external and device_internal. Be sure to know what you're doing.
        ///
        void SetStoragePath(StorageLocation location, string path);

        /// @brief
        /// Write a file to a private location.
        ///
        void WritePrivateFile(PrivateFile file, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null);
     
        /// @brief
        /// Read a file from a device storage.
        ///
        void ReadPrivateFile(PrivateFile file, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null);
     
        /// @brief
        /// Delete a file from device storage.
        ///
        void DeletePrivateFile(PrivateFile file, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null);

        /// @brief
        /// Retrieve a helper object to load a file.
        ///
        FileLoader GetFileLoader(StorageLocation location);
    }

    

    //-----------------------------------------------------------------------------------------------------------------

    //  Wifi

    /// @brief
    /// Frontend access to WIFI functionality.
    ///
    public interface IWifiBridge : Internal.IDeviceBridge
    {
        bool isEnabled();
        bool isConnected();
        string GetSSID();
        int GetRSSI();
    }

    /// @brief
    /// Representaton of an accessible Wifi.
    /// 
    public interface IWifiAccessPoint
    {
        string ssid { get; }
        int connectivity { get; }
        bool Equals(IWifiAccessPoint other);
    }

    /// @brief
    /// Representation of all currently accessible Wifis.
    ///
    public interface IWifiList
    {
        IEnumerable<IWifiAccessPoint> GetAccessPoints();
    }

    //-----------------------------------------------------------------------------------------------------------------


    //  Screen Casting

    /// @brief
    /// Frontend access to screen cast functionality.
    ///
    public interface IScreencastBridge : Internal.IDeviceBridge
    {
        Wifi.ScreencastRouteInfo CurrentDisplay { get; }
        Wifi.ScreencastRouteInfo DefaultDisplay { get; }


        /// @returns if currently casting to a display
        ///
        bool isConnected();

        /// @brief
        /// force refresh cast route list, tries to enable feature if not available.
        ///
        void SearchDevices();

        /// @brief
        /// try connect to default display
        ///
        void ConnectDefaultDisplay(DeviceCommandCallback callback=null);

        /// @brief
        /// try connect to given display
        ///
        void ConnectDisplay(string displayID, DeviceCommandCallback callback=null);
        
        /// @brief
        /// set the home display for quick connect
        ///
        void SetDefaultDisplay(string displayID, DeviceCommandCallback callback=null);

        void Disconnect(DeviceCommandCallback callback=null);
    }

    /// @brief
    /// Representation of an accessible screen cast/mirror target.
    ///
    public interface ICastRoute
    {
        string displayID { get; }
        string address { get; }
        bool isAvailable { get; }
        bool isConnected { get; }
    }
    /// @brief
    /// Representation of all currently accessible screen cast/mirror targets.
    public interface ICastRouteList
    {
        int Count { get; }
        ICastRoute Get(int index);
        IEnumerable<ICastRoute> GetDevices();
    }    

    //-----------------------------------------------------------------------------------------------------------------

    //  listeners

    /// @brief
    /// Observer that gets notified when the app state on running device changes.
    /// @details @see @ref dbridge_s3
    public interface IAppStateListener : DeviceBridge.Internal.IDeviceBridgeListener
    {
        void OnApplicationSleep();
        void OnApplicationWake();
        void OnApplicationStop(int reason);
        void OnApplicationDestroy();
    }
    /// @brief
    /// Observer that gets notified when the battery state running device changes.
    /// 
    public interface IBatteryStateListener : DeviceBridge.Internal.IDeviceBridgeListener
    {
        void OnUpdatedBatteryLevel(int level);
    }
    /// @brief
    /// Observer that gets notified when a command from the plugin is received.
    /// @details @see @ref dbridge_s3
    public interface ICommandListener : DeviceBridge.Internal.IDeviceBridgeListener
    {
        void OnReceiveCommand(IDeviceCommand cmd);
    }
    /// @brief
    /// Observer that gets notified when the running device's Wifi state changes.
    ///
    /// @details
    /// Implement this interface and add it as listener
    /// @see @ref dbridge_s3 
    ///
    public interface IWifiStateListener : DeviceBridge.Internal.IDeviceBridgeListener
    {
        void OnChangedWifiState(bool state);
        void OnConnectedToWifi(string ssid);
        void OnDisconnectedFromWifi(int reason);
        void OnUpdatedRSSI(int rssi);
    }
    /// @brief
    /// Observer that gets notified when available wifi points change
    /// 
    public interface IWifiAPListener : DeviceBridge.Internal.IDeviceBridgeListener
    {
        void OnUpdatedWifiAPList(IWifiList list);
    }

    /// @brief
    /// Observer that gets notified when available cast devices change
    ///
    public interface ICastAPListener : DeviceBridge.Internal.IDeviceBridgeListener
    {   
        void OnRefreshedAvailableDisplays(ICastRouteList list);
        void OnConnectedDisplay(ICastRoute route);
        void OnConnectionAttemptFailed(ICastRoute route, Wifi.ScreencastRouteState reason);
        void OnDisconnectedDisplay(ICastRoute route);
//        void OnDisplayStateChange(ICastRoute route, Wifi.ScreencastRouteState state);
    }


    //-----------------------------------------------------------------------------------------------------------------

    //  INTERNAL
    
    //  base interfaces are hidden

    namespace Internal
    {
        public interface IDeviceBridge
        {
            bool isFeatureSupported();
        }

        public interface IDeviceBridgeListener : Utility.ManagedObservers.IManagedObserver {}
    }

}