using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public abstract class ProjectFile : IProjectFile
    {
        protected readonly BufferedStream Body;

        /// <summary>
        /// Поток с телом файла.
        /// Блокируем файл на время существования экземпляра.
        /// </summary>
        private FileStream BodyStream { get; set; }
        /// <summary>
        /// Длина потока
        /// </summary>
        /// <returns></returns>
        public long Length()
        {
            return this.BodyStream.Length;
        }

        public ProjectFile(string inputFilePath, FileMode mode, FileAccess access, FileShare share)
        {
            this.CurrentSeqNo = 0;

            var filePath = Path.GetDirectoryName(inputFilePath);

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            this.BodyStream = new FileStream(inputFilePath, mode, access, share);
            this.Body = new BufferedStream(this.BodyStream);
        }

        /// <summary>
        /// Порядковый номер находящегося в обработке фрагмента данных
        /// </summary>
        protected long CurrentSeqNo;

        public virtual void Dispose()
        {
            if (this.BodyStream != null)
            {
                if (this.BodyStream.CanRead || this.BodyStream.CanWrite || this.BodyStream.CanSeek)
                {
                    this.BodyStream.Close();
                }
                this.BodyStream.Dispose();
                this.BodyStream = null;
            }
        }
    }
}
