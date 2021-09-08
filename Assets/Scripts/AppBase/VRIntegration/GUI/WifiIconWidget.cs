using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using DeviceBridge;
//using GUI.Feedback;



/*
*   Beispiel eines UI-Widgets. Alle Referenzen zum eigenen UI-System sowie uGUI-Elemente wurden auskommentiert. 
*   Relevant ist die Anbindung zur DeviceBridge, um den WifiState auslesen zu können.
*
*/



namespace F360.Menus.Widgets
{
    public class WifiIconWidget : MonoBehaviour, /*GUI.IWidget,*/ IWifiStateListener
    {
        //#pragma warning disable
        //[SerializeField] private Image icon;
        //[SerializeField] private Sprite icon_ON;
        //[SerializeField] private Sprite icon_OFF;
        //[SerializeField] private GUI.Feedback.TooltipTarget tooltip;
        //#pragma warning restore

        //ButtonFeedback mButton;

        IWifiBridge wifi { get { return DeviceAdapter.Instance != null ? DeviceAdapter.Instance.Wifi : null; } }

        void Awake()
        {
            if(!canUpdate())
            {
                enabled = false;
            }
            //mButton = GetComponent<ButtonFeedback>();
        }

        void Start()
        {
            if(enabled)
            {
                DeviceAdapter.Instance.AddListener<IWifiStateListener>(this);
                readWifiState();
            }
        }

        public bool isDisplayed { get; private set; }

        public bool canUpdate()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        public void Show(bool immediate=false)
        {
            if(!isDisplayed)
            {
                isDisplayed = true;
                readWifiState();
            }
        }
        public void Hide(bool immediate=false)
        {
            if(isDisplayed)
            {
                isDisplayed = false;
            }
        }

        void readWifiState()
        {
            if(wifi != null)
            {
                setWifiIcon(DeviceAdapter.Instance.Wifi.isConnected());
            }
        }

        void setWifiIcon(bool state)
        {
            //icon.sprite = state ? icon_ON : icon_OFF;
            //icon.SetAlpha(state ? 1f : 0.4f);
            /*if(tooltip != null)
            {
                if(state)
                {
                    Debug.Log("set Wifi ICON :: [" + wifi.GetSSID() + "]");
                    tooltip.text = "Verbunden mit: " + wifi.GetSSID();
                }
                else
                {
                    tooltip.text = "Nicht verbunden";
                }
            }
            if(mButton != null)
            {
                if(!state)
                {
                    mButton.Focus();
                }
                else
                {
                    mButton.Unfocus();
                }
            }*/
        }


        void IWifiStateListener.OnChangedWifiState(bool state)
        {
            Debug.Log("--------> WifiIcon... OnChangedWifiState()");
            setWifiIcon(state);
        }
        void IWifiStateListener.OnConnectedToWifi(string ssid)
        {
            Debug.Log("--------> WifiIcon... OnConnectedToWifi(" + ssid + ")");
            setWifiIcon(true);
        }
        void IWifiStateListener.OnDisconnectedFromWifi(int reason)
        {
            Debug.Log("--------> WifiIcon... OnDisconnectedFromWifi()");
            setWifiIcon(false);
        }
        void IWifiStateListener.OnUpdatedRSSI(int rssi)
        {
            Debug.Log("--------> WifiIcon... OnUpdatedRSSI");
        }



        /*GUI.Interaction.Trigger GUI.IWidget.GetTrigger() { return null; }
        void GUI.IWidget.SetTrigger(GUI.Interaction.Trigger trigger) {}*/
    }

}
