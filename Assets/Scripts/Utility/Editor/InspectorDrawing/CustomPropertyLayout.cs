using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Utility.Inspector
{
    /// @brief
    /// implement this interface to encapsulate inspector drawing 
    /// making it a normal PropertyDrawer.
    ///
    public interface ICustomPropertyDrawer
    {
        Editor inspector { get; }   ///< null if drawn as property
        Layouter layout { get; }    //< layouter state


        void Begin(Editor inspector=null);
        void End();
        void Cleanup();
        Rect Draw(Rect area);
        Rect Draw(float lineY, float maxWidth=-1);
    }

    
    //-----------------------------------------------------------------------------------------------------------------


    public abstract class CustomPropertyLayout
    {
        protected readonly Layouter mLayout;
        protected Editor mInspector { get; private set; }

        private SerializedObject mSerializedObj;
        private SerializedProperty mSerializedProp;


        public Editor inspector { get { return mInspector; } }
        public Layouter layout { get { return mLayout; } }

        public CustomPropertyLayout(Layouter layouter, SerializedObject obj, string propertyPath="")
        {
            this.mLayout = layouter;
            mSerializedObj = obj;
            if(!string.IsNullOrEmpty(propertyPath))
            {
                mSerializedProp = mSerializedObj.FindProperty(propertyPath);
                UnityEngine.Assertions.Assert.IsNotNull(mSerializedProp, "property path=[" + propertyPath + "] not valid!");
            }
        }

        public virtual void Begin(Editor inspector=null)
        {
            mInspector = inspector;
        }
        public virtual void End()
        {
            if(mInspector != null)
            {
                //  reserve space after drawing in inspector
                GUILayout.Space(mLayout.layoutedArea.height);
            }
        }
        public virtual void Cleanup() 
        {
            mInspector = null;
        }

        public Rect Draw(Rect area)
        {
            mLayout.setting_base_indent = area.x;
            return Draw(area.y, area.width);
        }

        public Rect Draw(float startY, float maxWidth=-1)
        {
            mSerializedObj.Update();
            drawInternal(startY, maxWidth);
            mSerializedObj.ApplyModifiedProperties();
            return mLayout.layoutedArea;
        }

        protected abstract void drawInternal(float startY, float maxWidth);

        protected SerializedProperty FindProperty(string property)
        {
            if(mSerializedProp != null)
            {
                return mSerializedProp.FindPropertyRelative(property);
            }
            else
            {
                return mSerializedObj.FindProperty(property);
            }
        }

        protected SerializedProperty FindPropertyRelative(SerializedProperty parent, string property)
        {
            if(parent != null)
            {
                return parent.FindPropertyRelative(property);
            }
            return null;
        }
    }

}

