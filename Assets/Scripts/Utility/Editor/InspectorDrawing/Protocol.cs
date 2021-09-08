using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Utility.Inspector
{
    /// @brief
    /// helper object drawing a protocol to the inspector/editor window
    ///
    public class ProtocolDrawer
    {
        const string COL_LINE_CUSTOM = "protocolCLineC";
        const string COL_LINE_2 = "protocolCLine2";
        const string COL_LINE_3 = "protocolCLine3";
        const string COL_BUTTONS = "protocolCButtons";

        enum PLayout
        {
            Single,
            DateTime,
            Context,
            DateTimeAndContext,
            Custom
        }


        /// @brief
        /// autolayout the protocol with a timestamp for each message.
        /// Inactive when custom protocol layout was set in constructor.
        ///
        public bool show_timeStamp 
        {
             get { return _showTimeStamp; }
             set {
                 _showTimeStamp = value;
                 updateCurrentLayout();
             }
        }
        /// @brief
        /// Autolayout the protocol with an additional context provided by the user for each message.
        /// Inactive when custom protocol layout was set in constructor.
        ///
        public bool show_context 
        { 
            get { return _showContext; }
            set {
                _showContext = value;
                updateCurrentLayout();
            }
        }
        public Color BGColor    
        {
             get { return _bgColor; } 
             set {
                 _bgColor = value;
                 _lineColor = new Color(value.r-0.05f,value.g-0.05f, value.b-0.05f);
             }
        }
        public Color LineColor  
        { 
            get { return _lineColor; }
            set { _lineColor = value; }
        }
        

        //-----------------------------------------------------------------------------------------------------------------

        /// @brief 
        /// constructor
        ///
        /// @param layouter The layout object used to draw your editor
        /// @param protocolLayout (optional) custom column layout for the protocol. 
        /// This deactivates autolayout settings. Layout must have one, two or three columns.
        ///
        public ProtocolDrawer(Layouter layouter, IColumnLayout protocolLayout=null)
        {
            this.layouter = layouter;
            entries = new List<Entry>();
            BGColor = new Color(0.5f, 0.5f, 0.5f);
            
            if(protocolLayout != null)
            {
                layouter.AddColumnLayout(COL_LINE_CUSTOM, protocolLayout);
                currLayout = PLayout.Custom;
                customColumnCount = protocolLayout.Count;
            }
            else
            {
                layouter.AddColumnLayout(COL_LINE_2, new WeightedColumn(1, 0.3f, 0.7f));
                layouter.AddColumnLayout(COL_LINE_2, new WeightedColumn(1, 0.2f, 0.2f, 0.6f));
            }
        }


        public void Add(string msg, string context="")
        {
            entries.Add(new Entry(msg, context));
            layouter.ForceRepaint();
        }
        public void Clear()
        {
            entries.Clear();
            layouter.ForceRepaint();
        }

        //-----------------------------------------------------------------------------------------------------------------


        /// @brief
        /// this must be called after Layout.Begin() in your editor GUI function.
        ///
        public void Draw(Rect area)
        {
            float y = area.y;        
            Drawing.Box(area, BGColor);

            Rect viewRect = new Rect(area.x, area.y, area.width-24, layouter.lineFormat.height * entries.Count);
            scrollP = GUI.BeginScrollView(area, scrollP, viewRect);
            for(int i = 0; i < entries.Count; i++)
            {
                drawEntry(i, ref y);
            }
            GUI.EndScrollView(true);


            y += 4;
            Drawing.HorizontalSeparator(ref y, 2, layouter);

            Rect[]b;
            Layout.GetNColumnLine(ref y, 4, out b);
            if(Drawing.Button("Clear", b[0]))
            {
                Clear();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  internal

        
        void drawEntry(int index, ref float y)
        {
            float startY = y;
            Rect line = Layout.GetNewLine(ref y);
            if(index % 2 == 0)
            {
                //  draw a shade darker every other line
                Drawing.Box(line, LineColor);
            }

            var entry = entries[index];
            var layout = currLayout;
            if(string.IsNullOrEmpty(entry.context))
            {
               if(layout == PLayout.Context) layout = PLayout.Single;
               else if(layout == PLayout.DateTimeAndContext) layout = PLayout.DateTime; 
            }

            var column = "";
            switch(layout)
            {                
                case PLayout.Context:               column = COL_LINE_2; break;
                case PLayout.DateTime:              column = COL_LINE_2; break;
                case PLayout.DateTimeAndContext:    column = COL_LINE_3; break;
                case PLayout.Custom:
                    if(customColumnCount == 3)
                    {
                        layout  = !string.IsNullOrEmpty(entry.context)
                                ? PLayout.DateTimeAndContext
                                : PLayout.DateTime;
                    }
                    else if(customColumnCount == 2)
                    {
                        layout  = !string.IsNullOrEmpty(entry.context)
                                ? PLayout.Context
                                : PLayout.DateTime;
                    }
                    else
                    {
                        layout = PLayout.Single;
                    }
                    column = COL_LINE_CUSTOM;
                    break;
            }

            Rect[] r;
            switch(layout)
            {
                case PLayout.Single:
                    EditorGUI.LabelField(line, entry.msg);
                    break;

                case PLayout.Context:
                    r = new Rect[2];
                    Layout.GetTwoColumnLine(ref startY, out r[0], out r[1], layouter.Column(column));
                    EditorGUI.LabelField(r[0], entry.context);
                    EditorGUI.LabelField(r[1], entry.msg);
                    break;

                case PLayout.DateTime:
                    r = new Rect[2];
                    Layout.GetTwoColumnLine(ref startY, out r[0], out r[1], layouter.Column(column));
                    EditorGUI.LabelField(r[0], entry.FormatDateTime());
                    EditorGUI.LabelField(r[1], entry.msg);
                    break;

                case PLayout.DateTimeAndContext:
                    r = new Rect[3];
                    Layout.GetThreeColumnLine(ref startY, out r[0], out r[1], out r[2], layouter.Column(column));
                    EditorGUI.LabelField(r[0], entry.FormatDateTime());
                    EditorGUI.LabelField(r[1], entry.context);
                    EditorGUI.LabelField(r[2], entry.msg);
                    break;
            }
        }

        

        void updateCurrentLayout()
        {
            if(currLayout != PLayout.Custom)
            {
                if(show_context && show_timeStamp)
                {
                    currLayout = PLayout.DateTimeAndContext;
                }
                else if(show_context)
                {
                    currLayout = PLayout.Context;
                }
                else if(show_timeStamp)
                {
                    currLayout = PLayout.DateTime;
                }   
                else
                {
                    currLayout = PLayout.Single;
                }
                layouter.ForceRepaint();
            }
        }



        //-----------------------------------------------------------------------------------------------------------------

        readonly Layouter layouter;
        List<Entry> entries;
        PLayout currLayout;
        int customColumnCount = 0;
        bool _showTimeStamp, _showContext;
        Color _bgColor, _lineColor;
        Vector2 scrollP;

        struct Entry
        {
            public string context;
            public string msg;
            public System.DateTime timeStamp;

            public Entry(string msg, string context="")
            {
                this.context = context;
                this.msg = msg;
                this.timeStamp = System.DateTime.Now;
            }

            public string FormatDateTime()
            {
                return "";
            }
        }
    }

}   

