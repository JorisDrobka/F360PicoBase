using System.Collections;
using System.Collections.Generic;

using DeviceBridge.Internal;

namespace DeviceBridge.Android
{

    //-----------------------------------------------------------------------------------------------------------------
    //
    //  SENDER
    //  
    //  the Unity-side interface to communicate with 
    //  the Android portion of the system.


    public interface IAndroidBridge
    {
        bool isReady { get; }
        bool isWaitingForPermissions { get; }

        ///<summary>
        /// call this to initialize all needed resources for the bridge to work
        ///</summary>
        void StartBridge(DeviceFeature capabilities, string fullActivityName=AndroidHelper.UnityActivityName, IPermissionsRationale rationale=null, System.Action whenReady=null);

        ///<summary>
        /// activate android-side services (BroadcastReceivers etc.)
        ///</summary>
        void ActivateFeature(DeviceFeature c, IPermissionsRationale rationale=null, System.Action whenReady=null);
        ///<summary>
        /// deactivate android-side services when not needed.
        ///</summary>
        void DeactivateFeature(DeviceFeature c);
        ///<summary>
        /// checks if a feature can be used in current enviromment.
        ///</summary>
        bool isFeatureProvided(DeviceFeature c);
        ///<summary>
        /// checks wether a feature is currently enabled in current environment
        ///</summary>
        bool isFeatureEnabled(DeviceFeature c);
        ///<summary>
        /// returns wether needed permissions of given feature are granted
        ///</summary>
        bool hasFeaturePermissions(DeviceFeature features);
        

        ///<summary>
        /// send a generic command to be interpreted by the android side.
        ///</summary>
        void SendCommand(IDeviceCommand cmd, AndroidCommandCallback callback=null);

        ///<summary>
        /// start listening to events from the bridge.
        ///</summary>
        void AddListener<TListener>(TListener listener) where TListener : class, IDeviceBridgeListener;
        
        ///<summary>
        /// stop listening to events from the bridge
        ///</summary>
        void RemoveListener(IDeviceBridgeListener listener);
    } 



    


    ///<summary>
    /// a bridge implementing a specific android feature.
    ///</summary>
    public interface IAndroidFeatureBridge
    {
        DeviceFeature mFeature { get; }
    }


    //  REFACTOR::: remove the following intermediary interface

    public interface IAndroidWifiBridge : IAndroidFeatureBridge, IWifiBridge
    {
 /*       bool isConnected();
        string GetSSID();
        int GetRSSI(); */
    }

    public interface IAndroidScreencastBridge : IAndroidFeatureBridge, IScreencastBridge
    {
/*         bool isConnected();
        string GetDevice();
        int GetRSSI(); */
    }

    public interface IAndroidPlayerBridge : IAndroidFeatureBridge, IPlayerBridge
    {
/*         bool isPaused();
            int GetBatteryLevel(); */         //  in percent

        /// @TODO push to internal bridge 
        ///
        /// @brief
        /// bridge may want to handle command differently.
        /// @returns wether brigde handled command.
        ///
        bool RouteCommand(IDeviceCommand cmd, AndroidCommandCallback callback=null);
    }


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  LISTENERS

    


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  Types

/*     public interface IDeviceCommand
    {
        string CMD { get; }
        string meta { get; }

        string ToJson();
    }

    public interface IWifiAccessPoint
    {
        string ssid { get; }
        int connectivity { get; }
        bool Equals(IWifiAccessPoint other);
    }

    public interface IWifiList
    {
        IEnumerable<IWifiAccessPoint> GetAccessPoints();
    }
*/
    public interface IMiracastDevice : ICastRoute
    {
//        string identifier { get; }
    }

    public interface IMiracastDeviceList : ICastRouteList
    {
        IEnumerable<IMiracastDevice> GetMiracastDevices();
    }


    //-----------------------------------------------------------------------------------------------------------------




    
}