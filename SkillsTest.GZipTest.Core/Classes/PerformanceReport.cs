using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public struct PerformanceReport : IComparer<PerformanceReport>
    {
        public PerformanceCorrector Corrector;
        public ulong BufferSize;
        public uint BufferAmount;
        public MrAbstractBlock Block;
        public uint ThreadCount;

        public ulong BufferSizeMb
        {
            get { return this.BufferSize/1024/1024; }
        }

        public int Compare(PerformanceReport x, PerformanceReport y)
        {
            if (x.BufferSize > y.BufferSize)
            {
                return 1;
            }
            else if(y.BufferSize > x.BufferSize)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
