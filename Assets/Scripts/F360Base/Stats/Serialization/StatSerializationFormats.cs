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


    //-----------------------------------------------------------------------------------------------
    //
    //  VTrainer Chapter
    //      
    //-----------------------------------------------------------------------------------------------

    public struct SerializedVTrainerChapter : ISerializable, ISynchedTerm
    {
        public short chapter { get; set; }
        public string created { get; set; }
        public short[] ratings { get; set; }

        public SerializedVTrainerChapter(VTrainerStats stats)
        {
            chapter = (short)stats.ChapterID;
            created = StatSerializationUtil.FormatTimeString(stats.Time_Created);
            ratings = StatSerializationUtil.ConvertToShortArray(stats.lectureRatings);
        }

        public SerializedVTrainerChapter(SerializationInfo info, StreamingContext context)
        {
            chapter = info.GetInt16("chapter");
            created = info.GetString("created");
            ratings = (short[]) info.GetValue("ratings", typeof(short[]));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("chapter", chapter, typeof(short));
            info.AddValue("created", created, typeof(string));
            info.AddValue("ratings", ratings, typeof(short[]));
        }

        public static string Serialize(VTrainerStats stats)
        {
            return Serialize(new SerializedVTrainerChapter(stats));
        }
        public static string Serialize(SerializedVTrainerChapter data)
        {
            var serializer = new SerializerBuilder().Build();
            try {
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                serializer.Serialize(sw, data);
                return sb.ToString();
            }
            catch(Exception ex) {
                Debug.LogError("Failed to serialize VTrainerChapter yaml: " + ex);
                return "";
            }
        }

        public static bool Deserialize(string data, out SerializedVTrainerChapter chapter)
        {
            var serializer = new DeserializerBuilder().Build();
            try {
                chapter = serializer.Deserialize<SerializedVTrainerChapter>(data);
                return true;
            }
            catch(Exception ex) {
                Debug.LogError("Failed to deserialize VTrainerChapter yaml: " + ex);
                chapter = default(SerializedVTrainerChapter);
                return false;
            }
        }


        string ISynchedTerm.SynchUri { get { return SynchURI.CreateVTrainerChapter(chapter); } }
        DateTime ISynchedTerm.GetTimeCreated()
        {
            DateTime t = default(DateTime);
            StatSerializationUtil.TryParseTime(created, ref t);
            return t;
        }
        string ISynchedTerm.ToYaml()
        {
            return Serialize(this);
        }
    }


    

    //-----------------------------------------------------------------------------------------------
    //
    //  DRIVE SESSION
    //      
    //-----------------------------------------------------------------------------------------------
    
    public struct SerializedDriveSession : ISerializable, ISynchedTerm
    {
        public string uri { get; set; }
        public string created { get; set; }
        public short video { get; set; }
        public short[] looks { get; set; }
        public short[] hazards { get; set; }

        public bool isValid()
        {
            return !string.IsNullOrEmpty(uri);
        }

        public SerializedDriveSession(DriveSessionStats stats)
        {
            uri = stats.SynchUri;
            created = StatSerializationUtil.FormatTimeString(stats.Time_Created);
            video = (short) stats.VideoID;
            looks = StatSerializationUtil.ConvertToShortArray(stats.l_ratings);
            hazards = StatSerializationUtil.ConvertToShortArray(stats.h_ratings);
        }

        public SerializedDriveSession(SerializationInfo info, StreamingContext context)
        {
            uri = info.GetString("uri");
            created = info.GetString("createT");
            video = info.GetInt16("vid");
            
            looks = (short[]) info.GetValue("aw", typeof(short[]));
            hazards = (short[]) info.GetValue("hz", typeof(short[]));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("uri", uri, typeof(string));
            info.AddValue("created", created, typeof(string));
            info.AddValue("vid", video, typeof(short));
            info.AddValue("aw", looks, typeof(short[]));
            info.AddValue("hz", hazards, typeof(short));
        }

        public static string Serialize(DriveSessionStats session)
        {
            return Serialize(new SerializedDriveSession(session));
        }
        public static string Serialize(SerializedDriveSession data)
        {
            var serializer = new SerializerBuilder().Build();
            try {
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                serializer.Serialize(sw, data);
                return sb.ToString();
            }
            catch(Exception ex) {
                Debug.LogError("Failed to serialize DriveSession yaml: " + ex);
                return "";
            }
        }

        public static bool Deserialize(string data, out SerializedDriveSession session)
        {
            var serializer = new DeserializerBuilder().Build();
            try {
                session = serializer.Deserialize<SerializedDriveSession>(data);
                return true;
            }
            catch(Exception ex) {
                Debug.LogError("Failed to deserialize DriveSession yaml: " + ex);
                session = default(SerializedDriveSession);
                return false;
            }
        }

        string ISynchedTerm.SynchUri { get { return uri; } }
        DateTime ISynchedTerm.GetTimeCreated()
        {
            DateTime t = default(DateTime);
            StatSerializationUtil.TryParseTime(created, ref t);
            return t;
        }
        string ISynchedTerm.ToYaml()
        {
            return Serialize(this);
        }
    }



    //-----------------------------------------------------------------------------------------------
    //
    //  USER META
    //      
    //-----------------------------------------------------------------------------------------------

    /// @brief
    /// helper object to serialize user metadata (yaml and binary)
    ///
    public struct SerializedUserMetaData : ISerializable, ISynchedTerm
    {
        public string synchT { get; set; }
        public string changeT { get; set; }

        public string bronzeT { get; set; }
        public string silverT { get; set; }
        public string goldT { get; set; }

        public int vTrainerT { get; set; }
        public int driveVRT { get; set; }
        public int examT { get; set; }
        public int menuT { get; set; }
        public string rentedDevice { get; set; }

        private short[] visitedDriveVRTasks { get; set; }       //  only serialize, prevent synch

        public int[] GetVisitedDriveVRTasks()
        {
            return StatSerializationUtil.ConvertToIntArray(visitedDriveVRTasks);
        }

        public SerializedUserMetaData(UserMeta meta)
        {
            synchT = StatSerializationUtil.FormatTimeString(meta.LastSynchTime);
            changeT = StatSerializationUtil.FormatTimeString(meta.LastChangedTime);
            bronzeT = StatSerializationUtil.FormatTimeString(meta.BronzeAttemptTime);
            silverT = StatSerializationUtil.FormatTimeString(meta.SilverAttemptTime);
            goldT = StatSerializationUtil.FormatTimeString(meta.GoldAttemptTime);
            vTrainerT = meta.Minutes_VTrainer;
            driveVRT = meta.Minutes_DriveVR;
            examT = meta.Minutes_Exam;
            menuT = meta.Minutes_Menu;
            rentedDevice = meta.Rented_Device;
            visitedDriveVRTasks = StatSerializationUtil.ConvertToShortArray(meta.GetVisitedDriveVRSessions());
        }


        public SerializedUserMetaData(SerializationInfo info, StreamingContext context)
        {
            synchT = info.GetString("synch");
            changeT = info.GetString("change");
            bronzeT = info.GetString("bronze");
            silverT = info.GetString("silver");
            goldT = info.GetString("gold");
            vTrainerT = info.GetInt16("vt");
            driveVRT = info.GetInt16("dvr");
            examT = info.GetInt16("ex");
            menuT = info.GetInt16("menu");
            rentedDevice = info.GetString("rent");
            visitedDriveVRTasks = (short[]) info.GetValue("dvrvisit", typeof(short[]));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("synch", synchT, typeof(string));
            info.AddValue("change", changeT, typeof(string));
            info.AddValue("bronze", bronzeT, typeof(string));
            info.AddValue("silver", silverT, typeof(string));
            info.AddValue("gold", goldT, typeof(string));
            info.AddValue("vt", vTrainerT, typeof(short));
            info.AddValue("dvr", driveVRT, typeof(short));
            info.AddValue("ex", examT, typeof(short));
            info.AddValue("menu", menuT, typeof(short));
            info.AddValue("rent", rentedDevice, typeof(string));
            info.AddValue("dvrvisit", visitedDriveVRTasks, typeof(short[]));
        }

        

        public static string Serialize(UserMeta meta)
        {
            return Serialize(new SerializedUserMetaData(meta));
        }
        public static string Serialize(SerializedUserMetaData yaml)
        {
            var serializer = new SerializerBuilder().Build();
            try {
                var sb = new StringBuilder();
                var sw = new StringWriter(sb);
                serializer.Serialize(sw, yaml);
                return sb.ToString();
            }
            catch(Exception ex) {
                Debug.LogError("Failed to serialize UserMeta yaml: " + ex);
                return "";
            }
        }

        public static bool Deserialize(string yaml, out SerializedUserMetaData data)
        {
            var serializer = new DeserializerBuilder().Build();
            try {
                data = serializer.Deserialize<SerializedUserMetaData>(yaml);
                return true;
            }
            catch(Exception ex) {
                Debug.LogError("Failed to deserialize UserMeta yaml: " + ex);
                data = default(SerializedUserMetaData);
                return false;
            }
        }

        string ISynchedTerm.SynchUri { get { return SynchURI.USER_META; } }
        DateTime ISynchedTerm.GetTimeCreated()
        {
            DateTime t = default(DateTime);
            StatSerializationUtil.TryParseTime(synchT, ref t);
            return t;
        }
        string ISynchedTerm.ToYaml()
        {
            return Serialize(this);
        }
    }

}