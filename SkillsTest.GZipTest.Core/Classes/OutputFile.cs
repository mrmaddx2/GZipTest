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

        public int LastWrittenSeqNo;

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
            this.LastWrittenSeqNo = 0;
        }

        public virtual void AddPiece(HashSet<PieceOf> value)
        {
            var tmp = new SortedDictionary<int, PieceOf>();

            foreach (var current in value)
            {
                tmp.Add(current.SeqNo, current);
            }

            this.AddPiece(tmp);
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
                var actualSeqNo = this.LastWrittenSeqNo + (this.LastWrittenSeqNo == 0 ? 0 : 1);
                if (value.ContainsKey(actualSeqNo))
                {
                    PieceOf nextPiece;
                    do
                    {
                        nextPiece = null;

                        if (!value.TryGetValue(actualSeqNo, out nextPiece))
                        {
                            if (this.Pieces.TryGetValue(actualSeqNo, out nextPiece))
                            {
                                this.Pieces.Remove(actualSeqNo);
                            }
                        }

                        if (nextPiece != null)
                        {
                            var tmpResultArray = nextPiece.GetBodyBuffer(true);
                            this.Body.Write(tmpResultArray, 0, tmpResultArray.Length);
                            actualSeqNo++;
                            this.LastWrittenSeqNo = nextPiece.SeqNo;
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
