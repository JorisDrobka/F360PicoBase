using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360.Users
{

    /*
    *
    *   Identity Codes          Server Input
    *   
    *   0 - None        
    *        
    *   1 - System              Username/Mail (name/mail) or logged token
    *   2 - Admin               "-"
    *   3 - Teacher             "-"
    *   4 - Student             "-"
    *   
    *   11 - Device             DeviceID (name)
    *   12 - License            LicenseHash (name)
    *   
    *
    *   42 - Developer
    *
    */

    public enum UserType
    {
        Undefined=0,
        
        System = 1,
        Admin = 2,
        Teacher = 3,
        Student = 4
    }

    

    public enum UserActivity
    {
        WaitingForAccountConfirmation=-2,
        Disabled=-1,
        Inactive=0,

        Minor=1,
        Medium=2,
        High=3
    }


    public static class IdentityUtil
    {

        public static bool isPerson(this UserType type)
        {
            switch(type) {
                case UserType.Admin:
                case UserType.Teacher:
                case UserType.Student:  return true;
                default:                return false;
            }
        }

        public static string Readable(this UserType type)
        {
            switch(type) {
                case UserType.Admin:    return "Lizenznehmer";
                case UserType.Teacher:  return "Lehrer";
                case UserType.Student:  return "Schüler";
                default:                return "Unbekannt";
            }
        }

        public static string Readable(this UserActivity activity)
        {
            switch(activity)
            {
                case UserActivity.WaitingForAccountConfirmation: return "nicht aktiviert";
                case UserActivity.Disabled: return "inaktiv";
                case UserActivity.Minor:    return "selten";
                case UserActivity.Medium:   return "mittel";
                case UserActivity.High:     return "hoch";
                default:                    return "";
            }
        }
        public static string Readable(int identity)
        {
            switch(identity)
            {
                case 1:     return "System";
                case 2:     return "Admin/Customer";
                case 3:     return "Teacher";
                case 4:     return "Student";

                case 11:    return "Device";
                case 12:    return "License";

                case 42:    return "Developer";
                
                default:    return "undefined";
            }
        }

        public static bool isUserType(int identity, bool includeSystem=false)
        {
            return identity == (int)UserType.Admin
                || identity == (int)UserType.Teacher
                || identity == (int)UserType.Student
                || (includeSystem && identity == (int)UserType.System);
        }

        public static UserType IdentityToUserType(int identity, bool includeSystem=false)
        {
            if(identity == (int)UserType.System)
            {
                return includeSystem ? UserType.System : UserType.Undefined;
            }
            else if(identity == (int)UserType.Admin)
            {
                return UserType.Admin;
            }
            else if(identity == (int)UserType.Teacher)
            {
                return UserType.Teacher;
            }
            else if(identity == (int)UserType.Student)
            {
                return UserType.Student;
            }
            else
            {
                return UserType.Undefined;
            }
        }

        public static int FromUserType(UserType type)
        {
            return (int) type;
        }
        

        public static bool isDevice(int identity)
        {
            return identity == 11;
        }

        public static bool isLicense(int identity)
        {
            return identity == 12;
        }

        public static bool isDeveloper(int identity)
        {
            return identity == 42;
        }
    }

}

