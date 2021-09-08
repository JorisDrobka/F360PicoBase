using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DeviceBridge.Files
{


    /// @brief
    /// Callback method after loading operation.
    ///
    public delegate void FileLoaderCallback<TObject>(TObject result, FileLoader loader);



    /// @brief
    /// Helper class to easily access files in all storage locations.
    /// 
    public class FileLoader
    {
        ///@cond PRIVATE
        public const string PATH_TOKEN = "/";
        ///@endcond

        public string fullPath { get; protected set; }          ///< Last formatted file path used.
        public string folderPath { get; private set; }          ///< The relative folderpath within the storage location.
        public string fileName { get; private set; }            ///< Name of the file to load.
        public string fileType { get; private set; }            ///< The file extension.
        public StorageLocation location { get; private set; }   ///< The storage location to load from.
        public bool _hiddenFileType { get; private set; }

        /// @brief
        /// Metadata you can read out on the callback.
        ///
        public object context { get; set; }
        

        /// @brief
        /// The successfully loaded object.
        ///
        public object loadedObject { get; protected set; }
        
        /// @brief
        /// State of the loader.
        ///
        public FileAccessState mState { get; set; }

        /// @brief
        /// One-shot event when loading is done, must be set before calling any load method.
        ///
        public event System.Action<FileLoader> finished;


        /// @brief
        /// Cached asynchronous loading operation.
        ///
        protected AsyncFileLoadWorker currentWorker { get; private set; }

    
        //  tmp load state data 
        private bool _loadAsUnityObject;
        private bool _loadAsString;

        protected readonly DeviceStorageBridge bridge;


        public FileLoader(DeviceStorageBridge context)
        {
            this.bridge = context;
            mState = FileAccessState.Idle;
        }        

        //-----------------------------------------------------------------------------------------------------------------

        /// @brief
        /// Get loaded object of the desired type.
        /// @returns if the object is loaded and of type T. 
        ///
        public bool GetResult<T>(out T obj)
        {
            if(loadedObject is T)
            {
                obj = (T)loadedObject;
                return true;
            }
            else 
            {
                obj = default(T);
                return false;
            }
        }

        /// @brief
        /// Resets this loader - automatically called on each load attempt.
        ///
        public void FlushState()
        {
            mState = FileAccessState.Idle;
            folderPath = "";
            fileName = "";
            fileType = "";
            fullPath = "";
            context = null;
            location = StorageLocation.Resources_Folder;
            loadedObject = null;
            _loadAsString = false;
            _loadAsUnityObject = false;
            finished = null;
            currentWorker = null;
        }


        /// @brief
        /// Returns wether the target storage location can be accessed immediately.
        /// @details
        /// Override this when implementing async loading operations.
        /// 
        public virtual bool canLoadImmediate(string folderpath, string filename, string filetype, StorageLocation location)
        {   
            return true;
        }

        /// @details
        /// Override this to implement async loading.
        /// make sure to call workers' SetResult() method at some point or the loader will be blocked.
        ///
        protected virtual void handleAsyncLoad(AsyncFileLoadWorker worker)
        {

        }

        /// @details
        /// Override this to implement custom filetype loading. 
        /// Method must return if filetype can handled and if so, must set loadedObject and mState accordingly.
        ///
        protected virtual bool customGenericLoad<T>(string fullPath, string folderpath, string filename, string filetype, StorageLocation location)
        {
            mState = FileAccessState.Idle;
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------------


        //  path formatting

        public string GetStoragePath(StorageLocation location)
        {
            return bridge.GetStoragePath(location);
        }

        /// @brief
        /// Formats an absolute path to desired location.
        ///
        /// @details
        /// Format a path for specified storage location/method.
        /// Handles Unity Filepaths and takes a guess on how to format storage paths of foreign devices. 
        /// Override this to specify path handling for specific device.
        ///
        /// @param folderpath Path to file relative to app data folder
        /// @param filename Name of the file to load
        /// @param filetype Type extension or empty if hidden filetype
        /// @param location Location of the file in the application logic.
        ///
        public virtual string FormatPath(string folderpath, string filename, string filetype, StorageLocation location)
        {
  //          Debug.Log("@FormatPath:: folderPath=[" + folderpath + "] filename=[" + filename + "] filetype=[" + filetype + "]  LOCATION=[" + location + "]");
            bool hasFileName = !string.IsNullOrEmpty(filename);
            bool hasFileType = !string.IsNullOrEmpty(filetype);

            string path = folderpath;

            //  add filename
            if(hasFileName) 
            {
                string split = path;
                int separator = path.LastIndexOf(PATH_TOKEN);
                if(separator != -1) {
                    split = path.Substring(separator+1);
                }
                int index = split.IndexOf(filename);
                if(index == -1) {
                    path = addPathToken_end(path);
                    path += filename;
                }
            }

            //  prepare type
            if(location != StorageLocation.Resources_Folder && hasFileType)
            {
                if(filetype.StartsWith("."))
                    filetype = filetype.Remove(0, 1);
            }

            //  format for unity locations  TODO: add StreamingAssets folder
            switch(location)
            {
                case StorageLocation.Resources_Folder:

                    //  format for resources folder
                    int rid = path.IndexOf("Resources");
                    if(rid != -1) {
                        path = path.Remove(0, rid + "Resources".Length);
                    }
                    path = removePathToken_start(path);
                    if(hasFileType && path.EndsWith(filetype)) 
                    {
                        path = path.Remove(path.Length-1-filetype.Length-1);
                    }
                    break;

                case StorageLocation.Asset_Folder:

                    //  format for assets folder
                    if(!path.StartsWith(Application.dataPath))
                    {
                        path = addPathToken_start(path);
                        path = Application.dataPath + path;
                    }
                    if(hasFileType && !path.EndsWith(filetype)) 
                    {
                        path += "." + filetype;
                    }
                    break;

                case StorageLocation.Device_Internal:
                case StorageLocation.Device_External:

                    //  format for foreign device
                    string devicePath = bridge.GetStoragePath(location); 
                    if(!path.StartsWith(devicePath))
                    {
                        devicePath = addPathToken_end(devicePath);
                        path = removePathToken_start(path);
                        path = devicePath + path;
                    }

                    if(hasFileType && !path.EndsWith(filetype)) 
                    {
                        path += "." + filetype;
                    }
                    break;

                case StorageLocation.Custom:
                    if(hasFileType && !path.EndsWith(filetype)) 
                    {
                        path += "." + filetype;
                    }
                    break;
            }

        //    Debug.Log("@FormatPath:: result=[" + path + "]");
            return path;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  loader interface
        

        /// @brief
        /// Load a json object from any storage location.
        ///
        public void LoadJsonAsObject<T>(string folderpath, string filename, StorageLocation location, FileLoaderCallback<T> callback, bool hiddenFileType=false)
        {
            if(mState != FileAccessState.Pending)
            {
                string filetype = hiddenFileType ? "" : "json";
                if(prepareLoadAndCheckAsync<T>(folderpath, filename, filetype, location, callback))
                {
                    _hiddenFileType = hiddenFileType;
                    
//                    Debug.Log("LOAD Json Obj:: " + fullPath);

                    //  immediate load
                    if(typeof(T) == typeof(string))
                    {
                        string result;
                        _loadJsonAsText(fullPath, filename, location, out result);
                        _invokeCallback<T>((T)loadedObject, callback);
                    }
                    else
                    {
                        T result;
                        _loadJsonAsObject<T>(fullPath, filename, location, out result);
                        _invokeCallback<T>(result, callback);
                    }
                }
            }
            else
            {
                Debug.LogError("cannot load another file, waiting for async load... path=[" + fullPath + "]");
            }
        }

        /// @brief
        /// Load a json object from any storage location.
        ///
        public void LoadCustomJsonAsObject<T>(string folderpath, string filename, StorageLocation location, FileLoaderCallback<T> callback, bool hiddenFileType=false) where T : Serialization.ISerializableData, new()
        {
            if(mState != FileAccessState.Pending)
            {
                string filetype = hiddenFileType ? "" : "json";
                if(prepareLoadAndCheckAsync<T>(folderpath, filename, filetype, location, callback))
                {
                    _hiddenFileType = hiddenFileType;
                    //Debug.Log("LOAD Json Obj:: " + fullPath);
                    //  immediate load
                    if(typeof(T) == typeof(string))
                    {
                        string result;
                        _loadJsonAsText(fullPath, filename, location, out result);
                        _invokeCallback<T>((T)loadedObject, callback);
                    }
                    else
                    {
                        T result;
                        _loadCustomJsonAsObject<T>(fullPath, filename, location, out result);
                        _invokeCallback<T>(result, callback);
                    }
                }
            }
            else
            {
                Debug.LogError("cannot load another file, waiting for async load... path=[" + fullPath + "]");
            }
        }

        


        /// @brief
        /// Load json text from any storage location.
        ///
        public void LoadJsonAsText(string folderpath, string filename, StorageLocation location, FileLoaderCallback<string> callback)
        {
            if(mState != FileAccessState.Pending)
            {
                if(prepareLoadAndCheckAsync<string>(folderpath, filename, "json", location, callback))
                {
                    string result;
                    _loadJsonAsText(fullPath, filename, location, out result);
                    _invokeCallback<string>(result, callback);
                }
            }
            else
            {
                Debug.LogError("cannot load another file, waiting for async load... path=[" + fullPath + "]");
            }
        }

        /// @brief
        /// Load plain text from any storage location.
        ///
        public void LoadPlainText(string folderpath, string filename, string filetype, StorageLocation location, FileLoaderCallback<string> callback)
        {
            if(mState != FileAccessState.Pending)
            {
                if(prepareLoadAndCheckAsync<string>(folderpath, filename, filetype, location, callback))
                {
                    string result;
                    _loadPlainText(fullPath, filename, filetype, location, out result);
                    _invokeCallback<string>(result, callback);
                }
            }
            else
            {
                Debug.LogError("cannot load another file, waiting for async load... path=[" + fullPath + "]");
            }
        }

        /// @brief
        /// Load a unity asset from any storage location - currently ONLY WORKING FOR RESOURCE LOCATION!
        ///
        public void LoadAsset<TAsset>(string folderpath, string filename, string filetype, StorageLocation location, FileLoaderCallback<TAsset> callback) where TAsset : Object
        {
            if(mState != FileAccessState.Pending)
            {
                if(prepareLoadAndCheckAsync<TAsset>(folderpath, filename, filetype, location, callback))
                {
                    TAsset result;
                    _loadAsset<TAsset>(fullPath, filename, filetype, location, out result);
                    _invokeCallback<TAsset>(result, callback);
                }
            }
            else
            {
                Debug.LogError("cannot load another file, waiting for async load... path=[" + fullPath + "]");
            }
        }

        /// @brief
        /// Generically load of data of type T from storage.
        ///
        /// @details
        /// Generically loads a file from specified storage location (performance heavy, if type is known beforehand use the other load functions).
        /// implements basic .json and .txt reading capabilities - override customGenericLoad<T> to implement further capabilities. 
        ///
        public void Load<T>(string folderpath, string filename, string filetype, StorageLocation location, FileLoaderCallback<T> callback)
        {
            if(mState != FileAccessState.Pending)
            {
                if(prepareLoadAndCheckAsync<T>(folderpath, filename, filetype, location, callback))
                {
                    _loadGeneric<T>(fullPath, folderpath, filename, filetype, location, callback);
                }
            }
            else
            {
                Debug.LogError("cannot load another file, waiting for async load... path=[" + fullPath + "]");
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  prepare

        private bool prepareLoadAndCheckAsync<T>(string folderpath, string filename, string filetype, StorageLocation location, FileLoaderCallback<T> callback)
        {
            FlushState();
            this.folderPath = folderpath;
            this.fileName = filename;
            this.fileType = filetype;
            this.location = location;
            this.fullPath = FormatPath(folderpath, filename, filetype, location);
            this._hiddenFileType = false;
            
            mState = FileAccessState.Pending;

            if(canLoadImmediate(folderpath, filename, filetype, location))
            {
                currentWorker = null;
                return true;
            }
            else
            {
                currentWorker = new AsyncFileLoadWorker<T>(this, folderpath, filename, filetype, location, callback);
                handleAsyncLoad(currentWorker);
                return false;
            }
        }

        
        internal void _invokeCallback<T>(T result, FileLoaderCallback<T> callback)
        {
            loadedObject = result;
            callback?.Invoke(result, this);
            finished?.Invoke(this);
            finished = null;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  loader functions

        /// @cond PRIVATE

        /*if(typeof(Serialization.ISerializableData).IsAssignableFrom(typeof(T)))
                        {
                            Debug.Log("---> FOUND custom serializable interface!");
                            Serialization.SerializableData.TryParse<T>(txt, out result, true);
                        }*/

        protected bool _loadJsonAsObject<T>(string path, string filename, StorageLocation location, out T result)
        {
            if(location == StorageLocation.Resources_Folder) 
            {
                Debug.Log("load json @path=[" + path + "] type<" + typeof(T) + "> location=[" + location.ToString() + "]");
                var txt = LoadObjectFromResourcePath<TextAsset>(path);
                if(txt != null) 
                {
    //                Debug.Log(">>>" + txt.text);
                    var currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    try {
                        result = JsonConvert.DeserializeObject<T>(txt.text);
                    }
                    finally {
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                    }
                    loadedObject = result;
                    mState = FileAccessState.Success;
                    return true;
                }
                else 
                {
                    Debug.LogError("FileLoader could not load json<" + typeof(T) + "> @path=[" + path + "]");
                }
            }
            else
            {
    //            Debug.Log("load json @path=[" + path + "] type<" + typeof(T) + "> location=[" + location.ToString() + "]");
                var txt = LoadTextFromAbsolutePath(path);
                if(!string.IsNullOrEmpty(txt)) 
                {
    //                Debug.Log("TXT:: " + txt);
                    var currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    try {
                        result = JsonConvert.DeserializeObject<T>(txt);
                    }
                    finally {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    }
                    loadedObject = result;
                    mState = FileAccessState.Success;
                    return true;
                }
            }
            result = default(T);
            return false;
        }
        protected bool _loadCustomJsonAsObject<T>(string path, string filename, StorageLocation location, out T result) where T : Serialization.ISerializableData, new()
        {
            if(location == StorageLocation.Resources_Folder) 
            {
    //            Debug.Log("load json @path=[" + path + "] type<" + typeof(T) + "> location=[" + location.ToString() + "]");
                var txt = LoadObjectFromResourcePath<TextAsset>(path);
                if(txt != null) 
                {
    //                Debug.Log(">>>" + txt.text);
                    var currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    try {
                        if(Serialization.SerializableData.TryParse<T>(txt.text, out result, true))
                        {
                            loadedObject = result;
                            mState = FileAccessState.Success;
                            return true;
                        }
                    }
                    finally {
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                    }
                    loadedObject = result;
                    mState = FileAccessState.Success;
                    return true;
                }
                else 
                {
                    Debug.LogError("FileLoader could not load json<" + typeof(T) + "> @path=[" + path + "]");
                }
            }
            else
            {
    //            Debug.Log("load custom json @path=[" + path + "] type<" + typeof(T) + "> location=[" + location.ToString() + "]");
                var txt = LoadTextFromAbsolutePath(path);
                if(!string.IsNullOrEmpty(txt)) 
                {
    //                Debug.Log("TXT:: " + txt);
                    var currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    try {
                        if(Serialization.SerializableData.TryParse<T>(txt, out result, true))
                        {
                            loadedObject = result;
                            mState = FileAccessState.Success;
                            return true;
                        }
                    }
                    finally {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    }
                    mState = FileAccessState.Type_mismatch;
                    return false;
                }
            }
            result = default(T);
            return false;
        }
        protected bool _loadJsonAsText(string path, string filename, StorageLocation location, out string result)
        {
            if(location == StorageLocation.Resources_Folder) 
            {
                var txt = LoadObjectFromResourcePath<TextAsset>(path);
                if(txt != null) {
                    loadedObject = txt.text;
                    result = txt.text;
                    mState = FileAccessState.Success;
                    return true;
                }
            }
            else
            {
                var txt = LoadTextFromAbsolutePath(path);
                if(!string.IsNullOrEmpty(txt)) {
                    loadedObject = txt;
                    result = txt;
                    mState = FileAccessState.Success;
                    return true;
                }
            }
            result = "";
            return false;
        }
        protected bool _loadPlainText(string path, string fileName, string fileType, StorageLocation location, out string result)
        {
            if(location == StorageLocation.Resources_Folder)
            {
                var txt = LoadObjectFromResourcePath<TextAsset>(path);
                if(txt != null) {
                    loadedObject = txt.text;
                    result = txt.text;
                    mState = FileAccessState.Success;
                    return true;
                }
            }
            else 
            {
                var txt = LoadTextFromAbsolutePath(path);
                if(!string.IsNullOrEmpty(txt)) {
                    loadedObject = txt;
                    result = txt;
                    mState = FileAccessState.Success;
                    return true;
                }
            }
            result = "";
            return false;
        }
        protected bool _loadAsset<T>(string path, string filename, string filetype, StorageLocation location, out T result) where T : Object
        {
            result = null;
            if(location == StorageLocation.Resources_Folder)
            {
                var obj = LoadObjectFromResourcePath<Object>(path);
                if(obj != null)
                {
                    loadedObject = obj;
                    result = obj as T;
                    if(result == null) 
                    {
                        mState = FileAccessState.Type_mismatch;
                    }
                }
            }
            else
            {
                //... any way to load binary and convert to UnityObj?
            }

            if(result != null)
            {
                mState = FileAccessState.Success;
                return true;
            }
            else
            {
                return false;
            }
        }


        private void _loadGeneric<T>(string path, string folderpath, string filename, string filetype, StorageLocation location, FileLoaderCallback<T> callback)
        {
            _loadAsUnityObject = typeof(T) == typeof(Object);
            _loadAsString = typeof(T) == typeof(string);

            bool success = false;
            loadedObject = null;

            if(customGenericLoad<T>(path, folderpath, filename, filetype, location))
            {
                success = mState == FileAccessState.Success;
            }
            else
            {
                switch(filetype) 
                {
                    //---------------- Json ------------------
                    case "json":
                        if(_loadAsString)
                        {
                            string json;
                            if(_loadJsonAsText(path, filename, location, out json))
                            {
                                success = true;
                            }
                        }
                        else
                        {   
                            T json;
                            if(_loadJsonAsObject<T>(path, filename, location, out json))
                            {
                                success = true;
                            }
                        }
                        break;

                    //---------------- Text ------------------
                    case "txt":
                        string text;
                        if(_loadPlainText(path, filename, filetype, location, out text))
                        {
                            success = true;
                        }
                        break;

                    //---------------- Prefabs ---------------
                    case "prefab":
                        GameObject prefab;
                        if(_loadAsUnityObject && _loadAsset<GameObject>(path, filename, filetype, location, out prefab))
                        {
                            success = true;
                        }
                        break;

                    //---------------- Assets ----------------
                    case "asset":
                        Object asset;
                        if(_loadAsUnityObject && _loadAsset<Object>(path, filename, filetype, location, out asset))
                        {
                            success = true;
                        }
                        break;
                    
                    //--------------- Graphics ---------------
                    case "png":
                    case "jpeg":
                    case "jpg":
                    case "tif":
                        Texture tex;
                        if(_loadAsUnityObject && _loadAsset<Texture>(path, filename, filetype, location, out tex))
                        {
                            success = true;
                        }
                        break;

                    //---------------- Fonts -----------------
                    case "ttf":
                        Font font;
                        if(_loadAsUnityObject && _loadAsset<Font>(path, filename, filetype, location, out font))
                        {
                            success = true;
                        }
                        break;
                    

                    default:
                        success = false;
                        mState = FileAccessState.Type_mismatch;
                        break;
                }
            }

            //  result & callback
            if(success && loadedObject != null)
            {
                T result = (T)loadedObject;
                if(result is T)
                {
                    mState = FileAccessState.Success;
                    
                }
                else 
                {
                    mState = FileAccessState.Type_mismatch;
                }
                _invokeCallback<T>(result, callback);
            }
            else
            {
                
                mState = FileAccessState.Type_mismatch;
                _invokeCallback<T>(default(T), callback);
                loadedObject = null;
            }
        }   


 /*       protected bool LoadImage(string path, string filename, string filetype, StorageLocation location, out Texture result)
        {
            loadedObject = null;
            result = null;
            if(location == StorageLocation.Resources_Folder)
            {
                if(_loadAsUnityObject)
                {
                    var obj = LoadObjectFromResourcePath<Object>(path);
                    loadedObject = obj;
                    result = obj as Texture;
                }
            }
            else
            {
                //... any way to load binary and convert to UnityObj?
            }
            return result != null;
        }
        protected bool LoadFont(string path, string filename, string filetype, StorageLocation location, out Font result)
        {
            loadedObject = null;
            result = null;
            if(location == StorageLocation.Resources_Folder)
            {
                if(_loadAsUnityObject)
                {
                    var obj = LoadObjectFromResourcePath<Object>(path);
                    loadedObject = obj;
                    result = obj as Font;
                }
            }
            else
            {
                //... any way to load binary and convert to UnityObj?
            }
            return result != null;
        }
        protected bool LoadPrefab(string path, string filename, string filetype, StorageLocation location, out GameObject result)
        {
            loadedObject = null;
            result = null;
            if(location == StorageLocation.Resources_Folder)
            {
                if(_loadAsUnityObject)
                {
                    var obj = LoadObjectFromResourcePath<Object>(path);
                    loadedObject = obj;
                    result = obj as GameObject;
                }
            }
            else
            {
                //... any way to load binary and convert to UnityObj?
            }
            return result != null;
        }
*/

        protected T LoadObjectFromResourcePath<T>(string path) where T : Object
        {
            var obj = Resources.Load<T>(path);
            if(obj == null)
            {
                mState = FileAccessState.File_not_found;
            }
            return obj;
        }

        protected string LoadTextFromAbsolutePath(string path)
        {
            if(File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            else 
            {
                mState = FileAccessState.File_not_found;
                return "";
            }
        }

        //-----------------------------------------------------------------------------------------------------------------


        //  helper

       

        protected string removePathToken_start(string s) {
            if(s.StartsWith(PATH_TOKEN))
                s = s.Remove(0, 1);
            return s;
        }
        protected string removePathToken_end(string s) {
            if(s.EndsWith(PATH_TOKEN))
                s = s.Remove(s.Length-1, 1);
            return s;
        }
        protected string addPathToken_start(string s) {
            if(!s.StartsWith(PATH_TOKEN))
                s = PATH_TOKEN + s;
            return s;
        }
        protected string addPathToken_end(string s) {
            if(!s.EndsWith(PATH_TOKEN))
                s += PATH_TOKEN;
            return s;
        }


        ///@endcond
    }


    //==================================================================================================================


    /// @brief
    /// Helper class for async loading.
    ///
    public abstract class AsyncFileLoadWorker
    {
        public readonly FileLoader context;
        public readonly float startTime;
        public readonly string folderPath;
        public readonly string fileName;
        public readonly string fileType;
        public readonly StorageLocation location;
        public FileAccessState state { get; protected set; }
        public object result { get; private set; }

        public bool finished { get; private set; }

        public AsyncFileLoadWorker(FileLoader context, string folderpath, string filename, string filetype, StorageLocation location)
        {
            this.context = context;
            this.startTime = Time.time;
            this.folderPath = folderpath;
            this.fileName = filename;
            this.fileType = filetype;
            this.location = location;
        }
        
        public abstract void SetTxtResult(string txt, FileAccessState state);
        public abstract void SetJsonResult(string json, FileAccessState state);
        public abstract void SetBinaryResult(byte[] bytes, FileAccessState state);
    }

    public class AsyncFileLoadWorker<T> : AsyncFileLoadWorker
    {
        public readonly FileLoaderCallback<T> callback;
        public AsyncFileLoadWorker(FileLoader context, string folderpath, string filename, string filetype, StorageLocation location, FileLoaderCallback<T> callback)
            : base(context, folderpath, filename, filetype, location)
        {
            this.callback = callback;
        }

        public override void SetTxtResult(string txt, FileAccessState state)
        {
            if(state == FileAccessState.Success 
                && typeof(T) == typeof(string))
            {
                if(Commands.RESPONSE_CREATED_FILE.Equals(txt))
                {
                    Debug.Log("CREATED empty JSON FILE!!");
                    context.mState = FileAccessState.Success;
                    context._invokeCallback<T>(default(T), callback);
                }
                else if(!string.IsNullOrEmpty(txt))
                {
                    context.mState = FileAccessState.Success;
                    context._invokeCallback<T>((T)(object)txt, callback);
                }
                else
                {
                    context.mState = FileAccessState.File_not_found;
                    context._invokeCallback<T>(default(T), callback);
                }
            }
            else
            {
                context.mState = state == FileAccessState.Success ? FileAccessState.Type_mismatch : FileAccessState.File_not_found;
                context._invokeCallback<T>(default(T), callback);
            }
        }

        public override void SetJsonResult(string json, FileAccessState state)
        {
            if(state == FileAccessState.Success)
            {
                if(Commands.RESPONSE_CREATED_FILE.Equals(json))
                {
                    context.mState = FileAccessState.Success;
                    context._invokeCallback<T>(default(T), callback);
                }
                else if(!string.IsNullOrEmpty(json))
                {
                    if(typeof(T) == typeof(string))
                    {
                        SetTxtResult(json, state);
                    }
                    else
                    {
                        var currentCulture = Thread.CurrentThread.CurrentCulture;
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                        try {
                            T result = JsonConvert.DeserializeObject<T>(json);
                            context.mState = FileAccessState.Success;
                            context._invokeCallback<T>(result, callback);
                        }
                        catch(IOException ex)
                        {
                            Debug.LogWarning("AsyncFileLoaderException: " + ex.Message);
                            context.mState = FileAccessState.Type_mismatch;
                            context._invokeCallback<T>(default(T), callback);
                        }
                        finally {
                            Thread.CurrentThread.CurrentCulture = currentCulture;
                        }
                    }
                }
                else
                {
                    //  accessed empty file, load default
                    context.mState = FileAccessState.Success;
                    context._invokeCallback<T>(default(T), callback);
                }
            }
            else
            {
                context.mState = state;
                context._invokeCallback<T>(default(T), callback);
            }
        }

        public override void SetBinaryResult(byte[] bytes, FileAccessState state)
        {
            if(state == FileAccessState.Success && bytes != null && bytes.Length > 0)
            {

            }
            else
            {
                context.mState = FileAccessState.File_not_found;
                context._invokeCallback<T>(default(T), callback);
            }
        }
    }

}

