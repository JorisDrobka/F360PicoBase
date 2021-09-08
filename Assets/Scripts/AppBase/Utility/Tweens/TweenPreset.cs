using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Utility.UI.Tweens
{
    [CreateAssetMenu(fileName = "new TweenPreset", menuName = "GUI/NEW TweenPreset")]
    public class TweenPreset : ScriptableObject
    {
        public bool _useCurve_IN;
        public bool _useCurve_OUT;
        public bool _useOutState; 

        public TweenData _tween_IN;
        public CurveData _curve_IN;
        public TweenData _tween_OUT;
        public CurveData _curve_OUT;

        #if UNITY_EDITOR
        public bool _foldout_IN;
        public bool _foldout_OUT;
        #endif

        public bool hasOutState()
        {
            return _useOutState;
        }
        
        public ITweenData Get(bool direction)
        {
            if(direction || !_useOutState)
            {
                if(_useCurve_IN)
                {
                    return _curve_IN;
                }
                else
                {
                    return _tween_IN;
                }
            }
            else
            {
                if(_useCurve_OUT)
                {
                    return _curve_OUT;
                }
                else
                {
                    return _curve_IN;
                }
            }
        }
    }

}

