using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public enum PerformanceActionEnum
    {
        Sleep = int.MinValue,
        Lowest = -200,
        Lower = -100,
        DontTuch = 0,
        Higher = 100,
        Highest = 200
    }
}
