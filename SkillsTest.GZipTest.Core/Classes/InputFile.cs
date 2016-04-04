using System;
using System.CodeDom;
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
        protected static byte[] gZipMagicheader = { 31, 139, 08 };

        /// <summary>
        /// Значение по умолчанию для размера фрагмента сжимаемых данных. Заполняется в статическом конструкторе
        /// </summary>
        protected static uint DefaultFragmentSize;

        private byte[] prevReadBuffer;

        static InputFile()
        {
            DefaultFragmentSize = 512000;
        }

        public InputFile(string inputFilePath)
            : base(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
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

        /// <summary>
        /// Тип файла
        /// </summary>
        public readonly ProjectFileTypeEnum FileType;

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

                    result = new PieceOf(this.CurrentSeqNo);
                    Interlocked.Increment(ref this.CurrentSeqNo);

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
                        int nRead;
                        //Позиция начала следующего кусочка. На эту позицию будет установлен поток после заполнения текущего результата
                        int positionOfNextPart = -1;
                        using (var tmpMemStream = new MemoryStream())
                        {
                            while (((nRead = Body.Read(buffer, 0, buffer.Length)) > 0) && positionOfNextPart == -1)
                            {
                                positionOfNextPart = FindMatches(ref buffer, ref gZipMagicheader, ref matchesCount, 0, nRead - 1, false);

                                if (positionOfNextPart >= 0)
                                {
                                    matchesCount = 0;
                                }

                                tmpMemStream.Write(buffer, 0,
                                    positionOfNextPart == -1 ? nRead : positionOfNextPart);
                            }

                            result.ResetBody(tmpMemStream);
                            tmpMemStream.Close();
                        }
                        

                        if (positionOfNextPart != -1)
                        {
                            this.Body.Position = positionOfNextPart;
                        }
                    }
                    else
                    {
                        using (var tmpMemStream = new MemoryStream())
                        {
                            //Отщипываем по кусочку фиксированной длины
                            //Отличаться от прочих может лишь последний кусочек
                            var buffer = new byte[fragmentSize];
                            var nRead = Body.Read(buffer, 0, buffer.Length);

                            tmpMemStream.Write(buffer, 0, nRead);

                            result.ResetBody(tmpMemStream);
                            tmpMemStream.Close();
                        }
                    }

                    this.Body.Flush();
                }
                catch (Exception exception)
                {
                    throw new Exception(
                        string.Format("Получение кусочка источника. Начиная с позиции {0}",
                            setStreamPosition), exception);
                }

                var tmpLength = result.Length();
                if (tmpLength > 0)
                {
                    result.PercentOfSource = Convert.ToDecimal(tmpLength)/Convert.ToDecimal(this.Length())*100;
                    return result;
                }
                else if(Body.Position != Body.Length)
                {
                    throw new Exception("Кусочек нулевой длины обнаружен до окончания потока");
                }
                else
                {
                    return null;
                }
            }
        }


        private int FindMatches(ref byte[] where, ref byte[] what, ref int matchesCount, int startIndex = 0, int endIndex = 0, bool ignoreFirstMatch = false)
        {
            //Найдем индекс первой позиции совпадения
            int currentIndex = startIndex - 1;
            var whereLength = where.Length;
            var whatLength = what.Length;

            if (endIndex == 0)
            {
                endIndex = whereLength;
            }

            while ((currentIndex = Array.IndexOf(where, what[matchesCount], currentIndex + 1, (ignoreFirstMatch ? 2 : 1))) >= 0)
            {
                matchesCount++;
                ignoreFirstMatch = false;
                if (currentIndex >= endIndex || matchesCount >= whatLength)
                {
                    break;
                }
            }

            if (matchesCount == whatLength || whereLength == currentIndex)
            {
                return currentIndex - matchesCount;
            }

            matchesCount = 0;

            return -1;
        }
    }
}
