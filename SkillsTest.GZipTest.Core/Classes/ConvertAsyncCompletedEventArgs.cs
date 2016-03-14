using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class ConvertAsyncCompletedEventArgs : AsyncCompletedEventArgs
    {
        private CompressionMode compressionMode;

        public CompressionMode CompressionMode
        {
            get
            {
                return compressionMode;
            }
        }

        public ConvertAsyncCompletedEventArgs(CompressionMode mode, Exception error, bool isCancelled, object state)
            : base(error, isCancelled, state)
        {
            this.compressionMode = mode;
        }
    }
}
