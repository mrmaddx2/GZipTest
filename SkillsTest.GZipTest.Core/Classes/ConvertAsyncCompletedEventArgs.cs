using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Информация о результатах работы потока обработки данных
    /// </summary>
    public class ConvertAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        public CompressionMode CompressionMode { get; protected set; }

        public ConvertAsyncCompletedEventArgs(CompressionMode mode, Exception error, bool isCancelled, object state)
            : base(error, isCancelled, state)
        {
            this.CompressionMode = mode;
        }
    }
}
