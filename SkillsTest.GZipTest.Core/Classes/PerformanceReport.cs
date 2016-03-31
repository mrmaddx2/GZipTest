using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public struct PerformanceReport
    {
        public PerformanceCorrector Corrector;
        public ulong BufferSize;
        public uint BufferAmount;
        public MrAbstractBlock Block;
    }
}
