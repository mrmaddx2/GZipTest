using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace SkillsTest.GZipTest.Core
{
    public class MrsDispatcher : MrAbstractBlock
    {
        public event AsyncCompletedEventHandler Completed;

        private readonly object workersDummy = new object();
        private List<MrAbstractBlock> workers = new List<MrAbstractBlock>();

        public MrsDispatcher()
        {
            this.Start();
        }

        protected override Exception PostError(Exception exception)
        {
            var result = base.PostError(exception);

            this.OnConvertAsyncCompleted(new AsyncCompletedEventArgs(result, this.Status == ProjectStatusEnum.Canceled, Thread.CurrentThread));

            return result;
        }


        protected virtual void OnConvertAsyncCompleted(AsyncCompletedEventArgs e)
        {
            var handler = Completed;
            if (handler != null) handler(this, e);
        }

        protected override void MainAction()
        {
            while (this.Status == ProjectStatusEnum.InProgress)
            {
                ComputerInfo CI = new ComputerInfo();
                var currentProc = Process.GetCurrentProcess();
                var avalMem = Convert.ToDecimal(CI.AvailablePhysicalMemory) /
                              Convert.ToDecimal(CI.TotalPhysicalMemory) * 100;

                //Начинаются проблемы
                if (avalMem < 50)
                {
                    List<PerformanceReport> controlledReports = null;
                    lock (workersDummy)
                    {
                        //Соберем отчеты источников
                        controlledReports = workers.Where(x => x.Status == ProjectStatusEnum.InProgress && (x is MrSource || x is MrConverter)).Select(x => x.GenerateReport()).ToList();
                    }

                    //50 процентов от занятой приложением памяти
                    var maxMemoryUsage = (ulong)Math.Floor(Convert.ToDouble(currentProc.PrivateMemorySize64) * 0.25);

                    var problemSources =
                        controlledReports
                            .Where(
                                x =>
                                    x.BufferSize > maxMemoryUsage &&
                                    !(x.Block is MrConverter && !x.Block.AllSourcesDone))
                            .ToList();

                    if (problemSources.Any())
                    {
                        foreach (var current in problemSources.Where(x => x.Corrector == null || !x.Corrector.IsActive))
                        {
                            var oldCorrector =
                                current.Block.SetPerformanceCorrector(
                                    new PerformanceCorrector(PerformanceActionEnum.Sleep,
                                        2000, current.ThreadCount));
                        }
                    }

                    
                }

                if (avalMem < 20)
                {
                    GC.Collect();
                }

                if (this.PostDone() != ProjectStatusEnum.Done)
                {
                    Thread.Sleep(this.SleepTime);
                }
            }
        }

        public override void AddSource(MrAbstractBlock value)
        {
            this.workers.Add(value);
        }

        public override void RemoveSource(MrAbstractBlock value)
        {
            this.workers.Remove(value);
        }

        public override void RemoveTarget(MrAbstractBlock value)
        {
            throw new NotSupportedException();
        }

        public override void AddTarget(MrAbstractBlock value)
        {
            throw new NotSupportedException();
        }

        public override bool AllSourcesDone
        {
            get { return this.workers.Any() && this.workers.All(x => x.Status == ProjectStatusEnum.Done); }
        }

        public override void Dispose()
        {
            this.workers.Clear();
            base.Dispose();
        }
    }
}
