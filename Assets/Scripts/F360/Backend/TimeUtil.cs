using System;
using System.Globalization;

namespace F360.Backend
{



    public static class TimeUtil
    {

        public const string DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss";       //  equivalent to python datetime.strptime() format "%d/%m/%Y %H:%M:%S"
        public const string DATE_FORMAT = "dd/MM/yyyy";

        public static string FormatServerTimeString(DateTime t)
        {
            return t.ToString(DATE_TIME_FORMAT);
        }
        public static string FormatServerTimeStringShort(DateTime t)
        {
            return t.ToString(DATE_FORMAT);
        }

        public static bool ParseServerTimeFromString(string s, out DateTime result)
        {
            try {
                
                if(DateTime.TryParseExact(s, DATE_TIME_FORMAT, null, DateTimeStyles.None, out result))
                {
                    return true;
                }
                else if(DateTime.TryParseExact(s, DATE_FORMAT, null, DateTimeStyles.None, out result))
                {
                    return true;
                }
                else
                {
                    result = DateTime.UtcNow;
                    return false;
                }
            }
            catch(Exception ex) {

                UnityEngine.Debug.LogError("TimeUtil:: Error parsing datetime string=[" + s + "]\nMessage: " + ex.ToString());
                result = DateTime.UtcNow;
                return false;
            }
        }   

        public static DateTime ServerTime
        {
            get { return DateTime.UtcNow.AddHours(1); }
        }

        public static DateTime LocalTime
        {
            get { return DateTime.Now; }
        }

        public static DateTime MinimumTime
        {
            get { return DateTime.MinValue; }
        }

        public static DateTime ServerToLocalTime(DateTime t)
        {
            return t.ToLocalTime();
        }

        public static DateTime LocalToServerTime(DateTime t)
        {
            return t.ToUniversalTime();
        }

    }




}
