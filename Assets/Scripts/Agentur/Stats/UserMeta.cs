using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Text;
using System.IO;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;


namespace F360.Users.Stats
{

    /* ######### ILUME ##########
    
        Muss serialisiert & synchronisiert werden
        Nach Deserialisierung bitte OnAfterDeserialize() aufrufen.

    */


    


    /// @brief
    /// synchable user data
    ///
    public class UserMeta : ISynchable
    {
        
        //-----------------------------------------------------------------------------------------------
        //
        //  serialized data

        /// ###   SERIALIZE & SYNCH   ### 

        DateTime synch_T;
        DateTime change_T;
        DateTime bronze_T;
        DateTime silver_T;
        DateTime gold_T;
        int m_vTrainer;
        int m_driveVr;
        int m_exam;
        int m_menu;
        string r_device;

        List<int> visitedDriveVRTasks;


        public void ReadSerializedData(SerializedUserMetaData data)
        {
            StatSerializationUtil.TryParseTime(data.synchT, ref synch_T);
            StatSerializationUtil.TryParseTime(data.changeT, ref change_T);
            StatSerializationUtil.TryParseTime(data.bronzeT, ref bronze_T);
            StatSerializationUtil.TryParseTime(data.silverT, ref silver_T);
            StatSerializationUtil.TryParseTime(data.goldT, ref gold_T);
            m_vTrainer = data.vTrainerT;
            m_driveVr = data.driveVRT;
            m_exam = data.examT;
            m_menu = data.menuT;
            r_device = data.rentedDevice;
            int[] visitedIds = data.GetVisitedDriveVRTasks();
            if(visitedDriveVRTasks != null)
            {
                visitedDriveVRTasks.Clear();
                visitedDriveVRTasks.AddRange(visitedIds);
            }
            else
            {
                visitedDriveVRTasks = new List<int>(visitedIds);
            }
        }

        ISynchedTerm ISynchable.GetSynchTerm()
        {
            return new SerializedUserMetaData(this);
        }

        //-----------------------------------------------------------------------------------------------

        public DateTime LastChangedTime { 
            get { return change_T; }
        }

        public DateTime LastSynchTime {
            get { return synch_T; }
            set { synch_T = value; onChangedValue(); }
        }

        public DateTime BronzeAttemptTime {
            get { return bronze_T; }
            set { bronze_T = value; onChangedValue(); }
        }
        public DateTime SilverAttemptTime {
            get { return silver_T; }
            set { silver_T = value; onChangedValue(); }
        }
        public DateTime GoldAttemptTime {
            get { return gold_T; }
            set { gold_T = value; onChangedValue(); }
        }
        
        public int Minutes_VTrainer {
            get { return m_vTrainer; }
            set { m_vTrainer = value; onChangedValue(); }
        }
        public int Minutes_DriveVR {
            get { return m_driveVr; }
            set { m_driveVr = value; onChangedValue(); }
        }
        public int Minutes_Exam {
            get { return m_exam; }
            set { m_exam = value; onChangedValue(); }
        }

        /// @brief
        /// interaction time in menu without device sleeping
        public int Minutes_Menu {
            get { return m_menu; }
            set { m_menu = value; onChangedValue(); }
        }        

        /// @brief
        /// device id bound to this user
        public string Rented_Device {
            get { return r_device; }
            set { r_device = value; onChangedValue(); }
        }

        //-----------------------------------------------------------------------------------------------

        /// @returns wether driveVR task was already seen by user
        ///
        public bool hasVisitedDriveVRSession(int videoID)
        {
            return visitedDriveVRTasks.Contains(videoID);
        }

        /// @returns videoIDs of all driveVR tasks the user has already seen
        ///
        public IEnumerable<int> GetVisitedDriveVRSessions()
        {
            return visitedDriveVRTasks;
        }

        /// @brief
        /// call to mark a driveVR session as seen by user
        ///
        public void AddVisitedDriveVRTask(int videoID)
        {
            if(!visitedDriveVRTasks.Contains(videoID))
            {
                visitedDriveVRTasks.Add(videoID);
            }
        }

        //-----------------------------------------------------------------------------------------------

        public int Minutes_Total { get { return Minutes_VTrainer + Minutes_DriveVR + Minutes_Exam; } }


        //-----------------------------------------------------------------------------------------------

        //  ISynchable interface

        string ISynchable.SynchUri { get { return SynchURI.USER_META; } }
        DateTime ISynchable.Time_Created { get { return LastSynchTime; } }


        void onChangedValue()
        {
            change_T = Backend.TimeUtil.ServerTime;
        }


        //-----------------------------------------------------------------------------------------------

        public void UpdateFromServer(SerializedUserMetaData data)
        {
            ReadSerializedData(data);
            synch_T = Backend.TimeUtil.ServerTime;
            change_T = synch_T;
        }
    }


}

