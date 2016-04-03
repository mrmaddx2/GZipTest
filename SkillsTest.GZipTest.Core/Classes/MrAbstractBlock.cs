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
    public abstract class MrAbstractBlock : IDisposable
    {
        public uint MaxThreads { get; protected set; }
        public int SleepTime { get; protected set; }

        private ICollection<MrAbstractBlock> sources = new List<MrAbstractBlock>();
        private ICollection<MrAbstractBlock> targets = new List<MrAbstractBlock>();
        private BlockBuffer buffer = new BlockBuffer();
        private ThreadDictionary ThreadDictionary = new ThreadDictionary();
        private readonly object correctorDummy = new object();
        private PerformanceCorrector PerformanceCorrector;

        private ProjectStatusEnum status;
        private readonly object statusDummy = new object();
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


        protected virtual void AddToBuffer(PieceOf value)
        {
            if (value.Length() == 0)
            {
                var a = 1;
            }

            this.buffer.Add(value);
            this.ExecPerformanceCorrector();
        }

        protected void ExecPerformanceCorrector()
        {
            if (this.PerformanceCorrector != null)
            {
                this.PerformanceCorrector.CorrectPerformance();
            }
        }

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

        protected PieceOf ReadFromSourcesSingle()
        {
            return this.ReadFromSources(1).SingleOrDefault();
        }

        protected List<PieceOf> Receive(int count = 1)
        {
            return this.buffer.Fetch(count);
        }


        public void Start()
        {
            DoMainWork(MainAction, this.MaxThreads);
        }

        protected abstract void MainAction();

        private readonly object _mainWorkDummy = new object();
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
