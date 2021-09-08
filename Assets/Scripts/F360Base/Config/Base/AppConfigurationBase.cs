using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//using F360.Manage;

namespace F360
{

    public delegate string AppPathGetter(bool absolutePath=true);
    public delegate int AppValueGetter(bool cached=true);
    public delegate bool AppSettingsGetter(bool cached=true);

    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Configuration object for running client.
    ///
    public interface IAppConfig
    {

        //IPatchLevelManifest patchManifest { get; set; }

        string SDRootPath  { get; }     ///< application data root path (absolute)
        string DataFolder  { get; }       ///< path to current persistent device storage, relative to RootPath
        string JsonDataFolder { get; }      ///< path to inner data, relative to RootPath. Location of license & other cached data

        string DefaultDeviceName { get; }

        //string GetPatchLevel();

        string GetPath(string key, bool absolute=true);
        int GetValue(string key, bool cached=true);
        bool GetSetting(string key, bool cached=true);

        void AddPath(string key, string path);
        void AddValue(string key, int value);
        void AddSetting(string key, bool setting);

        void AddPath(string key, AppPathGetter getter);     ///< bool param of getter defines wether path is absolute or relative
        void AddValue(string key, AppValueGetter getter);
        void AddSetting(string key, AppSettingsGetter getter);
        string ResolvePath(string path);

        string Print();
    }



    //-----------------------------------------------------------------------------------------------------------------



    /// @brief
    /// Defines path & value settings for an app.
    ///
    public abstract class AppConfigBase : IAppConfig
    {

        // default defines

        public const string PATH_PATCH_MANIFEST = "p_patch_manifest";       ///< Path to current patch manifest   
        public const string PATH_LOCAL_USERDATA = "p_localUser";            ///< Path to cached userdata
        public const string PATH_LOCAL_BUNDLEDATA = "p_localBundles";       ///< Path to current Bundle data
        public const string PATH_ACTIVE_BUILD = "p_current_build";          ///< Path to current Build data

        public const string FOLDERNAME_LOCAL_BUNDLEDATA = "f_localBundles";     ///< Bundle Folder Name (w/o path)

        //  dynamic path values
        const string TOKEN_STREAMINGASSETS = "<streamingassets>/";     //  exchanged for Application.streamingAssetsPath
        const string TOKEN_BUILDASSETS = "<assets>/";                  //  exchanged for Application.dataPath
        const string TOKEN_PERSISTENTASSETS = "<appdata>/";            //  exchanged for Application.persistentDataPath
 //       const string TOKEN_DATA_ROOT = "<root>/";                    //  exchanged for Application.persistentDataPath         [OBSOLETE?]
        const string TOKEN_SD_DATA_ROOT = "<sdcard>/";                 //  exchanged for sdcard root (or <root>)
        const string TOKEN_BUILD = "<build>/";                         //  exchanged for active build path
        const string TOKEN_PROJECT = "<project>/";                     //  exchanged for parent of Application.dataPath

        //-----------------------------------------------------------------------------------------------------------------

        //  static access

        public static bool Exists() { return _instance != null; }

        ///  singleton access to client's current configuration
        public static IAppConfig Current 
        { 
            get { 
                if(_instance == null)
                {
                    throw new System.Exception("No AppConfiguration Instance found!");
                }
                return _instance;
            } 
        }
        static IAppConfig _instance;    

        /// @brief  
        /// sets a new config and returns the old
        ///
        public static IAppConfig SwitchConfig(IAppConfig newConfig)
        {
            var old = Current;
            _instance = newConfig;
            return old;
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  static interface

        public static string AppDataFolder { get { return Current.DataFolder; } }

        public static string GetPath(string key, bool absolute=true) { return Current.GetPath(key, absolute); }
        public static int GetValue(string key, bool cached=true) { return Current.GetValue(key, cached); }
        public static bool GetSetting(string key, bool cached=true) { return Current.GetSetting(key, cached); }
        public static void AddPath(string key, string path) { Current.AddPath(key, path); }
        public static void AddPath(string key, AppPathGetter getter) { Current.AddPath(key, getter); }
        public static void AddValue(string key, int value) { Current.AddValue(key, value); }
        public static void AddValue(string key, AppValueGetter getter) { Current.AddValue(key, getter); }
        public static void AddSetting(string key, bool setting) { Current.AddSetting(key, setting); }
        public static void AddSetting(string key, AppSettingsGetter getter) { Current.AddSetting(key, getter); }
        public static string ResolvePath(string path) { return Current.ResolvePath(path); }


        /*public static TManifest GetPatchManifest<TManifest>() where TManifest : class, IPatchLevelManifest
        {
            return Current.patchManifest as TManifest;
        }
        public static void SetPatchManifest(IPatchLevelManifest manifest)
        {
            Current.patchManifest = manifest;
        }*/


        /*public static string GetAppVersion()
        {
            return Current.GetPatchLevel();
        }*/


        public static string GetDeviceSerial()
        {
            return VRInterface.GetHardwareSerial();
        }

        public static string GetDefaultDeviceName()
        {
            return Current.DefaultDeviceName;
        }



        //-----------------------------------------------------------------------------------------------------------------

        //  interface

        //public abstract IPatchLevelManifest patchManifest { get; set; }


        public abstract string SDRootPath { get; protected set; }

        /// @brief
        /// relative data
        ///
        public abstract string DataFolder { get; protected set; }

        public virtual string JsonDataFolder { get; protected set; }

        public abstract string DefaultDeviceName { get; }

        //public abstract string GetPatchLevel();

        public virtual string Print() { return PrintPathsAndValues(); }


        /// @param rootPath Absolute path to root data folder on current filesystem
        /// @param relativeDataPath Live data path for current app instance
        ///
        public void SetDeviceRootPath(string rootPath, string relativeDataPath, string jsonDataPath, bool useUnityResourceFolder=false)
        {
            SDRootPath = rootPath;
            DataFolder = relativeDataPath;
            JsonDataFolder = jsonDataPath;
            loadFromUnityResourceFolder = useUnityResourceFolder;

            //Debug.Log("AppConfig:: set paths...\n\troot: " + rootPath + "\n\tdata: " + relativeDataPath + "\n\tjson: " + jsonDataPath);
        }

        /// @brief
        /// clears all cached values & paths
        ///
        public void ClearCache()
        {
            cachedPaths.Clear();
            cachedValues.Clear();
        }   
        

        //-----------------------------------------------------------------------------------------------------------------

        //  internal


        protected void addPathInternal(string key, string path)
        {
            if(!cachedPaths.ContainsKey(key)) cachedPaths.Add(key, path);
            else cachedPaths[key] = path;
        }
        protected void addValueInternal(string key, int value)
        {
            if(!cachedValues.ContainsKey(key)) cachedValues.Add(key, value);
            else cachedValues[key] = value;
        }
        protected void addSettingInternal(string key, bool setting)
        {
            if(!cachedSettings.ContainsKey(key)) cachedSettings.Add(key, setting);
            else cachedSettings[key] = setting;
        }


        protected void addPathInternal(string key, AppPathGetter getter) 
        {
            if(!pathCreators.ContainsKey(key))
                pathCreators.Add(key, getter);
            else
                pathCreators[key] = getter;
        }
        protected void addValueInternal(string key, AppValueGetter getter)
        {
            if(!valueCreators.ContainsKey(key)) 
                valueCreators.Add(key, getter);
            else
                valueCreators[key] = getter;
        }
        protected void addSettingInternal(string key, AppSettingsGetter getter)
        {
            if(!settingCreators.ContainsKey(key))
                settingCreators.Add(key, getter);
            else 
                settingCreators[key] = getter;
        }

        protected string getPathInternal(string key, bool absolute)
        {
            if(pathCreators[key] != null && pathCreators.ContainsKey(key))
            {
                if(absolute)
                {
                    if(!cachedPaths.ContainsKey(key)) cachedPaths.Add(key, pathCreators[key](true));
                    else if(string.IsNullOrEmpty(cachedPaths[key])) cachedPaths[key] = pathCreators[key](true);
                    return cachedPaths[key];
                }
                else
                {
                    return pathCreators[key](false);
                }
            }
            else if(cachedPaths.ContainsKey(key))
            {
                return cachedPaths[key];
            }
            Debug.LogWarning("AppConfiguration does not contain a path with key=[" + key + "]");
            return "";
        }
        protected int getValueInternal(string key, bool cached)
        {
            if(valueCreators != null && valueCreators.ContainsKey(key))
            {
                if(cached)
                {
                    if(!cachedValues.ContainsKey(key)) cachedValues.Add(key, valueCreators[key](true));
                    else if(cachedValues[key] == -1) cachedValues[key] = valueCreators[key](true);
                    return cachedValues[key];
                }
                else
                {
                    return valueCreators[key](false);
                }
            }
            else if(cachedValues.ContainsKey(key)) 
            {
                return cachedValues[key];
            }
            Debug.LogWarning("AppConfiguration does not contain a value with key=[" + key + "]");
            return -1;
        }
        protected bool getSettingInternal(string key, bool cached)
        {
            if(settingCreators != null && settingCreators.ContainsKey(key))
            {
                if(cached)
                {
                    if(!cachedSettings.ContainsKey(key)) cachedSettings.Add(key, settingCreators[key](true));
                    return cachedSettings[key];
                }
                else
                {
                    return settingCreators[key](false);
                }
            }
            else if(cachedSettings.ContainsKey(key))
            {
                return cachedSettings[key];
            }
            Debug.LogWarning("AppConfiguration does not contain setting with key=[" + key + "]");
            return false;
        }




        //-----------------------------------------------------------------------------------------------------------------

        protected abstract string Define_PathToPatchManifest(bool absolute);
        protected abstract string Define_PathToBundleData(bool absolute); 
        protected abstract string Define_PathToUserData(bool absolute);
        protected abstract string Define_PathToCurrentBuildData(bool absolute);

        protected string Define_BundleFolderName(bool b)
        {
            //  get folder name from path
            var bundlePath = GetPath(PATH_LOCAL_BUNDLEDATA, false);
            if(!string.IsNullOrEmpty(bundlePath))
            {
                if(bundlePath.EndsWith("/"))
                    bundlePath = bundlePath.Remove(bundlePath.Length-1, 1);
                int id = bundlePath.LastIndexOf("/");
                if(id != -1)
                {
                    id++;
                    return bundlePath.Substring(id, bundlePath.Length-id) + "/";
                }
                else
                {
                    return bundlePath + "/";
                }
            }
            return "Error_No_Bundle_Path";
        }

        protected enum PathFormat
        {
            Absolute,
            RelativeToDataFolder
        }

        string IAppConfig.ResolvePath(string path) { return FormatDataPath(path, PathFormat.Absolute, PathFormat.Absolute); }
        
        protected string FormatDataPath(string path, PathFormat src, bool absoluteOutput=true)
        {
            return FormatDataPath(path, src, absoluteOutput ? PathFormat.Absolute : PathFormat.RelativeToDataFolder);
        }
        protected string FormatDataPath(string path, PathFormat src, PathFormat trgt)
        {
            src = PathFormat.Absolute;          //  NOT NEEDED ANYMORE?


            if(string.IsNullOrEmpty(path))
            {
                return "Error_Empty_Path";
            }
            
            string inputPath = path;
            string outputPath = path;

   //         UnityEngine.Debug.Log("A FormatDataPath=[" + inputPath + "] src=[" + src + "] trgt=[" + trgt + "]\nroot=[" + SDRootPath + "]");
            if(true || trgt == PathFormat.Absolute)
            {
                HandleDynamicPathTokens(ref inputPath);
            }
            else
            {
                RemoveDynamicPathToken(ref inputPath);
            }
            
  //          UnityEngine.Debug.Log("B FormatDataPathAfterTokens=[" + inputPath + "]");


            switch(src)
            {
                case PathFormat.Absolute:
                    switch(trgt)
                    {
                        case PathFormat.Absolute:               
                            outputPath = inputPath;
                            break;

                        case PathFormat.RelativeToDataFolder:
                            if(!string.IsNullOrEmpty(SDRootPath) && inputPath.StartsWith(SDRootPath))
                            {
                                outputPath = inputPath.Substring(SDRootPath.Length, inputPath.Length-SDRootPath.Length);
                            }
                            else if(inputPath.StartsWith(Application.persistentDataPath))
                            {
                                outputPath = inputPath.Substring(Application.persistentDataPath.Length, inputPath.Length-Application.persistentDataPath.Length);
                            }
                            else
                            {
                                outputPath = inputPath;
                            }
                            break;
                    }
                    break;

                case PathFormat.RelativeToDataFolder:
                    switch(trgt)
                    {
                        case PathFormat.RelativeToDataFolder:   
                            outputPath = inputPath;
                            break;

                        case PathFormat.Absolute:               
                            //  in editor, resources marked as being located relative to data folder are always under a Resources folder
                            //  directly under DataPath, i.e "<ProjectPath>/Assets/<DataPath>/Resources/..."

                            if(Application.isEditor && loadFromUnityResourceFolder)                
                            {
                                string rootPath = Application.dataPath + "/" + DataFolder + "Resources/";
                                if(!inputPath.Contains(rootPath))
                                {
                                    outputPath = rootPath + inputPath;
                                }
                                else
                                {
                                    outputPath = inputPath;
                                }
                            }
                            else if(!inputPath.Contains(SDRootPath))
                            {
                                if(!inputPath.Contains(DataFolder))
                                {
                                    outputPath = SDRootPath + DataFolder + inputPath;
                                }
                                else
                                {
                                    outputPath = SDRootPath + inputPath;
                                }
                            }
                            else
                            {
                                outputPath = inputPath;
                            }
                            break;
                    }
                    break;
            }

  //          UnityEngine.Debug.Log("C FormatDataPathAfterTransformation=[" + outputPath + "]");
            return outputPath;
        }


        //-----------------------------------------------------------------------------------------------------------------

        bool HandleDynamicPathTokens(ref string path)
        {
            if(_handleToken(ref path, TOKEN_STREAMINGASSETS, Application.streamingAssetsPath)) return true;
            else if(_handleToken(ref path, TOKEN_BUILDASSETS, Application.dataPath + "/")) return true;
            else if(_handleToken(ref path, TOKEN_PERSISTENTASSETS, Application.persistentDataPath  + "/" + DataFolder)) return true;
            else if(_handleToken(ref path, TOKEN_SD_DATA_ROOT, SDRootPath + DataFolder)) return true;
            else if(_handleToken(ref path, TOKEN_BUILD, ()=> { return GetPath(PATH_ACTIVE_BUILD); })) return true;
            #if UNITY_EDITOR
            else if(_handleToken(ref path, TOKEN_PROJECT, Application.dataPath.Remove(Application.dataPath.Length-"Assets/".Length))) return true;
            #endif
            return false;
        }

        bool _handleToken(ref string path, string token, string replace)
        {
            if(path.Contains(token))
            {
                if(!replace.EndsWith("/")) replace += "/";
    //            Debug.Log("replace " + token + " in [" + path + "] with : [" + replace + "]");
                path = path.Replace(token, replace);
                return true;
            }
            return false;
        }
        bool _handleToken(ref string path, string token, System.Func<string> replace)
        {
            if(path.Contains(token))
            {
                var r = replace();
                if(!r.EndsWith("/")) r += "/";
    //            Debug.Log("replace " + token + " in [" + path + "] with : [" + r + "]");
                path = path.Replace(token, r);
                return true;
            }
            return false;
        }


        bool RemoveDynamicPathToken(ref string path)
        {
            if(_removeToken(ref path, TOKEN_STREAMINGASSETS)) return true;
            if(_removeToken(ref path, TOKEN_PERSISTENTASSETS)) return true;
            if(_removeToken(ref path, TOKEN_SD_DATA_ROOT)) return true;
            if(_removeToken(ref path, TOKEN_BUILD)) return true;
            #if UNITY_EDITOR
            if(_removeToken(ref path, TOKEN_PROJECT)) return true;
            #endif
            return false;
        }

        bool _removeToken(ref string path, string token)
        {
            int id = path.IndexOf(token);
            if(id != -1)
            {
                path = path.Remove(id, token.Length);
                return true;
            }
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------------

        protected AppConfigBase()
        {
            pathCreators = new Dictionary<string, AppPathGetter>();
            valueCreators = new Dictionary<string, AppValueGetter>();
            settingCreators = new Dictionary<string, AppSettingsGetter>();
            cachedPaths = new Dictionary<string, string>();
            cachedValues = new Dictionary<string, int>();
            cachedSettings = new Dictionary<string, bool>();

            //  paths
            addPathInternal(PATH_PATCH_MANIFEST,        Define_PathToPatchManifest);
            addPathInternal(PATH_LOCAL_USERDATA,        Define_PathToUserData);
            addPathInternal(PATH_LOCAL_BUNDLEDATA,      Define_PathToBundleData);
            addPathInternal(PATH_ACTIVE_BUILD,          Define_PathToCurrentBuildData);
            
            //  folders
            addPathInternal(FOLDERNAME_LOCAL_BUNDLEDATA,    Define_BundleFolderName);

            /*if(_instance == null)
            {
                Debug.Log(RichText.color("----> New Config Loaded! <----", Color.blue));
            }
            else
            {
                Debug.Log(RichText.color("----> New Config Created! <----", Color.blue));
            }*/
            _instance = this;
        }

        Dictionary<string, AppPathGetter> pathCreators;
        Dictionary<string, AppValueGetter> valueCreators;
        Dictionary<string, AppSettingsGetter> settingCreators;

        Dictionary<string, string> cachedPaths;     //  lazy caching absolute paths
        Dictionary<string, int> cachedValues;
        Dictionary<string, bool> cachedSettings;


        protected bool loadFromUnityResourceFolder = false;

        protected string PrintPathsAndValues()
        {
            var b = new System.Text.StringBuilder("PATHS=");
            foreach(var key in cachedPaths.Keys)
                b.Append("\n\t" + key + "[" + cachedPaths[key] + "]");
            b.Append("\nVALUES=");
            foreach(var key in cachedValues.Keys)
                b.Append("\n\t" + key + "[" + cachedValues[key] + "]");
            return b.ToString();
        }



        //  IAppConfig interface implementation

        void IAppConfig.AddPath(string key, string path)
        {
            addPathInternal(key, path);
        }
        void IAppConfig.AddPath(string key, AppPathGetter getter)
        {
            addPathInternal(key, getter);
        }
        void IAppConfig.AddValue(string key, int value)
        {
            addValueInternal(key, value);
        }
        void IAppConfig.AddValue(string key, AppValueGetter getter)
        {   
            addValueInternal(key, getter);
        }
        void IAppConfig.AddSetting(string key, bool setting)
        {
            addSettingInternal(key, setting);
        }
        void IAppConfig.AddSetting(string key, AppSettingsGetter getter)
        {
            addSettingInternal(key, getter);
        }
        string IAppConfig.GetPath(string key, bool absolute)
        {
            return getPathInternal(key, absolute);    
        }
        int IAppConfig.GetValue(string key, bool cached)
        {
            return getValueInternal(key, cached);
        }
        bool IAppConfig.GetSetting(string key, bool cached)
        {   
            return getSettingInternal(key, cached);
        }
        
    }
}


