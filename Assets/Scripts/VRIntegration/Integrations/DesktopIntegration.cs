using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;


namespace VRIntegration
{
    

    public class DesktopIntegration : VRIntegrationBase
    {
        [SerializeField] private CustomStandaloneInputModule inputModule;

        public override VRInterface.Platform platform
        {
            get { return VRInterface.Platform.Desktop; } 
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
            return Input.GetMouseButtonDown(0);
        }
        public override bool Input_Pressed()
        {
            return Input.GetMouseButton(0);
        }
        public override bool Input_Up()
        {
            return Input.GetMouseButtonUp(0);
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
                Debug.Log("--------- ENABLE DESKTOP INTEGRATON ---------");
            base.Enable(state);
            //ToggleGazePointer(state);
            toggleRaycasters<GraphicRaycaster>(state, _casterCondition);
        
            inputModule.enabled = state;
        }

        public override void SetupCanvas(Canvas canvas)
        {
    //        Debug.Log(RichText.emph("DesktopIntegration:: ") + "setup canvas<" + canvas.name + ">");
            toggleRaycasters<GraphicRaycaster>(canvas.gameObject, true, _casterCondition);

            #if PLATFORM_PICO

            var pvr = canvas.GetComponent<Pvr_UICanvas>();
//            if(pvr != null) { Component.Destroy(pvr); Debug.Log("DESTROYED PVR CAnvas of " + canvas.name); }

            #endif
        }

        public override void ToggleCanvas(Canvas canvas, bool b)
        {
            
       //     canvas.enabled = b;
            var caster = canvas.GetComponent<GraphicRaycaster>();

//            Debug.Log("Toggle Canvas<" + canvas.name + "> " + b + "\nCASTER? " + (caster != null));
            if(caster != null)
            {
                #if PLATFORM_PICO
                if(!(caster is Pvr_UIGraphicRaycaster)) caster.enabled = b;
                #else
                caster.enabled = b;
                #endif
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