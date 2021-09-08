using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using F360.Users.Internal;

using F360.Backend;
using F360.Backend.Synch;
using F360.Backend.Messages;

namespace F360.Users
{

    public enum LoginState
    {
        MissingUser,
        CreatedUser,
        Success,
        WrongPass,

        Connection_Exception,
        Timeout_Exception,
        Not_Allowed
    }

    public delegate void LoginHandler(string user, LoginState result);

    public static class LoginUtil
    {
        public static bool hasError(this LoginState state)
        {
            switch(state)
            {
                case LoginState.MissingUser:
                case LoginState.WrongPass:
                case LoginState.Connection_Exception:
                case LoginState.Timeout_Exception:      
                case LoginState.Not_Allowed:            return true;
                default:                                return false;
            }
        }
    }

    /// @brief
    /// define which parts of the database should be cache locally on device
    ///
    /// NoCaching - requires server connection for all data
    /// UserData  - cache user profiles & login data to allow offline working. Changes are cached until next connection
    /// Full      - store all data locally
    ///
    public enum CacheMode
    {
        NoCaching=0,
        UserProfiles,  
        Full
    }


    public struct UserSynchInfo
    {
        public User user;
        public SynchState state;
        public SC_UserInfo data;

        public UserSynchInfo(User user, SynchState state, SC_UserInfo data) {
            this.user = user;
            this.state = state;
            this.data = data;
        }
    }
    
    //-----------------------------------------------------------------------------------------------------------------

    public interface IUserRepo
    {
        Database Database { get; }

        bool UserExists(string user);
        User GetUser(string user);
        void Login(string user, string pass, LoginHandler handler, bool createNew=false, bool validatedByServer=false);
        bool Logout();
    }



    //-----------------------------------------------------------------------------------------------------------------




    /// @brief
    /// base class for user repositories that can be cached on disk or synchronized with a server.
    ///
    public abstract class UserRepo<TUser> : MonoBehaviour, IUserRepo, ISynchronizedRepo where TUser : User, new()
    {

        internal static bool logging = true;



        //  static access
        public static TUser GetCurrent() { return currentUser; }

        static TUser currentUser;


        //  constructor

        protected static TRepo CreateRepo<TRepo>() where TRepo : Component, IUserRepo
        {
            GameObject go = new GameObject("UserRepo<" + typeof(TRepo).Name + ">");
            return go.AddComponent<TRepo>();
        }


        //  interface

        /// @brief
        /// Call on app start to setup the repository
        ///
        /// @params useCache enable caching of user data on local device. If disabled, internet connection is mandatory.
        ///
        public bool Init(Database target, string name, CacheMode caching, string pathToFile)
        {
            if(!initialized)
            {
                initialized = true;
                Caching = caching;
                repository = new UserCache(target, name);
                ActiveUser.addRepo(this);

                var client = F360Backend.GetClient<IDataSynchClient>();
                client.AddRepo(this);

                if(caching != CacheMode.NoCaching)
                {
                    if(!repository.LoadFromCache(pathToFile, this.FileName))
                    {
                        Debug.LogWarning("UserRepo<" + typeof(TUser).Name + ">:: no cached userdata found");
                    }
                    else
                    {
                        //Debug.Log("REPO<" + this.GetType().Name + "> available!! " + repository.Count + " users. path=[" + pathToFile + "] FileName=[" + this.FileName + "]");
                        OnRepoAvailable();
                    }
                }
            }  
            return repository != null;
        }


        public abstract Database Database { get; }
        public abstract int Identity { get; }

        protected abstract string FileName { get; }

        public CacheMode Caching { get; private set; }

        public int Count()
        {
            return repository != null ? repository.Count : 0;
        }

        public bool UserExists(string user)
        {
            return repository != null && repository.Exists(user);
        }
        public bool UserExists(System.Func<TUser, bool> predicate) {
            if(repository != null) {
                foreach(var u in repository.GetAll()) {
                    var t = u as TUser;
                    if(t != null && predicate(t)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public User GetUser(string user)
        {
            return repository?.Get(user);
        }
        public User GetUser(int id)
        {
            return repository?.Get(id);
        }

        public bool CreateUser(string name, string mail, string pass="")
        {
            if(repository != null && !repository.Exists(name))
            {
                var user = User.Create<TUser>(name, pass, mail);
                return repository.Add(user);
            }
            return false;
        }

        public bool DeleteUser(string user)
        {
            return repository != null && repository.DeleteUser(user);
        }

        public void Login(string user, string pw, LoginHandler handler, bool createIfNotFound=true, bool validatedByServer=false)
        {
//            Debug.Log("UserRepo.Login >>>>  [" + user + "][" + pw + "] createIfNotFound? " + createIfNotFound);
            if(!createIfNotFound && repository != null && !repository.Exists(user))
            {
                handler?.Invoke(user, LoginState.MissingUser);
            }
            else
            {
                processLogin(user, pw, handler, createIfNotFound, validatedByServer);
            }
        }

        public bool Logout()
        {
            if(ActiveUser.Current != null && ActiveUser.Current is TUser)
            {
                repository?.SaveToCache();
                currentUser = null;
                ActiveUser.Current = null;
                return true;
            }
            return false;
        }

        public bool Save()
        {
//            Debug.Log("AFTER SYNCH... SAVE ??????? " + (Caching != CacheMode.NoCaching));
            if(Caching != CacheMode.NoCaching)
            {   
                var b = repository != null && repository.SaveToCache();
//                Debug.Log(this.GetType().Name + " Save! " + RichText.emph(b.ToString()));
                return b;
            }
            else
            {
                Debug.LogWarning("User repository cannot be cached locally! cacheMode=[" + Caching + "]");
                return false;
            }
        }

        public List<UserSynchInfo> UpdateFromServer(IEnumerable<SC_UserInfo> infos)
        {   
            if(infos != null)
            {
                return new List<UserSynchInfo>(processUpdateFromServer(infos));
            }
            else
            {
                return new List<UserSynchInfo>();
            }
        }

        public SynchState UpdateFromServer(SC_UserInfo info)
        {
            return receiveUserFromServer(info).state;
        }

        //-----------------------------------------------------------------------------------------------------------------

        //  restricted interface

        internal IEnumerable<TUser> GetAll()
        {
            if(repository != null)
            {   
                foreach(var u in repository.GetAll())
                {
                    var t = u as TUser;
                    if(t != null) yield return t;
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------

        /// called either on init when loading from cache or after server connection was established
        protected virtual void OnRepoAvailable()
        {

        }

        protected virtual void OnLoginSuccess(string user)
        {
            if(logging)
            {
                Debug.Log("UserRepo<" + this.GetType().Name + "> loginSuccess... user=[" + user + "] exists? " + repository.Exists(user));
            }
            ActiveUser.Current = repository?.Get(user);
            currentUser = ActiveUser.Current as TUser;
            UnityEngine.Assertions.Assert.IsNotNull(ActiveUser.Current, "UserRepo: successful login, but userdata is null!");
        }


        //-----------------------------------------------------------------------------------------------------------------

        //  internal

        protected UserCache repository;
        bool initialized = false;


        void OnApplicationQuit()
        {
            if(repository != null)
            {
                repository.SaveToCache();
            }
        }

        void processLogin(string user, string pw, LoginHandler handler, bool createIfNotFound, bool validatedByServer)
        {
            if(logging)
            {
                Debug.Log("UserRepo.processLogin=[" + user + ", " + pw + "].. createIfNotFound: " + createIfNotFound + " server login? " + validatedByServer
                            + "\nrepo? " + (repository != null ? ("has user... " + repository.Exists(user).ToString()) : "null") + "\ncaching? " + Caching);
            }
            if(repository != null)
            {
                if(repository.Exists(user))
                {
                    if(repository.CheckPassword(user, pw))
                    {   
                        if(logging)
                        {
                            Debug.Log("UserRepo.processlogin PW success user=[" + user + "] data=[" + (repository.Get(user) != null) + "]  in Repo? " + repository.Count);
                        }

                        var usr = repository.Get(user);
                        usr.onLogin();
                        handler?.Invoke(user, LoginState.Success);
                        OnLoginSuccess(user);
                    }
                    else if(validatedByServer)
                    {
                        //  override cached pw
                        if(logging)
                        {
                            Debug.Log("UserRepo.processlogin override PW! user=[" + user + "] data=[" + (repository.Get(user) != null) + "]  in Repo? " + repository.Count);
                        }

                        var usr = repository.Get(user);
                        usr.SetPass(pw);
                        usr.onLogin();
                        handler?.Invoke(user, LoginState.Success);
                        OnLoginSuccess(user);
                    }
                    else
                    {
                        handler?.Invoke(user, LoginState.WrongPass);
                    }
                }
                else if(createIfNotFound)
                {
                    if(Caching != CacheMode.NoCaching)
                    {
                        var usr = User.Create<TUser>(user, pw);
                        repository.Add(usr);
                        usr.onLogin();
                        handler?.Invoke(user, LoginState.CreatedUser);
                        OnLoginSuccess(user);
                    }
                    else
                    {
                        handler?.Invoke(user, LoginState.MissingUser);
                    }
                }
                else
                {
                    handler?.Invoke(user, LoginState.MissingUser);
                }
            }
            else
            {
                handler?.Invoke(user, LoginState.Not_Allowed);
            }
        }


        IEnumerable<UserSynchInfo> processUpdateFromServer(IEnumerable<SC_UserInfo> list)
        {
            if(repository != null)
            {
                int c = 0;
                foreach(var info in list)
                {
                    if(info.identity == Identity)
                    {
                        var nfo = receiveUserFromServer(info);
                        if(nfo.state == SynchState.Created || nfo.state == SynchState.Updated)
                        {
                            c++;
                            yield return nfo;
                        }
                    }
                }

                List<SC_UserInfo> fromServer = new List<SC_UserInfo>(list);
                List<User> users = new List<User>(repository.GetAll());
                for(int i = 0; i < users.Count; i++)
                {
                    if(!fromServer.Exists(x=> x.name==users[i].Name))
                    {
                        if(repository.DeleteUser(users[i].Name))
                        {
                            if(logging)
                            {
                                Debug.Log(RichText.emph(this.GetType().Name) + " deleted user<" + users[i].Name + ">");
                            }
                            users[i].onDeleteUser();
                            yield return new UserSynchInfo(users[i], SynchState.Deleted, null);
                            users.RemoveAt(i); i--;
                            c++;
                        }
                        else
                        {
                            Debug.LogWarning("UserRepo<" + this.GetType().Name + "> FAILED to delete user <" + users[i].Name + ">");
                        }
                    }
                }

                Debug.Log("UserRepo.UpdateFromServer... changed: " + c);

                if(c > 0)
                {
                    onAfterUpdateFromServer();
                    if(repository != null)
                    {
                        repository.SetDirty();
                    }
                    Save();
                }

                for(int i = 0; i < users.Count; i++)
                {
                    users[i].isDirty = false;
                }

                if(logging)
                {
                    Debug.Log("UserRepo<" + this.GetType().Name + "> updated from server => " + c + " updates!");
                }
            }
        }
        protected virtual void onAfterUpdateFromServer() {}


        internal UserSynchInfo receiveUserFromServer(SC_UserInfo info)
        {
            if(logging)
            {
                Debug.Log("\t...receive user=[" + info.name + "] from server..  exists? " + (repository != null && repository.Exists(info.name)));
            }
            if(repository == null)
            {
                return new UserSynchInfo(null, SynchState.Error_Invalid_Repo, null);
            }

            TUser usr = null;
            bool created = false;
            if(repository.Exists(info.name))
            {
                usr = repository.Get(info.name) as TUser;
            }
            else
            {
                //  new user
                usr = creatorFunc(info);
                repository.Add(usr);
                created = true;
            }
            if(usr != null)
            {
                usr.SetUserID(info.id);
                if(created || usr.LastLogin <= info.lastLogin)
                {
                    usr.isDirty = true;
                    usr._lastSynchT = TimeUtil.ServerTime;
                    usr.processUpdateFromServer(info);
                    serverUpdateFunc(usr, info);
                    return new UserSynchInfo(usr, SynchState.Updated, info);
                }
                else
                {
                    return new UserSynchInfo(usr, SynchState.Unchanged, info);
                }
            }
            else
            {
                return new UserSynchInfo(null, SynchState.Error_Missing_Key, info);
            }
        }

        protected abstract TUser creatorFunc(SC_UserInfo info);

        protected abstract bool serverUpdateFunc(TUser user, SC_UserInfo info);


        //-----------------------------------------------------------------------------------------------------------------

        //  data synching

        public string GetName()
        {
            return "UserRepo<" + typeof(TUser).Name + ">";
        }

        bool ISynchronizedRepo.isDirty()
        {
            if(repository != null)
            {
                foreach(var u in repository.GetAll())
                {
                    if(u.isDirty) return true;
                }
            }
            return false;
        }

        bool ISynchronizedRepo.MatchURI(SynchedURI uri)
        {
            return checkKeyMap(uri.key);
        }

        IEnumerable<ISynchronizedData> ISynchronizedRepo.GetChanges()
        {
            if(repository != null)
            {
                foreach(var u in repository.GetAll())
                {
                    if(u.ID != -1)
                    {
                        /*foreach(var c in u.SynchedData.GetChanges(this.Database, u.ID))
                        {
                            yield return c;
                        }*/

                        yield return default(ISynchronizedData);
                    }
                }
            }
        }
        SynchState ISynchronizedRepo.PushChange(ISynchronizedData d)
        {
            if(repository == null)
            {
                return SynchState.Error_Invalid_Repo;
            }
            else
            {
                var c = 0;
                /*foreach(var u in repository.GetAll())
                {
                    var s = u.SynchedData.UpdateFromServer(d);
                    Debug.Log("UserRepo.PushChange<" + d.Uri.key + "> to <" + u.Name + ">..? " + s);
                    if(s == SynchState.Updated)
                    {
                        u._lastSynchT = TimeUtil.ServerTime;
                        c++;
                    }
                }*/
                if(c > 0)
                {
                    repository.SetDirty();
                }
                return c > 0 ? SynchState.Updated : SynchState.Unchanged;
            }
        }
        void ISynchronizedRepo.ClearChanges()
        {
            if(repository != null)
            {
                if(logging)
                {
                    Debug.Log(GetName() + " ...Clear Changes after push!");
                }
                foreach(var u in repository.GetAll())
                {
                    //u.SynchedData.ClearChanges();
                    u.isDirty = false;
                }
            }
        }   


        bool checkKeyMap(string key)
        {
            if(repository != null)
            {
                /*var all = repository.GetAll();
                var u = all.FirstOrDefault();
                if(u != null)
                {
                    return u.SynchedData.hasKey(key);
                }*/
            }
            return false;
        }
    }



}
