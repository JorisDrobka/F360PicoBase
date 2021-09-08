using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using System.Runtime.Serialization;
using Newtonsoft.Json;


using F360.Users;


namespace F360.Backend.Messages
{
    	
    #pragma warning disable




    [DataContract]
    public class SC_SuccessResponse
    {
        [DataMember]
        public bool success;
    }


    

    //-----------------------------------------------------------------------------------------------
    //
    //  IDENTITY
    //
    //-----------------------------------------------------------------------------------------------

    /// @brief
    /// check by name and/or mail.
    /// if both are left empty, system returns identity of session token.
    ///
    [DataContract]
    public class CS_IdentityRequest
    {
        [DataMember]
        public string mail;
        [DataMember]
        public string name;
        [DataMember]
        public string hash;
    }

    [DataContract]
    public class SC_IdentityResponse
    {
        [DataMember]
        public int id;          ///< database id

        [DataMember]
        public int identity;    ///< UserType or other system components


        //  received if admin/teacher login:

        [DataMember]
        public string hash;     ///< license hash

        [DataMember]
        public System.DateTime expire_date; ///< license expire date


        /// @returns Usertype of received identity or Undefined
        ///
        public UserType AsUserType()
        {
            return IdentityUtil.IdentityToUserType(identity);
        }

        public bool isDeveloper()
        {
            return IdentityUtil.isDeveloper(identity);
        }

        public override string ToString()
        {
            var b = new System.Text.StringBuilder("[Identity]");
            b.Append("\n\trole:    " + IdentityUtil.Readable(identity));
            b.Append(" (" + identity + ")");
            b.Append("\n\tpk       " + id);
            if(!string.IsNullOrEmpty(hash))
                b.Append("\n\tlicense: " + hash);
            if(expire_date != default(System.DateTime))
                b.Append("\n\texpires: " + expire_date.ToShortDateString());
            return b.ToString();
        }
    }

    //-----------------------------------------------------------------------------------------------
    //
    //  AUTHENTICATION
    //
    //-----------------------------------------------------------------------------------------------

    [DataContract]
    public class CS_ServerAuthRequest
    {
        [DataMember]
        public string username;

        [DataMember]
        public string password;

        public CS_ServerAuthRequest(string usr, string pass) {
            this.username = usr;
            this.password = pass;
        }
    }

    [DataContract]
    public class SC_ServerAuthResponse
    {
        [DataMember]
        public string token;
    }

    //-----------------------------------------------------------------------------------------------
    //
    //  ACCOUNTS
    //
    //-----------------------------------------------------------------------------------------------

    [DataContract]
    public class CS_CreateAccountRequest
    {
        [DataMember]
        public string username;
        [DataMember]
        public string email;

        public CS_CreateAccountRequest(string name, string mail)
        {
            this.username = name;
            this.email = mail;
        }
    }

    [DataContract]
    public class SC_CreateAccountResponse
    {
        public const int CREATED = 1;
        public const int EXISTING = 0;
        public const int FAILED = -1;

        [DataMember]
        public int state;   //  1: success, 0: already existing, -1: failed    

        [DataMember]
        public bool invalid_mail;

        public bool UserExistsAlready() { return state == 0; }
        public bool UserCreated() { return state > 0; }
        public bool hasError() { return state < 0; }
    }

    //-----------------------------------------------------------------------------------------------
    //
    //  ACCOUNT INFO
    //
    //-----------------------------------------------------------------------------------------------

    [DataContract]
    public class SC_UserInfo
    {
        [DataMember]
        public int id;

        [DataMember]
        public string parent;

        [DataMember]
        public bool active;

        [DataMember]
        public bool archived;

        [DataMember]
        public string name;

        [DataMember]
        public string mail;

        [DataMember]
        string last_login;

        [DataMember]
        public int identity;


        public System.DateTime lastLogin { 
            get {  
                if(__parsedLoginDate == default(System.DateTime)) {
                    System.DateTime parsed;
                    if(TimeUtil.ParseServerTimeFromString(last_login, out parsed))
                    {
                        __parsedLoginDate = parsed;
                    }
                    else
                    {
                        __parsedLoginDate = TimeUtil.ServerTime;        /// @TODO  TMP!!!
                    }
                }
                return __parsedLoginDate;
            } 
        }
        System.DateTime __parsedLoginDate;

        public bool Exists()
        {
            return identity > 0 && !string.IsNullOrEmpty(mail) && !string.IsNullOrEmpty(name);
        }

        public override string ToString()
        {
            var b = new System.Text.StringBuilder("[User] - " + name);
            b.Append("\n\tid:       " + id.ToString());
            b.Append("\n\tmail:     " + mail);
            b.Append("\n\tidentity: " + IdentityUtil.Readable(identity));
            b.Append("\n\tactive:   " + active);
            b.Append("\n\tarchived: " + archived);
    //        b.Append("\n\tlogin:    " + last_login.ToShortDateString());
            return b.ToString();
        }
    }

    //  permission: [system, admin, teacher]
    [DataContract]
    public class CS_ListStudentsRequest
    {
        [DataMember]
        public string hash;

        [DataMember]
        public bool archived = false;

        [DataMember, OptionalField]
        public int[] ids;
    }
    
    //  permission: [system, admin]
    [DataContract]
    public class CS_ListTeachersRequest
    {
        [DataMember]
        public string hash;

        [DataMember]
        public bool archived = false;

        [DataMember, OptionalField]
        public int[] ids;
    }

    [DataContract]
    public class SC_UserListResponse
    {
        [DataMember]
        public SC_UserInfo[] list;

        public IEnumerable<SC_UserInfo> GetInfos()
        {
            if(list != null)
            {
                var b = new System.Text.StringBuilder("Deserialize UserInfos... " + list.Length);
                foreach(var s in list) b.Append("\n\t" + s);
                Debug.Log(b);
                return list;
            }
            else
            {
                return new SC_UserInfo[0];
            }
        }
    }


    [DataContract]
    public class CS_RepoClearRequest
    {
        [DataMember]
        public int user_id;
    }


    //-----------------------------------------------------------------------------------------------
    //
    //  DEVICE INFO
    //
    //-----------------------------------------------------------------------------------------------
    

    [DataContract]
    public class SC_DeviceInfo
    {
        [DataMember]
        public int id;
        [DataMember]
        public string name;         //  @TODO: implement in backend
        [DataMember]
        public string serial;
        [DataMember]
        public string level;
        [DataMember]
        public string location;
        [DataMember]
        string register_date;
        
        [DataMember]
        public string rented;
        [DataMember]
        string rent_date;       //  implement server-side


        public System.DateTime GetRegisterTime()
        {
            System.DateTime t;
            if(TimeUtil.ParseServerTimeFromString(register_date, out t))
            {
                return t;
            }
            return default(System.DateTime);
        }

        public System.DateTime GetRentTime()
        {
            System.DateTime t;
            if(TimeUtil.ParseServerTimeFromString(rent_date, out t))
            {
                return t;
            }
            return default(System.DateTime);
        }
    }


    [DataContract]
    public class CS_DeviceInfoRequest
    {
        [DataMember]
        public string id;
    }

    [DataContract]
    public class CS_ListDevicesRequest
    {
        [DataMember]
        public string hash;
    }

    [DataContract]
    public class SC_DeviceListResponse
    {
        [DataMember]
    //    string[] list;
        SC_DeviceInfo[] list;

        public IEnumerable<SC_DeviceInfo> GetInfos()
        {
            if(list != null)
            {
                var b = new System.Text.StringBuilder("Deserialize UserInfos... " + list.Length);
                foreach(var s in list) b.Append("\n\t" + s.ToString());
                Debug.Log(b);
                return list;
            }
            else
            {
                return new SC_DeviceInfo[0];
            }
        }
    }

    //-----------------------------------------------------------------------------------------------
    //
    //  META DATA FORMATS
    //
    //-----------------------------------------------------------------------------------------------

    [DataContract]
    public class ServerFileInfo
    {
        [DataMember]
        public string file_uri;         ///< server resource id

        [DataMember]
        public int size;                  ///< rounded size in MB

        [DataMember]
        public string type;               ///< MIME type

        [DataMember]
        public string storage;            ///< server storage class


        public string download_path { get; set; }

        public string Print()
        {
            var b = new System.Text.StringBuilder("[ServerFile");
            b.Append("\n\turi= " + file_uri);
            b.Append("\n\tsize= " + size.ToString());
            b.Append("\n\ttype= " + type);
            b.Append("\n\tstorage= " + storage);
            return b.ToString();
        }

        public string GetFileName()
        {
            if(!string.IsNullOrEmpty(file_uri))
            {
                return F360FileSystem.RemoveFolderFromFilePath(file_uri);
            }
            return file_uri;
        }

        public string EncodeToDownloadURI()
        {
            return FileInfoUtil.EncodeFileInfo(this);
        }

        public static ServerFileInfo ParseDownloadURI(string uri)
        {
            return FileInfoUtil.DecodeFileInfo(uri);
        }
    }

    [DataContract]
    public class CS_FileDownload
    {
        [DataMember]
        public string hash;                 ///< license hash

        [DataMember]
        public string file;
    }

    /// @brief
    /// special response structure created automatically by FileDownloadHandler
    ///
    [DataContract]
    public class SC_FileDownloadResponse
    {
        public string fileName;             ///< name of downloaded file (with extension)
        public string filePath;             ///< full path to file
        public string targetFolder;         ///< download folder
        
        public ServerFileInfo fileInfo;     ///< additional file info
        public bool zipped;                 ///< flag when file is zipped


        /*public string filePath;         ///< full path to downloaded file

        public string download_uri;     ///< resource path on server

        public string GetFolder()
        {
            return filePath.Substring(0, filePath.Length-fileName.Length);
        }*/
    }    



    
    public static class FileInfoUtil
    {
        const string MIME_SEPARATOR = ":";
        const string SIZE_SEPARATOR = "@";
        const string DOWNLOAD_SEPARATOR = ">>";

        public static string EncodeFileInfo(ServerFileInfo info)
        {
            if(info != null)
            {
                if(info.size > 0)
                {
                    return info.storage 
                    + MIME_SEPARATOR + MimeType.GetMimeID(info.type).ToString()
                    + SIZE_SEPARATOR + info.size.ToString()
                    + DOWNLOAD_SEPARATOR + info.file_uri;
                }
                else
                {   
                    return info.storage
                    + MIME_SEPARATOR + MimeType.GetMimeID(info.type).ToString()
                    + DOWNLOAD_SEPARATOR + info.file_uri;
                }
            }
            return "";
        }
        public static ServerFileInfo DecodeFileInfo(string download_uri)
        {
            if(!string.IsNullOrEmpty(download_uri))
            {
                Debug.Log(">>> Decode FileInfo uri=[" + download_uri + "]");

                string storage = "";
                string uri = "";
                string mime = "";
                int size = 0;

                int l = download_uri.Length;

                int m_id = download_uri.IndexOf(MIME_SEPARATOR);
                int s_id = download_uri.IndexOf(SIZE_SEPARATOR);
                int d_id = download_uri.IndexOf(DOWNLOAD_SEPARATOR);

                if(d_id != -1)
                {
                    //  file uri
                    uri = download_uri.Substring(d_id+DOWNLOAD_SEPARATOR.Length, l-(d_id+DOWNLOAD_SEPARATOR.Length));

                    //  storage
                    if(m_id != -1)
                    {
                        storage = download_uri.Substring(0, m_id);
                    }
                    else if(s_id != -1)
                    {
                        storage = download_uri.Substring(0, s_id);
                    }
                    else
                    {
                        storage = download_uri.Substring(0, d_id);
                    }

                    //  mime
                    if(m_id != -1)
                    {
                        m_id += MIME_SEPARATOR.Length;
                        string mval = "";
                        if(s_id != -1)
                        {
                            mval = download_uri.Substring(m_id, s_id-m_id);
                        }
                        else
                        {
                            mval = download_uri.Substring(m_id, d_id-m_id);
                        }
                        int mimeId;
                        if(System.Int32.TryParse(mval, out mimeId))
                        {
                            mime = MimeType.FromMimeID(mimeId);
//                            Debug.Log("parsed mime id: " + mimeId + " [" + mime + "]");
                        }
                        else
                        {
                            Debug.LogWarning("failed to parse mime id from [" + mval + "]");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("no mime id found in server uri!");
                    }

                    //  size
                    if(s_id != -1)
                    {
                        s_id += SIZE_SEPARATOR.Length;
                        string sval = download_uri.Substring(s_id, l-d_id);
                        int s;
                        if(System.Int32.TryParse(sval, out s))
                        {
                            size = s;
                        }
                    }

                    //  extension check
                    int ext_id = download_uri.LastIndexOf(".");
                    if(ext_id == -1)
                    {
                        if(!string.IsNullOrEmpty(mime))
                        {
                            uri += MimeType.GetFileExtension(mime);
                        }
                    }

                    return new ServerFileInfo()
                    {
                        file_uri = uri,
                        storage = storage,
                        type = mime,
                        size = size
                    };
                }
                else
                {
                    return new ServerFileInfo()
                    {
                        file_uri = download_uri
                    };
                }
            }
            return null;
        }
    }

    //-----------------------------------------------------------------------------------------------
    //
    //  LOGGING
    //
    //-----------------------------------------------------------------------------------------------


    [DataContract]
    public class CS_LogUpload
    {
        [DataMember]
        public string log_string;

        [DataMember]
        public string settings_string;

        [DataMember]
        public System.DateTime log_date;
    }

    [DataContract]
    public class SC_LogSettings
    {
        [DataMember] 
        public string level;
        
        [DataMember]
        public string[] channels;

        public string Print()
        {
            var b = new System.Text.StringBuilder("[Device LogSettings]");
            b.Append("\n\tlevel:\t" + level);
            if(channels != null)
            {
                b.Append("\n\tchannels:");
                foreach(var c in channels)
                {
                    b.Append("\n\t\t-" + c);
                }
            }
            return b.ToString();
        }

        public override string ToString()
        {
            return ServerLogSettingsUtil.Encode_LogSettings(this);
        }
    }
    

    public static class ServerLogSettingsUtil
    {
        const string BEGIN_TOKEN = "log{";
        const string END_TOKEN = "}";
        const string CHANNEL_SEPARATOR = "-";

        /// encodes LogSettings to plain string
        public static string Encode_LogSettings(SC_LogSettings settings)
        {
            var b = new System.Text.StringBuilder(BEGIN_TOKEN);
            b.Append(settings.level);
            if(settings.channels != null) {
                foreach(var c in settings.channels) {
                    b.Append('-' + c);
                }
            }
            b.Append(END_TOKEN);
            return b.ToString();
        }



        /// @brief
        /// parses LogSettings from info string send by server   
        ///
        /// @details
        /// the string is formatted like this: 
        ///     log{warn-dc-a} 
        ///     or 
        ///     log{-dc}
        ///
        /// where the first token can be on of the log levels:
        ///     - exception
        ///     - warn
        ///     - info
        ///     - verbose
        /// 
        /// and the following parameters can be individual channels prefixed with a '-'
        ///     
        ///     - dc    [ Inter-Device Connections ]
        ///     - sc    [ Server Connection ]
        ///     - h     [ Hardware ]
        ///     - t     [ Training ]
        ///     - a     [ Activity ]
        ///
        ///
        public static SC_LogSettings ParseSettingsFromString(string s)
        {
            string level = "exception";
            if(!string.IsNullOrEmpty(s))
            {
                int a = s.IndexOf(BEGIN_TOKEN);
                int b = s.IndexOf(END_TOKEN);
                if(a != -1 && b != -1)
                {
                    a += BEGIN_TOKEN.Length;
                    s = s.Substring(a, b-a);
                    if(s.Contains(CHANNEL_SEPARATOR))
                    {
                        int p = s.IndexOf(CHANNEL_SEPARATOR);
                        if(p > 0)
                        {
                            level = s.Substring(0, p);
                        }
                        int pp = p + CHANNEL_SEPARATOR.Length;
                        List<string> channels = new List<string>();
                        while(p != -1)
                        {
                            p = s.IndexOf(CHANNEL_SEPARATOR, pp);
                            if(p != -1)
                            {
                                channels.Add(s.Substring(pp,p-pp));
                                pp = p + CHANNEL_SEPARATOR.Length;
                            }
                        }
                        channels.Add(s.Substring(pp, s.Length-pp));
                        
                        return new SC_LogSettings()
                        {
                            level = level,
                            channels = channels.ToArray()
                        };
                    }
                    else if(!string.IsNullOrEmpty(s))
                    {
                        level = s;
                    }
                }
            }
            return new SC_LogSettings(){ level=level };
        }
    }


    #pragma warning restore
}