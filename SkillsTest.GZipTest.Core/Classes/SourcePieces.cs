using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class SourcePieces : List<PieceOfSource>
    {
        public void SafeAdd(PieceOfSource value)
        {
            lock (this)
            {
                base.Add(value);
            }
        }

        public void SafeRemove(PieceOfSource key)
        {
            lock (this)
            {
                base.Remove(key);
            }
        }

        public void SafeClear()
        {
            lock (this)
            {
                base.Clear();
            }
        }


        public int SafeCount
        {
            get
            {
                int result;

                lock (this)
                {
                    result = this.Count;
                }

                return result;
            }
        }
    }
}
