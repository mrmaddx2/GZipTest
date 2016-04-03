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
        private FileStream HiddenBody { get; set; }
        public long Length()
        {
            return this.HiddenBody.Length;
        }

        public ProjectFile(string inputFilePath, FileMode mode, FileAccess access, FileShare share)
        {
            this.CurrentSeqNo = 0;
            this.HiddenBody = new FileStream(inputFilePath, mode, access, share);

            this.Body = new BufferedStream(this.HiddenBody);

            
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
            }

            if (this.HiddenBody != null)
            {
                if (this.HiddenBody.CanRead || this.HiddenBody.CanWrite || this.HiddenBody.CanSeek)
                {
                    this.HiddenBody.Close();
                }
                this.HiddenBody.Dispose();
                this.HiddenBody = null;
            }
        }
    }
}
