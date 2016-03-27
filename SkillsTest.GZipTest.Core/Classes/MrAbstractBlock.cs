using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public abstract class MrAbstractBlock : IDisposable
    {
        protected ICollection<MrAbstractBlock> sources = new List<MrAbstractBlock>();
        protected ICollection<MrAbstractBlock> targets = new List<MrAbstractBlock>();
        protected ICollection<PieceOf> buffer = new List<PieceOf>();

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


        public MrAbstractBlock()
        {
            this.Status = ProjectStatusEnum.Unknown;
        }


        protected virtual void AddToBuffer(PieceOf value)
        {
            lock ((buffer as ICollection).SyncRoot)
            {
                this.buffer.Add(value);
            }
        }


        public ICollection<PieceOf> Receive(uint? count = null)
        {
            lock ((buffer as ICollection).SyncRoot)
            {
                var tmpValue = this.buffer.ToList().AsQueryable();

                tmpValue = tmpValue.OrderBy(x => x.SeqNo);

                if (count != null)
                {
                    tmpValue = tmpValue.Take((int)count);
                }

                foreach (var current in tmpValue)
                {
                    this.buffer.Remove(current);
                }

                return tmpValue.ToList();
            }
        }


        protected virtual Exception PostError(Exception exception)
        {
            Exception result = new Exception(this.GetType().ToString(), exception);

            if (this.Status == ProjectStatusEnum.InProgress)
            {
                this.Status = ProjectStatusEnum.Error;

                foreach (var current in this.targets.ToList())
                {
                    current.PostError(result);
                }
            }

            return exception;
        }


        protected virtual ProjectStatusEnum PostDone()
        {
            try
            {
                if (this.sources.All(x => x.Status == ProjectStatusEnum.Done))
                {
                    if (this.Status == ProjectStatusEnum.InProgress)
                    {
                        this.Status = ProjectStatusEnum.Done;
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

            return this.Status;
        }


        public void AddTarget(MrAbstractBlock value)
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


        public void RemoveTarget(MrAbstractBlock value)
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


        public void AddSource(MrAbstractBlock value)
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

        public void RemoveSource(MrAbstractBlock value)
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

        public void Dispose()
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
