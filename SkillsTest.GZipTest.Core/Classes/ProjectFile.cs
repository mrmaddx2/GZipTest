using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public abstract class ProjectFile : IProjectFile
    {
        //protected readonly BufferedStream Body;

        /// <summary>
        /// Поток с телом файла.
        /// Блокируем файл на время существования экземпляра.
        /// </summary>
        protected FileStream Body { get; set; }
        /// <summary>
        /// Длина потока
        /// </summary>
        /// <returns></returns>
        public long Length()
        {
            return this.Body.Length;
        }

        protected readonly int ClusterSize;

        public ProjectFile(string inputFilePath, FileMode mode, FileAccess access, FileShare share)
        {
            this.CurrentSeqNo = 0;

            this.ClusterSize = ProcessInfo.GetClusterSize(inputFilePath);

            this.Body = new FileStream(inputFilePath, mode, access, share, this.ClusterSize);
            //this.Body = new BufferedStream(this.Body, ProcessInfo.GetClusterSize(inputFilePath));
        }

        /// <summary>
        /// Порядковый номер находящегося в обработке фрагмента данных
        /// </summary>
        protected long CurrentSeqNo;

        public virtual void Dispose()
        {
            if (this.Body != null)
            {
                if (this.Body.CanRead || this.Body.CanWrite || this.Body.CanSeek)
                {
                    this.Body.Close();
                }
                this.Body.Dispose();
                this.Body = null;
            }
        }
    }
}
