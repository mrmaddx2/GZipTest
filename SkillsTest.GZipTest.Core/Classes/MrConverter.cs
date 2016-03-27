using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class MrConverter : MrAbstractBlock
    {
        #region Delegates
        protected delegate void ConvertPiecesActionHandler(
            CompressionMode compressionMode, long? fragmentSize = null);
        #endregion

        public MrConverter(CompressionMode mode, long? compressFragmentSize = null)
        {
            ConvertAsync(mode, compressFragmentSize);
        }


        static uint MaxThreads;
        static MrConverter()
        {
            //В дальнейшем будем создавать по потоку на процессор
            MaxThreads = ProcessInfo.NumberOfProcessorThreads;
        }

        private readonly object convertAsyncDummy = new object();
        protected void ConvertAsync(CompressionMode mode, long? compressFragmentSize = null)
        {
            try
            {
                lock (convertAsyncDummy)
                {
                    this.Status = ProjectStatusEnum.InProgress;

                    for (int i = 0; i <= MaxThreads - 1; i++)
                    {
                        ConvertPiecesActionHandler convertPiecesAction = ConvertPieces;
                        convertPiecesAction.BeginInvoke(
                            mode,
                            compressFragmentSize,
                            null,
                            null);
                    }
                }
            }
            catch (Exception exception)
            {
                this.PostError(exception);
            }

            
        }

        /// <summary> 
        /// Упаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        protected virtual void Compress(PieceOf PieceOfSource)
        {
            try
            {
                var tmpBodyStream = new MemoryStream();
                using (GZipStream compressedzipStream = new GZipStream(tmpBodyStream, CompressionMode.Compress, true))
                {
                    byte[] tmpBuffer = PieceOfSource.GetBodyBuffer(true);
                    compressedzipStream.Write(tmpBuffer, 0, tmpBuffer.Length);
                    compressedzipStream.Close();
                    PieceOfSource.ResetBody(tmpBodyStream);
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Упаковка фрагмента {0}", PieceOfSource.SeqNo),
                    exception);
            }
        }


        /// <summary> 
        /// Распаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        protected virtual void Decompress(PieceOf PieceOfSource)
        {
            try
            {
                var sourceStream = PieceOfSource.GetBodyStream(true);

                using (GZipStream compressedzipStream = new GZipStream(sourceStream, CompressionMode.Decompress, true))
                {
                    byte[] buffer = new byte[InputFile.DefaultFragmentSize];
                    int nRead;
                    while ((nRead = compressedzipStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        PieceOfSource.AddToBody(buffer, 0, nRead);
                    }

                    compressedzipStream.Close();
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Распаковка фрагмента {0}", PieceOfSource.SeqNo),
                    exception);
            }
        }

        private void ConvertPieces(CompressionMode mode, long? compressFragmentSize = null)
        {
            try
            {
                while (this.Status == ProjectStatusEnum.InProgress)
                {
                    PieceOf source = null;
                    foreach (var current in sources)
                    {
                        source = current.Receive(1).FirstOrDefault();
                        if (source != null)
                        {
                            switch (mode)
                            {
                                case CompressionMode.Compress:
                                    this.Compress(source);
                                    break;
                                case CompressionMode.Decompress:
                                    this.Decompress(source);
                                    break;
                            }

                            this.AddToBuffer(source);
                            break;
                        }
                    }

                    if (source == null)
                    {
                        if (this.PostDone() != ProjectStatusEnum.Done)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                this.PostError(exception);
            }
        }
    }
}
