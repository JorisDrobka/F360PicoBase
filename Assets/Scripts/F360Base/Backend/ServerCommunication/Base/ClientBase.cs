using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;




namespace F360.Backend.Core
{

    /// @brief
    /// base behaviour class for all clients. Derive from this if you want to implement a custom client solution
    /// that is fully integrated with F360Backend.
    ///
    ///
    public abstract class ClientBase : MonoBehaviour, IClientInternal
    {

        public const string DEFAULT_SESSION_ID = "_defaultSession";

        

        internal struct SessionInfo
        {
            public IServerSessionInternal session;
            public ServerSessionAuthMode authMode;
            public System.Action<IServerSession> renewDelegate;

            public SessionInfo(IServerSessionInternal s, ServerSessionAuthMode m=ServerSessionAuthMode.KEEP, System.Action<IServerSession> renew=null)
            {
                this.session = s;
                this.authMode = m;
                this.renewDelegate = renew;
            }
        }



        [SerializeField] protected bool debug;

        internal Dictionary<string, SessionInfo> sessions = new Dictionary<string, SessionInfo>();
        protected string _userSecret = "";
        protected string _userPass = "";

        protected bool preventHeartbeat;

        protected virtual void Awake()
        {
            F360Backend.AddClient(this);
//            GameObject.DontDestroyOnLoad(this);
        }



        public event System.Action<string, string, int> authenticated;


        /// @brief
        /// returns wether client is active and connect / trying to connect
        ///
        public abstract bool isRunning();

        /// @brief
        /// returns wether conditions for connection are fulfilled (internet, license state..)
        ///
        public abstract bool canConnect();
        
        /// @brief
        /// returns wether a connection was established
        ///
        public abstract bool isConnected();

        /// @brief
        /// starts the client and returns isRunning()
        ///
        public abstract bool Run(params object[] args);
        /// @brief
        /// stops the client and returns wether an active connection was interrupted
        ///
        public abstract bool Stop();
        
        /// @brief
        /// starts the client and creates or returns the default session
        ///
        public abstract void Connect(ServerConnectCallback callback);

        /// @brief
        /// creates a new session or retrieves an existing one.
        ///
        /// @param id the session id
        ///
        public abstract TSession OpenSession<TSession>(string id, params object[] args) where TSession : class, IServerSession, new();

        /// @brief
        /// enqueues a request to be send to the server. create requests via the session instead of directly calling this.
        ///
        public abstract THandler CreateAndEnqueue<THandler, R>(ServerSession session, ServerRequest request, ServerRequestCallback<R> callback, ServerRequestErrorCallback error) 
                                                                where THandler : Core.ServerRequestHandler<R>, new() where R : class;



        public void DisableServerHeartbeat(bool b)
        {
            preventHeartbeat = b;
        }


        public void ProcessRequestsNow()
        {
            if(isRunning())
            {
                processRequestsNow();
            }
        }

        protected abstract bool isProcessingRequests();
        protected abstract void processRequestsNow();

        protected abstract void stopProcessRequests();
        protected abstract void restartProcessRequests();

        protected ServerSession baseSession
        {
            get {
                if(canConnect()) {
                    if(_baseSession == null || _baseSession.isClosed())
                    {
                        _baseSession = createBaseSession();
                    }
                }
                return _baseSession;
            }
        }
        protected ServerSession _baseSession;


        //  creates a new base session - configFunc is called before session.onConnect() to allow additional setup
        protected virtual ServerSession createBaseSession(System.Action<ServerSession> configFunc=null)
        {
            var session = hasCredentials() ? new AuthenticatedServerSession() : new ServerSession();
            if(!sessions.ContainsKey(DEFAULT_SESSION_ID))
                sessions.Add(DEFAULT_SESSION_ID, new SessionInfo(session));
            else
                sessions[DEFAULT_SESSION_ID] = new SessionInfo(session);
            sessions[DEFAULT_SESSION_ID].session.Init(DEFAULT_SESSION_ID, this, _userSecret, _userPass);
            if(configFunc != null)
            {
                configFunc(session);
            }
            sessions[DEFAULT_SESSION_ID].session.onConnect();

            //  share token with other sessions
            foreach(var id in sessions.Keys)
            {
                if(id != DEFAULT_SESSION_ID && sessions[id].authMode == ServerSessionAuthMode.SHARE_TOKEN)
                {
                    if(sessions[id].session is AuthenticatedServerSession)
                    {
                        configFunc?.Invoke(sessions[id].session as AuthenticatedServerSession);
                    }
                    else if(sessions[id].renewDelegate != null)
                    {
                        var authSession = new AuthenticatedServerSession();
                        var authSessionI = authSession as IServerSessionInternal;
                        authSessionI.Init(id, this, _userSecret, _userPass);
                        configFunc?.Invoke(authSession);
                        sessions[id] = new SessionInfo(authSessionI, ServerSessionAuthMode.SHARE_TOKEN, sessions[id].renewDelegate);
                        sessions[id].renewDelegate(sessions[id].session);
                    }
                }
            }

            if(isProcessingRequests())
            {
                restartProcessRequests();
            }
            
            if(debug)
            {
                Debug.Log("Client created new baseSession! credentials? " + (hasCredentials() ? ("usr=[" + _userSecret + ", " + _userPass + "]") : "[ None ]"));
            }
            return session;
        }

        /// @brief
        /// opens a new server session with given credentials, creating a new token.
        ///
        public void SetCredentials(string usr, string pass)
        {
            if(_userSecret != usr || _userPass != pass)
            {
                if(debug)
                {
                    Debug.Log("ClientBase:: SET CREDENTIALS:: " + usr + " " + pass);
                }
                _userSecret = usr;
                _userPass = pass;

                //  close base session
                if(_baseSession != null)
                {
                    if(debug)
                    {
                        Debug.Log("CLOSE current base session due to credentials change");
                    }
                    _baseSession.Close();
                    _baseSession = null;
                    if(sessions.ContainsKey(DEFAULT_SESSION_ID)) sessions.Remove(DEFAULT_SESSION_ID);
                }
            }
        }

        protected bool hasCredentials()
        {
            return !string.IsNullOrEmpty(_userSecret) 
                && !string.IsNullOrEmpty(_userPass);
        }


        /// @brief
        /// ask server if user can login with given credentials
        ///
        public IServerRequestHandler<Messages.SC_IdentityResponse> CheckIdentity( string uri, string identifier="", string context="",
                                                                                  IServerSession session=null, 
                                                                                  ServerRequestCallback<Messages.SC_IdentityResponse> callback=null,
                                                                                  ServerRequestErrorCallback errorHandler=null)
        {
            if(debug)
            {
                Debug.Log("Identity Check:: {" + identifier + "} context={" + context + "}");
            }
            session = session != null ? session : baseSession;

            ServerRequest request;
            if(!string.IsNullOrEmpty(identifier) || !string.IsNullOrEmpty(context))
            {   
                var param = new Messages.CS_IdentityRequest();
                if(identifier.Contains('@'))
                {
                    param.mail = identifier;
                }
                else
                {
                    param.name = identifier;
                }
                param.hash = context;
                request = baseSession.GetRequestBuilder(uri, RESTMethod.POST)
                                            .SetContentHeader_Json()
                                            .SetJsonData<Messages.CS_IdentityRequest>(param)
                                            .Build();
            }
            else
            {
                request = baseSession.GetRequestBuilder(uri, RESTMethod.POST).Build();
            }
            
            request.system_flag = true;
            return session.SendJsonRequest<Messages.SC_IdentityResponse>(request, callback, errorHandler);            
        }


        /// @brief
        /// login as a new user
        ///
        public virtual IServerRequestHandler<Messages.SC_ServerAuthResponse> UserLogin( string name, string pass,
                                                                                IServerSession session=null,
                                                                                ServerRequestCallback<Messages.SC_ServerAuthResponse> callback=null,
                                                                                ServerRequestErrorCallback errorHandler=null)
        {
            session = session != null ? session : baseSession;
            //Debug.Log("USER LOGIN " + name + "  " + pass + " " + (session.GetType()));
            var param = new Messages.CS_ServerAuthRequest(name, pass);
            var request = baseSession.GetRequestBuilder(AuthenticatedServerSession.AUTH_URL, RESTMethod.POST)
                            .SetJsonData<Messages.CS_ServerAuthRequest>(param)
                            .Build();
            request.system_flag = true;
            return session.SendJsonRequest<Messages.SC_ServerAuthResponse>(request, (h)=>  {
                
                //  setup new authenticated session in response
                if(!(_baseSession is AuthenticatedServerSession) || _userPass != pass || _userSecret != name)
                {
                    if(debug)
                    {
                        Debug.Log("ClientBase:: base session credentials on UserLogin: " + name + " / " + pass);
                    }
                    _userPass = pass;
                    _userSecret = name;
                    if(_baseSession != null)
                    {
                        _baseSession.Close();
                    }
                    //  manually create auth session & set token
                    _baseSession = createBaseSession((s)=> {
                        var a = s as AuthenticatedServerSession;
                        if(a != null) {
                            a.SetToken(h.response.token);
                        }
                    });
                }
                notifySuccessfulAuthentication(name, pass);
                callback?.Invoke(h);
            }, errorHandler);
        }


        internal void notifySuccessfulAuthentication(string name, string pass, int identity=0)
        {
            authenticated?.Invoke(name, pass, identity);
        }
    }

    //==================================================================================================================


    /// @brief
    /// Generic Client implementation with automatic greeting & status update.
    ///
    public abstract class ClientBase<THandler, TStatusResponse> : ClientBase, IClientInternal
                                where THandler : ServerRequestHandler<TStatusResponse>, new() where TStatusResponse : class
    {

        const int SERVER_CONNECT_INTERVAL = 5;


        //-----------------------------------------------------------------------------------------------


        
        List<ServerRequestHandler> pending = new List<ServerRequestHandler>();
        List<ServerRequestHandler> resolved = new List<ServerRequestHandler>();

        bool _running;
        bool _connected;

        event ServerConnectCallback serverConnected;

        //-----------------------------------------------------------------------------------------------

        /// @brief server base ip string
        public abstract string BaseURL { get; }

        /// @brief server ping interval in seconds
        protected abstract int Heartbeat_IntervalS { get; }


        /// @brief returns wether the client is running & trying to connect
        public override bool isRunning()
        {
            return _running;
        }

        /// @brief returns wether client is connected to webserver
        public override bool isConnected() 
        {
            return _connected;
        }


        


        //-----------------------------------------------------------------------------------------------

        /// @brief
        /// Starts the client and returns isRunning()
        ///
        public override bool Run(params object[] args)
        {
            //    Debug.Log("client.run() running? " + _running + " connected? " + _connected);
            if(!_running || !_connected)
            {
                startInternal();
            }
            if(!_connected || string.IsNullOrEmpty(_userSecret))
            {
                if(args != null && args.Length >= 2 
                && args[0] is string && args[1] is string)
                {
                    SetCredentials(args[0] as string, args[1] as string);
                }
                else
                {
                    SetCredentials("", "");
                }
            }
            return _running;
        }

        /// @brief
        /// Stops the client and returns wether a active connection was interrupted
        ///
        public override bool Stop()
        {
            if(_running)
            {
                bool b = _connected;
                stopInternal();
                return b;
            }
            return false;
        }   

        /// @brief
        /// creates or returns the default session
        ///
        public override void Connect(ServerConnectCallback callback)
        {
            if(!_connected)
            {
                serverConnected += callback;
                Run();
            }
            else
            {
                callback(baseSession);
            }
        }
        
        /// @brief
        /// creates a new session or retrieves an existing one.
        ///
        /// @param id the session id
        ///
        public ServerSession OpenSession(string id)
        {
            return OpenSession<ServerSession>(id);
        }


        /// @brief
        /// creates a new session or retrieves an existing one. only call when isConnected() was checked, otherwise NULL is returned
        ///
        /// @param id the session id
        ///
        public override TSession OpenSession<TSession>(string id, params object[] args) //where TSession : ServerSession, new()
        {
            clearClosedSessions();
            if(sessions.ContainsKey(id))
            {
                UnityEngine.Assertions.Assert.IsTrue((sessions[id] is TSession));
                return sessions[id] as TSession;
            }
            else if(isConnected())
            {
                //  check for session params
                ServerSessionAuthMode authMode = ServerSessionAuthMode.KEEP;
                System.Action<IServerSession> renewDelegate=null;
                bool forceAuthSession = false;
                if(args != null && args.Length > 0 && args[0] is ServerSessionParams)
                {
                    var p = (ServerSessionParams)args[0];
                    authMode = p.auth;
                    renewDelegate = p.renewSessionDelegate;

                    if(authMode == ServerSessionAuthMode.SHARE_TOKEN)
                    {
                        if(_baseSession != null && _baseSession is AuthenticatedServerSession)
                        {
                            var a = _baseSession as AuthenticatedServerSession;
                            p.token = a.GetToken();
                            forceAuthSession = true;
//                            Debug.Log("Client:: Force Auth Session! " + p.token);
                        }
                    }
                    args[0] = p;
                }

                var s = forceAuthSession ? new AuthenticatedServerSession() as TSession : new TSession();
                var si = s as IServerSessionInternal;
                sessions.Add(id, new SessionInfo(si, authMode, renewDelegate));
                si.Init(id, this, args);
                si.onConnect();
                return s;
            }
            else
            {
                Debug.Log(RichText.color("ClientBase:: Failed to open session, not connected", Color.red));
                return null;
            }
        }

        /// @brief
        /// creates and enqueues a new request, returning a handler object
        /// called by ServerSession, but can be used to manually.
        ///
        public override T CreateAndEnqueue<T, R>(ServerSession session, ServerRequest request, ServerRequestCallback<R> callback, ServerRequestErrorCallback error) 
        {
            var handler = new T();
            handler.Set(session, request);
            handler.callback = callback;
            handler.errorAction = error;
            pending.Add(handler);
            session.pending.Add(handler);
            processRequestsNow();

            if(debug)
            {
                Debug.Log("ClientBase::createAndEnqueue<" + typeof(T).Name + ", " + typeof(R).Name + ">"
                            + "\n\turl: " + BaseURL + request.uri
                            + (hasCredentials() ? ("\nusr: " + _userSecret + " pass={" + _userPass + "}") : "")
                            + "\njson: " + request.jsonParams);
            }
            return handler;
        }

        public IServerRequestHandler SendServerPing(System.Action onSuccess, System.Action onError)
        {
            if(!isConnected())
            {
                return null;
            }
            else
            {
                return CreateAndEnqueue<THandler, TStatusResponse>(baseSession, 
                                    FormatServerPingRequest(), 
                                    (handler)=> { onServerPingResponseInternal(handler); onSuccess?.Invoke(); }, 
                                    (handler)=> { onError?.Invoke(); });
            }
        }

        //-----------------------------------------------------------------------------------------------

        protected void connectInternal()
        {
            if(!_connected)
            {   
                serverConnected?.Invoke(baseSession);
                serverConnected = null;
                _connected = true;
                onConnect();

                if(debug)
                {
//                    Debug.Log("ClientBase::connectInternal() session=" + baseSession.id + " heartbeat=" + Heartbeat_IntervalS);
                }

                if(Heartbeat_IntervalS > 0)
                {
                    StopCoroutine("heartbeat");
                    StartCoroutine("heartbeat");
                }
            }
        }
        protected void disconnectInternal()
        {
            if(isConnected())
            {
                onDisconnect();
                _connected = false;
                _processing = false;
                _abortFlag = false;
                _baseSession = null;
                foreach(var s in sessions.Values) 
                {
                    s.session.onDisconnect();
                }
                sessions.Clear();
            }
        }

        protected void startInternal()
        {
            if(debug)
            {
                Debug.Log("try start client<" + this.GetType().Name + ">... \n\taborting connect routine");
            }
            _running = true;
            onClientStart();
            StopCoroutine("tryConnect");
            StartCoroutine("tryConnect");
        }

        protected void stopInternal()
        {
            disconnectInternal();
            onClientStop();
            _running = false;
        }

        /// create custom server status message for greeting & heartbeat
        protected abstract ServerRequest FormatServerPingRequest();

        protected virtual void onServerStatusResponse(TStatusResponse response) {}

        protected virtual void onClientStart() {}
        protected virtual void onClientStop() {}
        protected virtual void onConnect() {}
        protected virtual void onDisconnect() {}
        

        //-----------------------------------------------------------------------------------------------

        //  repeatedly try to send status message
        private IEnumerator tryConnect()
        {
            //Debug.Log("Client:: StartTryConnect.. running? " + _running + " connected? " + (_connected));
            yield return null;
            while(_running && !_connected)
            {
                while(!canConnect())
                {
                    yield return new WaitForSeconds(0.5f);
                }
                
                var pingHandler = baseSession.SendJsonRequest<TStatusResponse>(FormatServerPingRequest(), onServerPingResponseInternal, null);
                while(!pingHandler.isDone)
                {
                    yield return null;
                }
                if(pingHandler.state.hasException())
                {
                    yield return new WaitForSeconds(SERVER_CONNECT_INTERVAL);
                }
            }
        }

        //  send status message at Heartbeat_IntervalS  
        private IEnumerator heartbeat()
        {
            while(_baseSession != null)
            {
                yield return new WaitForSeconds(Heartbeat_IntervalS);
                while(preventHeartbeat)
                {
                    yield return null;
                }

                var pingHandler = CreateAndEnqueue<THandler, TStatusResponse>(_baseSession, FormatServerPingRequest(), null, null);      //  don't call onServerPingResponseInternal() all the time, on connect/reconnect is enough
                while(!pingHandler.isDone)
                {
                    yield return null;
                }
                if(pingHandler.state.hasException())
                {
                    disconnectInternal();
                    if(_running)
                    {
                        StartCoroutine("tryConnect");
                    }
                }
                else
                {
                    onServerStatusResponse(pingHandler.response);
                }
            }
        }

        private void onServerPingResponseInternal(IServerRequestHandler<TStatusResponse> handler)
        {
//            Debug.Log(RichText.emph("Server Ping Response!"));
            if(!isConnected())
            {
                connectInternal();
            }
            onServerStatusResponse(handler.response);
        }

        private void clearClosedSessions()
        {
            if(sessions.Count > 0)
            {
                var toRemove = new List<string>();
                foreach(var key in sessions.Keys) {
                    if(sessions[key].session == null || sessions[key].session.isClosed()) {
                        toRemove.Add(key);
                    }
                }
                foreach(var id in toRemove) {
                    sessions.Remove(id);
                }
            }
        }

        //-----------------------------------------------------------------------------------------------

        protected bool hasPendingRequests()
        {
            if(sessions.Count > 0)
            {
                foreach(var s in sessions.Values)
                {
                    if(s.session.pending.Count > 0) return true;
                }
            }
            return false;
        }

        protected override bool isProcessingRequests()
        {
            return _processing;
        }

        protected override void processRequestsNow()
        {
            if(debug)
            {   
                Debug.Log("ClientBase::processRequests(" + hasPendingRequests() + "/" + sessions.Count + ")");
            }
            if(!_processing && hasPendingRequests())
            {
                StartCoroutine("processRequests");
            }
        }

        protected override void stopProcessRequests()
        {
            if(_processing)
            {
                _abortFlag = true;
                _processing = false;
            }
            
        //    StopCoroutine("processRequests");
        }

        protected override void restartProcessRequests()
        {
            if(_processing)
            {
                _abortFlag = true;
            }
        }

        List<SessionInfo> sessionBuffer = new List<SessionInfo>(2);

        private IEnumerator processRequests()
        {
       //     Debug.Log("REQUESTQUEUE:: START " + hasPendingRequests());
            _processing = true;
            while(hasPendingRequests())
            {
                if(!canConnect())
                {
          //          Debug.Log("REQUESTQUEUE... cannot connect!");
                    yield return new WaitForSeconds(1f);
                }
                else
                {
         //           Debug.Log("requestQueue.onBeforeWaitForEndOfFrame");
                    yield return null;   //  wait 1 frame to let user manipulate requests//handlers further
                    sessionBuffer.Clear();
                    sessionBuffer.AddRange(sessions.Values);

        //            Debug.Log("requestQueue.sessionBuffer: " + sessionBuffer.Count);
                    foreach(var info in sessionBuffer)
                    {
                        if(debug)
                        {
                  //          Debug.Log("ClientBase.processRequests:: <" + name + "> handles session... " + (info.session.id) + " / " + info.session.isOpened());
                        }
                        if(info.session.isOpened())
                        {
                            if(info.session.canSendRequests())
                            {
                                while(info.session.pending.Count > 0)
                                {
                                    var error = false;
                                    var h = info.session.pending[0] as ServerRequestHandler;
                                    info.session.pending.RemoveAt(0);
                                    pending.Remove(h);
                                    resolved.Add(h);

                                    if(debug)
                                    {
                                        Debug.Log("Client:: onbefore send request: " + h.GetType().Name + " " + h.request.url + ">>" + h.request.uri);
                                    }
                                    yield return StartCoroutine(sendRequest(h, ()=> 
                                    {
                                        if(h.state.hasException())
                                        {
                                            //  handle exception
                                            if(debug)
                                            {
                                                 Debug.LogWarning("Client<" + this.GetType().Name + ">:: could not handle request of session=[" + info.session.id + "] error=[" + h.state + "/" + h.errorMessage + "] url=[" + h.request.url + "]\nerror=" + h.errorMessage + "\njsonUpload: " + h.request.jsonParams);
                                            }
                                            error = true;
                                        }
                                    }));

                                    if(error)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                //  handle system-flagged requests (ie. authorization)
                                var query = info.session.pending.Where(x=> x.request.system_flag).ToList();
             //                   Debug.Log("session cannot send requests... --> System Queue? " + query.Count());
                                while(query.Count > 0)
                                {
                                    var error = false;
                                    var h = query[0] as ServerRequestHandler;
                                    info.session.pending.Remove(h);
                                    query.RemoveAt(0);
                                    pending.Remove(h);
                                    resolved.Add(h);

                                    if(debug)
                                    {
                                        Debug.Log("client onbefore send system request: " + h.GetType().Name + " " + h.request.url + ">>" + h.request.uri + "\nHASH=" + Utility.Misc.FormatHash(h));
                                    }
                                    yield return StartCoroutine(sendRequest(h, ()=> 
                                    {
                                        if(h.state.hasException())
                                        {
                                            //  handle exception
                                            if(debug)
                                            {
                                                 Debug.LogWarning("Client<" + this.GetType().Name + "> could not handle request of session=[" + info.session.id + "(" + info.GetType().Name + ")]"
                                                                + "\nerror=[" + h.state + " / " + h.errorMessage + "]\nurl=[" + h.urequest.url + "]");
                                            }
                                            error = true;
                                        }
                                    }));

                                    if(error)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("REQUESTQUEUE:: Session no Open");
                        }

                        if(_abortFlag)
                        {
                            _abortFlag = false;
                            break;
                        }
                    }
                    if(!_processing)
                    {
                        break;
                    }
                }
            }
  //          Debug.Log("REQUESTQUEUE:: END");
            _processing = false;
        }
        protected bool _processing { get; private set; } = false;
        private bool _abortFlag = false;

        private IEnumerator sendRequest(IServerRequestHandler handler, System.Action whendone)
        {
            var hh = handler as ServerRequestHandler;
            var url = FormatRequestUrl(handler.request.url); 
            var urequest = hh.FormatWebRequest(url, handler.request.postForm);

            if(debug)
            {
                Debug.Log("ClientBase" + (hasCredentials() ? ("[:" + _userSecret + "]") : "") 
                        + "::sendRequest() url=[" + RichText.emph(url) + "] -> " + handler.session.GetType().Name 
                        + "\njson=" + handler.request.jsonParams);
            }
            yield return StartCoroutine(hh.Run(urequest));
            whendone();
        }


        //-----------------------------------------------------------------------------------------------

        //  util

        protected virtual string FormatRequestUrl(string url)
        {
            if(!url.EndsWith("/")) {
                url += "/";
            }
            if(!url.StartsWith(BaseURL))
            {
                if(url.StartsWith("/")) {
                    url = url.Substring(1, url.Length-1);
                }
                url = BaseURL + url;
            }
            return url;
        }

        protected static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

    

        //-----------------------------------------------------------------------------------------------

        //  Request posting

        /*protected void PostRequest<P, R>(string url, P parameters, System.Action<R> doneAction, System.Action<UnityWebRequest> errorAction)
        {
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            string parameterJson = JsonConvert.SerializeObject(parameters);
            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(parameterJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            //request.SetRequestHeader("Authorization", "Basic " + Base64Encode(USER_NAME + ":" + PASS));
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            Debug.Log("POST request:\njson: " + parameterJson + "\n" + request.GetRequestHeader("Content-Type"));

            StartCoroutine(sendRequest(request, (r) =>
            {
                if (request.isHttpError)
                {
                    errorAction(request);
                }
                else if (request.isNetworkError)
                {
                    errorAction(request);
                }
                else
                {
                    R ret = JsonUtility.FromJson<R>(request.downloadHandler.text);
                    doneAction(ret);
                }
            }));
        }



        private IEnumerator sendRequest(UnityWebRequest request, System.Action<UnityWebRequest> doneAction)
        {
            yield return request.SendWebRequest();
            doneAction(request);

        }*/
        
    }


}