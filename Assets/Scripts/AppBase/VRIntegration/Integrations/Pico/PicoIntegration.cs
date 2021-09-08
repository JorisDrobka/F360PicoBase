using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace VRIntegration
{

    public class PicoIntegration : VRIntegrationBase
    {

        #pragma warning disable
        
        #if PLATFORM_PICO
        
        
        //    [SerializeField] private Pvr_GazeInputModule gazeModule;
        //    [SerializeField] private Pvr_UnitySDKSightInputModule sightModule;
            [SerializeField] private Pvr_UIPointer gaze;


            private CustomPvrInputModule inputModule;

        #endif

        [SerializeField] private GameObject controllerManager;
        [SerializeField] private GameObject inputModuleObj;

        [SerializeField][Tooltip("optional if no VRCursor is used in scene, for example if you're using the default Pico Controller")] 
        private SpriteRenderer pointerRenderer;
        [SerializeField] private Color activePointerColor;
        private Color defaultPointerColor;



        #pragma warning restore

    

        protected override void Awake()
        {
            base.Awake();
            if(pointerRenderer != null)
            {
                defaultPointerColor = pointerRenderer.color;
            }

            #if PLATFORM_PICO

                inputModule = inputModuleObj.GetComponent<CustomPvrInputModule>();

            #endif
        }

        //-----------------------------------------------------------------------------------------------------------------

        public override VRInterface.Platform platform
        {
            get { return VRInterface.Platform.Pico; } 
        }

        public override string GetSDKVersion()
        {
            #if PLATFORM_PICO
            return Pvr_UnitySDKAPI.System.UnitySDKVersion;
            #else
            return "Unsupported_Platform";
            #endif
        }

        public override string GetHardwareSerial()
        {
            #if PLATFORM_PICO
            return Pvr_UnitySDKAPI.System.UPvr_GetDeviceSN();
            #else
            return "Platform_Error";
            #endif
        }

        protected override float clickT()
        {
            return 0.45f;
        }

        public override Ray GetGaze()
        {
            #if PLATFORM_PICO
            if(gaze != null)
            {
                return new Ray(gaze.GetOriginPosition(), gaze.GetOriginForward());
            }
            #endif
            return new Ray(eventCam.transform.position, eventCam.transform.forward);
        }
        
        /*public override void ToggleGazePointer(bool state)
        {
            #if PLATFORM_PICO

                vrCursor.Show(state);
                if(gaze != null)
                {
//                    gaze.gameObject.SetActive(state);
                }
                
            #endif
        }
        public override void HighlightGazePointer(bool state)
        {
            if(vrCursor != null)
            {
       //         vrCursor.SetState(state ? VRCursor.State.Gaze : VRCursor.State.Off);
            }
            else if(pointerRenderer != null)
            {
                pointerRenderer.color = state ? activePointerColor : defaultPointerColor;
            }
            
        //   gaze?.Highlight(state);
        }*/

        public override bool Input_Down()
        {
            #if UNITY_EDITOR
            return Input.GetMouseButtonDown(0);
            #else
            return Input.GetKeyDown(KeyCode.Joystick1Button0);
            #endif
        }
        public override bool Input_Pressed()
        {
            #if UNITY_EDITOR
            return Input.GetMouseButton(0);
            #else
            return Input.GetKey(KeyCode.Joystick1Button0);
            #endif
        }
        public override bool Input_Up()
        {
            #if UNITY_EDITOR
            return Input.GetMouseButtonUp(0);
            #else
            return Input.GetKeyUp(KeyCode.Joystick1Button0);
            #endif
        }

        public override PointerEventData GetPointerData()
        {
            #if PLATFORM_PICO
            PointerEventData data=null;
            if(inputModule != null)
            {
                inputModule.GetEventData(CustomPvrInputModule.PointerType.Head, out data);
            }
            /*else if(gazeModule != null)
            {
                gazeModule.GetEventData(out data);
            }
            else if(sightModule != null)
            {
                sightModule.GetEventData(out data);
            }*/
            return data;
            #else
            return new PointerEventData(null);
            #endif
        }


        public override void Enable(bool state)
        {
            base.Enable(state);
            //ToggleGazePointer(state);

            #if PLATFORM_PICO
            if(inputModule != null)
            {
                inputModule.enabled = state;
            }

            toggleRaycasters<GraphicRaycaster>(state, (r)=> { return r.GetType()==typeof(Pvr_UIGraphicRaycaster); }, setupCasterObj);
            controllerManager?.SetActive(state);
    

            foreach(var canvas in GameObject.FindObjectsOfType<Pvr_UICanvas>())
            {
         //       Debug.Log("found pvr canvas " + canvas.name);
                canvas.isActive = state;
                canvas.enabled = state;
            }

            #endif
        }
        void setupCasterObj(GameObject go)
        {
            var canvas = go.GetComponent<Canvas>();
            if(canvas != null) SetupCanvas(canvas);
        }

        public override void SetupCanvas(Canvas canvas)
        {
            /*#if PLATFORM_PICO

            var pvr = canvas.GetComponent<Pvr_UIGraphicRaycaster>();
            var dflt = canvas.GetComponents<GraphicRaycaster>().Where(x=> x.GetType() != typeof(Pvr_UIGraphicRaycaster)).FirstOrDefault();

            if(pvr != null)
            {
                pvr.enabled = true;
            }
            else
            {
                pvr = canvas.gameObject.AddComponent<Pvr_UIGraphicRaycaster>();
                pvr.enabled = true;
            }
            if(dflt != null) dflt.enabled = false;
            #endif

            Debug.Log(RichText.emph("PicoIntegration::::") + " Setup canvas=[" + canvas.name + "]... cam? " + (canvas.worldCamera != null ? canvas.worldCamera.name : "(mull)"));*/
        }

        public override void ToggleCanvas(Canvas canvas, bool b)
        {
            var caster = canvas.GetComponent<Pvr_UIGraphicRaycaster>();
            if(caster != null)
            {
                caster.enabled = b;
            }
      //      canvas.enabled = b;
        }


        protected override void setupSceneInternal(Scene scene, bool state, GameObject[] rootObjs)
        {
            toggleRaycasters<GraphicRaycaster>(rootObjs, state, _casterCondition);

            #if PLATFORM_PICO

            foreach(var root in rootObjs)
            {
                foreach(var canvas in root.GetComponentsInHierarchy<Pvr_UICanvas>())
                {
                    canvas.isActive = state;
                    canvas.enabled = state;
                }
            }

            #endif
        }

        private bool _casterCondition(GraphicRaycaster caster)
        {
            return caster.GetType().Equals(typeof(GraphicRaycaster));
        }

    }
    

}