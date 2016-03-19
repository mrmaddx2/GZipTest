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
    public class PieceOfResult : IDisposable
    {
        #region Public
        /// <summary>
        /// Позиция в исходном файле, с которой начинается область проассоциированная с данным экземпляром
        /// </summary>
        public long StartIndex { get; private set; }
        #endregion

        #region Private
        /// <summary>
        /// Продолжительность проассоциированной с данным экземпляром области исходного файла
        /// </summary>
        private long InputLength;
        /// <summary>
        /// Путь к файлу-результату
        /// </summary>
        private string OutputFilePath;
        /// <summary>
        /// Поток файла-результата
        /// </summary>
        /// <remarks>Поток остается открыт на протяжении работы операций сжатия и распаковки. Это необходимо для защиты модификации файла во время работы методов.</remarks>
        private FileStream outputFile;
        /// <summary>
        /// Путь к исходному файлу
        /// </summary>
        private string inputFilePath;
        #endregion

        

        /// <param name="inputFilePath">Путь к исходному файлу</param>
        /// <param name="startIndex">Позиция в исходном файле, с которой начинается область проассоциированная с данным экземпляром</param>
        /// <param name="inputLength">Продолжительность проассоциированной с данным экземпляром области исходного файла</param>
        /// <param name="outputFileFolder">Путь к папке файла-результата</param>
        /// <param name="outputFileNamePattern">Шаблон наименования для файла-результата. По умолчанию будет сгенерировано уникальноме имя.</param>
        public PieceOfResult(string inputFilePath, long startIndex, long inputLength, string outputFileFolder, string outputFileNamePattern = "")
        {
            this.StartIndex = startIndex;
            this.InputLength = inputLength;
            this.OutputFilePath = Path.Combine(outputFileFolder,
                (!StringHelper.IsNullOrWhiteSpace(outputFileNamePattern) ? outputFileNamePattern : Guid.NewGuid().ToString()));
            this.inputFilePath = inputFilePath;
        }


        /// <param name="inputFilePath">Путь к исходному файлу</param>
        /// <param name="pieceOfSource">Структура, описывающая положение области проассоциированной с данным экземпляром</param>
        /// <param name="outputFileFolder">Путь к папке файла-результата</param>
        /// <param name="outputFileNamePattern">Шаблон наименования для файла-результата. По умолчанию будет сгенерировано уникальноме имя.</param>
        public PieceOfResult(string inputFilePath, PieceOfSource pieceOfSource, string outputFileFolder,
            string outputFileNamePattern = "")
            : this(
                inputFilePath, pieceOfSource.StartIndex, pieceOfSource.Length, outputFileFolder, outputFileNamePattern)
        {

        }


        /// <summary> 
        /// Упаковывает связанный с данным классом фрагмент файла и записывает результат в новый файл на диск.
        /// </summary>
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
        public void Decompress()
        {
            FileStream inFile = null;
            MemoryStream tmpMemStream = new MemoryStream();
            try
            {
                inFile = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                inFile.Position = this.StartIndex;

                StreamHelper.CopyTo(inFile, tmpMemStream, this.InputLength);
                tmpMemStream.Position = 0;

                outputFile = new FileStream(this.OutputFilePath, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.Read);

                using (GZipStream zipStream = new GZipStream(tmpMemStream, CompressionMode.Decompress, true))
                {
                    byte[] buffer = new byte[this.InputLength];
                    int nRead;
                    while ((nRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputFile.Write(buffer, 0, nRead);
                    }

                    zipStream.Close();
                }                
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Распаковка фрагмента {0}-{1}", this.StartIndex, this.StartIndex + this.InputLength),
                    exception);
            }
            finally
            {
                if (inFile != null && inFile.CanRead)
                {
                    inFile.Flush();
                    inFile.Close();
                }

                if (tmpMemStream != null)
                {
                    tmpMemStream.Close();
                }
            }
        }


        /// <summary>
        /// Служит для получения результатов работы методов <see cref="Compress"/> и <see cref="Decompress"/>
        /// </summary>
        /// <param name="releaseLock">Снимать ли лок с файла-результата (так же будет произведено удаление)</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком файла работы</returns>
        public byte[] GetOutputBuffer(bool releaseLock = true)
        {
            byte[] result = new byte[this.outputFile.Length];

            this.outputFile.Position = 0;

            this.outputFile.Read(result, 0, result.Length);

            if (releaseLock)
            {
                //TODO : вынести в отдельный поток
                this.ReleaseLock();
            }

            return result;
        }

        /// <summary>
        /// Освобождает поток, связанный с файлом-результатом. Удаляет файл-результат
        /// </summary>
        private void ReleaseLock()
        {
            if (this.outputFile != null && this.outputFile.CanRead)
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
