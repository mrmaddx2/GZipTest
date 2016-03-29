using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class ThreadDictionary : Dictionary<string, AsyncOperation>, IThreadDictionary
    {
        public Object SyncRoot { get; private set; }

        public ThreadDictionary()
        {
            this.SyncRoot = new object();
        }

        public void SafeAdd(AsyncOperation value)
        {
            lock (this.SyncRoot)
            {
                base.Add(value.UserSuppliedState.ToString(), value);
            }
        }

        public void SafeRemove(AsyncOperation value)
        {
            lock (this.SyncRoot)
            {
                base.Remove(value.UserSuppliedState.ToString());
            }
        }

        public void SafeRemoveAndComplete(AsyncOperation value)
        {
            lock (this.SyncRoot)
            {
                string key = value.UserSuppliedState.ToString();

                if (this.ContainsKey(key))
                {
                    var operation = this[key];
                    operation.OperationCompleted();
                    this.Remove(key);
                }
            }
        }

        public void SafeClear()
        {
            lock (this.SyncRoot)
            {
                foreach (var currentThread in this.Values)
                {
                    currentThread.OperationCompleted();
                }
                
                base.Clear();
            }
        }


        public int SafeCount
        {
            get
            {
                lock (this.SyncRoot)
                {
                    return this.Count;
                }
            }
        }


        public bool SafeIamTheLast(AsyncOperation value)
        {
            lock (this.SyncRoot)
            {
                return ContainsValue(value) && Count == 1;
            }
        }
    }
}
