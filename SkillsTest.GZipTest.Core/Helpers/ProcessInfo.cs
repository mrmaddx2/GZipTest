using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    //Код честно украден из коммента http://stackoverflow.com/questions/188503/detecting-the-number-of-processors/189371#189371

    /// <summary>
    /// Provides a single property which gets the number of processor threads
    /// available to the currently executing process.
    /// </summary>
    public static class ProcessInfo
    {
        /// <summary>
        /// Gets the number of processors.
        /// </summary>
        /// <value>The number of processors.</value>
        public static uint NumberOfProcessorThreads
        {
            get
            {
                uint processAffinityMask;

                using (var currentProcess = Process.GetCurrentProcess())
                {
                    processAffinityMask = (uint)currentProcess.ProcessorAffinity;
                }

                const uint BitsPerByte = 8;
                var loop = BitsPerByte * sizeof(uint);
                uint result = 0;

                while (--loop > 0)
                {
                    result += processAffinityMask & 1;
                    processAffinityMask >>= 1;
                }

                return (result == 0) ? 1 : result;
            }
        }
    }
}
