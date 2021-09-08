using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using F360.Backend.Core;

namespace F360.Backend
{


    /// @brief
    /// Manages all running clients of this app instance.
    ///
    /// must be cleaned manually via Shutdown() on app reload.
    ///
    ///
    public class F360Backend : MonoBehaviour
    {
        

        /// @brief
        /// Server request to get a list of bundle versions matching this client's patchlevel.
        ///
        /// input:      client patchlevel
        /// returns:    hashset[bundleID, version_on_server]
        ///
        public const string REQUEST_CHECK_BUNDLE_UPDATES = "checkBundleVersions";                       //  Needed anymore?



        //-----------------------------------------------------------------------------------------------
        //
        //  INTERFACE
        //
        //-----------------------------------------------------------------------------------------------


        public static event System.Action<IClient> clientAvailable;

        /// @returns wether client of given type exists
        ///
        public static bool Exists<TClient>() where TClient : class, IClient
        {
            var t = typeof(TClient);
            var c = instance.clients;
            return c.ContainsKey(t) && c[t] != null;
        }


        /// @returns wether client of given type exists and is running (is connected // tries to connect)
        ///
        public static bool isRunning<TClient>() where TClient : class, IClient
        {
            var t = typeof(TClient);
            var c = instance.clients;
            return c.ContainsKey(t) && c[t] != null && c[t].isRunning();
        }


        /// @returns wether client of given type exists and is connected
        ///
        public static bool isConnected<TClient>() where TClient : class, IClient
        {
            var t = typeof(TClient);
            var c = instance.clients;
            return c.ContainsKey(t) && c[t] != null && c[t].isConnected();
        }


        /// @brief
        /// create & start a client object of given type
        ///
        /// @param TClient concrete type of client, must derive from ClientBase and implement IClient
        /// @returns wether client exists & was started
        ///
        public static bool StartClient<TClient>(params object[] args) where TClient : ClientBase
        {
            return instance.startClientInternal<TClient>(args);
        }


        /// @brief
        /// get instance of an existing client
        ///
        public static TClient GetClient<TClient>() where TClient : class, IClient
        {
            return instance.getClientInternal<TClient>();
        }

        /// @brief
        /// called when user logs in
        ///
        public static void SetUserCredentials(string usr, string pass)
        {
            instance.setUserCredentialsInternal(usr, pass);
        }


        /// @brief
        /// stops & clears all running clients
        ///
        public static void ShutDown(bool killObjects=false)
        {
            instance.shutdownInternal(killObjects);
        }


        //-----------------------------------------------------------------------------------------------

        bool startClientInternal<TClient>(params object[] args) where TClient : ClientBase
        {
            var t = typeof(TClient);
            if(!clients.ContainsKey(t) || clients[t] == null)
            {
                var c = GameObject.FindObjectOfType<TClient>();
                if(c != null)
                {
                    var cc = c as IClient;
                    if(addToClients(t, cc))
                    {
                        clientAvailable?.Invoke(cc);
                        return cc.Run(args);
                    }
                    else
                    {
                        Debug.LogError(t.Name + " does not implement IClient interface!");
                    }
                }
                else
                {
                    var go = new GameObject(t.Name);
                    c = go.AddComponent<TClient>();
                    var cc = c as IClient;
                    if(addToClients(t, cc))
                    {
                        clientAvailable?.Invoke(cc);
                        return cc.Run(args);
                    }
                    else
                    {
                        GameObject.Destroy(go);
                        Debug.LogError(t.Name + " does not implement IClient interface!");
                    }
                }
                return false;
            }
            else
            {
                if(!clients[t].isRunning())
                {
                    clients[t].Run(args);
                }
                return clients[t].isRunning();
            }
        }

        TClient getClientInternal<TClient>() where TClient : class, IClient
        {
            var t = typeof(TClient);
            foreach(var key in clients.Keys)
            {
                if(key == t)
                {
                    return clients[key] as TClient;
                }
                else if(clients[key] is TClient)
                {
                    return clients[key] as TClient;
                }
            }
            return null;
        }

        void setUserCredentialsInternal(string usr, string pass)
        {
            foreach(var key in clients.Keys)
            {
                clients[key].SetCredentials(usr, pass);
            }
        }

        void shutdownInternal(bool killObjects)
        {
            foreach(var c in clients.Values)
            {
                if(c != null)
                {
                    c.Stop();
                    if(killObjects)
                    {
                        var component = c as Component;
                        if(component != null)
                        {
                            GameObject.Destroy(component.gameObject);
                        }
                    }
                }
            }
            clients.Clear();
        }


        //-----------------------------------------------------------------------------------------------

        Dictionary<Type, IClient> clients = new Dictionary<Type, IClient>();


        internal static void AddClient(IClient c)
        {
            var t = c.GetType();
            if(instance.addToClients(t, c))
            {
                clientAvailable?.Invoke(c);
            }
        }


        bool addToClients(Type t, IClient c) 
        {
            if(c != null)
            {
                if(!clients.ContainsKey(t)) clients.Add(t, c);
                else clients[t] = c;
                return true;
            }
            return false;
        }



        //-----------------------------------------------------------------------------------------------

        //  Singleton access

        static F360Backend instance
        {
            get 
            {
                if(_i == null)
                {
                    _i = GameObject.FindObjectOfType<F360Backend>();
                    if(_i == null) {
                        GameObject go = new GameObject("Backend");
                        _i = go.AddComponent<F360Backend>();
                    }
                }
                return _i;
            }
        }
        static F360Backend _i;


        void Awake()
        {
            if(instance != null)
            {
                _i = this;
                Users.ActiveUser.login += onUserLogin;
            }
            else
            {
                GameObject.Destroy(this.gameObject);
            }
        }


        void onUserLogin(Users.User usr)
        {
            /*if(usr != null)
            {
                setUserCredentialsInternal(usr.Name, usr.Pass);
            }
            else
            {
                setUserCredentialsInternal("", "");
            }*/
        }
        
    }

}

