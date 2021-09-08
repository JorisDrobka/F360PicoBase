using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;

using F360.Backend;

using F360.Users.Stats;

namespace F360.Users
{


    /*
    *   User-Klasse mit ausgeklammerter Datensynchronisation
    *
    */


    
    [System.Serializable]
    public class F360Student : User
    {
        public override UserType Type { get { return UserType.Student; } }

        public override Database Database { get { return Database.Students; } }

        protected override void onCreateUser()
        {
            
        }

        protected override void onUpdateFromServer(Backend.Messages.SC_UserInfo info)
        {

        }

        internal override void onDeleteUser()
        {
            //StatRepo.DeleteLocalCache(this);
        }

        //-----------------------------------------------------------------------------------------------------------------

        public F360Student() 
        {
            
        }
        public F360Student(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }


        internal override void onLogin()
        {
            base.onLogin();
            //StatRepo.LoadRepo(this);
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  interface

        /*public string RENTED_DEVICE 
        { 
        //    get { return __rentedDevice; } 
            get { return SynchedData.GetValue<string>(SYNCH_RENTED_DEVICE); } 
            private set { 
        //        __rentedDevice = value; 
                SynchedData.SetValue<string>(SYNCH_RENTED_DEVICE, value); 
            } 
        }*/
        

        /*public int OVERALL_PROGRESS             
        {
            //get { return __overallProgress; }                           //  @TODO: save in userstats?
            get { return SynchedData.GetValue<int>(SYNCH_OVERALL_PROGRESS); }
            private set { 
           //      __overallProgress = value; 
                 SynchedData.SetValue<int>(SYNCH_OVERALL_PROGRESS, value);
            } 
        }


        public UserStats OVERALL_STATS
        {
            get {
                if(__stats == null) {
                    __stats = new UserStats();
                    SynchedData.SetValue<UserStats>(SYNCH_OVERALL_STATS, __stats);
                } 
                return __stats; 
            }
            set {
                __stats = value;
                Debug.Log("Student SET Stats..");
                SynchedData.SetValue<UserStats>(SYNCH_OVERALL_STATS, value, forcePush: true);
         //       _dirtyFlag = true;
            }
        }*/
        

        /*public DateTime LAST_TEACHER_ACCESS
        { 
            //get { return __lastTeacherT; } 
            get { return SynchedData.GetValue<DateTime>(SYNCH_LAST_TEACHER_ACCESS); }
            set { 
            //    __lastTeacherT = value; 
                SynchedData.SetValue<DateTime>(SYNCH_LAST_TEACHER_ACCESS, value); 
            } 
        }*/
        

        /*public DateTime STAT_SYNCH_T 
        { 
            //get { return __statSynchT; } 
            get { return SynchedData.ServerUpdateTime; }
        }*/

        //-----------------------------------------------------------------------------------------------------------------
        
        /*public StatRepo GetStatRepo()
        {
            return StatRepo.LoadRepo(this);
        }*/

        /*public bool RentDevice(string deviceID)
        {
            //if(string.IsNullOrEmpty(__rentedDevice) 
            if(!SynchedData.hasKeyAndValue(deviceID)
                && !string.IsNullOrEmpty(deviceID))
            {
            //    __rentedDevice = deviceID;
                SynchedData.SetValue(SYNCH_RENTED_DEVICE, deviceID);
                return true;
            }
            return false;
        }*/

        /*public bool RetrieveRentedDevice()
        {
            //if(!string.IsNullOrEmpty(__rentedDevice))
            if(SynchedData.hasKeyAndValue(SYNCH_RENTED_DEVICE))
            {
                //__rentedDevice = "";
                SynchedData.SetValue(SYNCH_RENTED_DEVICE, "");
                return true;
            }
            return false;
        }*/


        //-----------------------------------------------------------------------------------------------------------------

        //  data synching

        /*const string SYNCH_OVERALL_PROGRESS = "_Overall";
        const string SYNCH_RENTED_DEVICE = "_Rented";
        const string SYNCH_LATEST_STAT = "_LatestStat";
        const string SYNCH_LAST_TEACHER_ACCESS = "_LastTeacher";
        const string SYNCH_OVERALL_STATS = "_UserStats";

        protected override void initSynchData(Backend.Synch.SynchList data)
        {
            base.initSynchData(data);

            data.RegisterValue<int>(SYNCH_OVERALL_PROGRESS);
            data.RegisterValue<int>(SYNCH_RENTED_DEVICE);
            data.RegisterValue<DateTime>(SYNCH_LATEST_STAT);
            data.RegisterValue<DateTime>(SYNCH_LAST_TEACHER_ACCESS);
            data.RegisterValue<UserStats>(SYNCH_OVERALL_STATS, (s)=> {
                __stats = s;
                Debug.Log(Name + " --> " + RichText.emph("User:: Update UserStats from SynchList!") + (s != null ? s.Readable() : "ERROR:NULL"));
            });
        }

        UserStats __stats;*/


    }

}

