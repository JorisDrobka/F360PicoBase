using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace F360.Users.Stats
{
    public static class StatSerializationUtil
    {   

        public static bool TryParseTime(string term, ref DateTime time)
        {   
            DateTime t;
            if(Backend.TimeUtil.ParseServerTimeFromString(term, out t))
            {
                time = t;
                return true;
            }
            return false;
        }

        public static string FormatTimeString(DateTime time)
        {
            return Backend.TimeUtil.FormatServerTimeString(time);
        }
        public static string FormatTimeStringShort(DateTime time)
        {
            return Backend.TimeUtil.FormatServerTimeStringShort(time);
        }

        public static short[] ConvertToShortArray(IEnumerable<int> ratings)
        {
            if(ratings != null)
            {
                var count = ratings.Count();
                var result = new short[count];
                var i = 0;
                foreach(var r in ratings)
                {
                    result[i] = (short)r;
                    i++;
                }
                return result;
            }
            else
            {
                Debug.LogWarning("StatSerialization:: Received Rating Null-Array");
                return new short[0];
            }
        }
        public static int[] ConvertToIntArray(IEnumerable<short> ratings)
        {
            if(ratings != null)
            {
                var count = ratings.Count();
                var result = new int[count];
                var i = 0;
                foreach(var r in ratings)
                {
                    result[i] = (int)r;
                    i++;
                }
                return result;
            }   
            else
            {
                Debug.LogWarning("StatSerialization:: Received Rating Null-Array");
                return new int[0];
            }
        }
    }
}
