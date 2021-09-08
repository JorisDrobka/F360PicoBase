using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360
{

    /// @brief
    /// the context in which a training was created.
    ///
    public enum TrainingContext
    {
        None,
        Exam,
        Custom,
        FromSelection,
        VRTrainer,              //  longer training videos
        Hazards,               //  

        AwarenessTrainer,       //  shows awareness guides
        DriveVR,

        TEACHER                 //  ----> TODO: this needs to go!!!!!!!

    }

    public static class TrainingContextHelper
    {

        public const int EXAM_MAX_ERROR_POINTS = 10;



        public const string PREFIX_EXAM = "exam_";
        public const string PREFIX_DRIVE_VR = "drivevr_";
        public const string PREFIX_VR_TRAINER = "vrtrainer_";
        public const string PREFIX_CUSTOM = "custom_";


        public static bool hasHazardTraining(this TrainingContext context)
        {
            switch(context) {
                case TrainingContext.Hazards:
                case TrainingContext.DriveVR:   return true;
                default:                        return false;
            }
        }

        public static TrainingContext FromTaskID(string taskID)
        {
            taskID = taskID.ToLower();
            if(taskID.Contains(PREFIX_EXAM))
            {
                return TrainingContext.Exam;
            }
            else if(taskID.Contains(PREFIX_DRIVE_VR))
            {
                return TrainingContext.Hazards;
            }
            else if(taskID.Contains(PREFIX_VR_TRAINER))
            {
                return TrainingContext.VRTrainer;
            }
            else
            {
                return TrainingContext.Custom;
            }
        }


        /// @returns Readable name of context
        ///
        public static string GetName(this TrainingContext context)
        {
            switch(context)
            {
                case TrainingContext.Custom:            return "Individuell";
                case TrainingContext.FromSelection:     return "Individuell";

                case TrainingContext.Exam:              return "Prüfungsvorbereitung";
                case TrainingContext.Hazards:           return "Gefahrentraining";
                case TrainingContext.AwarenessTrainer:  return "Blickanalyse";

                case TrainingContext.VRTrainer:         return "VR Trainer";

                case TrainingContext.TEACHER:           return "Empfohlene Übung";

                default:                                return "Unbekannte Kategorie";
            }
        }

        /// @brief
        /// defines wether a training can be saved as highscore into the user profile
        ///
        public static bool canSaveHighscore(this TrainingContext context)
        {
            switch(context)
            {
                case TrainingContext.Exam:
                case TrainingContext.Custom:
                case TrainingContext.VRTrainer:
                case TrainingContext.AwarenessTrainer:
                case TrainingContext.Hazards:           return true;     
                            
                case TrainingContext.TEACHER:
                case TrainingContext.FromSelection:
                default:                                return false;
            }
        }
    }
}
