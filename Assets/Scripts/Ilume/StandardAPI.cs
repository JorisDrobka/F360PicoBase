using F360.Data;
using F360.Users;

namespace F360
{

    /*
    *   Vordefiniertes Interface für externe Coder
    *
    */
    

    public interface IStandardAPI
    {
        IMenuAPI menu { get; }
        IDataAPI data { get; }
        IBackendAPI backend { get; }
        IDeviceAPI device { get; }
        IDeviceNetworkAPI deviceNetwork { get; }
    }


    //-----------------------------------------------------------------------------------------------------------------


    public interface IMenuAPI
    {


        //  endpoints
        void runLecture(int lectureID);     
        void runDriveVR(int driveID);       ///< videoID
        void runExam(int level);            ///< 1 - bronze | 2 - silver | 3 - gold
    }

    //-----------------------------------------------------------------------------------------------------------------

    public interface IDataAPI
    {
        VTrainerChapter GetVTrainerChapter(int chapterID);
        VTrainerLecture GetVTrainerLecture(int lectureID);
        VideoMetaData GetVideoData(int videoID);            ///< for driveVR & exam
        F360Student GetUser(string name);
        
        // xxx GetUserStats(string name);
    }

    //-----------------------------------------------------------------------------------------------------------------

    public interface IBackendAPI
    {
        void Login(string user);
    }


    //-----------------------------------------------------------------------------------------------------------------

    public interface IDeviceAPI
    {
        bool hasWifiConnection();

        void GotoWifiMenu();
        void GotoScreenshareMenu();
    }

    //-----------------------------------------------------------------------------------------------------------------

    public interface IDeviceNetworkAPI
    {
        //void SubscribeStateUpdate(System.Action<DeviceInfo> callback);
    }


}


