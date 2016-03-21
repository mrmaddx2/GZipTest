using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class SourcePieces : List<PieceOfSource>
    {
        public readonly Object SyncRoot = new object();

        public void SafeAdd(PieceOfSource value)
        {
            lock (SyncRoot)
            {
                base.Add(value);
            }
        }

        public void SafeRemove(PieceOfSource key)
        {
            lock (SyncRoot)
            {
                base.Remove(key);
            }
        }

        public void SafeClear()
        {
            lock (SyncRoot)
            {
                base.Clear();
            }
        }


        public int SafeCount
        {
            get
            {
                lock (SyncRoot)
                {
                    return this.Count;
                }
            }
        }
    }
}
