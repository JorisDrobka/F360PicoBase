using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using UnityEngine;

using F360.Backend;
using F360.Backend.Messages;
using F360.Backend.Synch;


namespace F360.Users
{


    [Serializable]
    public abstract class User : ISerializable
    {

        protected static bool logging = true;


        /// @brief
        /// defines URI used to locate server-side database resources.
        /// user name is used as uri pointer in database when saving user-specific data
        ///
        protected static string DEFINE_DATABASE_URI(User u) 
        { 
            return u.Database.GetURI() + SynchedURI.URI_LEAD + u.name + "/"; 
        }        


        //-----------------------------------------------------------------------------------------------------------------
    
        //  base cachable properties
        
        [SerializeField] int id = -1;
        [SerializeField] protected string name;
        [SerializeField] string pw;
        [SerializeField] string mail;
        [SerializeField] bool activated;
        [SerializeField] bool archived;
        [SerializeField] string parentAccount;
        
        [SerializeField] DateTime time_created;
        [SerializeField] DateTime time_login;

        //  runtime properties
        protected bool _dirtyFlag;

        //internal SynchList SynchedData { get; private set; }

        //-----------------------------------------------------------------------------------------------------------------


        //  interface

        public int ID { get { return id; } }          
        public virtual string Name { get { return name; } }
        public string Mail { get { return mail; } }

        /// @brief
        /// the account that created this user
        ///
        public string CreatedBy { get { return parentAccount; } }
        public DateTime Created { get { return time_created; } }
        public DateTime LastLogin { get { return time_login; } }
        
        /// @brief
        /// flag set if user synched data once in this session
        ///
        public bool SynchedOnce { get; set; }

        /// @brief
        /// last time users personal settings where synched (runtime only)
        ///
        public DateTime LastSynchTime { 
            get { return _lastSynchT; } 
            set {
                _lastSynchT = value;
                //SynchedData.SetValue(SYNCH_LAST_SYNCH_T, value);
            } 
        }
        internal DateTime _lastSynchT;


        /// @brief
        /// archived users are not considered in vast areas of the system
        ///
        public bool Archived { get { return archived; } }


        /// @brief
        /// true if account was activated via mail once
        ///
        public virtual bool Activated { get { return activated; } }

        public string PathToProfilePicture { get; set; }        //  maybe use lazy loading helper obj?

        public abstract UserType Type { get; }
        public abstract Database Database { get; }

        //-----------------------------------------------------------------------------------------------------------------

        public bool isActive()
        {
            return this.Activated && !this.archived;
        }

        public void SetUserID(int id)
        {
            if(this.id != id)
            {
                this.id = id;
                _dirtyFlag = true;
                if(logging) Debug.Log(name + RichText.darkRed(" SetUserID() _dirtyFlag=") + RichText.emph("true"));
            }
        }    



        //-----------------------------------------------------------------------------------------------------------------

        //  restricted interface

        

        internal string Pass { get { return pw; } }
        
        public virtual bool isDirty { get { return _dirtyFlag /*|| SynchedData.isDirty()*/; } internal set { _dirtyFlag = value; } }    

        public void SetDirty() { _dirtyFlag = true; if(logging) Debug.Log(name + RichText.darkRed(" SetDirty() _dirtyFlag=") + RichText.emph("true")); }
        public void SetPass(string pass)
        {
            this.pw = pass;
            _dirtyFlag = true;
            if(logging) Debug.Log(name + RichText.darkRed(" SetPass() _dirtyFlag=") + RichText.emph("true"));
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  creation

        public static TUser Create<TUser>(string user, string pass, string mail="") where TUser : User, new()
        {
            var u = new TUser();
            u.name = user;
            u.pw = pass;
            u.mail = mail;
            u.time_created = DateTime.Now;
            u.time_login = DateTime.Now;
            //u.SynchedData = new SynchList();
            //u.initSynchData(u.SynchedData);
            //if(logging) Debug.Log(RichText.color("CREATED USER", Color.magenta) + "(" + typeof(TUser) + ")=[" + user + "," + pass + "] " + (u.SynchedData != null));
            return u;
        }

        protected abstract void onCreateUser();

        internal virtual void onDeleteUser()
        {
            
        }


        //-----------------------------------------------------------------------------------------------------------------

        internal virtual void onLogin()
        {
            time_login = TimeUtil.ServerTime;
        //    _dirtyFlag = true;
        //    Debug.Log(name + RichText.darkRed(" OnLogin() _dirtyFlag=") + RichText.emph("true"));
        }



        
        //-----------------------------------------------------------------------------------------------------------------

        //  activity & status

        /// @returns activity state
        ///
        public UserActivity GetUserActivity()
        {
            if(!activated)
            {
                return UserActivity.WaitingForAccountConfirmation;
            }
            else if(archived)
            {
                return UserActivity.Disabled;
            }
            else
            {   
                return (UserActivity) GetActivityRating();
            }
        }

        public string GetStatusDescription()
        {
            if(!activated)
            {
                return "Email noch nicht bestätigt";
            }
            else if(archived)
            {
                return "archiviert";
            }
            else
            {
                return "aktiv";
            }
        }

        /// @returns a value from 0-3 to describe activity within last 2 weeks
        ///
        internal int GetActivityRating()
        {
            var now = TimeUtil.ServerTime;
            var min = now.AddDays(-14);
            var lastLogin = this.LastLogin;
            int r_activity = Mathf.RoundToInt
            (
                Utility.Math.MapF(lastLogin.Ticks, min.Ticks, now.Ticks, 0, 3)
            );
            return r_activity;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  server synching

        internal void processUpdateFromServer(Backend.Messages.SC_UserInfo info)
        {
            if(logging) Debug.Log("User<" + name + "> receives update from server:: " + info.ToString());
            this.id = info.id;
            if(string.IsNullOrEmpty(parentAccount))
            {
                this.parentAccount = info.parent;
            }
            this.mail = info.mail;
            this.archived = info.archived;
            this.activated = info.active;
            this.time_login = info.lastLogin;
            this.onUpdateFromServer(info);
        }
        protected abstract void onUpdateFromServer(Backend.Messages.SC_UserInfo info);



        //  data synching


        const string SYNCH_LAST_SYNCH_T = "_lastSynch";
        const string SYNCH_NOTES = "_notes";

        /*protected virtual void initSynchData(SynchList data)
        {
            //Debug.Log("User<" + Name + "/" + Type + "> initSynchData..  my id? " + id);

            data.RegisterValue<DateTime>(SYNCH_LAST_SYNCH_T);
        }*/

        //-----------------------------------------------------------------------------------------------------------------

        //  serialization

        protected User() 
        {
            //this.SynchedData = new SynchList();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            onSerializeData(info, context);
        }
        public User(SerializationInfo info, StreamingContext context)
        {
            onDeserializeData(info, context);
        }

        protected virtual void onSerializeData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("id", id, typeof(int));
            info.AddValue("name", name, typeof(string));
            info.AddValue("mail", mail, typeof(string));
            info.AddValue("pw", pw, typeof(string));
            info.AddValue("createdby", parentAccount, typeof(string));
            info.AddValue("created", time_created, typeof(DateTime));
            info.AddValue("login", time_login, typeof(DateTime));
            info.AddValue("active", activated, typeof(bool));
            info.AddValue("archived", archived, typeof(bool));

            /*
            //  synch data
            info.AddValue("hSynchData", (this.SynchedData != null), typeof(bool));
            if(SynchedData != null)
            {
                info.AddValue("synchdata", this.SynchedData, typeof(SynchList));
            }

            //  cached notes
            info.AddValue("cNotes", noteData, typeof(CachedNotes));
            //info.AddValue("cNotes", cachedNotes, typeof(Notifications.CachedNote[]));
            */
            
        }
        protected virtual void onDeserializeData(SerializationInfo info, StreamingContext context)
        {
            id              = (int) info.GetInt32("id");
            name            = (string) info.GetString("name");
            mail            = (string) info.GetString("mail");
            pw              = (string) info.GetString("pw");
            parentAccount   = (string) info.GetString("createdby");
            time_created    = (DateTime) info.GetValue("created", typeof(DateTime));
            time_login      = (DateTime) info.GetValue("login", typeof(DateTime));
            activated       = (bool) info.GetBoolean("active");
            archived        = (bool) info.GetBoolean("archived");

            /*bool hasSynchData = (bool) info.GetBoolean("hSynchData");
            if(hasSynchData)
            {
                this.SynchedData = (SynchList) info.GetValue("synchdata", typeof(SynchList));
            }
            else
            {
                this.SynchedData = new SynchList();
            }
            initSynchData(this.SynchedData);


            noteData = (CachedNotes) info.GetValue("cNotes", typeof(CachedNotes));*/

            /*var cachedN = (Notifications.CachedNote[]) info.GetValue("cNotes", typeof(Notifications.CachedNote[]));
            if(cachedN != null)
            {
                cachedNotes.Clear();
                cachedNotes.AddRange(cachedN);

                Debug.Log("USER Deserialzízed " + cachedNotes.Count + " cached notes!!");
            }*/
        }



        

        
    }


}

