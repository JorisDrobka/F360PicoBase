using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.IO;
using UnityEngine;

using DeviceBridge.Serialization;

using F360.Backend;
using F360.Backend.Messages;
using F360.Backend.Synch;

namespace F360.Users.Internal
{

    /// @brief
    /// helper object to cache userdata
    ///
    public class UserCache
    {

        const string DEFAULT_FILE_NAME = "usr_";    //  name of local userdata cache file

        
        /// @brief
        /// binary-serialized user data.
        ///
        [System.Serializable]
        struct CachedData : ISerializable
        {
            public DateTime lastSynchT;
            public User[] users;

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("synchT", lastSynchT, typeof(DateTime));
                info.AddValue("users", users, typeof(User[]));
            }

            public CachedData(SerializationInfo info, StreamingContext context)
            {
                lastSynchT = (DateTime) info.GetValue("synchT", typeof(DateTime));
                users = (User[]) info.GetValue("users", typeof(User[]));
            }
        }


        //----------------------------------------------------------------------------------------------------------------
        

        readonly Database database;
        readonly string ID;
        
        string pathToFile = "";
        Dictionary<string, User> data = new Dictionary<string, User>();
        bool dirtyFlag;

        public UserCache(Database database, string id)
        {
            this.database = database;
            this.ID = id;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  data

        public bool isLoaded { get; private set; }

        public int Count { get { return data.Count; } }

        public bool Exists(string user)
        {
            return data.ContainsKey(user);
        }

        public bool Add(User user)
        {
            if(user != null && !data.ContainsKey(user.Name))
            {
                data.Add(user.Name, user);
                dirtyFlag = true;
                return true;
            }
            return false;
        }

        public bool CheckPassword(string user, string pass)
        {
            if(data.ContainsKey(user))
            {
                return data[user].Pass == pass;
            }
            return false;
        }

        public User Get(string user)
        {
            if(!string.IsNullOrEmpty(user) && data.ContainsKey(user))
            {
                return data[user];
            }
            return null;
        }
        public User Get(int id)
        {
            foreach(var key in data.Keys)
            {
                if(data[key].ID == id) return data[key];
            }
            return null;
        }

        internal IEnumerable<User> GetAll()
        {
            return data.Values;
        }

        public bool DeleteUser(string user)
        {
            if(data.ContainsKey(user))
            {
                data.Remove(user);
                dirtyFlag = true;
                return true;
            }
            return false;
        }

        public void SetDirty()
        {
            dirtyFlag = true;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  Server

        public DateTime LastSynchTime { get; set; }


        //-----------------------------------------------------------------------------------------------------------------

        //  Local

        public bool SaveToCache(string filename="")
        {
//            Debug.Log("CachedUserRepo:: saveToCache... dirty: " + (hasDirtyEntries() + "/self? " + dirtyFlag) + "\n\tpathToFile: " + pathToFile + "\n\tfilename: " + filename);
            return /*!hasDirtyEntries() || */saveBinaryInternal(pathToFile, filename);
        }

        public bool LoadFromCache(string filepath, string filename="")
        {
            isLoaded = loadBinaryInternal(filepath);
            return isLoaded;
        }

        bool hasDirtyEntries()
        {
            if(dirtyFlag) return true;
            else {
                foreach(var user in data.Values)
                {
                    if(user.isDirty) { return true; }
                }
                return false;
            }            
        }

        bool saveBinaryInternal(string path, string filename="")
        {
            if(string.IsNullOrEmpty(path))
            {
                path = AppConfigBase.GetPath(AppConfigBase.PATH_LOCAL_USERDATA);
            }

            if(!string.IsNullOrEmpty(path))
            {   
                if(path.EndsWith("/"))
                {
                    if(!string.IsNullOrEmpty(filename)) {
                        if(!filename.EndsWith("_")) filename += "_";
                        path += filename + ID;
                    }   
                    else {
                        path += DEFAULT_FILE_NAME + ID;
                    }
                }
                
                CachedData cached = new CachedData
                {
                    users = data.Values.ToArray(),
                    lastSynchT = LastSynchTime
                };

                if(!path.EndsWith(".json"))
                {
                    path += ".json";
                }

                FileStream file=null;
                try
                {
                    var folder = F360FileSystem.RemoveFileFromPath(path);
                    DirectoryInfo info = new DirectoryInfo(folder);
                    Debug.Log("CachedUserRepo:: " + RichText.emph(RichText.darkGreen("Save Userdata")) + " @folder=[" + folder + "] exists? " + info.Exists + "\nfullpath=[" + path + "]");
                    if(!info.Exists)
                    {
                        info.Create();
                    }
                    
            //        Debug.Log("CachedUserRepo:: now open for write... file exists? " + File.Exists(path));

                    if(File.Exists(path))
                    {
                        file = File.OpenWrite(path);
                    }
                    else
                    {
                        file = File.Create(path);
                    }
                }
                catch(System.IO.IOException ex)
                {
                    Debug.LogError("CachedUserRepo:: Error while trying to save cached userdata at path=[" + path + "]: " + ex.Message);
                    return false;
                }
                
                if(BinarySerializationHelper.Serialize<CachedData>(file, cached))
                {
            //        Debug.Log("CachedUserRepo:: Saved userdata[" + data.Count + "] to cache @path=" + path);
                    pathToFile = path;
                    dirtyFlag = false;
                    return true;
                }
                else
                {
                    Debug.LogError("CachedUserRepo:: error while saving userdata @path=[" + path + "]");
                    return false;
                }
            }
            else
            {
                Debug.LogError("CachedUserRepo:: No Userdata path defined!");
                return false;
            }
        }

        bool loadBinaryInternal(string path, string filename="")
        {
            if(string.IsNullOrEmpty(path))
            {
                path = AppConfigBase.GetPath(AppConfigBase.PATH_LOCAL_USERDATA);
            }

            if(!string.IsNullOrEmpty(path))
            {
                if(!string.IsNullOrEmpty(filename)) {
                    if(!filename.EndsWith("_")) filename += "_";
                    path += filename + ID;
                }
                else {
                    path += DEFAULT_FILE_NAME + ID;
                }
                if(!path.EndsWith(".json"))
                {
                    path += ".json";
                }

                Debug.Log("load userdata @path=[" + path + "]  ..exists? " + File.Exists(path));

                if(File.Exists(path))
                {
                    CachedData cached;
                    if(BinarySerializationHelper.Deserialize<CachedData>(File.ReadAllBytes(path), out cached))
                    {
                        if(cached.users != null)
                        {
                            LastSynchTime = cached.lastSynchT;
                            foreach(var u in cached.users)
                            {
                                if(data.ContainsKey(u.Name))
                                {
                                    Debug.LogWarning("User profile doubled!");
                                    if(u.LastLogin.CompareTo(data[u.Name].LastLogin) < 0)
                                    {
                                        data[u.Name] = u;
                                    }
                                }
                                else
                                {
                                    data.Add(u.Name, u);
                                }
                            }

                            pathToFile = path;
                        //    Debug.Log("CachedUserRepo:: " + RichText.emph("Load userdata") + " from path: " + path + " num accounts loaded=" + data.Count);
                            return true;
                        }
                        else
                        {
                            Debug.LogError("CachedUserRepo:: Cached userdata file could not be read!\npath=[" + path + "]");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("CachedUserRepo:: No cached userdata file found at path=[" + path + "]");
                    }
                }                
                else
                {
                    pathToFile = path;
                    Debug.LogWarning("CachedUserRepo:: No cached userdata file found at path=[" + path + "]");        //  TODO: how to handle missing cache?
                    return true;
                }
            }
            else
            {
                Debug.LogError("CachedUserRepo:: No Userdata path defined!");
            }
            return false;
        }



    }




    

}   

