using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360.Manage.States
{

    public interface IAppStateListener
    {
        void onEnter(string previous);
        void onExit(string next);
    }



    public class AppStateController : MonoBehaviour
    {
        const string TOKEN = "AppController:: ";
        


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Properties

        public string CurrentState { get { return state != null ? state.Name : ""; } }
        public string NextState { get { return transition != null ? transition.To.Name : ""; } }
        public bool isTransitioning { get { return transition != null; } }


        public IAppState EntryState { get; private set; }       ///< representation of the entry point to the state machine. 
        public IAppState AnyState { get; private set; }         ///< can be used when setting up conditional transitions.
        
        

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Events

        public event Action<string> onBeginState;
        public event Action<string> onEndState;
        public event Action<string, string> onStateTransition;


        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Interface

        public bool hasState(string name) { return allStates.ContainsKey(name); }

        public bool GotoState(string name) { return gotoStateInternal(name); }
        
        public void AddState(IAppState state) { addStateInternal(state); }

        public void AddStateListener(string name, IAppStateListener listener) { addListenerInternal(name, listener); }

        public void AddTransition(IAppState from, IAppState to, string mapID, string state)
        {

        }
        public void AddTransition(IAppState from, IAppState to, string mapID, int state)
        {

        }
        public void AddTransition(IAppState from, IAppState to, string mapID, bool state)
        {

        }
        public void AddTransition(IAppState from, IAppState to, Func<bool> condition)
        {

        }

        

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Internal

        void addStateInternal(IAppState state)
        {
            if(!allStates.ContainsKey(state.Name))
            {
                allStates.Add(state.Name, state);
            }
            else
            {
                Debug.LogWarning(TOKEN + "cannot add state<" + state.Name + ">, already exists!");
            }
        }

        void addListenerInternal(string name, IAppStateListener listener)
        {
            if(allStates.ContainsKey(name))
            {   
                if(!listeners.ContainsKey(name))
                {
                    listeners.Add(name, new List<IAppStateListener>());
                    listeners[name].Add(listener);
                }
                else if(!listeners[name].Contains(listener))
                {
                    listeners[name].Add(listener);
                }
            }
            else
            {
                Debug.LogWarning(TOKEN + " cannot add listener, state<" + name + "> does not exist!");
            }
        }

        bool gotoStateInternal(string name)
        {
            if(allStates.ContainsKey(name))
            {
                if(CurrentState != name)
                {
                    processGotoState(allStates[name]);
                    return true;
                }   
            }
            else
            {
                Debug.LogWarning(TOKEN + "state<" + name + "> does not exist!");
            }
            return false;
        }

        void processGotoState(IAppState next)
        {
            var prev = state; 
        }



        void Update()
        {
            if(state != null)
            {
                
            }
        }

        //-----------------------------------------------------------------------------------------------------------------
        //
        //  Init


        IAppState state;
        StateTransitionProcess transition;

        Dictionary<string, IAppState> allStates;
        Dictionary<string, List<IAppStateListener>> listeners;


        void Awake()
        {
            allStates = new Dictionary<string, IAppState>();
            listeners = new Dictionary<string, List<IAppStateListener>>();
        }



        
    }


}

