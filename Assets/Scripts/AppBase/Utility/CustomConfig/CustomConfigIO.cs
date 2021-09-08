using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utility.Config
{   



    /// @brief
    /// context-bound dataset that can be read from/written to by CustomConfigAPI
    ///
    public interface IDataTarget<TContext> : IDataTarget where TContext : Enum
    {
        /// @brief
        /// receive inner data objects after parsing
        /// 
        void PushContextData(TContext context, IContextData[] data);   
    }


    /// @brief      
    /// basic interface for datasets that can be read/written by CustomConfig API
    /// Use the Context-bound version of this interface.
    ///
    public interface IDataTarget
    {
        /// @brief
        /// index of data (or 0 if no other datasets of this type exists)
        ///
        int Index { get; }


        /// @returns if data was parsed successfully
        ///
        bool isValid();

        string Readable(bool deep);
    }


    /// @brief
    /// Inner dataset bound to an outer dataset and a specific context.
    ///
    /// Important: IContextData can only be inherited by classes with parameterless constructors 
    ///
    public interface IContextData
    {
        /// @brief
        /// the context of this inner dataset
        ///
        string Context { get; }

        /// @brief
        /// to outer config object (automatically set by reader)
        ///
        IDataTarget Parent { get; set; }                                        /// @TODO: SET from reader

        /// @brief
        /// inner flag marking data cannot be written to anymore
        ///
        bool isClosed { get; set; }
    }


    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// helper object to selectively write specific contexts only
    ///
    public interface IWriteConstraint<TContext>
    {
        bool hasContext(TContext ctx);
    }


    //-----------------------------------------------------------------------------------------------------------------


    public interface ICustomConfigIO
    {
        ICustomIOSetup GetSetup();
        ICustomReader GetReader();
        ICustomWriter GetWriter();
    }

    public interface ICustomIOSetup
    {
        bool hasDescriptor(string descriptor);
        bool hasDescriptor(string descriptor, string context);
    }

    public interface ICustomReader
    {
        ICustomIOSetup Setup { get; }
        bool logging { get; set; }

        bool ParseStream(string file, StreamReader reader, ref int lineIndex, ref string line, out IDataTarget data);
    }

    public interface ICustomWriter
    {
        ICustomIOSetup Setup { get; }

        bool logging { get; set; }

        bool WriteStream(StreamWriter writer, IDataTarget data);
    }
        

    //-----------------------------------------------------------------------------------------------------------------

    public static class CustomConfigIO
    {
        public const int PACK_DIGITS = 3;
        public const int PACK_LINE_SPACING = 5;
        public const string TOKEN_PACKED_META_PREFIX = "[ ";
        public const string TOKEN_PACKED_META_SUFFIX = " ]";
        public const string UNDEFINED = "UNDEFINED";
        public const int TAB_DEFAULT = 2;


        public static bool GetDescriptor<TContext>(this CustomConfigSetup setup, string line, TContext context, out string descriptor, out string content) where TContext : Enum
        {
            descriptor = "";
            content = "";

            int id = line.IndexOf(":");
            if(id != -1)
            {
                descriptor = line.Substring(0, id);
                descriptor = descriptor.Trim();
                content = line.Substring(id+1, line.Length-id-1);
                if(setup.HasDescriptor(descriptor))
                {   
                    content = content.Trim();
                    return true;
                }
            }
            return false;
        }

        public static bool DefaultLineReader<TData, TContext, TValues>(CustomConfigSetup<TData, TContext, TValues> setup, TContext context, string line, int lineIndex, out Term term)
                                                where TData : class, IDataTarget<TContext>, new()
                                                where TContext : Enum
                                                where TValues : Enum
        {
            string ctx = context.ToString();
            string descr, content;
            if(setup.GetDescriptor(line, context, out descr, out content))
            {
                if(setup.LineMatchesContext(descr, ctx))
                {
                    term = new Term(ctx, descr, content, lineIndex);
                    return true;
                }
                else if(!context.Equals(setup.NullContext))
                {
                    //  line format error
                    Debug.LogWarning("Line Format Error:: does not match context!\n\tline: " + line + "\n\tcontext: " + context
                                     + "\n\tdescriptor: " + descr + "   \tcontent: " + content + "\nSetup<" + typeof(TData).Name + ", " + typeof(TContext).Name + ">");
                }
            }
            else if(setup.LineMatchesContext(line, ctx))
            {
                term = new Term(setup, ctx, line, lineIndex);
                return true;
            }
            else
            {   
                //  line format error
                //Debug.LogWarning("Line Format Error:: does not match context!\n\tline: " + line + "\n\tcontext: " + context);
            }
            term = default(Term);
            return false;
        }


        public static string GetPackedFileName(this CustomConfigSetup setup, int packing, int packID, string extensionOverride="")
        {
            UnityEngine.Assertions.Assert.IsTrue(setup.HasPackingRules(), "CustomConfig:: config not setup for packing!");
            int start = (packID * packing) + 1;
            int end = start + packing - 1;
            string ext = !string.IsNullOrEmpty(extensionOverride) ? extensionOverride : setup.FileExtension;
            return FileSystemUtil.FormatIDWithLeadingZeros(start, setup.PackDigits) + "-" + FileSystemUtil.FormatIDWithLeadingZeros(end, setup.PackDigits) + ext;
            //return setup.FormatVideoID(start) + "-" + F360FileSystem.FormatVideoID(end) + setup.FileExtension;
        }

        public static bool isPackedFile(this CustomConfigSetup setup, string fileName)
        {
            if(setup.HasPackingRules())
            {
                int p1, p2;
                return UnpackFile(setup, fileName, out p1, out p2);
            }
            else
            {
                return false;
            }
        }

        public static bool UnpackFile(this CustomConfigSetup setup, string fileName, out int packing, out int packID)
        {
            UnityEngine.Assertions.Assert.IsTrue(setup.HasPackingRules(), "CustomConfig:: config not setup for packing!");
            if(fileName.EndsWith(setup.FileExtension))
            {
                fileName = FileSystemUtil.RemoveFolderFromFilePath(fileName);
                fileName = FileSystemUtil.RemoveFileExtension(fileName);
                string[] splt = fileName.Split('-');
                if(splt.Length == 2)
                {
                    int n1, n2;
                    if(setup.PackIndexParser(splt[0].Trim(), out n1)
                        && setup.PackIndexParser(splt[1].Trim(), out n2))
                    {
                        packID = n1;
                        packing = n2-n1;
                        return true;   
                    }
                    /*if(F360FileSystem.ParseVideoID(splt[0].Trim(), out n1)
                        && F360FileSystem.ParseVideoID(splt[1].Trim(), out n2))
                    {
                        packID = n1;
                        packing = n2-n1;
                        return true;   
                    }*/
                }
            }
            packing = -1;
            packID = -1;
            return false;
        }

        internal static bool GetPackingContext(this CustomConfigSetup setup, string line, out int videoID)
        {
            UnityEngine.Assertions.Assert.IsTrue(setup.HasPackingRules(), "CustomConfig:: config not setup for packing!");
            string inner;
            if(FileSystemUtil.TryFormatTerm(ref line, setup.PackPrefix, setup.PackSuffix, out inner, addTokensToInnerTerm: false))
            {
                return setup.PackIndexParser(inner, out videoID);
            }
            videoID = -1;
            return false;
        }

        

        internal static bool ValidatePackingIndices<TContext>(int packing, int packID, IEnumerable<IDataTarget<TContext>> data) where TContext : Enum
        {
            foreach(var d in data)
            {
                if(!ValidatePackingIndex(packing, packID, d.Index)) return false;
            }
            return true;
        }

        internal static bool ValidatePackingIndex(int packing, int packID, int videoID)
        {
            if(packing > 1)
                return (videoID-1) / packing == packID;
            return false;
        }

        public static string ReformatTabbing(string term, int insertAt=-1, int tabSpacing=TAB_DEFAULT, bool debug=false)
        {
            const int TAB_CHARS = 8;

            string prefix = term.Trim();
            string suffix = "";
            
            if(insertAt >= 0 && insertAt < term.Length-1)
            {
                prefix = term.Substring(0, insertAt).Trim();
                suffix = term.Substring(insertAt, term.Length-insertAt).Trim();
            }
            
            int lastTab = term.LastIndexOf("\t");
            int len = prefix.Length;
            if(lastTab != -1)
            {
                len = prefix.Length - lastTab;
            }
            if(insertAt > 0)
            {
                len -= (len - insertAt);
            }

            int tabs = Mathf.Max(0, len) / TAB_CHARS;
            

            if(debug)
            {
                Debug.Log("::tabs:: <" + prefix + " ::" + tabs + "/" + tabSpacing + ":: " + suffix + "\nconsidered len: " + len + "\nfull term: " + term + "\nindex: " + insertAt);
            }

            if(tabs < tabSpacing)
            {
                tabs = tabSpacing - tabs;
                var b = new System.Text.StringBuilder(prefix);
                for(int i = 0; i < tabs; i++) b.Append("\t");
                b.Append(suffix);
                return b.ToString();
            }
            else
            {
                return prefix + "\t" + suffix;
            }
        }


        public static bool isValid(this ValueState state)
        {
            switch(state)
            {
                case ValueState.Undefined:
                case ValueState.Invalid:
                case ValueState.DescriptorError:
                case ValueState.DataError:          return false;
                default:                            return true;
            }
        }

        public static bool hasError(this ValueState state)
        {
            return !isValid(state);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------


    public interface IDescriptor
    {
        string descriptor { get; }
        bool isContextDescriptor { get; }
        bool Match(string descr);
        bool hasSetter();
    }

    public interface IDescriptor<TData, TContext, TValues> : IDescriptor
                                where TData : class, IDataTarget<TContext>, new() 
                                where TContext : Enum 
                                where TValues : Enum
    {
        TContext context { get; }
        TValues valueType { get; }

        ValueState Set(TData target, object val);
        ValueState Set(IContextData data, object val);
    }



    /// @brief
    /// info structure representing a parsable term within a CustomConfig main dataset
    ///
    public struct MainEntry<TData, TContext, TValues, TValue> : IDescriptor<TData, TContext, TValues>
                        where TData : class, IDataTarget<TContext>, new() 
                        where TContext : Enum 
                        where TValues : Enum
    {

        string IDescriptor.descriptor { get { return descriptor; } }
        bool IDescriptor.isContextDescriptor { get { return isContextDescriptor; } }
        TContext IDescriptor<TData, TContext, TValues>.context { get { return context; } }
        TValues IDescriptor<TData, TContext, TValues>.valueType { get { return valueType; } }
        ValueState IDescriptor<TData, TContext, TValues>.Set(TData target, object val)
        {
            return this.Set(target, val);
        }
        ValueState IDescriptor<TData, TContext, TValues>.Set(IContextData data, object val)
        {
            throw new System.NotImplementedException("ContextEntry cannot handle IContextData!\n\this.context= " + context + "\n\tthis.descriptor= " + descriptor);
        }

        public TContext context;
        public TValues valueType;
        public string descriptor;
        public bool isContextDescriptor;

        string[] aliases;

        DataSetter<TData, TValue> dataSetter;


        public MainEntry(TContext context, string mainDescriptor, TValues type, bool isContext, params string[] aliases)
        {
            UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(mainDescriptor), "Entry cannot have empty main descriptor!");
            this.descriptor = mainDescriptor;
            this.aliases = aliases;
            this.context = context;
            this.valueType = type;
            this.dataSetter = null;
            this.isContextDescriptor = isContext;
        }
        public MainEntry(TContext context, string mainDescriptor, TValues type, DataSetter<TData, TValue> setter, bool isContext, params string[] aliases)
        {
            UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(mainDescriptor), "Entry cannot have empty main descriptor!");
            this.descriptor = mainDescriptor;
            this.aliases = aliases;
            this.context = context;
            this.valueType = type;
            this.dataSetter = setter;
            this.isContextDescriptor = isContext;
        }

        public bool Match(string descriptor)
        {
            if(!string.IsNullOrEmpty(descriptor))
            {
                if(descriptor == this.descriptor)
                {
                    return true;
                }
                else if(aliases != null)
                {
                    for(int i = 0; i < aliases.Length; i++)
                        if(aliases[i] == descriptor)
                            return true;
                }
            }
            return false;
        }

        public bool hasSetter()
        {
            return dataSetter != null;
        }

        public ValueState Set(TData target, object val)
        {
            if(dataSetter != null) 
            {
                return dataSetter(target, (TValue) val) ? ValueState.Valid : ValueState.Invalid;
            }
            return ValueState.DataError;
        }
    }



    /// @brief
    /// info structure representing a parsable term within a CustomConfig inner dataset
    ///
    public struct InnerEntry<TData, TTarget, TContext, TValues, TValue> : IDescriptor<TData, TContext, TValues>
                                where TData : class, IDataTarget<TContext>, new()
                                where TTarget : IContextData
                                where TContext : Enum 
                                where TValues : Enum
    {
        string IDescriptor.descriptor { get { return descriptor; } }
        bool IDescriptor.isContextDescriptor { get { return false; } }
        TContext IDescriptor<TData, TContext, TValues>.context { get { return context; } }
        TValues IDescriptor<TData, TContext, TValues>.valueType { get { return valueType; } }
        ValueState IDescriptor<TData, TContext, TValues>.Set(TData target, object val)
        {
            throw new System.NotImplementedException("ContextEntry cannot handle IDataTarget!");
        }
        ValueState IDescriptor<TData, TContext, TValues>.Set(IContextData data, object val)
        {
            return this.Set(data, val);
        }
        


        public TContext context;
        public TValues valueType;
        public string descriptor;
        string[] aliases;

        ContextDataSetter<TTarget, TValue> contextDataSetter;


        public InnerEntry(TContext context, string mainDescriptor, TValues type, ContextDataSetter<TTarget, TValue> setter, params string[] aliases)
        {
            UnityEngine.Assertions.Assert.IsFalse(string.IsNullOrEmpty(mainDescriptor), "Entry cannot have empty main descriptor!");
            this.descriptor = mainDescriptor;
            this.aliases = aliases;
            this.context = context;
            this.valueType = type;
            this.contextDataSetter = setter;
        }

        public bool Match(string descr)
        {
            if(!string.IsNullOrEmpty(descr))
            {
                if(descr == this.descriptor)
                {
                    return true;
                }
                else if(aliases != null)
                {
                    for(int i = 0; i < aliases.Length; i++)
                        if(aliases[i] == descr)
                            return true;
                }
            }
            return false;
        }

        public bool hasSetter()
        {
            return contextDataSetter != null;
        }

        public ValueState Set(IContextData data, object val)
        {
            if(contextDataSetter != null && data is TTarget) 
            {
                return contextDataSetter((TTarget) data, (TValue) val) ? ValueState.Valid : ValueState.Invalid;
            }
            return ValueState.DataError;
        }
    }

    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Data intermediate used while parsing CustomConfigs line-by-line
    ///    
    public struct Term
    {
        public string context;
        public string descriptor;
        public string content;
        
        public string rawLine;
        public int index;               ///< index within context (0 if line opens new context)

        public Term(CustomConfigSetup setup, string context, string line, int index)
        {
            this.rawLine = line;
            this.context = context;
            this.index = index;
            
            string dc, c;
            if(setup.GetDescriptor(line, context, out dc, out c))
            {
                descriptor = dc;
                content = c;
            }
            else 
            {
                descriptor = "";
                content = line;
            }
        }

        public Term(string context, string descriptor, string content, int index)
        {
            this.rawLine = descriptor + ": " + content;
            this.context = context;
            this.descriptor = descriptor;
            this.content = content;
            this.index = index;
        }

        public bool isNewContext()
        {
            return index == 0;
        }

        public bool hasDescriptor()
        {
            return !string.IsNullOrEmpty(descriptor);
        }

        public bool isValid()
        {
            return !string.IsNullOrEmpty(context) 
                && context != CustomConfigIO.UNDEFINED 
                && !string.IsNullOrEmpty(content);
        }
    }
}