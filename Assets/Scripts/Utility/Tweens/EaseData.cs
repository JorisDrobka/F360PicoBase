using System;
using UnityEngine;

namespace Utility.UI.Tweens
{

    public delegate void DataUpdateCallback<TData>(EaseData<TData> data);

    
    public interface ITweenBeginListener
    {
        void onBeginTween(TweenManager.Tween tween);
    }
    public interface ITweenUpdateListener
    {
        void onUpdateTween(TweenManager.Tween tween);
    }
    public interface ITweenEndListener
    {
        void onEndTween(TweenManager.Tween tween);
    }
    
    
    //------------------------------------------------------------------------------------------------------------------

    public interface ITweenData {}

    [Serializable]
    public struct TweenData : ITweenData
    {
        public EaseType type;
        public float duration;
        public float paramA;
        public float paramB;

        public TweenData(EaseType type, float duration, float paramA=1f, float paramB=1f)
        {
            this.type = type;
            this.duration = duration;
            this.paramA = paramA;
            this.paramB = paramB;
        }

        public TweenData Clone()
        {
            return (TweenData) this.MemberwiseClone();
        }
    }

    [Serializable]
    public struct CurveData : ITweenData
    {
        public AnimationCurve curve;
        public float duration;

        public CurveData(AnimationCurve curve, float duration)
        {
            this.curve = curve;
            this.duration = duration;
        }
    }
    
    
    //==================================================================================================================

    
    public abstract class EaseData
    {
        public enum DataType
        {
            None,
            Float,
            Vec2,
            Vec3,
            Quat
        }

        public abstract DataType dataType { get; }
        
        public abstract void flip();
        public abstract void setDirection(bool b);

        public bool isFlipped { get; protected set;  }

        public int index = 0;

        public abstract void setNormalized(float t, EaseType type, float paramA, float paramB);
        public abstract object getCurrValue();
        public abstract object getStartValue();
        public abstract object getTargetValue();
        public abstract void setCurrValue(object value);

        public abstract float currT { get; }
        
       

        protected static DataType getDataType(Type t)
        {
            if (t == typeof(float))
            {
                return DataType.Float;
            }
            else if (t == typeof(Vector2))
            {
                return DataType.Vec2;
            }
            else if(t == typeof(Vector3))
            {
                return DataType.Vec3;
            }
            else if(t == typeof(Quaternion))
            {
                return DataType.Quat;
            }
            else
            {
                return DataType.None;
            }
        }


        public static Boolean supportsDataType<TData>()
        {
            return getDataType(typeof(TData)) != DataType.None;
        }
    }

    
    //==================================================================================================================
    

    public class EaseData<TData> : EaseData
    {
        private TData start;
        private TData target;
        private TData data;
        private DataUpdateCallback<TData> callback;

        public override DataType dataType
        {
            get { return _dataType; }
        }
        private readonly DataType _dataType;

        public override float currT
        {
            get { return _currT; }
        }
        private float _currT;

        public TData currValue
        {
            get { return data; }
        }

        public TData startValue
        {
            get { return start; }
        }

        public TData targetValue
        {
            get { return target; }
        }

        public override object getCurrValue()
        {
            return data;
        }

        public override object getStartValue()
        {
            return start;
        }

        public override object getTargetValue()
        {
            return target;
        }


        public void Set(TData start, TData target)
        {
            setDirection(true);
            this.start = start;
            this.target = target;
        }

        public override void setCurrValue(object value)
        {
            if (testValue(value))
            {
                this.data = (TData) value;
                
                
                //    calc currT
            }
        }

        public void setCurrValue(TData value)
        {
            this.data = value;
        }

        public void setStartValue(TData value)
        {
            if (isFlipped)
            {
                this.target = value;
            }
            else
            {
                this.start = value;
            }
        }

        public void setTargetValue(TData value)
        {
            if (isFlipped)
            {
                this.start = value;
            }
            else
            {
                this.target = value;
            }
        }

        //    constructor

        public EaseData(TData data, TData start, TData target, DataUpdateCallback<TData> callback=null, float startTVal = 0f, int index=0)
        {
            this._dataType = getMyDataType();
            this.start = start;
            this.target = target;
            this.data = data;
            this.callback = callback;
            this._currT = Mathf.Clamp01(startTVal);
            this.index = index;
        }
        
        
        public override void flip()
        {
            TData tmp = start;
            start = target;
            target = tmp;
            isFlipped = !isFlipped;
        }

        public override void setDirection(bool forward)
        {
            if (forward != !isFlipped)
            {
                flip();
            }
        }


        public override void setNormalized(float t, EaseType type, float paramA, float paramB)
        {
            this._currT = Mathf.Clamp01(t);
                        
            object obj = null;
            switch (dataType)
            {
                case DataType.Float:

                    obj = (object) Easing.Ease<float>(type, t, 1, start, target, paramA, paramB);
                    break;
                case DataType.Vec2:

                    obj = (object) Easing.Ease<Vector2>(type, t, 1, start, target, paramA, paramB);
                    break;
                case DataType.Vec3:

                    obj = (object) Easing.Ease<Vector3>(type, t, 1, start, target, paramA, paramB);
                    break;
                case DataType.Quat:

                    obj = (object) Easing.Ease<Quaternion>(type, t, 1, start, target, paramA, paramB);
                    break;
            }

            if (obj != null)
            {
                data = (TData) obj;
                if (callback != null)
                {
                    callback(this);
                }
            }
        }


        protected bool testValue(object obj)
        {
            return obj != null && getDataType(obj.GetType()) == dataType;
        }

        protected DataType getMyDataType()
        {
            return getDataType(typeof(TData));
        }


        public override string ToString()
        {
            return "EaseData<" + dataType.ToString() + ">";
        }
    }
}