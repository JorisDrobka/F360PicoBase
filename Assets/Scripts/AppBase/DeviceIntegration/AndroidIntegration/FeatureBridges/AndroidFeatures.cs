using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeviceBridge.Android
{

    //-----------------------------------------------------------------------------------------------------------------



    public static class AndroidFeatureUtil
    {
        

        //  feature permissions
        private static string[] permissions_internet = new string[] {
            Permissions.INTERNET
        };
        private static string[] permissions_wifi_access = new string[] {
            Permissions.ACCESS_NETWORK_STATE,
            Permissions.ACCESS_WIFI_STATE,
            Permissions.CHANGE_WIFI_STATE,
            Permissions.CHANGE_NETWORK_STATE
        };
        private static string[] permissions_wifiscan = new string[] {
            Permissions.ACCESS_NETWORK_STATE, 
            Permissions.ACCESS_WIFI_STATE,
            Permissions.ACCESS_COARSE_LOCATION
        };
        private static string[] permissions_miracast = new string[] {
            Permissions.ACCESS_WIFI_STATE,
            Permissions.CHANGE_WIFI_STATE,
            Permissions.CHANGE_WIFI_MULTICAST
        };
        private static string[] permissions_external_storage = new string[] {
            Permissions.READ_EXTERNAL_STORAGE,
            Permissions.WRITE_EXTERNAL_STORAGE
//            Permissions.WRITE_MEDIA_STORAGE
        };
        private static string[] permissions_private_storage = new string[] {
//            Permissions.WRITE_MEDIA_STORAGE
        };

        //-----------------------------------------------------------------------------------------------------------------

        

        //-----------------------------------------------------------------------------------------------------------------

        /// @brief
        /// Get permissions for specific feature or feature mask.
        ///
        public static IEnumerable<string> GetNeededPermissions(this DeviceFeature features)
        {
            var list = new List<string>();
            foreach(var capability in features.GetAll())
            {
                switch(capability)
                {
                    case DeviceFeature.INTERNET:           _addToPermissionsList(permissions_internet, list); break;
                    case DeviceFeature.WIFI_ACCESS:        _addToPermissionsList(permissions_wifi_access, list); break;
                    case DeviceFeature.WIFI_SCAN_ACCESS:   _addToPermissionsList(permissions_wifiscan, list); break;
                    case DeviceFeature.SCREEN_CAST:        _addToPermissionsList(permissions_miracast, list); break;
                    case DeviceFeature.EXTERNAL_STORAGE:   _addToPermissionsList(permissions_external_storage, list); break;
                    case DeviceFeature.PRIVATE_STORAGE:    _addToPermissionsList(permissions_private_storage, list); break;
                }
            }
            return list;
        }

        /// @brief
        /// Returns all features that need given permission
        ///
        public static DeviceFeature FilterByPermission(this DeviceFeature features, string permission)
        {
            var result = DeviceFeature.None;
            foreach(var f in features.GetAll())
            {
                if(GetNeededPermissions(f).Contains(permission))
                {
                    result &= f;
                }
            }
            return result;
        }


        private static void _addToPermissionsList(string[] permissions, List<string> list)
        {
            for(int i = 0; i < permissions.Length; i++)
            {
                if(!list.Contains(permissions[i]))
                    list.Add(permissions[i]);
            }
        }

    }
}