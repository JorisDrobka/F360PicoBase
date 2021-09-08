using UnityEditor;
using UnityEngine;

namespace Utility.Materials
{

    [CustomEditor(typeof(MaterialAnimatorPreset))]
    public class MaterialAnimatorPresetInspector : Editor 
    {
        
        private MaterialAnimatorPreset preset;

        void OnEnable()
        {
            preset = (MaterialAnimatorPreset) target;
        }


        public override void OnInspectorGUI()
        {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("stateName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("propertyName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tween"));
            
            var typeProp = serializedObject.FindProperty("type");
            EditorGUILayout.PropertyField(typeProp);
            
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.BeginVertical();

            switch((ShaderProperty) typeProp.enumValueIndex)
            {
                case ShaderProperty.FLOAT:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("float_start"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("float_target"));
                    break;
                case ShaderProperty.VEC2:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("vec2_start"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("vec2_target"));
                    break;
                case ShaderProperty.VEC3:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("vec3_start"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("vec3_target"));
                    break;
                case ShaderProperty.VEC4:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("vec4_start"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("vec4_target"));
                    break;
                case ShaderProperty.COLOR:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("color_start"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("color_target"));
                    break;
                case ShaderProperty.TEX:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("texture"));
                    break;
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }



}