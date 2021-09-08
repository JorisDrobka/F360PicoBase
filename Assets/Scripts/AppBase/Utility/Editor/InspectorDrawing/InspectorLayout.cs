using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;


namespace Utility.Inspector
{


    public static class Layout
    {
    
        const int LINE_HEIGHT = 22;
        const int MARGIN_RIGHT = 10;
        const int MARGIN_LEFT = 16;

        private static Rect line = new Rect(0,0,100,LINE_HEIGHT);
        private static Rect area;
        private static float _indent;
        private static float _w;
        private static Layouter LAYOUTER;


        //-----------------------------------------------------------------------------------------------------------------

        //  getter

        public static int GetInspectorWidth()
        {
            return Screen.width;
        }

        public static IColumnLayout Column2
        {
            get { return __2Column; }
        }
        private static IColumnLayout __2Column = new WeightedColumn(3, 0.5f, 0.5f);

        public static IColumnLayout Column3 
        {
            get { return __3Column; }
        }
        private static IColumnLayout __3Column = new WeightedColumn(3, 0.33f, 0.33f, 0.33f);

        public static IColumnLayout Column4 
        {
            get { return __4Column; }
        }
        private static IColumnLayout __4Column = new WeightedColumn(3, 0.25f, 0.25f, 0.25f, 0.25f);
        public static IColumnLayout Column5 
        {
            get { return __5Column; }
        }
        private static IColumnLayout __5Column = new WeightedColumn(3, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f);

        public static IColumnLayout FilePathColumm
        {
            get { return __filepathColumn; }
        }
        private static IColumnLayout __filepathColumn = new WeightedColumn(3, 0.7f, 0.15f, 0.15f);

        public static IColumnLayout AssetLoadOrCreateColumn
        {
            get { return __assetLoadOrCreateColumn; }
        }
        private static IColumnLayout __assetLoadOrCreateColumn = new WeightedColumn(3, 0.85f, 0.15f);

        //-----------------------------------------------------------------------------------------------------------------

        //  settings & preparation

        /// @brief
        /// call this in OnEnable() to get a custom layouter.
        ///
        public static Layouter CreateLayouter(System.Action repaint)
        {
            return new Layouter(line, repaint);
        }

        /// @brief
        /// call this at the start of your layout function
        ///
        public static void BeginLayout(Layouter layouter, float maxWidth=-1)
        {
            if(Event.current.type == EventType.Layout)
            {
                LAYOUTER = layouter;
                maxWidth = GetMaxWidth(maxWidth);
                LAYOUTER._begin(maxWidth);
                ResetArea();
            }
        }

        public static void BeginArea(Rect a)
        {
            if(true || Event.current.type == EventType.Layout)
            {
                area = a;
                if(LAYOUTER != null)
                {
                    LAYOUTER.currentArea = a;
                    SetLine(LAYOUTER.maxWidth, LAYOUTER.indent, LAYOUTER.lineFormat.height);
                }
                else
                {
                    SetLine(_w, _indent, line.height);
                }
            }
        }
        public static void ResetArea()
        {
            BeginArea(new Rect(0,0,Screen.width,Screen.height));
        }

        private static float GetMaxWidth(float maxW=-1)
        {
            if(maxW <= 0 || maxW > area.width-MARGIN_RIGHT)
            {
                maxW = area.width - MARGIN_RIGHT - MARGIN_LEFT;
            }
            return maxW;
        }


        /// @brief
        /// call after BeginLayout() to customize draw area
        ///
        public static void SetLine(float width, float indent=0, float height=LINE_HEIGHT)
        {
            width = GetMaxWidth(width);
            if(width <= 0 || width > area.width-MARGIN_RIGHT)
            {
    //            width = area.width - MARGIN_RIGHT - MARGIN_LEFT;
            }
            if(LAYOUTER != null)
            {
                indent += LAYOUTER.setting_base_indent;
            }
            _indent = indent;
            _w = width;

            line = new Rect(indent + MARGIN_LEFT + area.x, area.y, width-indent, height);
            if(LAYOUTER != null)
            {
                LAYOUTER.lineFormat = line;
                LAYOUTER.indent = _indent;
            }
        }
        public static void SetLineHeight(float height=LINE_HEIGHT)
        {
            if(LAYOUTER != null)
            {
                SetLine(LAYOUTER.lineFormat.width, LAYOUTER.indent, height);
            }
            else
            {
                SetLine(line.width, _indent, height);
            }
        }

        public static void SetIndent(float amount=0)
        {
            if(LAYOUTER != null)
            {
                SetLine(LAYOUTER.lineFormat.width, amount, LAYOUTER.lineFormat.height);
            }
            else
            {
                SetLine(line.width, amount, line.height);
            }
        }
        

        //-----------------------------------------------------------------------------------------------------------------

        //  general layouting functions

        /// @brief
        /// TODO: zzzzzzzzzzzzzzzzzzzzzz
        ///
        public static Rect GetNewLine(ref float y)
        {
            if(LAYOUTER != null)
            {
                Rect r = new Rect(LAYOUTER.lineFormat);
                y += LAYOUTER.lineFormat.height;
                r.y = y;
                LAYOUTER._progress(r);
                return r;
            }
            else
            {
                Rect r = new Rect(line);
                y += line.height;
                r.y = y;
                y += r.height;
                return r;
            }
        }

        public static Rect GetCenteredLine(ref float y, float width)
        {
            Rect r = GetNewLine(ref y);
            if(r.width > width)
            {
                float d = (r.width - width) / 2f;
                r.x += d;
                r.width = width;
            }
            return r;
        }

        public static float CalcHeight(int numLines, float marginTop=0, float marginBottom=0)
        {
            return (marginBottom + marginTop) + numLines * (LAYOUTER != null ? LAYOUTER.lineFormat.height : line.height);
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  column line layouting functions

        public static Rect GetTwoColumnLine(ref float y, out Rect col1, out Rect col2)
        {
            Rect r = GetNewLine(ref y);
            TwoColumnLayout(r, out col1, out col2);
            return r;
        }
        public static Rect GetTwoColumnLine(ref float y, out Rect col1, out Rect col2, IColumnLayout layout)
        {
            Rect r = GetNewLine(ref y);
            TwoColumnLayout(r, out col1, out col2, layout);
            return r;
        }
        public static Rect GetThreeColumnLine(ref float y, out Rect col1, out Rect col2, out Rect col3)
        {
            Rect r = GetNewLine(ref y);
            ThreeColumnLayout(r, out col1, out col2, out col3);
            return r;
        }
        public static Rect GetThreeColumnLine(ref float y, out Rect col1, out Rect col2, out Rect col3, IColumnLayout layout)
        {
            Rect r = GetNewLine(ref y);
            ThreeColumnLayout(r, out col1, out col2, out col3, layout);
            return r;
        }
        public static Rect GetNColumnLine(ref float y, int count, out Rect[] columns)
        {
            Rect r = GetNewLine(ref y);
            NColumnLayout(r, count, out columns);
            return r;
        }
        public static Rect GetNColumnLine(ref float y, int count, out Rect[] columns, IColumnLayout layout)
        {
            Rect r = GetNewLine(ref y);
            NColumnLayout(r, count, out columns, layout);
            return r;
        }

        //  basic column layouting functions

        public static void TwoColumnLayout(Rect area, out Rect col1, out Rect col2)
        {
            var w = area.width/2;
            col1 = new Rect(area.x, area.y, w, area.height);
            col2 = new Rect(area.x+w, area.y, w, area.height);
            visualizeColumns(col1, col2);
        }
        public static void TwoColumnLayout(Rect area, out Rect col1, out Rect col2, IColumnLayout layout)
        {
            if(!layout.isValid())
            {
                TwoColumnLayout(area, out col1, out col2);
            }
            else
            {
                var w = area.width;
                col1 = layout.GetRect(0, 2, area, LAYOUTER);
                col2 = layout.GetRect(1, 2, area, LAYOUTER);
                visualizeColumns(col1, col2);
            }
        }
        public static void ThreeColumnLayout(Rect area, out Rect col1, out Rect col2, out Rect col3)
        {
            var w = area.width/3;
            col1 = new Rect(area.x, area.y, w, area.height);
            col2 = new Rect(col1.x+w, area.y, w, area.height);
            col3 = new Rect(col2.x+w, area.y, w, area.height);
            visualizeColumns(col1, col2, col3);
        }
        public static void ThreeColumnLayout(Rect area, out Rect col1, out Rect col2, out Rect col3, IColumnLayout layout)
        {
            if(!layout.isValid())
            {
                Debug.Log(RichText.color("Fallback!!!", Color.green));
                ThreeColumnLayout(area, out col1, out col2, out col3);
            }
            else
            {
                var w = area.width;
                col1 = layout.GetRect(0, 3, area, LAYOUTER);
                col2 = layout.GetRect(1, 3, area, LAYOUTER);
                col3 = layout.GetRect(2, 3, area, LAYOUTER);
                visualizeColumns(col1, col2, col3);
            }
        }

        public static void NColumnLayout(Rect area, int count, out Rect[] columns)
        {
            count = Mathf.Max(1, count);
            columns = new Rect[count];

            var w = area.width / count;
            var x = area.x;
            for(int i = 0; i < count; i++)
            {
                columns[i] = new Rect(x, area.y, w, area.height);
                x += w;
            }
            visualizeColumns(columns);
        }
        public static void NColumnLayout(Rect area, int count, out Rect[] columns, IColumnLayout layout)
        {
            if(!layout.isValid())
            {
                NColumnLayout(area, count, out columns);
            }
            else
            {
                count = Mathf.Max(1, count);
                columns = new Rect[count];

                var w = area.width / count;
                var x = 0f;
                for(int i = 0; i < count; i++)
                {
                    columns[i] = layout.GetRect(i, count, area, LAYOUTER);
                    x += w;
                }
                visualizeColumns(columns);
            }
        }

        private static void visualizeColumns(params Rect[] r)
        {
            for(int i = 0; i < r.Length; i++) {
                visualizeColumn(r[i]);
            }
        }
        private static void visualizeColumn(Rect r)
        {
            if(LAYOUTER != null && LAYOUTER.setting_visualizeLayout)
            {
                //  debug draw layouted areas
                GUI.color = Color.red;
                GUI.Box(r, "");
                GUI.color = Color.white;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  TODO:  grid...
       
    }


}
