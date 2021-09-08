using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRIntegration.Video
{


    /// @brief
    /// Access to current VideoPlayer, if any.
    ///
    /// @details
    /// When implementing IVRPlayer, the player must call VRPlayer.SetCurrentPlayer()
    /// to make it accessible to the app logic.
    ///
    public static class VRPlayer
    {
        public static IVRPlayer Player { get { return _playerInstance; } }
        public static IVRPlayerFader Fader { get { return _faderInstance; } }

        //-----------------------------------------------------------------------------------------------------------------

        public static void SetCurrentPlayer(IVRPlayer player)
        {
            _playerInstance = player;
        }
        public static void SetCurrentFader(IVRPlayerFader fader)
        {
            _faderInstance = fader;
        }



        static IVRPlayer _playerInstance;
        static IVRPlayerFader _faderInstance;
    }

    //-----------------------------------------------------------------------------------------------------------------
    
}
