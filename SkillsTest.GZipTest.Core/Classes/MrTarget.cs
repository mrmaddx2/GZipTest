using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class MrTarget : MrAbstractBlock
    {
        public event ProgressChangedEventHandler ProgressChanged;
        public event AsyncCompletedEventHandler ConvertAsyncCompleted;

        protected OutputFile outputFile;

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
        protected int PercentCompletedInt
        {
            get
            {
                return (int)Math.Floor(PercentCompleted);
            }
        }


        protected override void ExecPerformanceCorrector()
        {
            throw new NotSupportedException();
        }

        public MrTarget(string value)
        {
            this.outputFile = new OutputFile(value);
        }

        protected override void MainAction()
        {
            this.AsyncOpStartDttm = DateTime.Now;

            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            while (this.Status == ProjectStatusEnum.InProgress)
            {
                var cntOfPieces = this.AllSourcesDone ? 100 : 10;

                var source = this.ReadFromSources(cntOfPieces);
                if (source.Any())
                {
                    this.outputFile.AddPiece(source);
                    this.ReportProgress(source.Sum(x => x.PercentOfSource));
                }

                if (!source.Any())
                {
                    if (this.PostDone() != ProjectStatusEnum.Done)
                    {
                        Thread.Sleep(this.SleepTime);
                    }
                    else
                    {
                        this.outputFile.Dispose();
                        this.outputFile = null;
                    }
                }
            }
        }


        protected override ProjectStatusEnum PostDone()
        {
            var result = base.PostDone();

            if (result == ProjectStatusEnum.Done)
            {
                this.OnConvertAsyncCompleted(new AsyncCompletedEventArgs(null, this.Status == ProjectStatusEnum.Canceled, Thread.CurrentThread));
            }

            return result;
        }


        protected override ProjectStatusEnum PostCancel()
        {
            var result = base.PostCancel();

            if (result == ProjectStatusEnum.Canceled)
            {
                this.OnConvertAsyncCompleted(new AsyncCompletedEventArgs(null, this.Status == ProjectStatusEnum.Canceled, Thread.CurrentThread));
            }

            return result;
        }


        protected override Exception PostError(Exception exception)
        {
            var result = base.PostError(exception);

            this.OnConvertAsyncCompleted(new AsyncCompletedEventArgs(result, this.Status == ProjectStatusEnum.Canceled, Thread.CurrentThread));

            return result;
        }


        protected virtual void OnProgressChanged(ConvertProgressChangedEventArgs e)
        {
            var handler = ProgressChanged;
            if (handler != null) handler(e);
        }

        protected virtual void OnConvertAsyncCompleted(AsyncCompletedEventArgs e)
        {
            var handler = ConvertAsyncCompleted;
            if (handler != null) handler(this, e);
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
        protected virtual void ReportProgress(decimal incProgress)
        {
            bool changed = false;
            var tmpPerc = IncPersentCompleted(incProgress, out changed);

            if (changed)
            {
                OnProgressChanged(new ConvertProgressChangedEventArgs(tmpPerc, Thread.CurrentThread.ManagedThreadId, this.AsyncOpStartDttm));
            }
        }


        public override void AddTarget(MrAbstractBlock value)
        {
            throw new NotSupportedException();
        }

        public override void RemoveTarget(MrAbstractBlock value)
        {
            throw new NotSupportedException();
        }
    }
}
