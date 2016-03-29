using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class InputFile : ProjectFile
    {
        /// <summary>
        /// Значение-костыль, являющееся по совместительству магическим числом для gz формата
        /// </summary>
        protected static readonly byte[] gZipMagicheader = { 31, 139, 08 };

        /// <summary>
        /// Значение по умолчанию для размера фрагмента сжимаемых данных. Заполняется в статическом конструкторе
        /// </summary>
        protected static uint DefaultFragmentSize;

        static InputFile()
        {
            DefaultFragmentSize = 512000;
        }

        protected int currentSeqNo;

        public InputFile(string inputFilePath)
            : base(inputFilePath)
        {
            this.Body = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.currentSeqNo = 0;

            var prevPosition = this.Body.Position;

            var tmpArray = new byte[gZipMagicheader.Length];
            this.Body.Position = 0;
            this.Body.Read(tmpArray, 0, tmpArray.Length);

            this.Body.Position = prevPosition;

            if (gZipMagicheader.SequenceEqual(tmpArray))
            {
                FileType = ProjectFileTypeEnum.GZip;
            }
            else
            {
                FileType = ProjectFileTypeEnum.Unknown;
            }
        }

        public override ProjectFileTypeEnum FileType { get; protected set; }

        /// <summary>
        /// Служит для получения очередного необработанного фрагмента файла-источника.
        /// </summary>
        /// <param name="inputFragmentSize">Размер буфера при считывании</param>
        /// <returns>Фрагмент файла-источника или null если все обработаны</returns>
        public virtual PieceOf Fetch(long? inputFragmentSize = null)
        {
            return this.Fetch(inputFragmentSize, null);
        }

        /// <summary>
        /// Служит для получения очередного необработанного фрагмента файла-источника.
        /// </summary>
        /// <param name="inputFragmentSize">Размер буфера при считывании</param>
        /// <param name="setStreamPosition">Позиция в потоке источника на которую необходимо перейти перед началом считывания</param>
        /// <returns>Фрагмент файла-источника или null если все обработаны</returns>
        public virtual PieceOf Fetch(long? inputFragmentSize = null, long? setStreamPosition = null)
        {
            var fragmentSize = inputFragmentSize ?? DefaultFragmentSize;

            if (fragmentSize < DefaultFragmentSize)
            {
                throw new ArgumentException(
                    string.Format(
                        "Укажите большее значение для размера блока данных операции сжатия. необходимо указать значение более {0}",
                        DefaultFragmentSize), "inputFragmentSize");
            }


            lock (this.Body)
            {
                PieceOf result = null;
                try
                {
                    if (setStreamPosition != null)
                    {
                        this.Body.Position = (long)setStreamPosition;
                    }
                    else
                    {
                        setStreamPosition = this.Body.Position;
                    }

                    result = new PieceOf(currentSeqNo);

                    //Нужно отщипнуть кусочек необходимомго размера

                    if (this.FileType == ProjectFileTypeEnum.GZip)
                    {
                        //С GZip все не просто
                        //Логика такова:
                        //Читаем файл-источник в буфер
                        //Ищем в буфере магическое число
                        //Если число найдено пишем в поток результата считанный буфер от начала и до магического числа
                        //Если в считанном буфере магического числа не оказалось - пишем в поток результата весь буфер

                        int matchesCount = 0;
                        var buffer = new byte[fragmentSize];
                        long nRead;
                        //Позиция начала следующего кусочка. На эту позицию будет установлен поток после заполнения текущего результата
                        long? positionOfNextPart = null;
                        //Костыль для предотвращения петли, т.к. начинаем мы читать всегда с магического числа
                        bool isFirstRead = true;
                        while (((nRead = Body.Read(buffer, 0, buffer.Length)) > 0) && positionOfNextPart == null)
                        {
                            long tmpIndex;
                            for (tmpIndex = 0; tmpIndex <= nRead - 1; tmpIndex++)
                            {
                                if (buffer[tmpIndex] == gZipMagicheader[matchesCount] && !isFirstRead)
                                {
                                    matchesCount++;
                                }
                                else
                                {
                                    matchesCount = 0;
                                }

                                if (matchesCount == gZipMagicheader.Length)
                                {
                                    positionOfNextPart = this.Body.Position - (nRead - 1) + tmpIndex - gZipMagicheader.Length;
                                    break;
                                }
                                isFirstRead = false;
                            }
                            
                            result.AddToBody(buffer, 0, (int)(positionOfNextPart == null ? nRead : (tmpIndex + 1) - gZipMagicheader.Length));
                        }

                        if (positionOfNextPart != null)
                        {
                            this.Body.Position = (long)positionOfNextPart;
                        }
                    }
                    else
                    {
                        //Отщипываем по кусочку фиксированной длины
                        //Отличаться от прочих может лишь последний кусочек
                        var buffer = new byte[fragmentSize];
                        var nRead = Body.Read(buffer, 0, buffer.Length);
                        result.AddToBody(buffer, 0, nRead);
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception(
                        string.Format("Получение кусочка источника. Начиная с позиции {0}",
                            setStreamPosition), exception);
                }
                finally
                {
                    if (this.Body != null && this.Body.CanRead)
                    {
                        this.Body.Flush();
                    }
                }

                var tmpLength = result.Length();
                if (tmpLength > 0)
                {
                    Interlocked.Increment(ref this.currentSeqNo);
                    result.PercentOfSource =
                        Convert.ToDecimal(tmpLength) / Convert.ToDecimal(this.Length()) * 100;
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
