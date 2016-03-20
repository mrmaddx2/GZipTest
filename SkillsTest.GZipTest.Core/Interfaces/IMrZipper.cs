using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IMrZipper : IDisposable
    {
        #region Events
        event ProgressChangedEventHandler ProgressChanged;
        event ConvertEventHandler CompressAsyncCompleted;
        event ConvertEventHandler DecompressAsyncCompleted;
        event ConvertEventHandler ConvertAsyncCompleted;
        #endregion

        void Compress(string inputFilePath, string outputFilePath, long? compressFragmentSize);
        void CompressAsync(string inputFilePath, string outputFilePath, long? compressFragmentSize);
        void Decompress(string inputFilePath, string outputFilePath);
        void DecompressAsync(string inputFilePath, string outputFilePath);
        void CancelConvertAsync();
    }
}
