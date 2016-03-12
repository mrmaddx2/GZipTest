using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class MrZipper : IDisposable
    {
        private static long compressFragmentSize = 512000;

        private static int maxThreads = 5;

        private long currentIndex { get; set; }
        private readonly object _indexDummy = new object();
        private long inputFileLength;

        private string inputFilePath { get; set; }
        private string outputFilePath { get; set; }
        private string outputFileFolder { get { return Path.GetDirectoryName(this.outputFilePath); } }

        private List<PieceOfResult> ResultList = new List<PieceOfResult>();
        private CompressionMode mode;
        private FileStream inputFile;


        private PieceOfResult Fetch()
        {
            long? localCurrentIndex = null;
            long? localNextIndex = null;

            PieceOfResult result = null;

            lock (this._indexDummy)
            {
                if (!(currentIndex >= inputFileLength))
                {
                    localCurrentIndex = currentIndex;

                    if (this.mode == CompressionMode.Compress)
                    {
                        localNextIndex = localCurrentIndex + compressFragmentSize;
                    }
                    else
                    {
                    //TODO: Надо бы для процесса распаковки определять динамически размеры блоков
                        throw new NotImplementedException();
                    }

                    if (localNextIndex > inputFileLength)
                    {
                        localNextIndex = inputFileLength;
                    }

                    currentIndex = (long)localNextIndex;
                }
            }

            if (localCurrentIndex != null && localNextIndex != null)
            {
                result = new PieceOfResult(this.inputFilePath, (long) localCurrentIndex,
                    Convert.ToInt64(localNextIndex - localCurrentIndex), this.outputFileFolder);
            }

            return result;
        }


        private void Refresh(string inputFilePath, string outputFilePath, CompressionMode mode)
        {
            lock (this._indexDummy)
            {
                this.outputFilePath = outputFilePath;
                this.inputFilePath = inputFilePath;
                this.mode = mode;

                lock (this.ResultList)
                {
                    this.ResultList.Clear();
                }

                this.inputFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                this.inputFileLength = inputFile.Length;
                this.inputFile.Flush();

                this.currentIndex = 0;
            }
        }

        public void Compress(string inputFilePath, string outputFilePath)
        {
            this.Refresh(inputFilePath, outputFilePath, CompressionMode.Compress);

            var newPiece = this.Fetch();
            while (newPiece != null)
            {
                newPiece.Compress();

                this.ResultList.Add(newPiece);

                newPiece = this.Fetch();
            }

            this.WriteResult();
        }

        private void WriteResult()
        {
            FileStream targetFile = null;
            try
            {
                targetFile = new FileStream(this.outputFilePath, FileMode.Create, FileAccess.Write);

                var pieces =
                    this.ResultList;

                foreach (
                    var currentPiece in
                        pieces)
                {
                    var tmpBuffer = currentPiece.GetOutputBuffer();

                    targetFile.Write(tmpBuffer, 0, tmpBuffer.Length);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Запись в файл {0}", this.outputFilePath), exception);
            }
            finally
            {
                if (targetFile != null)
                {
                    targetFile.Close();
                }
            }
        }

        public void Dispose()
        {
            if (this.inputFile != null)
            {
                this.inputFile.Flush();
                this.inputFile.Close();
            }
        }
    }
}
