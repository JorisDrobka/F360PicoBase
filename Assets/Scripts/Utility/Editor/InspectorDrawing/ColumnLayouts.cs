using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;


namespace Utility.Inspector
{
    
    public interface ILayout
    {
        int GetHashCode();
    }

    public interface IColumnLayout : ILayout
    {
        int Count { get; }
        bool isValid();
        Rect GetRect(int column, int count, Rect area, Layouter layouter);
        float GetWidth(int column, int count, float area, Layouter layouter);
    }

    //-----------------------------------------------------------------------------------------------------------------

    static class ColumnUtil
    {   
        private static int _columnSequencer = 0;
        public static int GetNextColumnID() { return ++_columnSequencer; }

        public static Rect CalcColumnRect(this IColumnLayout layout, int column, int count, Rect area, Layouter layouter, int spacing=0)
        {
            column = Mathf.Max(0, column);
            spacing = Mathf.Max(0, spacing);

            Rect result;
            float width = layout.GetWidth(column, count, area.width, layouter);
            float x_pos = area.x;
            int seq = 0;
            if(layouter.isLayouting())
            {
                if(column == 0)
                {
                    if(!layouter._hasFloat(layout, Layouter.KEY_COLUMN_SEQ))
                    {
                        layouter._setValue(layout, Layouter.KEY_COLUMN_SEQ, 0);
                    }
                    else
                    {
                        seq = Mathf.RoundToInt(layouter._getFloat(layout, Layouter.KEY_COLUMN_SEQ)) + 1;
                        layouter._setValue(layout, Layouter.KEY_COLUMN_SEQ, seq);
                    }
                    
                    result = new Rect(area.x, area.y, width, area.height);
                    x_pos = area.x + width + spacing;
                }
                else
                {
                    seq = Mathf.RoundToInt(layouter._getFloat(layout, Layouter.KEY_COLUMN_SEQ));
                    x_pos = layouter._getFloat(layout, Layouter.KEY_XPOS + (count * seq));
                    result = new Rect(x_pos, area.y, width, area.height);
                    x_pos += width + spacing;
                }
                if(column < count-1)
                {
                    //  save tmp marching pos
                    layouter._setValue(layout, Layouter.KEY_XPOS + (count * seq), x_pos);
                }

                //  save column as array
                layouter._setValue(layout, Layouter.KEY_X_ARRAY + column + (count * seq), result.x);
            }
            else
            {
                seq = Mathf.RoundToInt(layouter._getFloat(layout, Layouter.KEY_COLUMN_SEQ));
                result = new Rect
                (
                    layouter._getFloat(layout, Layouter.KEY_X_ARRAY + column + (count * seq)),
                    area.y,
                    width, 
                    area.height
                );
            }
            return result;
        }
    }


    //-----------------------------------------------------------------------------------------------------------------


    public struct FixedColumn : IColumnLayout
    {
        const int XPOS = 101;

        int id;
        int[] w;
        public FixedColumn(params int[] widths)
        {
            this.id = ColumnUtil.GetNextColumnID();
            this.w = widths;
        }

        public int Count { get { return w.Length; } }

        public bool isValid()
        {
            return w != null && w.Length > 0;
        }

        public Rect GetRect(int column, int count, Rect area, Layouter layouter)
        {
            return this.CalcColumnRect(column, count, area, layouter);
        }

        public float GetWidth(int column, int count, float area, Layouter layouter)
        {
            if(column >= 0 && column < w.Length)
            {
                return w[column];
            }
            else if(count > 0)
            {
                Debug.LogWarning("wrong column count");
                return ((float)column / (float)count) * area;
            }
            else
            {
                return area;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17 * this.GetType().GetHashCode();
                hash += id;
                return hash;
            }
        }
        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null))
            {
                if(obj is FixedColumn)
                {
                    return Equals((FixedColumn)obj);
                }
            }
            return false;
        }
        public bool Equals(FixedColumn other)
        {
            return other.id == id && other.w == w;
        }
    }


    //-----------------------------------------------------------------------------------------------------------------
    

    public struct WeightedColumn : IColumnLayout
    {
        const int XPOS = 101;

        private int id;
        private float[] ratios;
        private int spacing;
        public WeightedColumn(int spacing, params float[] columns)
        {
            this.id = ColumnUtil.GetNextColumnID();
            this.ratios = columns;
            this.spacing = spacing;
  //          Normalize();
        }

        public int Count { get { return ratios.Length; } }

        public bool isValid()
        {
            return ratios != null && ratios.Length > 0;
        }

        public Rect GetRect(int column, int count, Rect area, Layouter layouter)
        {
            if(layouter.isLayouting())
            {
                if(column == 0)
                {
                    precalc(column, count, area, layouter);
                }
            }
            return this.CalcColumnRect(column, count, area, layouter, spacing);
        }

        public float GetWidth(int column, int count, float area, Layouter layouter)
        {
            if(column >= 0 && column < ratios.Length)
            {
                return layouter._getFloat(this, Layouter.KEY_W_ARRAY + column);
            }
            else if(count > 0)
            {
                Debug.LogWarning("wrong column ratio count");
                return ((float)column / (float)count) * area;
            }
            else
            {
                return area;
            }
        }

        void precalc(int column, int count, Rect area, Layouter layouter)
        {
            for(int i = 0; i < count; i++)
            {
                float w = ratios[i] * area.width;
                if(spacing > 0)
                {
                    if(i == 0 || i == count-1)
                    {
                        w -= spacing/2f;
                    }
                    else
                    {
                        w -= spacing;
                    }
                }
                layouter._setValue(this, Layouter.KEY_W_ARRAY + i, w);
            }
        }

        void Normalize()
        {
            if(isValid())
            {
                float sum = 0f;
                for(int i = 0; i < ratios.Length; i++)
                {
                    ratios[i] = Mathf.Max(0.01f, ratios[i]);
                    sum += ratios[i];
                }
                for(int i = 0; i < ratios.Length; i++)
                {
                    ratios[i] /= sum;
                }
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17 * this.GetType().GetHashCode();
                hash += id;
                return hash;
            }
        }
        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null))
            {
                if(obj is WeightedColumn)
                {
                    return Equals((WeightedColumn)obj);
                }
            }
            return false;
        }
        public bool Equals(WeightedColumn other)
        {
            return other.id == id && other.ratios == ratios;
        }
    }


    //-----------------------------------------------------------------------------------------------------------------



    /// @brief
    /// (NOT TESTED) autoformatted column via layouting instructions.
    /// 
    ///
    public struct SmartColumn : IColumnLayout
    {

        //  layout instructions
        public const string FixedElement = "/fw";           //< Reserve fixed space for an element.
        public const string FlexibleElement = "/w";         //< Reserve flexible space for an element, which will be compressed last.
        public const string FixedRatioElement = "/fr";      //< Reserve space for a scaling element with fixed space.
        public const string FlexibleRatioElement = "/r";    //< Reserve space for a scaling element with flexible space.
        public const string Space = "/s";                   //< Reserve fixed empty space that can be compressed.

        public const string AlignLeft = "/left";            //< Force elements to the left. 
        public const string AlignRight = "/right";          //< Force elements to the right.
        public const string FlexSpace = "/flex";            //< Pushes elements to the sides. May occur multiple times. Overrides other "align" statements.
        public const string WeightedFlexSpace = "/wflex";   //< A flexspace that expects an additional weighting parameter from 0.5 - 2.0
        public const string AlignSpread = "/spread";        //< Distribute elements evenly. Overrides other "align" and "flex" statements


        const int _FixedE = 1;
        const int _FlexE = 2;
        const int _FixedRatioE = 3;
        const int _FlexRatioE = 4;

        const int _Space = 5;
        const int _FlexSpace = 6;
        const int _WeightedFlex = 7;
        const int _AlignLeft = 8;
        const int _AlignRight = 9;
        const int _AlignSpread = 10;
        const int __ALIGN_ERROR = -999;

        int id;
        List<KeyValuePair<int, float>> _input;
        bool inputValid;

        public SmartColumn(params object[] format)
        {
            this.id = ColumnUtil.GetNextColumnID();
            _input = new List<KeyValuePair<int, float>>();
            inputValid = true;

            bool hasElement = false;
            int nextInstruction=-1;
            for(int i = 0; i < format.Length; i++)
            {
                if(format[i] is string)
                {
                    nextInstruction = toInstructionID((format[i] as string));
                    UnityEngine.Assertions.Assert.IsFalse(nextInstruction == -1, "unknown instruction=[" + (format[i] as string) + "]");
                }
                else if(format[i] is float)
                {
                    if(nextInstruction != -1)
                    {
                        _input.Add(new KeyValuePair<int, float>(_FixedRatioE, (float)format[i]));
                    }
                    else
                    {
                        _input.Add(new KeyValuePair<int, float>(nextInstruction, (float)format[i]));
                    }
                    hasElement = true;
                    nextInstruction = -1;
                }
                else if(format[i] is int)
                {
                    if(nextInstruction != -1)
                    {
                        _input.Add(new KeyValuePair<int, float>(_FlexE, (float)format[i]));
                    }
                    else
                    {
                        _input.Add(new KeyValuePair<int, float>(nextInstruction, (float)format[i]));
                    }
                    hasElement = true;
                    nextInstruction = -1;
                }
            }
            inputValid = hasElement;
        }

        public int Count { get { return _input.Count; } }

        public bool isValid()
        {
            return inputValid;
        }

        public Rect GetRect(int column, int count, Rect area, Layouter layouter)
        {
            if(layouter.isLayouting())
            {
                if(column == 0)
                {
                    precalc(column, count, area, layouter);
                }
            }
            return new Rect(
                layouter._getFloat(this, Layouter.KEY_X_ARRAY + column),
                area.y,
                layouter._getFloat(this, Layouter.KEY_W_ARRAY + column),
                area.height
            );
        }

        public float GetWidth(int column, int count, float area, Layouter layouter)
        {
            if(layouter.isLayouted(this))
            {
                return layouter._getFloat(this, Layouter.KEY_W_ARRAY + column);
            }
            else
            {
                return 0f;
            }
        }

        void precalc(int column, int count, Rect area, Layouter layouter)
        {
            var wBuffer = layouter._getBuffer();
            float availableArea = area.width;
            float reserved = 0f;
            float flexible = 0f;

            int alignment = _AlignLeft;
            int numElements = 0;
            int numFlexSpace = 0;

            //  #1 get alignment + reserved & flexible space

            for(int i = 0; i < _input.Count; i++)
            {
                var instruction = _input[i];
                var val = 0f;
                switch(instruction.Key)
                {
                    case _FixedE:
                        availableArea -= instruction.Value;
                        numElements++;
                        break;

                    case _FlexE:
                        val = instruction.Value;
                        reserved += val;
                        numElements++;
                        break;

                    case _FixedRatioE:
                        availableArea -= instruction.Value * area.width;
                        numElements++;
                        break;

                    case _FlexRatioE:
                        val = instruction.Value * area.width;
                        reserved += val;
                        numElements++;
                        break;


                    case _Space:
                        val = instruction.Value;
                        flexible += val;
                        break;
                    
                    case _AlignRight:
                        alignment = alignment == -1 ? _AlignRight : __ALIGN_ERROR;
                        break;

                    case _FlexSpace:
                        alignment = alignment == -1 ? _FlexSpace : __ALIGN_ERROR;
                        numFlexSpace++;
                        break;

                    case _WeightedFlex: 
                        alignment = (alignment == -1 || alignment == _FlexSpace) ? _WeightedFlex : __ALIGN_ERROR;
                        numFlexSpace++;
                        break;
                    
                    case _AlignSpread:
                        alignment = alignment == -1 ? _AlignSpread : __ALIGN_ERROR;
                        break;

                }

                //  fill wBuffer
                if(isBufferTarget(i))
                {
                    wBuffer.Add(_input[i].Value);
                }
            }


            //  #2  handle overshoot / excess space.
            //      after this process, wbuffer and xbuffer are filled with layouted widths & positions

            var xBuffer = layouter._getBuffer2();
            if(reserved > availableArea)
            {

                //  compress & discard rest
                remove(_Space, _FlexSpace, _WeightedFlex, _AlignLeft, _AlignRight, _AlignSpread);
                compress(wBuffer, reserved, availableArea, _FlexE, _FlexRatioE);
                writeColumnPositions(wBuffer, xBuffer);

            }
            else if(reserved + flexible > availableArea)
            {

                //  reduce free space
                var diff = (reserved + flexible) - availableArea;
                if(flexible >= diff) 
                {
                    //  free space is big enough
                    remove(_FlexSpace, _WeightedFlex, _AlignLeft, _AlignRight, _AlignSpread);
                    compress(wBuffer, diff, availableArea, _Space);
                }
                else 
                {
                    //  free space not enough, compress flexible elements
                    remove(_Space, _FlexSpace, _WeightedFlex, _AlignLeft, _AlignRight, _AlignSpread);
                    compress(wBuffer, diff, availableArea, _FlexE, _FlexRatioE);
                }
                writeColumnPositions(wBuffer, xBuffer); 

            }
            else
            {
                //  handle flexible space & alignment
                var dir = alignment;
                var diff = area.width - (reserved + flexible);
                var x = area.x;

                switch(alignment)
                {
                case _AlignRight:
                    x = area.x + area.width;
                    break;

                case _FlexSpace:
                case _WeightedFlex:

                    //  convert flex to normal space
                    calcFlexWeights(numFlexSpace, alignment);
                    for(int i = 0; i < _input.Count; i++)
                    {
                        if(isInstruction(i, _FlexSpace, _WeightedFlex))
                        {
                            wBuffer[i] = _input[i].Value * diff;
                            writeInstruction(i, _Space);    
                        }
                    }
                    break;

                case _AlignSpread:
                    //  calc spread between displayed elements
                    diff /= (float)numElements;     
                    break;
                }


                //  march & fill xbuffer
                for(int i = 0; i < _input.Count; i++)
                {
                    switch(alignment)
                    {
                    case _AlignLeft:
                    case _FlexSpace:
                    case _WeightedFlex:
                        if(isElement(i, includeEmptySpace: true))
                        {
                            xBuffer.Add(x);
                            x += wBuffer[i];
                        }
                        break;

                    case _AlignSpread:
                        if(isElement(i, includeEmptySpace: true))
                        {
                            xBuffer.Add(x);
                            x += wBuffer[i] + diff;
                        }
                        break;

                    case _AlignRight:
                        int ii = _input.Count - 1 - i;
                        if(isElement(ii, includeEmptySpace: true))
                        {
                            x -= wBuffer[ii];
                            xBuffer.Add(x);
                        }
                        break;
                    }
                }
            }

            Debug.Log("Buffers filled== w=[" + wBuffer.Count + "] x=[" + xBuffer.Count + "]  input=[" + _input.Count 
                        + "\n" + PrintStatements() 
                        + "\n\nxBuffer:" + PrintBuffer(xBuffer) 
                        + "\n\nwBuffer:" + PrintBuffer(wBuffer));


            //  #3 apply instructions
            int c = 0;
            for(int i = 0; i < _input.Count; i++)
            {
                if(isElement(i, includeEmptySpace: false))
                {
                    layouter._setValue(this, Layouter.KEY_X_ARRAY + c, xBuffer[i]);
                    layouter._setValue(this, Layouter.KEY_W_ARRAY + c, wBuffer[i]);
                    c++;
                }
            }
        }


        bool isInstruction(int index, params int[] instruction)
        {
            if(index >= 0 && index < _input.Count)
            {
                if(instruction.Length == 1)
                    return instruction[0] == _input[index].Key;
                else
                    return instruction.Contains(_input[index].Key);
            }
            return false;
        }
        bool isElement(int index, bool includeEmptySpace=false)
        {
            if(index >= 0 && index < _input.Count)
            {
                var s = _input[index];
                return s.Key == _FlexE 
                    || s.Key == _FixedE 
                    || s.Key == _FixedRatioE 
                    || s.Key == _FlexRatioE
                    || (includeEmptySpace && s.Key == _Space);
            }
            return false;
        }
        bool isBufferTarget(int index)
        {
            if(index >= 0 && index < _input.Count)
            {
                var s = _input[index];
                return !(s.Key == _AlignLeft || s.Key == _AlignRight);
            }
            return false;
        }

        void calcFlexWeights(int count, int alignment)
        {
            if(count == 0)
            {
                for(int i = 0; i < _input.Count; i++) {
                    if(isInstruction(i, _FlexSpace, _WeightedFlex)) {
                        writeValue(i, 1f);
                        break;
                    }
                }
            }
            else if(alignment == _FlexSpace)
            {
                float weight = 1f / count;
                for(int i = 0; i < _input.Count; i++)
                {
                    if(isInstruction(i, _FlexSpace)) {
                        writeValue(i, weight);
                    }
                }
            }
            else if(alignment == _WeightedFlex) 
            {
                float weight = 1f / count;
                float n = 0f;
                int c = 0;
                for(int i = 0; i < _input.Count; i++)
                {
                    if(isInstruction(i, _FlexSpace))
                    {
                        c++;
                    }
                    else if(isInstruction(i, _WeightedFlex)) 
                    {
                        n += Mathf.Abs(_input[i].Value - 1f);
                    }
                }
                n /= c;
                for(int i = 0; i < _input.Count; i++)
                {
                    if(isInstruction(i, _FlexSpace))
                    {
                        writeValue(i, Mathf.Max(0.05f, weight - n));
                    }
                    else if(isInstruction(i, _WeightedFlex)) 
                    {
                        writeValue(i, _input[i].Value * weight);
                    }
                }
            }
        }

        void writeColumnPositions(List<float> buffer, List<float> target)
        {
            target.Clear();
            for(int i = 0; i < buffer.Count; i++)
            {
                if(_input[i].Key == _FlexE || _input[i].Key == _FixedE)
                {
                    target.Add(buffer[i]);
                }
            }  
        }
        void writeValue(int index, float value)
        {
            _input[index] = new KeyValuePair<int, float>(_input[index].Key, value);
        }
        void writeInstruction(int index, int instruction)
        {
            _input[index] = new KeyValuePair<int, float>(instruction, _input[index].Value);
        }
        void write(int index, int instruction, float value)
        {
            _input[index] = new KeyValuePair<int, float>(instruction, value);
        }


        void compress(List<float> buffer, float sum, float availableArea)
        {
            for(int i = 0; i < buffer.Count; i++)
            {
                buffer[i] /= sum;
                buffer[i] *= availableArea;
            }
        }
        void compress(List<float> buffer, float sum, float availableArea, params int[] instructions)
        {
            for(int i = 0; i < buffer.Count; i++)
            {
                int instruction = _input[i].Key;
                if(instructions.Contains(instruction))
                {
                    buffer[i] /= sum;
                    buffer[i] *= availableArea;
                }               
            }
        }
        void remove(params int[] instructions)
        {
            for(int i = 0; i < _input.Count; i++)
            {
                if(instructions.Contains(_input[i].Key))
                {
                    _input[i] = new KeyValuePair<int, float>(_input[i].Key, -1);
                }               
            }
        }


        int toInstructionID(string txt)
        {
            switch(txt)
            {
                case FixedElement:          return _FixedE;
                case FlexibleElement:       return _FlexE;
                case FixedRatioElement:     return _FixedRatioE;
                case FlexibleRatioElement:  return _FlexRatioE;
                case Space:                 return _Space;
                case FlexSpace:             return _FlexSpace;
                case WeightedFlexSpace:     return _WeightedFlex;
                case AlignLeft:             return _AlignLeft;
                case AlignRight:            return _AlignRight;
                case AlignSpread:           return _AlignSpread;
                default:                    return -1;
            }
        }
        string toInstructionTxt(int id)
        {
            switch(id)
            {
                case _FixedE:           return FixedElement;
                case _FlexE:            return FlexibleElement;
                case _FixedRatioE:      return FixedRatioElement;
                case _FlexRatioE:       return FlexibleRatioElement;
                case _Space:            return Space;
                case _FlexSpace:        return FlexSpace;
                case _WeightedFlex:     return WeightedFlexSpace;
                case _AlignLeft:        return AlignLeft;
                case _AlignRight:       return AlignRight;
                case _AlignSpread:      return AlignSpread;
                default:                return "UNKNOWN";
            }
        }


        public string PrintStatements()
        {
            var b = new System.Text.StringBuilder("SmartColumn->");
            for(int i = 0; i < _input.Count; i++)
            {
                b.Append("\n\t");
                b.Append(i.ToString());
                b.Append(": " + toInstructionTxt(_input[i].Key));
                b.Append(" [" + Misc.Float2String(_input[i].Value, 2) + "]");
            }
            return b.ToString();
        }
        string PrintBuffer(List<float> buffer)
        {
            var b = new System.Text.StringBuilder("\tBuffer->");
            for(int i = 0; i < buffer.Count; i++)
            {
                b.Append("\n\t" + Misc.Float2String(buffer[i], 2));
            }
            return b.ToString();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17 * this.GetType().GetHashCode();
                hash += id;
         //       for(int i = 0; i < _input.Count; i++)
         //       {
         //           hash += 13 * _input[i].GetHashCode();
         //       }
                return hash;
            }
        }
        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null))
            {
                if(obj is SmartColumn)
                {
                    return Equals((SmartColumn)obj);
                }
            }
            return false;
        }
        public bool Equals(SmartColumn other)
        {
            return other.id == id && other._input == _input;
        }
    }


}
