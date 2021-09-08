using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility.Config
{


    public enum ContextMode
    {
        Singular=0,
        Multiple
    }


    public enum ValueState
    {
        Undefined,
        
        Valid,
        Invalid,

        DataError,
        DescriptorError,
    }

    

    //-----------------------------------------------------------------------------------------------------------------
    //
    //  SETUP BASE
    //  
    //-----------------------------------------------------------------------------------------------------------------


    public abstract class CustomConfigSetup : ICustomIOSetup
    {
        public readonly string FileExtension;
        public int PackDigits = CustomConfigIO.PACK_DIGITS;
        public int PackLineSpacing = CustomConfigIO.PACK_LINE_SPACING;
        public string PackPrefix = CustomConfigIO.TOKEN_PACKED_META_PREFIX;
        public string PackSuffix = CustomConfigIO.TOKEN_PACKED_META_SUFFIX;
        public PackIndexParser PackIndexParser;

        protected readonly string[] contextNames;
        protected readonly string[] valueNames;
        protected Dictionary<string, IContextRule> contextRules;
        

        //-----------------------------------------------------------------------------------------------------------------

        public CustomConfigSetup(string fileExtension)
        {
            if(!fileExtension.StartsWith(".")) fileExtension = "." + fileExtension;
            this.FileExtension = fileExtension;
            contextNames = GetContextNamesInternal();
            valueNames = GetValueNamesInternal();
        }

        public void SetPackingRules(PackIndexParser indexParser, 
                                    string prefix=CustomConfigIO.TOKEN_PACKED_META_PREFIX, 
                                    string suffix=CustomConfigIO.TOKEN_PACKED_META_SUFFIX)
        {
            PackIndexParser = indexParser;
            PackPrefix = prefix;
            PackSuffix = suffix;
        }

        /// @brief
        /// Adds a new inner data target by context.
        ///
        /// @param context context term of the data object
        /// @param creator object constructor
        ///
        public void AddSingleContextRule<TContextTarget>(string context, ContextDataCreator creator) where TContextTarget : IContextData
        {   
            if(contextNames.Contains(context))
            {
                if(contextRules == null) contextRules = new Dictionary<string, IContextRule>();
                if(!contextRules.ContainsKey(context)) contextRules.Add(context, new ContextRuleA(ContextMode.Singular, creator));
                else contextRules[context] = new ContextRuleA(ContextMode.Singular, creator);
            }
            else
            {
                Debug.LogWarning("ConfigSetup:: cannot add singular context rule, name=[" + context + "] does not exist!");
            }
        }
        /// @brief
        /// Adds a new inner data target by context that may appear multiple times
        ///
        /// @param context context term of the data object
        /// @param creator object constructor
        /// @param indexParser (optional) second term parser to allow direct indexing - i.e. "contextterm 1" or "contextterm title"
        ///
        public void AddMultiContextRule<TContextTarget>(string context, ContextDataCreator creator, ContextIndexParser<TContextTarget> indexParser=null) 
                                                        where TContextTarget : IContextData
        {
            if(contextNames.Contains(context))
            {
                if(contextRules == null) contextRules = new Dictionary<string, IContextRule>();
                if(!contextRules.ContainsKey(context)) contextRules.Add(context, new ContextRuleB<TContextTarget>(ContextMode.Multiple, creator, indexParser));
                else contextRules[context] = new ContextRuleB<TContextTarget>(ContextMode.Multiple, creator, indexParser);
            }
            else
            {
                Debug.LogWarning("ConfigSetup:: cannot add multi context rule, name=[" + context + "] does not exist!");
            }
        }
        public bool hasContextRule(string context) 
        { 
            return contextRules != null && contextRules.ContainsKey(context);
        }
        public IContextRule GetContextRule(string context)
        {
            if(contextRules != null && contextRules.ContainsKey(context))
            {
                return contextRules[context];
            }
            return null;
        }

        /*public abstract CustomConfigSetup AddDescriptor(string descriptor, string context, string type);
        public abstract CustomConfigSetup AddDescriptor(string descriptor, string context, string type, ConfigDataSetter setter);
        public abstract CustomConfigSetup AddDescriptor(string descriptor, string context, string type, ContextDataSetter setter);*/

        public abstract bool HasDescriptor(string descr);
        public abstract bool HasDescriptor(string descr, string context);
        
        public IDescriptor<TData, TContext, TValues> GetDescriptorEntry<TData, TContext, TValues>(string descr) 
                    where TData : class, IDataTarget<TContext>, new() 
                    where TContext : Enum
                    where TValues : Enum
        {
            if(HasDescriptor(descr))
            {   
                return GetEntryInternal<TData, TContext, TValues>(descr);
            }
            else return null;
        }

        protected abstract IDescriptor<TData, TContext, TValues> GetEntryInternal<TData, TContext, TValues>(string descr) 
                                where TData : class, IDataTarget<TContext>, new() 
                                where TContext : Enum
                                where TValues : Enum;

        public virtual bool HasPackingRules()
        {
            return PackIndexParser != null 
                    && !string.IsNullOrEmpty(PackPrefix) 
                    && !string.IsNullOrEmpty(PackSuffix);
        }

        public IEnumerable<string> GetWriteableContexts()
        {
            for(int i = 1; i < contextNames.Length; i++)
            {
                yield return contextNames[i];
            }
        }
        
        public abstract bool CheckNewContext(string line, out int context);

        public bool LineMatchesContext(string line, string context)
        {
            int id = IndexOfContext(context);
            if(id != -1)
            {
                return CheckLineMatchesContext(line, id);
            }
            return false;
        }

        
        public object GetDefaultValue(string type)
        {
            int id = IndexOfValue(type);
            if(id != -1)
            {
                return GetDefaultValue(id);
            }
            return null;
        }


        internal bool TryParseValue(string raw, string type, out object val)
        {
            int id = IndexOfValue(type);
            if(id != -1)
            {
                return ParseTermValue(id, raw, out val);
            }
            val = null;
            return false;
        }

        internal bool GetDescriptor(string line, string context, out string descriptor, out string content)
        {
            descriptor = "";
            content = "";

            int id = line.IndexOf(":");
            if(id != -1)
            {
                descriptor = line.Substring(0, id);
                descriptor = descriptor.Trim();
                content = line.Substring(id+1, line.Length-id-1);
                if(HasDescriptor(descriptor))
                {   
                    content = content.Trim();
                    return true;
                }
            }
            return false;
        }

        public abstract string FormatPackingIndex(object target);

        protected abstract string[] GetContextNamesInternal();
        protected abstract string[] GetValueNamesInternal();

        protected abstract bool CheckLineMatchesContext(string line, int contextId);

        protected abstract bool ValidateValue(int valueID, object value);
        protected abstract object GetDefaultValue(int valueID);
        protected abstract bool ParseTermValue(int valueID, string raw, out object val);

        protected int IndexOfContext(string context)
        {
            return System.Array.IndexOf(contextNames, context);
        }
        protected int IndexOfValue(string valueType)
        {
            return System.Array.IndexOf(valueNames, valueType);
        }


        bool ICustomIOSetup.hasDescriptor(string descriptor) { return HasDescriptor(descriptor); }
        bool ICustomIOSetup.hasDescriptor(string descriptor, string context) { return HasDescriptor(descriptor); }



        //-----------------------------------------------------------------------------------------------------------------


        public interface IContextRule
        {
            ContextMode Mode { get; }
            ContextDataCreator Creator { get; }

            bool TryParseIndex(IContextData data, string term);
        }

        public struct ContextRuleA : IContextRule
        {
            public ContextMode Mode;
            public ContextDataCreator Creator;

            public ContextRuleA(ContextMode m, ContextDataCreator c)
            {
                this.Mode = m;
                this.Creator = c;
            }

            public bool TryParseIndex(IContextData data, string term)
            {
                return false;
            }

            ContextMode IContextRule.Mode { get { return Mode; } }
            ContextDataCreator IContextRule.Creator { get { return Creator; } }
        }
        public struct ContextRuleB<TContextTarget> : IContextRule where TContextTarget : IContextData
        {
            public ContextMode Mode;
            public ContextDataCreator Creator;
            public ContextIndexParser<TContextTarget> IndexParser;

            public ContextRuleB(ContextMode m, ContextDataCreator c, ContextIndexParser<TContextTarget> p)
            {
                this.Mode = m;
                this.Creator = c;
                this.IndexParser = p;
            }

            public bool TryParseIndex(IContextData data, string term)
            {
                if(IndexParser != null && data is TContextTarget)
                {
                    return IndexParser((TContextTarget)data, term);
                }
                return false;
            }

            ContextMode IContextRule.Mode { get { return Mode; } }
            ContextDataCreator IContextRule.Creator { get { return Creator; } }
        }
    }


}