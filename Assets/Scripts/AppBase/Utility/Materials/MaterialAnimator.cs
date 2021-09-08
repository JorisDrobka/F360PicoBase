using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


using Utility.Pooling;
using Utility.UI.Tweens;

namespace Utility.Materials
{

    public enum ShaderProperty
    {
        FLOAT,
        VEC2,
        VEC3,
        VEC4,
        COLOR,
        TEX
    }

    public enum Handling
        {
            KeepCurrent,
            PoolAndCleanup,
            PoolAndKeepClone
        }   


    //=================================================================================================================


    public static class MaterialAnimator
    {

        //  Custom Tween

        public class MaterialTween : TweenManager.Tween
        {
            public MaterialAnimatorPreset preset;
            public AnimatedMaterial[] targets;
            public Handling handling;

            public MaterialTween(AnimatedMaterial mat, MaterialAnimatorPreset preset, Handling handling) : base(new EaseData<float>(0,0,1), preset.tween) 
            {
                this.targets = new AnimatedMaterial[] { mat };
                this.preset = preset;
                this.handling = handling;
            }
            public MaterialTween(IEnumerable<AnimatedMaterial> mats, MaterialAnimatorPreset preset, Handling handling) : base(new EaseData<float>(0,0,1), preset.tween)
            {
                this.preset = preset;
                this.handling = handling;
                setMultipleTargets(mats);
            }

            public void set(AnimatedMaterial mat, MaterialAnimatorPreset preset, Handling handling)
            {
                this.targets = new AnimatedMaterial[] { mat };
                this.preset = preset;
                this.handling = handling;
                base.set(this.data, preset.tween);
            }
            public void set(IEnumerable<AnimatedMaterial> mats, MaterialAnimatorPreset preset, Handling handling)
            {
                this.preset = preset;
                this.handling = handling;
                base.set(this.data, preset.tween);
                setMultipleTargets(mats);
            }

            private void setMultipleTargets(IEnumerable<AnimatedMaterial> mats)
            {
                mats = mats.Where(x=> x.isValid());
                this.targets = new AnimatedMaterial[mats.Count()];
                int c = 0;
                foreach(var mat in mats)
                {
                    this.targets[c] = mat;
                    c++;
                }
            }

            protected override void OnUpdateTween()
            {
                for(int i = 0; i < targets.Length; i++)
                {
                    setProperty(targets[i].currentMaterial, preset, data.currT);
                }
                base.OnUpdateTween();
            }

            protected override void OnEndTween()
            {
                if(handling == Handling.PoolAndCleanup)
                {
                    //  revert material after animation
                    foreach(var mat in targets)
                    {
                        mat.RevertMaterial();
                    }
                }
            }
        }


    	//=================================================================================================================

        //  INTERFACE        

        
        public static MaterialTween Play(Renderer renderer, Material material, MaterialAnimatorPreset state, Handling handling=Handling.PoolAndCleanup, object listener=null)
        {
            return prepareAndPlay(AnimatedMaterial.Get(renderer, material), state, handling, listener);
        }

        public static IEnumerable<MaterialTween> Play(MaterialGroup group, MaterialAnimatorPreset state, Handling handling=Handling.PoolAndCleanup, object listener=null)
        {
            foreach(var tween in prepareAndPlay(group, state, handling, listener))
            {
                yield return tween;
            }
        }



        //-----------------------------------------------------------------------------------------------------------------


        private static MaterialTween prepareAndPlay(AnimatedMaterial m, MaterialAnimatorPreset preset, Handling handling, object listener)
        {
            if(m.isValid())
            {
                Material mat = m.currentMaterial;
                if(mat.HasProperty(preset.propertyName))
                {
                    if(handling != Handling.KeepCurrent)
                    {
                        m.CloneMaterial();
                    }

                    MaterialTween tween;
                    if(TweenManager.GetPooledCustomTween<MaterialTween>(out tween))
                    {
                        tween.set(m, preset, handling);
                    }
                    else
                    {
                        tween = new MaterialTween(m, preset, handling);
                    }
                    tween.setListener(listener);
                    tween.run();
                    return tween;
                }
            }
            return null;
        }
        private static IEnumerable<MaterialTween> prepareAndPlay(MaterialGroup group, MaterialAnimatorPreset preset, Handling handling, object listener)
        {
            foreach(var shader in group.GetAllShaders())
            {
                if(group.CheckForProperty(shader, preset.propertyName))
                {
                    var mats = group.GetByShader(shader);
                
                    if(handling != Handling.KeepCurrent)
                    {
                        foreach(var m in mats)
                        {
                            m.CloneMaterial();
                        }
                    }

                    MaterialTween tween;
                    if(TweenManager.GetPooledCustomTween<MaterialTween>(out tween))
                    {
                        tween.set(mats, preset, handling);
                    }
                    else
                    {
                        tween = new MaterialTween(mats, preset, handling);
                    }
                    tween.setListener(listener);
                    tween.run();

                    yield return tween;
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        
        public static void setProperty(Material target, MaterialAnimatorPreset preset, float t)
        {
            if(target.HasProperty(preset.propertyName))
            {
                switch(preset.type)
                {
                    case ShaderProperty.FLOAT:
                        target.SetFloat(preset.propertyName, Easing.EaseFloat(EaseType.Linear, t, 1, preset.float_start, preset.float_target));
                        break;
                    case ShaderProperty.VEC2:
                        var vec2 = Easing.EaseVector2(EaseType.Linear, t, 1, preset.vec2_start, preset.vec2_target);
                        target.SetVector(preset.propertyName, new Vector4(vec2.x, vec2.y, 1, 1));
                        break;
                    case ShaderProperty.VEC3:
                        var vec3 = Easing.EaseVector3(EaseType.Linear, t, 1, preset.vec3_start, preset.vec3_target);
                        target.SetVector(preset.propertyName, new Vector4(vec3.x, vec3.y, vec3.z, 1));
                        break;
                    case ShaderProperty.VEC4:
                        var vec4 = Easing.EaseVector4(EaseType.Linear, t, 1, preset.vec3_start, preset.vec3_target);
                        target.SetVector(preset.propertyName, new Vector4(vec4.x, vec4.y, vec4.z, vec4.w));
                        break;
                    case ShaderProperty.COLOR:
                        target.SetColor(preset.propertyName, Color.Lerp(preset.color_start, preset.color_target, t));
                        break;
                    case ShaderProperty.TEX:
                        if(t >= 0.99f)
                        {
                            //  texture flip
                            target.SetTexture(preset.propertyName, preset.texture);
                        }
                        break;
                }
            }
        }

    }


}
