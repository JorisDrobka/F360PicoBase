using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using F360.Backend.Core;
using F360.Backend.Messages;

namespace F360.Backend
{

    public enum ServerSessionAuthMode
    {
        KEEP,           //  keep login credentials as session was initialized
        SHARE_TOKEN     //  synchronize with base session credentials (shared token)
    }


    /// @brief
    /// can be used as parameter on starting a server / opening an individual session
    ///
    public struct ServerSessionParams
    {
        public ServerSessionAuthMode auth;
        public string usr;
        public string pass;
        public string token;
        public System.Action<IServerSession> renewSessionDelegate;
    }





    /// @brief
    /// server session that automatically requests & holds a authentication token, 
    /// automatically closing when token expires.
    ///
    public class AuthenticatedServerSession : ServerSession
    {
        public const string AUTH_URL = "api/expire-token/login/";
        public const string AUTH_TOKEN_KEY = "Authorization";
        public const string AUTH_TOKEN_PREFIX = "Token ";

        const int VALID_TIME = 60 * 60;     //  in seconds
        const int DEFAULT_LATENCY = 1;      //  in seconds 

        bool isRequestingToken = false;
        string user = "";
        string pass = "";
        string token = "";
        System.DateTime expires = System.DateTime.MaxValue;


        public override bool canSendRequests()
        {
            return !string.IsNullOrEmpty(token) && TimeUtil.ServerTime < expires;
        }
        public override bool isClosed()
        {
            return base.isClosed() || TimeUtil.ServerTime > expires;
        }

        public override JsonRequestHandler<R> SendJsonRequest<R>(ServerRequest request, 
                                                                 ServerRequestCallback<R> callback=null, 
                                                                 ServerRequestErrorCallback errorHandler=null) 
        {
            addTokenToRequest(request);
            return base.SendJsonRequest<R>(request, callback, (e)=> {
                if(checkTokenExpired(e)) {
                    handleOnExpiredToken(e);
                } 
                else errorHandler?.Invoke(e);
            });
        }

        public override TextRequestHandler SendTextRequest(ServerRequest request, 
                                                           ServerRequestCallback<string> callback=null, 
                                                           ServerRequestErrorCallback errorHandler=null)
        {
            addTokenToRequest(request);
            return base.SendTextRequest(request, callback, (e)=> {
                if(checkTokenExpired(e)) {
                    handleOnExpiredToken(e);
                } 
                else errorHandler?.Invoke(e);
            });
        }
        
        public override FileDownloadHandler SendFileDownloadRequest(ServerRequest request, string targetFolder, int timeout=120,
                                                                    ServerRequestCallback<SC_FileDownloadResponse> callback = null, 
                                                                    ServerRequestErrorCallback errorHandler = null)
        {
            addTokenToRequest(request);
            return base.SendFileDownloadRequest(request, targetFolder, timeout, callback, (e)=> {
                if(checkTokenExpired(e)) {
                    handleOnExpiredToken(e);
                } 
                else errorHandler?.Invoke(e);
            });
        }

        


        protected override void onInit(object[] args)
        {
            if(args.Length == 1 && args[0] is ServerSessionParams)
            {
                //  from server session params
                var p = (ServerSessionParams) args[0];
                this.user = p.usr;
                this.pass = p.pass;
                this.token = p.token;
            }
            else if(args.Length >= 2
                && args[0] is string
                && args[1] is string)
            {
                //  from plain strings
                this.user = args[0] as string;
                this.pass = args[1] as string;
            }
            else
            {
                Debug.LogError("AuthenticatedSererSession:: malformatted init arguments!");
            }
            base.onInit(args);
        }

        protected override void onConnect()
        {
            base.onConnect();
            if(string.IsNullOrEmpty(token))
            {
                authenticate(user, pass);
            }
        }

        /// @brief
        /// manually set token
        ///
        internal void SetToken(string authToken)
        {
            this.token = authToken;
        }
        internal string GetToken()
        {
            return this.token;
        }

        void authenticate(string usr, string pass)
        {
            var auth = new CS_ServerAuthRequest(usr, pass);
            var request = GetRequestBuilder(AUTH_URL, RESTMethod.POST)
                            .SetJsonData<CS_ServerAuthRequest>(auth)
                            .Build();
            request.system_flag = true;
            isRequestingToken = true;

  //          Debug.Log("ServerSession::Authenticate Request [" + usr + " : " + pass + "]\nauth json: " + request.jsonParams);
            base.SendJsonRequest<SC_ServerAuthResponse>(request, (handler)=> {
  //              Debug.Log("ServerSession::Authenticate Response! " + handler.state);
                if(handler.state == RequestState.Success)
                {
                    //  Authentication success
                    token = handler.response.token;
                    expires = TimeUtil.ServerTime.Add(
                        new System.TimeSpan(0, 0, VALID_TIME - DEFAULT_LATENCY)
                    );
//                    Debug.Log("TOKEN:: " + token + " " + expires);

                    for(int i = 0; i < pending.Count; i++)
                    {
                        addTokenToRequest(pending[i].request);
                    }
                    var cs = serverInternal as ClientBase;
                    if(cs != null)
                    {
                        cs.notifySuccessfulAuthentication(user, pass); 
                    }
                    serverInternal.ProcessRequestsNow();
                }
                else
                {
                    //  handle auth error
                    foreach(var p in pending)
                    {
                        p.errorAction?.Invoke(p);
                    }
                    this.Close();
                }
                isRequestingToken = false;
            });
        }

        bool addTokenToRequest(ServerRequest r)
        {
            if(string.IsNullOrEmpty(token))
            {
                Debug.LogError("AuthenticatedServerSession:: no token for request: " + r.ToString());
                return false;
            }
            else
            {
//                Debug.Log("Add token to request " + r + "\n\n" + token);
                if(!r.headers.ContainsKey(AUTH_TOKEN_KEY))
                {
                    r.headers.Add(AUTH_TOKEN_KEY, AUTH_TOKEN_PREFIX + token);
                }
                else
                {
                    r.headers[AUTH_TOKEN_KEY] = AUTH_TOKEN_PREFIX + token;
                }
                return true;
            }
        }


        bool checkTokenExpired(IServerRequestHandler handler)
        {
            if(handler.errorCode == ServerUtil.CODE_UNAUTHORIZED || handler.errorCode == ServerUtil.CODE_FORBIDDEN)
            {
                if(!string.IsNullOrEmpty(token))
                {
                    Debug.Log("AuthorizedSession:: token expired!");
                    return true;
                }
            }
            return false;
        }

        void handleOnExpiredToken(IServerRequestHandler handler)
        {   
            handler.PrepareForResend();
            handler.request.headers.Remove(AUTH_TOKEN_KEY);

            if(!pending.Contains(handler)) {
                pending.Add(handler);
            }
            if(!isRequestingToken)
            {
                token = "";
                if(!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                {
                    authenticate(user, pass);
                }
                else
                {
                    Debug.LogError("Authenticated session has lost token, but cannot regain it!\nsession closed.");
                    foreach(var request in pending)
                    {
                        request.errorAction?.Invoke(request);
                    }
                    Close();
                }
            }
        }
    }


    //-----------------------------------------------------------------------------------------------


    /// @brief
    /// default server session implementation for RESTful communication.
    /// main interface for higher-level services.
    ///
    public class ServerSession : Core.ServerSessionBase
    {
        bool _initialized;
        bool _opened;
        bool _closed;
        int _max_logged_requests = 10;

        //-----------------------------------------------------------------------------------------------

        //  interface

        public List<IServerRequestHandler> closed { get; private set; }

        public int MaxCachedRequests 
        { 
            get { return _max_logged_requests; } 
            set { _max_logged_requests = Mathf.Max(0, value); } 
        }

        //-----------------------------------------------------------------------------------------------

        /// @brief
        /// called once when connection is created
        ///
        protected override void onInit(object[] args)
        {
            if(closed == null) closed = new List<IServerRequestHandler>(MaxCachedRequests);
            else closed.Clear();
        }
    
        /// @brief
        /// returns wether session was opened once.
        ///
        public override bool isOpened()
        {
            return _opened;
        }

        public override bool canSendRequests()
        {
            return _opened;
        }

        /// @brief
        /// returns wether session was closed or false if session wasn't opened yet
        ///
        public override bool isClosed()
        {
            return _closed;
        }

        //-----------------------------------------------------------------------------------------------

        //  request handling
        
        /// @brief
        /// format and enqueue a webrequest expecting json data in return
        ///
        /// @param R            jsondata response type
        /// @param request      formatted request data: make sure both url AND uri are set for successful download
        /// @param callback     optional success callback function (can also be managed via the returned request handler)
        /// @param errorHandler optional error callback (can also be managed via the returned request handler)
        ///
        /// @returns the requesthandler object for given type (to keep track of request state, i.e. when used in coroutines)
        ///
        public override JsonRequestHandler<R> SendJsonRequest<R>(ServerRequest request, 
                                                                 ServerRequestCallback<R> callback=null, 
                                                                 ServerRequestErrorCallback errorHandler=null) 
        {
            return SendRequest<JsonRequestHandler<R>, R>(request, callback, errorHandler);
        }

        /// @brief
        /// format and enqueue a webrequest expecting plain text in return.
        ///
        /// @param request      formatted request data: make sure both url AND uri are set for successful download
        /// @param callback     optional success callback function (can also be managed via the returned request handler)
        /// @param errorHandler optional error callback (can also be managed via the returned request handler)
        ///
        /// @returns the text requesthandler object (to keep track of request state, i.e. when used in coroutines)
        ///
        public override TextRequestHandler SendTextRequest(ServerRequest request, 
                                                           ServerRequestCallback<string> callback=null, 
                                                           ServerRequestErrorCallback errorHandler=null)
        {
            return SendRequest<TextRequestHandler, string>(request, callback, errorHandler);
        }

        /// @brief
        /// format and enqueue a file download request
        ///
        /// @param request      formatted request data: make sure both url AND uri are set for successful download (if using RequestBuilder, make sure to call SetURI())
        /// @param targetFolder absolute path where the file should be stored
        /// @param callback     optional success callback function (can also be managed via the returned request handler)
        /// @param errorHandler optional error callback (can also be managed via the returned request handler)
        ///
        /// @returns a filedownload requesthandler object (to keep track of request state, i.e. when used in coroutines)
        ///
        ///
        public override FileDownloadHandler SendFileDownloadRequest(ServerRequest request, string targetFolder, int timeout=120,
                                                                    ServerRequestCallback<SC_FileDownloadResponse> callback=null, 
                                                                    ServerRequestErrorCallback errorHandler=null)
        {
            UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(request.uri), "ServerSession:: no uri set for filedownload request!");

            var filename = request.GetURIFileName();
    //        Debug.Log("FileDownloadRequest::: [" + filename + "]");
            var handler = SendRequest<FileDownloadHandler, SC_FileDownloadResponse>(request, callback, errorHandler);
            handler.SetTimeout(timeout);
            handler.SetDestinationPath(targetFolder, filename);
            return handler;
        }


        /// @brief
        /// format and enqueue a generic webrequest
        ///
        /// @param THandler     used requesthandler type (must be of type: ServerRequestHandler<R>)
        /// @param R            response type
        /// @param request      formatted request data: make sure both url AND uri are set for successful download
        /// @param callback     optional success callback function (can also be managed via the returned request handler)
        /// @param errorHandler optional error callback (can also be managed via the returned request handler)
        ///
        /// @returns the requesthandler object for given type (to keep track of request state, i.e. when used in coroutines)
        ///
        public THandler SendRequest<THandler, R>(ServerRequest request, 
                                                 ServerRequestCallback<R> callback=null, 
                                                 ServerRequestErrorCallback errorHandler=null) 
                                                 where THandler : ServerRequestHandler<R>, new() where R : class
        {
            if(server != null)
            {
                return serverInternal.CreateAndEnqueue<THandler, R>(this, request, 
                (h)=> 
                {
                    onResponse(h);
                    callback?.Invoke(h);
                }, 
                (h)=> {
                    onError(h);
                    errorHandler?.Invoke(h);
                });
            }
            return null;
        }


        //-----------------------------------------------------------------------------------------------

        //  internal interface

        protected override void onConnect()
        {
            _opened = true;
        }

        protected override void onDisconnect()
        {
            _closed = true;
        }

        //-----------------------------------------------------------------------------------------------


        protected virtual void onResponse(IServerRequestHandler handler)
        {
            //  received server response
            addToClosedRequests(handler);
        }
        
        protected virtual void onError(IServerRequestHandler handler)
        {
            //  error handling
            addToClosedRequests(handler);
        }



        private void addToClosedRequests(IServerRequestHandler handler)
        {
            if(closed.Count > MaxCachedRequests)
            {
                closed.RemoveAt(0);
            }
            closed.Add(handler);
        }
    }





    //==================================================================================================================




    namespace Core
    {

        /// @brief
        /// base class for all server session implementations. Note that IClient interface must be implemented manually.
        ///
        ///
        public abstract class ServerSessionBase : IServerSessionInternal
        {
            public string id { get; private set; }
            public IClient server { get; private set; }
            internal IClientInternal serverInternal { get; private set; }

            public List<IServerRequestHandler> pending { get; private set; } = new List<IServerRequestHandler>();

            public abstract bool isOpened();
            public abstract bool canSendRequests();
            public abstract bool isClosed();

            public void Close()
            {
                if(!isClosed())
                {
                    onForcedClose();
                    onDisconnect();
                }
            }

            /// @returns a helper object to construct a webrequest
            ///
            public RequestBuilder GetRequestBuilder(string url, RESTMethod method)
            {
                if(builder==null) builder = new RequestBuilder();
                else builder.Flush();
                builder.SetUrl(url)
                        .SetMethod(method);
                return builder;
            }

            public abstract JsonRequestHandler<R> SendJsonRequest<R>(ServerRequest request, 
                                                                    ServerRequestCallback<R> callback=null, 
                                                                    ServerRequestErrorCallback errorHandler=null) 
                                                                    where R : class;

            public abstract TextRequestHandler SendTextRequest(ServerRequest request, 
                                                            ServerRequestCallback<string> callback=null, 
                                                            ServerRequestErrorCallback errorHandler=null);

            public abstract FileDownloadHandler SendFileDownloadRequest(ServerRequest request, string targetFolder, int timeout=120,
                                                                        ServerRequestCallback<SC_FileDownloadResponse> callback=null, 
                                                                        ServerRequestErrorCallback errorHandler=null);

            protected abstract void onInit(object[] args);
            protected abstract void onConnect();
            protected abstract void onDisconnect();
            protected virtual void onForcedClose() {}

            void IServerSessionInternal.Init(string id, IClient c, params object[] args)
            {
                if(!_initialized)
                {
                    this.id = id;
                    this.server = c;
                    this.serverInternal = c as IClientInternal;
                    if(pending == null) pending = new List<IServerRequestHandler>();
                    else pending.Clear();
                    onInit(args);
                    _initialized = true;
                }
            }
            void IServerSessionInternal.onConnect() { onConnect(); }
            void IServerSessionInternal.onDisconnect() { onDisconnect(); }

            bool _initialized = false;
            RequestBuilder builder = null;
        }

    }

}
