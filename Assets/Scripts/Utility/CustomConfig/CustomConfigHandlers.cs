using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Utility.Config
{   


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  DESCRIPTORS
    //
    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Part of a descriptor entry, sets a single value of a dataset
    ///
    public delegate bool DataSetter<TData, TValue>(TData target, TValue val) where TData : class, IDataTarget, new();
    
    
    /// @brief
    /// Part of a descriptor entry, sets a single value of inner data target (IContextData)
    ///
    public delegate bool ContextDataSetter<TTarget, TValue>(TTarget target, TValue val) where TTarget : IContextData;



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  READER
    //
    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Can be added to CustomConfigSetup. Parses a pack of terms belonging to given context
    ///
    /// @returns wether given context was solved - otherwise, DefaultSolver() is called
    ///
    public delegate bool TermSolver<TData, TContext>(TContext context, TData data, List<Term> termBuffer);



    //-----------------------------------------------------------------------------------------------------------------
    //
    //  WRITER
    //
    //-----------------------------------------------------------------------------------------------------------------

    /// @brief
    /// Can be added to CustomConfigWriter to control formatting of text output
    ///
    public delegate string TermFormatter<TContext>(TContext context, string line);

    /// @brief
    /// Must be added to CustomConfigWriter to fill term buffer from runtime data
    ///
    /// @returns number of lines written
    ///
    public delegate int TermBufferWriter<TData, TContext>(TContext context, TData source, List<Term> buffer, IWriteConstraint<TContext> constraint=null);




    //-----------------------------------------------------------------------------------------------------------------
    //
    //  SETUP
    //
    //-----------------------------------------------------------------------------------------------------------------


    /// @brief
    /// Can be added to CustomConfigSetup.
    ///
    /// @returns wether descriptor belongs to TContext
    ///
    public delegate bool LineValidator<TContext>(CustomConfigSetup setup, string descriptor, TContext context) where TContext : Enum; 

    /// @brief
    /// Can be added to CustomConfigSetup. Parses a line from a config text file into a interpretable term.
    ///
    /// @returns wether line could be parsed
    ///
    public delegate bool LineReader<TContext>(CustomConfigSetup setup, TContext context, string line, int lineIndex, out Term term);


    /// @brief
    /// Must be added to CustomConfigSetup. Controls how values within a context are parsed
    ///
    /// @returns wether value could be parsed
    ///
    public delegate bool ValueParser<TData, TContext, TValues>(CustomConfigSetup<TData, TContext, TValues> setup, TValues type, string raw, out object data) 
                                    where TData : class, IDataTarget<TContext>, new()
                                    where TContext : Enum
                                    where TValues : Enum;
    
    /// @brief
    /// Must be added to CustomConfigSetup. Returns wether value is of correct type.
    ///
    public delegate bool ValueValidator<TContext, TValues>(TValues type, object value)
                                    where TContext : Enum
                                    where TValues : Enum;

    /// @brief
    /// Must be added to CustomConfigSetup to provide defaults for custom value parsing.
    ///
    public delegate object ValueDefaults<TData, TContext, TValues>(CustomConfigSetup<TData, TContext, TValues> setup, TValues type)
                                    where TData : class, IDataTarget<TContext>, new()
                                    where TContext : Enum
                                    where TValues : Enum;

    
    

    //-----------------------------------------------------------------------------------------------------------------
    //
    //  SETUP - CONTEXT DATA
    //
    //-----------------------------------------------------------------------------------------------------------------


    //  Context data allows for inner data objects to be created, bound by a single context


    /// @brief
    /// Can be added to CustomConfigSetup to allow inner data objects bound by a specific context (IContextData)
    ///
    public delegate IContextData ContextDataCreator();
    
    public delegate bool ContextIndexParser<TContextTarget>(TContextTarget target, string term) where TContextTarget : IContextData;


    //-----------------------------------------------------------------------------------------------------------------
    //
    //  SETUP - PACKING
    //
    //-----------------------------------------------------------------------------------------------------------------


    //  packing allows reading/writing multiple IDataTargets into same file


    /// @brief
    /// Can be added to CustomConfigSetup to control how pack indices are loaded.
    /// Pack tokens can also be customized in the setup 
    ///
    /// @returns wether given string value could be parsed into a matching packID
    ///
    public delegate bool PackIndexParser(string raw, out int id);

    /// @brief
    /// Can be added to CustomConfigWriter to enable packed writing (writing multiple datasets into one config)
    ///
    /// @returns the inner term of the packing instruction that can be parsed by corresponding PackIndexParser
    ///
    public delegate string PackIndexWriter<TData>(TData target);



}