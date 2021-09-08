using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeviceBridge
{   
    
    ///<summary>
    /// generic couroutine host acc
    ///</summary>
    public class CoroutineHost : MonoBehaviour
    {
        
        public void Awake()
        {
            _instance = this;
        }

        public static CoroutineHost Instance
        {
            get {
                if(_instance == null) {
                    GameObject go = new GameObject("coroutineHost");
                    _instance = go.AddComponent<CoroutineHost>();
                } 
                return _instance;
            }
        }
        private static CoroutineHost _instance;
    }

}

