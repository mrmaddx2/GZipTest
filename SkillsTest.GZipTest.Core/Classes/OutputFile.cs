using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class OutputFile : IOutputFile
    {
        /// <summary>
        /// Поток с файлом-результатом.
        /// Блокируем файл на время существования экземпляра.
        /// </summary>
        protected virtual Stream Target { get; private set; }

        private readonly object piecesDummy = new object();
        /// <summary>
        /// Коллекция с кусочками файла-результата
        /// </summary>
        protected ICollection<IExtendedPieceOfResult> Pieces = new List<IExtendedPieceOfResult>();

        public OutputFile(string filePath)
        {
            this.Target = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                    FileShare.None);
        }

        /// <summary>
        /// Готов ли кусочек к записи в <see cref="Target"/>
        /// </summary>
        /// <param name="value">Кусочек файла-результата. Должен являться частью коллекции <see cref="Pieces"/></param>
        /// <returns>true если готов и false в противном случае</returns>
        protected bool ReadyForWriting(IExtendedPieceOfResult value)
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
                if (value.Status == ExtendedPieceOfResultStatus.Ready)
                {
                    //  2.1  Это первый кусочек
                    if (value.InputStartIndex == 0)
                    {
                        result = true;
                    }
                    //  2.2  Или все предыдущие кусочки уже находятся в коллекции и либо уже записаны в файл, либо готовы к записи.
                    else
                    {
                        result = this.Pieces.Any(
                            x =>
                                x.InputEndIndex == value.InputStartIndex &&
                                (x.Status == ExtendedPieceOfResultStatus.Written || this.ReadyForWriting(x)));
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
        public virtual void AddPiece(IPieceOfResult value)
        {
            lock (piecesDummy)
            {
                this.Pieces.Add(new ExtendedPieceOfResult(value));

                var tmpReadyPieces = this.Pieces.Where(this.ReadyForWriting).OrderBy(x => x.InputStartIndex).ToList();

                foreach (var currentPiece in tmpReadyPieces)
                {
                    var tmpResultArray = currentPiece.GetOutputBuffer();
                    this.Target.Write(tmpResultArray, 0, tmpResultArray.Length);
                    currentPiece.Status = ExtendedPieceOfResultStatus.Written;
                }

                this.Target.Flush();
            }
        }

        public virtual void Dispose()
        {
            if (this.Target != null)
            {
                this.Target.Dispose();
            }
        }
    }
}
