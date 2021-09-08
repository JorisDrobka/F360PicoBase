using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using VRIntegration;


[System.Flags]
public enum VRSpace
{
    Unspecific=0,
    Front= 1,
    Left = 2,
    Right = 4,
    Back = 8,

    Active = Front | Left | Right,
    Inactive = Back
}



/// @brief
/// platform-independent access to VR input & orientation.
///
public class VRInterface : MonoBehaviour
{


    public enum Platform
    {
        Desktop,
        Oculus,
        Pico,
        Mobile,
        NotSupported
    }



    [SerializeField] private float renderScale = 1.5f;
    
    [SerializeField, HideInInspector] private VRIntegrationBase current;

    private float defaultRenderScale;

    private static VRInterface Instance;

    private void Awake()
    {
        if(Instance != null)
        {
            GameObject.Destroy(this);
        }
        else
        {
            Instance = this;
            
            defaultRenderScale = renderScale;
            SetRenderScaleDefault();
            GameObject.DontDestroyOnLoad(this.gameObject);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    void Update()
    {
        if(current != null)
        {
            if(VRClickEvent != null && current.Input_GazeButton())
            {
                VRClickEvent();
            }
        }
    }


    //-----------------------------------------------------------------------------------------------------------------
    //  
    //  INPUT
    //
    //-----------------------------------------------------------------------------------------------------------------

    public static event System.Action VRClickEvent;


    public static Ray GetGaze() 
    {
        if(Instance != null && Instance.current != null)
        {
            return Instance.current.GetGaze();
        }
        return new Ray();
    }
    public static UnityEngine.EventSystems.EventSystem GetEventSystem()
    {
        if(Instance != null && Instance.current != null)
        {
            return Instance.current.GetEventSystem();
        }
        return null;
    }
    public static Camera GetMainCamera()
    {
        if(Instance != null && Instance.current != null)
        {
            return Instance.current.eventCam;
        }
        return null;
    }
    
    
    public static float Input_ClickT
    {
        get {
            return Instance != null && Instance.current != null ? Instance.current.Input_ClickT : 0.35f;
        }
    }
    public static float Input_DoubleClickT
    {
        get {
            return Instance != null && Instance.current != null ? Instance.current.Input_DoubleClickT : 0.3f;
        }
    }
    public static float Input_LongPressT
    {
        get {
            return Instance != null && Instance.current != null ? Instance.current.Input_LongPressT : 0.75f;
        }
    }

    public static bool Input_GazeButtonClick(VRSpace constraint=VRSpace.Unspecific)
    {
        if(Instance != null && Instance.current != null && HasOrientationTowards(constraint))
        {
            return Instance.current.Input_GazeButton();
        }
        else return false;
    }
    
    public static bool Input_GazeButtonDown(VRSpace constraint=VRSpace.Unspecific)
    {
        if(Instance != null && Instance.current != null && HasOrientationTowards(constraint))
        {
            return Instance.current.Input_Down();
        }
        else return false;
    }
    public static bool Input_GazeButtonPressed(VRSpace constraint=VRSpace.Unspecific)
    {
        if(Instance != null && Instance.current != null && HasOrientationTowards(constraint))
        {
            return Instance.current.Input_Pressed();
        }
        else return false;
    }
    public static bool Input_GazeButtonUp(VRSpace constraint=VRSpace.Unspecific)
    {
        if(Instance != null && Instance.current != null && HasOrientationTowards(constraint))
        {
            return Instance.current.Input_Up();
        }
        else return false;
    }
    public static bool Input_EscapeButton(VRSpace constraint=VRSpace.Unspecific)
    {
        if(Instance != null && Instance.current != null && HasOrientationTowards(constraint))
        {
            return Instance.current.Input_EscapeButton();
        }
        else return false;
    }

    public static bool Input_CurrentPointerData(out UnityEngine.EventSystems.PointerEventData pointer)
    {
        pointer = null;
        if(Instance != null && Instance.current != null)
        {
            pointer = Instance.current.GetPointerData();
        }
        return pointer != null;
    }

    //-----------------------------------------------------------------------------------------------------------------
    //  
    //  PLATFORM
    //
    //-----------------------------------------------------------------------------------------------------------------

    /// @returns a generic Monobehaviour Coroutine host (unmanaged)
    ///
    public static MonoBehaviour CoroutineHost()
    {
        return Instance;
    }

    public static Platform GetCurrentPlatform() 
    {
        if(Instance != null)
            return Instance.GetPlatform(); 
        else {
            Debug.LogError("No Instance of VRDeveloperSettings found in current scene!");
            return Platform.NotSupported;
        }
    }

    public static string GetSDKVersion()
    {
        if(Instance != null)
            return Instance.current.GetSDKVersion();
        else 
            return "Error - Unknown";
    }

    public static string GetHardwareSerial()
    {
        if(Instance != null) {
            #if UNITY_EDITOR
            if(Instance.availableIntegrations.ContainsKey(Platform.Desktop)) {
                return Instance.availableIntegrations[Platform.Desktop].GetHardwareSerial();
            }
            #endif
            return Instance.current.GetHardwareSerial();
        }
        else
            return "Error - Unknown";
    }

    public static bool isVRPlatform()
    {
        if(Instance != null)
        {
            return isVRPlatform(Instance.GetPlatform());
        }
        else
        {
            return false;
        }
    }
    public static bool isVRPlatform(Platform p)
    {
        switch(p)
        {
            case Platform.Oculus:
            case Platform.Pico:     return true;
            default:                return false;
        }
    }

    public static void SetPlatform(Platform target)
    {
        if(Instance == null)
        {
            Debug.LogError("No Instance of VRDeveloperSettings found in current scene!");
        }
        else
        {
//            Debug.Log("VRSettings: switch platform to [" + target + "]");
            Instance.SwitchPlatform(target);
        }
    }

    public static void SetPlatformOfScene(Scene scene, Platform target)
    {
        if(Instance == null)
        {
            Debug.LogError("No Instance of VRDeveloperSettings found in current scene!");
        }
        else
        {
//            Debug.Log("VRSettings: switch platform of scene=[" + scene.name + "]  to  [" + target + "]");
            Instance.SetupSceneForPlatform(scene, target);
        }
    }

    

    public static void SetRenderScaleTo(float r)
    {
        if(Instance != null)
        {
            Instance.renderScale = r;
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = r;
//            Debug.Log("Set XR Renderscale to [ " + r + " ]");
        }
        else
        {
            Debug.LogError("No Instance of VRDeveloperSettings found in current scene!");
        }
    }
    public static void SetRenderScaleDefault()
    {
        if(Instance != null)
        {
            SetRenderScaleTo(Instance.defaultRenderScale);
        }
        else
        {
            Debug.LogError("No Instance of VRDeveloperSettings found in current scene!");
        }
    }


    //-----------------------------------------------------------------------------------------------------------------
    //  
    //  ORIENTATION
    //
    //-----------------------------------------------------------------------------------------------------------------
    

    public static bool HasOrientationTowards(VRSpace space, float tolerance=0.5f)
    {
        if(space == VRSpace.Unspecific)
        {
            return true;
        }
        else
        {
            bool oriented = true;
            foreach(var flag in GetFlags(space))
            {
                if(space != VRSpace.Back)
                {
                    oriented &= GetOrientation(space) >= tolerance;
                }
                else
                {
                    oriented &= !HasOrientationTowards(VRSpace.Front, tolerance) 
                            && !HasOrientationTowards(VRSpace.Left, tolerance) 
                            && !HasOrientationTowards(VRSpace.Right, tolerance);
                }
            }
            return oriented;
        }
    }

    public static float GetOrientation(VRSpace space)
    {
        var gaze = GetGaze();
        if(gaze.direction.sqrMagnitude > 0)
        {
            switch(space)
            {
                case VRSpace.Front:         return Vector3.Dot(gaze.direction, SPACE_FRONT);
                case VRSpace.Left:          return Vector3.Dot(gaze.direction, SPACE_LEFT);
                case VRSpace.Right:         return Vector3.Dot(gaze.direction, SPACE_RIGHT);
                case VRSpace.Back:          return Vector3.Dot(gaze.direction, SPACE_BACK);
                case VRSpace.Unspecific:    return 1;
            }
        }
        return -1;
    }

    static IEnumerable<VRSpace> GetFlags(VRSpace space)
    {   
        foreach(var val in System.Enum.GetValues(typeof(VRSpace)))
        {
            var flag = (VRSpace) val;
            if(space.HasFlag(flag))
            {
                yield return flag;
            }
        }
    }

    static Vector3 SPACE_FRONT = Vector3.forward;
    static Vector3 SPACE_RIGHT = Vector3.Lerp(Vector3.forward, Vector3.right, 0.75f);
    static Vector3 SPACE_LEFT = Vector3.Lerp(Vector3.forward, Vector3.left, 0.75f);
    static Vector3 SPACE_BACK = Vector3.back;

    //-----------------------------------------------------------------------------------------------------------------
    //  
    //  CANVAS
    //
    //-----------------------------------------------------------------------------------------------------------------

    public static void SetupCanvas(Canvas canvas, Platform target)
    {
        if(Instance != null)
        {
            Instance.SetupCanvasForPlatform(canvas, target);
        }
    }

    public static void ToggleCanvas(Canvas canvas, bool b)
    {
        if(Instance != null && Instance.current != null)
        {
            Instance.current.ToggleCanvas(canvas, b);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    //  editor interface

    public void SwitchPlatform(Platform target)
    {
        if(availableIntegrations.ContainsKey(target))
        {
            if(current != null && current.platform != target)
            {
                current?.Enable(false);
            }
            current = availableIntegrations[target];
            current.Enable(true);
        }
        else
        {
            Debug.LogError("VRIntegration of platform<" + target + "> not found!");
        }
    }

    ///<summary> call when additively adding a scene </summary>
    public void SetupSceneForPlatform(Scene scene, Platform target)
    {
        if(availableIntegrations.ContainsKey(target))
        {
            if(availableIntegrations[target] != current)
            {
                SwitchPlatform(target);
            }
            else
            {
                //  setup for scene only
                current.SetupScene(scene, true);
            }
        }    
    }

    public void SetupCanvasForPlatform(Canvas canvas, Platform target)
    {
        if(availableIntegrations.ContainsKey(target))
        {
            if(availableIntegrations[target] != current)
            {
                SwitchPlatform(target);
            }
            else
            {
                //  setup for scene only
                current.SetupCanvas(canvas);
            }
        }    
    }

    public IEnumerable<Platform> GetAvailablePlatforms()
    {
        foreach(var key in availableIntegrations.Keys)
        {
            yield return key;
        }
    }

    public Platform GetPlatform() 
    {
        return current != null ? current.platform : Platform.NotSupported;
    }

    public void UpdateAvailableIntegrations()
    {
        availableIntegrations.Clear();
        var i = gameObject.GetComponents<VRIntegrationBase>();
        foreach(var integration in i)
        {
            if(!availableIntegrations.ContainsKey(integration.platform))
            {
                availableIntegrations.Add(integration.platform, integration);
            }
            else
            {
                integration.enabled = false;
                Debug.LogWarning("found double integration of platform: " + integration.platform + " (GO: " + integration.name + ")");
            }
        }
    }


    Dictionary<Platform, VRIntegrationBase> availableIntegrations
    {
        get {
            if(_available == null)
            {
                _available = new Dictionary<Platform, VRIntegrationBase>();
                UpdateAvailableIntegrations();
            }
            return _available;
        }
    }

    Dictionary<Platform, VRIntegrationBase> _available;
    
}
