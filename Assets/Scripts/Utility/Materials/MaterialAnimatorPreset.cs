using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Utility.UI.Tweens;

namespace Utility.Materials
{

    [CreateAssetMenu(fileName = "new MaterialAnimatorPreset", menuName = "FX/MaterialAnimatorPreset", order = 6)]
    public class MaterialAnimatorPreset : ScriptableObject
    {
        public string stateName = "new MaterialState";

        public string propertyName = "";

        public TweenData tween;

        public ShaderProperty type = ShaderProperty.FLOAT;

        public float float_start;
        public float float_target;

        public Vector2 vec2_start;
        public Vector2 vec2_target;

        public Vector3 vec3_start;
        public Vector3 vec3_target;

        public Vector4 vec4_start;
        public Vector4 vec4_target;

        public Color color_start;
        public Color color_target;

        public Texture2D texture;

    }


}