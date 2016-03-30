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
        private ICollection<MrAbstractBlock> sources = new List<MrAbstractBlock>();
        private ICollection<MrAbstractBlock> targets = new List<MrAbstractBlock>();
        private readonly object bufferDummy = new object();
        private HashSet<PieceOf> buffer = new HashSet<PieceOf>();

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
            lock (bufferDummy)
            {
                this.buffer.Add(value);
            }
        }

        protected HashSet<PieceOf> ReadFromSources(uint count = 1)
        {
            var result = new HashSet<PieceOf>();

            try
            {
                foreach (var currentSource in sources)
                {
                    result.UnionWith(currentSource.Receive(Convert.ToUInt32(count - result.Count)));

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

        protected HashSet<PieceOf> Receive(uint count = 1)
        {
            var result = new HashSet<PieceOf>();

            lock (bufferDummy)
            {
                result.UnionWith(this.buffer.OrderBy(x => x.SeqNo).Take((int)count));

                this.buffer.ExceptWith(result);
            }

            return result;
        }


        protected abstract void Start();


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


        private readonly object allSourcesDoneDummy = new object();
        protected bool AllSourcesDone
        {
            get
            {
                lock (allSourcesDoneDummy)
                {
                    return this.sources.All(x => x.Status == ProjectStatusEnum.Done);
                }
            }
        }


        private readonly object postStartDummy = new object();
        protected ProjectStatusEnum PostStart()
        {
            lock (postStartDummy)
            {
                if (this.Status == ProjectStatusEnum.Unknown)
                {
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

                return this.Status;
            }
        }


        protected virtual ProjectStatusEnum PostDone()
        {
            try
            {
                if (AllSourcesDone)
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
