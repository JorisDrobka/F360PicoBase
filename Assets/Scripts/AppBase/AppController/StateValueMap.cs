using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace F360.Manage.States
{

    /// @brief
    /// Map of values accessible & monitored by the state system.
    /// Changes in Values can be registered and influence state behaviour.
    ///
    public class StateValueMap
    {
        const string TOKEN = "StateValues:: ";

        public bool Contains(string id)
        {
            return entries.ContainsKey(id);
        }

        public bool HasChangedValue(string id, int frameDiff=1)
        {
            if(entries.ContainsKey(id))
            {
                return Time.frameCount - entries[id].LastChangeFrame <= frameDiff;
            }
            return false;
        }
        public bool HasChangedValueSince(string id, float timeStamp)
        {
            if(entries.ContainsKey(id))
            {
                return entries[id].LastChangeTime >= timeStamp;
            }
            return false;
        }

        public string GetString(string id) 
        { 
            if(entries.ContainsKey(id))
            {
                return entries[id].GetString();
            }
            else
            {
                Debug.LogWarning(TOKEN + "string<" + id + "> does not exist!");
                return "";
            }
        }
        public int GetInt(string id) 
        { 
            if(entries.ContainsKey(id))
            {
                return entries[id].GetInt();
            }
            else
            {
                Debug.LogWarning(TOKEN + "int<" + id + "> does not exist!");
                return -1;
            }
        }
        public bool GetBool(string id) 
        { 
            if(entries.ContainsKey(id))
            {
                return entries[id].GetBool();
            }
            else
            {
                Debug.LogWarning(TOKEN + "bool<" + id + "> does not exist!");
                return false;
            }
        }

        public void SetString(string id, string value) 
        {
            if(entries.ContainsKey(id))
            {
                entries[id].Set(value);
                entries[id].SetChanged();
            }
            else
            {
                entries.Add(id, new Entry(value));
            }
        }
        public void SetInt(string id, int value) 
        {
            if(entries.ContainsKey(id))
            {
                entries[id].Set(value);
                entries[id].SetChanged();
            }
            else
            {
                entries.Add(id, new Entry(value));
            }
        }
        public void SetBool(string id, bool value) 
        {
            if(entries.ContainsKey(id))
            {
                entries[id].Set(value);
                entries[id].SetChanged();
            }
            else
            {
                entries.Add(id, new Entry(value));
            }
        }
        public void SetString(string id, Func<string> getter)
        {
            if(entries.ContainsKey(id))
            {
                entries[id].Set(getter);
                entries[id].SetChanged();
            }
            else
            {
                entries.Add(id, new Entry(getter));
            }
        }
        public void SetInt(string id, Func<int> getter)
        {
            if(entries.ContainsKey(id))
            {
                entries[id].Set(getter);
                entries[id].SetChanged();
            }
            else
            {
                entries.Add(id, new Entry(getter));
            }
        }
        public void SetBool(string id, Func<bool> getter)
        {
            if(entries.ContainsKey(id))
            {
                entries[id].Set(getter);
                entries[id].SetChanged();
            }
            else
            {
                entries.Add(id, new Entry(getter));
            }
        }


        //-----------------------------------------------------------------------------------------------------------------


        Dictionary<string, Entry> entries;

        class Entry
        {
            public float LastChangeTime { get; set; }
            public int LastChangeFrame { get; set; }

            string stringVal;
            int intVal;
            bool boolVal;
            Func<string> stringF;
            Func<int> intF;
            Func<bool> boolF;

            public Entry(string s) { stringVal = s; }
            public Entry(int i) { intVal = i; }
            public Entry(bool b) { boolVal = b; }
            public Entry(Func<string> f) { stringF = f; }
            public Entry(Func<int> f) { intF = f; }
            public Entry(Func<bool> f) { boolF = f; }

            public bool isValue()
            {   
                return !isFunction();
            }

            public bool isFunction()
            {
                return boolF != null || stringF != null || intF != null;
            }
        
            public string GetString()
            {
                if(stringF != null) return stringF();
                else return stringVal;
            }
            public int GetInt()
            {
                if(intF != null) return intF();
                else return intVal;
            }
            public bool GetBool()
            {
                if(boolF != null) return boolF();
                else return boolVal;
            }

            public void SetChanged()
            {
                LastChangeFrame = Time.frameCount;
                LastChangeTime = Time.time;
            }

            public void Set(string s)
            {
                stringVal = s;
                stringF = null;
            }
            public void Set(int i)
            {
                intVal = i;
                intF = null;
            }
            public void Set(bool b)
            {
                boolVal = b;
                boolF = null;
            }
            public void Set(Func<string> f)
            {
                stringVal = "";
                stringF = f;
            }
            public void Set(Func<int> f)
            {
                intVal = -1;
                intF = f;
            }
            public void Set(Func<bool> f)
            {
                boolVal = false;
                boolF = f;
            }
            
        }
    }


}
