using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//=================================================================================================================

namespace Utility.Pooling
{

	public class MaterialPool : MonoBehaviour
	{
		private Dictionary<Material, List<Material>> materials;


		//	singleton

		public static MaterialPool instance
		{
			get { 
				if (_instance == null) {
					GameObject go = new GameObject ("MaterialPool");
					_instance = go.AddComponent<MaterialPool> ();
				}
				return _instance;
			}
		}
		private static MaterialPool _instance;

		//-----------------------------------------------------------------------------------------------------------------

		void Awake()
		{
			_instance = this;
			materials = new Dictionary<Material, List<Material>> (5);
			DontDestroyOnLoad (this);
		}

		//-----------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// call this if you have a number of objects in the scene
		/// and don't know the exact number
		/// </summary>
		public void PrewarmInSequence(Material sharedMat)
		{
			if (sharedMat != null) {
				_addToPool (sharedMat, 1, true);
			}
		}

		/// <summary>
		/// reserve a number of material clones for later use.
		/// </summary>
		public void Prewarm(Material sharedMat, int count)
		{
			if (sharedMat != null) {
				_addToPool (sharedMat, count, false);
			}
		}

		/// <summary>
		/// Get a pooled instance of an original material
		/// </summary>
		public Material Request(Material sharedMat)
		{
			if(sharedMat != null)
			{
				if(materials.ContainsKey(sharedMat))
				{
					var m = materials [sharedMat].FirstOrDefault ();
					if (m == null) {
						return _instantiate (sharedMat);
					}
					else {
						m.CopyPropertiesFromMaterial (sharedMat);
						return m;
					}
				}
				else
				{
					_addToPool (sharedMat, 2, false);
					return materials [sharedMat].First ();
				}
			}
			return null;
		}
			
		/// <summary>
		/// free a pooled instance and get the shared orignal back.
		/// if the material wasn't pooled, it is simply returned
		/// </summary>
		public Material Free(Material pooledMat)
		{
			Material m = _getKeyMat (pooledMat);
			if (m != null)
			{
				materials [m].Add (pooledMat);
				return m;
			}
			else
				return pooledMat;
		}

		public void Cleanup()
		{
			materials.Clear();
		}
			
		//-----------------------------------------------------------------------------------------------------------------

		const string _suffix = "_pooled";

		private void _addToPool(Material sharedMat, int count, bool addedAsSequence)
		{
			if(materials.ContainsKey(sharedMat))
			{
				if (addedAsSequence && materials [sharedMat].Count == materials [sharedMat].Capacity)
					materials [sharedMat].Capacity *= 2;
				else
					materials [sharedMat].Capacity += count;
			}
			else
			{
				materials.Add (sharedMat, new List<Material>(count));
			}
			for(int i = 0; i < count; i++)
			{
				var m = Material.Instantiate<Material> (sharedMat);
				m.name += _suffix;
				materials [sharedMat].Add (m);
			}
		}

		private Material _instantiate(Material sharedMat)
		{
			var m = Material.Instantiate<Material> (sharedMat);
			m.name += _suffix;
			return m;
		}

		private Material _getKeyMat(Material pooledMat)
		{
			foreach(var key in materials.Keys)
			{
				if(_compareToOriginal(key, pooledMat))
				{
					return key;
				}
			}
			return null;
		}
			
		private bool _compareToOriginal(Material original, Material pooled)
		{
			if (pooled.name.IndexOf (_suffix) != -1)
				return original.name == name.Substring (0, name.Length - _suffix.Length);
			else
				return false;
		}
	}


}


//=================================================================================================================