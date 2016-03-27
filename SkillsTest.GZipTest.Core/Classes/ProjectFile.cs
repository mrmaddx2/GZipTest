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
            
        }

        public abstract ProjectFileTypeEnum FileType { get; protected set; }

        public virtual void Dispose()
        {
            if (this.Body != null)
            {
                this.Body.Dispose();
                this.Body = null;
            }
        }
    }
}
