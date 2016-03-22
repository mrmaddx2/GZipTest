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
        public virtual long Length
        {
            get
            {
                if (this.Body != null)
                {
                    return this.Body.Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        public ProjectFile(string inputFilePath)
        {
            
        }

        public virtual ProjectFileTypeEnum FileType
        {
            get { return ProjectFileTypeEnum.Unknown; }
            protected set { }
        }

        public virtual ProjectStatusEnum Status
        {
            get
            {
                if (this.Body != null)
                {
                    if (this.Body.Position == this.Body.Length)
                    {
                        return ProjectStatusEnum.Done;
                    }
                }

                return ProjectStatusEnum.Unknown;
            }
        }

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
