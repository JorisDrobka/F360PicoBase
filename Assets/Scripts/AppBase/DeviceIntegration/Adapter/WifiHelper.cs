using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.Serialization;

using DeviceBridge.Serialization;

namespace DeviceBridge.Wifi
{

    public static class WifiConstants
    {
        public const int ERROR_CONNECTION_TIMEOUT = 1;
        public const int ERROR_WIFI_NOT_FOUND = 2;
        public const int ERROR_CONNECTION_LOST = 3;
        public const int CONNECTED_TO_OTHER = 4;
    
        /// @cond PRIVATE
        public static string ReadableWifiError(int code)
        {
            switch(code)
            {
                case ERROR_CONNECTION_LOST:     return "Connection Lost";
                case ERROR_CONNECTION_TIMEOUT:  return "Connection Timeout";
                case ERROR_WIFI_NOT_FOUND:      return "Wifi not found";
                case CONNECTED_TO_OTHER:        return "Connected to other Network";
                default:                        return "UnknownWifiCode";
            }
        }
        /// @endcond

    }




    //-----------------------------------------------------------------------------------------------------------------
    //
    //  Runtime Helper
    //
    //-----------------------------------------------------------------------------------------------------------------

    public class WifiListener : IWifiStateListener
    {
        public bool isActive { get; private set; }

        public event System.Action<bool> changedWifiState;
        public event System.Action<string> connected;
        public event System.Action<int> disconnected;
        public event System.Action<int> updateRssi;

        public WifiListener()
        {
            Enable();
        }

        public bool Enable() 
        {
            if(DeviceAdapter.Instance != null)
            {
                DeviceAdapter.Instance.AddListener<IWifiStateListener>(this);
                isActive = true;
                return true;
            }
            else
            {
                Debug.LogWarning("Wifilistener could not be enabled, no DeviceAdapter instance was found!");
                return false;
            }
        }
        public void Disable()
        {
            if(isActive)
            {
                DeviceAdapter.Instance.RemoveListener(this);
                isActive = false;
            }
        }

        void IWifiStateListener.OnChangedWifiState(bool state)
        {
            changedWifiState?.Invoke(state);
        }
        void IWifiStateListener.OnConnectedToWifi(string ssid)
        {
            connected?.Invoke(ssid);
        }
        void IWifiStateListener.OnDisconnectedFromWifi(int reason)
        {
            disconnected?.Invoke(reason);
        }   
        void IWifiStateListener.OnUpdatedRSSI(int rssi)
        {
            updateRssi?.Invoke(rssi);
        }
    }




    //-----------------------------------------------------------------------------------------------------------------
    //
    //  WIFI Serializable Data
    //  
    //-----------------------------------------------------------------------------------------------------------------

    public struct WifiAccessPoint : IWifiAccessPoint
    {
        public string ssid;
        public int connectivity;

        string IWifiAccessPoint.ssid { get { return ssid; } }
        int IWifiAccessPoint.connectivity{ get { return connectivity; } }

        public bool Equals(IWifiAccessPoint other)
        {
            return !ReferenceEquals(other, null) && other.ssid == ssid;
        }

        public WifiAccessPoint(string ssid, int connectivity)
        {
            this.ssid = ssid;
            this.connectivity = connectivity;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------
    
    [DataContract]
    public struct WifiConfig : ISerializableData
    {
        [DataMember(Order=0, EmitDefaultValue=false)]
        public string ssid;
        [DataMember(Order=1, EmitDefaultValue=false)]
        public string pass;

        public string ToJson()
        {
            return JsonSerializationHelper.SafeSerialization(this);
        }

        public static WifiConfig Parse(string json)
        {
            WifiConfig config;
            JsonSerializationHelper.TryParse<WifiConfig>(json, out config);
            return config;
        }

        void ISerializableData.onBeforeSerialize() {}
        void ISerializableData.onAfterDeserialize() {}
    }

    //-----------------------------------------------------------------------------------------------------------------

    [DataContract]
    public class WifiListInfo : SerializableData, IWifiList
    {
    
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string[] ssids;
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public int[] connectivities;
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string lastUpdateTime;

        public List<WifiAccessPoint> _accessPoints;


        public int Count 
        {
            get { return ssids != null ? ssids.Length : 0; }
        }

        public WifiListInfo()
        {
            ssids = new string[0];
            connectivities = new int[0];
            _accessPoints = new List<WifiAccessPoint>();
            lastUpdateTime = "";
        }

        public bool UpdateFromJson(string json)
        {
            WifiListInfo list;
            if(JsonSerializationHelper.TryParse<WifiListInfo>(json, out list))
            {
                this.ssids = list.ssids;
                this.connectivities = list.connectivities;
                this.lastUpdateTime = list.lastUpdateTime;

                UnityEngine.Assertions.Assert.IsTrue(ssids.Length==connectivities.Length, "WifiList: ssid array does not match connectivies array!");

                _accessPoints.Clear();
                for(int i = 0; i < ssids.Length; i++)
                {
                    _accessPoints.Add(new WifiAccessPoint(ssids[i], connectivities[i]));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        IEnumerable<IWifiAccessPoint> IWifiList.GetAccessPoints()
        {
            for(int i = 0; i < _accessPoints.Count; i++) {
                yield return _accessPoints[i];
            }
        }
    }

    //-----------------------------------------------------------------------------------------------------------------


    [DataContract]
    public class WifiState : SerializableData
    {
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string ssid;
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public int connectivity;
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string timeConnected;

        public bool isConnected()
        {
            return !string.IsNullOrEmpty(ssid);
        }

        public void UpdateFromJson(string json)
        {
            WifiState state;
            if(SerializableData.TryParse<WifiState>(json, out state))
            {
                this.ssid = state.ssid;
                this.connectivity = state.connectivity;
                this.timeConnected = state.timeConnected;
            }
        }
    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  ScreenCast Serializable Data
    //  
    //-----------------------------------------------------------------------------------------------------------------

    
    public enum ScreencastRouteState      ///         ( taken from PicoPlugin constants )
    {
        NONE=0,
        SCANNING = 1,
        CONNECTING = 2,
        AVAILABLE = 3,
        NOT_AVAILABLE = 4,        
        IN_USE = 5,          //  other device streams on route already
        CONNECTED = 6,
    }
    

    public static class ScreencastConstants
    {
        public static string Readable(this ScreencastRouteState state)
        {
            switch(state)
            {
                case ScreencastRouteState.AVAILABLE:        return "offen";
                case ScreencastRouteState.NOT_AVAILABLE:    return "nicht verfügbar";
                case ScreencastRouteState.CONNECTING:       return "verbindet";
                case ScreencastRouteState.IN_USE:           return "bereits in Verwendung";
                case ScreencastRouteState.CONNECTED:        return "verbunden";
                default:                                    return "";
            }
        }
    }

    [DataContract]
    public class ScreencastListInfo : SerializableData, ICastRouteList
    {
        const string PLAYERPREF_HOME_DISPLAY = "HOME_DISPLAY";

        public List<ScreencastRouteInfo> displays = new List<ScreencastRouteInfo>();

        public int Count { get { return displays.Count; } }

        public ScreencastRouteInfo GetCurrent()
        {
            return GetByName(currentDisplay);
        }

        public ScreencastRouteInfo GetLast()
        {
            return GetByName(lastDisplay);
        }


        public bool SetDefault(string displayID)
        {
            var display = GetByName(displayID);
            if(display != null)
            {
                PlayerPrefs.SetString(PLAYERPREF_HOME_DISPLAY, displayID);
                return true;
            }
            return false;
        }

        public ScreencastRouteInfo GetDefault()
        {
            var name = PlayerPrefs.GetString(PLAYERPREF_HOME_DISPLAY, "");
            if(!string.IsNullOrEmpty(name))
            {
                return GetByName(name);
            }
            return ScreencastRouteInfo.None;
        }

        public ScreencastRouteInfo GetByName(string displayID)
        {
            for(int i = 0; i < displays.Count; i++)
            {
                if(displays[i].name == displayID) return displays[i];
            }
            return new ScreencastRouteInfo();
        }
        public ScreencastRouteInfo GetByAddress(string address)
        {
            for(int i = 0; i < displays.Count; i++)
            {
                if(displays[i].address == address) return displays[i];
            }
            return new ScreencastRouteInfo();
        }

        public bool isDefaultAvailable()
        {
            var d = GetDefault();
            return d != null && d.state == ScreencastRouteState.AVAILABLE;
        }
        public bool isDefaultConnected()
        {
            var d = GetDefault();
            return d != null && d.state == ScreencastRouteState.CONNECTED;
        }

        [DataMember] bool isEnabled;
        [DataMember] string[] availableDisplays;
        [DataMember] string[] addresses;
        [DataMember] int[] states;
        [DataMember] string currentDisplay;
        [DataMember] string lastDisplay;


        ICastRoute ICastRouteList.Get(int index)
        {
            if(index >= 0 && index < displays.Count)
            {
                return displays[index];
            }
            return null;
        }
        IEnumerable<ICastRoute> ICastRouteList.GetDevices()
        {
            foreach(var d in displays) yield return d;
        }

        public bool UpdateFromJson(string json)
        {
            ScreencastListInfo list;
            if(JsonSerializationHelper.TryParse<ScreencastListInfo>(json, out list))
            {
                availableDisplays = list.availableDisplays;
                addresses = list.addresses;
                states = list.states;
                currentDisplay = list.currentDisplay;
                lastDisplay = list.lastDisplay;
                displays.Clear();
                displays.AddRange(list.displays);
                return true;
            }
            return false;
        }

        protected override void onAfterDeserialize()
        {
            if(availableDisplays != null)
            {
                UnityEngine.Assertions.Assert.IsTrue(availableDisplays.Length == addresses.Length, " ScreencastListInfo:: available displays length must match addresses length!");
                for(int i = 0; i < availableDisplays.Length; i++)
                {
                    if(!string.IsNullOrEmpty(availableDisplays[i]))
                    {
                        var info = new ScreencastRouteInfo();
                        info.name = availableDisplays[i];
                        info.address = addresses[i];
                        info.state = (ScreencastRouteState)states[i];
                        info.isConnected = availableDisplays[i] == currentDisplay;
                        displays.Add(info);
                    }
                }
            }
        }

        

        public override string ToString()
        {
            var b = new System.Text.StringBuilder("ScreencastState\n[");
            for(int i = 0; i < Count; i++)
            {
                b.Append("\t\t\n" + i.ToString() + ": " + this.displays[i].name + "(" + this.displays[i].address + ")");
            }
            b.Append("\n]");
            return b.ToString();
        }
    }


    
    public struct ScreencastRouteInfo : ICastRoute
    {

        public static ScreencastRouteInfo None { get { return _none; } }

        public string name;
        public string address;
        public bool isConnected;
        public ScreencastRouteState state;

        string ICastRoute.displayID { get { return name; } }
        string ICastRoute.address { get { return address; } }
        bool ICastRoute.isAvailable { get { return state == ScreencastRouteState.AVAILABLE; } }
        bool ICastRoute.isConnected { get { return isConnected; } }

        public bool isValid() { return !string.IsNullOrEmpty(name); }


        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null))
            {
                if(obj is ScreencastRouteInfo)
                {
                    return Equals((ScreencastRouteInfo)obj);
                }
            }
            return false;
        }
        public bool Equals(ScreencastRouteInfo other)
        {
            return other.name == name
                && other.address == address
                && other.isConnected == isConnected;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                if(isValid())
                {
                    int hash = name.GetHashCode() ^ 7;
                    hash += address != null ? address.GetHashCode() ^ 13 : 0;
                    hash += isConnected ? 937 : 0;
                    return hash;
                } 
                else
                {
                    return -1;
                }
            }
        }

        public static bool operator==(ScreencastRouteInfo a, ScreencastRouteInfo b)
        {
            return a.Equals(b);
        }
        public static bool operator!=(ScreencastRouteInfo a, ScreencastRouteInfo b)
        {
            return !a.Equals(b);
        }

        static ScreencastRouteInfo _none = new ScreencastRouteInfo();
    }


}
