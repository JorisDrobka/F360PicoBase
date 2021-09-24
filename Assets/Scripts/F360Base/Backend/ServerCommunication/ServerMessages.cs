
using System;
using System.Runtime.Serialization;


namespace F360.Backend.Messages
{   

    //-----------------------------------------------------------------------------------------------
    //
    //  REGISTRATION / STATUS
    //
    //-----------------------------------------------------------------------------------------------


    /// @brief
    /// message send by VR app client to server, once for initial registration,
    /// then at an interval determined by the client.
    ///
    [DataContract]
    public class CS_StatusMessage
    {
        [DataMember]
        public string id;                   ///< device hardware serial
        [DataMember]
        public string hash;                 ///< license hash aquired from server (or empty on no license)
        [DataMember]
        public string level;                ///< the currenly running version on this device
        [DataMember]
        public string name;                 ///< device name
    }


    /// @brief
    /// status response to the VR app client, containing information about
    /// the server instance - depending on state, it can set or block the license or advertise a new patchlevel
    ///
    [DataContract]
    public class SC_StatusResponse
    {
        //  mandatory responses
        [DataMember]
        public string license_hash;         ///< license hash for identification
        [DataMember]
        public string patch_level;          ///< target patchlevel as noted in database config


        //  optional responses

        [DataMember]
        public string set_name;


        [DataMember]
        public bool no_license;             ///< [optional] flag set when device does not have a license yet
                                            ///  if this is set, no other values are formatted with the response. 

        [DataMember]
        public bool registered;             ///< [optional] flag set on initial registration                        [OBSOLETE]
        [DataMember]
        public string set_license;          ///< [optional] server sets new license key & hash                      [OBSOLETE]
        [DataMember]
        public DateTime expire_date;        ///< [optional] send together with set_license
        [DataMember]
        public bool license_expired;        ///< [optional] flag set when license is expired                    
        [DataMember]
        public bool patchable;              ///< [optional] flag set when server confirmed patchlevel update


        public override string ToString()
        {
            var b = new System.Text.StringBuilder("SC_StatusResponse [");
            b.Append("\n\thash: " + license_hash);
            b.Append("\n\tlevel: " + patch_level);
            if(no_license) b.Append("\n\tNo License");
            else if(registered){
                b.Append("\n\tRegistered License: " + set_license);
            }
            else {
                b.Append("\n\tLicense expires: " + expire_date.ToShortDateString());
            }
            b.Append("\n\tpatchable? " + patchable);
            if(!string.IsNullOrEmpty(set_name)) {
                b.Append("\n\tSet Device Name: " + set_name);
            }
            return b.ToString();
        }
    }




    //-----------------------------------------------------------------------------------------------
    //
    //  PATCHING
    //
    //-----------------------------------------------------------------------------------------------
    //
    //
    //  URLS:
    //      /patch-m/   request current PatchLevelManifest
    //      /patch-u/   request patch filelist OR download individual file
    //      /patch-r/   send report
    //
    //
    //
    //  1. client sends StatusMessage to /patch-m/ to update its patch manifest
    //  2. server responds with SC_PatchManifestResponse
    //  3. at user confirm, client sends CS_Patch to /patch-u/ to start patch download 
    //  4. server responds with SC_PatchListResponse containing individual file uris
    //  5. for each uri, clients sends FileDownloadRequest
    //  6. client reinstalls & restarts itself
    //  7. client performs unit tests, reverts version on error
    //  8. client sends CS_PatchReport to /patch-r/
    //


    /// @brief
    /// Server patch manifest update, containing api-level and all bundles.
    /// 
    /// TODO: also download video ids?
    ///
    [DataContract]
    public class SC_PatchManifestResponse
    {
        [DataMember]
        public string patch_level;          ///< target patchlevel

        [DataMember]
        public string api_level;            ///< target api version

        [DataMember]
        public string[] bundles;            ///< bundleIDs+versions

        [DataMember]
        public string info;                 ///< update info that can be displayed as bullets before patch confirmation. The info string is separated via ';'

        [DataMember]
        public int patch_size;              ///< computed size of patch

        public string[] GetUpdateLogBullets()
        {
            if(!string.IsNullOrEmpty(info)) return info.Split(';');
            else                            return new string[0];
        }
    }

    /// @brief
    /// message send by VR app client to initialize and process patchfile download.
    ///
    /// @details
    /// when client is ready for patching (higher TargetPatchLevel in patch manifest & user confirmed),
    /// it sends this message once without the 'file' field to receive a list of files to download.
    /// For each file in this list, the client sends another request, this time with the file's uri set
    /// to indicate to the server readyness for download.
    ///
    [DataContract]
    public class CS_Download
    {
        [DataMember]
        public string id;                   ///< device id

        [DataMember]
        public string hash;                 ///< license hash

        [DataMember]
        public string level;                ///< the currenly running version on this device

        [DataMember]
        public string file;                 ///< file uri send on consecutive requests during patch
    }


    /// @brief
    /// A list of downloadable files received by server at beginning of patch process
    ///
    [DataContract]
    public class SC_PatchListResponse
    {
        [DataMember]
        public bool api_update;             ///< flag wether api install is necessary - install file is always first to download

        [DataMember]
        public bool force_log;              ///< optional flag telling the client to send its patch log regardless of errors

        [DataMember]
        public string[] files;              ///< special URIs for files to download - the uri can be decoded into a FileInfo type, containing server storage type, mime type and size

        public string Print()
        {
            var b = new System.Text.StringBuilder("[Server PatchList]");
            foreach(var f in files) b.Append("\n\t" + f);
            return b.ToString();
        }
    }


    /// @brief
    /// Client status report message
    ///
    [DataContract]
    public class CS_PatchReport
    {
        [DataMember]
        public string id;                   ///< device id

        [DataMember]
        public string hash;                 ///< license hash

        [DataMember]
        public string level;                ///< the currenly running version on this device

        [DataMember]
        public bool validated;              ///< flag set when new version has performed unit tests successfully

        /// @brief
        /// optional log, either send when force_log flag was set by server in SC_PatchListResponse 
        /// or when the new version could not be validated
        ///
        [DataMember]
        public string log;


        [DataMember]
        public System.DateTime date;
    }




    //-----------------------------------------------------------------------------------------------
    //
    //      STUDENT PROFILES
    //
    //-----------------------------------------------------------------------------------------------

    public enum StudentProfileState
    {
        DoesNotExist=0,
        Exists,

        Created,
        Updated
    }

    [DataContract]
    public class StudentProfileInfo
    {
        [DataMember]
        public int userID;

        [DataMember]
        public string name;

        [DataMember]
        public string mail;     // optional? 
    }



    [DataContract]
    public class CS_CreateStudentProfile
    {
        [DataMember]
        public string name;

        [DataMember]
        public string mail;     ///< optional? safe?
    }

    [DataContract]
    public class CS_UpdateStudentProfile
    {
        [DataMember]
        public int userID;

        [DataMember]
        public string name;

        [DataMember]
        public string mail;
    }

    [DataContract]
    public class CS_RequestStudentProfileInfo
    {
        [DataMember]
        public int userID;
    }

    [DataContract]
    public class SC_StudentInfoResponse
    {
        [DataMember]
        public int state;

        [DataMember]
        public int userID;

        [DataMember]
        public string name;

        [DataMember]
        public string mail;


        public StudentProfileInfo GetInfo()
        {
            return new StudentProfileInfo() {
                userID=userID,
                name=name,
                mail=mail
            };
        }
    }

    //-----------------------------------------------------------------------------------------------

    [DataContract]
    public class CS_ListStudentProfiles
    {
        [DataMember]
        public bool archived;
    }

    [DataContract]
    public class SC_ListStudentProfilesResponse
    {
        [DataMember]
        public StudentProfileInfo[] profiles;
    }

    //-----------------------------------------------------------------------------------------------
    //
    //      STAT SYNCHING
    //
    //-----------------------------------------------------------------------------------------------

    //  push data
    
    [DataContract]
    public class CS_PushUserData
    {
        [DataMember]
        public int userID;

        [DataMember]
        public string[] uris;        

        [DataMember]
        public string[] payload;   

        [DataMember]
        public DateTime[] timestamps;

        public bool isValid()
        {
            return uris != null && payload != null && timestamps != null 
                && uris.Length == payload.Length 
                && timestamps.Length == payload.Length;
        }
    }

    [DataContract]
    public class SC_PushUserDataResponse
    {
        [DataMember]
        public int userID;

        [DataMember]
        public int uploaded;
    }


    //-----------------------------------------------------------------------------------------------
    
    //  pull data

    [DataContract]
    public class CS_PullUserData
    {
        [DataMember]
        public int userID;

        [DataMember]
        public DateTime lastSynchTime;
    }

    [DataContract]
    public class SC_PullUserDataResponse
    {
        [DataMember]
        public int userID;

        [DataMember]
        public string[] uris;

        [DataMember]
        public string[] payload;

        public bool isValid()
        {
            return uris != null && payload != null && uris.Length == payload.Length;
        }
    }



}
