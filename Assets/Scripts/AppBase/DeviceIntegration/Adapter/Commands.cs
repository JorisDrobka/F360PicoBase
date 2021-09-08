namespace DeviceBridge
{


    public static class Commands
    {

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  INTERNAL
        //
        //-----------------------------------------------------------------------------------------------------------------

        /// @brief
        /// called once at startup to establish plugin->unity communication.
        /// plugin calls Unity methods on a GameObject in the scene (the Bridge object).
        ///
        public const string INTERNAL_SET_MESSAGE_OBJECT = "set_message_obj";

        public const string INTERNAL_ENABLE_BRIDGE_DEBUG = "enable_bridge_debug";
        public const string INTERNAL_DISABLE_BRIDGE_DEBUG = "disable_bridge_debug";



        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Commands: UNITY -> PLUGIN
        //
        //-----------------------------------------------------------------------------------------------------------------

        /// @brief
        /// enable a feature on android side. extra=[ DeviceFeature name ]
        ///
        public const string ENABLE_FEATURE = "enable_feature";
        
        /// @brief
        /// disable a feature on android side. extra=[ DeviceFeature name ]
        ///
        public const string DISABLE_FEATURE = "disable_feature";

        /// @brief
        /// Connect to a specific wifi. extra=[ WifiConfig config ]
        /// 
        public const string CONNECT_TO_WIFI = "connect_wifi";

        /// @brief
        /// Request the current sdcard path.
        ///
        public const string GET_SDCARD_PATH = "get_sdcard_path";

        /// @brief
        /// Read a file from the internal app folder. extra=[ string filePath ]
        ///
        public const string READ_PRIVATE_FILE = "read_private_file";
        /// @brief
        /// Write or create a file within the internal app folder. extra=[ PrivateFile file ]
        ///
        public const string WRITE_PRIVATE_FILE = "write_private_file";
        /// @brief
        /// Delete a file from the internal app folder. extra=[ string filePath ]
        ///
        public const string DELETE_PRIVATE_FILE = "delete_private_file";

        /// @brief
        /// Try to start an activity on the device.
        ///
        public const string GOTO_ACTIVITY = "pm_goto_screencast";


        public const string ACTIVITY_DISPLAY_SETTINGS = "activity_display_settings";

        public const string ACTIVITY_WIFI_SELECTION = "activity_wifi_selection";
        public const string ACTIVITY_SCREEN_SHARE = "activity_screenshare_selection";

        public const string WIFI_ENABLE = "wifi_enable";
        public const string WIFI_STATUS_UPDATE = "wifi_status";


        /// @brief
        /// check wether screencast is currently enabled on the device.
        ///
        public const string SCREENCAST_ENABLED = "get_screencast_status";

        /// @brief
        /// force refrest available cast routes.
        ///
        public const string SCREENCAST_REFRESH = "refresh_castroutes";

        /// @brief
        /// connect to an available screencast display. extra=[ string displayID ]
        ///
        public const string SCREENCAST_SELECT_ROUTE = "select_castroute";

        /// @brief
        /// disconnnects current cast route.
        ///
        public const string SCREENCAST_DESELECT_ROUTE = "deselect_castroute";

        /// @brief
        /// get the current default cast route.
        ///
        public const string SCREENCAST_GET_DEFAULT_DISPLAY = "get_default_castroute";
        
        /// @brief
        /// set the default cast route for quick connection.
        ///
        public const string SCREENCAST_SET_DEFAULT_DISPLAY = "set_default_castroute";

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Responses: PLUGIN -> UNITY
        //
        //-----------------------------------------------------------------------------------------------------------------

        //  general error codes

        public static bool isReponseToken(string code)
        {
            switch(code)
            {
                case RESPONSE_COMMAND_EXECUTED:
                case RESPONSE_CREATED_FILE:     return true;
                default:                        return isErrorCode(code);
            }
        }

        public static bool isErrorCode(string code)
        {
            switch(code)
            {
                case ERR_UNKOWN_ERROR:
                case ERR_COMMAND_INVALID:
                case ERR_RESPONSE_NOT_PARSED:
                case ERR_COMMAND_NOT_RESOLVED:
                case ERR_FEATURE_NOT_IMPLEMENTED:
                case ERR_NO_PERMISSION:
                case ERR_TIMEOUT:
                case ERR_DIRECTORY_NOT_FOUND:
                case ERR_FILE_NOT_FOUND:
                case ERR_IO_EXCEPTION:
                case ERR_WIFI_OCCUPIED:             return true;
                default:                            return false;
            }
        }

        /// @brief
        /// Fallback error code.
        ///
        public const string ERR_UNKOWN_ERROR = "unknown_error";

        /// @brief
        /// Error response when a command is unknown OR its content could not be parsed.
        ///
        public const string ERR_COMMAND_INVALID = "err_command_not_parsed";
        
        /// @brief
        /// Error message when a response could not be parsed.
        ///
        public const string ERR_RESPONSE_NOT_PARSED = "err_response_not_parsed";

        /// @brief
        /// Error response when command is known, but the addressed feature is missing. 
        ///
        public const string ERR_COMMAND_NOT_RESOLVED = "err_command_not_resolved";

        public const string ERR_FEATURE_NOT_IMPLEMENTED = "err_feature_not_implemented";
        public const string ERR_NO_PERMISSION = "err_command_no_permission";
        public const string ERR_TIMEOUT = "err_command_timeout";


        //  feature error codes

        public const string ERR_DIRECTORY_NOT_FOUND = "err_directory_not_found";
        public const string ERR_FILE_NOT_FOUND = "err_file_not_found";
        public const string ERR_IO_EXCEPTION = "err_io_exception";
        public const string ERR_WIFI_OCCUPIED = "err_wifi_occupied";
        public const string ERR_SCREENCAST_DISABLED = "error_screencast_disabled";
        public const string ERR_NO_DEFAULT_CASTROUTE = "err_default_castroute_missing";
        public const string ERR_CASTROUTE_INVALID = "err_castroute_invalid";
        public const string ERR_SCREENCAST_CONNECT = "err_screencast_connect";
        

        //  general response states

        /// @brief
        /// Generic success response state.
        ///
        public const string RESPONSE_COMMAND_EXECUTED = "command_executed";


        //  special response states

        /// @brief
        /// Special response constant when a new empty file was created on the device.
        ///
        public const string RESPONSE_CREATED_FILE = "created_new_file";

    }


}