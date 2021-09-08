using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

//=================================================================================================================

namespace Utility.Pooling
{


	public class PoolObject
	{
		public bool isObjDead;
		public GameObject gameObject;
		public PoolObject(bool isObjDead, GameObject obj)
		{
			this.isObjDead = isObjDead;
			this.gameObject = obj;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------



    public class ObjectPool : MonoBehaviour
    {
		static Vector3 NULLPOS = Vector3.one * -100;
        Dictionary<GameObject, List<PoolObject>> pools = new Dictionary<GameObject, List<PoolObject>>();
        Dictionary<GameObject, GameObject> prefabLookUp = new Dictionary<GameObject, GameObject>();
        List<IPoolComponent> postActivateQueue = new List<IPoolComponent>();
		Dictionary<ITimedPoolComponent, DeactivationInfo> timedDeactivations = new Dictionary<ITimedPoolComponent, DeactivationInfo>();


		//	singleton
		public static ObjectPool instance {
			get {
				if (_instance == null) {
					GameObject go = new GameObject ("ObjectPool");
					_instance = go.AddComponent<ObjectPool> ();
				}
				return _instance;
			}
		}
		private static ObjectPool _instance;



		private void Awake()
		{
			_instance = this;
			DontDestroyOnLoad (this.gameObject);
		}


		//-----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Request an object from the pool. It will search for a dead object of the prefab type. If the prefab is not
        /// yet known or if there are no dead objects of that type, then it will prewarm and activate one new object (and return it).
        /// </summary>
		/// <param name="prefab">Prefab an instance is requested from</param>
		/// <param name="hierarchyDepth">All IPoolComponents in the prefab's hierarchy up to given depth are notified on activation.</param>
        public GameObject Request(GameObject prefab, int hierarchyDepth=int.MaxValue)
        {
            if (prefab == null)
            {
                Debug.Log("PrefabPool.Request: Prefab was null");
                return null;
            }
            if (!pools.ContainsKey(prefab))
            {
                return PrewarmAndActivate(prefab, hierarchyDepth);
            }

            foreach (PoolObject obj in pools[prefab])
            {
                if (obj.isObjDead)
                {
                    Activate(obj, hierarchyDepth);
                    return obj.gameObject;
                }
            }
            return PrewarmAndActivate(prefab, hierarchyDepth);
        }

		//-----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Spawn several dead objects, the prefab will be instantiated but not yet activated.
        /// </summary>
        /// <param name="count">How many objects will be prewarmed.</param>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                return;
            }
            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new List<PoolObject>();
            }
            for (int i = 0; i < count; i++)
            {
                PoolObject spawnedObject = InstantiateInactive(prefab);
                pools[prefab].Add(spawnedObject);
            }
        }

        /// <summary>
        /// Spawn a dead object, the prefab will be instantiated but not yet activated.
        /// </summary>
        public PoolObject Prewarm(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }
            if (!pools.ContainsKey(prefab))
            {
                pools[prefab] = new List<PoolObject>();
            }

            PoolObject spawnedObject = InstantiateInactive(prefab);
            pools[prefab].Add(spawnedObject);
            return spawnedObject;
        }

		//-----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Mark GameObject as dead, so that it can be reused later.
        /// Will call Deactivate on all IPoolComponents / ITimedPoolComponents
        /// </summary>
		/// </param name="gameObject">pooled gameObject to kill</param>
		/// <param name="hierarchyDepth">All IPoolComponents in the prefab's hierarchy up to given depth are notified on activation</param>
        public bool Kill(GameObject gameObject, int hierarchyDepth=int.MaxValue)
        {
            if (gameObject == null)
            {
                return false;
            }
            if (prefabLookUp.ContainsKey(gameObject) && pools.ContainsKey(prefabLookUp[gameObject]))
            {
                PoolObject pObj = pools[prefabLookUp[gameObject]].Find((p) => p.gameObject == gameObject);
                Deactivate(pObj, hierarchyDepth);
				return true;
            }
			return false;
        }

		//-----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Removes all dead objects from the pool and optionally calls Object.Destroy with them.
        /// </summary>
        public void Cleanup(bool useUnityDestroy = true)
        {
            foreach (var pool in pools.Values)
            {
                pool.RemoveAll((p) =>
                {
                    if (p.isObjDead)
                    {
                        prefabLookUp.Remove(p.gameObject);
                        if (useUnityDestroy && p.gameObject != null)
                        {
                            Destroy(p.gameObject);
                        }
                        return true;
                    }
                    return false;
                });
            }
        }



		//-----------------------------------------------------------------------------------------------------------------


        GameObject PrewarmAndActivate(GameObject prefab, int depth)
        {
            PoolObject newOne = Prewarm(prefab);
            Activate(newOne, depth);
            return newOne.gameObject;
        }

        PoolObject InstantiateInactive(GameObject prefab)
        {
            bool oldStatus = prefab.activeSelf;
            prefab.SetActive(false);
            PoolObject spawnedObject = new PoolObject(true, Instantiate(prefab));
            prefab.SetActive(oldStatus);

            if(spawnedObject.gameObject.GetComponent<RectTransform>() == null)
            {
                spawnedObject.gameObject.transform.position = NULLPOS;
                spawnedObject.gameObject.transform.SetParent(transform);
            }
            else
            {
                spawnedObject.gameObject.transform.position = prefab.transform.position;
                spawnedObject.gameObject.transform.SetParent(prefab.transform);
            }
            prefabLookUp[spawnedObject.gameObject] = prefab;
            return spawnedObject;
        }

        void Activate(PoolObject poolObject, int depth)
        {
            poolObject.isObjDead = false;
			poolObject.gameObject.SetActive(true);
			poolObject.gameObject.SendMessagesToComponents<IPoolComponent>((IPoolComponent x)=> { if(x != null) x.Activate(); }, depth);
           
			poolObject.gameObject.SendMessagesToComponents<IPoolComponent>((IPoolComponent x)=> { if(x != null) postActivateQueue.Add(x); });
        }

        void Deactivate(PoolObject poolObject, int depth)
        {
			poolObject.gameObject.SendMessagesToComponents<IPoolComponent>((x)=> {
				if(postActivateQueue.Contains(x))
				{
					postActivateQueue.Remove(x);
				}
			});

			//	kill coroutines (always applied on all objects)
			poolObject.gameObject.SendMessagesToComponents<MonoBehaviour>(x=>x.StopAllCoroutines());	

			List<ITimedPoolComponent> timedComponents = poolObject.gameObject.GetInterfacesInHierarchy<ITimedPoolComponent>(depth);
			if(timedComponents.Count == 0)
			{
				//	deactivate immedtiately
				_freePoolObject(poolObject, depth);
			}
			else
			{
				//	deactivate timed
				foreach(var timed in timedComponents)
				{
					if(!timedDeactivations.ContainsKey(timed))
					{
						timedDeactivations.Add(timed, new DeactivationInfo(poolObject, depth));
						StartCoroutine(timed.ProcessTimedDeactivate(onDeactivateTimed));
					}
				}
			}
        }

		/// <summary>
		/// callback for timed deactivations. Frees the associated poolObject when no more
		/// deactivation routines are running for it.
		/// </summary>
		void onDeactivateTimed(ITimedPoolComponent component)
		{
			if(timedDeactivations.ContainsKey(component))
			{
				DeactivationInfo info = timedDeactivations[component];
				timedDeactivations.Remove(component);
				if(!timedDeactivations.Values.ToList().Exists(x=> x.obj==info.obj))
				{
					//	deactivate obj
					_freePoolObject(info.obj, info.hierarchyDepth);
				}
			}
		}

		/// <summary>
		/// Deactivates all PoolComponents, the deactives the pool gameobject and resets its transform
		/// </summary>
		void _freePoolObject(PoolObject obj, int hierarchyDepth=int.MaxValue)
		{
			if(obj != null)
			{
				//	call deactivate on all pool components
				obj.gameObject.SendMessagesToComponents<IPoolComponent>( x=>x.Deactivate(), hierarchyDepth );
				obj.gameObject.SetActive(false);
				obj.isObjDead = true;

                if(obj.gameObject.GetComponent<RectTransform>() == null)
                {
                    obj.gameObject.transform.position = NULLPOS;
				    obj.gameObject.transform.SetParent(transform);
                }
			}
		}

		struct DeactivationInfo
		{
			public PoolObject obj;
			public int hierarchyDepth;
			
			public DeactivationInfo(PoolObject obj, int depth)
			{
				this.obj = obj;
				this.hierarchyDepth = depth;
			}
		}


        void Start()
        {
            StartCoroutine(EndOfFrame());
        }

        IEnumerator EndOfFrame()
        {
            while(true)
            {
				yield return new WaitForEndOfFrame();
                int currentCount = postActivateQueue.Count;
                for (int i = 0; i < currentCount; i++)
                {
                    postActivateQueue[i].PostActivate();
                }
				postActivateQueue.Clear();
            }
        }
    }
}



//=================================================================================================================