using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.EventSystems
{ 
    public class CustomStandaloneInputModule : StandaloneInputModule
    {

        #pragma warning disable

        [Space(12), Header("Custom")]
        [SerializeField] EventSystem system;

        #pragma warning restore

        public bool GetEventData(out PointerEventData data)
        {
            switch(Application.platform)
            {
                case RuntimePlatform.Android:       
                case RuntimePlatform.IPhonePlayer:  data = GetTouchPointerData(); break;
                default:                            data = GetMousePointerData(); break;

            }
            
    //        Debug.Log("....customStandaloneInputModule.getEventData()= " + (data != null) + " platform=[" + Application.platform + "] kMouseLeft= " + kMouseLeftId + " kFakeTouch= " + kFakeTouchesId);
            return data != null;
        }



        PointerEventData GetMousePointerData()
        {
            PointerEventData data;
            m_PointerData.TryGetValue(kMouseLeftId, out data);
            return data;
        }

        PointerEventData GetTouchPointerData()
        {
    //        Debug.Log("touches:: " + Input.touchCount);
            if(Input.touchCount > 0)
            {
                hoverBuffer.Clear();
                hoverBuffer.AddRange(m_RaycastResultCache.Select(x=> x.gameObject));

                PointerEventData p = new PointerEventData(system);
                Touch t = Input.GetTouch(0);
                p.position = t.position;
                p.hovered = hoverBuffer;
                p.delta = t.deltaPosition;
                return p;
            }   
            else
            {
                return null;
            }
        }

        List<GameObject> hoverBuffer = new List<GameObject>();
    }

}

