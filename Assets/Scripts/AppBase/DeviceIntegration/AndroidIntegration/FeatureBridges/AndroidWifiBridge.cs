using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeviceBridge.Wifi;

namespace DeviceBridge.Android.Internal
{


    public class AndroidWifiBridge : FeatureBridge, IAndroidWifiBridgeInternal
    {
        public override DeviceFeature mFeature { 
            get { 
                return DeviceFeature.WIFI_ACCESS | DeviceFeature.WIFI_SCAN_ACCESS; 
            }
        }
    
        private WifiListInfo scanList = new WifiListInfo();
        private bool wifiState = false;
        private string currentSSID = "";
        private int lastRSSI = 0;

        protected override void Awake()
        {
            base.Awake();

            //  register base callback types
            Utility.ManagedObservers.ObserverManager.RegisterObserverType<IWifiStateListener>();
            Utility.ManagedObservers.ObserverManager.RegisterObserverType<IWifiAPListener>();
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  frontend

        public bool isEnabled()
        {
            return wifiState;
        }

        public bool isConnected()
        {
            return wifiState && !string.IsNullOrEmpty(currentSSID) && Application.internetReachability != NetworkReachability.NotReachable;
        }

        public string GetSSID()
        {
            return currentSSID;
        }
        public int GetRSSI()
        {
            return lastRSSI;
        }

        

        //-----------------------------------------------------------------------------------------------------------------

        //  backend

        protected override void OnInitializeBridge(AndroidBridge main)
        {
            if(debug)
            {
                Debug.Log("Initialize WifiBridge... wifistate=[" + wifiState + "] scanList=[" + scanList.Count + "] ssid=[" + currentSSID + "]");
            }
            
            //  if we have changed state before initialization, send current state now
            if(wifiState)
            {
                SendMessageToObservers<IWifiStateListener>(x=> x.OnChangedWifiState(true));
            }
            if(scanList.Count > 0)
            {
                var list = scanList as IWifiList;
                SendMessageToObservers<IWifiAPListener>(x=> x.OnUpdatedWifiAPList(list));
            }
            if(!string.IsNullOrEmpty(currentSSID))
            {
                SendMessageToObservers<IWifiStateListener>(x=> x.OnConnectedToWifi(currentSSID));
                if(lastRSSI > 0)
                {
                    SendMessageToObservers<IWifiStateListener>(x=> x.OnUpdatedRSSI(lastRSSI));
                }
            }
        }


        public virtual void OnWifiStateChange(string state)
        {
            bool parsed;
            if(parseBoolean(state, out parsed))
            {
                if(debug)
                {
                    Debug.Log("WifiBridge.OnWifiStateChange=[" + parsed + "]");
                }
                wifiState = parsed;
                if(isInitialized)
                {
                    SendMessageToObservers<IWifiStateListener>(x=> x.OnChangedWifiState(parsed));
                }                
            }
        }

        IEnumerator waitForInternetConnection(string ssid)
        {
            while(!isInitialized && Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("waits for internet <" + ssid  + ">");
                yield return new WaitForSeconds(0.5f);
                
            }
            Debug.Log("WIFI BRIDGE NOW Reachable!");
            SendMessageToObservers<IWifiStateListener>(x=> x.OnConnectedToWifi(ssid));
        }

        public virtual void OnConnectedToWifi(string ssid)
        {
            Debug.Log("WifiBridge.onBeforeConnected with=[" + ssid + "] / " + Application.internetReachability);
            if(!string.IsNullOrEmpty(ssid))
            {
                if(debug)
                {
                    Debug.Log("--------------> WifiBridge.OnConnectedWifi=[" + ssid + "] " + isInitialized + "\n\treachability: " + Application.internetReachability);
                }

                currentSSID = ssid;
        //        StopCoroutine("waitForInternetConnection");
        //        StartCoroutine("waitForInternetConnection", ssid);
                if(isInitialized)
                {
                    SendMessageToObservers<IWifiStateListener>(x=> x.OnConnectedToWifi(ssid));
                }               
            }
        }

        public virtual void OnDisconnectedFromWifi(string reason)
        {
            int parsed;
            if(parseInteger(reason, out parsed))
            {
                if(debug)
                {
                    Debug.Log("WifiBridge.OnDisconnectedWifi=[" + WifiConstants.ReadableWifiError(parsed) + "]");
                }
                StopCoroutine("waitForInternetConnection");

                currentSSID = "";
                if(isInitialized)
                {
                    SendMessageToObservers<IWifiStateListener>(x=> x.OnDisconnectedFromWifi(parsed));
                }
            }
            else
            {
                Debug.LogWarning("failed to parse disconnect reason code [" + reason + "]");
            }
        }


        public virtual void UpdatedWifiList(string json)
        {
            if(scanList.UpdateFromJson(json))
            {
                if(debug)
                {   
                    Debug.Log("WifiBridge.UpdatedWifiList=[" + scanList.ssids.Length + "] initialized? " + isInitialized);
                }   
                var list = scanList as IWifiList;
                if(isInitialized)
                {
                    SendMessageToObservers<IWifiAPListener>(x=> x.OnUpdatedWifiAPList(list));
                    this.mainBridge.SendCommand(new Command(Commands.WIFI_STATUS_UPDATE));
                }
            }
        }

        public virtual void UpdateRSSI(string level)
        {
            int parsed;
            if(parseInteger(level, out parsed))
            {
                if(debug)
                {
                    Debug.Log("WifiBridge.UpdateRSSI=[" + parsed + "]");
                }

                lastRSSI = parsed;
                if(isInitialized)
                {
                    SendMessageToObservers<IWifiStateListener>(x=> x.OnUpdatedRSSI(parsed));
                }
            }
            
        }
    }

}

