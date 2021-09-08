using System;
using System.Collections.Generic;
using System.IO;

namespace Utility
{

    

    public static class FileSystemUtil
    {

        //-----------------------------------------------------------------------------------------------
        //
        //  FILE / FOLDER UTILITY
        //
        //-----------------------------------------------------------------------------------------------


        public static bool isFolder(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return false;
            }
            else if(path.EndsWith("/"))
            {
                return true;
            }
            else
            {
                int s_id = path.LastIndexOf('/');
                int p_id = path.LastIndexOf('.');
                return s_id != -1 && (p_id == -1 || p_id < s_id);
            }
        }

        public static string RemoveFolderFromFilePath(string filepath)
        {
            if(string.IsNullOrEmpty(filepath))
            {
                return "";
            }
            int s_id = filepath.LastIndexOf('/');
            if(s_id != -1 && s_id < filepath.Length-1)
            {
                s_id++;
                return filepath.Substring(s_id, filepath.Length-s_id);
            }
            return filepath;
        }

        public static string GetFileExtension(string file, params string[] allowed_extensions)
        {
            if(string.IsNullOrEmpty(file))
            {
                return "";
            }
            int s_id = file.LastIndexOf('/');
            int p_id = file.LastIndexOf('.');
            if(s_id != -1 || p_id > s_id)
            {
                string ext = file.Substring(p_id, file.Length-p_id);
                if(allowed_extensions.Length > 0)
                {
                    for(int i = 0; i < allowed_extensions.Length; i++)
                    {
                        if(ext.StartsWith(allowed_extensions[i]))
                        {
                            return allowed_extensions[i];
                        }
                    }
                }
                else
                {
                    return ext;
                }
            }
            return "";
        }

        public static string RemoveFileExtension(string file)
        {
            if(string.IsNullOrEmpty(file))
            {
                return "";
            }
            int s_id = file.LastIndexOf('/');
            int p_id = file.LastIndexOf('.');
            if(s_id != -1 || p_id > s_id)
            {
                string ext = file.Substring(p_id, file.Length-p_id);
                return file.Substring(0, p_id);
            }
            return file;
        }

        public static string RemoveFileFromPath(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return "";
            }
//            Debug.Log("Remove File From path[" + path + "]\nnot folder: " + (!isFolder(path)));
            if(!isFolder(path))
            {
                int lid = path.LastIndexOf('/');
                if(lid != -1)
                {
                    path = path.Substring(0, lid+1);
                }
            }
            if(!path.EndsWith("/")) path += "/";
            return path;
        }


        //-----------------------------------------------------------------------------------------------
        //
        //  TERM FORMATTING
        //
        //-----------------------------------------------------------------------------------------------

        public static string FormatIDWithLeadingZeros(int id, int digits)
        {
            var s = id.ToString();
            var b = new System.Text.StringBuilder(s);
            var d = digits - s.Length;
            if(d > 0)
            {
                for(int i = 0; i < d; i++) b.Insert(0, "0");
            }
            return b.ToString();
        }

        public static string RemoveLeadingZeroes(string term)
        {
            if(term.StartsWith("00"))
            {
                term = term.Substring(2);
            }
            else if(term.StartsWith("0"))
            {
                term = term.Substring(1);
            }
            return term; 
        }

        public static bool TryParseId(string name, string qualifier, out int num, params char[] stopTokens)
        {
            name = RemoveFolderFromFilePath(name);
            name = RemoveFileExtension(name);

            int lID = name.IndexOf(qualifier);
            if(lID != -1)
            {
                string term = name.Substring(lID+qualifier.Length, name.Length-lID-qualifier.Length);
                string parseString = null;
                int startIndex = -1;
                for(int i = 0; i < term.Length; i++)
                {
                    if(System.Char.IsDigit(term[i]))
                    {
                        if(startIndex == -1)
                        {
                            startIndex = i;
                        }
                    }
                    else if(stopTokens.Length > 0 && System.Array.Exists(stopTokens, x=> x == term[i]))
                    {
                        //  stop at version/variant separator
                        break;
                    }
                    else
                    {
                        if(startIndex != -1)
                        {
                            parseString = term.Substring(startIndex, i-startIndex-1);
                            break;
                        }
                    }
                }

                 if(!string.IsNullOrEmpty(parseString))
                 {
                     parseString = RemoveLeadingZeroes(parseString);
                     return System.Int32.TryParse(parseString, out num); 
                 }
            }
            num = -1;
            return false;
        }

        public static bool TryFormatTerm(ref string term, string removeTerm)
        {
            int index = term.IndexOf(removeTerm);
            if(index != -1)
            {
                int len = removeTerm.Length;
                string ls = term.Substring(0, index);
                string rs = term.Substring(index+len, term.Length-(index+len));                
                //Debug.Log("LS: < " + RichText.darkGreen(ls) + " >  RS: < " + RichText.darkGreen(rs) + " >\nterm:: " + term);
                term = ls.Trim() + rs.Trim();
                return true;
            }
            return false;
        }

        public static bool TryFormatTerm( ref string term, 
                                            char beginToken, char endToken, 
                                            out string inner, 
                                            bool removeFromTerm=true, 
                                            bool addTokensToInnerTerm=true)
        {
            int left = term.IndexOf(beginToken);
            int right = term.IndexOf(endToken);
            if(left != -1 && right != -1 && left < right)
            {
                inner = term.Substring(left+1, right-left-1).Trim();
                if(addTokensToInnerTerm)
                {
                    inner = beginToken.ToString() + inner + endToken.ToString();
                }
                if(!string.IsNullOrEmpty(inner))
                {  
                    if(removeFromTerm)
                    {
                        string ls = term.Substring(0, left);
                        string rs = term.Substring(right+1, term.Length-right-1);
                        term = ls.Trim() + rs.Trim();
                    } 
                    return true;
                }
            }
            inner = "";
            return false;
        }

        public static bool TryFormatTerm( ref string term, 
                                            string beginToken, string endToken, 
                                            out string inner, 
                                            bool removeFromTerm=true, 
                                            bool addTokensToInnerTerm=true)
        {
            int left = term.IndexOf(beginToken);
            int right = term.IndexOf(endToken);
            if(left != -1 && right != -1 && left < right)
            {
                int lLen = beginToken.Length;
                int rLen = endToken.Length;

                inner = term.Substring(left+lLen, right-(left+lLen)).Trim();
                if(addTokensToInnerTerm)
                {
                    inner = beginToken.ToString() + inner + endToken.ToString();
                }
                if(!string.IsNullOrEmpty(inner))
                {  
                    if(removeFromTerm)
                    {
                        string ls = term.Substring(0, left);
                        string rs = term.Substring(right+rLen, term.Length-(right+rLen));
                        term = ls.Trim() + rs.Trim();
                    } 
                    return true;
                }
            }
            inner = "";
            return false;
        }



        //-----------------------------------------------------------------------------------------------
        //
        //  COPYING
        //
        //-----------------------------------------------------------------------------------------------



        /// @brief 
        /// Copy folder recursively without recursion to avoid stack overflow.
        ///
        /// from: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        ///
        public static void CopyDirectory(string source, string target, bool overwrite)
        {
            var stack = new Stack<Folders>();
            stack.Push(new Folders(source, target));

            while (stack.Count > 0)
            {
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                {
                    var path = Path.Combine(folders.Target, Path.GetFileName(file));
                    if(overwrite || !File.Exists(path))
                    {
                        File.Copy(file, path, overwrite);
                    }
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
        }

        public static void DeleteDirectoryContent(string directory)
        {
            if(Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                var folders = Directory.GetDirectories(directory);
                for(int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
                for(int i = 0; i < folders.Length; i++)
                {
                    Directory.Delete(folders[i], recursive: true);
                }
            }
        }

        /// @brief
        /// return content of defined folder as names, folders first
        ///
        public static IEnumerable<string> GetDirectoryEntryNames(string folderpath)
        {
            int l = folderpath.Length;
            
            /*foreach(var entry in Directory.EnumerateFileSystemEntries(folderpath))
            {
                var nfo = new DirectoryInfo(entry);
                var last = entry.LastIndexOf("/");
                var result = entry;
                if(last != -1)
                {
                    last++;
                    result = entry.Substring(last, entry.Length-last);
                }
                if(nfo.Exists)
                {
                    yield return result + "/";
                }
                else
                {
                    yield return result;
                }
            }*/


            foreach(var folder in Directory.EnumerateDirectories(folderpath))
            {
                yield return folder.Substring(l, folder.Length-l) + "/";
            }
            foreach(var file in Directory.EnumerateFiles(folderpath))
            {
                yield return file.Substring(l, file.Length-l);
            }
            
            /*foreach(var entry in Directory.EnumerateFileSystemEntries(folderpath))
            {
                yield return entry.Substring(l, entry.Length-l);
            }*/
        }

        struct Folders
        {
            public string Source;
            public string Target;

            public Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }
    }

}