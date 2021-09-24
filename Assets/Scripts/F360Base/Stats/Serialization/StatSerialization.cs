using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


using System.Text;
using System.IO;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;


using DeviceBridge.Serialization;

namespace F360.Users.Stats
{


    /// @brief
    /// Userdata Serialization functions
    ///
    public static class StatSerialization
    {

        //  serialization

        public static bool WriteUserStatsToDisk(string path, UserStats stats)
        {
            FileStream file=null;
            try {

                if(File.Exists(path))
                {
                    file = File.OpenWrite(path);
                }
                else
                {
                    file = File.Create(path);
                }
                SavedUserStats format = new SavedUserStats();
                format.userID = stats.UserID;
                format.meta = new SerializedUserMetaData(stats.Meta);
                format.bronze = new SerializedDriveSession(stats.Exam_Bronze);
                format.silver = new SerializedDriveSession(stats.Exam_Silver);
                format.gold = new SerializedDriveSession(stats.Exam_Gold);
                
                var query = stats.GetDriveVRSessionsAll(includeArchived: true);
                var count = query.Count();
                var sessions = new SerializedDriveSession[count];
                var i = 0;
                foreach(var session in query)
                {
                    sessions[i] = new SerializedDriveSession(session);
                    i++;
                }
                format.driveVR = sessions;
                if(BinarySerializationHelper.Serialize<SavedUserStats>(file, format))
                {
                    return true;
                }
            }
            catch(IOException ex) {
                Debug.LogError("Failed to save user stats @path=[" + path + "] to disk!\n" + ex);
            }
            return false;
        }


        public static bool LoadUserStatsFromDisk(string path, out SavedUserStats data)
        {
            
            try {
                var content = File.ReadAllBytes(path);
                return BinarySerializationHelper.Deserialize<SavedUserStats>(content, out data);
            }
            catch(IOException ex) {
                Debug.LogError("Failed to save user stats @path=[" + path + "] to disk!\n" + ex);
                data = default(SavedUserStats);
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------

        //  synching


        public static ISynchedTerm ParseSynchTerm(string uri, string term)
        {
            if(uri.StartsWith(SynchURI.USER_META))
            {
                SerializedUserMetaData data;
                if(SerializedUserMetaData.Deserialize(term, out data))
                {
                    return data;
                }
            }
            else if(uri.StartsWith(SynchURI.EXAM_BRONZE))
            {
                return TryDeserializeDriveSession(term);
            }
            else if(uri.StartsWith(SynchURI.EXAM_SILVER))
            {
                return TryDeserializeDriveSession(term);
            }
            else if(uri.StartsWith(SynchURI.EXAM_GOLD))
            {
                return TryDeserializeDriveSession(term);
            }
            else
            {
                var ctx = SynchURI.GetContext(uri);
                switch(ctx)
                {
                    case TrainingContext.VRTrainer:
                        SerializedVTrainerChapter chapter;
                        if(SerializedVTrainerChapter.Deserialize(term, out chapter))
                        {
                            return chapter;
                        }
                        break;
                    case TrainingContext.DriveVR:
                        SerializedDriveSession session;
                        if(SerializedDriveSession.Deserialize(term, out session))
                        {
                            return session;
                        }
                        break;
                }
            }
            return null;
        }

        static ISynchedTerm TryDeserializeDriveSession(string term)
        {
            SerializedDriveSession data;
            if(SerializedDriveSession.Deserialize(term, out data))
            {
                return data;
            }
            return null;
        }


    }



    //-----------------------------------------------------------------------------------------------
    //
    //  FILE
    //      
    //-----------------------------------------------------------------------------------------------


    /// @brief
    /// the userdata file serialized on disk
    ///
    public struct SavedUserStats : ISerializable
    {
        public int userID;
        public SerializedUserMetaData meta;
        public SerializedVTrainerChapter[] vTrainer;
        public SerializedDriveSession[] driveVR;
        public SerializedDriveSession bronze;
        public SerializedDriveSession silver;
        public SerializedDriveSession gold;

        public SavedUserStats(SerializationInfo info, StreamingContext context)
        {
            userID = info.GetInt32("user");
            meta = (SerializedUserMetaData) info.GetValue("meta", typeof(SerializedUserMetaData));
            vTrainer = (SerializedVTrainerChapter[]) info.GetValue("vt", typeof(SerializedVTrainerChapter[]));
            driveVR = (SerializedDriveSession[]) info.GetValue("dvr", typeof(SerializedDriveSession[]));
            bronze = (SerializedDriveSession) info.GetValue("bronze", typeof(SerializedDriveSession));
            silver = (SerializedDriveSession) info.GetValue("silver", typeof(SerializedDriveSession));
            gold = (SerializedDriveSession) info.GetValue("gold", typeof(SerializedDriveSession));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("user", userID, typeof(int));
            info.AddValue("meta", meta, typeof(SerializedUserMetaData));
            info.AddValue("vt", vTrainer, typeof(SerializedVTrainerChapter[]));
            info.AddValue("dvr", driveVR, typeof(SerializedDriveSession[]));
            info.AddValue("bronze", bronze, typeof(SerializedDriveSession));
            info.AddValue("silver", silver, typeof(SerializedDriveSession));
            info.AddValue("gold", gold, typeof(SerializedDriveSession));
        }
    }


}
