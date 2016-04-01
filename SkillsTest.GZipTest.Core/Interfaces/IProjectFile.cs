using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IProjectFile : IDisposable
    {
        long Length();
        ProjectFileTypeEnum FileType { get; }
        //long CurrentSeqNo { get; }
    }
}
