using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;

namespace SkillsTest.GZipTest.Core
{
    public class PerformanceCorrector
    {
        public static int SleepMax = 5000;

        public PerformanceActionEnum SetLevel { get; private set; }
        public int Duration { get; private set; }

        private readonly ThreadDictionary wasAppliedTo = new ThreadDictionary();

        public int WasAppliedTo()
        {
            lock (wasAppliedTo.SyncRoot)
            {
                return wasAppliedTo.Count;
            }
        }

        public PerformanceCorrector(PerformanceActionEnum newLevel, int duration)
        {
            this.SetLevel = newLevel;
            this.Duration = duration;
        }


        private readonly object correctPerformanceDummy = new object();
        public bool CorrectPerformance()
        {
            lock (correctPerformanceDummy)
            {
                try
                {
                    var currentThread = Thread.CurrentThread;

                    if (this.wasAppliedTo.SafeExists(currentThread))
                    {
                        return false;
                    }
                    else
                    {
                        this.wasAppliedTo.SafeAdd(currentThread);
                    }
                    
                    switch (SetLevel)
                    {
                        case PerformanceActionEnum.Sleep:
                            if (this.Duration <= 0)
                            {
                                throw new ArgumentOutOfRangeException("Duration",
                                    string.Format(
                                        "Для действия {0} должно быть указано положительное значение длительности.",
                                        SetLevel));
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
                                string.Format("Неизвестное значение [{0}] действия с производительностью!", SetLevel));
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
}
