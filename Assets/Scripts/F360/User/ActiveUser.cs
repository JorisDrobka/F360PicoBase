using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using F360.Backend.Synch;

namespace F360.Users
{
    
    
    /// @brief
    /// must be implemented to define how application behaves on user logins
    ///
    /*public abstract class ActiveUserModel
    {
        internal abstract bool onLoginAttempt(string usr, string pass);
        internal abstract bool onLoginSuccess(User u);
        internal abstract bool onLoginFail(string usr, string pass, int reason);
    }*/




    /// @brief
    /// Quick access to currently logged user, if any. As both students and teachers may log in
    /// (depending on the build) and they use different repos, this provides a quick way to access user
    /// without checking both.
    ///
    public static class ActiveUser
    {


        public const string DEV_ACC = ".dev";
        public const string DEV_PW = "76cY2a37R9hUD8h";

        public const string SYS_ACC = ".sys";
        const string SYS_PW = "2lP0hwaszgUKeGm";

        
        public static event System.Action<User> login;
        public static event System.Action<User> logout;
        static bool __overrideEvents = false;

        public static User Current 
        { 
            get 
            { 
                return user; 
            }
            internal set 
            {
                if(!__overrideEvents) {
                    if(user != null) 
                    {
                        logout?.Invoke(user);
                    }
                    user = value;
                    if(user != null) 
                    {
                        login?.Invoke(user);
                    }
                }
                else {
                    user = value;
                }
            } 
        }
        static User user;


        /// @brief
        /// override automatic firing of login events
        ///
        public static void OverrideLoginHandler(bool b)
        {
            __overrideEvents = b;
        }
        public static void FireUserLogin(User last=null)
        {
            Debug.Log("====> FIRE USER LOGIN EVENT " + (last != null) + " " + (Current != null) + " " + (Current != last));
            if(last != null) {
                logout?.Invoke(last);
            }
            if(Current != null) {
                login?.Invoke(Current);
            }
        }



        /// @returns wether a person is logged in (not system account)
        ///
        public static bool hasLoggedUser(bool includeSystem=false)
        {
            return Current != null 
                && (Current.Type.isPerson() || includeSystem && Current.Type == UserType.System);
        }
        public static bool hasLoggedStudent() { return Current is F360Student; }
        public static bool hasLoggedTeacher() { return false /*Current is F360Teacher*/; }
        public static bool hasLoggedAdmin() { return false /*Current is F360Admin*/; }
 

        public static void TryLogin(string username, string pass, LoginHandler handler)
        {
            /*if(Backend.VendorAPI.Exists() && DeviceBridge.DeviceAdapter.Instance.Wifi.isConnected())
            {
                //  try vendor login
                vendorLogin(username, pass, (s, r)=> {
                    foreach(var repo in repos)
                    {
                        if(repo.UserExists(username))
                        {
                            repo.Login(username, pass, handler, false);
                            return;
                        }
                    }
                    handler(s, r);
                });
            }
            else
            {*/
                Debug.Log("User::Login [" + username + "/" + pass + "]\nrepos: " + repos.Count);
                foreach(var repo in repos)
                {
                    if(repo.UserExists(username))
                    {
                        repo.Login(username, pass, handler, false);
                        return;
                    }
                }
                handler(username, LoginState.MissingUser);
        //    }
            
        }
        public static void Logout()
        {
            if(Current != null)
            {
                foreach(var repo in repos)
                {
                    if(repo.UserExists(Current.Name))
                    {
                        repo.Logout();
                        return;
                    }
                }
            }
        }

        /*public static LoginState AdminLogin(F360Admin admin)
        {
            if(admin != null)
            {
                Current = admin;
                return LoginState.Success;
            }
            return LoginState.MissingUser;
        }*/


        public static void DeveloperLogin(LoginHandler handler)
        {
            TryLogin(DEV_ACC, DEV_PW, handler);
        }

        public static void GetSystemAccount(out string name, out string password)
        {
            name = SYS_ACC;
            password = SYS_PW;
        }


        public static User GetUser(string name, UserType type)
        {
            switch(type)
            {
                /*case UserType.Admin:
                    var admin = F360Admin.Get(); 
                    if(admin.Name == name) {
                        return admin;
                    }
                    else return null;
                    
                case UserType.Teacher:

                    return TeacherRepo.Instance.GetUser(name);*/
                case UserType.Student:
                    
                    return StudentRepo.Instance.GetUser(name);
                default:

                    return null;
            }
        }

        public static User GetUser(int id)
        {
            //var admin = F360Admin.Get();
            //if(admin.ID == id) return admin;
            var student = GetUser(id, UserType.Student);
            if(student != null) return student;
            //var teacher = GetUser(id, UserType.Teacher);
            //if(teacher != null) return teacher;
            
            return null;
        }

        public static User GetUser(int id, UserType type)
        {
            switch(type)
            {
                /*case UserType.Admin:
                    var admin = F360Admin.Get(); 
                    if(admin.ID == id) {
                        return admin;
                    }
                    else return null;
                    
                case UserType.Teacher:
                    return TeacherRepo.Instance.GetUser(id);*/

                case UserType.Student:
                    return StudentRepo.Instance.GetUser(id);

                default:
                    return null;
            }
        }

        public static User FindUser(string name)
        {
            foreach(var repo in repos)
            {
                var usr = repo.GetUser(name);
                if(usr != null) return usr;
            }
            return null;
        }

        public static SynchState UpdateUser(Backend.Messages.SC_UserInfo info, User user=null)
        {
            if(info.id == -1)
            {
                return SynchState.Error_Malformatted_Data;  
            }
            if(user == null)
            {
                user = FindUser(info.name);
            }
            if(user != null && user.Type == (UserType)info.identity)
            {
                switch(user.Type)
                {
                    /*case UserType.Admin:
                        var admin = F360Admin.Get();
                        if(admin != null) {
                            admin.processUpdateFromServer(info);
                            return SynchState.Updated;
                        }
                        return SynchState.Error_Missing_Key;
                    case UserType.Teacher:
                        return TeacherRepo.Instance.receiveUserFromServer(info).state;*/
                    case UserType.Student:
                        return StudentRepo.Instance.receiveUserFromServer(info).state;
                }
                return SynchState.Error_Invalid_Repo;
            }
            return SynchState.Error_Missing_Key;
        }  

        //-----------------------------------------------------------------------------------------------------------------

        static HashSet<IUserRepo> repos = new HashSet<IUserRepo>();

        static ActiveUser()
        {
            Backend.F360Backend.clientAvailable += onClientAvailable;
        }   

        static void onClientAvailable(Backend.IClient client)
        {
            var c = client as Backend.Core.ClientBase;
            if(c != null)
            {
  //              c.authenticated += handleServerAuthentication;
            }
        }
        
        static void handleServerAuthentication(string name, string pass, int identity)
        {
            Debug.Log("handle server auth: " + name + " {" + identity  + " / " + IdentityUtil.IdentityToUserType(identity) + "}");
            if(IdentityUtil.isUserType(identity))
            {
                switch(IdentityUtil.IdentityToUserType(identity))
                {
                    /*case UserType.Admin:    
                        var admin = F360Admin.Get();
                        if(admin != null)
                        {
                            admin.onLogin();
                        }
                        break;
                    
                    case UserType.Teacher:  
                        TeacherRepo.Instance.Login(name, pass, null, true);
                        break;*/

                    case UserType.Student:
                        StudentRepo.Instance.Login(name, pass, null, true);
                        break;
                }
            }
            else
            {
                var user = ActiveUser.FindUser(name);
                if(user != null && IdentityUtil.isUserType((int)user.Type))
                {
                    handleServerAuthentication(name, pass, (int)user.Type);
                }
                else 
                {
                    /*var admin = F360Admin.Get();
                    if(admin != null && admin.Name == name)
                    {
                        handleServerAuthentication(name, pass, (int)UserType.Admin);
                    }
                    else if(name == SYS_ACC)
                    {
                        Debug.Log("Logged with base system account [" + SYS_ACC + "]");
                    }
                    else
                    {
                        Debug.LogWarning("user=[" + name + "] authenticated, but no login! user? " + (user != null) + " \nadmin? " + (admin != null ? admin.Name : "(null)"));
                    }*/
                }
            }
        }

        internal static void addRepo(IUserRepo repo)
        {
            repos.Add(repo);
        }

    }

}