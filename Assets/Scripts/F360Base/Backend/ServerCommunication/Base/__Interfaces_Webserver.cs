using System;
using System.Collections.Generic;
using UnityEngine.Networking;


using F360.Backend.Messages;
using F360.Backend.Synch;

namespace F360.Backend
{


    public delegate void ServerConnectCallback(ServerSession connection);

    public delegate void ServerRequestCallback<R>(IServerRequestHandler<R> handler) where R : class;

    public delegate void ServerRequestErrorCallback(IServerRequestHandler handler);


    //-----------------------------------------------------------------------------------------------

    public interface IServerRequestHandler
    {
        ServerSession session { get; } 
        ServerRequest request { get; }
        UnityWebRequest urequest { get; }

        bool isDone { get; } 
        RequestState state { get; } 
        ServerRequestErrorCallback errorAction { get; }
        string errorMessage { get; }
        int errorCode { get; }

        void PrepareForResend();
        object GetResponse();
    }

    public interface IServerRequestHandler<R> : IServerRequestHandler where R : class
    {
        R response { get; }
    }

    //-----------------------------------------------------------------------------------------------

    //  client base


    /// @brief
    /// The public client interface accessable by higher-level functionality.
    ///
    /// @brief
    /// All client services running in application implement this and are 
    /// subscribed & accessed via F360Backend class.
    ///
    public interface IClient
    {

        /// @brief
        /// returns wether client is active and connect / trying to connect
        ///
        bool isRunning();

        /// @brief
        /// returns wether conditions for connection are fulfilled (internet, license state..)
        ///
        bool canConnect();

        /// @brief
        /// returns wether a connection was established
        ///
        bool isConnected();

        /// @brief
        /// starts the client and returns isRunning()
        ///
        bool Run(params object[] args);

        /// @brief
        /// stops the client and returns wether an active connection was interrupted
        ///
        bool Stop();

        void SetCredentials(string usr, string pass);

        /// @brief
        /// starts the client and creates or returns the default session
        ///
        void Connect(ServerConnectCallback callback);

        /// @brief
        /// creates a new session or retrieves an existing one.
        ///
        /// @param id the session id
        ///
        TSession OpenSession<TSession>(string id, params object[] args) where TSession : class, IServerSession, new();
        
    }

    //-----------------------------------------------------------------------------------------------


    /// @brief
    /// base interface for an individual session / communication channel to server. 
    ///
    public interface IServerSession
    {
        string id { get; }

        IClient server { get; }

        bool isOpened();
        bool canSendRequests();
        bool isClosed();

        /// @returns a helper object to construct a webrequest
        ///
        RequestBuilder GetRequestBuilder(string url, RESTMethod method);

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
        JsonRequestHandler<R> SendJsonRequest<R>(ServerRequest request, 
                                                        ServerRequestCallback<R> callback=null, 
                                                        ServerRequestErrorCallback errorHandler=null) 
                                                        where R : class;
        /// @brief
        /// format and enqueue a webrequest expecting plain text in return.
        ///
        /// @param request      formatted request data: make sure both url AND uri are set for successful download
        /// @param callback     optional success callback function (can also be managed via the returned request handler)
        /// @param errorHandler optional error callback (can also be managed via the returned request handler)
        ///
        /// @returns the text requesthandler object (to keep track of request state, i.e. when used in coroutines)
        ///
        TextRequestHandler SendTextRequest(ServerRequest request, 
                                                  ServerRequestCallback<string> callback=null, 
                                                  ServerRequestErrorCallback errorHandler=null);
        /// @brief
        /// format and enqueue a file download request
        ///
        /// @param request      formatted request data: make sure both url AND uri are set for successful download
        /// @param targetFolder absolute path where the file should be stored
        /// @param callback     optional success callback function (can also be managed via the returned request handler)
        /// @param errorHandler optional error callback (can also be managed via the returned request handler)
        ///
        /// @returns a filedownload requesthandler object (to keep track of request state, i.e. when used in coroutines)
        ///
        ///
        FileDownloadHandler SendFileDownloadRequest(ServerRequest request, string targetFolder, int timeout=120,
                                                           ServerRequestCallback<SC_FileDownloadResponse> callback=null, 
                                                           ServerRequestErrorCallback errorHandler=null);
    }

    //-----------------------------------------------------------------------------------------------

    //  internal interfaces

    namespace Core
    {

        /// @brief
        /// internal interface of clients, accessed by ServerSession to generate new requests.
        ///
        internal interface IClientInternal : IClient
        {
            void ProcessRequestsNow();
            
            /// @brief
            /// enqueues a request to be send to the server. create requests via the session instead of directly calling this.
            ///
            THandler CreateAndEnqueue<THandler, R>(ServerSession session, ServerRequest request, ServerRequestCallback<R> callback, ServerRequestErrorCallback error) 
                                where THandler : Core.ServerRequestHandler<R>, new() where R : class;
        }

        /// @brief
        /// internal interface for sessions, accessed by ClientBase
        ///
        internal interface IServerSessionInternal : IServerSession
        {
            List<IServerRequestHandler> pending { get; }

            void Init(string id, IClient c, params object[] args);
            void Close();
            void onConnect();
            void onDisconnect();
        }
    }
    

    //-----------------------------------------------------------------------------------------------


    /// @brief
    /// loading process callback
    ///
    public interface ILoadProcessHandler
    {
        void OnBegin(string context);
        void SetProgress(float t);   ///< set normalized progress
        void OnFinish();
        void OnError(string message);
    }

    
}