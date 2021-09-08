using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(VRInterface))]
public class VRDeveloperSettingsInspector : Editor 
{

    VRInterface mTarget;

    private void OnEnable()
    {
        mTarget = (VRInterface)target;
        mTarget.UpdateAvailableIntegrations();
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Current Platform: " + mTarget.GetPlatform().ToString());
        GUILayout.Space(6);
        EditorGUILayout.LabelField("Available Modes");
        foreach(var platform in mTarget.GetAvailablePlatforms())
        {
            if(GUILayout.Button("Set Platform:: " + platform.ToString()))
            {
                Debug.Log("Set Platform:: " + platform);
                mTarget.SwitchPlatform(platform);

                EditorSceneManager.MarkAllScenesDirty();
                EditorSceneManager.SaveOpenScenes();
            }
        }
    }
}