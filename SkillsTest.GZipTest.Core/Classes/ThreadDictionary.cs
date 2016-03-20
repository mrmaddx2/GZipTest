using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class ThreadDictionary : Dictionary<string, AsyncOperation>
    {
        public readonly Object SyncRoot = new object();

        public void SafeAdd(string key, AsyncOperation value)
        {
            lock (this.SyncRoot)
            {
                base.Add(key, value);
            }
        }

        public void SafeRemove(string key)
        {
            lock (this.SyncRoot)
            {
                base.Remove(key);
            }
        }

        public void SafeRemoveAndComplete(string key)
        {
            lock (this.SyncRoot)
            {
                var operation = this[key];

                if (operation != null)
                {
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
