using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class ThreadDictionary : Dictionary<int, Thread>
    {
        public Object SyncRoot { get; private set; }

        public ThreadDictionary()
        {
            this.SyncRoot = new object();
        }

        public void SafeAdd(Thread value)
        {
            lock (this.SyncRoot)
            {
                base.Add(value.ManagedThreadId, value);
            }
        }

        public void SafeRemove(Thread value)
        {
            lock (this.SyncRoot)
            {
                base.Remove(value.ManagedThreadId);
            }
        }

        public void SafeRemoveAndComplete(Thread value)
        {
            lock (this.SyncRoot)
            {
                this.Remove(value.ManagedThreadId);
            }
        }

        public void SafeClear()
        {
            lock (this.SyncRoot)
            {
                base.Clear();
            }
        }


        public uint SafeCount
        {
            get
            {
                lock (this.SyncRoot)
                {
                    return (uint)this.Count;
                }
            }
        }


        public bool SafeExists(Thread value)
        {
            return this.SafeExists(value.ManagedThreadId);
        }

        public bool SafeExists(int key)
        {
            lock (this.SyncRoot)
            {
                return this.ContainsKey(key);
            }
        }


        public bool SafeIamTheLast(Thread value)
        {
            lock (this.SyncRoot)
            {
                return ContainsValue(value) && Count == 1;
            }
        }
    }
}
