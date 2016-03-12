using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class PieceOfResult : IDisposable
    {
        public long StartIndex { get; private set; }
        public long InputLength { get; private set; }
        public string OutputFilePath { get; private set; }
        private FileStream outputFile;
        private string inputFilePath;

        public PieceOfResult(string inputFilePath, long startIndex, long inputLength, string outputFileFolder, string outputFileNamePattern = "")
        {
            this.StartIndex = startIndex;
            this.InputLength = inputLength;
            this.OutputFilePath = Path.Combine(outputFileFolder,
                (!StringHelper.IsNullOrWhiteSpace(outputFileNamePattern) ? outputFileNamePattern : Guid.NewGuid().ToString()));
            this.inputFilePath = inputFilePath;
        }


        public void Compress()
        {
            FileStream inFile = null;
            try
            {
                inFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                inFile.Position = this.StartIndex;

                outputFile = new FileStream(this.OutputFilePath, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.None);

                var tmpMemStr = new MemoryStream();

                using (GZipStream compressedzipStream = new GZipStream(tmpMemStr, CompressionMode.Compress, true))
                {
                    byte[] tmpBuffer = new byte[this.InputLength];

                    inFile.Read(tmpBuffer, 0, tmpBuffer.Length);

                    compressedzipStream.Write(tmpBuffer, 0, tmpBuffer.Length);
                    compressedzipStream.Close();


                    tmpMemStr.Position = 0;

                    var newTmpBuffer = new byte[tmpMemStr.Length];
                    tmpMemStr.Read(newTmpBuffer, 0, newTmpBuffer.Length);

                    outputFile.Write(newTmpBuffer, 0, newTmpBuffer.Length);
                    outputFile.Flush();
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Упаковка фрагмента {0}-{1}", this.StartIndex, this.StartIndex + this.InputLength),
                    exception);
            }
            finally
            {
                if (inFile != null)
                {
                    inFile.Close();
                }
            }
        }


        public byte[] GetOutputBuffer(bool releaseLock = true)
        {
            byte[] result = new byte[this.outputFile.Length];

            this.outputFile.Position = 0;

            this.outputFile.Read(result, 0, result.Length);

            if (releaseLock)
            {
                this.ReleaseLock();
            }

            return result;
        }

        private void ReleaseLock()
        {
            if (this.outputFile != null)
            {
                this.outputFile.Flush();
                this.outputFile.Close();
            }

            File.Delete(this.OutputFilePath);
        }

        public void Dispose()
        {
            this.ReleaseLock();
        }
    }
}
