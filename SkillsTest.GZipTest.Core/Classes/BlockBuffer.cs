using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core.Classes
{
    public class BlockBuffer
    {
        public readonly object SyncRoot = new object();
        private SortedDictionary<int, PieceOf> buffer = new SortedDictionary<int, PieceOf>();

        public uint Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return (uint)this.buffer.Count;
                }
            }
        }

        public ulong BufferSize { get; private set; }

        public void Add(PieceOf value)
        {
            lock (this.SyncRoot)
            {
                this.buffer.Add(value.GetHashCode(), value);
                this.BufferSize += value.Length();
            }
        }

        public HashSet<PieceOf> Fetch(uint count = 1)
        {
            var result = new HashSet<PieceOf>();

            lock (this.SyncRoot)
            {
                result.UnionWith(this.buffer.Values.Take((int)count));

                foreach (var current in result)
                {
                    this.buffer.Remove(current.GetHashCode());
                }

                this.BufferSize -= (ulong)result.Sum(x => (long)x.Length());
            }

            return result;
        }
    }
}
