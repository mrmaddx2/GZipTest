using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    public class MrConverter : MrAbstractBlock
    {
        #region Delegates
        protected delegate void ConvertPiecesActionHandler(
            AsyncOperation operation);
        #endregion

        

        private readonly CompressionMode mode;

        public MrConverter(CompressionMode inputMode)
        {
            this.mode = inputMode;
        }


        static readonly uint MaxThreads;
        static MrConverter()
        {
            //В дальнейшем будем создавать по потоку на процессор
            MaxThreads = ProcessInfo.NumberOfProcessorThreads;
        }

        protected override void Start()
        {
            this.DoMainWork(BlockBody, MaxThreads);
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
                using (GZipStream compressedzipStream = new GZipStream(PieceOfSource.GetBodyStream(true), CompressionMode.Decompress, true))
                {
                    byte[] buffer = new byte[512000];
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

        private void BlockBody()
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            while (this.Status == ProjectStatusEnum.InProgress)
            {
                var source = this.ReadFromSourcesSingle();
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
    }
}
