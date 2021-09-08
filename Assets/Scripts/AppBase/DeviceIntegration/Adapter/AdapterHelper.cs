using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Text;

namespace DeviceBridge
{

    /// @brief
    /// Callback interface when handling device's user permissions.
    ///
    public interface IPermissionsRationale
    {
        void OnRequestDenied(string permission);
        bool UserAgrees();
        bool UserDeclines();
    }


    //-----------------------------------------------------------------------------------------------------------------

    /// @cond PRIVATE

    public static class AdapterUtil
    {
        public static string FormatCommand(IDeviceCommand cmd, bool printContent=true)
        {
            var sb = new StringBuilder("DeviceCommand {");
            sb.Append("\n\tuid: " + cmd.UID);
            sb.Append("\n\tcmd: " + cmd.CMD);
            if(printContent)
                sb.Append("\n\tcontent: " + cmd.content);
            sb.Append("\n}");
            return sb.ToString();
        }

        public static string FormatResponse(IDeviceResponse response, bool printContent=true)
        {
            var sb = new StringBuilder("DeviceResponse {");
            sb.Append("\n\tuid: " + response.UID);
            sb.Append("\n\ttoken: " + response.TOKEN);
            if(printContent)
                sb.Append("\n\tcontent: " + response.content);
            sb.Append("\n}");
            return sb.ToString();
        }
    }
    
    /// @endcond

}

