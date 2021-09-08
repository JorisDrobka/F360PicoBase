using UnityEngine;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

using DeviceBridge.Files;

namespace DeviceBridge.Serialization
{


    /// @brief
    /// custom serialization interface to receive callbacks before & after, using SerializationHelper class
    /// or the static methods provided by SerializableData
    ///
    public interface ISerializableData
    {
        void onBeforeSerialize();
        void onAfterDeserialize();
    }

    public interface IProcessedSerializableData : ISerializableData
    {
        string preprocessJson(string json);
        string postprocessJson(string json);
    }

    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Base for all data classes used to communicate with the plugin.
    ///
    public abstract class SerializableData : ISerializableData
    {
        public string ToJson()
        {
            return JsonSerializationHelper.SafeSerialization(this);
        }

        protected virtual void onBeforeSerialize()
        {

        }
        protected virtual void onAfterDeserialize()
        {

        }

        void ISerializableData.onBeforeSerialize() { this.onBeforeSerialize(); }
        void ISerializableData.onAfterDeserialize() { this.onAfterDeserialize(); }

        public static string SafeSerialization(ISerializableData obj)
        {
            return JsonSerializationHelper.SafeSerialization(obj);
        }

        public static bool TryParse<TData>(string json, out TData result, bool throwErrorOnEmptyContent=true) where TData : ISerializableData, new()
        {
            return JsonSerializationHelper.TryParse<TData>(json, out result, throwErrorOnEmptyContent);
        }
    }


    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Base class for persistent files stored on the device.
    ///
    public abstract class SerializedFile : SerializableData, IProcessedSerializableData
    {
        public abstract string fileName { get; }
        public abstract string filePath { get; }
        public abstract string fileExtension { get; }


        /// @brief
        /// Helper method for writing the file to disk.
        /// 
        /// @details
        /// Creates a wrapper object for this file to send it to the plugin. 
        /// the wrapper is used for the Commands.WRITE_PRIVATE_FILE command.
        ///
        public virtual PrivateFile PrepareForWrite() 
        {
            PrivateFile file = new PrivateFile();
            file.filePath = filePath + fileName + "." + fileExtension;
            file.fileContent = this.ToJson();
            file.objectType = this.GetType().ToString();
            return file;
        }

        /// @brief
        /// called directly after this file is serialized to json to modify the json-result
        ///
        protected virtual string OnPostprocessJson(string json) { return json; }

        /// @brief
        /// called directly before a json-string is deserialized into this object to modify json beforehand
        ///
        protected virtual string OnPreprocessJson(string json) { return json; }

        string IProcessedSerializableData.postprocessJson(string json) { return OnPostprocessJson(json); }
        string IProcessedSerializableData.preprocessJson(string json) { return OnPreprocessJson(json); }
    }


    //-----------------------------------------------------------------------------------------------------------------

    public static class JsonSerializationHelper
    {
        public static string SafeSerialization(ISerializableData obj)
        {
            string result = "";
            if(obj != null)
            {
                var postprocess = obj as IProcessedSerializableData;
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                try {
                    obj.onBeforeSerialize();
                    result = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    if(postprocess != null)
                    {
                        result = postprocess.postprocessJson(result);
                    }
                }
                catch(System.IO.IOException ex) 
                {
                    Debug.LogError("error while serializing<" + obj.GetType() + ">! message=" + ex.Message);
                }
                finally {
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                }
            }
            return result;
        }

        public static bool TryParse<TData>(string json, out TData result, bool throwErrorOnEmptyContent=true) where TData : ISerializableData, new()
        {
            if(string.IsNullOrEmpty(json))
            {
                result = default(TData);
                if(throwErrorOnEmptyContent)
                {
                    Debug.LogError("error while deserializing<" + typeof(TData) + ">!  received empty json!");
                    return false;   
                }
                else
                {
                    return true;
                }
            }
            else
            {
            //    var preprocess = default(TData) as IProcessedSerializableData;
                var preprocess = new TData() as IProcessedSerializableData;
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                try {
                    if(preprocess != null)
                    {
                        json = preprocess.preprocessJson(json);
                    }
                    result = JsonConvert.DeserializeObject<TData>(json);
                }
                catch(System.IO.IOException ex) 
                {
                    result = default(TData);
                    Debug.LogError("error while deserializing<" + typeof(TData) + ">! message=" + ex.Message);
                }
                finally {
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                }

                if(result != null)
                {
                    result.onAfterDeserialize();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    //-----------------------------------------------------------------------------------------------------------------

    public static class BinarySerializationHelper
    {


        public static bool Serialize<TData>(FileStream stream, TData obj) where TData : ISerializable, new()
        {
            if(stream != null && obj != null)
            {
                var hasError = false;
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                try 
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, obj);
                }
                catch(System.IO.IOException ex) 
                {
                    hasError = true;
                    Debug.LogError("error while serializing <" + typeof(TData).Name + ">! message=" + ex.Message);
                }
                finally {
                    stream.Close();
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                }
                return !hasError;
            }
            return false;
        }

        public static bool Deserialize<TData>(FileStream stream, out TData obj) where TData : ISerializable, new()
        {
            if(stream != null)
            {
                var hasError = false;
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                try 
                {   
                    BinaryFormatter bf = new BinaryFormatter();
                    obj = (TData) bf.Deserialize(stream);
                }
                catch(System.IO.IOException ex) 
                {
                    obj = new TData();
                    hasError = true;
                    Debug.LogError("error while deserializing<" + typeof(TData).Name + ">! message=" + ex.Message);
                }
                finally 
                {
                    stream.Close();
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                }
                return !hasError;
            }
            else
            {
                obj = new TData();
                return false;
            }
        }


        public static bool Deserialize<TData>(byte[] bytes, out TData obj) where TData : ISerializable, new()
        {
            if(bytes != null)
            {
                var hasError = false;
                var currentCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                try
                {
                    using(MemoryStream ms = new MemoryStream(bytes))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        obj = (TData) bf.Deserialize(ms);
                    }
                }
                catch(System.IO.IOException ex)
                {
                    obj = new TData();
                    hasError = true;
                    Debug.LogError("error while deserializing<" + typeof(TData).Name + ">! message=" + ex.Message);
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = currentCulture;
                }
                return !hasError;
            }
            else
            {
                obj = new TData();
                return false;
            }
        }

    }
}


