using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class ConvertProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public TimeSpan Elapsed
        {
            get
            {
                return new TimeSpan(DateTime.Now.Ticks - this.StartDttm.Ticks);
            }
        }

        public TimeSpan Remains
        {
            get
            {
                var elapsed = this.Elapsed;

                if (this.ProgressPercentage == 0 || elapsed.Ticks == 0)
                {
                    return TimeSpan.MaxValue;
                }
                else
                {
                    return new TimeSpan
                        (Convert.ToInt64(Convert.ToDecimal(elapsed.Ticks)/Convert.ToDecimal(this.ProgressPercentage)*
                                         Convert.ToDecimal(100 - this.ProgressPercentage)));
                }
            }
        }

        public DateTime StartDttm { get; protected set; }

        public ConvertProgressChangedEventArgs(int progressPercentage, object userState, DateTime? startDttm = null)
            : base(progressPercentage, userState)
        {
            this.StartDttm = startDttm ?? DateTime.Now;
        }
    }
}
