using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360.Backend
{
    
/*
    public abstract class Diff : ISynchronizedData
    {
        public Database Database { get; private set; }
        public string Uri { get; private set; }
        public string Key { get; private set; }
        public DateTime Timestamp { get; private set; }

        public abstract object Value { get; }
        public abstract Type Type { get; }

        public Diff(Database target, string uri, string key, DateTime timestamp)
        {
            this.Database = target;
            this.Uri = uri;
            this.Key = key;
            this.Timestamp = timestamp;
        }

        public abstract string Readable();
    }


    public class Diff<TData> : Diff
    {
        TData data;

        public Diff(Database database, string uri, string key, TData data)
                    : base(database, uri, key, DateTime.Now)
        {
            this.data = data;
        }
        public Diff(Database database, string uri, string key, TData data, DateTime timestamp) 
                    : base(database, uri, key, timestamp)
        {
            this.data = data;
        }

        public override object Value { get { return data; } }
        public override Type Type { get { return typeof(TData); } }


        public override string Readable()
        {
            var b = new System.Text.StringBuilder("Diff<");
            b.Append(Type.Name);
            b.Append("> {");
            b.Append(data != null ? data.ToString() : "(NULL)");
            b.Append("} uri=[");
            b.Append(Uri);
            b.Append("}");
            return b.ToString();
        }
    }
*/
}
