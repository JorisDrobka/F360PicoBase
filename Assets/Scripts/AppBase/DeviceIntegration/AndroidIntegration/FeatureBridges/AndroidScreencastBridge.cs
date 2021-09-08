using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeviceBridge.Wifi;

namespace DeviceBridge.Android.Internal
{


    public class AndroidScreencastBridge : FeatureBridge, IAndroidScreencastBridgeInternal
    {

        const float REFRESH_INTERVAL_NO_AVAILABLE_DISPLAYS = 20f;
        const float REFRESH_INTERVAL_NO_DEFAULT = 240f;
        const float REFRESH_INTERVAL_DEFAULT_AVAILABLE = 480f;
        

        public override DeviceFeature mFeature { 
            get { 
                return DeviceFeature.SCREEN_CAST; 
            }
        }

        bool featureState = false;
        ScreencastRouteInfo selectedRoute;

        ScreencastListInfo scanList = new ScreencastListInfo();


        protected override void Awake()
        {
            base.Awake();

            //  register base callback types
            Utility.ManagedObservers.ObserverManager.RegisterObserverType<ICastAPListener>();
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  frontend


        public ScreencastRouteInfo CurrentDisplay { get { return selectedRoute; } }
        public ScreencastRouteInfo DefaultDisplay { get { return scanList.GetDefault(); } }


        public bool isAvailable()
        {
            if(!featureState)
            {
                refreshFeatureState();
            }
            return featureState;
        }

        public bool isConnected()
        {
            return selectedRoute.isValid() && selectedRoute.isConnected;
        }

        /// @brief
        /// Force refresh the list of available displays & enables Screencast if necessary.
        ///
        public void SearchDevices()
        {
            if(debug)
            {
                Debug.Log("ScreencastBridge:: called SearchDevices() \n\tfeature available? " + featureState);
            }
            this.mainBridge.SendCommand(new Command(Commands.SCREENCAST_REFRESH));
        }

        /// @brief
        /// Connect to the set default display, if any.
        ///
        public void ConnectDefaultDisplay(DeviceCommandCallback callback=null)
        {
            if(debug)
            {
                Debug.Log("ScreencastBridge:: connect default display.. featureState=[" + featureState + "]");
            }
            if(featureState)
            {
                if(!DEBUG_tryConnectToDeveloperRoutes(callback))
                {
                    tryConnectDisplay( scanList.GetDefault(), callback );
                }
            }
            else
            {
                formatErrorResponse(callback, Commands.ERR_SCREENCAST_DISABLED);
            }
        }

        /// @brief
        /// Connect to display with given ID
        ///
        public void ConnectDisplay(string displayID, DeviceCommandCallback callback=null)
        {
            if(featureState)
            {
                tryConnectDisplay( scanList.GetByName(displayID), callback );
            }
            else
            {
                formatErrorResponse(callback, Commands.ERR_SCREENCAST_DISABLED);
            }
        }

        /// @brief
        /// Disconnect from current device
        ///
        public void Disconnect(DeviceCommandCallback callback=null)
        {
            if(featureState)
            {
                this.mainBridge.SendCommand(new Command(Commands.SCREENCAST_DESELECT_ROUTE));
            }
            else
            {
                formatErrorResponse(callback, Commands.ERR_SCREENCAST_DISABLED);
            }
        }

        /// @brief
        /// Saves given ID as default, enabling calls to ConnectDefaultDisplay()
        ///
        public void SetDefaultDisplay(string displayID, DeviceCommandCallback callback=null)
        {
            if(featureState)
            {
                this.mainBridge.SendCommand(
                    new Command(Commands.SCREENCAST_SET_DEFAULT_DISPLAY, displayID), 
                    AndroidCommandCallback.Get(callback)
                );
            }
            else
            {
                formatErrorResponse(callback, Commands.ERR_SCREENCAST_DISABLED);
            }
        }

    

        //-----------------------------------------------------------------------------------------------------------------

        //  backend

        IEnumerator forceRefreshInterval()
        {
            while(true)
            {
                if(!isConnected())
                {
                    if(enabled && featureState)
                    {
                        SearchDevices();
                        if(scanList.isDefaultAvailable())
                        {
                            yield return new WaitForSeconds(REFRESH_INTERVAL_DEFAULT_AVAILABLE);
                        }
                        else if(scanList.Count > 0)
                        {
                            yield return new WaitForSeconds(REFRESH_INTERVAL_NO_DEFAULT);
                        }
                        else
                        {
                            yield return new WaitForSeconds(REFRESH_INTERVAL_NO_AVAILABLE_DISPLAYS);
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(10f);
//                        Debug.Log("ScreencastBridge:: " + enabled + " || " + featureState);
                    }
                }
                yield return null;
            }
        }

        protected override void OnInitializeBridge(AndroidBridge main)
        {
            if(debug)
            {
                Debug.Log("Initialize ScreencastBridge... scanList=[" + scanList.Count + "]");
            }

            featureState = false;
            refreshFeatureState();
            
            if(scanList.Count > 0)
            {
                SendMessageToObservers<ICastAPListener>(x=> x.OnRefreshedAvailableDisplays(scanList));
            }
            if(isConnected())
            {
                SendMessageToObservers<ICastAPListener>(x=> x.OnConnectedDisplay(selectedRoute));
            }

            StartCoroutine("forceRefreshInterval");
        }

        public virtual void OnChangedWifiSettings(string state)
        {
            if(debug)
            {
                Debug.Log("ScreencastBridge::OnChangedWifiSettings=[" + state + "]");
            }

            bool s;
            if(parseBoolean(state, out s))
            {
                featureState = s;
                if(!s && isConnected())
                {
                    Debug.LogWarning("ScreencastBridge:: changed WifiSettings=false during active cast.");
                }
            }
        }

        public virtual void OnRefreshAvailableDisplays(string json)
        {
            if(debug)
            {
                Debug.Log("ScreencastBridge::OnRefreshAvailableDisplays()  json={\n" + json + "\n}");
            }
            if(scanList.UpdateFromJson(json))
            {
                if(debug)
                {   
                    Debug.Log("--------> ScreencastBridge:: " + scanList.ToString());
                }   
                var list = scanList as ICastRouteList;
                if(isInitialized)
                {
                    SendMessageToObservers<ICastAPListener>(x=> x.OnRefreshedAvailableDisplays(list));
                }
            }
        }



        public virtual void OnConnectedDisplay(string name)
        {
            if(debug)
            {
                Debug.Log("--------------> ScreencastBridge.OnConnectedDisplay=[" + name + "] " + isInitialized);
            }

            var route = scanList.GetByName(name);
            if(route.isValid())
            {
                selectedRoute = route;  
                if(isInitialized)
                {
                    SendMessageToObservers<ICastAPListener>(x=> x.OnConnectedDisplay(route));
                }
            }
            else
            {
                Debug.LogWarning("ScreencastBridge:: selected unknown route=[" + name + "]");
            }
        }



        public virtual void OnDisconnectedDisplay(string name)
        {
            if(debug)
            {
                Debug.Log("--------------> WifiBridge.OnDisconnectedDisplay=[" + name + "] " + isInitialized);
            }

            var route = scanList.GetByName(name);
            if(route.isValid())
            {
                if(selectedRoute.name == name)
                {
                    selectedRoute = ScreencastRouteInfo.None;
                    if(isInitialized)
                    {
                        SendMessageToObservers<ICastAPListener>(x=> x.OnDisconnectedDisplay(route));
                    }
                }
                else
                {
                    Debug.LogWarning("ScreencastBridge:: cast state not synched on disconnect=[" + name + "]");
                }
            }
            else
            {
                Debug.LogWarning("ScreencastBridge:: selected unknown route=[" + name + "]");
            }
        }



        //-----------------------------------------------------------------------------------------------------------------

        //  util

        void refreshFeatureState()
        {
            this.mainBridge.SendCommand(
                    new Command(Commands.SCREENCAST_ENABLED),
                    AndroidCommandCallback.Get((x)=> {featureState = x.success; Debug.Log("ScreencastBridge::refreshFeatureStateCallback() ? " + x.success);})
            );
        }


        void tryConnectDisplay(ScreencastRouteInfo display, DeviceCommandCallback callback)
        {
            if(display.isValid())
            {
                var cmd = new Command(Commands.SCREENCAST_SELECT_ROUTE, display.name);
                if(callback != null)
                {
                    this.mainBridge.SendCommand(cmd, AndroidCommandCallback.Get(callback));
                }
                else
                {
                    this.mainBridge.SendCommand(cmd);
                }
            }
            else 
            {
                formatErrorResponse(callback, Commands.ERR_NO_DEFAULT_CASTROUTE, display.name);
            }
        }



        bool DEBUG_tryConnectToDeveloperRoutes(DeviceCommandCallback callback)
        {
            var yehua = scanList.displays.Find(x=> x.state == ScreencastRouteState.AVAILABLE && x.name.ToLower().Contains("yehua"));
            Debug.Log("try connect yehua... " + yehua.isValid());
            if(yehua.isValid())
            {
                tryConnectDisplay(yehua, callback);
                return true;
            }
            var mirascreen = scanList.displays.Find(x=> x.state == ScreencastRouteState.AVAILABLE && x.name.ToLower().Contains("mirascreen"));
            if(mirascreen.isValid())
            {
                tryConnectDisplay(mirascreen, callback);
                return true;
            }
            
            
            return false;
        }

    }



}


