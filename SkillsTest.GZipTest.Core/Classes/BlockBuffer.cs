using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core.Classes
{
    /// <summary>
    /// Буфер.
    /// </summary>
    /// <remarks>
    /// Представляет из себя обертку для коллекции фрагментов инофрмации.
    /// </remarks>
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

        /// <summary>
        /// Общее кол-во информации в буфере. В Kb
        /// </summary>
        public ulong BufferSize { get; private set; }

        /// <summary>
        /// Функция предназначена для того, чтобы фрагменты в коллекции распологались в обратном порядке.
        /// </summary>
        /// <param name="value">фрагмент данных</param>
        /// <returns>long.MaxValue минут порядковый номер фрагмента данных</returns>
        private long GetKey(PieceOf value)
        {
            return long.MaxValue - value.SeqNo;
        }

        public void Add(PieceOf value)
        {
            lock (this.SyncRoot)
            {
                this.buffer.Add(this.GetKey(value), value);
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

        /// <summary>
        /// Извлекает <paramref name="count"/> фрагментов из буфера.
        /// </summary>
        /// <param name="count">Кол-во извлекаемых фрагментов</param>
        /// <returns>Коллекция извлеченных фрагментов</returns>
        public List<PieceOf> Fetch(int count = 1)
        {
            try
            {
                if (count <= 0)
                {
                    throw new ArgumentOutOfRangeException("count", "Значение должно быть положительным числом");
                }

                List<PieceOf> result = null;

                lock (this.SyncRoot)
                {
                    //TODO: переделать
                    result = new List<PieceOf>(this.buffer.OrderByDescending(x => x.Key).Select(x => x.Value).Take(count).ToList());

                    /*

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
                    }*/

                    foreach (var current in result)
                    {
                        this.buffer.Remove(this.GetKey(current));
                    }

                    if (result.Any())
                    {
                        this.BufferSize -= (ulong)result.Sum(x => (long)x.Length());
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
