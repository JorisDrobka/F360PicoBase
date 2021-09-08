using System;

namespace F360.Backend.Synch
{

    /// @brief
    /// Identifier structure for any database-bound resource. 
    /// used to identify items in device cache that needs updating or can be updated.
    ///
    /// Can be completely expressed as a string received from/send to server. 
    ///
    [System.Serializable]
    public struct SynchedURI
    {
        static bool logging = false;


        /// @brief
        /// start of each complete URI, prefixed with the database
        ///
        public const string URI_LEAD = "//";
        public const int URI_LEAD_DIGITS = 3;           //  Database URI prefix (1) + LEAD length (2)

        

        public const char URI_KEY_TOKEN = '%';
        public const char URI_META_TOKEN = '#';


        const char TIMESTAMP_BEGIN = '[';
        const char TIMESTAMP_END = ']';

        /// @brief
        /// used to enclose multiple synch statements
        ///
        public static char[] TIME_ENCLOSURE { get; private set; } = new char[] { TIMESTAMP_BEGIN, TIMESTAMP_END };
        

        /// @brief
        /// used to enclose multiple synch statements
        ///
        public static char[] BULK_ENCLOSURE { get; private set; } = new char[] { '(', ')' };

        /// @brief
        /// used to separator multiple statements within a bulk
        ///
        public const char BULK_SEPARATOR = ';';


        public static char[] ENCLOSURE_1 { get; private set; } = new char[] { '[', ']' };         ///< an enclosure that does not interfere with general synch statements
        public static char[] ENCLOSURE_2 { get; private set; } = new char[] { '<', '>' };         ///< an enclosure that does not interfere with general synch statements


        //-----------------------------------------------------------------------------------------------

        public static SynchedURI Invalid { get; private set; } = new SynchedURI(Database.Unknown, "", -1);
        public static SynchedURI None { get { return Invalid; } }

        //-----------------------------------------------------------------------------------------------


        public Database database;
        public string key;
        public int user;
        

        public SynchedURI(Database d, string key, int user=-1)
        {
            this.database = d;
            this.key = key;
            this.user = user;
        }

        public bool isUserBound() { return user >= 0; }

        //-----------------------------------------------------------------------------------------------


        /// @returns true if given string is formatted as valid synchronizable URI
        ///
        public static bool ValidateURI(string raw, bool expectsTimestamp=false)
        {
            bool signature = raw != null && raw.Length >= URI_LEAD.Length + 1 && raw.Substring(1, URI_LEAD.Length) == URI_LEAD;
            if(signature)
            {
                if(expectsTimestamp) return CheckForTimestamp(raw);
                else return true;
            }
            return false;
        }

        /// @returns true if given string contains a URI timestamp (Does not validate URI)
        /// 
        public static bool CheckForTimestamp(string raw)
        {
            return raw != null && raw.StartsWith(TIME_ENCLOSURE[0].ToString()) && raw.Contains(TIME_ENCLOSURE[1].ToString());
        }

        /// @brief:
        /// try parse a synchedURI, timestamp and payload
        ///
        public static bool TryParse(string raw, out DateTime timestamp, out SynchedURI uri, out string meta, bool expectsTimestamp=true)
        {
            if(!TryParseTimestamp(ref raw, out timestamp) && expectsTimestamp)
            {
                if(logging)
                {
                    UnityEngine.Debug.LogWarning("error unpacking timestamp! [" + raw + "]");
                }
                uri = Invalid;
                meta = "";
                return false;
            }
            if(logging)
            {
                UnityEngine.Debug.Log("SynchedURI:: try parse=[" + raw + "]");
            }
            return __tryParseInternal(raw, out uri, out meta);
        }

        /// @brief:
        /// try parse a synchedURI and payload (not expected)
        ///
        public static bool TryParse(string raw, out SynchedURI uri, out string meta)
        {
            DateTime timestamp;
            TryParseTimestamp(ref raw, out timestamp);
            return __tryParseInternal(raw, out uri, out meta);
        }

        /// @brief
        /// try parse a SynchedURI from given string
        ///
        public static bool TryParse(string raw, out SynchedURI uri)
        {
            string meta; DateTime timestamp;
            TryParseTimestamp(ref raw, out timestamp);
            return __fromString(raw, out uri, out meta);
        }

        public static bool TryParseTimestamp( string uri, out DateTime d, 
                                                char beginToken=TIMESTAMP_BEGIN, 
                                                char endToken=TIMESTAMP_END)
        {
            if(!string.IsNullOrEmpty(uri))
            {
                int begin = uri.IndexOf(beginToken);
                int end = uri.IndexOf(endToken);
                if(begin != -1 && end > begin)
                {
                    string inner = uri.Substring(begin+1, end-begin-1);
                    if(TimeUtil.ParseServerTimeFromString(inner, out d))
                    {
                        return true;
                    }
                }
            }
            d = DateTime.Now;
            return false;
        }
        public static bool TryParseTimestamp( ref string uri, out DateTime d, 
                                                char beginToken=TIMESTAMP_BEGIN, 
                                                char endToken=TIMESTAMP_END)
        {
            if(!string.IsNullOrEmpty(uri))
            {
                int begin = uri.IndexOf(beginToken);
                int end = uri.IndexOf(endToken, begin+1);
                if(logging) UnityEngine.Debug.Log("TryParseTimestamp:: " + begin + " // " + end + "\nraw: " + uri);
                if(begin != -1 && end > begin)
                {
                    string inner = uri.Substring(begin+1, end-begin-1);
                    if(TimeUtil.ParseServerTimeFromString(inner, out d))
                    {
                        uri = uri.Substring(end+1);
                        //  try remove method string
                        if(uri[end+1] == beginToken)
                        {
                            begin = end+1;
                            end = uri.IndexOf(endToken, begin+1);
                            if(begin != -1 && end > begin)
                            {
                                uri = uri.Substring(end+1);
                                if(logging) UnityEngine.Debug.Log("Removed Method from URI! left=[" + uri + "]");
                            }
                        }
                        return true;
                    }
                }
            }
            d = DateTime.Now;
            return false;
        }

        static bool __tryParseInternal(string raw, out SynchedURI uri, out string meta)
        {
            if(logging)
            {
                UnityEngine.Debug.Log("SyncherdURI.parseInternal raw=[" + raw + "]");
            }
            if(!string.IsNullOrEmpty(raw)) {
                return __fromString(raw, out uri, out meta);
            }
            else {
                uri = Invalid;
                meta = "";
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------

        /// @returns string-formatted URI
        ///
        public static string Format(Database d, string key, int user=-1)
        {
            return Format(d, key, default(DateTime), user);
        }

        public static string Format(Database d, string key, DateTime timestamp, int user=-1, string meta="")
        {
            var b = new System.Text.StringBuilder();
            if(timestamp != default(DateTime))
            {
                b.Append(TIME_ENCLOSURE[0]);
                b.Append(TimeUtil.FormatServerTimeString(timestamp));
                b.Append(TIME_ENCLOSURE[1]);
            }
            b.Append(d.GetURI());
            b.Append(URI_LEAD);
            if(user >= 0)
            {
                b.Append(user);
            }
            b.Append(URI_KEY_TOKEN);
            b.Append(key);
            if(!string.IsNullOrEmpty(meta))
            {
                b.Append(URI_META_TOKEN);
                b.Append(meta);
            }
            return b.ToString();
        }

        

        //-----------------------------------------------------------------------------------------------

        static bool __fromString(string raw, out SynchedURI uri, out string meta)
        {
            if(logging)
            {
                UnityEngine.Debug.Log("__fromString1... " + raw);
            }
            string baseURI;
            raw = __splitMetaFromURI(raw, out baseURI, out meta);
            
            if(logging)
            {
                UnityEngine.Debug.Log("__fromString2... " + raw + "\nbase: " + baseURI);
            }
            if(!string.IsNullOrEmpty(baseURI))
            {
                Database database;
                if(!DatabaseUtil.ParseFromTerm(raw, out database))
                {
                    uri = Invalid;
                    if(logging) UnityEngine.Debug.LogWarning("...Database parse error!");
                    return false;
                }
                raw = raw.Substring(raw.IndexOf(URI_LEAD)+URI_LEAD.Length);
                
                string[] split = raw.Split(URI_KEY_TOKEN);
                if(split.Length == 1)
                {
                    uri = new SynchedURI(database, split[0]);
                    return true;
                }
                else if(split.Length == 2)
                {
                    int userID;
                    if(F360FileSystem.ParseIntegerFromDataset(split[0], out userID))
                    {
                        uri = new SynchedURI(database, split[1], userID);
                    }
                    else
                    {
                        uri = new SynchedURI(database, split[1]);
                    }
                    return true;
                }
            }
            uri = Invalid;
            return false;  
        }

        static string __splitMetaFromURI(string raw, out string baseURI, out string meta)
        {
            int id = raw.IndexOf(URI_META_TOKEN);
            if(id != -1) {
                meta = raw.Substring(id+1);
                raw = raw.Substring(0, id);
                if(ValidateURI(raw)) {
                    baseURI = raw;
                }
                else {
                    baseURI = "";
                    if(logging) UnityEngine.Debug.LogWarning("Invalid base SynchURI!\n\t" + raw);
                }
            }
            else {
                meta = "";
                baseURI = raw;
            }
            return raw;
        } 
        

        //-----------------------------------------------------------------------------------------------

        public override string ToString()
        {
            return Format(database, key, user);
        }

        public override bool Equals(object obj)
        {
            if(!ReferenceEquals(obj, null))
            {
                if(obj is SynchedURI)
                {
                    return Equals((SynchedURI)obj);
                }
            }
            return false;
        }

        public bool Equals(SynchedURI other)
        {
            return other.database == this.database 
                && other.key.Equals(this.key)
                && other.user.Equals(this.user);
        }

        public override int GetHashCode()
        {
            unchecked {
                int hash = database.GetHashCode();
                hash += key.GetHashCode();
                if(user >= 0) {
                    hash += user * 19;
                }
                hash += 17;
                return hash;
            }
        }

        public static bool operator==(SynchedURI a, SynchedURI b)
        {
            return a.Equals(b);
        }
        public static bool operator!=(SynchedURI a, SynchedURI b)
        {
            return !a.Equals(b);
        }
    }




}

