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
        private readonly CompressionMode mode;

        public MrConverter(CompressionMode inputMode)
        {
            this.mode = inputMode;
            this.MaxThreads = ProcessInfo.NumberOfProcessorThreads;
        }


        /// <summary> 
        /// Упаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        protected virtual void Compress(PieceOf PieceOfSource)
        {
            try
            {
                using(var tmpBodyStream = new MemoryStream())
                using (GZipStream compressedzipStream = new GZipStream(tmpBodyStream, CompressionMode.Compress, true))
                {
                    byte[] tmpBuffer = PieceOfSource.GetBodyBuffer(true);
                    compressedzipStream.Write(tmpBuffer, 0, tmpBuffer.Length);
                    compressedzipStream.Close();

                    PieceOfSource.ResetBody(tmpBodyStream);

                    tmpBodyStream.Close();
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
                using (var sourceStream = PieceOfSource.GetBodyStream(true))
                using (var decompressedStream = new MemoryStream())
                {
                    using (GZipStream compressedzipStream = new GZipStream(sourceStream, CompressionMode.Decompress, false))
                    {
                        byte[] buffer = new byte[512000];
                        int nRead;
                        while ((nRead = compressedzipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            decompressedStream.Write(buffer, 0, nRead);
                        }

                        compressedzipStream.Flush();

                        PieceOfSource.ResetBody(decompressedStream);

                        if (PieceOfSource.Length() == 0)
                        {
                            //TODO: очень странная проблема. На выходе упаковщик не отдает ничего
                            var a = 1;
                        }

                        compressedzipStream.Close();
                        decompressedStream.Close();
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Распаковка фрагмента {0}", PieceOfSource.SeqNo),
                    exception);
            }
        }

        
        protected override void MainAction()
        {
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

            while (this.Status == ProjectStatusEnum.InProgress)
            {
                PieceOf source = this.ReadFromSourcesSingle();
                if (source != null)
                {
                    switch (mode)
                    {
                        case CompressionMode.Compress:
                            this.Compress((PieceOf)source);
                            break;
                        case CompressionMode.Decompress:
                            this.Decompress((PieceOf)source);
                            break;
                    }

                    this.AddToBuffer((PieceOf)source);
                }

                if (source == null)
                {
                    if (this.PostDone() != ProjectStatusEnum.Done)
                    {
                        Thread.Sleep(this.SleepTime);
                    }
                }
            }
        }
    }
}
