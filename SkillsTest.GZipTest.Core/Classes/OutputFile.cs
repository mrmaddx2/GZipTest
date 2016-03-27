using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class OutputFile : ProjectFile
    {
        private readonly object piecesDummy = new object();
        /// <summary>
        /// Коллекция с кусочками файла-результата
        /// </summary>
        protected HashSet<PieceOf> Pieces = new HashSet<PieceOf>();
        protected int LastWrittenSeqNo { get; set; }

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
        /// Готов ли кусочек к записи в файл/>
        /// </summary>
        /// <param name="value">Кусочек файла-результата. Должен являться частью коллекции <see cref="Pieces"/></param>
        /// <returns>true если готов и false в противном случае</returns>
        protected bool ReadyForWriting(PieceOf value)
        {
            bool result = false;

            lock (this.piecesDummy)
            {
                //Условиями готовности кусочка к записи в файл являются:
                //  2.1  Это первый кусочек
                if (this.LastWrittenSeqNo == value.SeqNo - 1 && value.SeqNo == this.Pieces.Min(x => x.SeqNo))
                {
                    result = true;
                }
                //  2.2  Или все предыдущие кусочки уже находятся в коллекции и либо уже записаны в файл, либо готовы к записи.
                else
                {
                    result =
                        this.Pieces.Any(
                        x => x.SeqNo + 1 == value.SeqNo &&
                             ReadyForWriting(x));
                }
            }

            return result;
        }

        /// <summary>
        /// Записывает в файл-результат очередной кусочек.
        /// </summary>
        /// <remarks>Для оптимального расходования ресурсов необходимо чтобы кусочки попадали в файл в прямом порядке</remarks>
        /// <param name="value">Кусочек файла-результата</param>
        public virtual void AddPiece(PieceOf value)
        {
            this.AddPiece(new HashSet<PieceOf>(){value});
        }

        public virtual void AddPiece(HashSet<PieceOf> value)
        {
            lock (this.piecesDummy)
            {
                this.Pieces.UnionWith(value);

                var tmpReadyPieces = this.Pieces.Where(ReadyForWriting).OrderBy(x => x.SeqNo).ToList();

                if (tmpReadyPieces.Any())
                {
                    foreach (var currentPiece in tmpReadyPieces)
                    {
                        var tmpResultArray = currentPiece.GetBodyBuffer(true);
                        this.Body.Write(tmpResultArray, 0, tmpResultArray.Length);
                    }

                    this.Pieces.RemoveWhere(x => tmpReadyPieces.Contains(x));

                    this.LastWrittenSeqNo = tmpReadyPieces.Max(x => x.SeqNo);
                }

                

                this.Body.Flush();
            }
        }
    }
}
