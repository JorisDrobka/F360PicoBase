using UnityEditor;
using UnityEngine;

using Utility.Inspector;

namespace Utility.UI.Tweens
{
    [CustomEditor(typeof(TweenPreset))]
    public class TweenPresetInspector : Editor 
    {
        
        TweenPreset preset;
        TweenDrawingHelper.TweenPresetDrawer drawer;

        void OnEnable()
        {
            preset = (TweenPreset) target;
            drawer = new TweenDrawingHelper.TweenPresetDrawer(target.name, serializedObject);
            drawer.layout.setting_base_indent = 6;

     //       drawer.layout.setting_visualizeLayout = true;
        }

        public override void OnInspectorGUI()
        {
            GUI.color = Color.grey;
            Rect r = new Rect(0,0,Screen.width,Screen.height).Constraint(drawer.layout.layoutedArea, false, true);
            GUI.Box(r, "");
            GUI.color = Color.white;

            drawer.Begin(this);
            Rect drawn = drawer.Draw(0);
            drawer.End();

            
        }
    }
}