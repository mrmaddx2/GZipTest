using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Отвечает за сжатие и распаковку данных с помощью класса GZipStream
    /// </summary>
    public sealed class MrZipper
    {
        #region Events
        public event ProgressChangedEventHandler ProgressChanged;
        public event ConvertEventHandler ConvertAsyncCompleted;
        #endregion

        #region Properties & fields       

        #endregion

        public MrZipper()
        {
        }

        /// <summary>
        /// Отменить выполнение текущей асинхронной операции
        /// </summary>
        public void CancelConvertAsync()
        {
            
        }

        /// <summary>
        /// Оповещает подписчиков асинхроных операций
        /// </summary>
        /// <param name="e">Информация о результатах работы</param>
        private void OnConvertAsyncCompleted(AsyncCompletedEventArgs e)
        {
            var handler = ConvertAsyncCompleted;
            if (handler != null)
            {
                handler(e);
            }
        }

        private void OnProgressChanged(ConvertProgressChangedEventArgs e)
        {
            var handler = ProgressChanged;
            if (handler != null)
            {
                handler(e);
            }
        }


        public void ConvertAsync(string inputFilePath, string outputFilePath, CompressionMode mode, long? compressFragmentSize = null)
        {
            var source = new MrSource();
            var action = new MrConverter(mode);
            var result = new MrTarget(outputFilePath);
            var dispatcher = new MrsDispatcher();
            dispatcher.Completed += DispatcherOnCompleted;

            dispatcher.AddSource(source);
            dispatcher.AddSource(action);

            source.AddTarget(action);
            action.AddTarget(result);

            result.ConvertAsyncCompleted += ResultOnConvertAsyncCompleted;
            result.ProgressChanged += OnProgressChanged;

            source.Post(inputFilePath);
        }

        private void DispatcherOnCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.OnConvertAsyncCompleted(e);
        }

        private void ResultOnConvertAsyncCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.OnConvertAsyncCompleted(e);
        }
    }
}
