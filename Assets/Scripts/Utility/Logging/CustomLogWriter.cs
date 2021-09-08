using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Utility.Logging
{
    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error
    }

    public class CustomLogWriter
    {
        public const string CHANNEL_MAIN = "Main/";
        const string BEGIN_TOKEN = "/begin";
        const string END_TOKEN = "/end";

        struct Entry
        {
            public LogLevel level;
            public string indent;
            public string channel;
            public string message;
            public string process;
            public string operation;
            public string stacktrace;
            public string token;
        }


        public Color Color_Process = Color.blue;
        public Color Color_Operation = Color.magenta;
        

        int indent_level = 0;
        string process = "";
        string operation = "";
        List<Entry> entries = new List<Entry>();


        //-----------------------------------------------------------------------------------------------


        public void BeginProcess(string processName, string channel=CHANNEL_MAIN, string msg="", LogLevel level=LogLevel.Info)
        {
            this.process = processName;
            addEntry(channel, msg, level, indent_level, process: processName, token: BEGIN_TOKEN);
            indent_level++;
        }
        public void BeginOperation(string operation, string channel=CHANNEL_MAIN, string msg="", LogLevel level=LogLevel.Info)
        {
            this.operation = operation;
            addEntry(channel, msg, level, indent_level, process: process, op: operation, token: BEGIN_TOKEN);
            indent_level++;
        }
        public void EndSection(string channel=CHANNEL_MAIN, string msg="", LogLevel level=LogLevel.Info)
        {   
            if(!string.IsNullOrEmpty(operation))
            {
                indent_level = Mathf.Max(0, indent_level-1);
                addEntry(channel, msg, level, indent_level, "", operation, END_TOKEN);
                operation = "";
            }
            else if(!string.IsNullOrEmpty(process))
            {
                indent_level = Mathf.Max(0, indent_level-1);
                addEntry(channel, msg, level, indent_level, process, "", END_TOKEN);
                process = "";
            }
        }

        //-----------------------------------------------------------------------------------------------

        public void Log(string msg, LogLevel level=LogLevel.Info, int indent=0)
        {
            addEntry(CHANNEL_MAIN, msg, level, indent);
        }
        public void Log(string channel, string msg, LogLevel level=LogLevel.Info, int indent=0)
        {
            addEntry(channel, msg, level, indent);
        }

        public void Warning(string msg)
        {
            addEntry(CHANNEL_MAIN, msg, LogLevel.Warning, 0);
        }
        public void Warning(string channel, string msg)
        {
            addEntry(channel, msg, LogLevel.Warning, 0);
        }

        public void Error(string msg, System.Exception ex=null)
        {
            Error(CHANNEL_MAIN, msg, ex);
        }   
        public void Error(string channel, string msg, System.Exception ex=null)
        {
            var stack = "";
            if(ex != null)
            {
                stack = ex.StackTrace;
                msg += "\n\t>>" + ex.GetType().Name + ":: " + ex.Message;
            }
            addEntry(channel, msg, LogLevel.Error, 0, stacktrace: stack);
        }

        //-----------------------------------------------------------------------------------------------

        public IEnumerable<string> EnumerateEntries(int depth=120, bool richtext=false)
        {
            int start = Mathf.Max(0, entries.Count-depth);
            int end = Mathf.Min(entries.Count, start+depth);
            for(int i = start; i < end; i++)
            {
                yield return formatLogLine(entries[i], true, false, richtext);
            }
        }

        public string PrintLog(int depth=120, bool richtext=false)
        {
            var b = new StringBuilder();
            foreach(var line in EnumerateEntries(depth, richtext))
            {
                b.Append("\n" + line);
            }
            return b.ToString();
        }

        public string PrintChannel(string channel=CHANNEL_MAIN, int depth=60, bool richtext=false)
        {
            var l = new List<string>();
            for(int i = entries.Count-1; i >= 0; i--)
            {
                if(depth <= 0)
                {
                    break;
                }
                if(entries[i].channel == channel)
                {
                    l.Add(formatLogLine(entries[i], false, richtext));
                    depth--;
                }
            }
            l.Reverse();
            var b = new StringBuilder();
            foreach(var line in l) b.Append("\n" + l);
            return b.ToString();
        }

        public string PrintProcess(string process, int iter=1, bool richtext=false)
        {
            var b = new StringBuilder();
            iter = Mathf.Max(1, iter);
            for(int i = 0; i < iter; i++)
            {
                int start= entries.FindLastIndex(x=> x.process==process && x.token==BEGIN_TOKEN);
                if(start != -1)
                {
                    int end = entries.FindLastIndex(x=> x.process==process && x.token==END_TOKEN);
                    if(end > start)
                    {
                        for(int k = start; k <= end; k++)
                        {
                            b.Append(formatLogLine(entries[k], false, false, richtext));
                        }
                    }
                    else break;
                }
                else break;
            }
            return b.ToString();
        }
        public string PrintOperation(string operation, int iter=1, bool richtext=false)
        {
            var b = new StringBuilder();
            iter = Mathf.Max(1, iter);
            for(int i = 0; i < iter; i++)
            {
                int start= entries.FindLastIndex(x=> x.operation==operation && x.token==BEGIN_TOKEN);
                if(start != -1)
                {
                    int end = entries.FindLastIndex(x=> x.operation==operation && x.token==END_TOKEN);
                    if(end > start)
                    {
                        for(int k = start; k <= end; k++)
                        {
                            b.Append(formatLogLine(entries[k], false, false, richtext));
                        }
                    }
                    else break;
                }
                else break;
            }
            return b.ToString();
        }


        //-----------------------------------------------------------------------------------------------


        void addEntry(string channel, string message, LogLevel level, int indentLvl, string process="", string op="", string token="", string stacktrace="")
        {
            var entry = new Entry();
            var indent = new StringBuilder();
            for(int i = 0; i < indentLvl; i++) indent.Append("\t");
            entry.channel = channel;
            entry.message = message;
            entry.process = process;
            entry.operation = op;
            entry.level = level;
            entry.token = token;
            entry.indent = indent.ToString();
            entry.stacktrace = stacktrace;
            entries.Add(entry);
        }

        string formatLogLine(Entry e, bool showChannel=true, bool stacktrace=false, bool richtext=false)
        {
            var b = new StringBuilder(e.indent);
            if(showChannel)
            {
                if(!string.IsNullOrEmpty(e.channel))
                {
                    b.Append(e.channel);
                    if(!e.channel.EndsWith(">>"))
                        b.Append(">>");
                    b.Append(" ");
                }
            }
            b.Append(e.indent);
            if(!string.IsNullOrEmpty(e.process))
            {
                b.Append("\n" + e.token + " ");
                if(richtext) b.Append(RichText.color(e.process, Color_Process));
                else b.Append(e.process);
                b.Append(" " + e.message);
            }
            else if(!string.IsNullOrEmpty(e.operation))
            {
                b.Append("\n" + e.token + " ");
                if(richtext) b.Append(RichText.color(e.operation, Color_Operation));
                else b.Append(e.operation);
                b.Append(" " + e.message);
            }
            else
            {
                b.Append(e.message);
            }

            if(stacktrace && !string.IsNullOrEmpty(e.stacktrace))
            {
                if(richtext) b.Append("\n\tstacktrace= " + RichText.italic(e.stacktrace));
                else b.Append("\n\tstacktrace= " + e.stacktrace);
            }
            return b.ToString();
        }
    }
}


