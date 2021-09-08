using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;

using F360.Backend.Messages;


namespace F360.Backend
{


    /// @brief
    /// generic web request info structure
    ///
    public class ServerRequest
    {
        const string URI_META_SEPARATOR = ServerUtil.URI_META_SEPARATOR;

        public readonly string url;
        public readonly RESTMethod method;
        public readonly string uri;
        public string mime = MimeType.JSON;
        public string jsonParams = "";
        public byte[] byteParams;           
        public Dictionary<string, string> headers;
        public WWWForm postForm;
        public bool system_flag;


        public ServerRequest(string url, RESTMethod method, string uri="")
        {
            this.url = url;
            this.uri = uri;
            this.method = method;
            this.headers = new Dictionary<string, string>();
        }

        public string GetURIFileName()
        {
            if(!string.IsNullOrEmpty(uri))
            {
                string _uri = uri;
                int sep = uri.LastIndexOf(URI_META_SEPARATOR);
                if(sep != -1)
                {
                    sep += URI_META_SEPARATOR.Length;
                    _uri = uri.Substring(sep, uri.Length-sep);
                }
                int id = _uri.LastIndexOf("/");
                if(id != -1)
                {
                    Debug.Log("GetURIFilename [" + _uri + "]   /?" + id);
                    _uri = _uri.Substring(id+1, _uri.Length-id-1);
                }
                
                /// HOTFIX mp4 boto download
                
                id = _uri.IndexOf(".mp4");
                if(id != -1)
                {
                    _uri = _uri.Substring(0, id+".mp4".Length);
                }
                return _uri;
            }
            return "";
        }

        public override string ToString()
        {
            var b = new System.Text.StringBuilder("[ServerRequest]");
            b.Append(" @" + url);
            if(system_flag) b.Append(" [SYS]");
            b.Append("\n\tmethod: " + method);
            if(!string.IsNullOrEmpty(uri))
                b.Append("\n\turi: " + uri);
            if(!string.IsNullOrEmpty(mime))
                b.Append("\n\tmime: " + mime);
            if(!string.IsNullOrEmpty(jsonParams))
                b.Append("\n\tjson: " + jsonParams);
            return b.ToString();
        } 
    }


    //-----------------------------------------------------------------------------------------------
    

    //  json handler

    /// @brief
    /// receive json data
    ///
    public class JsonRequestHandler<R> : Core.ServerRequestHandler<R> where R : class
    {
        public override bool Validate()
        {
            return !string.IsNullOrEmpty(request.jsonParams);
        }

        protected override RequestState UnpackResponse(UnityWebRequest urequest)
        {
  //          if(urequest != null && urequest.downloadHandler != null) Debug.Log("JsonHandler<" + typeof(R) +">.Unpack: " + urequest.downloadHandler.text);
            response = Newtonsoft.Json.JsonConvert.DeserializeObject<R>(urequest.downloadHandler.text);
            if(response != null)
            {
                return RequestState.Success;
            }
            else
            {
                return RequestState.MalformatedResponseException;
            }
        }
    }

    //-----------------------------------------------------------------------------------------------


    //  text handler


    /// @brief
    /// receive plain text
    ///
    public class TextRequestHandler : Core.ServerRequestHandler<string>
    {
        public override bool Validate()
        {
            return true;
        }

        protected override RequestState UnpackResponse(UnityWebRequest urequest)
        {
            response = urequest.downloadHandler.text;
            return RequestState.Success;
        }
    }


    //-----------------------------------------------------------------------------------------------

    //  file download


    /// @brief
    /// handler for downloading large files
    ///
    public class FileDownloadHandler : Core.ServerRequestHandler<SC_FileDownloadResponse>
    {
        string file_name = "";
        string path_to_directory = "";
        string dest_path = "";

        int timeoutOverride = -1;

        public void SetTimeout(int timeout=120)
        {
            timeoutOverride = timeout;
        }

        public bool SetDestinationPath(string path, string filename)
        {
            this.path_to_directory = path;
            if(!path.EndsWith("/"))
            {
                this.path_to_directory += "/";
            }
            this.file_name = filename;
            this.dest_path = Path.Combine(path_to_directory, file_name);
            
//            Debug.Log("......FileDownloadHandler:: path=[" + path + "]\ndirectory=[" + path_to_directory + "] exists? " + Directory.Exists(path_to_directory) + "\nfilename=[" + file_name + "]");
            return Directory.Exists(path_to_directory);
        }

        public override bool Validate()
        {
            return Directory.Exists(path_to_directory);
        }

        protected override RequestState UnpackResponse(UnityWebRequest urequest)
        {
            var file = ServerFileInfo.ParseDownloadURI(request.uri);
            Debug.Log("Unpack response:: [" + request.uri + "] file=" + (file != null) + "\ndownloadBytes: " + urequest.downloadedBytes);
            if(file == null)
            {
                return RequestState.FileNotFoundException;
            }
            else
            {
                file.download_path = dest_path;
                response = new SC_FileDownloadResponse
                {
                    fileInfo = file,
                    fileName = file_name,
                    targetFolder  = path_to_directory,
                    filePath = dest_path,
                    zipped = file_name.EndsWith("zip")
                };
                return RequestState.Success;
            }            
        }

        protected override int GetTimeout()
        {
            return timeoutOverride > 0 ? timeoutOverride : 120;
        }

        protected override DownloadHandler CreateDownloadHandler()
        {
  //          Debug.Log("==========> Created Download handler url=[" + request.url + "] dest_path=[" + dest_path + "]");
            try {
                return new DownloadHandlerFile(dest_path);
            }
            catch(System.Exception ex) {
                Debug.LogError(ex);
                return null;
            }
        }

        protected override float GetDownloadProgress(UnityWebRequest urequest, UnityWebRequestAsyncOperation op)
        {
            return urequest.downloadProgress;
        }
    }



    //==================================================================================================================


    namespace Core
    {

        /// @brief
        /// Request handler for better handling of expected responses.
        ///
        public abstract class ServerRequestHandler<R> : ServerRequestHandler, IServerRequestHandler<R> where R : class
        {
            public ServerRequestCallback<R> callback { get; set; }
            public R response { get; protected set; }

            public override void PrepareForResend() 
            {
                base.PrepareForResend();
                response = null;
            }
            

            internal override IEnumerator Run(UnityWebRequest urequest)
            {
                isDone = false;
                
                var operation = urequest.SendWebRequest();
                while(!operation.isDone)
                {
                    this.progress = GetDownloadProgress(urequest, operation);
                    Debug.Log("Webrequest [" + request.url + "] progress -> " + this.progress);
                    yield return new WaitForSeconds(1f);
                }
                
                if (urequest.isHttpError)
                {
                    state = RequestState.RequestFormatException;
                    errorMessage = urequest.error;
                    errorCode = ServerUtil.GetErrorCode(errorMessage);
                    Debug.LogWarning("REQUEST EXCEPTION: code[" + errorCode + "]:: " + urequest.error + "\naddress: " + urequest.url + "\njson: " + request.jsonParams);
                    errorAction?.Invoke(this);
                }
                else if (urequest.isNetworkError)
                {
                    state = RequestState.TimeoutException;
                    errorAction?.Invoke(this);
                }
                else
                {
                    state = UnpackResponse(urequest);
                    if(state.hasException())
                    {
                        errorAction?.Invoke(this);
                    }
                    else
                    {
                        callback?.Invoke(this);
                    }
                }
                isDone = true;
            }

            protected virtual float GetDownloadProgress(UnityWebRequest urequest, UnityWebRequestAsyncOperation op)
            {
                return op.progress;
            }

            protected abstract RequestState UnpackResponse(UnityWebRequest urequest);

            public override object GetResponse()
            {
                return response;
            }
        }

        //-----------------------------------------------------------------------------------------------


        /// @brief
        /// basic handler for keeping track of an issued server request.
        /// can be used in coroutine handlers as alternative to direct callbacks.
        ///
        public abstract class ServerRequestHandler : IServerRequestHandler
        {
            
            public ServerSession session { get; private set; }

            public ServerRequest request { get; private set; }

            public UnityWebRequest urequest { get; protected set; }

            public bool isDone { get; protected set; }

            public RequestState state { get; protected set; }

            public float progress { get; protected set; }       //  float range 0..1

            public string errorMessage { get; protected set; }
            public int errorCode { get; protected set; }


            public ServerRequestErrorCallback errorAction { get; set; }
            
            internal void Set(ServerSession session, ServerRequest r)
            {
                this.session = session;
                this.request = r;
                progress = 0;

                Init();
            }

            protected virtual void Init() {}

            public virtual void PrepareForResend()
            {
                errorCode = 0;
                errorMessage = "";
                progress = 0f;
                state = RequestState.Pending;
                isDone = false;
                urequest = null;
            }

            internal virtual UnityWebRequest FormatWebRequest(string url, WWWForm postForm=null)
            {
                return CreateWebRequest(url, postForm);
            }   

            internal abstract IEnumerator Run(UnityWebRequest urequest);

            public abstract bool Validate();

            public abstract object GetResponse();

            protected UnityWebRequest CreateWebRequest(string url, WWWForm postForm)
            {
                if(postForm != null && request.method == RESTMethod.POST)
                {
                    //  create webrequest via formdata
                    if(request.headers != null)
                    {
                        foreach(var header in request.headers)
                        {
                            postForm.headers.Add(header.Key, header.Value);
                        }
                    }
                    urequest = UnityWebRequest.Post(url, postForm);
                }
                else
                {
                    //  create generic webrequest
                    urequest = new UnityWebRequest(url, request.method.ToString());                    
                    foreach(var header in request.headers)
                    {
//                        Debug.Log("request header:: " + header.Key + " : " + header.Value);
                        urequest.SetRequestHeader(header.Key, header.Value);
                    }
                }

                //  download handler
                urequest.downloadHandler = CreateDownloadHandler();
          //      urequest.timeout = GetTimeout();
                    
                //  upload handler
                if(request.method == RESTMethod.POST || request.method == RESTMethod.PUT || request.method == RESTMethod.GET)
                {
                    if(!string.IsNullOrEmpty(request.jsonParams))
                    {
                    //    Debug.Log("UPLOAD json params=" + request.jsonParams);
                        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(request.jsonParams);
                        urequest.uploadHandler = CreateUploadHandler(bodyRaw); //new UploadHandlerRaw(bodyRaw);
                    }
                    else if(request.byteParams != null)
                    {
                        var handler = CreateUploadHandler(request.byteParams);
                        handler.contentType = request.mime;
                        urequest.uploadHandler = handler;
                    }
                }
                
                return urequest;
            }

            protected virtual int GetTimeout()
            {
                return 30;
            }

            protected virtual DownloadHandler CreateDownloadHandler() 
            { 
                return new DownloadHandlerBuffer(); 
            }

            protected virtual UploadHandler CreateUploadHandler(byte[] bodyRaw) 
            { 
                var h = new UploadHandlerRaw(bodyRaw);
                h.contentType = MimeType.JSON;
                return h;
            }
        }

    }

}