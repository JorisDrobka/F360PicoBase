using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using F360.Backend;
using F360.Backend.Messages;

/* ######### ILUME ##########
    
    Bitte Implementieren

*/


namespace F360.Users.Stats
{

    /// @brief 
    /// access to locally saved statistics of each user
    ///
    /// manages synchronization
    ///
    public class StatRepo
    {

        public static StatRepo Current
        {
            get { return instance; }
        }
        static StatRepo instance = new StatRepo();

        Dictionary<int, UserStats> cache = new Dictionary<int, UserStats>();

        string FormatFilePath(int userID)
        {
            var b = new System.Text.StringBuilder();
            b.Append(AppConfig.GetPath(AppConfig.PATH_LOCAL_USERDATA));
            b.Append("-");
            b.Append(userID.ToString());
            return b.ToString();
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  interface

        

        public bool isAvailable(int userID)
        {
            return cache.ContainsKey(userID);
        }

        /// @brief
        /// try load stats from disk
        ///
        public bool LoadStats(int userID, ref UserStats stats)
        {
            if(cache.ContainsKey(userID))
            {
                stats = cache[userID];
                return true;
            }
            else
            {
                //  access serialized statistics of user 
                if(stats == null)
                {
                    stats = new UserStats(userID);
                }
                var path = FormatFilePath(userID);
                SavedUserStats data;
                if(StatSerialization.LoadUserStatsFromDisk(path, out data))
                {
                    stats.LoadFromSerializedData(data);
                    if(!cache.ContainsKey(userID))
                    {
                        cache.Add(userID, stats);
                    }
                    return true;
                }
                else
                {
                    Debug.LogWarning("StatRepo:: unable to load data of user=[" + RichText.emph(userID) + "]\npath=[" + RichText.italic(path) + "]");
                }
            }
            return false;
        }
        

        public bool SaveToDisk()
        {
            string path = AppConfig.GetPath(AppConfig.PATH_LOCAL_USERDATA);
            try {
                if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                
                foreach(var userdata in cache.Values)
                {
                    if(userdata.isDirty())
                    {
                        var filepath = FormatFilePath(userdata.UserID);
                        if(StatSerialization.WriteUserStatsToDisk(filepath, userdata))
                        {
                            userdata.SerializationTime = Backend.TimeUtil.ServerTime;
                        }
                        else
                        {
                            Debug.LogWarning("StatRepo:: unable to save data of user=[" + RichText.emph(userdata.UserID) + "]\npath=[" + RichText.italic(filepath) + "]");
                        }
                    }
                }
                return true;
            }
            catch(IOException ex) {
                Debug.LogError("StatRepo:: cannot save to disk! path=[" + RichText.italic(path) + "]\n" + ex);
                return false;
            }
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  Server Communication


        public bool PullFromServer(int userID, System.Action<bool> whendone=null)
        {
            //  try load from server via StandardAPI & return when done or failed.
            //  if wifi is unavailable, return false
            var user = StudentRepo.Instance.GetUser(userID) as F360Student;
            if(user != null)
            {
                F360Client client;
                if(GetClient(out client))
                {

                }
            }
            else
            {   
                Debug.LogWarning("StatRepo:: UserID<" + userID + "> does not exist!");
            }
            return false;
        }

        public bool PushToServer(int userID, System.Action<bool> whendone=null)
        {
            var user = StudentRepo.Instance.GetUser(userID) as F360Student;
            if(user != null)
            {
                if(user.Stats.isReadyToSynch())
                {
                    F360Client client;
                    if(GetClient(out client))
                    {
                        var tbuffer = GetTermBuffer();
                        tbuffer.AddRange(user.Stats.GetChangedValues());
                        if(tbuffer.Count > 0)
                        {
                            var sbuffer = GetStringBuffer(tbuffer.Count);
                            foreach(var term in tbuffer)
                            {
                                sbuffer.Add(term.ToYaml());
                            }

                            //  push terms
                            
                        }
                    }
                }
            }
            else
            {   
                Debug.LogWarning("StatRepo:: UserID<" + userID + "> does not exist!");
            }
            return false;
        }

        
        //-----------------------------------------------------------------------------------------------

        void onReceiveServerData(SC_PullUserDataResponse response)
        {
            if(response.isValid())
            {
                //  get user
                var user = StudentRepo.Instance.GetUser(response.userID) as F360Student;
                var buffer = GetTermBuffer(response.uris.Length);
                if(user != null)
                {
                    for(int i = 0; i < response.uris.Length; i++)
                    {
                        var data = StatSerialization.ParseSynchTerm(response.uris[i], response.payload[i]);
                        if(data != null)
                        {
                            buffer.Add(data);
                        }
                        else
                        {
                            Debug.LogWarning("StatRepo:: unable to parse synchTerm<" + response.uris[i] + ">:\n\nyaml:\n" + response.payload[i]);
                        }
                    }
                    //  unpack
                    user.Stats.UpdateFromServer(buffer);
                }
            }
            else
            {
                Debug.LogWarning("StatRepo:: pulled data is not valid, uri->payload mismatch.");
            }
        }


        void onReceivePushReponse(SC_PushUserDataResponse response)
        {
            var user = StudentRepo.Instance.GetUser(response.userID) as F360Student;
            if(user != null)
            {
                user.Stats.Meta.LastSynchTime = Backend.TimeUtil.ServerTime;
            }
        }



        bool GetClient(out F360Client client)
        {
            client = F360Backend.GetClient<F360Client>();
            if(client == null)
            {
                Debug.LogWarning("StatRepo:: Backend Client unavailable!");
                return false;
            }
            else
            {
                return true;
            }
        }


        //-----------------------------------------------------------------------------------------------

        //  buffers


        List<ISynchedTerm> __termBuffer;
        List<string> __stringBuffer;
        
        List<ISynchedTerm> GetTermBuffer(int count=10)
        {
            if(__termBuffer ==  null) {
                __termBuffer = new List<ISynchedTerm>(count);
            }
            else {
                __termBuffer.Clear();
            }
            return __termBuffer;
        }
        List<string> GetStringBuffer(int count=10)
        {
            if(__stringBuffer ==  null) {
                __stringBuffer = new List<string>(count);
            }
            else {
                __stringBuffer.Clear();
            }
            return __stringBuffer;
        }

    }



}

