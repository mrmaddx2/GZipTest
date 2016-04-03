using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Корректирует производительность блоков конвейера
    /// </summary>
    public class PerformanceCorrector
    {
        /// <summary>
        /// Предпринимаемое действие, призванное спасти от outOfMemory
        /// </summary>
        public PerformanceActionEnum DoAction { get; private set; }
        /// <summary>
        /// Продолжительность действия
        /// </summary>
        public int Duration { get; private set; }
        /// <summary>
        /// Кол-во потоков к которым уже было применено <see cref="DoAction"/>
        /// </summary>
        public uint ApplyTo { get; private set; }

        /// <summary>
        /// Коллекция потоков к которым было применено <see cref="DoAction"/>
        /// </summary>
        private readonly ThreadDictionary wasAppliedTo = new ThreadDictionary();

        public int WasAppliedTo()
        {
            lock (wasAppliedTo.SyncRoot)
            {
                return wasAppliedTo.Count;
            }
        }

        /// <summary>
        /// Признак активности корректора
        /// </summary>
        public bool IsActive
        {
            get { return this.WasAppliedTo() < this.ApplyTo; }
        }

        public PerformanceCorrector(PerformanceActionEnum newLevel, int duration, uint applyTo)
        {
            this.DoAction = newLevel;
            this.Duration = duration;
            this.ApplyTo = applyTo;
        }


        private readonly object correctPerformanceDummy = new object();
        /// <summary>
        /// Корректирует активность текущего потока.
        /// </summary>
        /// <returns>Было ли применено <see cref="DoAction"/></returns>
        public bool CorrectPerformance()
        {
            if (!IsActive)
            {
                return false;
            }

            var currentThread = Thread.CurrentThread;

            lock (correctPerformanceDummy)
            {
                if (this.wasAppliedTo.SafeExists(currentThread))
                {
                    return false;
                }
                else
                {
                    this.wasAppliedTo.SafeAdd(currentThread);
                }
            }

            try
            {
                switch (DoAction)
                {
                    case PerformanceActionEnum.Sleep:
                        if (this.Duration <= 0)
                        {
                            throw new ArgumentOutOfRangeException("Duration",
                                string.Format(
                                    "Для действия {0} должно быть указано положительное значение длительности.",
                                    DoAction));
                        }
                        Thread.Sleep(this.Duration);
                        break;
                    case PerformanceActionEnum.Lower:
                        currentThread.Priority--;
                        break;
                    case PerformanceActionEnum.Higher:
                        currentThread.Priority++;
                        break;
                    case PerformanceActionEnum.Lowest:
                        currentThread.Priority = ThreadPriority.Lowest;
                        break;
                    case PerformanceActionEnum.Highest:
                        currentThread.Priority = ThreadPriority.Highest;
                        break;
                    case PerformanceActionEnum.DontTuch:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException("SetLevel",
                            string.Format("Неизвестное значение [{0}] действия с производительностью!", DoAction));
                }

                return true;

            }
            catch (Exception exception)
            {
                throw new Exception("Коррекция производительности", exception);
            }
        }
    }
}
