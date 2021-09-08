using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;


namespace Utility.Inspector
{
    public static class Drawing
    {

        //-----------------------------------------------------------------------------------------------------------------

        //  States

        /// @brief
        /// displays the next control element (button, toggle, list etc) set
        /// via the Drawing class as inactive if given condition isn't met
        ///
        public static void SetControlCondition(bool condition)
        {
            _nextInactive = !condition;
        }
        static bool _nextInactive;
        static Color _cachedColor;

        static bool openState() 
        {
            _cachedColor = UnityEngine.GUI.color;
            if(_nextInactive)
            {
                alpha(0.5f);
                return false;
            }
            return true;
        }
        static void closeState() 
        {
            color(_cachedColor);
            _nextInactive = false;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  GUI color

        public static void color(Color c)
        {
            UnityEngine.GUI.color = c;
        }
        public static void alpha(float a)
        {
            Color c = UnityEngine.GUI.color;
            color(new Color(c.r, c.g, c.b, a));
        }
        public static void colorDefault()
        {
            UnityEngine.GUI.color = Color.white;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  basic elements

        public static void HorizontalSeparator(ref float y, int indent=0, Layouter layouter=null)
        {
            Rect r;
            if(layouter != null)
            {
                r = layouter.currentLine;
                r.y = y;
            }
            else
            {
                float yy = y; 
                r = Layout.GetNewLine(ref yy);
                r.y = y;
            }
            r.width -= indent * 2;
            r.x += indent;
            r.height = 2;
            UnityEngine.GUI.Box(r, "");
            y += r.height + 4;
        }

        public static void Box(Rect r)
        {
            UnityEngine.GUI.Box(r, "");
        }
        public static void Box(Rect r, Color c)
        {
            color(c);
            Box(r);
            colorDefault();
        }
        public static void Box(string label, Rect r)
        {
            UnityEngine.GUI.Box(r, label);
        }

        //-----------------------------------------------------------------------------------------------------------------
        
        //  buttons

        public static bool Button(string label, Rect r, float maxheight=0)
        {
            bool canInteract = openState();
            if(maxheight > 0)
                r.height = Mathf.Min(r.height, maxheight);
            bool result = GUI.Button(r, label);
            closeState();
            return canInteract && result;
        }
        public static bool Button(string label, Rect r, GUIStyle style, float maxheight=0)
        {
            bool canInteract = openState();
            if(maxheight > 0)
                r.height = Mathf.Min(r.height, maxheight);
            bool result = GUI.Button(r, label, style);
            closeState();
            return canInteract && result;
        }
        public static bool ButtonCentered(string label, Rect r, float width)
        {
            bool canInteract = openState();
            if(width > r.width)
            {
                width = r.width;
            }
            float d = (r.width - width) / 2;
            Rect rr = new Rect(r.x + d, r.y, width, r.height);
            bool result = GUI.Button(rr, label);
            closeState();
            return canInteract && result;
        }
        public static bool ButtonCentered(string label, Rect r, float width, GUIStyle style)
        {
            bool canInteract = openState();
            if(width > r.width)
            {
                width = r.width;
            }
            float d = (r.width - width) / 2;
            Rect rr = new Rect(r.x + d, r.y, width, r.height);
            bool result = GUI.Button(rr, label, style);
            closeState();
            return canInteract && result;
        }
        
        public static int RadioButtonGroup(Rect r, string[] labels, int selected)
        {
            bool canInteract = openState();
            if(labels.Length > 0)
            {
                int id = -1;
                Rect rr = new Rect(r);
                rr.width = r.width / labels.Length;
                for(int i = 0; i < labels.Length; i++)
                {
                    if(GUI.Button(rr, labels[i]))
                    {
                        id = i;
                    }
                    rr.x += rr.width;
                }
                return canInteract ? id : selected;
            }
            closeState();
            return -1;
        }




        //-----------------------------------------------------------------------------------------------------------------

    }

}

