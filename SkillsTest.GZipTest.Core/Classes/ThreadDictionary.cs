using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class ThreadDictionary : Dictionary<string, AsyncOperation>
    {
        public void SafeAdd(string key, AsyncOperation value)
        {
            lock (this.Values)
            {
                base.Add(key, value);
            }
        }

        public void SafeRemove(string key)
        {
            lock (this.Values)
            {
                base.Remove(key);
            }
        }

        public void SafeRemoveAndComplete(string key)
        {
            lock (this.Values)
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
            lock (this.Values)
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
                int result;

                lock (this.Values)
                {
                    result = this.Count;
                }

                return result;
            }
        }


        public bool SafeIamTheLast(AsyncOperation value)
        {
            lock (this.Values)
            {
                return ContainsValue(value) && Count == 1;
            }
        }
    }
}
