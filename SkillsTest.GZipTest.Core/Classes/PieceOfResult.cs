using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Вспомогательный класс для упаковки фрагмента исходного файла с помощью класса GZipStream
    /// </summary>
    public class PieceOfResult : PieceOf, IPieceOfResult
    {
        #region Fields & Properties
        protected IPieceOfSource PieceOfSource { get; set; }

        public override long EndIndex { get; protected set; }

        #endregion


        /// <param name="pieceOfSource">Кусочек файла-источника</param>
        public PieceOfResult(IPieceOfSource pieceOfSource)
            : base((pieceOfSource == null ? 0 : pieceOfSource.StartIndex))
        {
            if (pieceOfSource == null || pieceOfSource.Length <= 0)
            {
                throw new ArgumentException("Кусочек источника должен иметь ненулевую длину", "pieceOfSource");
            }

            this.EndIndex = pieceOfSource.EndIndex;
            this.PieceOfSource = pieceOfSource;
        }


        /// <summary> 
        /// Упаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        public virtual void Compress()
        {
            try
            {
                if (this.Body.Length > 0)
                {
                    throw new Exception("При старте операции упаковки внутренний буфер должен быть пуст");
                }
                else
                {
                    using (GZipStream compressedzipStream = new GZipStream(this.Body, CompressionMode.Compress, true))
                    {
                        byte[] tmpBuffer = this.PieceOfSource.GetBodyBuffer(true);

                        compressedzipStream.Write(tmpBuffer, 0, tmpBuffer.Length);
                        compressedzipStream.Close();
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Упаковка фрагмента {0}-{1}", this.StartIndex, this.EndIndex),
                    exception);
            }
        }


        /// <summary> 
        /// Распаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        public virtual void Decompress()
        {
            try
            {
                if (this.Body.Length > 0)
                {
                    throw new Exception("При старте операции распаковки внутренний буфер должен быть пуст");
                }
                else
                {
                    var sourceStream = this.PieceOfSource.GetBodyStream(true);
                    sourceStream.Position = 0;

                    using (GZipStream compressedzipStream = new GZipStream(sourceStream, CompressionMode.Decompress, true))
                    {
                        byte[] buffer = new byte[InputFile.DefaultFragmentSize];
                        int nRead;
                        while ((nRead = compressedzipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            this.Body.Write(buffer, 0, nRead);
                        }

                        compressedzipStream.Close();
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Распаковка фрагмента {0}-{1}", this.StartIndex, this.StartIndex + this.Length),
                    exception);
            }
        }
    }
}
