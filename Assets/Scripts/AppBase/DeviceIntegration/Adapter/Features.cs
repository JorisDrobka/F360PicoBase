using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeviceBridge
{

    /// @brief
    /// Defines what the plugin should be capabable of doing,
    /// resulting in different permissions needed from the device OS.
    ///
    [System.Flags]
    public enum DeviceFeature
    {
        None = 0,
        ACTIVITY_ACCESS = 1,     
        INTERNET = 2,
        WIFI_ACCESS = 4,
        WIFI_SCAN_ACCESS = 8,
        SCREEN_CAST = 16,
        EXTERNAL_STORAGE = 32,
        PRIVATE_STORAGE = 64,

        All = ACTIVITY_ACCESS | WIFI_ACCESS | WIFI_SCAN_ACCESS | SCREEN_CAST | EXTERNAL_STORAGE | PRIVATE_STORAGE
    }


    //-----------------------------------------------------------------------------------------------------------------

    /// @cond PRIVATE

    public static class DeviceFeatureUtil
    {

        public const string FEATURE_PREFIX = "feature:";
        public const string FEATURE_SEPARATOR = "|";


        public static string GetName(this DeviceFeature feature, bool debugPrefix=false)
        {
            var count = 0;
            var b = new System.Text.StringBuilder(debugPrefix ? FEATURE_PREFIX : "");
            foreach(var f in feature.GetAll())
            {
                if(count > 0)
                {
                    b.Append(FEATURE_SEPARATOR);
                }
                b.Append(_getFeatureName(f));
                count++;
            }
            return b.ToString();
        }
        private static string _getFeatureName(DeviceFeature f)
        {
            switch(f)
            {
                case DeviceFeature.ACTIVITY_ACCESS:    return "ActivityAccess";
                case DeviceFeature.INTERNET:           return "Internet";
                case DeviceFeature.WIFI_ACCESS:        return "WifiAccess";
                case DeviceFeature.WIFI_SCAN_ACCESS:   return "WifiScan";
                case DeviceFeature.SCREEN_CAST:           return "Miracast";
                case DeviceFeature.EXTERNAL_STORAGE:   return "ExternalStorage";
                case DeviceFeature.PRIVATE_STORAGE:    return "PrivateStorage";
                default:                                return "UNKNOWN_FEATURE";
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        public static bool hasFeature(this DeviceFeature mask, DeviceFeature flag)
        {
            return (mask & flag) == flag;
        }
        public static IEnumerable<DeviceFeature> GetAll(this DeviceFeature mask)
        {
            if(mask.hasFeature(DeviceFeature.INTERNET))
            {
                yield return DeviceFeature.INTERNET;
            }
            if(mask.hasFeature(DeviceFeature.WIFI_ACCESS))
            {
                yield return DeviceFeature.WIFI_ACCESS;
            }
            if(mask.hasFeature(DeviceFeature.WIFI_SCAN_ACCESS))
            {
                yield return DeviceFeature.WIFI_SCAN_ACCESS;
            }
            if(mask.hasFeature(DeviceFeature.SCREEN_CAST))
            {
                yield return DeviceFeature.SCREEN_CAST;
            }
            if(mask.hasFeature(DeviceFeature.EXTERNAL_STORAGE))
            {
                yield return DeviceFeature.EXTERNAL_STORAGE;
            }
            if(mask.hasFeature(DeviceFeature.PRIVATE_STORAGE))
            {
                yield return DeviceFeature.PRIVATE_STORAGE;
            }
        }
    }

    /// @endcond

}
