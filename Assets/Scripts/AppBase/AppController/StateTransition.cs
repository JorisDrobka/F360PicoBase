using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace F360.Manage.States
{


    public enum StateTransitionConditionType
    {
        None=0,
        StringVal,          ///< transition is allowed if statemap string has defined value
        IntVal,             ///< transition is allowed if statemap integer has defined value
        BoolVal,            ///< transition is allowed if statemap boolean has defined value
        AnyVal,             ///< transition is allowed if statemap value has changed
        Function            ///< custom condition function with access to state map
    }


    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Defines a possible transition between two states with an optional condition.
    /// The condition can either be a custom function or a value change within the state map
    ///
    public class StateTransition
    {
        public readonly string From;
        public readonly string To;

        public StateTransitionConditionType Type { get; private set; }

        public StateTransition(string from, string to)
        {
            this.From = from;
            this.To = to;
        }

        public bool CheckCondition(StateValueMap map)
        {
            if(func != null) return func(map);
            else if(map.HasChangedValue(valueID))
            {
                switch(Type)
                {
                    case StateTransitionConditionType.StringVal:
                        if(ignoreStringCases)
                            return map.GetString(valueID).ToLower() == targetString;
                        else 
                            return map.GetString(valueID) == targetString;
                            
                    case StateTransitionConditionType.IntVal:
                        return map.GetInt(valueID) == targetInt;

                    case StateTransitionConditionType.BoolVal:
                        return map.GetBool(valueID) == targetBool;

                    case StateTransitionConditionType.AnyVal:
                        return true;
                }
            }
            return false;
        }
        
        public void SetCondition(string valueID)
        {
            this.valueID = valueID;
            this.Type = StateTransitionConditionType.AnyVal;
        }
        public void SetCondition(string valueID, string value, bool ignoreCases=true)
        {
            this.valueID = valueID;
            this.targetString = value;
            this.ignoreStringCases = ignoreCases;
            this.Type = StateTransitionConditionType.StringVal;
        }
        public void SetCondition(string valueID, int value)
        {
            this.valueID = valueID;
            this.targetInt = value;
            this.Type = StateTransitionConditionType.IntVal;
        }
        public void SetCondition(string valueID, bool value)
        {
            this.valueID = valueID;
            this.targetBool = value;
            this.Type = StateTransitionConditionType.BoolVal;
        }
        public void SetCondition(Func<StateValueMap, bool> condition)
        {
            this.func = condition;
            this.Type = StateTransitionConditionType.Function;
        }



        string valueID;
        string targetString;
        int targetInt;
        bool targetBool;
        bool ignoreStringCases;
        Func<StateValueMap, bool> func;
    }


}