using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json.Linq;




namespace F360.Backend
{



    /// @brief
    /// Helper object to correctly format web requests 
    ///
    ///
    public class RequestBuilder
    {
        string url;
        string uri;
        RESTMethod method = RESTMethod.GET;
        Dictionary<string, string> headers = new Dictionary<string, string>(); 
        string jsonParams = "";
        WWWForm form = null;
        JObject jsonBuilder;


        //-----------------------------------------------------------------------------------------------

        public ServerRequest Build()
        {
            if(jsonBuilder != null) jsonParams = jsonBuilder.ToString();            
            ServerRequest r = new ServerRequest(url, method, uri);
            r.jsonParams = jsonParams; 
            r.headers = headers;
            return r;
        }

        public void Flush()
        {
            method = RESTMethod.GET;
            headers.Clear();
            jsonBuilder = null;
            form = null;
            uri = "";
            jsonParams = "";
        }

        //-----------------------------------------------------------------------------------------------

        //  base data

        public RequestBuilder SetUrl(string url)
        {
            this.url = url;
            return this;
        }
        public RequestBuilder SetURI(string uri)
        {
            this.uri = uri;
            return this;
        }

        public RequestBuilder SetMethod(RESTMethod method)
        {
            this.method = method;
            return this;
        }

        public RequestBuilder SetForm(WWWForm form)
        {
            this.form = form;
            return this;
        }


        //-----------------------------------------------------------------------------------------------

        //  serialize jsondata 

        public RequestBuilder SetJsonData<P>(P parameters)
        {
            try
            {
                jsonParams = JsonUtility.ToJson(parameters);
                return SetContentHeader_Json();
            }
            catch(Exception ex)
            {
                Debug.LogError("Could not format Webrequest json parameters:: <" + parameters.GetType() + ">\nerror=" + ex.Message);
                return this;
            }
        }

        //-----------------------------------------------------------------------------------------------

        //  OR: set plain text

        public RequestBuilder SetPlainText(string txt)
        {
            jsonParams = txt;
            return this;
        }

        //-----------------------------------------------------------------------------------------------


        // OR: generic json parameters

        public RequestBuilder SetStrParameter(string key, params string[] value)
        {
            openJsonBuilder();
            if(value.Length > 1) jsonBuilder[key] = new JArray(value);
            else                 jsonBuilder[key] = value[0];
            return this;
        }
        public RequestBuilder SetBoolParameter(string key, params bool[] value)
        {
            openJsonBuilder();
            if(value.Length > 1) jsonBuilder[key] = new JArray(value);
            else                 jsonBuilder[key] = value[0];
            return this;
        }
        public RequestBuilder SetIntParameter(string key, params int[] value)
        {
            openJsonBuilder();
            if(value.Length > 1) jsonBuilder[key] = new JArray(value);
            else                 jsonBuilder[key] = value[0];
            return this;
        }
        public RequestBuilder SetDateParameter(string key, params System.DateTime[] value)
        {
            openJsonBuilder();
            if(value.Length > 1) jsonBuilder[key] = new JArray(value);
            else                 jsonBuilder[key] = value[0];
            return this;
        }
        
        //-----------------------------------------------------------------------------------------------
        

        //  headers

        /*
        *   headers to consider:
        *
        *   "Accept", "application/json"
        *   "Authorization", "Basic " + Base64Encode(USER_NAME + ":" + PASS)
        *   "Content-Type", "application/x-www-form-urlencoded"
        *
        */

        public RequestBuilder SetHeader(string key, string content)
        {
            if(!headers.ContainsKey(key))
                headers.Add(key, content);
            else
                headers[key] = content;
            return this;
        }
        public RequestBuilder SetAuthToken(string token)
        {
            return SetHeader(AuthenticatedServerSession.AUTH_TOKEN_KEY, AuthenticatedServerSession.AUTH_TOKEN_PREFIX + token);
        }
        public RequestBuilder SetSSLAuth(string ssl_key)
        {
            return SetHeader("Authorization", ServerUtil.Base64Encode(ssl_key));
        }
        public RequestBuilder SetContentHeader_Json()
        {
            return SetHeader("Content-Type", MimeType.JSON);
        }
        public RequestBuilder SetContentHeader_FormUrlEncoded()
        {
            return SetHeader("Content-Type", MimeType.URL_ENCODED);
        }
        public RequestBuilder SetContentHeader_Text()
        {
            return SetHeader("Content-Type", MimeType.TEXT);
        }
        public RequestBuilder SetContentHeader_Zip()
        {
            return SetHeader("Content-Type", MimeType.ZIP);
        }
        public RequestBuilder SetContentHeader_GZip()
        {
            return SetHeader("Content-Type", MimeType.GZIP);
        }




        //  util

        void openJsonBuilder()
        {
            if(jsonBuilder == null)
            {
                jsonBuilder = new JObject();
            }
            jsonParams = "";
        }
    
    }


}