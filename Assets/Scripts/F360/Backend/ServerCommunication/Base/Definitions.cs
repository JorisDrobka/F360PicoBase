

namespace F360.Backend
{
    public enum RESTMethod
    {
        GET,
        POST,
        PUT,
//        PATCH,
        DELETE
    }


    public enum RequestState
    {
        Pending,
        Success,

        NoConnectionException,
        TimeoutException,
        RequestFormatException,     //  Bad Request Exception (rename)
        FileNotFoundException,
        MalformatedResponseException,

        UnauthorizedException,
        InternalServerException
    }
    
    //-----------------------------------------------------------------------------------------------

    public static class MimeType
    {
        const string INSTALL_FILEDOWNLOAD_TYPE = ".zip";
        const string BUNDLE_FILEDOWNLOAD_TYPE = ".zip";


        //  basic mime type
        public const string TEXT = "text/plain";
        public const string JSON = "application/json";
        public const string VIDEO = "video/mp4";
        public const string URL_ENCODED = "application/x-www-form-urlencoded";
        public const string ZIP = "application/zip";
        public const string GZIP = "application/gzip";


        //  custom mime types
        public const string BUNDLE = "application/bundle";
        public const string INSTALL = "application/install";

        public static string GetFileExtension(string type)
        {
            switch(type)
            {
                case TEXT:          return ".txt";
                case JSON:          return ".json";
                case VIDEO:         return ".mp4";
                case ZIP:           return ".zip";
                case GZIP:          return ".gzip";
                case URL_ENCODED:   return ".txt";

                case BUNDLE:        return BUNDLE_FILEDOWNLOAD_TYPE;
                case INSTALL:       return INSTALL_FILEDOWNLOAD_TYPE;
                default:            return "";
            }
        }

        public static int GetMimeID(string type)
        {
            switch(type)
            {
                case TEXT:          return 1;
                case JSON:          return 2;
                case VIDEO:         return 3;
                case ZIP:           return 4;
                case GZIP:          return 5;
                case URL_ENCODED:   return 6;

                case BUNDLE:        return 8;
                case INSTALL:       return 9;
                default:            return 0;
            }
        }
        public static string FromMimeID(int id)
        {
            switch(id)
            {
                case 1: return TEXT;
                case 2: return JSON;
                case 3: return VIDEO;
                case 4: return ZIP;
                case 5: return GZIP;
                case 6: return URL_ENCODED;

                case 8: return BUNDLE;
                case 9: return INSTALL;
                default: 
                    return "";
            }
        }

    }


    //-----------------------------------------------------------------------------------------------


    public static class ServerUtil
    {

        public const string URI_META_SEPARATOR = ">>";      ///< serve resource ids may be lead by some meta information

        public const int CODE_OK = 200;
        public const int CODE_BAD_REQUEST = 400;
        public const int CODE_UNAUTHORIZED = 401;
        public const int CODE_FORBIDDEN = 403;
        public const int CODE_NOT_FOUND = 404;
        public const int CODE_METHOD_NOT_ALLOWED = 405;
        public const int CODE_REQUEST_TIMEOUT = 408;

        public static string ReadableCode(int httpCode)
        {
            switch(httpCode)
            {
                case CODE_OK:                   return "ok";
                case CODE_BAD_REQUEST:          return "Bad Request";
                case CODE_UNAUTHORIZED:         return "Unauthorized";  
                case CODE_FORBIDDEN:            return "Forbidden";
                case CODE_NOT_FOUND:            return "Not Found";
                case CODE_METHOD_NOT_ALLOWED:   return "Method not Allowed";
                case CODE_REQUEST_TIMEOUT:      return "Timeout";
                default:                        return "";
            }
        }

        public static int GetErrorCode(string errorMessage)
        {
            if(errorMessage.Contains(CODE_OK.ToString())) return CODE_OK;
            else if(errorMessage.Contains(CODE_BAD_REQUEST.ToString())) return CODE_BAD_REQUEST;
            else if(errorMessage.Contains(CODE_UNAUTHORIZED.ToString())) return CODE_UNAUTHORIZED;
            else if(errorMessage.Contains(CODE_FORBIDDEN.ToString())) return CODE_FORBIDDEN;
            else if(errorMessage.Contains(CODE_NOT_FOUND.ToString())) return CODE_NOT_FOUND;
            else if(errorMessage.Contains(CODE_METHOD_NOT_ALLOWED.ToString())) return CODE_METHOD_NOT_ALLOWED;
            else if(errorMessage.Contains(CODE_REQUEST_TIMEOUT.ToString())) return CODE_REQUEST_TIMEOUT;
            return 0;
        }   


        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        

        public static string ToString(this RESTMethod m)
        {
            switch(m)
            {
                case RESTMethod.GET:    return "GET";
                case RESTMethod.POST:   return "POST";
                case RESTMethod.PUT:    return "PUT";
                case RESTMethod.DELETE: return "DELETE";
                default:                return "ERROR";
            }
        }


        public static bool hasException(this RequestState state)
        {
            switch(state)
            {
                case RequestState.NoConnectionException:
                case RequestState.TimeoutException:
                case RequestState.RequestFormatException:
                case RequestState.FileNotFoundException:    
                case RequestState.InternalServerException:  
                case RequestState.UnauthorizedException:    return true;
                default:                                    return false;
            }
        }
    }
}