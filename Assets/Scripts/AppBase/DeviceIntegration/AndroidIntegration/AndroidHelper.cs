using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeviceBridge.Android
{
    

    public static class AndroidHelper
    {

        public const string UnityActivityName = "com.unity3d.player.UnityPlayer";

        /// @brief
        /// the name of the GameObject capable of receiving 
        /// function calls from the Android side.
        ///
        /// all receiver interfaces found in F360.Android.Internal
        /// must be implemented by Monobehaviours residing on this object
        ///
        ///
        public const string MESSAGE_OBJECT = "AndroidBridge";


    }


    //-----------------------------------------------------------------------------------------------------------------


    public static class Permissions
    {
        

        public const string INTERNET = "android.permission.INTERNET";
        public const string ACCESS_WIFI_STATE = "android.permission.ACCESS_WIFI_STATE";
        public const string ACCESS_NETWORK_STATE = "android.permission.ACCESS_NETWORK_STATE";
        public const string CHANGE_NETWORK_STATE = "android.permission.CHANGE_NETWORK_STATE";
        public const string CHANGE_WIFI_STATE = "android.permission.CHANGE_WIFI_STATE";
        public const string ACCESS_COARSE_LOCATION = "android.permission.ACCESS_COARSE_LOCATION";
        public const string CHANGE_WIFI_MULTICAST = "android.permission.CHANGE_WIFI_MULTICAST_STATE";
        public const string READ_EXTERNAL_STORAGE = "android.permission.READ_EXTERNAL_STORAGE";
        public const string WRITE_EXTERNAL_STORAGE = "android.permission.WRITE_EXTERNAL_STORAGE";
        public const string WRITE_MEDIA_STORAGE = "android.permission.WRITE_MEDIA_STORAGE";
  //      public const string ACCESS_MEDIA_LOCATION = "android.permission.ACCESS_MEDIA_LOCATION";



        public static bool hasPermission(string permission)
        {
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission);
        }

        public static IEnumerator requestPermission(string permission, System.Action<string, bool> callback=null, IPermissionsRationale r=null)
        {
            if(Application.platform == RuntimePlatform.Android && !hasPermission(permission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(permission);
                if(r != null)
                {
                    yield return new WaitForSeconds(1f);
                    if(!hasPermission(permission))
                    {
                        r.OnRequestDenied(permission);
                        while(!hasPermission(permission))
                        {
                            if(r.UserAgrees())
                            {
                                callback?.Invoke(permission, true);
                                yield break;
                            }
                            else if(r.UserDeclines())
                            {
                                callback?.Invoke(permission, false);
                                yield break;
                            }
                        }
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                    callback?.Invoke(permission, hasPermission(permission));
                }
            }
            else
            {
                callback?.Invoke(permission, true);
            }
        }
    }


}