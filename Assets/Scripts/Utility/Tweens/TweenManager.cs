using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = System.Object;


namespace Utility.UI.Tweens
{
    
    

    public class TweenManager : MonoBehaviour
    {

        //  interface

        private static TweenManager instance;

        public static void ResetSystem()
        {
            isDestroyed = false;
        }

        public static TweenManager Get()
        {
            if (isDestroyed)
            {
                return null;
            }
            else if (instance == null)
            {
                instance = new GameObject("TweenManager").AddComponent<TweenManager>();
            }
            return instance;
        }

        public static Tween create(EaseData data, ITweenData setup, object listener=null)
        {
            if (!isDestroyed)
            {
                return Get()._create(data, setup, listener);
            }
            return null;
        }

        public static Tween createAndRun(EaseData data, ITweenData setup, object listener=null)
        {
            if (!isDestroyed)
            {
                return Get()._createAndRun(data, setup, listener);
            }
            return null;
        }

        public static AutoTween<TData> createAuto2<TData, TObject>(TData startVal, TData targetVal, ITweenData setup, TObject targetObj, Channel channel, object listener=null) where TObject : UnityEngine.Object
        {
            if (!isDestroyed)
            {
                if (checkAutoTweenTypes<TData>(targetObj, channel))
                {
                    EaseData<TData> ease = new EaseData<TData>(startVal, startVal, targetVal);
                    return Get()._createAuto<TData, TObject>(ease, setup, targetObj, channel, listener);
                }
            }
            return null;
        }
        
        public static AutoTween<TData> createAutoAndRun2<TData, TObject>(TData startVal, TData targetVal, ITweenData setup, TObject targetObj, Channel channel, object listener=null) where TObject : UnityEngine.Object
        {
            if (!isDestroyed)
            {
                if (checkAutoTweenTypes<TData>(targetObj, channel))
                {
                    EaseData<TData> ease = new EaseData<TData>(startVal, startVal, targetVal);
                    return Get()._createAutoAndRun<TData, TObject>(ease, setup, targetObj, channel, listener);
                }
            }
            return null;
        }

        public static AutoTween<TData> createAuto<TData, TObject>(EaseData<TData> data, ITweenData setup, TObject target, Channel channel, object listener=null) where TObject : UnityEngine.Object
        {
            if (!isDestroyed)
            {
                if (checkAutoTweenTypes<TData>(target, channel))
                {
                    return Get()._createAuto<TData, TObject>(data, setup, target, channel, listener);
                }
            }
            return null;
        }

        public static AutoTween<TData> createAutoAndRun<TData, TObject>(EaseData<TData> data, ITweenData setup, TObject target, Channel channel, object listener=null) where TObject : UnityEngine.Object
        {
            if (!isDestroyed)
            {
                if (checkAutoTweenTypes<TData>(target, channel))
                {
                    return Get()._createAuto<TData, TObject>(data, setup, target, channel, listener);
                }
            }
            return null;
        }
        

        public static bool GetPooledCustomTween<TCustom>(out TCustom tween) where TCustom : Tween
        {
            tween = null;
            if(!isDestroyed)
            {
                tween = Get()._getCustomTween<TCustom>();
            }
            return tween != null;
        }



        //-----------------------------------------------------------------------------------------------------------------

        //  internal interface (called by tweens)
       

        private static bool checkAutoTweenTypes<TData>(Object target, Channel channel)
        {
            if (AutoTween<TData>.validate(target, channel))
            {
                return true;
            }
            else
            {
                throw new System.Exception("cannot create tween with target type=[" + typeof(TData) + "] and target of type=[" + target.GetType() + "] !!!");
            }
        }


        private static Coroutine startCoroutine(IEnumerator routine)
        {
            if (!isDestroyed && routine != null)
            {
                return Get().StartCoroutine(routine);
            }

            return null;
        }

        private static void stopCoroutine(Coroutine routine)
        {
            if (!isDestroyed && routine != null)
            {
                Get().StopCoroutine(routine);
            }
        }

        private static void addToRunningTweens(Tween t)
        {
            if (!isDestroyed)
            {
                var manager = Get();
                if (!manager.runningTweens.Contains(t))
                {
                    manager.runningTweens.Add(t);
                }
            }
        }

        private static void endTween(Tween t, bool pool)
        {
            if (!isDestroyed)
            {
                var manager = Get();
                manager.runningTweens.Remove(t);
                if (pool)
                {
                    if(t.isCustomTween)
                    {
                        manager.customPool.Add(t);
                    }
                    else
                    {
                        manager.pool.Add(t);
                    }
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------


        private List<Tween> runningTweens = new List<Tween>();
        private List<Tween> pool = new List<Tween>();
        private List<Tween> customPool = new List<Tween>();
        private static bool isDestroyed = false;
        

        private void Awake()
        {
            instance = this;
            isDestroyed = false;

            //Debug.Log(RichText.color("CREATE TWEEN POOL", Color.red));
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            pool.Clear();
            runningTweens.Clear();
            isDestroyed = true;
            instance = null;

            //Debug.Log(RichText.color("DESTROY TWEEN POOL", Color.red));
        }
        
        

        private Tween _create(EaseData data, ITweenData setup, object listener=null)
        {
            Tween tween = null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!(pool[i] is AutoTween))
                {
                    tween = pool[i];
                    tween.set(data, setup);
                    pool.RemoveAt(i);
                    break;
                }
            }
            
            if (tween == null)
            {
                tween = new Tween(data, setup);
                tween._flag_created_by_system = true;
            }
            tween.setListener(listener);
            return tween;
        }

        private Tween _createAndRun(EaseData data, ITweenData setup, object listener=null)
        {
            Tween tween = _create(data, setup, listener);
            tween.run();
            return tween;
        }

        private AutoTween<TData> _createAuto<TData, TObject>(EaseData<TData> data, ITweenData setup, TObject target, Channel channel, object listener) where TObject : UnityEngine.Object
        {
            AutoTween<TData> tween = null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] is AutoTween<TData>)
                {
                    tween = pool[i] as AutoTween<TData>;
                    tween.set(data, setup, target, channel);
                    pool.RemoveAt(i);
                    break;
                }
            }
            
            if (tween == null)
            {
                tween = new AutoTween<TData>(data, setup, target, channel);
                tween._flag_created_by_system = true;
            }
            tween.setListener(listener);
            return tween;
        }

        private AutoTween<TData> _createAutoAndRun<TData, TObject>(EaseData<TData> data, ITweenData setup, TObject target, Channel channel, object listener) where TObject : UnityEngine.Object
        {
            AutoTween<TData> tween = _createAuto<TData, TObject>(data, setup, target, channel, listener);
            tween.run();
            return tween;
        }


        private TCustom _getCustomTween<TCustom>() where TCustom : Tween
        {
            for(int i = 0; i < customPool.Count; i++)
            {
                if(!customPool[i].isRunning)
                {
                    var t = customPool[i] as TCustom;
                    if(t != null)
                    {
                        customPool.RemoveAt(i);
                        return t;
                    }
                }
            }
            return null;
        }


        //==================================================================================================================

        
        public enum Channel
        {
            alpha,
            color,
            localposition,
            worldposition,
            localscale,
            worldrotation,
            localrotation
        }
        

        public abstract class AutoTween : Tween
        {
            protected Channel mChannel;

            public AutoTween(EaseData data, ITweenData setup, Channel channel) : base(data, setup)
            {
                mChannel = channel;
            }
        }

        
        public sealed class AutoTween<TData> : AutoTween
        {
            private Object mTarget;
            private Handler mHandler;
            
            public AutoTween(EaseData<TData> data, ITweenData setup, Object target, Channel channel) : base(data, setup, channel)
            {
                mTarget = target;
                mHandler = createHandler(target, channel);
            }

            public void set(EaseData<TData> data, ITweenData setup, Object target, Channel channel)
            {
                base.set(data, setup);
                mTarget = target;
                mChannel = channel;
                mHandler = createHandler(target, channel);
            }

            protected override void OnUpdateTween()
            {
                base.OnUpdateTween();                
                mHandler.setValue(data, mChannel);
            }

            public static Handler createHandler(Object target, Channel c)
            {
                if (target is CanvasGroup)
                {
                    if (typeof(TData) == typeof(float))
                    {
                        return new CanvasGroupHandler(target as CanvasGroup);
                    }
                }
                else if (target is Image)
                {
                    if (typeof(TData) == typeof(float))
                    {
                        return new ImageOpacityHandler(target as Image);
                    }
                    else if (typeof(TData) == typeof(Color))
                    {
                        return new ImageColorHandler(target as Image);
                    }
                }
                else if(target is Transform)
                {
                    if (typeof(TData) == typeof(Vector3) || typeof(TData) == typeof(Quaternion))
                    {
                        return new TransformHandler(target as Transform);
                    }
                }
                return null;
            }

            public static bool validate(Object target, Channel channel)
            {
                if (target is CanvasGroup)
                {
                    return channel == Channel.alpha;
                }
                else if (target is Image)
                {
                    return channel == Channel.alpha || channel == Channel.color;
                }
                else if (target is Transform)
                {
                    switch (channel)
                    {
                        case Channel.worldrotation:
                        case Channel.localrotation:
                        case Channel.localscale:
                        case Channel.localposition:
                        case Channel.worldposition:
                            return true;
                        
                        default:
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            public abstract class Handler
            {
                public abstract void setValue(EaseData data, Channel c);
            }

            public abstract class Handler<TObject, T> : Handler where TObject : UnityEngine.Object
            {
                public override void setValue(EaseData data, Channel c)
                {
                    EaseData<T> d = data as EaseData<T>;
                    if (d != null)
                    {
                        this.setValue(d, c);
                    }
                }

                public abstract void setValue(EaseData<T> data, Channel c);
            }

            public class ImageOpacityHandler : Handler<Image, float>
            {
                private Image target;
                public ImageOpacityHandler(Image img)
                {
                    this.target = img;
                }

                public override void setValue(EaseData<float> data, Channel c)
                {
                    target.SetAlpha(data.currValue);
                }
            }

            public class ImageColorHandler : Handler<Image, Color>
            {
                private Image target;
                public ImageColorHandler(Image img)
                {
                    this.target = img;
                }

                public override void setValue(EaseData<Color> data, Channel c)
                {
                    if (c == Channel.alpha)
                    {
                        target.SetAlpha(data.currValue.a);
                    }
                    else if (c == Channel.color)
                    {
                        target.color = data.currValue;
                    }                    
                }
            }

            public class CanvasGroupHandler : Handler<CanvasGroup, float>
            {
                private CanvasGroup target;
                public CanvasGroupHandler(CanvasGroup g)
                {
                    this.target = g;
                }

                public override void setValue(EaseData<float> data, Channel c)
                {
                    target.alpha = data.currValue;
                }
            }

            public class TransformHandler : Handler<Transform, Vector3>
            {
                private Transform target;
                private RectTransform rTarget;
                public TransformHandler(Transform t)
                {
                    this.target = t;
                    this.rTarget = t as RectTransform;
                }

                public override void setValue(EaseData<Vector3> data, Channel c)
                {
                    switch (c)
                    {
                        case Channel.localscale:
                            target.localScale = data.currValue;
                            break;
                        
                        case Channel.worldposition:
                            target.position = data.currValue;
                            break;
                        
                        case Channel.localposition:

                            if (rTarget != null)
                            {
                                rTarget.anchoredPosition3D = data.currValue;
                            }
                            else
                            {
                                target.localPosition = data.currValue;
                            }
                            break;
                    }
                }
            }

            public class TransformRotationHandler : Handler<RectTransform, Quaternion>
            {
                private Transform target;
                public TransformRotationHandler(Transform t)
                {
                    this.target = t;
                }

                public override void setValue(EaseData<Quaternion> data, Channel c)
                {
                    target.rotation = data.currValue;
                }
            }
        }
        
        
        

        //==================================================================================================================        


        public class Tween
        {
            public EaseData data;
            private TweenData setup;
            private CurveData curveSetup;
            private Coroutine routine;
            private float currT;
            private bool reuse;

            private ITweenBeginListener beginListener;
            private ITweenUpdateListener updateListener;
            private ITweenEndListener endListener;

            public bool isRunning { get; private set; }

            public bool isCustomTween { get { return !_flag_created_by_system; } }

            public bool _flag_created_by_system = false;

            public Tween(EaseData data, ITweenData setup)
            {
                set(data, setup);
            }

            public bool isMyTween(EaseData e)
            {
                return e == data;
            }

            public void set(EaseData data, ITweenData setup)
            {
                this.data = data;
                if (setup is CurveData)
                {
                    this.curveSetup = (CurveData) setup;
                    this.setup = new TweenData();
                }
                else
                {
                    this.setup = (TweenData) setup;
                    this.curveSetup = new CurveData();
                }
            }


            public void setListener(object listener)
            {
                if (listener != null)
                {
                    beginListener = listener as ITweenBeginListener;
                    updateListener = listener as ITweenUpdateListener;
                    endListener = listener as ITweenEndListener;
                }
                else
                {
                    beginListener = null;
                    updateListener = null;
                    endListener = null;
                }
            }

            public void setReusable(bool b)
            {
                reuse = b;
            }


            public void run()
            {
                if (data != null)
                {
                    TweenManager.stopCoroutine(routine);
                    if (!isRunning)
                    {
                        TweenManager.addToRunningTweens(this);
                    }

                    routine = TweenManager.startCoroutine(performTween());
                    OnBeginTween();
                }
            }

            public void reverse()
            {
                if (data != null)
                {
                    data.flip();
                    TweenManager.stopCoroutine(routine);
                    if (isRunning)
                    {
                        routine = TweenManager.startCoroutine(performTween(true));
                        OnBeginTween();
                    }
                }
            }

            public void moveInDirection(bool forward)
            {
                if (data != null)
                {
                    _moveInDirection(forward);
                }
            }
            public void moveInDirection(bool forward, ITweenData setup)
            {
                if (data != null)
                {
                    if (setup is TweenData)
                    {
                        this.setup = (TweenData) setup;
                        this.curveSetup = new CurveData();
                    }
                    else if (setup is CurveData)
                    {
                        this.curveSetup = (CurveData)setup;
                        this.setup = new TweenData();
                    }
                    _moveInDirection(forward);
                }
            }


            protected virtual void OnBeginTween()
            {
                if (beginListener != null)
                {
                    beginListener.onBeginTween(this);
                }
            }

            protected virtual void OnUpdateTween()
            {
                if (updateListener != null)
                {
                    updateListener.onUpdateTween(this);
                }
            }

            protected virtual void OnEndTween()
            {
                if (endListener != null)
                {
                    endListener.onEndTween(this);
                }
            }



            private void _moveInDirection(bool forward)
            {
                data.setDirection(forward);
                TweenManager.stopCoroutine(routine);
                if (isRunning)
                {
                    routine = TweenManager.startCoroutine(performTween(true));
                    OnBeginTween();
                }
                else
                {
                    run();
                }
            }

            public void stop(bool cleanup=true)
            {
   //             Debug.Log( RichText.color("stop tween(", Color.red) +  Misc.FormatHash(this) + ")  isRunning=[" + isRunning + "] cleanup=[" + cleanup + "]");
                
                TweenManager.stopCoroutine(routine);
   
                if (isRunning)
                {
                    isRunning = false;
                    OnEndTween();

                    if (cleanup)
                    {
                        TweenManager.endTween(this, !reuse);
                    }

                    data = null;
                }
            }


            private IEnumerator performTween(bool reverse=false)
            {
    //            Debug.Log( RichText.color("start tween (", Color.green) + Misc.FormatHash(this) + ")");
                
                isRunning = true;

                bool useCurve = curveSetup.curve != null && curveSetup.curve.keys.Length >= 2 && curveSetup.duration > 0;
                float startT = Time.time;
                float duration = useCurve ? curveSetup.duration : setup.duration;

                if (reverse)
                {
                    currT = Mathf.Clamp01(currT);
                    if (currT <= 0.01f)
                    {
                        setValue(currT = 1);
                        stop(true);
                    }
                    else
                    {
                        float timeOff = duration - (currT * duration);
                        currT = 1 - currT;
                        startT -= timeOff;
                        duration -= timeOff;
                    }
                }
                else
                {
                    setValue(currT = 0);
                }

                do
                {
                    currT = Easing.EaseFloat(EaseType.Linear, Time.time - startT, duration, 0, 1);
                    if (useCurve)
                    {
                        currT = curveSetup.curve.Evaluate(currT);
                    }

   //               if(this is AutoTween<float>)
//                        Debug.Log("tween.. " + currT + " " + duration + " " + startT + " " + Time.time);
                    setValue(currT);
                    yield return null;
                } 
                while (Time.time < startT + duration);

                setValue(currT = 1);
                stop(true);
            }

            
            
            private void setValue(float t)
            {
                if (data != null)
                {
                    data.setNormalized(Mathf.Clamp01(currT), setup.type, setup.paramA, setup.paramB);
                    OnUpdateTween();
                }
            }


            
            

            public override string ToString()
            {
                if (data != null)
                {
                    return "Tween<" + data.dataType.ToString() + ">";
                }
                else
                {
                    return "Tween<empty>";
                }
            }
        }


    }
}