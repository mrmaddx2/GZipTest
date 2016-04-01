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
        protected HashSet<PieceOf> Pieces = new HashSet<PieceOf>();

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
        }
        
        /// <summary>
        /// Записывает в файл-результат очередной кусочек.
        /// </summary>
        /// <remarks>Для оптимального расходования ресурсов необходимо чтобы кусочки попадали в файл в прямом порядке</remarks>
        /// <param name="value">Кусочек файла-результата</param>
        public virtual void AddPiece(HashSet<PieceOf> value)
        {
            if (value == null || value.Count == 0)
            {
                return;
            }

            lock (piecesDummy)
            {
                this.Pieces.UnionWith(value);

                if (this.Pieces.Count >= 100)
                {
                    var a = 1;
                }

                var actualSeqNo = this.CurrentSeqNo + 1;
                if (value.Any(x => x.SeqNo == actualSeqNo))
                {
                    PieceOf nextPiece;
                    while ((nextPiece = this.Pieces.SingleOrDefault(x => x.SeqNo == actualSeqNo)) != null)
                    {
                        var tmpBodyLength = nextPiece.Length();
                        this.Body.Write(nextPiece.GetBodyBuffer(true), 0, (int)tmpBodyLength);
                        actualSeqNo++;
                    }

                    if (actualSeqNo - 1 > this.CurrentSeqNo)
                    {
                        this.CurrentSeqNo = actualSeqNo - 1;
                    }

                    this.Body.Flush();

                    this.Pieces.RemoveWhere(x => x.SeqNo < actualSeqNo);
                }
            }
        }
    }
}
