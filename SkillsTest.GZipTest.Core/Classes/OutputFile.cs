using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class OutputFile : ProjectFile
    {
        private readonly object piecesDummy = new object();
        /// <summary>
        /// Коллекция с кусочками файла-результата
        /// </summary>
        protected SortedDictionary<int, PieceOf> Pieces = new SortedDictionary<int, PieceOf>();

        protected int LastWrittenSeqNo;

        public override ProjectFileTypeEnum FileType
        {
            get { return ProjectFileTypeEnum.Unknown;}
            protected set { }
        }

        public OutputFile(string inputFilePath)
            : base(inputFilePath)
        {
            this.Body = new FileStream(inputFilePath, FileMode.Create, FileAccess.Write,
                    FileShare.None);
            this.LastWrittenSeqNo = -1;
        }

        /// <summary>
        /// Записывает в файл-результат очередной кусочек.
        /// </summary>
        /// <remarks>Для оптимального расходования ресурсов необходимо чтобы кусочки попадали в файл в прямом порядке</remarks>
        /// <param name="value">Кусочек файла-результата</param>
        public virtual void AddPiece(SortedDictionary<int, PieceOf> value)
        {
            if (value == null || value.Count == 0)
            {
                return;
            }

            lock (piecesDummy)
            {
                if (value.ContainsKey(this.LastWrittenSeqNo + 1))
                {
                    PieceOf nextPiece;
                    do
                    {
                        nextPiece = null;

                        Interlocked.Increment(ref this.LastWrittenSeqNo);

                        if (!value.TryGetValue(this.LastWrittenSeqNo, out nextPiece))
                        {
                            if (this.Pieces.TryGetValue(this.LastWrittenSeqNo, out nextPiece))
                            {
                                this.Pieces.Remove(this.LastWrittenSeqNo);
                            }
                        }

                        if (nextPiece != null)
                        {
                            var tmpResultArray = nextPiece.GetBodyBuffer(true);
                            this.Body.Write(tmpResultArray, 0, tmpResultArray.Length);
                        }

                        
                    } while (nextPiece != null);

                    this.Body.Flush();
                }

                foreach (var currentKey in value.Keys.Where(x => x > this.LastWrittenSeqNo))
                {
                    this.Pieces.Add(currentKey, value[currentKey]);
                }
            }
        }
    }
}
