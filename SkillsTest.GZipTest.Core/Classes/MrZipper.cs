using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    public class MrZipper : IMrZipper
    {
        #region Delegates
        protected delegate void ConvertPiecesActionHandler(
            CompressionMode compressionMode, AsyncOperation asyncOperation, long? fragmentSize = null);
        #endregion

        #region Events
        public event ProgressChangedEventHandler ProgressChanged;
        public event ConvertEventHandler CompressAsyncCompleted;
        public event ConvertEventHandler DecompressAsyncCompleted;
        public event ConvertEventHandler ConvertAsyncCompleted;
        #endregion

        #region Properties & fields
        /// <summary>
        /// Дата и время старта асинхронной операции. Заполняется в методе <see cref="Refresh"/>
        /// </summary>
        protected DateTime? AsyncOpStartDttm { get; set; }

        private readonly object percentCompletedDummy = new object();
        private decimal percentCompleted;
        /// <summary>
        /// Выраженный в процентах прогресс текущей асинхронной операции.
        /// </summary>
        protected decimal PercentCompleted
        {
            get
            {
                lock (percentCompletedDummy)
                {
                    return this.percentCompleted;
                }
            }
            set
            {
                lock (percentCompletedDummy)
                {
                    this.percentCompleted = value;
                }
            }
        }

        /// <summary>
        /// Выраженный в процентах прогресс текущей асинхронной операции.
        /// </summary>
        public int PercentCompletedInt
        {
            get
            {
                return (int)Math.Floor(PercentCompleted);
            }
        }

        /// <summary>
        /// Максимальное кол-во потоков обработки. Заполняется в статическом конструкторе
        /// </summary>
        protected static uint MaxThreads { get; private set; }

        /// <summary>
        /// Информация о бегущих потоках
        /// </summary>
        protected IThreadDictionary runningThreads = new ThreadDictionary();


        private ProjectStatusEnum status;
        private readonly object statusDummy = new object();
        /// <summary>
        /// Текущий статус экземпляра. Актуален лишь во время выполнения асинхронных операций.
        /// </summary>
        protected ProjectStatusEnum Status
        {
            get
            {
                lock (statusDummy)
                {
                    return this.status;
                }
            }
            set
            {
                lock (statusDummy)
                {
                    if (this.status != value && value == ProjectStatusEnum.InProgress)
                    {
                        this.AsyncOpStartDttm = DateTime.Now;
                    }
                    else if(value == ProjectStatusEnum.Unknown)
                    {
                        this.AsyncOpStartDttm = null;
                    }
                    this.status = value;
                }
            }
        }
        /// <summary>
        /// Результат работы методов конфертации данных. Инициируется в методе <see cref="Refresh"/>
        /// </summary>
        /// <remarks>Поток остается открыт на протяжении работы операций сжатия и распаковки. Это необходимо для защиты от модификации файла во время работы методов.</remarks>
        protected IOutputFile outputFile;
        /// <summary>
        /// Поток с исходным файлом. Инициируется в методе <see cref="Refresh"/>
        /// </summary>
        /// <remarks>Поток остается открыт на протяжении работы операций сжатия и распаковки. Это необходимо для защиты от модификации файла во время работы методов.</remarks>
        protected IInputFile inputFile;

        #endregion

        public MrZipper()
        {
        }

        /// <summary>
        /// Отменить выполнение текущей асинхронной операции
        /// </summary>
        public virtual void CancelConvertAsync()
        {
            if (this.Status == ProjectStatusEnum.InProgress)
            {
                this.Status = ProjectStatusEnum.Canceled;
            }
            else
            {
                throw new InvalidAsynchronousStateException("В данный момент нет активных асинхронных операций");
            }
        }

        /// <summary>
        /// Оповещает подписчиков асинхроных операций
        /// </summary>
        /// <param name="e">Информация о результатах работы</param>
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


        /// <summary>
        /// Завершает жизненный цикл потока обработки кусочков исходного файла
        /// </summary>
        /// <param name="e">Информация о результатах работы</param>
        /// <param name="operation">Информация о потоке</param>
        protected virtual void ConvertPiecesCompleted(ConvertAsyncCompletedEventArgs e, AsyncOperation operation)
        {
            lock (this.runningThreads.SyncRoot)
            {
                try
                {
                    //Если в одном из потоков произошла ошибка - необходимо остановить все прочие
                    if (e.Error != null)
                    {
                        this.Status = ProjectStatusEnum.Error;
                        OnConvertAsyncCompleted(e);
                    }
                    else
                    {
                        //Только последний поток из пула возвращает результаты и освобождает занятые ресурсы
                        if (this.runningThreads.SafeIamTheLast(operation))
                        {
                            if (e.Cancelled || this.inputFile.Status == ProjectStatusEnum.Done)
                            {
                                this.Status = ProjectStatusEnum.Done;
                                OnConvertAsyncCompleted(e);
                            }

                            this.ReleaseLock();
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
                    this.runningThreads.SafeRemoveAndComplete(operation);
                }
            }
        }

        protected virtual void OnProgressChanged(ConvertProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(e);
            }
        }

        static MrZipper()
        {
            //В дальнейшем будем создавать по потоку на процессор
            MrZipper.MaxThreads = (uint)Process.GetCurrentProcess().ProcessorAffinity;
            if (MrZipper.MaxThreads == 0)
            {
                MrZipper.MaxThreads = 1;
            }
        }
        
        

        /// <summary>
        /// Сбрасывает на дефолтное внутреннее состояние экземпляра.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="mode">Режим работы</param>
        protected virtual void Refresh(string inputFilePath, string outputFilePath, CompressionMode mode, ProjectStatusEnum initStatus = ProjectStatusEnum.Unknown)
        {
            //Очень уж обширное обновление
            lock (this)
            {
                this.ReleaseLock();
                this.runningThreads.SafeClear();

                this.PercentCompleted = 0;
                this.Status = initStatus;

                this.inputFile = new InputFile(inputFilePath);
                this.outputFile = new OutputFile(outputFilePath);


                #region Валидация

                switch (mode)
                {
                    case CompressionMode.Compress:
                        break;
                    case CompressionMode.Decompress:
                        if (inputFile.FileType != ProjectFileTypeEnum.GZip)
                        {
                            throw new ArgumentException("Файл-источник не является архивом gzip!", "inputFilePath");
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mode");
                }



                #endregion
            }
        }


        /// <summary>
        /// Фетчит один за другим кусочки от файла-источника и проделывает с ними операцию <paramref name="compressionMode"/>
        /// </summary>
        /// <param name="asyncOperation">Экземпляр описывающий текущий поток. Если null то метод считает, что он выполняется в синхронном режиме</param>
        /// <param name="compressionMode">Тип текущей операции</param>
        /// <param name="fragmentSize">Размер буфера при считывании</param>
        protected virtual void ConvertPieces(CompressionMode compressionMode, AsyncOperation asyncOperation = null, long? fragmentSize = null)
        {
            Exception e = null;

            try
            {
                IPieceOfSource newPiece;
                //До тех пор пока не прочтем весь исходный файл, не отменят асинхронную операцию или не произойдет ошибки в других потоках
                while ((newPiece = this.inputFile.Fetch(fragmentSize)) != null && !(this.Status == ProjectStatusEnum.Canceled || this.Status == ProjectStatusEnum.Error))
                {
                    //Нужно запоминать длину кусочка на этом этапе, т.к. в процессе обработки данные кусочка источника могут быть очищены
                    var sourcePieceLength = newPiece.Length;
                    var pieceOfResult = new StatusedPieceOfResult(newPiece);

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
                    newPiece.Dispose();

                    this.outputFile.AddPiece(pieceOfResult);

                    //Если запущены в асинхронном режиме - необходимо проделать чуть больше работы
                    if (asyncOperation != null)
                    {
                        var percentCompletedInc =
                            Convert.ToDecimal(sourcePieceLength) / Convert.ToDecimal(this.inputFile.Length) * 100;

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
                        new ConvertAsyncCompletedEventArgs(compressionMode, e, this.Status == ProjectStatusEnum.Canceled,
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
        /// <returns>Значение свойства <see cref="PercentCompleted"/> после приращения</returns>
        protected virtual int IncPersentCompleted(decimal incValue, out bool changed)
        {
            decimal result = 0;
            lock (percentCompletedDummy)
            {
                result = this.PercentCompleted + incValue;

                changed = Math.Floor(result) > this.PercentCompletedInt;

                this.PercentCompleted = result;
            }

            return (int)Math.Floor(result);
        }

        /// <summary>
        /// Если значение свойства <see cref="PercentCompletedInt"/> после приращения значения из <paramref name="incProgress"/> изменилось, то необходимо вызвать соответствующее событие
        /// </summary>
        /// <param name="incProgress">Значение на которое будет увеличено свойство</param>
        /// <param name="state">Идентификатор потока в котором была обработана порция данных</param>
        protected virtual void ReportProgress(decimal incProgress, object state)
        {
            bool changed = false;
            var tmpPerc = IncPersentCompleted(incProgress, out changed);

            if (changed)
            {
                OnProgressChanged(new ConvertProgressChangedEventArgs(tmpPerc, state, this.AsyncOpStartDttm));
            }
        }


        private readonly object convertAsyncDummy = new object();
        protected void ConvertAsync(string inputFilePath, string outputFilePath, CompressionMode mode, long? compressFragmentSize = null)
        {
            lock (convertAsyncDummy)
            {
                if (this.Status == ProjectStatusEnum.InProgress)
                {
                    throw new ThreadStateException("Асинхронная операция уже запущена!");
                }

                this.Refresh(inputFilePath, outputFilePath, mode, ProjectStatusEnum.InProgress);

                for (int i = 0; i <= MaxThreads - 1; i++)
                {
                    var currentThread = AsyncOperationManager.CreateOperation(Guid.NewGuid().ToString());

                    this.runningThreads.SafeAdd(currentThread);

                    ConvertPiecesActionHandler convertPiecesAction = ConvertPieces;
                    convertPiecesAction.BeginInvoke(
                        mode,
                        currentThread,
                        compressFragmentSize,
                        null,
                        null);
                }
            }
        }


        /// <summary>
        /// Асинхронный вариант метода <see cref="Compress"/>
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="compressFragmentSize">Размер фрагмента данных</param>
        public virtual void CompressAsync(string inputFilePath, string outputFilePath, long? compressFragmentSize = null)
        {
            ConvertAsync(inputFilePath, outputFilePath, CompressionMode.Compress, compressFragmentSize);
        }


        /// <summary>
        /// Асинхронный вариант метода <see cref="Decompress"/>
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        public virtual void DecompressAsync(string inputFilePath, string outputFilePath)
        {
            ConvertAsync(inputFilePath, outputFilePath, CompressionMode.Decompress);
        }


        protected void ConvertMethod(string inputFilePath, string outputFilePath, CompressionMode mode,
            long? compressFragmentSize = null)
        {
            try
            {
                this.Refresh(inputFilePath, outputFilePath, mode);

                this.ConvertPieces(mode);
            }
            catch (Exception exception)
            {
                throw exception;
            }
            finally
            {
                this.ReleaseLock();
            }
        }


        /// <summary>Сжатие файла источника и заись результата на диск</summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="compressFragmentSize">Размер фрагмента данных</param>
        public virtual void Compress(string inputFilePath, string outputFilePath, long? compressFragmentSize = null)
        {
            ConvertMethod(inputFilePath, outputFilePath, CompressionMode.Compress, compressFragmentSize);
        }


        /// <summary>Распаковка файла и запись результатан а диск</summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        public virtual void Decompress(string inputFilePath, string outputFilePath)
        {
            ConvertMethod(inputFilePath, outputFilePath, CompressionMode.Decompress);
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
