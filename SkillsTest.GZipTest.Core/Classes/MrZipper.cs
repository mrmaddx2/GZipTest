using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Отвечает за сжатие и распаковку данных с помощью класса GZipStream
    /// </summary>
    public class MrZipper : IDisposable
    {
        /// <summary>
        /// Значение-костыль, являющееся по совместитульству магическим числом для gz формата
        /// </summary>
        private static readonly byte[] gZipMagicheader = { 31, 139, 08 };

        /// <summary>
        /// Максимальное кол-во потоков обработки
        /// </summary>
        private static int maxThreads;

        
        static MrZipper()
        {
            //TODO : Надо бы определять значение в зависимости от кол-ва процессоров
            MrZipper.maxThreads = 5;
        }
        /// <summary>
        /// Размер фрагмента файла-источника для операции сжатия
        /// </summary>
        private static long compressFragmentSize = 512000;
        /// <summary>
        /// Заглушка для синхронизации доступа к value членам
        /// </summary>
        private readonly object _indexDummy = new object();
        /// <summary>
        /// Путь к файлу-источнику. Значение присваивается в методе <see cref="Refresh"/>
        /// </summary>
        private string inputFilePath { get; set; }
        /// <summary>
        /// Путь к файлу-результату. Значение присваивается в методе <see cref="Refresh"/>
        /// </summary>
        private string outputFilePath { get; set; }
        /// <summary>
        /// Папка файла-результата.
        /// </summary>
        private string outputFileFolder { get { return Path.GetDirectoryName(this.outputFilePath); } }
        /// <summary>
        /// Коллекция с наметками фрагментов исходного файла. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        private List<PieceOfSource> SourceList = new List<PieceOfSource>();
        /// <summary>
        /// Коллекция с результами работы экземпляра. Очищается в методе <see cref="Refresh"/>, а заполняется по мере работы методов сжатия и распаковки
        /// </summary>
        private List<PieceOfResult> ResultList = new List<PieceOfResult>();
        /// <summary>
        /// Режим работы в данный момент. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        private CompressionMode mode;
        /// <summary>
        /// Поток с исходным файлом. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        /// <remarks>Поток остается открыт на протяжении работы операций сжатия и распаковки. Это необходимо для защиты модификации файла-источника во время работы методов.</remarks>
        private FileStream inputFile;


        /// <summary>
        /// Служит для получения наметки очередного необработанного фрагмента файла-источника.
        /// Полученная наметка будет удалена из коллекции <see cref="SourceList"/>
        /// </summary>
        /// <returns>Наметка фрагмента файла-источника или null если все обработаны</returns>
        private PieceOfSource? Fetch()
        {
            PieceOfSource? result = null;

            lock (this.SourceList)
            {
                int lastIndex = this.SourceList.Count - 1;

                if (lastIndex >= 0)
                {
                    result = this.SourceList[lastIndex];
                    this.SourceList.RemoveAt(lastIndex);
                }
            }

            return result;
        }

        /// <summary>
        /// Сбрасывает на дефолтное внутреннее состояние экземпляра.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="mode">Режим работы</param>
        private void Refresh(string inputFilePath, string outputFilePath, CompressionMode mode)
        {
            lock (this._indexDummy)
            {
                this.outputFilePath = outputFilePath;
                this.inputFilePath = inputFilePath;
                this.mode = mode;

                lock (this.ResultList)
                {
                    this.ResultList.Clear();
                }

                lock (this.SourceList)
                {
                    this.SourceList.Clear();
                }

                this.inputFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                

                //Делаем раскройку входящего файла, чтобы далее не приходилось лочить ресурсы для поиска границ кусочков
                long currentIndex = 0;
                long nextIndex = 0;
                long inputFileLength = inputFile.Length;
                while (currentIndex < inputFileLength)
                {
                    switch (mode)
                    {
                        case CompressionMode.Compress:
                            //Для сжатия все просто
                            //Отщипываем по кусочку фиксированной длины
                            //Отличаться от прочих может лишь последний кусочек
                            nextIndex = currentIndex + compressFragmentSize;
                            if (nextIndex > inputFileLength)
                            {
                                nextIndex = inputFileLength;
                            }
                            break;
                        case CompressionMode.Decompress:
                            //С распаковкой сложнее
                            nextIndex = this.IndexOfNextCompressedPart((currentIndex == 0 ? (long?)gZipMagicheader.Length : null)) ??
                                        inputFileLength;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("mode");
                    }                    

                    this.SourceList.Add(new PieceOfSource(currentIndex, nextIndex - currentIndex));

                    currentIndex = nextIndex;
                }


                this.inputFile.Flush();
            }
        }


        /// <summary>
        /// Поиск начала следующего фрагмента данных в архиве. Метод не детерминированный, т.к. зависит и меняет состояние <see cref="inputFile"/>
        /// </summary>
        /// <param name="setStreamPosition">Позиция в <see cref="inputFile"/>, с которой метод начнет читать данные</param>
        /// <returns>Позиция начала фрагмента данных в архиве</returns>
        private long? IndexOfNextCompressedPart(long? setStreamPosition = null)
        {
            long? result = null;

            try
            {
                if (setStreamPosition != null)
                {
                    this.inputFile.Position = (long) setStreamPosition;
                }
                else
                {
                    setStreamPosition = this.inputFile.Position;
                }

                int currentByte;
                int matchesCount = 0;
                while ((currentByte = this.inputFile.ReadByte()) != -1)
                {
                    if (Convert.ToByte(currentByte) == gZipMagicheader[matchesCount])
                    {
                        matchesCount++;
                    }
                    else
                    {
                        matchesCount = 0;
                    }

                    if (matchesCount == gZipMagicheader.Length)
                    {
                        result = this.inputFile.Position - gZipMagicheader.Length;
                        break;
                    }
                }

                //На случай достижения окончания файла
                if (matchesCount == 0 && this.inputFile.Position >= this.inputFile.Length)
                {
                    result = this.inputFile.Length;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Поиск начала следующего кусочка данных для распаковки. Начиная с позиции {0}",
                        setStreamPosition), exception);
            }
            finally
            {
                if (this.inputFile != null)
                {
                    this.inputFile.Flush();
                }
            }

            return result;
        }


        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        public void Compress(string inputFilePath, string outputFilePath)
        {
            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Compress);

            PieceOfSource? newPiece;
            while ((newPiece = this.Fetch()) != null)
            {
                var pieceOfResult = new PieceOfResult(this.inputFilePath, (PieceOfSource)newPiece, this.outputFileFolder);

                pieceOfResult.Compress();

                this.ResultList.Add(pieceOfResult);
            }

            this.WriteResult();
        }


        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        public void Decompress(string inputFilePath, string outputFilePath)
        {
            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Decompress);

            PieceOfSource? newPiece;
            while ((newPiece = this.Fetch()) != null)
            {
                var pieceOfResult = new PieceOfResult(this.inputFilePath, (PieceOfSource)newPiece, this.outputFileFolder);

                pieceOfResult.Decompress();

                this.ResultList.Add(pieceOfResult);
            }

            this.WriteResult();
        }

        /// <summary>
        /// Собирает все обработанные кусочки данных в единый файл-результат
        /// </summary>
        private void WriteResult()
        {
            FileStream targetFile = null;
            try
            {
                targetFile = new FileStream(this.outputFilePath, FileMode.Create, FileAccess.Write);

                var pieces =
                    this.ResultList.OrderBy(x => x.StartIndex);

                foreach (
                    var currentPiece in
                        pieces)
                {
                    var tmpBuffer = currentPiece.GetOutputBuffer();

                    targetFile.Write(tmpBuffer, 0, tmpBuffer.Length);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Запись в файл {0}", this.outputFilePath), exception);
            }
            finally
            {
                if (targetFile != null)
                {
                    targetFile.Close();
                }
            }
        }

        public void Dispose()
        {
            if (this.inputFile != null)
            {
                this.inputFile.Flush();
                this.inputFile.Close();
            }
        }
    }
}
