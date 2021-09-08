using UnityEditor;
using UnityEngine;

using Utility.Inspector;

namespace Utility.UI.Tweens
{

    public static class TweenDrawingHelper
    {
        static GUIContent EMPTY = new GUIContent();
        static Color DARK_YELLOW = new Color(0.975f,0.85f,0);



        //-----------------------------------------------------------------------------------------------------------------

        public class TweenPresetDrawer : CustomPropertyLayout
        {            
            string label;
            
            IColumnLayout headerColumn;
            IColumnLayout dataColumn;

            public TweenPresetDrawer(string label, SerializedObject obj, Layouter layouter=null)
                : base( layouter != null ? layouter : Layout.CreateLayouter(null), obj )
            {
                this.label = label;
                headerColumn = new WeightedColumn(2, 0.5f, 0.35f, 0.15f);
                dataColumn = new WeightedColumn(2, 0.3f, 0.2f, 0.5f);
            }  

        
            public override void Begin(Editor inspector)
            {
                base.Begin(inspector);
            }
            public override void End()
            {
                base.End();
            }
            public override void Cleanup()
            {
                base.Cleanup();
            }


            protected override void drawInternal(float lineY, float maxWidth=-1)
            {
                var prop_foldIN = FindProperty("_foldout_IN");
                var prop_foldOUT = FindProperty("_foldout_OUT");
                var prop_useOUT = FindProperty("_useOutState");

                GUI.matrix = Matrix4x4.identity;

                Rect line, col1, col2, col3;
                Layout.BeginLayout(mLayout);
                Layout.SetLine(maxWidth);

                float startY = lineY;
                line = Layout.GetThreeColumnLine(ref lineY, out col1, out col2, out col3);
                
                EditorGUI.LabelField(col1, label, EditorStyles.boldLabel);
                EditorGUI.LabelField(col2, "out state:", EditorStyles.whiteMiniLabel);
                EditorGUI.PropertyField(col3, prop_useOUT, EMPTY);


                line = Layout.GetTwoColumnLine(ref lineY, out col1, out col2);
                prop_foldIN.boolValue = EditorGUI.Foldout(col1, prop_foldIN.boolValue, prop_useOUT.boolValue ? "Fade In" : "Settings", true, EditorStyles.foldoutHeader);
                if(prop_useOUT.boolValue)
                {
                    prop_foldOUT.boolValue = EditorGUI.Foldout(col2, prop_foldOUT.boolValue, "Fade Out", true, EditorStyles.foldoutHeader);
                }
                                
                Layout.SetIndent(6);
                if(prop_foldIN.boolValue)
                {
                    var useCurveIN = FindProperty("_useCurve_IN");
                    if(useCurveIN.boolValue)
                    {
                        drawCurveData(ref lineY, prop_useOUT.boolValue ? "Curve IN" : "Curve", FindProperty("_curve_IN"), useCurveIN, "use Curve");
                    }
                    else
                    {
                        drawTweenData(ref lineY, prop_useOUT.boolValue ? "Tween IN" : "Tween", FindProperty("_tween_IN"), useCurveIN, "use Curve");
                    }
                }
                if(prop_useOUT.boolValue)
                {
                    if(prop_foldOUT.boolValue)
                    {
                        var useCurveOut = FindProperty("_useCurve_OUT");
                        if(useCurveOut.boolValue)
                        {
                            drawCurveData(ref lineY, "Curve OUT", FindProperty("_curve_OUT"), useCurveOut, "use Curve");
                        }
                        else
                        {
                            drawTweenData(ref lineY, "Tween OUT", FindProperty("_tween_IN"), useCurveOut, "use Curve");
                        }
                    }
                }
            }


            void drawTweenData(ref float y, string label, SerializedProperty data, SerializedProperty toggle=null, string toggleLabel="")
            {
                _drawToggleHeader(ref y, label, toggle, toggleLabel);

                var type = FindPropertyRelative(data, "type");

                Rect col1, col2, col3;
                Rect line = Layout.GetThreeColumnLine(ref y, out col1, out col2, out col3, dataColumn);

                EditorGUI.LabelField(col1, "duration:", EditorStyles.miniLabel);
                EditorGUI.PropertyField(col2, FindPropertyRelative(data, "duration"), EMPTY);
                EditorGUI.PropertyField(col3, type, EMPTY);

                if(((EaseType)type.enumValueIndex).hasAdditionalParameters())
                {
                    Rect[] cols;
                    line = Layout.GetNColumnLine(ref y, 4, out cols);
                    EditorGUI.LabelField(cols[0], "paramA:", EditorStyles.miniLabel);
                    EditorGUI.PropertyField(cols[1], FindPropertyRelative(data, "paramA"), EMPTY);
                    EditorGUI.LabelField(cols[2], "paramB:", EditorStyles.miniLabel);
                    EditorGUI.PropertyField(cols[3], FindPropertyRelative(data, "paramB"), EMPTY);
                    
                }
            }

            void drawCurveData(ref float y, string label, SerializedProperty data, SerializedProperty toggle=null, string toggleLabel="")
            {
                _drawToggleHeader(ref y, label, toggle, toggleLabel);

                Rect col1, col2, col3;
                Rect line = Layout.GetThreeColumnLine(ref y, out col1, out col2, out col3, dataColumn);
                EditorGUI.LabelField(col1, "duration:", EditorStyles.miniLabel);
                EditorGUI.PropertyField(col2, FindPropertyRelative(data, "duration"), EMPTY);
                EditorGUI.PropertyField(col3, FindPropertyRelative(data, "curve"), EMPTY);
            }

            void _drawToggleHeader(ref float y, string header, SerializedProperty toggle, string toggleLabel="")
            {
                Rect col1, col2, col3;
                Layout.GetThreeColumnLine(ref y, out col1, out col2, out col3, headerColumn);
                EditorGUI.LabelField(col1, header, EditorStyles.miniBoldLabel);
                EditorGUI.LabelField(col2, toggleLabel, EditorStyles.whiteMiniLabel);
                EditorGUI.PropertyField(col3, toggle, EMPTY);
            }
        }


    }

}
