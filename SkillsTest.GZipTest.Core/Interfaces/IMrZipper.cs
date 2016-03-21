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

        /// <summary>Сжатие файла источника и заись результата на диск</summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="compressFragmentSize">Размер фрагмента данных</param>
        void Compress(string inputFilePath, string outputFilePath, long? compressFragmentSize);
        /// <summary>
        /// Асинхронный вариант метода <see cref="Compress"/>
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        /// <param name="compressFragmentSize">Размер фрагмента данных</param>
        void CompressAsync(string inputFilePath, string outputFilePath, long? compressFragmentSize);
        /// <summary>Распаковка файла и запись результатан а диск</summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        void Decompress(string inputFilePath, string outputFilePath);
        /// <summary>
        /// Асинхронный вариант метода <see cref="Decompress"/>
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу-источнику</param>
        /// <param name="outputFilePath">Путь к файлу-результату</param>
        void DecompressAsync(string inputFilePath, string outputFilePath);
        /// <summary>
        /// Отменить выполнение текущей асинхронной операции
        /// </summary>
        void CancelConvertAsync();
    }
}
