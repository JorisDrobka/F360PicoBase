using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;
using System.Runtime.Serialization;



namespace DeviceBridge.Files
{

    /// @brief
    /// Data storage locations.
    ///
    public enum StorageLocation
    {
        Device_Internal,
        Device_External,
        Asset_Folder,
        Resources_Folder,
        Custom
    }

    public enum DeviceStorageState
    {
        WaitingForPermission=0,
        NoPermission,
        Error,
        Read,
        ReadAndWrite
    }

    public enum FileAccessState
    {
        Idle=0,
        Pending,
        Success,
        File_not_found,
        Access_denied,
        Type_mismatch
    }
    
    /// @brief
    /// Wrapper object for text content about to be written to disk.
    ///
    /// @details
    /// Wrapper for json content that should be stored in a private location in the device.
    /// Content of the file is accessed with the Commands.READ_PRIVATE_FILE and Commands.WRITE_PRIVATE_FILE commands.
    ///
    [DataContract]
    public class PrivateFile : Serialization.SerializableData
    {
        [DataMember(Order=0, EmitDefaultValue=false)]
        public string filePath;
        [DataMember(Order=1, EmitDefaultValue=false)]
        public string fileContent;
        [DataMember(Order=1, EmitDefaultValue=false)]
        public string objectType;       //  json type

        public bool isValid()
        {
            return !string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileContent);
        }

        public bool hasJsonContent()
        {
            return !string.IsNullOrEmpty(objectType);
        }
    }


}