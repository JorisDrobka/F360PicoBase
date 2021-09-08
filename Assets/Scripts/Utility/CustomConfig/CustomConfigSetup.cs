using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility.Config
{



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  SETUP
    //  
    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Data structure containing information on how a CustomConfig can be read from & written to
    ///
    public class CustomConfigSetup<TData, TContext, TValues> : CustomConfigSetup 
                                    where TData : class, IDataTarget<TContext>, new()
                                    where TContext : Enum 
                                    where TValues : Enum
    {

        public PackIndexWriter<TData> PackIndexWriter;


        Array contextMap;
        Array valueMap;
        LineValidator<TContext> lineValidator;
        LineReader<TContext> lineReader;
        ValueParser<TData, TContext, TValues> valueParser;
        ValueValidator<TContext, TValues> valueValidator;
        ValueDefaults<TData, TContext, TValues> valueDefaults;


        Dictionary<TContext, Dictionary<string, IDescriptor<TData, TContext, TValues>>> descriptors;
        Dictionary<string, (TContext context, ICustomConfigIO io)> childSetups;

        

        public CustomConfigSetup(string fileExtension, 
                                 ValueParser<TData, TContext, TValues> lineParser,
                                 ValueValidator<TContext, TValues> valueValidator,
                                 ValueDefaults<TData, TContext, TValues> valueDefaults,
                                 LineValidator<TContext> lineValidator=null,
                                 LineReader<TContext> lineReader=null) 
                                 : base(fileExtension) 
        {
            this.descriptors = new Dictionary<TContext, Dictionary<string, IDescriptor<TData, TContext, TValues>>>();
            this.contextMap = Enum.GetValues(typeof(TContext));
            this.valueMap = Enum.GetValues(typeof(TValues));
            this.valueParser = lineParser;
            this.valueValidator = valueValidator;
            this.valueDefaults = valueDefaults;
            this.lineValidator = lineValidator;
            this.lineReader = lineReader;
        }

        /// @brief
        /// Adds a new inner data target by context.
        ///
        /// @param context context term of the data object
        /// @param creator object constructor
        ///
        public void AddSingleContextRule<TContextTarget>(TContext context, ContextDataCreator creator) where TContextTarget : IContextData
        {   
            base.AddSingleContextRule<TContextTarget>(context.ToString(), creator);
        }
        /// @brief
        /// Adds a new inner data target by context that may appear multiple times
        ///
        /// @param context context term of the data object
        /// @param creator object constructor
        /// @param indexParser (optional) second term parser to allow direct indexing - i.e. "contextterm 1" or "contextterm title"
        ///
        public void AddMultiContextRule<TContextTarget>(TContext context, ContextDataCreator creator, ContextIndexParser<TContextTarget> indexParser=null) where TContextTarget : IContextData
        {
            base.AddMultiContextRule<TContextTarget>(context.ToString(), creator, indexParser);
        }
        public IContextRule GetContextRule(TContext context)
        {
            return base.GetContextRule(context.ToString());
        }


        /// @brief
        /// adds another read/write configuration to parse nested objects
        ///
        /// IMPORTANT: add a desriptor for the token with the Context ValueType set
        ///
        public void AddChildSetup<TChildData>(string token, TContext context, ICustomConfigIO io) where TChildData : IDataTarget, IContextData
        {
            token = token.ToLower();
            if(!string.IsNullOrEmpty(token) && io != null)
            {
                if(childSetups == null) childSetups = new Dictionary<string, (TContext, ICustomConfigIO)>();
                if(!childSetups.ContainsKey(token)) childSetups.Add(token, (context, io));
                else childSetups[token] = (context, io);
            }
        }

        public bool CheckChildContext(string line, out ICustomConfigIO io, out int contextID)
        {
            if(childSetups != null)
            {
                line = line.Trim().ToLower();
                if(childSetups.ContainsKey(line))
                {
                    io = childSetups[line].io;
                    contextID = IndexOfContext(childSetups[line].context.ToString());
                    return true;
                }
            }   
            io = null;
            contextID = -1;
            return false;
        }

        public override bool HasDescriptor(string descr)
        {
            if(descr.EndsWith(":")) descr = descr.Substring(0, descr.Length-1);
            foreach(var dict in descriptors.Values)
            {
                foreach(var entry in dict.Values)
                {
                    if(entry.Match(descr))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override bool HasDescriptor(string descr, string context)
        {
            
            if(descr.EndsWith(":")) descr = descr.Substring(0, descr.Length-1);
            var ctx = ContextFromName(context);
            if(!ctx.Equals(NullContext) && descriptors.ContainsKey(ctx))
            {
                foreach(var entry in descriptors[ctx].Values)
                {
                    if(entry.context.Equals(ctx) && entry.Match(descr))
                    {
//                        Debug.Log("has descriptor[" + descr + "]...ctx? [" + context + "]---> " + RichText.emph(RichText.darkGreen("True")) + "\n" + PrintDescriptors());
                        return true;
                    }
                }
            }
//            Debug.Log("has descriptor[" + descr + "]...ctx? [" + context + "]---> " + RichText.emph(RichText.darkRed("False")) + "\n" + PrintDescriptors());
            return false;
        }

        /// @brief
        /// adds a descriptor marking the opening of a new context
        ///
        public CustomConfigSetup AddContextDescriptor(string descriptor, TContext context, TValues type, params string[] aliases)
        {
            return addDescriptor(descriptor, true, context, type, aliases);
        }

        /// @brief
        /// adds a value descriptor without a setter function to existing context
        ///
        public CustomConfigSetup AddDescriptor(string descriptor, TContext context, TValues type, params string[] aliases)
        {
            return addDescriptor(descriptor, false, context, type, aliases);
        }
        /// @brief
        /// adds a value descriptor with a setter function to existing context
        ///
        public CustomConfigSetup AddDescriptor<TValue>(string descriptor, TContext context, TValues type, DataSetter<TData, TValue> setter, params string[] aliases)
        {
            return addDescriptor<TValue>(descriptor, false, context, type, setter, aliases);
        }
        /// @brief
        /// adds a value descriptor with a setter function to existing context
        ///
        public CustomConfigSetup AddDescriptor<TTarget, TValue>(string descriptor, TContext context, TValues type, ContextDataSetter<TTarget, TValue> setter, params string[] aliases)
                                            where TTarget : IContextData
        {
            return addInnerDescriptor<TTarget, TValue>(descriptor, context, type, setter, aliases);
        }


        public IDescriptor<TData, TContext, TValues> GetDescriptor(string descr)
        {
            TContext context;
            if(GetDescriptorAliasSafe(ref descr, out context))
            {
                return descriptors[context][descr];
            }
            return null;
        }

        public IDescriptor<TData, TContext, TValues> GetDescriptor(string descr, TContext context)
        {
            if(GetDescriptorAliasSafe(ref descr, context))
            {
                return descriptors[context][descr];
            }
            return null;
        }

        

        protected override IDescriptor<TData2, TContext2, TValues2> GetEntryInternal<TData2, TContext2, TValues2>(string descr)
        {
            TContext context;
            if(GetDescriptorAliasSafe(ref descr, out context))
            {
                if(typeof(TData2) == typeof(TData)
                    && typeof(TContext2) == typeof(TContext)
                    && typeof(TValues2) == typeof(TValues))
                {
                    //  boxing
                    var obj = (object)descriptors[context][descr];
                    return (IDescriptor<TData2, TContext2, TValues2>)obj;
                }
            }
            return null;
        }

        public string PrintDescriptors()
        {
            var b = new System.Text.StringBuilder();
            foreach(var dict in descriptors.Values)
            {
                foreach(var entry in dict.Values)
                {
                    b.Append("[" + entry.context.ToString() + "]");
                    b.Append(RichText.emph(entry.descriptor));
                    if(entry.isContextDescriptor) b.Append(" (context)");
                    b.Append("\n");
                }
            }
            return b.ToString();
        }

        bool GetDescriptorAliasSafe(ref string descr, out TContext context)
        {
            foreach(var dict in descriptors.Values)
            {
                foreach(var entry in dict.Values)
                {
                    if(entry.Match(descr))
                    {
                        descr = entry.descriptor;
                        context = entry.context;
                        return true;
                    }
                }
            }
            context = NullContext;
            return false;
        }
        bool GetDescriptorAliasSafe(ref string descr, TContext context)
        {
            if(descriptors.ContainsKey(context))
            {
                foreach(var entry in descriptors[context].Values)
                {
                    if(entry.Match(descr))
                    {
                        descr = entry.descriptor;
                        return true;
                    }
                }
            }
            return false;
        }



        public void SetPackingRules(PackIndexParser indexParser,
                                    PackIndexWriter<TData> indexWriter, 
                                    string prefix=CustomConfigIO.TOKEN_PACKED_META_PREFIX, 
                                    string suffix=CustomConfigIO.TOKEN_PACKED_META_SUFFIX)
        {
            PackIndexWriter = indexWriter;
            base.SetPackingRules(indexParser, prefix, suffix);
        }

        public override bool HasPackingRules()
        {
            return base.HasPackingRules() && PackIndexWriter != null;
        }

        public override string FormatPackingIndex(object target)
        {
            var tdata = target as TData;
            if(tdata != null)
            {
                return PackIndexWriter(tdata);
            }   
            return "";
        }

        public TContext NullContext 
        {
            get { return contextFromID(0); }
        }
        public TValues NullValueType
        {
            get { return valueFromID(0); }
        }

        public bool ValidateValue(TValues valueType, object value)
        {
            return valueValidator(valueType, value);
        }
        protected override bool ValidateValue(int valueID, object value)
        {
            return valueValidator(valueFromID(valueID), value);
        }

        public object GetDefaultValue(TValues valueType)
        {
            return valueDefaults(this, valueType);
        }
        protected override object GetDefaultValue(int valueID)
        {
            return valueDefaults(this, valueFromID(valueID));
        }

        public bool LineMatchesContext(string line, TContext context)
        {
            if(lineValidator != null)
            {
                return lineValidator(this, line, context);
            }
            else
            {
                return HasDescriptor(line, context.ToString());
            }
        }

        /// @brief
        /// tries to parse an indexer from the end of given line
        /// will terminate if descriptor-token ':' is encountered.
        /// is called before CheckNewContext()
        ///
        public bool GetContextIndexer(ref string line, out string indexer)
        {   
            line = line.Trim();
            if(!line.Contains(":"))
            {
                var splt = line.Split(' ');
                if(splt.Length == 2)
                {
                    indexer = splt[1];
                    line = splt[0];
                    return true;
                }
            }
            indexer = "";
            return false;
/*

            if(!line.Contains(":"))
            {
                int sub = -1;
                for(int i = line.Length-1; i >= 0; i--)
                {
                    if(System.Char.IsDigit(line[i]))
                    {
                        sub = line.Length-i;
                    }
                    else
                    {
                        break;
                    }
                }

                if(sub != -1)
                {
                    var term = line.Substring(line.Length-sub, sub);
            //        Debug.Log(RichText.darkRed("REMOVED indexer from context!") + " term: <" + term + ">\nline: " + line);
                    
                    line = line.Substring(0, line.Length-sub).Trim();
                    if(System.Int32.TryParse(term, out index))
                    {
                        return true;
                    }
                }
            }
            index = -1;
            return false;*/
        }

        public bool CheckNewContext(string line, out TContext context)
        {
            line = line.Trim();
            if(HasDescriptor(line))
            {
                var descr = GetDescriptor(line);
                if(descr != null && descr.isContextDescriptor)
                {
                    context = descr.context;
                    //Debug.Log(RichText.emph("Context from descr:: ") + "<" + descr.descriptor + "> // " + descr.context + " " + descr.valueType);
                    return true;
                }
                else
                {
                    //Debug.Log(this.GetType().ToString() + " " + RichText.darkRed((descr != null).ToString()) + " ??? " + (descr != null ? ("[" + descr.context + "] " + descr.descriptor.ToString() +  "/ " + descr.isContextDescriptor) : ""));
                }
            }

            //  fallback: search directly for context names if no specific descriptor was set
            for(int i = 0; i < contextNames.Length; i++)
            {
                if(line.Contains(contextNames[i]))
                {
                    context = ContextFromName(contextNames[i]);
                    //Debug.Log(RichText.emph("Context from name:: ") + context);
                    return true;
                }
            }
            context = NullContext;
            return false;
        }
        public override bool CheckNewContext(string line, out int context)
        {
            TContext ctx;
            if(CheckNewContext(line, out ctx))
            {
                context = IndexOfContext(ctx.ToString());
                return true;
            }
            context = 0;
            return false;
        }

        internal void EmptyLine(TContext current, int index)
        {
            Term t;
            if(lineReader != null)
            {
                lineReader(this, current, "", index, out t);
            }   
            else
            {
                CustomConfigIO.DefaultLineReader<TData, TContext, TValues>(this, current, "", index, out t);
            }
        }

        internal bool TryReadLine(TContext current, string line, int index, out Term term)
        {
            if(lineReader != null)
            {
                return lineReader(this, current, line, index, out term);
            }
            else
            {
                return CustomConfigIO.DefaultLineReader<TData, TContext, TValues>(this, current, line, index, out term);
            }
        }
        internal bool TryParseValue(string raw, TValues type, out object val)
        {
            return valueParser(this, type, raw, out val);
        }

        internal TContext ContextFromID(int id)
        {
            if(id >= 0 && id < contextMap.Length) return (TContext) contextMap.GetValue(id);
            else return NullContext;
        }
        internal TValues ValueTypeFromID(int id)
        {   
            if(id >= 0 && id < valueMap.Length) return (TValues) valueMap.GetValue(id);
            else return NullValueType;
        }
        internal TContext ContextFromName(string context)
        {
            int id = IndexOfContext(context);
            if(id != -1)
            {
                return contextFromID(id);
            }
            return NullContext;
        }
        internal TValues ValueTypeFromName(string type)
        {
            int id = IndexOfValue(type);
            if(id != -1)
            {
                return valueFromID(id);
            }
            return NullValueType;
        }

        internal new IEnumerable<TContext> GetWriteableContexts()
        {
            for(int i = 1; i < contextMap.Length; i++)
            {
                yield return (TContext) contextMap.GetValue(i);
            }
        }

        

        protected override string[] GetContextNamesInternal()
        {
            return Enum.GetNames(typeof(TContext));
        }
        protected override string[] GetValueNamesInternal()
        {
            return Enum.GetNames(typeof(TValues));
        }

        protected override bool CheckLineMatchesContext(string line, int contextId)
        {
            if(lineValidator != null)
            {
//                Debug.Log("checkLineMatch via validator");
                return lineValidator(this, line, contextFromID(contextId));
            }
            else if(contextId >= 0 && contextId < contextNames.Length)
            {
//                Debug.Log("checkLineMatch via descriptor\n" + PrintDescriptors());
                return HasDescriptor(line, contextNames[contextId]);
            }
            else
            {
                return false;
            }
        }

        protected override bool ParseTermValue(int valueId, string raw, out object val)
        {
            return valueParser(this, valueFromID(valueId), raw, out val);
        }


        TContext contextFromID(int contextId)
        {
            if(contextId >= 0 && contextId < contextMap.Length)
            {
                return (TContext) contextMap.GetValue(contextId);
            }
            return (TContext) contextMap.GetValue(0);
        }
        TValues valueFromID(int valueId)
        {
            if(valueId >= 0 && valueId < valueMap.Length)
            {
                return (TValues) valueMap.GetValue(valueId);
            }
            return (TValues) valueMap.GetValue(0);
        }


        //-----------------------------------------------------------------------------------------------------------------


        CustomConfigSetup addDescriptor(string descriptor, bool isContext, TContext context, TValues type, params string[] aliases)
        {
            if(!string.IsNullOrEmpty(descriptor))
            {
                if(!descriptors.ContainsKey(context)) descriptors.Add(context, new Dictionary<string, IDescriptor<TData, TContext, TValues>>());
                if(!descriptors[context].ContainsKey(descriptor))
                {
                    //Debug.Log(RichText.darkGreen("added descriptor") + "<" + descriptor + "> [" + context + "]  isContext? " + isContext);
                    descriptors[context].Add(descriptor, new MainEntry<TData, TContext, TValues, bool>(context, descriptor, type, isContext, aliases));
                }
                else
                {   
                    Debug.LogWarning("Unable to Add descriptor<" + descriptor + "> [" + context + "], already exists!");
                }   
            }
            return this;
        }
        CustomConfigSetup addDescriptor<TValue>(string descriptor, bool isContext, TContext context, TValues type, DataSetter<TData, TValue> setter, params string[] aliases)
        {
            if(!string.IsNullOrEmpty(descriptor))
            {
                if(!descriptors.ContainsKey(context)) descriptors.Add(context, new Dictionary<string, IDescriptor<TData, TContext, TValues>>());
                if(!descriptors[context].ContainsKey(descriptor))
                {
                    //Debug.Log(RichText.darkGreen("added descriptor") + "<" + descriptor + "> [" + context + "] isContext? " + isContext);
                    descriptors[context].Add(descriptor, new MainEntry<TData, TContext, TValues, TValue>(context, descriptor, type, setter, isContext, aliases));
                }
                else
                {   
                    Debug.LogWarning("Unable to Add descriptor<" + descriptor + "> [" + context + "], already exists!");
                }  
            }
            return this;
        }
        CustomConfigSetup addInnerDescriptor<TTarget, TValue>(string descriptor, TContext context, TValues type, ContextDataSetter<TTarget, TValue> setter, params string[] aliases)
                                                                        where TTarget : IContextData
        {
            if(!string.IsNullOrEmpty(descriptor))
            {
                if(!descriptors.ContainsKey(context)) descriptors.Add(context, new Dictionary<string, IDescriptor<TData, TContext, TValues>>());
                if(!descriptors[context].ContainsKey(descriptor))
                {
                    //Debug.Log(RichText.darkGreen("added descriptor") + "<" + descriptor + "> [" + context + "]");
                    descriptors[context].Add(descriptor, new InnerEntry<TData, TTarget, TContext, TValues, TValue>(context, descriptor, type, setter, aliases));
                }
                else
                {   
                    Debug.LogWarning("Unable to Add descriptor<" + descriptor + "> [" + context + "], already exists!");
                }  
            }
            return this;
        }
    }



}