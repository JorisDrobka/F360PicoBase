using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using F360.Data;

namespace F360.Users.Stats
{


    /// @brief
    /// Runtime representation of user's performance considering a selection of looks.
    /// This format is used to feed data to "Blick-Rose" visualization
    ///
    public class LookEventMap
    {
        
        public const int NULL_VALUE = Constants.RATING_NONE;
        public const int MAX_VALUE = Constants.RATING_MAX;
        public const int MIN_VALUE = Constants.RATING_MIN;


        //  fields

        private Dictionary<LookEventType, int> map;
        private int total = -1;


        //-----------------------------------------------------------------------------------------------

        //  interface
        
        public int TotalRating
        {
            get {
                return total;
            }
        }

        public int this[LookEventType look]
        {
            get {
                if(map.ContainsKey(look)) return map[look];
                else return NULL_VALUE;
            }
            set {
                SetRating(look, value);
            }
        }

        public bool ContainsKey(LookEventType look)
        {
            return map.ContainsKey(look);
        }

        public bool HasRatingSet(LookEventType look)
        {
            return map.ContainsKey(look) && map[look] >= MIN_VALUE;
        }


        public bool SetRating(LookEventType look, int rating)
        {
            if(map.ContainsKey(look)) {
                map[look] = StatUtil.Clamp(rating);
                calc_average();
                return true;
            }
            else {
                return false;
            }
        }

        public Dictionary<LookEventType, int> GetDictionary()
        {
            return map;
        }

        public LookEventType[] GetSavedKeys()
        {
            return map.Keys.ToArray();
        }

        public void Reset()
        {
            if(map != null)
            {
                foreach(var key in map.Keys)
                {
                    map[key] = NULL_VALUE;
                }
            }
            this.total = -1;
        }

        //-----------------------------------------------------------------------------------------------

        //  constructor

        public LookEventMap()
        {
            initMap();
        }
        public LookEventMap(LookEventMap src)
        {
            initMap();
            foreach(var look in map.Keys)
            {
                if(src.HasRatingSet(look)) map[look] = src[look];
            }

        }
        public LookEventMap(Dictionary<LookEventType, int> src)
        {
            initMap();
            foreach(var look in src.Keys)
            {
                if(map.ContainsKey(look))
                {
                    map[look] = StatUtil.Clamp(src[look]);
                }
            }
        }

        void initMap()
        {
            this.map = new Dictionary<LookEventType, int>();
            foreach(var look in LookEventUtil.GetSerializableTypes())
            {
                map.Add(look, NULL_VALUE);
            }
        }

        void calc_average()
        {
            float sum = 0;
            foreach(var look in map.Keys)
            {
                if(HasRatingSet(look)) sum += map[look];
            }
            this.total = StatUtil.Clamp(Mathf.RoundToInt(sum / map.Count));
        }
    }


}


