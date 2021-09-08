using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace VRIntegration
{
    public class MobileIntegration : VRIntegrationBase
    {
        #pragma warning disable

        [SerializeField] bool keepScreenOn = true;


        protected override void Awake()
        {
            base.Awake();
            Input.simulateMouseWithTouches = true;


            if(keepScreenOn)
            {
                Application.runInBackground = true;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }

        [SerializeField] private CustomStandaloneInputModule inputModule;

        #pragma warning restore

        public override VRInterface.Platform platform
        {
            get { return VRInterface.Platform.Mobile; } 
        }

        public override string GetSDKVersion()
        {
            return Application.unityVersion;
        }

        public override string GetHardwareSerial()
        {
            #if UNITY_EDITOR
            return Utility.Win32.MachineGuid.GetMachineGuid();
            #else
            return "developer-windows-pc";
            #endif
        }
        
        protected override float clickT()
        {
            return 0.3f;
        }

        public override Ray GetGaze()
        {
            return new Ray(this.eventCam.transform.position, this.eventCam.transform.forward);
        }

        public override bool Input_Down()
        {
            if(Input.touchSupported)
            {
                if(Input.touchCount > 0)
                {
                    var t = Input.GetTouch(0);
                    return t.phase == TouchPhase.Began;
                }
                else return false;
            }
            else return Input.GetMouseButtonDown(0);
        }
        public override bool Input_Pressed()
        {
            if(Input.touchSupported)
            {
                if(Input.touchCount > 0)
                {
                    var t = Input.GetTouch(0);
                    return t.phase == TouchPhase.Stationary;
                }
                else return false;
            }
            else return Input.GetMouseButton(0);
        }
        public override bool Input_Up()
        {
            if(Input.touchSupported)
            {
                if(Input.touchCount > 0)
                {
                    var t = Input.GetTouch(0);
                    return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                }
                else return false;
            }
            else return Input.GetMouseButtonUp(0);
        }

        public override PointerEventData GetPointerData()
        {
            PointerEventData data;
            inputModule.GetEventData(out data);
            return data;
        }

        public override void Enable(bool state)
        {
            if(state)
                Debug.Log("--------- ENABLE TOUCH INTEGRATON ---------");
            base.Enable(state);
            //ToggleGazePointer(state);
            toggleRaycasters<GraphicRaycaster>(state, _casterCondition);
        
            inputModule.enabled = state;

            if(keepScreenOn)
            {
                Screen.sleepTimeout = state ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        protected override void setupSceneInternal(Scene scene, bool state, GameObject[] rootObjs)
        {
            toggleRaycasters<GraphicRaycaster>(rootObjs, state, _casterCondition);
        }

        private bool _casterCondition(GraphicRaycaster caster)
        {
            return caster.GetType().Equals(typeof(GraphicRaycaster));
        }

        float rotX, rotY;

        [Space(6)][Header("Head Movement")]
        [SerializeField] private bool noHeadMovement = false;
        [SerializeField] private KeyCode rotKey = KeyCode.LeftAlt;
        [SerializeField] private float xRotSpeed = 10;
        [SerializeField] private float yRotSpeed = 5;

        protected override void Update()
        {
            base.Update();

            #if UNITY_EDITOR
            if(!noHeadMovement)
            {
                //  implement LeftALT-lookaround in editor time
                if(Input.GetKey(rotKey))
                {
                    rotX += Input.GetAxis("Mouse X") * xRotSpeed * Time.deltaTime;
                    rotY -= Input.GetAxis("Mouse Y") * yRotSpeed * Time.deltaTime;

                    Quaternion q = Quaternion.Euler(rotY, rotX, 0);
                    eventCam.transform.rotation = q;
                }
            }
            #endif
        }


    }

}

