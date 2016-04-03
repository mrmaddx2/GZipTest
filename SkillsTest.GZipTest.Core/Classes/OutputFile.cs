using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SkillsTest.GZipTest.Core.Classes;

namespace SkillsTest.GZipTest.Core
{
    public class OutputFile : ProjectFile
    {
        private readonly object piecesDummy = new object();
        /// <summary>
        /// Коллекция с кусочками файла-результата
        /// </summary>
        protected BlockBuffer Pieces = new BlockBuffer();

        public override ProjectFileTypeEnum FileType
        {
            get { return ProjectFileTypeEnum.Unknown;}
            protected set { }
        }

        public OutputFile(string inputFilePath)
            : base(inputFilePath, FileMode.Create, FileAccess.Write,
                    FileShare.None)
        {
        }
        
        /// <summary>
        /// Записывает в файл-результат очередной кусочек.
        /// </summary>
        /// <remarks>Для оптимального расходования ресурсов необходимо чтобы кусочки попадали в файл в прямом порядке</remarks>
        /// <param name="value">Кусочек файла-результата</param>
        public virtual void AddPiece(List<PieceOf> value)
        {
            if (value == null || value.Count == 0)
            {
                return;
            }

            lock (piecesDummy)
            {
                this.Pieces.AddRange(value);

                for (int i = this.Pieces.BufferPieces.Count - 1; i >= 0; i--)
                {
                    var nextPiece = this.Pieces.BufferPieces.Values[i];

                    if (nextPiece.SeqNo != this.CurrentSeqNo)
                    {
                        break;
                    }
                    
                    var tmpBodyLength = nextPiece.Length();
                    this.Body.Write(nextPiece.GetBodyBuffer(true), 0, (int)tmpBodyLength);
                    this.Pieces.BufferPieces.RemoveAt(i);
                    Interlocked.Increment(ref this.CurrentSeqNo);
                }
                this.Body.Flush();
            }
        }
    }
}
