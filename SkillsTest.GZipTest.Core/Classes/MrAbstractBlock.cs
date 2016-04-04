using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using SkillsTest.GZipTest.Core.Classes;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Обезличенный блок конвейера.
    /// </summary>
    public abstract class MrAbstractBlock : IDisposable
    {
        /// <summary>
        /// Максимальное кол-во потоков обработки метода <see cref="MainAction"/>
        /// </summary>
        public uint MaxThreads { get; protected set; }
        /// <summary>
        /// Время ожидания потока при считывании данных из источников
        /// </summary>
        public int SleepTime { get; protected set; }

        /// <summary>
        /// Блоки-Источники информации
        /// </summary>
        private ICollection<MrAbstractBlock> sources = new List<MrAbstractBlock>();
        /// <summary>
        /// Целевые блоки данного экземпляра
        /// </summary>
        private ICollection<MrAbstractBlock> targets = new List<MrAbstractBlock>();
        /// <summary>
        /// Внутренний буфер блока обработки
        /// </summary>
        private BlockBuffer buffer = new BlockBuffer();
        /// <summary>
        /// Коллекция активных потоков
        /// </summary>
        private ThreadDictionary ThreadDictionary = new ThreadDictionary();

        private readonly object correctorDummy = new object();
        /// <summary>
        /// Корректор производительности
        /// </summary>
        private PerformanceCorrector PerformanceCorrector;

        private ProjectStatusEnum status;
        private readonly object statusDummy = new object();
        /// <summary>
        /// Текущий статус блока
        /// </summary>
        public ProjectStatusEnum Status
        {
            get
            {
                lock (statusDummy)
                {
                    return this.status;
                }
            }
            protected set
            {
                lock (statusDummy)
                {
                    this.status = value;
                }
            }
        }

        /// <summary>
        /// Генерирует отчет о внутреннем состоянии блока конвейера
        /// </summary>
        /// <returns>Отчет</returns>
        public PerformanceReport GenerateReport()
        {
            var result = new PerformanceReport()
            {
                Corrector = this.PerformanceCorrector,
                BufferSize = this.buffer.BufferSize,
                BufferAmount = this.buffer.Count,
                Block = this,
                ThreadCount = this.ThreadDictionary.SafeCount
            };

            return result;
        }

        /// <summary>
        /// Передает блоку указания по корректировке производительности. Возвращает экземпляр предыдущего корректировщика, если таковой был.
        /// </summary>
        /// <param name="corrector">Указания корректировки производительности</param>
        /// <returns></returns>
        public PerformanceCorrector SetPerformanceCorrector(PerformanceCorrector corrector)
        {
            lock (correctorDummy)
            {
                var oldCorrector = this.PerformanceCorrector;
                this.PerformanceCorrector = corrector;
                return oldCorrector;
            }
        }


        public MrAbstractBlock()
        {
            this.Status = ProjectStatusEnum.Unknown;
            this.PerformanceCorrector = null;
            this.MaxThreads = 1;
            this.SleepTime = 100;
        }


        /// <summary>
        /// Записывает в буфер фрагмент данных.
        /// После записи производится корректировка производительности.
        /// </summary>
        /// <param name="value"></param>
        protected virtual void AddToBuffer(PieceOf value)
        {
            this.buffer.Add(value);
            this.ExecPerformanceCorrector();
        }


        /// <summary>
        /// Корректировка производительности.
        /// Если блоку предписано изменить интенсивность работы
        /// </summary>
        protected virtual void ExecPerformanceCorrector()
        {
            if (this.PerformanceCorrector != null)
            {
                this.PerformanceCorrector.CorrectPerformance();
            }
        }

        /// <summary>
        /// Считать из источников <paramref name="count"/> фрагментов данных.
        /// Если в данный момент такого числа фрагментов в источниках нет - возвращаем что есть.
        /// </summary>
        /// <param name="count">Максимальное кол-во считываемых фрагментов</param>
        /// <returns>Коллекция фрагментов данных</returns>
        protected List<PieceOf> ReadFromSources(int count = 1)
        {
            List<PieceOf> result = new List<PieceOf>();

            try
            {
                foreach (var currentSource in sources)
                {
                    result.AddRange(currentSource.Receive(count - result.Count));

                    if (result.Count >= count)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                this.PostError(exception);
            }

            return result;
        }


        /// <summary>
        /// Версия <see cref="ReadFromSources"/> для возврата одного фрагмента.
        /// </summary>
        /// <returns>Фрагмент данных</returns>
        protected PieceOf ReadFromSourcesSingle()
        {
            return this.ReadFromSources(1).SingleOrDefault();
        }

        /// <summary>
        /// Отдать <paramref name="count"/> фрагментов данных.
        /// Если в данный момент такого числа фрагментов нет - возвращаем что есть.
        /// </summary>
        /// <param name="count">Максимальное кол-во считываемых фрагментов</param>
        /// <returns>Коллекция фрагментов данных</returns>
        protected List<PieceOf> Receive(int count = 1)
        {
            return this.buffer.Fetch(count);
        }

        /// <summary>
        /// Стартует работу блока
        /// </summary>
        public void Start()
        {
            DoMainWork(MainAction, this.MaxThreads);
        }

        /// <summary>
        /// Главный метод блока конвейера.
        /// Он будет выполнен в <see cref="MaxThreads"/> потоков.
        /// Внутри необходимо вызвать <see cref="PostDone"/> для сигнализации о завершении работы блока.
        /// Все прочие работы берет на себя класс-родитель.
        /// </summary>
        protected abstract void MainAction();

        private readonly object _mainWorkDummy = new object();
        /// <summary>
        /// Оборачивает <paramref name="mainWorkAction"/> во все необходимые для функционирования блока операции и выполняет в <paramref name="maxThreads"/> раз параллельно.
        /// </summary>
        /// <param name="mainWorkAction"></param>
        /// <param name="maxThreads"></param>
        private void DoMainWork(Action mainWorkAction, uint maxThreads = 1)
        {
            lock (_mainWorkDummy)
            {
                try
                {
                    if (this.Status != ProjectStatusEnum.Unknown || this.ThreadDictionary.Any())
                    {
                        return;
                    }
                    else
                    {
                        this.Status = ProjectStatusEnum.InProgress;
                    }

                    for (int i = 0; i <= maxThreads - 1; i++)
                    {
                        this.ThreadDictionary.SafeAdd(new Thread(
                            () =>
                            {
                                try
                                {
                                    mainWorkAction.Invoke();
                                }
                                catch (Exception exception)
                                {
                                    this.PostError(exception);
                                }
                            }));
                    }

                    foreach (var current in ThreadDictionary.Values)
                    {
                        current.Start();
                    }

                    if (this.PostStart() != ProjectStatusEnum.InProgress)
                    {
                        throw new Exception(string.Format("Ожидался переход в статус {0}", ProjectStatusEnum.InProgress));
                    }
                }
                catch (Exception exception)
                {
                    this.PostError(exception);
                }
            }
        }

        public override string ToString()
        {
            return this.GetType().ToString();
        }

        private readonly object postErrorDummy = new object();
        /// <summary>
        /// Сообщаем текущему и всем его целевым блокам об обнаруженном исключении.
        /// </summary>
        /// <param name="exception">исключение</param>
        /// <returns>исключение</returns>
        protected virtual Exception PostError(Exception exception)
        {
            lock (postErrorDummy)
            {
                try
                {
                    Exception result = new Exception(this.ToString(), exception);

                    if (this.Status == ProjectStatusEnum.InProgress)
                    {
                        this.Status = ProjectStatusEnum.Error;

                        foreach (var current in this.targets.ToList())
                        {
                            current.PostError(result);
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = new Exception(string.Format("Оповещение родителей блока {0} об ошибке", this.ToString()), e);
                }
            }

            return exception;
        }

        /// <summary>
        /// Завершили ли свою работы все источники данного блока конвейера
        /// </summary>
        public virtual bool AllSourcesDone
        {
            get
            {
                lock ((this.sources as ICollection).SyncRoot)
                {
                    return this.sources.All(x => x.Status == ProjectStatusEnum.Done);
                }
            }
        }


        private readonly object postStartDummy = new object();
        /// <summary>
        /// Сообщаем текущему и всем связанным блокам, что надо бы стартовать работу.
        /// </summary>
        /// <returns>Статус после завершения метода</returns>
        private ProjectStatusEnum PostStart()
        {
            lock (postStartDummy)
            {
                try
                {
                    if (this.Status == ProjectStatusEnum.Unknown)
                    {
                        this.Status = ProjectStatusEnum.InProgress;
                        this.Start();
                    }

                    foreach (var currentTarget in targets)
                    {
                        currentTarget.Start();
                    }

                    foreach (var currentSource in sources)
                    {
                        currentSource.Start();
                    }
                }
                catch (Exception exception)
                {
                    this.PostError(exception);
                }                

                return this.Status;
            }
        }

        public bool CancelOperation()
        {
            return PostCancel() == ProjectStatusEnum.Canceled;
        }

        private readonly object postCancelDummy = new object();
        /// <summary>
        /// Сообщаем текущему и всем связанным блокам, что всем спасибо - все свободны. Нас отменили.
        /// </summary>
        /// <returns>Статус после завершения метода</returns>
        protected virtual ProjectStatusEnum PostCancel()
        {
            lock (postCancelDummy)
            {
                try
                {
                    if (this.Status == ProjectStatusEnum.InProgress)
                    {
                        this.Status = ProjectStatusEnum.Canceled;

                        foreach (var current in this.targets.ToList())
                        {
                            current.PostCancel();
                        }

                        foreach (var current in this.sources.ToList())
                        {
                            current.PostCancel();
                        }
                    }
                }
                catch (Exception exception)
                {
                    this.PostError(exception);
                }
            }

            return this.Status;
        }


        private readonly object postDoneDummy = new object();
        /// <summary>
        /// Сообщаем текущему и всем связанным блокам, что работа была успешно завершена.
        /// </summary>
        /// <returns>Статус после завершения метода</returns>
        protected virtual ProjectStatusEnum PostDone()
        {
            lock (postDoneDummy)
            {
                try
                {
                    if (AllSourcesDone)
                    {
                        if (this.Status == ProjectStatusEnum.InProgress)
                        {
                            if (this.ThreadDictionary.SafeIamTheLast(Thread.CurrentThread))
                            {
                                this.Status = ProjectStatusEnum.Done;
                            }
                            this.ThreadDictionary.SafeRemoveAndComplete(Thread.CurrentThread);
                        }
                        else
                        {
                            throw new ThreadStateException(string.Format(
                                "В статус {0} можно перейти только из статуса {1}", ProjectStatusEnum.Done,
                                ProjectStatusEnum.InProgress));
                        }
                    }
                }
                catch (Exception exception)
                {
                    this.PostError(exception);
                }
            }

            return this.Status;
        }


        /// <summary>
        /// привязывает к текущему блоку блок из <paramref name="value"/> в качестве целевого
        /// </summary>
        /// <param name="value">Блок данных</param>
        public virtual void AddTarget(MrAbstractBlock value)
        {
            lock ((this.targets as ICollection).SyncRoot)
            {
                if (!this.targets.Contains(value))
                {
                    this.targets.Add(value);
                    value.AddSource(this);
                }
            }
        }


        /// <summary>
        /// Удаляет <paramref name="value"/> из списка целевых блоков
        /// </summary>
        /// <param name="value">Блок данных</param>
        public virtual void RemoveTarget(MrAbstractBlock value)
        {
            lock ((this.targets as ICollection).SyncRoot)
            {
                if (!this.targets.Contains(value))
                {
                    this.targets.Remove(value);
                    value.RemoveSource(this);
                }
            }
        }


        /// <summary>
        /// привязывает к текущему блоку блок из <paramref name="value"/> в качестве источника
        /// </summary>
        /// <param name="value">Блок данных</param>
        public virtual void AddSource(MrAbstractBlock value)
        {
            lock ((this.sources as ICollection).SyncRoot)
            {
                if (!this.sources.Contains(value))
                {
                    this.sources.Add(value);
                    value.AddTarget(this);
                }
            }
        }

        /// <summary>
        /// Удаляет <paramref name="value"/> из списка источников
        /// </summary>
        /// <param name="value">Блок данных</param>
        public virtual void RemoveSource(MrAbstractBlock value)
        {
            lock ((this.sources as ICollection).SyncRoot)
            {
                if (!this.sources.Contains(value))
                {
                    this.sources.Remove(value);
                    value.RemoveTarget(this);
                }
            }
        }

        public virtual void Dispose()
        {
            foreach (var current in this.targets.ToList())
            {
                current.RemoveSource(this);
            }
            this.targets.Clear();

            foreach (var current in this.sources.ToList())
            {
                current.RemoveSource(this);
            }
            this.sources.Clear();
        }
    }
}
