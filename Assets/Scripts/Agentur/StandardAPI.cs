using System.Collections;
using System.Collections.Generic;

using F360.Data;
using F360.Users;
using F360.Users.Stats;

using F360.Backend.Messages;


namespace F360
{

    /*
    *   Vordefiniertes Interface für externe Coder.
    *
    *   Erreichbar über api = StandardAPI.GetAPI(); 
    *   bzw. menu = menuStandardAPI.Menu;
    *
    */
    

    /// @brief
    /// Zugriff auf all relevanten Daten & Funktionen für externe Coder
    ///
    public interface IStandardAPI
    {
        IMenuAPI menu { get; }
        IDataAPI data { get; }
        IBackendAPI backend { get; }
        IDeviceAPI device { get; }
    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  MENU
    //
    //-----------------------------------------------------------------------------------------------------------------

    public interface IMenuAPI
    {
        //  endpoints
        void RunLecture(int lectureID);     
        void RunDriveVR(int videoID);
        void RunExam(ExamLevel level);

        void OnChangedMenu(AppLocation location);       ///< bei Menü-Wechsel aufrufen!
    }


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  DATA
    //
    //-----------------------------------------------------------------------------------------------------------------

    public interface IDataAPI
    {
        VTrainerChapter GetVTrainerChapter(int chapterID);
        VTrainerLecture GetVTrainerLecture(int lectureID);
        VideoMetaData GetVideoData(int videoID);            ///< for driveVR & exam

        F360Student GetCurrentUser();                       ///< only when logged in
        IUserStats GetUserStats();                          ///< only when logged in
    }


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  REST API
    //
    //-----------------------------------------------------------------------------------------------------------------

    public interface IBackendAPI
    {
        /// usage:
        /// 
        /// Login(user, (state, info)=> {
        ///     if(!myLoginState.hasError()) {
        ///         //  access UserLoginInfo
        ///     }
        ///     else {
        ///         // ...
        ///     }
        /// })
        ///
        void Login(string user, System.Action<LoginState, SC_UserLoginInfo> callback);      
        void Login(int userID, System.Action<LoginState, SC_UserLoginInfo> callback);
    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  DEVICE FUNCTIONALITY
    //
    //-----------------------------------------------------------------------------------------------------------------

    public interface IDeviceAPI
    {
        bool hasWifiConnection();
        int GetBatteryState();      ///< 0-100
        bool GotoWifiMenu();
        bool GotoScreenshareMenu();
    }



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  USER STATS
    //
    //-----------------------------------------------------------------------------------------------------------------

    public interface IUserStats
    {


        //  General

        int UserID { get; }                     ///< Database ID
        UserMeta Meta { get; }                  ///< Meta stats about user (synchronized)
        RatingMap OverallRatings { get; }       ///< Generated total rating of each discipline / mode


        //---- Progress ---------------------------------------------------------------------------------------------

        int Progress_Overall { get; }           ///< % completion of Fahrschule360 
        int Progress_VTrainer { get; }          ///< % completion of VTrainer / Virtueller Fahrlehrer
        int Progress_DriveVR { get; }           ///< % completion of DriveVR
        int Progress_Exam { get; }              ///< % completion of Exam / Prüfungs-Simulation


        //---- VTrainer / Virtueller Fahrlehrer ---------------------------------------------------------------------
        
        int Rating_VTrainer { get; }            ///< Overall VTrainer score


        /// @returns Ratings of given VTrainer chapter
        /// 
        VTrainerStats GetVTrainerStats(int chapterID);

        
        //---- DriveVR ----------------------------------------------------------------------------------------------
        
        int Rating_DriveVR { get; }                         ///< Overall DriveVR score
        DriveSessionStats AddedDriveVRSessions { get; }     ///< All rated sessions combined to read out overall stats


        /// @brief Merges stats of a single driveVR video for display
        /// @param videoID the driveVR video 
        /// @param buffer (optional) target data object to write stats to
        /// @param includeArchived (optional) include sessions older than the rated time frame
        /// @returns the averaged stats
        ///
        DriveSessionStats GetDriveVRSessionAveraged(int videoID, DriveSessionStats buffer=null, bool includeArchived=false);


        /// @param videoID the driveVR video 
        /// @param includeArchived (optional) include sessions older than the rated time frame
        /// @returns stats of all sessions of an individual driveVR video.
        ///
        IEnumerable<DriveSessionStats> GetDriveVRSessions(int videoID, bool includeArchived=false);

        /// @param includeArchived (optional) include sessions older than the rated time frame
        /// @returns all driveVR sessions of this user
        ///
        IEnumerable<DriveSessionStats> GetDriveVRSessionsAll(bool includeArchived=false);


        /// @returns wether a driveVR session was already seen by user in menu
        ///
        bool hasVisitedDriveVR(int videoID);

        /// @brief
        /// Call to mark a driveVR session as visited by user
        ///
        void MarkDriveVRVisited(int videoID);
        

        //---- Exam / Prüfungs-Simulation ---------------------------------------------------------------------------

        int Rating_Exam { get; }                    ///< Overall exam score

        DriveSessionStats Exam_Bronze { get; }      ///< can be null
        DriveSessionStats Exam_Silver { get; }      ///< can be null
        DriveSessionStats Exam_Gold { get; }        ///< can be null

        /// @returns wether user completed exam of given level
        ///
        bool hasCompletedExam(ExamLevel level);

        /// @returns time in seconds until user may start exam again.
        ///
        int GetTimeSecondsUntilExamUnlock(ExamLevel level);
    }


}


