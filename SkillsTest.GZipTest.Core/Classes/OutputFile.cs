using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class OutputFile : ProjectFile, IOutputFile
    {
        private readonly object piecesDummy = new object();
        /// <summary>
        /// Коллекция с кусочками файла-результата
        /// </summary>
        protected ICollection<IStatusedPieceOfResult> Pieces = new List<IStatusedPieceOfResult>();

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
        protected virtual bool ReadyForWriting(IStatusedPieceOfResult value)
        {
            bool result = false;

            lock (piecesDummy)
            {
                if (!this.Pieces.Contains(value))
                {
                    throw new ArgumentException(
                        "Переданный в качестве значения параметра кусочек не является частью файла-результата.", "value");
                }

                //Условиями готовности кусочка к записи в файл являются:
                //  1  Кусочек сам имеет соответствующий статус
                if (value.Status == PieceOfResultStatusEnum.Ready)
                {
                    //  2.1  Это первый кусочек
                    if (value.StartIndex == 0)
                    {
                        result = true;
                    }
                    //  2.2  Или все предыдущие кусочки уже находятся в коллекции и либо уже записаны в файл, либо готовы к записи.
                    else
                    {
                        result = this.Pieces.Any(
                            x =>
                                x.EndIndex == value.StartIndex &&
                                (x.Status == PieceOfResultStatusEnum.Written || this.ReadyForWriting(x)));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Записывает в файл-результат очередной кусочек.
        /// </summary>
        /// <remarks>Для оптимального расходования ресурсов необходимо чтобы кусочки попадали в файл в прямом порядке</remarks>
        /// <param name="value">Кусочек файла-результата</param>
        public virtual void AddPiece(IStatusedPieceOfResult value)
        {
            lock (piecesDummy)
            {
                this.Pieces.Add(value);

                var tmpReadyPieces = this.Pieces.Where(this.ReadyForWriting).OrderBy(x => x.StartIndex).ToList();

                if (tmpReadyPieces.Count == 0)
                {
                    var a = this.Pieces.OrderBy(x => x.StartIndex).ToList();
                }

                foreach (var currentPiece in tmpReadyPieces)
                {
                    var tmpResultArray = currentPiece.GetBodyBuffer(true);
                    this.Body.Write(tmpResultArray, 0, tmpResultArray.Length);
                    currentPiece.Status = PieceOfResultStatusEnum.Written;
                }

                this.Body.Flush();
            }
        }
    }
}
