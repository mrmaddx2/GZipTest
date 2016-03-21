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
    public class PieceOfResult : IPieceOfResult
    {
        #region Public
        /// <summary>
        /// Позиция в исходном файле, с которой начинается область проассоциированная с данным экземпляром
        /// </summary>
        public long InputStartIndex { get; private set; }
        /// <summary>
        /// Продолжительность проассоциированной с данным экземпляром области исходного файла
        /// </summary>
        public long InputLength { get; private set; }
        #endregion

        #region Private
        
        /// <summary>
        /// Путь к исходному файлу
        /// </summary>
        protected virtual string inputFilePath { get; set; }
        /// <summary>
        /// Внутренний буфер с результатом работы методов <see cref="Compress"/>/<see cref="Decompress"/>
        /// </summary>
        protected virtual byte[] result { get; set; }
        #endregion

        

        /// <param name="inputFilePath">Путь к исходному файлу</param>
        /// <param name="inputStartIndex">Позиция в исходном файле, с которой начинается область проассоциированная с данным экземпляром</param>
        /// <param name="inputLength">Продолжительность проассоциированной с данным экземпляром области исходного файла</param>
        public PieceOfResult(string inputFilePath, long inputStartIndex, long inputLength)
        {
            this.InputStartIndex = inputStartIndex;
            this.InputLength = inputLength;
            this.inputFilePath = inputFilePath;
        }


        /// <param name="inputFilePath">Путь к исходному файлу</param>
        /// <param name="pieceOfSource">Структура, описывающая положение области проассоциированной с данным экземпляром</param>
        public PieceOfResult(string inputFilePath, PieceOfSource pieceOfSource)
            : this(
                inputFilePath, pieceOfSource.StartIndex, pieceOfSource.Length)
        {

        }


        /// <summary> 
        /// Упаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        public virtual void Compress()
        {
            FileStream inFile = null;
            try
            {
                inFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                inFile.Position = this.InputStartIndex;

                using (var tmpMemStr = new MemoryStream())
                {
                    using (GZipStream compressedzipStream = new GZipStream(tmpMemStr, CompressionMode.Compress, true))
                    {
                        byte[] tmpBuffer = new byte[this.InputLength];

                        inFile.Read(tmpBuffer, 0, tmpBuffer.Length);

                        compressedzipStream.Write(tmpBuffer, 0, tmpBuffer.Length);
                        compressedzipStream.Close();

                        this.result = tmpMemStr.ToArray();
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Упаковка фрагмента {0}-{1}", this.InputStartIndex, this.InputStartIndex + this.InputLength),
                    exception);
            }
            finally
            {
                if (inFile != null && inFile.CanRead)
                {
                    inFile.Flush();
                    inFile.Close();
                }
            }
        }


        /// <summary> 
        /// Распаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
        public virtual void Decompress()
        {
            FileStream inFile = null;
            try
            {
                inFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                inFile.Position = this.InputStartIndex;

                using (var resultStream = new MemoryStream())
                {
                    using (var tmpMemStream = new MemoryStream())
                    {
                        StreamHelper.CopyTo(inFile, tmpMemStream, this.InputLength);
                        tmpMemStream.Position = 0;

                        using (GZipStream zipStream = new GZipStream(tmpMemStream, CompressionMode.Decompress, true))
                        {
                            byte[] buffer = new byte[this.InputLength];
                            int nRead;
                            while ((nRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                resultStream.Write(buffer, 0, nRead);
                            }

                            zipStream.Close();

                            this.result = resultStream.ToArray();
                        }
                    }
                }

                
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Распаковка фрагмента {0}-{1}", this.InputStartIndex, this.InputStartIndex + this.InputLength),
                    exception);
            }
            finally
            {
                if (inFile != null && inFile.CanRead)
                {
                    inFile.Flush();
                    inFile.Close();
                }
            }
        }


        /// <summary>
        /// Служит для получения результатов работы методов <see cref="Compress"/> и <see cref="Decompress"/>
        /// </summary>
        /// <param name="releaseLock">Очищать ли внутренний буфер с результатом</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком файла работы</returns>
        public virtual byte[] GetOutputBuffer(bool releaseLock = true)
        {
            var localResult = this.result;

            if (releaseLock)
            {
                ReleaseLock();
            }

            return localResult;
        }

        /// <summary>
        /// Очищает внутренний буфер с результатом
        /// </summary>
        protected virtual void ReleaseLock()
        {
            this.result = null;
        }

        public virtual void Dispose()
        {
            this.ReleaseLock();
        }
    }
}
