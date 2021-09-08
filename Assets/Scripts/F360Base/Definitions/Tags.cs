using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360
{


    /// @brief
    /// Runtime representation of a tag used to categorize and search individual tasks.
    ///
    public struct Tag
    {

        //  properties

        public int id;          ///< internal tag id, localization-independent
        public string label;    ///< display name
        public bool visible;    ///< visible to user?
        public Sprite icon;     ///< optional graphic

        /// @brief
        /// null value
        ///
        public static Tag None 
        {
            get { return _none;  }
        }
        static Tag _none = new Tag(-1, "", false);

        public bool isEmpty()
        {
            return id < 0 || string.IsNullOrEmpty(label);
        }


        //-----------------------------------------------------------------------------------------------

        //  static interface

        public static bool Exists(string tag)
        {
            return Get(tag) != Tag.None;
        }

        public static Tag Get(int id)
        {
            if(cache.ContainsKey(id))
            {
                return cache[id];
            }
            else
            {
                return Tag.None;
            }
        }
        public static Tag Get(string tag)
        {
            tag = tag.ToLower();
            if(name_to_id.ContainsKey(tag))
            {
                return cache[name_to_id[tag]];
            }
            else
            {
                return Tag.None;
            }
        }


        public static Tag Create(int id, string tag, bool visible, Sprite icon=null)
        {
            if(string.IsNullOrEmpty(tag))
            {
                return Tag.None;
            }
            else
            {
                if(!cache.ContainsKey(id))
                {
                    var t = new Tag(id, tag, visible, icon);
                    name_to_id.Add(tag.ToLower(), id);
                    cache.Add(id, t);
                    return t;
                }
                else
                {
                    var t = cache[name_to_id[tag.ToLower()]];
                    t.icon = icon;
                    t.visible = visible;
                    t.label = tag;
                    cache[name_to_id[tag]] = t;
                    return t; 
                }
            }
        }

        public static int Count() 
        {
            return cache.Count;
        }

        public static IEnumerable<Tag> GetAll()
        {
            foreach(var tag in cache.Values)
            {
                yield return tag;
            }
        }
        
        /// @brief
        /// Clear all known tags. Call before taglist is parsed. 
        ///
        public static void ClearAll()
        {
            cache.Clear();
            name_to_id.Clear();
        }

        //-----------------------------------------------------------------------------------------------

        //  internal

        static Dictionary<int, Tag> cache = new Dictionary<int, Tag>();
        static Dictionary<string, int> name_to_id = new Dictionary<string, int>();

        

        internal Tag(int id, string label, bool visible, Sprite icon=null)
        {
            this.id = id;
            this.label = label;
            this.visible = visible;
            this.icon = icon;
        }

        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null) && obj is Tag)
            {
                return Equals((Tag)obj);
            }
            return false;
        }
        public bool Equals(Tag other)
        {
            return other.id == id;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return label.GetHashCode() + id * 17;
            }
        }

        public override string ToString()
        {
            return "tag<" + label + ">[" + id.ToString() + "] visible? " + visible.ToString();
        }

        public static bool operator==(Tag a, Tag b)
        {
            return a.Equals(b);
        }
        public static bool operator!=(Tag a, Tag b)
        {
            return !a.Equals(b);
        }
    }

}

