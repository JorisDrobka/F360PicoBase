using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace F360
{
    /// @brief
    /// Locations within the app the user can navigate to
    ///
    public enum AppLocation
    {
        Loader,

        Stopped,
        Sleep,

        
        Tutorial,
        Menu_Dome,
        Menu_CustomTrainer,
        Menu_DriveVR_Selection,
        Menu_Teacher_Tasks,

        Menu_TrainingPreview,

        Menu_DeviceSettings,
        Menu_WifiSettings,
        Menu_UserSettings,


        Video_Exam,
        Video_ExamPlayback,
        Video_AwarenessTraining,
        Video_AwarenessPlayback,
        Video_DriveVR,
        Video_DriveVRPlayback
    }

    

    public static class LocationHelper
    {

        public static string Readable(this AppLocation location)
        {
            switch(location) {
                case AppLocation.Stopped:                   return "Ausgeschaltet";
                case AppLocation.Sleep:                     return "Ruhezustand";
                case AppLocation.Loader:                    return "Lade";
                case AppLocation.Menu_Dome:                 return "Hauptmenü";
                case AppLocation.Menu_CustomTrainer:
                case AppLocation.Menu_DriveVR_Selection:    return "Trainings-Auswahl";

                case AppLocation.Menu_TrainingPreview:      return "Trainings-Ansicht";

                case AppLocation.Menu_DeviceSettings:       
                case AppLocation.Menu_WifiSettings:         return "Einstellungen";
                case AppLocation.Menu_UserSettings:         return "User";

                case AppLocation.Video_Exam:
                case AppLocation.Video_ExamPlayback:
                case AppLocation.Video_AwarenessTraining:
                case AppLocation.Video_AwarenessPlayback:
                case AppLocation.Video_DriveVR:
                case AppLocation.Video_DriveVRPlayback:     return "Training";
                default:                                    return "Unbekannt";
            }
        }

        public static bool isResponding(this AppLocation location)
        {
            switch(location) {
                case AppLocation.Stopped: 
                case AppLocation.Sleep:     return false;
                default:                    return true;
            }
        }

        public static bool isMenu(this AppLocation location)
        {
            switch(location) {
                case AppLocation.Loader:
                case AppLocation.Menu_Dome:
                case AppLocation.Menu_CustomTrainer:
                case AppLocation.Menu_DriveVR_Selection:
                case AppLocation.Menu_TrainingPreview:
                case AppLocation.Menu_DeviceSettings:
                case AppLocation.Menu_WifiSettings:
                case AppLocation.Menu_UserSettings:         return true;
                default:                                    return false;
            }
        }

        public static bool isTraining(this AppLocation location)
        {
            switch(location) {
                case AppLocation.Video_Exam:
                case AppLocation.Video_ExamPlayback:
                case AppLocation.Video_AwarenessTraining:
                case AppLocation.Video_AwarenessPlayback:
                case AppLocation.Video_DriveVR:
                case AppLocation.Video_DriveVRPlayback:     return true;
                default:                                    return false;
            }
        }

        public static bool isVideoPlayer(this AppLocation location)
        {
            switch(location) {
                case AppLocation.Video_ExamPlayback:
                case AppLocation.Video_AwarenessPlayback:
                case AppLocation.Video_DriveVRPlayback:     return true;
                default:                                    return false;
            }
        }

    }
}