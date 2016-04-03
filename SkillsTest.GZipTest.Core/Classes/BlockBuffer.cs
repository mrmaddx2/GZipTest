using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core.Classes
{
    public class BlockBuffer
    {
        public readonly object SyncRoot = new object();
        private SortedList<long, PieceOf> buffer = new SortedList<long, PieceOf>();

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


        private long GetKey(long value)
        {
            return long.MaxValue - value;
        }

        public void Add(PieceOf value)
        {
            lock (this.SyncRoot)
            {
                this.buffer.Add(this.GetKey(value.SeqNo), value);
                this.BufferSize += value.Length();
            }
        }

        public SortedList<long, PieceOf> BufferPieces 
        {
            get { return this.buffer; }
        }

        public void AddRange(ICollection<PieceOf> value)
        {
            foreach (var current in value)
            {
                this.Add(current);
            }
        }

        public List<PieceOf> Fetch(int count = 1)
        {
            try
            {
                if (count <= 0)
                {
                    throw new ArgumentOutOfRangeException("count", "Значение должно быть положительным числом");
                }

                var result = new List<PieceOf>();

                lock (this.SyncRoot)
                {
                    int lastIndex = this.buffer.Count - 1;
                    int firstIndex = lastIndex - count;

                    if (firstIndex < 0)
                    {
                        firstIndex = 0;
                    }

                    for (int i = lastIndex; i > firstIndex; i--)
                    {
                        result.Add(this.buffer.Values[i]);
                        this.buffer.RemoveAt(i);
                    }

                    if (result.Any())
                    {
                        this.BufferSize -= (ulong)result.Sum(x => (long)x.Length());
                    }

                    if (count == 1 && result.Count > 1)
                    {
                        var a = 1;
                    }
                }

                return result;
            }
            catch (Exception exception)
            {
                throw new Exception("Извлечение данных из буфера", exception);
            }
        }
    }
}
