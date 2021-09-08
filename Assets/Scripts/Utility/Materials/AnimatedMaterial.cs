using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


using Utility.Pooling;
using Utility.UI.Tweens;

namespace Utility.Materials
{

    public class AnimatedMaterial
    {
        public Renderer renderer;
        public int matID;
        public Material currentMaterial
        {
            get { return isValid() ? renderer.materials[matID] : null; }
        }
        public Material srcMaterial;

        public int cloneAttempts { get; private set; }  //  how many entities attempted to clone this material


        private AnimatedMaterial(Renderer r, int index)
        {
            renderer = r;
            matID = index >= 0 && index < renderer.materials.Length ? index : -1;
            srcMaterial = currentMaterial;
        }

        public bool isValid()
        {
            return renderer != null && matID != -1;
        }

        public bool CheckForProperty(string property)
        {
            if(isValid() && !string.IsNullOrEmpty(property))
            {
                return currentMaterial.HasProperty(property);
            }
            return false;
        }
        public bool CheckForProperty(int propertyID)
        {
            if(isValid())
            {
                return currentMaterial.HasProperty(propertyID);
            }
            return false;
        }

        public bool hasClonedMaterial() 
        {
                return isValid() && srcMaterial != currentMaterial;
        }

        public void CloneMaterial()
        {
            if(isValid() && srcMaterial != null)
            {
                if(srcMaterial != currentMaterial)
                {
                    renderer.materials[matID] = MaterialPool.instance.Request(srcMaterial);
                }
                cloneAttempts++;
            }
        }

        public void RevertMaterial()
        {
            if(isValid() && srcMaterial != null)
            {
                cloneAttempts = Mathf.Max(cloneAttempts-1, 0);
                if(cloneAttempts == 0)
                {
                    var clone = renderer.materials[matID];
                    renderer.materials[matID] = srcMaterial;
                    MaterialPool.instance.Free(clone);
                }
            }
        }

        //  static access
        public static AnimatedMaterial Get(Renderer r, Material m)
        {
                return Get(r, System.Array.IndexOf(r.materials, m));
        }

        public static AnimatedMaterial Get(Renderer r, int matID)
        {
            var mat = pool.Find(x=> x.renderer==r && x.matID==matID);
            if(mat != null)
            {
                return mat;
            }
            else
            {
                mat = new AnimatedMaterial(r, matID);
                pool.Add(mat);
                return mat;
            }
        }

        private static List<AnimatedMaterial> pool = new List<AnimatedMaterial>();


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 11 * matID;
                if(renderer != null)
                    hash += 17 ^ renderer.GetHashCode();
                if(srcMaterial != null)
                    hash += 13 ^ srcMaterial.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(obj, null))
            {
                return false;
            }
            else if(obj is AnimatedMaterial)
            {
                return this.Equals((AnimatedMaterial)obj);
            }
            else return false;
        }

        public bool Equals(AnimatedMaterial other)
        {
            if(ReferenceEquals(other, null))
            {
                return false;
            }
            else
            {
                return other.renderer == renderer && other.matID == matID;
            }
        }


        public static bool operator==(AnimatedMaterial a, AnimatedMaterial b)
        {
            bool aIsNull = ReferenceEquals(a, null);
            bool bIsNull = ReferenceEquals(b, null);
            if(aIsNull && bIsNull)
            {
                return true;
            }
            else if(aIsNull != bIsNull)
            {
                return false;
            }
            else
            {
                return a.Equals(b);
            }
        }
        public static bool operator!=(AnimatedMaterial a, AnimatedMaterial b)
        {
            return !(a == b);
        }
        
    }


    //=================================================================================================================
    //
    //      Grouping



    public class MaterialGroup
    {
        private Dictionary<Renderer, List<AnimatedMaterial>> renderGroup;
        private Dictionary<Shader, List<AnimatedMaterial>> shaderGroup;

//        public float sequenceTime { get; set; }

        public MaterialGroup()
        {
            renderGroup = new Dictionary<Renderer, List<AnimatedMaterial>>();
            shaderGroup = new Dictionary<Shader, List<AnimatedMaterial>>();
        }


        //  group building

        public void Add(Renderer r, Material m)
        {
            var rm = AnimatedMaterial.Get(r, m);
            if(renderGroup.ContainsKey(r))
            {
                if(!renderGroup[r].Contains(rm))
                {
                    renderGroup[r].Add(rm);
                }
            }
            else
            {
                renderGroup.Add(r, new List<AnimatedMaterial>());
                renderGroup[r].Add(rm);
            }

            var shader = m.shader;
            if(shaderGroup.ContainsKey(shader))
            {
                if(!shaderGroup[shader].Contains(rm))
                {
                    shaderGroup[shader].Add(rm);
                }
            }
            else
            {
                shaderGroup.Add(shader, new List<AnimatedMaterial>());
                shaderGroup[shader].Add(rm);
            }
        }

        public void Add(IEnumerable<Renderer> r, Material m)
        {
            foreach(var renderer in r)
            {
                Add(r, m);
            }
        }
        public bool Add(IEnumerable<Renderer> r, Shader s)
        {
            bool added = false;
            foreach(var renderer in r)
            {
                foreach(var mat in renderer.materials)
                {
                    if(mat != null && mat.shader != null && mat.shader == s)
                    {
                        Add(renderer, mat);
                        added = true;
                    }
                }
            }
            return added;
        }
        public bool Add(IEnumerable<Renderer> r, string shader)
        {
            Shader s = Shader.Find(shader);
            if(s != null)
            {
                return Add(r, s);
            }
            return false;
        }

        public bool Remove(Renderer r, Handling handling=Handling.KeepCurrent)
        {
            if(renderGroup.ContainsKey(r))
            {
                bool removed = renderGroup[r].Count > 0;
                releaseHandling(handling, renderGroup[r]);
                renderGroup.Remove(r);
                foreach(var shader in shaderGroup.Keys)
                {
                    for(int i = 0; i < shaderGroup[shader].Count; i++)
                    {
                        if(shaderGroup[shader][i].renderer == r)
                        {
                            releaseHandling(handling, shaderGroup[shader][i]);
                            shaderGroup[shader].RemoveAt(i);
                            i--;
                            removed = true;
                        }
                    }
                }
                return removed;
            }
            return false;
        }
        public bool Remove(Material m, Handling handling=Handling.KeepCurrent)
        {
            var shader = m.shader;
            if(shaderGroup.ContainsKey(shader))
            {
                bool removed = shaderGroup[shader].Count > 0;
                releaseHandling(handling, shaderGroup[shader]);
                shaderGroup.Remove(shader);
                foreach(var renderer in renderGroup.Keys)
                {
                    for(int i = 0; i < renderGroup[renderer].Count; i++)
                    {
                        if(renderGroup[renderer][i].currentMaterial == m)
                        {
                            releaseHandling(handling, renderGroup[renderer][i]);
                            renderGroup[renderer].RemoveAt(i);
                            i--;
                            removed = true;
                        }
                    }
                }
                return removed;
            }
            return false;
        }
        public bool Remove(Shader shader, Handling handling=Handling.KeepCurrent)
        {
            if(shaderGroup.ContainsKey(shader))
            {
                bool removed = shaderGroup[shader].Count > 0;
                releaseHandling(handling, shaderGroup[shader]);
                shaderGroup.Remove(shader);
                foreach(var renderer in renderGroup.Keys)
                {
                    for(int i = 0; i < renderGroup[renderer].Count; i++)
                    {
                        if(renderGroup[renderer][i].currentMaterial.shader == shader)
                        {
                            releaseHandling(handling, renderGroup[renderer][i]);
                            renderGroup[renderer].RemoveAt(i);
                            i--;
                            removed = true;
                        }
                    }
                }
                return removed;
            }
            return false;
        }

        public void Clear(Handling handling=Handling.KeepCurrent)
        {
            foreach(var shader in shaderGroup.Keys)
            {
                releaseHandling(handling, shaderGroup[shader]);
            }
        }

        private void releaseHandling(Handling handling, AnimatedMaterial mat)
        {
            if(handling == Handling.PoolAndCleanup)
            {
                if(mat.hasClonedMaterial())
                {
                    mat.RevertMaterial();
                }
            }
        }
        private void releaseHandling(Handling handling, IEnumerable<AnimatedMaterial> mats)
        {
            foreach(var mat in mats)
            {
                releaseHandling(handling, mat);
            }
        }


        //  access

        public bool CheckForProperty(Shader s, string property)
        {
            return shaderGroup.ContainsKey(s) && shaderGroup[s].Count > 0 && shaderGroup[s][0].CheckForProperty(property);
        }

        public IEnumerable<Shader> GetAllShaders()
        {
            return shaderGroup.Keys;
        }

        public IEnumerable<AnimatedMaterial> GetByShader(Shader s, string propertyCheck="")
        {
            if(shaderGroup.ContainsKey(s))
            {
                if(shaderGroup[s].Count == 0 || string.IsNullOrEmpty(propertyCheck) || shaderGroup[s][0].CheckForProperty(propertyCheck))
                {
                    return shaderGroup[s];
                }
                
            }
            return new AnimatedMaterial[0];
        }
        public IEnumerable<AnimatedMaterial> GetByRenderer(Renderer r, string propertyCheck="")
        {
            if(renderGroup.ContainsKey(r))
            {
                if(renderGroup[r].Count == 0 || string.IsNullOrEmpty(propertyCheck) || renderGroup[r][0].CheckForProperty(propertyCheck))
                {
                    return renderGroup[r];
                }
            }
            return new AnimatedMaterial[0];
        }



        //  property setter


        public void SetFloatProperty(Shader shader, string prop, float val)
        {
            if(shader != null && CheckForProperty(shader, prop))
            {
                var query = GetByShader(shader);
                foreach(var mat in GetByShader(shader))
                {
                    mat.currentMaterial.SetFloat(prop, val);
                }
            }
        }
        public void SetBoolProperty(Shader shader, string prop, bool val)
        {
            SetFloatProperty(shader, prop, val ? 1f : 0f);
        }
        public void SetVec2Property(Shader shader, string prop, Vector2 val)
        {
            SetVec4Property(shader, prop, new Vector4(val.x, val.y, 0, 0));
        }
        public void SetVec3Property(Shader shader, string prop, Vector3 val)
        {
            SetVec4Property(shader, prop, new Vector4(val.x , val.y, val.z, 0));
        }
        public void SetVec4Property(Shader shader, string prop, Vector4 val)
        {
            if(shader != null && CheckForProperty(shader, prop))
            {
                foreach(var mat in GetByShader(shader))
                {
                    mat.currentMaterial.SetVector(prop, val);
                }
            }
        }
        public void SetColorProperty(Shader shader, string prop, Color val)
        {
            if(shader != null && CheckForProperty(shader, prop))
            {
                foreach(var mat in GetByShader(shader))
                {
                    mat.currentMaterial.SetColor(prop, val);
                }
            }
        }
        public void SetTexProperty(Shader shader, string prop, Texture val)
        {
            if(shader != null && CheckForProperty(shader, prop))
            {
                foreach(var mat in GetByShader(shader))
                {
                    mat.currentMaterial.SetTexture(prop, val);
                }
            }
        }

    }

}