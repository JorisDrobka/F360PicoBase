using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Utility.Inspector
{

    public interface IInspectorLayouter
    {
        
    }


    public class Layouter
    {

        //  predefined keys for cached layout data.

        public const int KEY_COLUMN_SEQ = 101;
        public const int KEY_YPOS = 102;
        public const int KEY_LAST_RECT = 103;
        
        public const int KEY_XPOS = 200;
        public const int KEY_X_ARRAY = 1000;
        public const int KEY_Y_ARRAY = 2000;
        public const int KEY_W_ARRAY = 3000;


        //-----------------------------------------------------------------------------------------------------------------

        public float setting_base_indent { get; set; }      ///< Global indent, always applied on top
        public bool setting_visualizeLayout { get; set; }   ///< Draw layouted areas for debugging
        
        public Rect currentArea { get; internal set; }      ///< The current area being layouted
        public Rect layoutedArea { get; internal set; }     ///< The full area encompassed by this layout
        public Rect currentLine { get; internal set; }      ///< The currently layouted line
        public Rect lineFormat { get; internal set; }       ///< The current blueprint for a layouted line 
        public float indent { get; internal set; }          ///< The current indent
        public float maxWidth { get; internal set; }

        public float currentY { get { return currentLine.y; } }

        
        Dictionary<string, IColumnLayout> customColumns;
        Dictionary<string, Section> sections;

        Dictionary<ILayout, int> map;       //  holds temporary values of layout entities
        List<Dictionary<int, float>> _f;    //  float values
        List<Dictionary<int, Rect>> _r;     //  rect values
        int sequencer = 0;

        List<float> buffer;
        List<float> buffer2;
        System.Action repaintAction;
        string tmpOpenSection="";

        internal Layouter(Rect line, System.Action repaint)
        {
            this.indent = 0;
            this.lineFormat = line;
            this.map = new Dictionary<ILayout, int>();
            this._f = new List<Dictionary<int, float>>();
            this._r = new List<Dictionary<int, Rect>>();
            this.repaintAction = repaint;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  interface

        public bool isLayouting()
        {
            return Event.current.type == EventType.Layout;
        }

        /// @brief
        /// check if layout has already written to the state.
        ///
        public bool isLayouted(ILayout layout)
        {
            return map.ContainsKey(layout);
        }

        public void ForceRepaint()
        {
            repaintAction?.Invoke();
        }

        /// @brief
        /// retrieve a custom column layout
        ///
        public IColumnLayout Column(string id) 
        { 
            if(customColumns != null && customColumns.ContainsKey(id))
            {
                return customColumns[id];
            }
            return null;
        }

        /// @brief
        /// register a column layout to be used later on.
        ///
        public void AddColumnLayout(string id, IColumnLayout layout) 
        {
            if(customColumns == null) {
                customColumns = new Dictionary<string, IColumnLayout>();
            }
            if(!customColumns.ContainsKey(id)) {
                customColumns.Add(id, layout);
            }
        }

        /// @brief
        /// mark areas within the layout and calculate their size until the second GUI loop.
        /// this is useful to underlay sections with a BG image, for example.
        ///
        /// @returns section area.
        ///
        public Rect BeginSection(string id, ref float y, int indent, int contentMargin, float maxWidth=0)
        {
            return BeginSection(id, ref y, indent, new RectOffset(contentMargin, contentMargin, contentMargin, contentMargin), maxWidth);
        }
        /// @brief
        /// mark areas within the layout and calculate their size until the second GUI loop.
        /// this is useful to underlay sections with a BG image, for example.
        ///
        /// @returns section area.
        ///
        public Rect BeginSection(string id, ref float y, int indent, RectOffset contentMargin, float maxWidth=0)
        {
            maxWidth = maxWidth > 0 ? maxWidth : this.maxWidth;
            currentLine = new Rect(currentLine.x, y, currentLine.width, currentLine.height);
            y += contentMargin.top;
            Layout.SetLine(maxWidth-(contentMargin.left+contentMargin.right), indent);
            if(Event.current.type == EventType.Layout)
            {
                Rect result;
                if(sections == null) {
                    sections = new Dictionary<string, Section>();
                }
                if(!sections.ContainsKey(id))
                {
                    result = currentLine;
                    sections.Add(id, new Section(result, contentMargin));
                }
                else
                {
                    result = sections[id].currArea;
                    sections[id] = new Section(currentLine, contentMargin);
                }
                tmpOpenSection = id;
                return result;
            }
            else
            {
                tmpOpenSection = "";
                if(sections.ContainsKey(id))
                {
                    return sections[id].currArea;
                }
            }
            return new Rect();
        }
        /// @brief
        /// mark end of a section
        ///
        public void EndSection(ref float y)
        {
            currentLine = new Rect(currentLine.x, y, currentLine.width, currentLine.height);
            if(sections != null 
                && !string.IsNullOrEmpty(tmpOpenSection)
                && sections.ContainsKey(tmpOpenSection))
            {
                Section s = sections[tmpOpenSection];
                if(Event.current.type == EventType.Layout)
                {
                    //  safe area
                    Rect r = s.currArea;
                    r = r.Enlarge(currentLine);
                    r.width = Mathf.Min(r.width, maxWidth-indent);
                    r.height = r.height - s.currArea.y + s.margin.bottom;
                    sections[tmpOpenSection] = new Section(sections[tmpOpenSection], r);
                }
                y += s.margin.bottom;
            }
            tmpOpenSection = "";
        }

        struct Section
        {
            public Rect currArea;
            public RectOffset margin;
            public Section(Rect r, RectOffset m)
            {
                this.currArea = r;
                this.margin = m;
            }
            public Section(Rect r)
            {
                this.currArea = r;
                this.margin = new RectOffset();
            }
            public Section(Section s, Rect r)
            {
                this.currArea = r;
                this.margin = s.margin;
            }
        }   

        //-----------------------------------------------------------------------------------------------------------------

        //  internal interface 

        internal void _begin(float maxWidth=-1)
        {
            this.maxWidth = maxWidth;
            layoutedArea = new Rect();
            _createdFirstLine = false;

       //     _clearLayoutDataAll();
            foreach(var key in map.Keys)
            {
                if(_hasFloat(key, KEY_COLUMN_SEQ))
                {
                    _f[map[key]].Remove(KEY_COLUMN_SEQ);
                }
            }
        }

        internal void _progress(Rect nextArea)
        {   
            currentLine = nextArea;
            if(!_createdFirstLine)
            {
                layoutedArea = nextArea;
                _createdFirstLine = true;
            }
            else
            {
                layoutedArea = layoutedArea.Enlarge(nextArea);
                if(maxWidth > 0)
                {
                    Rect r = new Rect(layoutedArea);
                    r.width = Mathf.Min(r.width, maxWidth);
                    layoutedArea = r;
                }
            }
        }

        internal bool _createdFirstLine = false;


        //-----------------------------------------------------------------------------------------------------------------
        
        internal bool _hasFloat(ILayout layout, int key)
        {
            return map.ContainsKey(layout) && _f[map[layout]].ContainsKey(key);
        }
        internal bool _hasRect(ILayout layout, int key)
        {
            return map.ContainsKey(layout) && _r[map[layout]].ContainsKey(key);
        }

        internal void _setValue(ILayout layout, int key, float value)
        {
            if(isLayouting())
            {
                tryAddToMap(layout);
                if(_f[map[layout]].ContainsKey(key))
                {
                    _f[map[layout]][key] = value;
                }
                else
                {
                    _f[map[layout]].Add(key, value);
                }
            }
        }
        internal void _setValue(ILayout layout, int key, Rect value)
        {
            if(isLayouting())
            {
                tryAddToMap(layout);
                if(_r[map[layout]].ContainsKey(key))
                {
                    _r[map[layout]][key] = value;
                }
                else
                {
                    _r[map[layout]].Add(key, value);
                }
            }
        }
        internal float _getFloat(ILayout layout, int key)
        {
            if(map.ContainsKey(layout))
            {
                var c = _f[map[layout]];
                if(c.ContainsKey(key))
                {
                    return c[key];
                } 
            }
            return 0f;
        }
        internal Rect _getRect(ILayout layout, int key)
        {
            if(map.ContainsKey(layout))
            {
                var c = _r[map[layout]];
                if(c.ContainsKey(key))
                {
                    return c[key];
                } 
            }
            return new Rect();
        }

        internal void _clearLayoutData(ILayout layout)
        {
     //       Debug.Log(RichText.emph(RichText.color("CLEAR", Color.red)) + ":: " + Misc.FormatHash(layout));
            if(map.ContainsKey(layout))
            {
                var i = map[layout];
                var cf = _f[i];
                var rf = _r[i];
                cf.Clear();
                rf.Clear();
                _f.RemoveAt(i);
                _r.RemoveAt(i);
                _f.Add(cf);
                _r.Add(rf);         
            }
        }

        internal void _clearLayoutDataAll()
        {
            Debug.Log(RichText.emph(RichText.color("CLEARALL", Color.red)));
            map.Clear();
            for(int i = 0; i < _f.Count; i++)
            {
                _f[i].Clear();
                _r[i].Clear();
            }
            sequencer = 0;
        }


        internal List<float> _getBuffer()
        {
            if(buffer == null) {
                buffer = new List<float>(10);
            }
            else {
                buffer.Clear();    
            }
            return buffer;
        }
        internal List<float> _getBuffer2()
        {
            if(buffer2 == null) {
                buffer2 = new List<float>(10);
            }
            else {
                buffer2.Clear();    
            }
            return buffer2;
        }



        void tryAddToMap(ILayout layout)
        {
            if(!map.ContainsKey(layout))
            {
//                Debug.Log(RichText.color("added layouter", Color.red) + "<" + layout.GetType() + "> " + Misc.FormatHash(this));
                map.Add(layout, sequencer);
                if(sequencer == _f.Count)
                {
                    _f.Add(new Dictionary<int, float>());
                    _r.Add(new Dictionary<int, Rect>());
                }
                sequencer++;
            }
        }

    }
}

