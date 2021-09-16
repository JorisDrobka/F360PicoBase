
using System;
using System.Collections;
using System.Collections.Generic;


namespace F360.Backend.Synch
{


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  INTERFACES
    //
    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Single database entry that can be synchronized with server.
    ///
    public interface ISynchronizedData
    {
        RESTMethod Method { get; }
        SynchedURI Uri { get; }             ///< resource identifier

        DateTime Timestamp { get; set; }    ///< last synch time
 
        string Value { get; }               ///< currently held, string-formatted value
        
        bool ForceSinglePush();             //  needed for json

        string Readable();
    }

    public interface IDataSynchClient : IClient
    {
        void AddRepo(ISynchronizedRepo repo);
        void RemoveRepo(ISynchronizedRepo repo);

        bool Pull(Database database, DateTime timestamp, int userID, System.Action<int, bool> whendone=null);
        //bool PullMultiple(Database database, DateTime timestamp, IEnumerable<int> userIDs, System.Action<int> whendone=null);
        bool Push(bool saveAfterwards=true);
    }

    public interface ISynchronizedRepo
    {
        Database Database { get; }

        bool isDirty();
        bool Save();

        string GetName();
        bool MatchURI(SynchedURI uri);

        IEnumerable<ISynchronizedData> GetChanges();
        SynchState PushChange(ISynchronizedData d);
        void ClearChanges();
        
    }
    
    

    /*public interface ISynchronizedDataCollection
    {
        string Uri { get; }
        
        bool isDirty();

        IEnumerable<ISynchronizedData> GetData();
        IEnumerable<ISynchronizedData> GetChanges();
    }*/




    //-----------------------------------------------------------------------------------------------------------------
    //
    //  STATE
    //
    //-----------------------------------------------------------------------------------------------------------------

    public enum SynchState
    {
        Unchanged,
        Created,
        Updated,
        Deleted,
        SendToServer,

        Error_Invalid_Uri,
        Error_Invalid_Repo,
        Error_Missing_Key,
        Error_Malformatted_Data,
        Error_Connection
    }

    public static class SynchStateHelper
    {
        public static bool isError(this SynchState state)
        {
            switch(state)
            {
                case SynchState.Error_Invalid_Repo:
                case SynchState.Error_Invalid_Uri:
                case SynchState.Error_Malformatted_Data:
                case SynchState.Error_Missing_Key:
                case SynchState.Error_Connection:           return true;
                default:                                    return false;
            }
        }
    }

}