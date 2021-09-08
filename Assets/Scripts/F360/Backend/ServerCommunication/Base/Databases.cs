using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using F360.Backend.Synch;

namespace F360.Backend
{
    
    /// @brief
    /// Fahrschule 360 Database Definitions
    ///
    public enum Database
    {
        Devices = 0,
        Licenses,
        Customers,
        Students,
        Teachers,
        Stats,

        User=100,           //  generic repo of specific user (either customer, teacher or student) 
        SynchRepo = 199,
        Unknown = 200
    }



    public static class DatabaseUtil
    {
        public static string GetURI(this Database database)
        {
            switch(database)
            {
                case Database.Devices:      return "d";
                case Database.Licenses:     return "l";
                case Database.Customers:    return "c";

                case Database.Students:
                case Database.Stats:        return "s";

                case Database.Teachers:     return "t";
                default:                    return "UNKNOWN";
            }
        }

        public static bool isDatabaseAddress(string term)
        {
            Database d;
            return ParseFromTerm(term, out d);
        }

        public static bool ParseFromTerm(string term, out Database database)
        {
            if(!string.IsNullOrEmpty(term))
            {
                char check = term[0];
                if(term.Length > 1)
                {
                    int id = term.IndexOf(SynchedURI.URI_LEAD);
                    if(id > 0)
                    {
                        check = term[id-1];
                    }
                }
                switch(check)
                {
                    case 'D':
                    case 'd':   database = Database.Devices; return true;
                    case 'L':
                    case 'l':   database = Database.Licenses; return true;
                    case 'C':
                    case 'c':   database = Database.Customers; return true;
                    case 'P':
                    case 'p':   database = Database.Students; return true;
                    case 'T':
                    case 't':   database = Database.Teachers; return true;
                    case 'S':
                    case 's':   database = Database.Stats; return true;
                    case 'U':
                    case 'u':   database = Database.User; return true;
                }
            }
            database = Database.Unknown;
            return false;
        }
    }
}