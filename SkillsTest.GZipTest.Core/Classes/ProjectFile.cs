using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public abstract class ProjectFile : IProjectFile
    {
        /// <summary>
        /// Поток с телом файла.
        /// Блокируем файл на время существования экземпляра.
        /// </summary>
        protected virtual FileStream Body { get; set; }
        public long Length()
        {
            return this.Body.Length;
        }

        public ProjectFile(string inputFilePath)
        {
            this.CurrentSeqNo = 0;
        }

        public abstract ProjectFileTypeEnum FileType { get; protected set; }

        private readonly object currentSeqNoDummy = new object();
        private long currentSeqNo;
        protected long CurrentSeqNo;
            /*
        {
            get
            {
                lock (currentSeqNoDummy)
                {
                    return this.currentSeqNo;
                }
            }
            protected set
            {
                lock (currentSeqNoDummy)
                {
                    this.currentSeqNo = value;
                }
            }
        }*/

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
