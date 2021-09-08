using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace VRIntegration
{

    public abstract class VRIntegrationBase : MonoBehaviour
    {    
        [SerializeField] protected float _clickT = 0.3f;
        [SerializeField] protected float doubleClickT = 0.3f;
        [SerializeField] protected float longPressT = 1f;

        [Space(12)]
        [SerializeField] protected GameObject eventSystem;
        [SerializeField] protected GameObject cameraRig;
        public Camera eventCam;


        public event System.Action clickInput;
        
        public abstract VRInterface.Platform platform { get; }

        public abstract string GetSDKVersion();

        public abstract string GetHardwareSerial();

        public EventSystem GetEventSystem()
        {
            return eventSystem.GetComponent<EventSystem>();
        }

        protected virtual void Awake() {}

        public virtual void Enable(bool state)
        {
            eventSystem?.SetActive(state);
            cameraRig?.SetActive(state);

            foreach(var canvas in GameObject.FindObjectsOfType<Canvas>())
            {
                if(canvas.isRootCanvas && canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.worldCamera = eventCam;
                }
            }
            this.enabled = state;
        }
        public void SetupScene(UnityEngine.SceneManagement.Scene scene, bool state)
        {
            setupSceneInternal(scene, state, scene.GetRootGameObjects());
        }
        protected abstract void setupSceneInternal(UnityEngine.SceneManagement.Scene scene, bool state, GameObject[] rootObjs);

        public abstract Ray GetGaze();

        public abstract PointerEventData GetPointerData();

        public abstract bool Input_Down();
        public abstract bool Input_Pressed();
        public abstract bool Input_Up();

        
        
        public bool Input_GazeButton()
        {
            return clickframe == Time.frameCount;
        }
        public virtual bool Input_EscapeButton()
        {
            return Input.GetKeyUp(KeyCode.Escape);
        }

        
        protected abstract float clickT();

        public float Input_ClickT
        {
            get { return Mathf.Max(0.25f, _clickT); }
        }
        public float Input_DoubleClickT
        {
            get { return Mathf.Max(0.25f, doubleClickT); }
        }
        public float Input_LongPressT
        {
            get { return Mathf.Max(0.25f, longPressT); }
        }


        public virtual void SetupCanvas(Canvas canvas)
        {

        }

        public virtual void ToggleCanvas(Canvas canvas, bool b)
        {

        }

        protected void toggleRaycasters<TCaster>(bool state, System.Func<TCaster, bool> condition=null, System.Action<GameObject> objHandler=null) where TCaster : GraphicRaycaster
        {
     //       Debug.Log(this.GetType() + " toggleRaycasters=[" + state + "]");
            var b = new System.Text.StringBuilder();
            foreach(var caster in GameObject.FindObjectsOfType<TCaster>())
            {
                objHandler?.Invoke(caster.gameObject);
                if((condition == null || condition(caster)))
                {
                    caster.enabled = state;
     //               b.Append("\n\tset <" + caster.name + "> to " + state.ToString());
                }
                else
                {
                    caster.enabled = false;
                    //b.Append("\n\tset <" + caster.name + "> to False, condition not met.");
                }
            }
      //      Debug.Log(b);
        }
        protected void toggleRaycasters<TCaster>(GameObject rootObj, bool state, System.Func<TCaster, bool> condition=null) where TCaster : GraphicRaycaster
        {
  //          Debug.Log(this.GetType() + " toggleRaycasters B =[" + state + "]");
            foreach(var caster in rootObj.GetComponentsInHierarchy<TCaster>())
            {
                if(caster.gameObject.activeInHierarchy && (condition == null || condition(caster)))
                {
                    caster.enabled = state;
                }
            }
        }
        protected void toggleRaycasters<TCaster>(GameObject[] rootObjs, bool state, System.Func<TCaster, bool> condition=null) where TCaster : GraphicRaycaster
        {
            foreach(var root in rootObjs)
            {
                toggleRaycasters(root, state, condition);
            }
        }

        private float inputDownT;
        private int clickframe;

        protected virtual void Update()
        {
            if(Input_Down())
            {   
                inputDownT = Time.time;

//                Debug.Log("INPUT >>> DOWN");
            }
            else if(Input_Up())
            {   
//                Debug.Log("INPUT >>> UP.... click? " + (Time.time <= inputDownT + clickT()));
                if(Time.time <= inputDownT + clickT() && Time.frameCount != clickframe)
                {
                    clickframe = Time.frameCount;
                    clickInput?.Invoke();
                }
            }
            
        }
    }


}