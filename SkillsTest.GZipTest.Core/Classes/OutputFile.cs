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
        /// <summary>
        /// Коллекция с кусочками файла-результата
        /// </summary>
        protected List<PieceOf> Pieces = new List<PieceOf>();

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
        protected virtual bool ReadyForWriting(PieceOf value)
        {
            bool result = false;

            lock ((Pieces as ICollection).SyncRoot)
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
                    if (value.SeqNo == 0)
                    {
                        result = true;
                    }
                    //  2.2  Или все предыдущие кусочки уже находятся в коллекции и либо уже записаны в файл, либо готовы к записи.
                    else
                    {
                        result = this.Pieces.Any(
                            x =>
                                x.SeqNo + 1 == value.SeqNo &&
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
        public virtual void AddPiece(PieceOf value)
        {
            this.AddPiece(new List<PieceOf>(){value});
        }

        public virtual void AddPiece(ICollection<PieceOf> value)
        {
            lock ((Pieces as ICollection).SyncRoot)
            {
                this.Pieces.AddRange(value);

                var tmpReadyPieces = this.Pieces.Where(this.ReadyForWriting).OrderBy(x => x.SeqNo).ToList();

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
