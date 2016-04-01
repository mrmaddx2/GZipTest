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

        public uint LastWrittenSeqNo;

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

                var actualSeqNo = this.LastWrittenSeqNo + (this.LastWrittenSeqNo == 0 ? 0 : 1);
                if (value.Any(x => x.SeqNo == actualSeqNo))
                {
                    PieceOf nextPiece;
                    while ((nextPiece = this.Pieces.SingleOrDefault(x => x.SeqNo == actualSeqNo)) != null)
                    {
                        var tmpBodyLength = nextPiece.Length();
                        this.Body.Write(nextPiece.GetBodyBuffer(true), 0, (int)tmpBodyLength);
                        actualSeqNo++;
                        this.LastWrittenSeqNo = nextPiece.SeqNo;
                    }

                    this.Body.Flush();

                    this.Pieces.RemoveWhere(x => x.SeqNo <= this.LastWrittenSeqNo);
                }
            }
        }
    }
}
