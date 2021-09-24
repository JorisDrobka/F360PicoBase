using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace F360.Manage.States
{
    public class StateTransitionProcess
    {
        public readonly AppState From;
        public readonly AppState To;

        public StateTransitionProcess(AppState from, AppState to)
        {
            From = from;
            To = to;
        }
    }

    public interface IAppState
    {
        string Name { get; }            ///< identifier of app state
        bool isActive { get; }          ///< true when state is running or transitioning
        bool isRunning { get; }         ///< true when state main loop is running
        bool isTransitioning { get; }   ///< true when state is in transition (either from or to)
        bool isNextState { get; }       ///< (during transition) true if state is next state during transition
    
        void Prepare();                 ///< called before state transition
        void Update();                  ///< called during state process
        void Cleanup();                 ///< called before next state transition
    }
    
    

    public abstract class AppState
    {
        public readonly string Name;

        public bool isActive { get; private set; }
        public bool isRunning { get; private set; }
        public bool isTransitioning { get; private set; }
        public bool isNextState { get; private set; }
        public bool isPreviousState { get; private set; }
        
        public AppState(string name)
        {
            Name = name;
        }

        public StateTransitionProcess StartTransitionTo(AppState previous)
        {
            var t = new StateTransitionProcess(previous, this);
            return t;
        }

        protected abstract void onEnterState(AppState previous);
        protected abstract void onExitState(AppState next);
    }

    public class Entry
    {

    }

    public class LoadData
    {

    }

    public class StartSequence
    {

    }

    public class Menu
    {

    }

    public class Training
    {

    }
    

    public class PatchProcess
    {

    }

    public class PatchValidation
    {

    }

    public class Sleep
    {

    }

}

