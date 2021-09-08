using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utility.ManagedObservers
{

    

    /// @brief
    /// Derive your own interfaces to allow any observer-style object to be messaged generically.
    ///
    public interface IManagedObserver
    {
        
    }

    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// Implemented by event-generating objects representing the source of a message send to an observer.
    ///
    public interface IObservable
    {
        void AddObserver<TObserver>(TObserver observer) where TObserver: class, IManagedObserver;
        void RemoveObserver<TObserver>(TObserver observer) where TObserver: class, IManagedObserver;   
    }


    //-----------------------------------------------------------------------------------------------------------------

    //  collection interfaces
    
    /// @internal
    /// @brief
    /// A collection of observers exposing a set of methods that can be called from anywhere.
    ///
    public interface IObserverCollection
    {
        Type mType { get; }
        int Count { get; }
        bool Match(IManagedObserver o);
        void Add(IManagedObserver o, IObservable binding=null);
        void Remove(IManagedObserver o, IObservable binding=null);
        void RemoveAll(IManagedObserver o);
    }
    /// @endinternal


    /// @copydoc IObserverCollection
    public interface IObserverCollection<TObserver> : IObserverCollection where TObserver : class, IManagedObserver
    {
        /// @brief
        /// Call a function on this collection of observers.
        ///
        void FireEvent(IObservable source, Action<TObserver> e);
    }



    //=================================================================================================================
    //
    //  MANAGER
    //

    /// @brief
    /// Generic observer pattern implementation via interfaces.
    ///
    /// @details
    /// The manager allows for generic interface observer.
    /// Listening objects can implement a generic interfaces and register it within the manager,
    /// event-generating entities can then anonymously and generically call individual methods 
    /// of all registered observers whenever an event occurs.
    ///
    public static class ObserverManager
    {
        static bool debug = false;

        private static Dictionary<Type, IObserverCollection> cache;

        static ObserverManager()
        {
            cache = new Dictionary<Type, IObserverCollection>();
        }

        public static void ClearAll()
        {
            if(cache != null) cache.Clear();
        }

        /// @brief
        /// Register an observer interface type for event-generating entities to call. 
        ///
        public static void RegisterObserverType<TObserver>() where TObserver : class, IManagedObserver
        {
            var type = typeof(TObserver);
            if(!cache.ContainsKey(type))
            {
                cache.Add(type, new ObserverCollection<TObserver>());
                if(debug)
                {
                    Debug.Log("ObserverManager: registered observer type=[" + type.ToString() + "]");
                }
            }
        }

        /// @brief
        /// Call this from the event-generating side to receive the right collection of observers to message.
        ///
        public static IObserverCollection<TObserver> Get<TObserver>() where TObserver : class, IManagedObserver
        {
            var type = typeof(TObserver);
            if(!cache.ContainsKey(type))
            {
                RegisterObserverType<TObserver>();
            }
            return cache[type] as IObserverCollection<TObserver>;
        }

        /// @brief
        /// Get a collection of listeners based on the type of given observer.
        ///
        public static IObserverCollection Get(IManagedObserver o)
        {
            foreach(var collection in cache.Values)
            {
                if(collection.Match(o))
                {
                    return collection;
                }
            }
            return null;
        }

        /// @brief
        /// Adds an observer to the system.
        ///
        /// @details
        /// Add an observer interface of a managed type to expose its methods to all observables
        /// that want to notify their events.
        /// Observers can be registered multiple time with different bindings. If no binding is given,
        /// all messages are received.
        ///
        /// @param observer The observing interface to be registered.
        /// @param binding Optional restriction to a specific observable object.
        ///
        public static void AddObserver<TObserver>(TObserver observer, IObservable binding=null) where TObserver : class, IManagedObserver
        {
            var type = typeof(TObserver);
            if(!cache.ContainsKey(type))
            {
                RegisterObserverType<TObserver>();
            }
            cache[type].Add(observer, binding);
        }

        /// @brief
        /// Removes an observer with a specific/no binding from the system.
        /// 
        public static void RemoveObserver<TObserver>(TObserver observer, IObservable binding=null) where TObserver : class, IManagedObserver
        {
            var type = typeof(TObserver);
            if(cache.ContainsKey(type))
            {
                cache[type].RemoveAll(observer);
            }
        }

        /// @brief
        /// Removes the specific type of observer instance from the system
        ///
        public static void ClearObserver<TObserver>(TObserver observer) where TObserver : class, IManagedObserver
        {
            var type = typeof(TObserver);
            if(cache.ContainsKey(type))
            {
                cache[type].RemoveAll(observer);
            }
        }

        /// @brief
        /// Removes all instances of this observer from the system.
        ///
        public static void ClearObserver(IManagedObserver observer)
        {
            foreach(var collection in cache.Values)
            {
                collection.RemoveAll(observer);
            }
        }


        [Obsolete("use generic version instead")]
        /// @brief
        /// Adds an observer to the system.
        ///
        /// @details
        /// Add an observer interface of a managed type to expose its methods to all entities
        /// that want to notify their events.
        ///
        /// @attention
        /// It is important to call RegisterObserverType() from the observed object before calling this, otherwise the manager will not be able
        /// to correctly assign type of observer. If you know the type in advance, use AddObserver(TObserver) instead. 
        ///
        public static void AddObserver(IManagedObserver o)
        {
            foreach(var collection in cache.Values)
            {
                if(collection.Match(o))
                {
                    if(debug)
                    {
                        Debug.Log("added observer of type=[" + o.GetType() + "] to managed list of type<" + collection.mType + ">");
                    }
                    collection.Add(o);
                    return;
                }
            }
            Debug.LogError("ObserverManager:: trying to add generic observer of a type that wasn't registered =[" + o.GetType() + "]");
        }

        [Obsolete("use generic version instead")]
        /// @brief
        /// Removes an observer with a specific/no binding from the system.
        /// 
        public static void RemoveObserver(IManagedObserver observer, IObservable binding=null)
        {
            foreach(var collection in cache.Values)
            {
                if(collection.Match(observer))
                {
                    collection.Remove(observer, binding);
                    return;
                }
            }
        }

        



        //=================================================================================================================
        //
        //  COLLECTION
        //

        private class ObserverCollection<TObserver> : IObserverCollection<TObserver> where TObserver : class, IManagedObserver 
        {
            public readonly Type mType;
            private List<TObserver> observers;
            private List<IObservable> bindings;

            public ObserverCollection()
            {
                observers = new List<TObserver>();
                bindings = new List<IObservable>();
                mType = typeof(TObserver);
            }

            Type IObserverCollection.mType { get { return mType; } }

            public int Count { get { return observers.Count; } }

            public bool Match(IManagedObserver o)
            {
                return o is TObserver;
      //          if(o is TObserver)
      //          {
      //              return !observers.Exists(x=> ReferenceEquals(o, x));
      //          }
      //          return false;
            }
            public void Add(IManagedObserver o, IObservable binding=null)
            {
                addObserver(o as TObserver, binding);
            }
            public void Add(TObserver o, IObservable binding=null)
            {
                addObserver(o, binding);
            }
            public void Remove(IManagedObserver o, IObservable binding=null)
            {
                removeObserver(o as TObserver, binding);
            }
            public void Remove(TObserver o, IObservable binding=null)
            {
                removeObserver(o, binding);
            }
            public void RemoveAll(IManagedObserver o)
            {
                removeAll(o);
            }
            public void FireEvent(IObservable binding, Action<TObserver> e)
            {
                if(e != null)
                {
                    for(int i = 0; i < observers.Count; i++)
                    {
                        if(binding == null || bindings[i] == binding)
                        {
                            e(observers[i]);
                        }
                    }
                }
            }


            private bool addObserver(TObserver observer, IObservable binding)
            {
                if(observer != null)
                {
                    bool canAdd = true;
                    for(int i = 0; i < observers.Count; i++)
                    {
                        if(ReferenceEquals(observers[i], observer))
                        {
                            if(bindings[i] == binding)
                            {
                                canAdd = false;
                                break;
                            }
                        }
                    }
                    if(canAdd)
                    {
                        observers.Add(observer);
                        bindings.Add(binding);
                        return true;
                    }
                }
                return false;
            }

            private bool removeObserver(TObserver observer, IObservable binding)
            {
                if(observer != null)
                {
                    for(int i = 0; i < observers.Count; i++)
                    {
                        if(ReferenceEquals(observers[i], observer) 
                        && ReferenceEquals(bindings[i], binding))
                        {
                            observers.RemoveAt(i);
                            bindings.RemoveAt(i);
                            return true;
                        }
                    }
                }
                return false;
            }

            private void removeAll(IManagedObserver o)
            {
                for(int i = 0; i < observers.Count; i++)
                {
                    if(ReferenceEquals(observers[i], o))
                    {
                        observers.RemoveAt(i);
                        bindings.RemoveAt(i);
                        i--;
                    }
                }
            }

        }


    }


}


