using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DeviceBridge.Android
{



    /// @brief
    /// A Command can be send back & forth between between app and plugin via the bridge.
    ///
    [DataContract]
    public class Command : IDeviceCommand
    {

        ///@cond PRIVATE
        public const int UID_LENGTH = 3;
        ///@endcond

        public static string GenerateNextCommandID()
        {
            return Utility.RandomString.GetUnique(UID_LENGTH);      //  generate random 3-digit string as identifier
        }

        
        /// @copydoc IDeviceCommand::CMD
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string CMD;

        /// @copydoc IDeviceCommand::UID
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string uid;

        /// @copydoc IDeviceCommand::content
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string content;
        

        string IDeviceCommand.UID { get { return uid; } }
        string IDeviceCommand.CMD { get { return CMD; } } 
        string IDeviceCommand.content { get { return content; } }

        public Command()
        {

        }
        public Command(string cmd, string meta="")
        {
            this.CMD = cmd;
            this.content = meta;
            this.uid = GenerateNextCommandID();
        }

        /// @copydoc IDeviceCommand::ToJson()
        public string ToJson()
        {
            try {
                return JsonConvert.SerializeObject(this);
            }
            catch(SerializationException ex)
            {
                Debug.LogError("Command serialization error: " + ex);
                return null;
            }
        }

        public static Command Parse(string json)
        {
            if(!string.IsNullOrEmpty(json))
            {
                try {
                    return JsonConvert.DeserializeObject<Command>(json);
                }
                catch(SerializationException ex)
                {
                    Debug.LogError("Command deserialization error: " + ex);
                }
            }
            return null;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// Reponse data following a command - by default created within plugin and parsed here.
    ///
    [DataContract]
    public class Response : IDeviceResponse
    {
        public static Response CreateWithRandomUID(string token, string content="", string jsonType="")
        {
            var r = new Response();
            r.uid = Command.GenerateNextCommandID();
            r.TOKEN = token;
            r.content = content;
            r.jsonType = jsonType;
            return r;
        }

        public static Response Create(string uid, string token, string content="", string jsonType="")
        {
            var r = new Response();
            r.uid = uid;
            r.TOKEN = token;
            r.content = content;
            r.jsonType = jsonType;
            return r;
        }


        /// @copydoc IDeviceResponse::TOKEN
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string TOKEN;
        /// @copydoc IDeviceResponse::UID
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string uid;
        /// @copydoc IDeviceResponse::content
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string content;
        /// The type of json object or null.
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string jsonType;
        
        public bool success { get { return Commands.isErrorCode(TOKEN); } }

        string IDeviceResponse.UID { get { return uid; } }
        string IDeviceResponse.TOKEN { get { return TOKEN; } } 
        string IDeviceResponse.content { get { return content; } }
        public bool isJson() { return !string.IsNullOrEmpty(jsonType); }

        public string ToJson()
        {
            try {
                return JsonConvert.SerializeObject(this);
            }
            catch(SerializationException ex)
            {
                Debug.LogError("Response serialization error: " + ex);
                return null;
            }
        }

        /// @copydoc IDeviceResponse::isJson()
        public static Response Parse(string json)
        {
            if(!string.IsNullOrEmpty(json))
            {
                try {
                    return JsonConvert.DeserializeObject<Response>(json);
                }
                catch(SerializationException ex)
                {
                    Debug.LogError("Response deserialization error: " + ex);
                }
            }
            return null;
        }
    }


    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// A java callback wrapper. 
    ///
    /// @details
    /// Allows to retrieve a direct response from within the plugin when sending a command to android.
    /// each callback is only used once and then released.
    ///
    public class AndroidCommandCallback : AndroidJavaProxy
    {
        const string packagepath = "de.fahrschule360.baseunityintegration.ICommandCallback";

        private DeviceCommandCallback successCallback;
        private DeviceCommandCallback failureCallback;

        private bool used;

        private static List<AndroidCommandCallback> cache = new List<AndroidCommandCallback>();
        private static Response response_parse_error;
        static AndroidCommandCallback()
        {
            response_parse_error = new Response();
            response_parse_error.TOKEN = Commands.ERR_RESPONSE_NOT_PARSED;
            response_parse_error.uid = "xxx";
            response_parse_error.content = "";
        }
        

        /// @brief
        /// Get a new callback wrapper.
        public static AndroidCommandCallback Get(DeviceCommandCallback success, DeviceCommandCallback failure)
        {
            if(cache.Count > 0)
            {
                var cc = cache[0];
                cache.RemoveAt(0);
                cc.Set(success, failure);
                return cc;
            }
            return new AndroidCommandCallback(success, failure);
        }

        /// @brief
        /// Get a new callback wrapper.
        public static AndroidCommandCallback Get(DeviceCommandCallback callback)
        {
            if(cache.Count > 0)
            {
                var cc = cache[0];
                cache.RemoveAt(0);
                cc.Set(callback, callback);
                return cc;
            }
            return new AndroidCommandCallback(callback, callback);
        }

        private AndroidCommandCallback(DeviceCommandCallback success, DeviceCommandCallback failure=null)
         : base(packagepath)
        {
            Set(success, failure);
        }

        private void Set(DeviceCommandCallback success, DeviceCommandCallback failure)
        {
            this.successCallback = success;
            this.failureCallback = failure;
            used = false;
        }

        private void OnSuccess(string content)
        {
            if(!used)
            {   
                Debug.Log("AndroidCmdCallback.OnSuccess.... content=[" + content + "]");
                Response response;
                if(parseReponse(content, out response))
                {
                    Debug.Log("CommandCallback OnSuccess! content=[" + content + "]");
                    successCallback?.Invoke(response);
                }
                cache.Add(this);
                flush();
            }
        }
        private void OnFailure(string content)
        {
            if(!used)
            {
                Response response;
                if(parseReponse(content, out response))
                {
                    Debug.Log("CommandCallback OnFailure! reason=[" + response.TOKEN + "] content?{" + response.content + "}");
                    failureCallback?.Invoke(response);
                }
                cache.Add(this);
                flush();
            }
        }

        private bool parseReponse(string content, out Response response)
        {
            response = Response.Parse(content);
            if(response != null)
            {
                return true;
            }
            else
            {   
                Debug.LogWarning("AndroidCommandCallback:: response could not be parsed!");
                failureCallback?.Invoke(response_parse_error);
                return false;
            }
        }

        private void flush()
        {
            used = false;
            successCallback = null;
            failureCallback = null;
        }
    }


    

    

}