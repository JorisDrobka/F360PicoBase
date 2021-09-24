using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


using Utility;

namespace F360
{


    public static class F360FileSystem
    {
        public const char FILENAME_VARIANT_QUALIFIER = '_';
        public const char FILENNAME_VERSION_QUALIFIER = '-';


        public const string LECTURE_TOKEN = "VF";

        const char CSV_TRIM = '\'';


        static string[] allowed_extensions = new string[]
        {
            ".txt", ".json", ".info", ".bundle", 
            ".zip", ".apk", ".mp4"
        };



        //-----------------------------------------------------------------------------------------------
        //
        //  FILE / FOLDER UTILITY
        //
        //-----------------------------------------------------------------------------------------------
        
        
        public static bool isFolder(string path)
        {
            return FileSystemUtil.isFolder(path);           
        }

        /// @returns wether path is a Unity resource folderpath
        ///
        public static bool isResourceFolder(string path)
        {
            return isFolder(path) && !path.StartsWith(Application.dataPath);
        }   

        /// @returns wether path is either a Unity resource folder or a Unity resource (Object) itself
        ///
        public static bool isResourcePath(string path)
        {
            if(!string.IsNullOrEmpty(path))
            {
                return !path.StartsWith(Application.dataPath)
                    && (isFolder(path)
                        || string.IsNullOrEmpty(GetFileExtension(path)));
            }
            return false;
        }

        public static string RemoveFolderFromFilePath(string filepath)
        {
            return FileSystemUtil.RemoveFolderFromFilePath(filepath);
        }

        public static string GetFileExtension(string file)
        {
            return FileSystemUtil.GetFileExtension(file, allowed_extensions);
        }

        public static string RemoveFileExtension(string file)
        {
            return FileSystemUtil.RemoveFileExtension(file);
        }

        public static string RemoveFileFromPath(string path)
        {
            return FileSystemUtil.RemoveFileFromPath(path);
        }



        //-----------------------------------------------------------------------------------------------
        //
        //  PARSING
        //
        //-----------------------------------------------------------------------------------------------

        public static bool ParseVersion(string file, out string version)
        {
            string clean;
            return ParseVersion(file, out version, out clean);
        }
        public static bool ParseVariant(string file, out string variant)
        {
            string clean;
            return ParseVersion(file, out variant, out clean);
        }

        public static bool ParseVersion(string file, out string version, out string cleanName, bool keepExtension=false)
        {
            string ext = GetFileExtension(file);
            file = RemoveFileExtension(file);
            version = "";
            cleanName = file;

            int s_id = file.LastIndexOf('/');
            int v_id = file.LastIndexOf(FILENNAME_VERSION_QUALIFIER);
            if(v_id != -1 && (s_id == -1 || v_id > s_id))
            {

                version = file.Substring(v_id+1);
                cleanName = file.Substring(0, v_id);
                if(keepExtension) cleanName += ext;
                return true;
            }
            return false;
        }

        public static bool ParseVariant(string file, out string variant, out string cleanName, bool keepExtension=false)
        {
            string ext = GetFileExtension(file);
            file = RemoveFileExtension(file);
            variant = "";
            cleanName = file;
            
            int s_id = file.LastIndexOf('/');
            int va_id = file.LastIndexOf(FILENAME_VARIANT_QUALIFIER);
            if(va_id != -1 && (s_id == -1 || va_id > s_id))
            {
                int v_id = file.LastIndexOf(FILENNAME_VERSION_QUALIFIER);
                if(va_id != -1 && va_id > v_id) 
                {
                    variant = file.Substring(va_id, v_id-va_id);
                }
                else
                { 
                    variant = file.Substring(va_id);
                }
                cleanName = file.Substring(0, va_id);
                if(keepExtension) cleanName += ext;
                return true;
            }
            return false;
        }

        static string[] versions = new string[] { "start", "base", "alpha", "beta", "release" };
        

        /// @brief
        /// tries to compares to strings with a version stamp encoded into them
        ///
        /// @returns
        ///    -1   : no version encoded in either of the given parameters
        ///     0   : encoded versions are equal
        ///     1   : first parameter has higher version encoded
        ///     2   : second parameter has higher version encoded
        ///
        public static int ComparePatchVersions(string nameA, string nameB)
        {
            if(string.IsNullOrEmpty(nameA) || string.IsNullOrEmpty(nameB))
            {
                return -1;
            }
            else if(nameA == nameB)
            {
                return 0;
            }
            else if(nameA.Contains("start"))
            {
                return 2;
            }
            else if(nameB.Contains("start"))
            {
                return 1;
            }

            string vA, vB;
            if(ParseVersion(nameA, out vA) && ParseVersion(nameB, out vB))
            {
                int iA = System.Array.IndexOf(versions, nameA);
                int iB = System.Array.IndexOf(versions, nameB);
                if(iA == iB)
                {
                    float f1, f2;
                    if(_parseFloat(vA, out f1) && _parseFloat(vB, out f2))
                    {
                        if(f1 > f2)
                        {
                            return 1;
                        }
                        else if(f2 > f1)
                        {
                            return 2;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
                else if(iA > iB)
                { 
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
            return -1;
        }

        static bool _parseFloat(string term, out float result)
        {
            result = 0f;
            if(!string.IsNullOrEmpty(term))
            {
                int id = term.IndexOf('.');
                if(id != -1 && _tryParseFloatTuple(term, id, out result))
                {
                    return true;
                }
                else 
                {
                    id = term.IndexOf(',');
                    if(_tryParseFloatTuple(term, id, out result))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        static bool _tryParseFloatTuple(string term, int index, out float result)
        {
            if(index != -1)
            {
                int a, b;
                if(index == 0)
                {
                    if(System.Int32.TryParse(term.Substring(1, term.Length-1), out b))
                    {
                        result = (float)b / 10f;
                        return true;
                    }
                }
                else if(index < term.Length-1 && System.Int32.TryParse(term.Substring(0, index), out a) && System.Int32.TryParse(term.Substring(index+1, term.Length-index-1), out b))
                {
                    result = a + ((float)b / 10f);
                    return true;
                }
                else if(System.Int32.TryParse(term.Substring(0, term.Length-1), out a))
                {
                    result = (float)a / 10f;
                    return true;
                }
            }
            result = 0f;
            return false;
        }



        //-----------------------------------------------------------------------------------------------
        //
        //  VIDEO ID
        //
        //-----------------------------------------------------------------------------------------------


        public static string FormatVideoID(int internalID)
        {
            return FileSystemUtil.FormatIDWithLeadingZeros(internalID, 3);
            /*string id = internalID.ToString();
            if(id.Length == 1)
            {
                id = "00" + id;
            }
            else if(id.Length == 2)
            {
                id = "0" + id;
            }
            return id;*/
        }

        public static bool ParseVideoIDXX(string name, out int videoID)
        {
            name = RemoveFolderFromFilePath(name);
            name = RemoveFileExtension(name);
            var split = name.Split('_');
            var parse = split[0];
            if(parse.StartsWith("00"))
            {
                parse = parse.Substring(2);
            }
            else if(parse.StartsWith("0"))
            {
                parse = parse.Substring(1);
            }
            int id;
            if(System.Int32.TryParse(parse, out id))
            {
                videoID = id;
                return true;
            }
            else
            {
                videoID = -1;
                return false;
            }
        }

        /// @brief
        /// Tries to find a valid 3-digit videoID from first part of given given name.
        /// Parsing stops at version Version ("-") and Variant ("_") -Qualifiers.
        ///
        public static bool ParseVideoID(string name, out int videoID)
        {
            name = RemoveFolderFromFilePath(name);
            name = RemoveFileExtension(name);
            name = name.Trim();


            int len = name.Length;
            int startID = -1;
            string parseString = null;
            for(int i = 0; i < len; i++)
            {
                if(System.Char.IsDigit(name[i]))
                {
                    if(startID == -1)
                    {
                        startID = i;
                    }
                    else
                    {
                        if(i-startID+1 == 3)
                        {
                            parseString = name.Substring(startID, 3);
                            break;
                        }
                    } 
                }
                else if(name[i] == FILENNAME_VERSION_QUALIFIER 
                    || name[i] == FILENAME_VARIANT_QUALIFIER)
                {
                    //  stop at version/variant separator
                    break;
                }
                else
                {
                    startID = -1;
                }
            }

            if(!string.IsNullOrEmpty(parseString))
            {
                parseString = FileSystemUtil.RemoveLeadingZeroes(parseString);
                //parseString = RemoveLeadingZeroes(parseString);
                return System.Int32.TryParse(parseString, out videoID);
            }

            videoID = -1;
            return false;
        }


        /// @brief
        /// Tries to find a valid lecture ID (starts with "VF" - Virtueller Fahrlehrer)
        /// Parsing stops at version Version ("-") and Variant ("_") -Qualifiers.
        ///
        public static bool ParseLectureID(string name, out int lectureID)
        {
            return FileSystemUtil.TryParseId(name, LECTURE_TOKEN,  out lectureID, FILENNAME_VERSION_QUALIFIER, FILENAME_VARIANT_QUALIFIER);
            //return parseNumberAfterQualifier(name, LECTURE_TOKEN, out lectureID, FILENNAME_VERSION_QUALIFIER, FILENAME_VARIANT_QUALIFIER);
        }

        /// @brief
        /// Tries to find a valid file index for given lecture file (starts with "VF" - Virtueller Fahrlehrer)
        ///
        public static bool ParseLectureFileIndex(string name, out int index)
        {
            if(name.Contains(LECTURE_TOKEN))
            {
                return FileSystemUtil.TryParseId(name, "-", out index);
                //return parseNumberAfterQualifier(name, "-", out index);
            }
            index = -1;
            return false;
        }

        

        //-----------------------------------------------------------------------------------------------
        //
        //  TIME FORMATTING
        //
        //-----------------------------------------------------------------------------------------------

        const int MINUTE_MS = 1000 * 60;

        public const string SUFFIX_MILLIS = " ms";
        public const string SUFFIX_SECONDS = " s";
        public const string SUFFIX_MINUTES = " m";


        public static string RemoveSuffixFromTimeString(string timing)
        {
            if(!string.IsNullOrEmpty(timing))
            {
                timing = timing.Trim();
                int len = timing.Length;
                for(int i = len-1; i >= 0; i--)
                {
                    if(System.Char.IsNumber(timing[i]))
                    {   
                        return timing.Substring(0, i+1);
                    }
                }
            }
            return "";
        }

        public static string FormatTimeRange(RangeInt timingMs, 
                                            bool showLeadingZeros=false, 
                                            bool showMillis=true, 
                                            bool showSuffix=false,
                                            string suffix_seconds=SUFFIX_SECONDS, 
                                            string suffix_minutes=SUFFIX_MINUTES)
        {
            if(timingMs.start >= MINUTE_MS || timingMs.end >= MINUTE_MS)
            {
                return FormatTimeRangeMinutes(timingMs, showLeadingZeros, showMillis, showSuffix, suffix_seconds, suffix_minutes);
            }
            else
            {
                return FormatTimeRangeSeconds(timingMs, showLeadingZeros, showMillis, showSuffix, suffix_seconds, suffix_minutes);
            }
        }
        public static string FormatTimeRangeSeconds(RangeInt timingMs, 
                                                    bool showLeadingZeros=false, 
                                                    bool showMillis=true, 
                                                    bool showSuffix=false,
                                                    string suffix_seconds=SUFFIX_SECONDS, 
                                                    string suffix_minutes=SUFFIX_MINUTES)
        {
            var b = new System.Text.StringBuilder();
            b.Append(FormatTimeSeconds(timingMs.start, showLeadingZeros, showMillis));
            b.Append("-");
            b.Append(FormatTimeSeconds(timingMs.end, showLeadingZeros, showMillis, showSuffix, suffix_seconds, suffix_minutes));
            return b.ToString();
        }
        public static string FormatTimeRangeMinutes(RangeInt timingMs, 
                                                    bool showLeadingZeros=false, 
                                                    bool showMillis=true, 
                                                    bool showSuffix=false,
                                                    string suffix_seconds=SUFFIX_SECONDS, 
                                                    string suffix_minutes=SUFFIX_MINUTES)
        {
            var b = new System.Text.StringBuilder();
            b.Append(FormatTimeMinutes(timingMs.start, showLeadingZeros, showMillis));
            b.Append("-");
            b.Append(FormatTimeMinutes(timingMs.end, showLeadingZeros, showMillis, showSuffix, suffix_seconds, suffix_minutes));
            return b.ToString();
        }

        public static string FormatTime(int timeMs, 
                                        bool showLeadingZeros=false, 
                                        bool showMillis=true, 
                                        bool showSuffix=false,
                                        string suffix_seconds=SUFFIX_SECONDS, 
                                        string suffix_minutes=SUFFIX_MINUTES)
        {
            if(timeMs >= MINUTE_MS)
            {
                return FormatTimeMinutes(timeMs, showLeadingZeros, showMillis, showSuffix, suffix_seconds, suffix_minutes);
            }
            else
            {
                return FormatTimeSeconds(timeMs, showLeadingZeros, showMillis, showSuffix, suffix_seconds, suffix_minutes);
            }
        }
        public static string FormatTimeSeconds( int timeMs,
                                                bool showLeadingZeros=false,
                                                bool showMillis=true, 
                                                bool showSuffix=false,
                                                string suffix_seconds=SUFFIX_SECONDS, 
                                                string suffix_minutes=SUFFIX_MINUTES)
        {
            string suffix = suffix_seconds;

            var b = new System.Text.StringBuilder();
            var t = Mathf.RoundToInt((timeMs / 1000f) * 100) / 100f;
            var hasMinutes = t > 1000;
            if(hasMinutes)
            {
                //  format minutes
                var m = t % 1000;
                b.Append((t-m).ToString());
                b.Append(":");

                suffix = suffix_minutes;
            }
            var s = Mathf.FloorToInt(t);
            if(hasMinutes || (showLeadingZeros && s < 10))
            {
                b.Append("0");
            }
            b.Append(s);

            Debug.Log("formatTS 1: " + b.ToString());

            if(showMillis)
            {
                var ms = Mathf.RoundToInt((t - s) * 100);
                b.Append(",");
                if(ms > 0)
                {
                    if(ms < 10)
                        b.Append("0");
                    b.Append(ms);
                }
                else
                {
                    b.Append("00");
                }
            }
            Debug.Log("formatTS 2: " + b.ToString());
            if(showSuffix)
            {
                b.Append(suffix);
            }
            Debug.Log("formatTS 3: " + b.ToString());
            return b.ToString();
        }
        public static string FormatTimeMinutes( int timeMs, 
                                                bool showLeadingZeros=false,
                                                bool showMillis=true, 
                                                bool showSuffix=false,
                                                string suffix_seconds=SUFFIX_SECONDS, 
                                                string suffix_minutes=SUFFIX_MINUTES)
        {
            string suffix = suffix_minutes;
            int minutes = timeMs / MINUTE_MS;
            timeMs = timeMs - (minutes * MINUTE_MS);

            var b = new System.Text.StringBuilder();
            if(showLeadingZeros && minutes < 10)
            {
                b.Append("0");
            }
            b.Append(minutes.ToString());  
            b.Append(":");
            b.Append(FormatTimeSeconds(timeMs, showLeadingZeros: true, showMillis: showMillis, showSuffix: false));
            if(showSuffix)
            {
                b.Append(suffix);
            }
            return b.ToString();
        }




        //-----------------------------------------------------------------------------------------------
        //
        //  TERM FORMATTING
        //
        //-----------------------------------------------------------------------------------------------


        public static bool TryFormatString(ref string term, string removeTerm)
        {
            return FileSystemUtil.TryFormatTerm(ref term, removeTerm);
        }

        public static bool TryFormatString( ref string term, 
                                            char beginToken, char endToken, 
                                            out string inner, 
                                            bool removeFromTerm=true, 
                                            bool addTokensToInnerTerm=true)
        {
            return FileSystemUtil.TryFormatTerm(ref term, beginToken, endToken, out inner, removeFromTerm, addTokensToInnerTerm);
        }

        public static bool TryFormatString( ref string term, 
                                            string beginToken, string endToken, 
                                            out string inner, 
                                            bool removeFromTerm=true, 
                                            bool addTokensToInnerTerm=true)
        {
            return FileSystemUtil.TryFormatTerm(ref term, beginToken, endToken, out inner, removeFromTerm, addTokensToInnerTerm);
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  VALUE READING
        //
        //-----------------------------------------------------------------------------------------------


        //  methods for quickly reading F360 datafiles
    

        /// @brief
        /// parsed human-entered second timing data in the format of 0,5 (=500 milliseconds) or 2,24 (=2 seconds, 240 milliseconds)
        /// for minutes, use ':', for example 5:02,24 or 05:02,24
        ///
        /// @returns parse success & time in milliseconds
        ///
        public static bool ParseTimeFromSecondsDataset(string term, out int result, char trim=CSV_TRIM)
        {
            term = RemoveSuffixFromTimeString(term);

            result = -1;
            string[] split = term.Split(',');
            string firstTerm = split[0].Trim();
            if(firstTerm.StartsWith("0")) firstTerm.Remove(0, 1);

            int minutes = 0;
            int mId = firstTerm.IndexOf(":");
            if(mId != -1)
            {
                //  parse minutes
                var split2 = firstTerm.Split(':');
                int m;
                if(ParseIntegerFromDataset(split2[0], out m, trim))
                {
                    minutes = m;
                }
                firstTerm = split2[1];
                if(firstTerm.StartsWith("0")) firstTerm.Remove(0, 1);
            }

            int seconds;
            if(ParseIntegerFromDataset(firstTerm, out seconds, trim))
            {
                if(split.Length > 1)
                {
                    split[1] = split[1].Trim();
                    int milliseconds = 0;
                    int leadingZeros = 0;
                    for(int i = 0; i < split[1].Length; i++)
                    {
                        if(split[1][i] == '0') leadingZeros++;
                        else break;
                    }
                    split[1] = split[1].TrimEnd('0');
                    if(split[1].Length == 0)
                    {
                        //  0 millis
                    }
                    else if(!ParseIntegerFromDataset(split[1], out milliseconds, trim))
                    {
                        return false;
                    }

          //          var b = new System.Text.StringBuilder("parse millis... <" + term + "> seconds=[" + seconds.ToString() + "] millis=[" + milliseconds.ToString() + "] lead zeros=[" + leadingZeros.ToString() + "]");

                    if(leadingZeros > 0)
                    {
                        
                    }
                    else
                    {
                        if(milliseconds > 999)
                        {
                            milliseconds = Mathf.RoundToInt(milliseconds / 1000f);
          //                  b.Append("\nooompf! >> " + milliseconds);
                        }
                        else if(milliseconds < 10)
                        {
                            milliseconds *= 100;
                        }
                        else if(milliseconds < 100)
                        {
                            milliseconds *= 10;
                        }
                    }
                    seconds += minutes * 60;
                    result = (seconds * 1000) + milliseconds;
                    return true;
                }
                else
                {
                    seconds += minutes * 60;
                    result = seconds * 1000;
                    return true;
                }
            }
            return false;
        }

        /// @brief
        /// parses a human-entered timerange in seconds
        ///
        /// @returns parse success & timerange in milliseconds
        ///
        public static bool ParseTimeRangeFromSecondsDataset(string term, out RangeInt range, char trim=CSV_TRIM)
        {
            term = RemoveSuffixFromTimeString(term);

            int min, max;
            string[] split = term.Split('-');
            if(split.Length == 2)
            {
                if(ParseTimeFromSecondsDataset(split[0], out min, trim) && ParseTimeFromSecondsDataset(split[1], out max, trim))
                {
                    range = new RangeInt(min, max-min);
                    return true;
                }
            }
            else if(ParseTimeFromSecondsDataset(term, out min, trim))
            {
                range = new RangeInt(min, 0);
                return true;
            }
            range = new RangeInt();
            return false;
        }


        public static bool ParseIntegerFromDataset(string term, out int num, char trim=CSV_TRIM)
        {
            term = term.Trim(trim);
            if(System.Int32.TryParse(term, out num))
            {
                return true;
            }
            num = -1;
            return false;
        }



        public static bool ParseFloatingPointMillisFromDataset(string term, out float num, char trim=CSV_TRIM)
        {
            int zeros = 0;
            for(int i = 0; i < term.Length; i++)
            {
                if(term[i] == '0') zeros++;
                else break;
            }
            int len = term.Length - zeros;
            int n;
            if(len > 0 && System.Int32.TryParse(term, out n))
            {
                if(n > 0)
                {
                    num = n / len;
                    return true;
                }
            }
            num = -1f;
            return false;
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  FILE NAME ENCODING
        //
        //-----------------------------------------------------------------------------------------------


        //  standard-encoding of filenames:
        //
        //  name-variant.ext
        //  example: config-1.2.json



        public static bool GetVariant(string filename, out string variant)
        {
            if(!isFolder(filename))
            {
                string c, v, e;
                unpack_filename(filename, out c, out variant, out v, out e);
                return !string.IsNullOrEmpty(variant);
            }
            variant = "";
            return false;
        }

        public static bool GetVersion(string filename, out string version)
        {
            if(!isFolder(filename))
            {
                string c, va, e;
                unpack_filename(filename, out c, out va, out version, out e);
                return !string.IsNullOrEmpty(version);
            }
            version = "";
            return false;
        }

        public static string FormatName(string filename, bool keepVariant=true, bool keepVersion=false, bool keepExtension=false)
        {
            if(string.IsNullOrEmpty(filename))
            {
                return "";
            }
            else if(isFolder(filename))
            {
                return filename;
            }
            filename = RemoveFolderFromFilePath(filename);
            string c, va, v, e;
            unpack_filename(filename, out c, out va, out v, out e);
            string result = c;
            if(keepVariant && !string.IsNullOrEmpty(va))
            {
                result += FILENAME_VARIANT_QUALIFIER + va;
            }
            if(keepVersion && !string.IsNullOrEmpty(v))
            {
                result += FILENNAME_VERSION_QUALIFIER + v;
            }
            if(keepExtension && !string.IsNullOrEmpty(e))
            {
                result += e;
            }
            return result;
        }
        

        static void unpack_filename(string filename, out string clean, out string variant, out string version, out string extension)
        {
            filename = RemoveFolderFromFilePath(filename);
            clean = filename;
            variant = "";
            version = "";
            extension = "";
            
            int e = clean.LastIndexOf(".");
            if(e != -1)
            {
                extension = clean.Substring(e, clean.Length-e);
                if(allowed_extensions.Contains(extension))
                {
                    clean = filename.Substring(0, e);
                }
                else
                {
                    extension = "";
                    e = -1;
                }
            }

            int v = clean.LastIndexOf(FILENNAME_VERSION_QUALIFIER);

            if(v != -1)
            {
                v++;
                version = clean.Substring(v, clean.Length-v);
                clean = clean.Substring(0, v-1);
            }

            int va = clean.LastIndexOf(FILENAME_VARIANT_QUALIFIER);
            if(va != -1)
            {
                va++;
                variant = clean.Substring(va, clean.Length-va);
                clean = clean.Substring(0, va-1);
            }
        }

    }




}