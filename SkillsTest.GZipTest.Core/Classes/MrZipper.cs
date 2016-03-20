using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Отвечает за сжатие и распаковку данных с помощью класса GZipStream
    /// </summary>
    public class MrZipper : IDisposable
    {
        #region Delegates
        protected delegate void ConvertPiecesActionHandler(
            string inputFilePath, string outputFilePath, CompressionMode compressionMode, AsyncOperation asyncOperation);

        public delegate void ProgressChangedEventHandler(
            ProgressChangedEventArgs e);

        public delegate void ConvertEventHandler(ConvertAsyncCompletedEventArgs e);
        #endregion

        #region Events
        public event ProgressChangedEventHandler ProgressChanged;
        public event ConvertEventHandler CompressAsyncCompleted;
        public event ConvertEventHandler DecompressAsyncCompleted;
        public event ConvertEventHandler ConvertAsyncCompleted;
        #endregion

        #region Properties & fields
        private readonly object IsCancelledDummy = new object();
        protected bool isCancelled;
        /// <summary>
        /// Отменена ли асинхронная операция
        /// </summary>
        public bool IsCancelled
        {
            get
            {
                lock (IsCancelledDummy)
                {
                    return isCancelled;
                }
            }
            set
            {
                lock (IsCancelledDummy)
                {
                    isCancelled = value;
                }
            }
        }

        /// <summary>
        /// Сколько процентов от общего числа составляет один обработанный кусочек. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        protected decimal percentCompletedInc;

        private readonly object percentCompletedDummy = new object();
        protected decimal percentCompleted;
        /// <summary>
        /// Выраженный в процентах прогресс текущей асинхронной операции.
        /// </summary>
        protected int PercentCompleted
        {
            get
            {
                lock (percentCompletedDummy)
                {
                    if (this.runningThreads.SafeCount == 0 && this.percentCompleted > 0)
                    {
                        percentCompleted = 100;
                    }

                    return (int)Math.Floor(percentCompleted);
                }
            }
        }

        /// <summary>
        /// Значение-костыль, являющееся по совместитульству магическим числом для gz формата
        /// </summary>
        protected static readonly byte[] gZipMagicheader = { 31, 139, 08 };

        /// <summary>
        /// Максимальное кол-во потоков обработки
        /// </summary>
        protected static uint maxThreads;
        /// <summary>
        /// Значение по умолчанию для размера фрагмента сжимаемых данных
        /// </summary>
        protected static uint defaultFragmentSize = 512000;

        /// <summary>
        /// Информация о бегущих потоках
        /// </summary>
        protected ThreadDictionary runningThreads = new ThreadDictionary();


        private readonly object StatusDummy = new object();
        /// <summary>
        /// Текущий статус экземпляра. Актуален лишь во время выполнения асинхронных операций.
        /// </summary>
        protected MrZipperStatusEnum Status
        {
            get
            {
                lock (StatusDummy)
                {
                    if (this.runningThreads.SafeCount != 0)
                    {
                        return MrZipperStatusEnum.InProgress;
                    }
                    else
                    {
                        if (this.SourceList.SafeCount == 0)
                        {
                            return MrZipperStatusEnum.Done;
                        }
                        else
                        {
                            return MrZipperStatusEnum.None;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Коллекция с наметками фрагментов исходного файла. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        protected SourcePieces SourceList = new SourcePieces();
        protected IOutputFile outputFile;
        /// <summary>
        /// Поток с исходным файлом. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        /// <remarks>Поток остается открыт на протяжении работы операций сжатия и распаковки. Это необходимо для защиты от модификации файла во время работы методов.</remarks>
        protected Stream inputFile;

        #endregion

        public MrZipper()
        {
        }

        public virtual void CancelConvertAsync()
        {
            this.IsCancelled = true;
        }

        protected virtual void OnConvertAsyncCompleted(ConvertAsyncCompletedEventArgs e)
        {
            switch (e.CompressionMode)
            {
                case CompressionMode.Compress:
                    this.OnCompressAsyncCompleted(e);
                    break;
                case CompressionMode.Decompress:
                    this.OnDecompressAsyncCompleted(e);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("e");
            }

            var handler = ConvertAsyncCompleted;
            if (handler != null)
            {
                handler(e);
            }
        }


        protected virtual void ConvertPiecesCompleted(ConvertAsyncCompletedEventArgs e, AsyncOperation operation)
        {
            lock (this.runningThreads.SyncRoot)
            {
                try
                {
                    if (e.Error != null)
                    {
                        OnConvertAsyncCompleted(e);
                    }
                    else
                    {
                        if (this.runningThreads.SafeIamTheLast(operation))
                        {
                            if (e.Cancelled || this.SourceList.SafeCount == 0)
                            {
                                OnConvertAsyncCompleted(e);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    var newEventArgs = new ConvertAsyncCompletedEventArgs(e.CompressionMode, exception, e.Cancelled,
                        e.UserState);
                    OnConvertAsyncCompleted(newEventArgs);
                }
                finally
                {
                    this.runningThreads.SafeRemoveAndComplete(e.UserState.ToString());
                }
            }
        }

        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(e);
            }
        }

        static MrZipper()
        {
            MrZipper.maxThreads = ProcessInfo.NumberOfProcessorThreads;
            if (MrZipper.maxThreads == 0)
            {
                MrZipper.maxThreads = 1;
            }
        }

        


        /// <summary>
        /// Служит для получения наметки очередного необработанного фрагмента файла-источника.
        /// Полученная наметка будет удалена из коллекции <see cref="SourceList"/>
        /// </summary>
        /// <returns>Наметка фрагмента файла-источника или null если все обработаны</returns>
        protected virtual PieceOfSource? Fetch()
        {
            PieceOfSource? result = null;

            lock (this.SourceList)
            {
                if (this.SourceList.Count > 0)
                {
                    result = this.SourceList[0];
                    this.SourceList.RemoveAt(0);
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
        /// <param name="compressFragmentSize">Размер фрагмента данных для операции сжатия</param>
        protected virtual void Refresh(string inputFilePath, string outputFilePath, CompressionMode mode, long? compressFragmentSize = null)
        {
            //Очень уж обширное обновление
            lock (this)
            {
                this.ReleaseLock();
                this.runningThreads.SafeClear();

                this.percentCompleted = 0;
                this.IsCancelled = false;

                this.SourceList.SafeClear();

                this.inputFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                this.outputFile = new OutputFile(outputFilePath);


                #region Валидация

                switch (mode)
                {
                    case CompressionMode.Compress:
                        compressFragmentSize = compressFragmentSize ?? defaultFragmentSize;
                        if (compressFragmentSize < defaultFragmentSize)
                        {
                            throw new ArgumentException(string.Format("Укажите большее значение для размера блока данных операции сжатия. необходимо указать значение более {0}", defaultFragmentSize), "compressFragmentSize");
                        }
                        break;
                    case CompressionMode.Decompress:
                        if (this.IndexOfNextCompressedPart(0) != 0)
                        {
                            throw new ArgumentException("Файл-источник не является архивом gzip!", "inputFilePath");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mode");
                } 


                
                #endregion

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
                            nextIndex = currentIndex + (long)compressFragmentSize;
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
                
                this.percentCompletedInc = Convert.ToDecimal(100) / Convert.ToDecimal(this.SourceList.Count);

                this.inputFile.Flush();
            }
        }


        /// <summary>
        /// Поиск начала следующего фрагмента данных в архиве. Метод не детерминированный, т.к. зависит и меняет состояние <see cref="inputFile"/>
        /// </summary>
        /// <param name="setStreamPosition">Позиция в <see cref="inputFile"/>, с которой метод начнет читать данные</param>
        /// <returns>Позиция начала фрагмента данных в архиве</returns>
        protected long? IndexOfNextCompressedPart(long? setStreamPosition = null)
        {
            //TODO : ускорить
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
                if (this.inputFile != null && this.inputFile.CanRead)
                {
                    this.inputFile.Flush();
                }
            }

            return result;
        }


        /// <summary>
        /// Фетчит одну за другой наметки от файла-источника и проделывает с ними операцию <paramref name="compressionMode"/>
        /// </summary>
        /// <param name="asyncOperation">Экземпляр описывающий текущий поток. Если null то метод считает, что он выполняется в синхронном режиме</param>
        protected virtual void ConvertPieces(string inputFilePath, string outputFileFolder, CompressionMode compressionMode, AsyncOperation asyncOperation = null)
        {
            Exception e = null;

            try
            {
                PieceOfSource? newPiece;
                //До тех пор пока не закончатся наметки или не отменят асинхронную операцию
                while ((newPiece = this.Fetch()) != null && !this.IsCancelled)
                {
                    var pieceOfResult = new PieceOfResult(inputFilePath, (PieceOfSource) newPiece);

                    switch (compressionMode)
                    {
                        case CompressionMode.Compress:
                            pieceOfResult.Compress();
                            break;
                        case CompressionMode.Decompress:
                            pieceOfResult.Decompress();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("compressionMode");
                    }

                    this.outputFile.AddPiece(pieceOfResult);

                    //Если запущены в асинхронном режиме - необходимо проделать чуть больше работы
                    if (asyncOperation != null)
                    {
                        ReportProgress(percentCompletedInc, asyncOperation.UserSuppliedState);
                    }
                }
            }
            catch (Exception exception)
            {
                e = exception;
            }
            finally
            {
                if (asyncOperation != null)
                {
                    this.ConvertPiecesCompleted(
                        new ConvertAsyncCompletedEventArgs(compressionMode, e, this.IsCancelled,
                            asyncOperation.UserSuppliedState), asyncOperation);
                }
                else
                {
                    if (e != null)
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Приращивает значение к свойству <see cref="PercentCompleted"/>
        /// </summary>
        /// <param name="incValue">Значение на которое будет увеличено свойство</param>
        /// <param name="changed">Изменилось ли значение свойства</param>
        /// <returns></returns>
        protected virtual int IncPersentCompleted(decimal incValue, out bool changed)
        {
            decimal result = 0;
            lock (percentCompletedDummy)
            {
                result = this.percentCompleted + incValue;

                //Небольшой костыль, для того чтобы пользователю не было отображено 100 процентов раньше того как все кусочки соберуться в единый файл-результат
                if (result > 99)
                {
                    result = 99;
                }

                if ((int)result > (int)this.percentCompleted)
                {
                    changed = true;
                }
                else
                {
                    changed = false;
                }

                this.percentCompleted = result;
            }

            return (int)Math.Floor(result);
        }

        /// <summary>
        /// Если значение свойства <see cref="PercentCompleted"/> после приращения значения из <paramref name="incProgress"/> изменилось, то необходимо вызвать соответствующее событие
        /// </summary>
        /// <param name="incProgress">Значение на которое будет увеличено свойство</param>
        /// <param name="state">Идентификатор потока в котором была обработана порция данных</param>
        protected virtual void ReportProgress(decimal incProgress, object state)
        {
            bool changed = false;
            var tmpPerc = IncPersentCompleted(incProgress, out changed);

            if (changed)
            {
                OnProgressChanged(new ProgressChangedEventArgs(tmpPerc, state));
            }
        }


        /// <summary>Сжатие файла источника и заись результата на диск</summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="compressFragmentSize">Размер фрагмента данных</param>
        public virtual void Compress(string inputFilePath, string outputFilePath, long compressFragmentSize = 512000)
        {
            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Compress, compressFragmentSize);

            this.ConvertPieces(inputFilePath, Path.GetDirectoryName(outputFilePath), CompressionMode.Compress);

            this.ReleaseLock();
        }


        /// <summary>
        /// Асинхронный вариант метода <see cref="Compress"/>
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="compressFragmentSize">Размер фрагмента данных</param>
        public virtual void CompressAsync(string inputFilePath, string outputFilePath, long? compressFragmentSize = null)
        {
            if (this.Status == MrZipperStatusEnum.InProgress)
            {
                throw new ThreadStateException("Асинхронная операция уже запущена!");
            }

            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Compress, compressFragmentSize);

            for (int i = 0; i <= maxThreads - 1; i++)
            {
                var currentThreadId = Guid.NewGuid().ToString();

                var currentThread = AsyncOperationManager.CreateOperation(currentThreadId);
                
                this.runningThreads.SafeAdd(currentThreadId, currentThread);

                ConvertPiecesActionHandler convertPiecesAction = ConvertPieces;
                convertPiecesAction.BeginInvoke(
                    inputFilePath,
                    Path.GetDirectoryName(outputFilePath),
                    CompressionMode.Compress,
                    currentThread,
                    null,
                    null);
            }
        }


        /// <summary>
        /// Асинхронный вариант метода <see cref="Decompress"/>
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        public virtual void DecompressAsync(string inputFilePath, string outputFilePath)
        {
            if (this.Status == MrZipperStatusEnum.InProgress)
            {
                throw new ThreadStateException("Асинхронная операция уже запущена!");
            }

            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Decompress);

            for (int i = 0; i <= maxThreads - 1; i++)
            {
                var currentThreadId = Guid.NewGuid().ToString();

                var currentThread = AsyncOperationManager.CreateOperation(currentThreadId);

                this.runningThreads.SafeAdd(currentThreadId, currentThread);

                ConvertPiecesActionHandler convertPiecesAction = ConvertPieces;
                convertPiecesAction.BeginInvoke(
                    inputFilePath,
                    Path.GetDirectoryName(outputFilePath),
                    CompressionMode.Decompress,
                    currentThread,
                    null,
                    null);
            }
        }


        /// <summary>Распаковка файла и запись результатан а диск</summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        public virtual void Decompress(string inputFilePath, string outputFilePath)
        {
            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Decompress);

            this.ConvertPieces(inputFilePath, Path.GetDirectoryName(outputFilePath), CompressionMode.Decompress);

            this.ReleaseLock();
        }

        /// <summary>
        /// Освобождает связанные с экземпляром ресурсы
        /// </summary>
        protected virtual void ReleaseLock()
        {
            if (outputFile != null)
            {
                this.outputFile.Dispose();
                this.outputFile = null;
            }

            if (inputFile != null)
            {
                this.inputFile.Dispose();
                this.inputFile = null;
            }
        }

        public virtual void Dispose()
        {
            ReleaseLock();
        }

        protected virtual void OnDecompressAsyncCompleted(ConvertAsyncCompletedEventArgs e)
        {
            var handler = DecompressAsyncCompleted;
            if (handler != null) handler(e);
        }

        protected virtual void OnCompressAsyncCompleted(ConvertAsyncCompletedEventArgs e)
        {
            var handler = CompressAsyncCompleted;
            if (handler != null) handler(e);
        }
    }
}
