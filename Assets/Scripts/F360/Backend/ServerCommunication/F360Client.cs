using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using DeviceBridge;
using F360.Backend.Core;
using F360.Backend.Messages;

namespace F360.Backend
{   


    /// @brief
    /// VR App Client to handle communication with F360 Webserver.
    ///
    ///
    public class F360Client : ClientBase<JsonRequestHandler<SC_StatusResponse>, SC_StatusResponse>
    {
/*        const string URL =

            #if UNITY_EDITOR
                "http://127.0.0.1:8000/";
            #else
            //    "http://127.0.0.1:8000/";
                "https://fs360.duckdns.org/";
            //    "http://167.71.38.67/";
            #endif
*/



        //  function uris
        const string APP_URI = "/api/";
     //   const string URI_REGISTER = APP_URI + "register/";
        const string URI_STATUS = APP_URI + "status/";
        const string URI_PATCH_MANIFEST = APP_URI + "patch-m/";
        const string URI_PATCH = APP_URI + "patch-p/";
        const string URI_PATCH_REPORT = APP_URI + "patch-r/";
        const string URI_LOG_UPLOAD = APP_URI + "log/";

        const string URI_CLEAR_REPO = APP_URI + "repo-clear/";


        //  device status
        //string license_key = "";
        string license_hash = "";
        string device_name = "";
        string hardware_serial = "";
        string patch_level = "";
        bool server_license_state = false;
        //string _url_override;


        //-----------------------------------------------------------------------------------------------
        //
        //  INTERFACE
        //
        //-----------------------------------------------------------------------------------------------

        public event System.Action<SC_StatusResponse> serverStatusUpdate;

        public override string BaseURL { 
            get { 
                return /*!string.IsNullOrEmpty(_url_override) 
                        ? _url_override 
                        : */AppConfig.Current.Backend_URL; 
                } 
        }
        
        /*public void OverrideURL(string url="") { _url_override = url; }

        public void SetNewPatchLevel(string level) 
        {
            patch_level = level;
        }*/

        protected override int Heartbeat_IntervalS { get { return 120; } }


        public override bool canConnect()
        {
            #if UNITY_EDITOR
            return !string.IsNullOrEmpty(hardware_serial);
            #else
            //  check wifi connection
            return DeviceAdapter.Instance != null 
                && DeviceAdapter.Instance.Wifi != null 
                && DeviceAdapter.Instance.Wifi.isConnected()

                //  extra condition: wifi bridge exposed device serial
                && !string.IsNullOrEmpty(hardware_serial);
            #endif
        }


        /// @returns wether server validated license
        ///
        public bool LicenseValidated()
        {
             return server_license_state;
        }




        //-----------------------------------------------------------------------------------------------
        //
        //  REQUESTS
        //
        //-----------------------------------------------------------------------------------------------



        /// @brief
        /// login as a new user
        ///
        public override IServerRequestHandler<Messages.SC_ServerAuthResponse> UserLogin( string name, string pass,
                                                                                IServerSession session=null,
                                                                                ServerRequestCallback<Messages.SC_ServerAuthResponse> callback=null,
                                                                                ServerRequestErrorCallback errorHandler=null)
        {
            return base.UserLogin(name, pass, session, callback, errorHandler);
        }


        public IServerRequestHandler ClearRepo( int userID, 
                                                IServerSession session=null,
                                                ServerRequestCallback<SC_SuccessResponse> callback=null,
                                                ServerRequestErrorCallback errorHandler=null)
        {
            session = session != null ? session : baseSession;
            var param = new CS_RepoClearRequest
            {
                user_id=userID
            };
            var request = session.GetRequestBuilder(URI_CLEAR_REPO, RESTMethod.POST)
                                .SetContentHeader_Json()
                                .SetJsonData<CS_RepoClearRequest>(param)
                                .Build();
            return session.SendJsonRequest(request, callback, errorHandler);
        }






        /// @brief send a request to update the current patch manifest
        ///
        public IServerRequestHandler Patching_UpdateManifest(IServerSession session=null,
                                                             ServerRequestCallback<SC_PatchManifestResponse> callback=null, 
                                                             ServerRequestErrorCallback errorHandler=null)
        {
            session = session != null ? session : baseSession;

            var param = FormatStatusMessage();
            var request = session.GetRequestBuilder(URI_PATCH_MANIFEST, RESTMethod.POST)
                                .SetContentHeader_Json()
                                .SetJsonData<CS_StatusMessage>(param)
                                .Build();
            return session.SendJsonRequest<SC_PatchManifestResponse>(request, callback, errorHandler);;
        }

        /// @brief send a request to get a list of file download uris needed for patching
        ///
        public IServerRequestHandler Patching_GetFileList(IServerSession session=null,
                                                          ServerRequestCallback<SC_PatchListResponse> callback=null, 
                                                          ServerRequestErrorCallback errorHandler=null)
        {
            session = session != null ? session : baseSession;
            var param = new CS_Download
            {
                id = hardware_serial,
                hash = license_hash,
                level = patch_level
            };
            var request = session.GetRequestBuilder(URI_PATCH, RESTMethod.POST)
                                .SetContentHeader_Json()
                                .SetJsonData<CS_Download>(param)
                                .Build();
            return session.SendJsonRequest<SC_PatchListResponse>(request, callback, errorHandler);
        }

        /// @brief send the patchlog of this device to server
        ///
        public IServerRequestHandler Patching_SendReport(bool validated,
                                                         DateTime patchDate,
                                                         string logString,
                                                         IServerSession session=null,
                                                         ServerRequestCallback<string> callback=null, 
                                                         ServerRequestErrorCallback errorHandler=null)
        {
            session = session != null ? session : baseSession;
            if(session != null)
            {
                var param = new CS_PatchReport
                {
                    id = hardware_serial,
                    hash = license_hash,
                    log = logString,
                    date = patchDate,
                    level = patch_level,
                    validated = validated   
                };
                var request = session.GetRequestBuilder(URI_PATCH_REPORT, RESTMethod.POST)
                                            .SetContentHeader_Json()
                                            .SetJsonData<CS_PatchReport>(param)
                                            .Build();
                return session.SendTextRequest(request, callback, errorHandler);
            }
            return null;
        }

        /// @brief send a request to download a specific file during patching
        /// @details the uri is retrieved from the patch list returned by Patching_GetFileList()
        ///
        public FileDownloadHandler DownloadFile(string uri, string targetFolder, int timeout=120,
                                                  IServerSession session=null,
                                                  ServerRequestCallback<SC_FileDownloadResponse> callback=null,
                                                  ServerRequestErrorCallback errorHandler=null)
        {
            if(debug)
            {
                Debug.Log("DownloadFile() \n\tid=" + hardware_serial + "\n\thash=" + license_hash + "\n\tlevel=" + patch_level + "\n\tfile=" + uri);
            }
            session = session != null ? session : baseSession;
            
            var param = new CS_Download
            {
                id = hardware_serial,
                hash = license_hash,
                level = patch_level,
                file = uri
            };
            
            var request = session.GetRequestBuilder(URI_PATCH, RESTMethod.POST)
                                .SetContentHeader_Json()
                                .SetURI(uri)
                                //.SetForm(form)
                                .SetJsonData<CS_Download>(param)
                                .Build();            
            return session.SendFileDownloadRequest(request, targetFolder, timeout, callback, errorHandler);   
        }







        /// @brief send the log of this device to server
        ///
        public IServerRequestHandler Debug_UploadLog(string logString, DateTime date,
                                                     IServerSession session=null, 
                                                     ServerRequestCallback<string> callback=null, 
                                                     ServerRequestErrorCallback errorHandler=null)
        {
            session = session != null ? session : baseSession;

            var param = new CS_LogUpload
            {
                log_string = logString,
                log_date = date
            };
            var request = session.GetRequestBuilder(URI_LOG_UPLOAD, RESTMethod.POST)
                                .SetContentHeader_Json()
                                .SetJsonData<CS_LogUpload>(param)
                                .Build();                       
            return session.SendTextRequest(request, callback, errorHandler);
        }

        

        //-----------------------------------------------------------------------------------------------



        protected override void onClientStart()
        {
            if(AppConfig.Current == null)
            {
                //Manage.AppController.Get().Loader.finishedLoading += (b)=> {
                //    updateDeviceStatus();
                //};
            }
            else
            {
                updateDeviceStatus();
            }
        }


        protected override ServerRequest FormatServerPingRequest()
        {
            updateDeviceStatus();

            var status = FormatStatusMessage();
            /*var uri = !string.IsNullOrEmpty(license_hash)           //  send register message if no license key is available
                    ? URI_STATUS 
                    : URI_REGISTER;*/
            var uri = URI_STATUS;

//            Debug.Log("F360Client.FormatServerPing... Logged User? " + (Users.ActiveUser.Current != null));
            
            return baseSession.GetRequestBuilder(uri, RESTMethod.POST)
                              .SetContentHeader_Json()
                              .SetJsonData<CS_StatusMessage>(status)
                              .Build();
        }

        //-----------------------------------------------------------------------------------------------

        //  process default server status response

        protected override void onServerStatusResponse(SC_StatusResponse response)
        {
            if(response == null)
            {
                return;
            }

            /*var license = Manage.LicenceState.Current;

            if(debug)
            {
                Debug.Log(RichText.emph("Server Status Response! ") + response.ToString());
            }

            //--------  check if device was registered ---------------
            if(response.registered)
            {
                license.InitialRegister();
            }


            //--------  check if license was received by server ------
            if(!string.IsNullOrEmpty(response.set_license))
            {
                UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(response.license_hash), "F360Client:: FATAL! received license key, but hash is empty!");
                license_key = response.set_license;
                license_hash = response.license_hash;

                Debug.Log("===============> Received License Key from Server! <=================\nkey=[" + license_key + "]\nhash=[" + license_hash + "]");   
                license.SetLicense(response.set_license, response.license_hash, response.expire_date);
            }
            else if(response.no_license)
            {
                license_hash = "";
                license_key = "";
            }

            //  device name
            if(!string.IsNullOrEmpty(response.set_name))
            {
                device_name = response.set_name;
                PlayerPrefs.SetString(AppConfig.KEY_DEVICE_NAME, device_name);
            }


            //--------  set current license state --------------------

            server_license_state = !response.no_license 
                                && !response.license_expired 
                                && !string.IsNullOrEmpty(license_hash);

            

            license.BlockLicense(!server_license_state);


            //--------  check if server has new patch ----------------
            if(server_license_state && response.patchable)
            {
                //  TODO: write patch manifest
                license.SetNextPatchLevel(response.patch_level);
            }*/

            //  callbacks
            serverStatusUpdate?.Invoke(response);
        }


        //-----------------------------------------------------------------------------------------------


        CS_StatusMessage FormatStatusMessage()
        {
            return new CS_StatusMessage
            {
                id      = hardware_serial,
                level   = patch_level,
                hash    = license_hash,
                name    = device_name
            };
        }


        void updateDeviceStatus()
        {
            if(AppConfig.Current != null)
            {
                if(string.IsNullOrEmpty(hardware_serial))
                {
                    hardware_serial = VRInterface.GetHardwareSerial();
                }
                /*if(string.IsNullOrEmpty(license_hash))
                {
                    license_hash = Manage.LicenceState.Current.LicenseHash;
                }
                if(AppConfig.Current.patchManifest != null)
                {
                    patch_level = AppConfig.Current.patchManifest.PatchLevel;
                }*/
                if(string.IsNullOrEmpty(device_name))
                {
                    if(Application.isEditor || !PlayerPrefs.HasKey(AppConfig.KEY_DEVICE_NAME))
                    {
                        if(!string.IsNullOrEmpty(AppConfig.Current.device_defaultName))
                        {
                            device_name = AppConfig.Current.device_defaultName;
                            PlayerPrefs.SetString(AppConfig.KEY_DEVICE_NAME, device_name);
                        }
                    }
                }
                                
                
            }
            if(debug)
            {
                Debug.Log("Client Update Device Status:: " + (AppConfig.Current != null) + " =[\n\tserial: " + hardware_serial 
                                                            + "\n\thash: " + license_hash + "\n\tlevel: " + patch_level + "\ndevice-name: " + device_name + "\n]");
            }
        }
    }
}


