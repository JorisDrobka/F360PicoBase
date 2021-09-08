using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;



public class SelectMySceneReferencesWindow : EditorWindow
{
    

    [MenuItem("Utility/Select my References in Scene")]
    static void Perform()
    {
        if(CheckSelectCondition())
        {
            self = EditorWindow.GetWindow<SelectMySceneReferencesWindow>(true, "Select my Scene References");
            self.minSize = new Vector2(260, 400);
            self.findReferences(Selection.activeTransform);
        }
        else
        {
            Debug.LogWarning("No Scene Object selected!");
        }
    }

    static SelectMySceneReferencesWindow self;
    
    static bool CheckSelectCondition()
    {
        return Selection.activeTransform != null && Selection.gameObjects.Length == 1;
    }
    
    Dictionary<GameObject, string> results = new Dictionary<GameObject, string>();


    void OnGUI()
    {
        bool hasSelection = CheckSelectCondition();
        
        GUILayout.Space(6);
        GUILayout.Label("Item to search:" + (hasSelection ? Selection.activeTransform.name : "None selected"));
        GUILayout.Space(6);
        
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.color = hasSelection ? Color.white : new Color(1,1,1,0.4f);
        if(GUILayout.Button("Search selected") && hasSelection)
        {
            findReferences(Selection.activeTransform);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(16);
        foreach(var key in results.Keys)
        {
            drawEntry(key, results[key]);
        }
    }

    void drawEntry(GameObject obj, string descriptor)
    {
        //  label
        //  select button
        GUILayout.BeginHorizontal();
        GUILayout.Label(formatObjPathName(obj));
        if(GUILayout.Button("Select", GUILayout.Width(64)))
        {
            Selection.objects = new Object[] { obj };
        }
        GUILayout.Space(24);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(24);
        GUILayout.Label(descriptor);
        GUILayout.Space(24);
        GUILayout.EndHorizontal();
    }


    void findReferences(Transform toFind)
    {
        results.Clear();
        List<int> instanceIDs = new List<int>();
        instanceIDs.Add(toFind.GetInstanceID());
        instanceIDs.Add(toFind.gameObject.GetInstanceID());
        foreach(var component in toFind.GetComponents<Component>())
        {
            instanceIDs.Add(component.GetInstanceID());
        }

        var scene = EditorSceneManager.GetActiveScene();
        foreach(var root in scene.GetRootGameObjects())
        {
            search(root.transform, toFind, instanceIDs);
        }
    }

    void search(Transform t, Transform toFind, List<int> searchIds)
    {
        if(t == toFind) return;
        foreach(var c in t.GetComponents<Component>())
        {
            if(c != null)
            {
                var obj = new SerializedObject(c);
                var iter = obj.GetIterator();
                searchProperty(c, iter, searchIds);
                while(iter.Next(true))
                {
                    searchProperty(c, iter, searchIds);
                }
            }
        }

        for(int i = 0; i < t.childCount; i++)
        {
            search(t.GetChild(i), toFind, searchIds);
        }
    }

    void searchProperty(Component component, SerializedProperty prop, List<int> searchIds)
    {
        if(ValidateComponent(component) 
            && prop != null 
            && prop.propertyType == SerializedPropertyType.ObjectReference 
            && prop.objectReferenceInstanceIDValue > 0)
        {
            if(component.GetType().Name == "ButtonFeedback")
            {
//                Debug.Log("check " + component.gameObject.name + "[" + component.GetType().Name + "] --> " + prop.displayName);
            }
            
            foreach(var id in searchIds)
            {
                if(prop.objectReferenceInstanceIDValue == id)
                {
                    if(!results.ContainsKey(component.gameObject))
                    {
                        results.Add(component.gameObject, formatLinkDescription(component, prop.objectReferenceValue));
                    }
                    else
                    {
                        results[component.gameObject] += "\n" + formatLinkDescription(component, prop.objectReferenceValue);
                    }
                }
            }
        }
    }

    bool ValidateComponent(Component c)
    {
        return !(c is RectTransform || c is CanvasRenderer);
    }

    string formatObjPathName(GameObject go)
    {
        var b = new System.Text.StringBuilder();
        b.Append(go.name);
        var parent = go.transform.parent;
        while(parent != null)
        {
            b.Insert(0, parent.name + "/");
            parent = parent.parent;
        }
        return b.ToString();
    }

    string formatLinkDescription(Component host, Object link)
    {
        return host.GetType().Name + "=>" + link.GetType().Name;
    }
}
