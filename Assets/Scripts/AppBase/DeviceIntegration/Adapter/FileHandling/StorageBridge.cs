using UnityEngine;



namespace DeviceBridge.Files
{

    /// @brief
    /// Basic bridge to read/write to storage, implementing paths to Unity resource folders as fallback.
    ///
    /// @details
    /// Basic storage bridge implementing access to all Unity storage locations.
    /// subclass this when writing the IDeviceStorageBridge of your adapter when porting a a new device
    /// and implement you own FileLoader if needed.
    ///
    public abstract class DeviceStorageBridge : IDeviceStorageBridge
    {
        /// @cond PRIVATE
        public MonoBehaviour coroutineHost {
            get { 
                if(_coroutineHost == null)
                    _coroutineHost = CoroutineHost.Instance;
                return _coroutineHost;
            }
            set {
                if(value != null)
                    _coroutineHost = value;
            }
        }
        private MonoBehaviour _coroutineHost;
        private DeviceAdapter adapter;

        protected DeviceStorageState mState;
        /// @endcond

        protected abstract string deviceExternalPath { get; set; } 
        protected abstract string deviceInternalPath { get; set; }



        public DeviceStorageBridge(DeviceAdapter adapter, MonoBehaviour host=null)
        {
            this.coroutineHost = host;
            this.adapter = adapter;
        }


        /// @brief
        /// Check wether the storage is accessible.
        /// 
        public bool isFeatureSupported()
        {
            return mState == DeviceStorageState.Read || mState == DeviceStorageState.ReadAndWrite;
        }

        public DeviceStorageState GetState()
        {
            return mState;
        }

        /// @brief
        /// Get an absolute path to the given location.
        ///
        public string GetStoragePath(StorageLocation location) 
        {
            string s = "";
            switch(location)
            {
                case StorageLocation.Asset_Folder:      s = Application.dataPath; break;
                case StorageLocation.Device_External:   s = deviceExternalPath; break;
                case StorageLocation.Device_Internal:   s = deviceInternalPath; break;
            }

            if(s.Length > 0 && !s.EndsWith("/"))
            {
                s += "/";
            }
            return s;
        }

        /// @brief
        /// Reroute storage paths during runtime - be sure to know what you're doing.
        /// Asset & Resource Folder location cannot be changed
        ///
        public void SetStoragePath(StorageLocation location, string path)
        {
            switch(location)
            {
                case StorageLocation.Device_External:   deviceExternalPath = path; break;
                case StorageLocation.Device_Internal:   deviceInternalPath = path; break;
            }
        }

        /// @brief
        /// Returns a wrapper object to handle file loading.
        ///
        /// @details
        /// Override to implement a custom fileloader.
        ///
        public virtual FileLoader GetFileLoader(StorageLocation location)
        {
            return new FileLoader(this);
        }


        

        /// @brief
        /// write a file to a private location.
        ///
        public virtual void WritePrivateFile(PrivateFile file, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null)
        {
            if(file != null) 
            {
                adapter.SendCommand(Commands.WRITE_PRIVATE_FILE, file.ToJson(), onSuccess, onFailure);
            }
            else
            {
                Debug.LogError("StorageBridge.WritePrivateFile() - file is null!");
            }
        }
        /// @brief
        /// Read a file from a private location.
        ///
        public virtual void ReadPrivateFile(PrivateFile file, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null)
        {
            if(file != null) 
            {
                adapter.SendCommand(Commands.READ_PRIVATE_FILE, file.ToJson(), onSuccess, onFailure);
            }
            else
            {
                Debug.LogError("StorageBridge.ReadPrivateFile() - file is null!");
            }
        }
        /// @brief
        /// Delete a file from a private location.
        ///
        public virtual void DeletePrivateFile(PrivateFile file, DeviceCommandCallback onSuccess=null, DeviceCommandCallback onFailure=null)
        {
            if(file != null) 
            {
                adapter.SendCommand(Commands.DELETE_PRIVATE_FILE, file.ToJson(), onSuccess, onFailure);
            }
            else
            {
                Debug.LogError("StorageBridge.DeletePrivateFile() - file is null!");
            }
        }
    }

}
