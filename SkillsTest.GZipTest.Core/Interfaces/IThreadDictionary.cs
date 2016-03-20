using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IThreadDictionary
    {
        Object SyncRoot { get; }
        int SafeCount { get; }
        void SafeAdd(AsyncOperation value);
        void SafeRemove(AsyncOperation value);
        void SafeRemoveAndComplete(AsyncOperation value);
        void SafeClear();
        bool SafeIamTheLast(AsyncOperation value);
    }
}
