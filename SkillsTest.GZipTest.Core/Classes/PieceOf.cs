using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public abstract class PieceOf : IPieceOf
    {
        protected virtual MemoryStream Body { get; set; }

        public long StartIndex { get; protected set; }

        public virtual long Length
        {
            get
            {
                if (this.Body != null && Body.CanRead)
                {
                    return this.Body.Length;
                }
                else
                {
                    return 0;
                }
            }
            protected set { }
        }

        public virtual long EndIndex
        {
            get { return this.StartIndex + this.Length; }
            protected set { }
        }

        protected PieceOf(long startIndex)
        {
            this.Body = new MemoryStream();
            this.StartIndex = startIndex;
        }

        /// <summary>
        /// Служит для получения результатов работы/>
        /// </summary>
        /// <param name="cleanBodyAfter">Снимать ли лок с результата</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком источника работы</returns>
        public virtual byte[] GetBodyBuffer(bool cleanBodyAfter)
        {
            var result = this.Body.ToArray();

            if (cleanBodyAfter)
            {
                ReleaseResources();
            }

            return result;
        }

        protected virtual void ReleaseResources()
        {
            if (this.Body != null)
            {
                this.Body.Dispose();
                this.Body = null;
            }
        }

        
        public virtual void AddToBody(byte[] value)
        {
            this.AddToBody(value, 0, value.Length);
        }

        public void AddToBody(byte[] value, int offset, int count)
        {
            try
            {
                this.Body.Position = this.Body.Length;
                this.Body.Write(value, offset, count);
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Добавление данных к результату кусочка {0}-{1}", this.StartIndex, this.EndIndex),
                    exception);
            }
        }

        public MemoryStream GetBodyStream(bool cleanBodyAfter)
        {
            var result = new MemoryStream(this.Body.ToArray());

            if (cleanBodyAfter)
            {
                ReleaseResources();
            }

            return result;
        }

        public virtual void Dispose()
        {
            this.ReleaseResources();
        }
    }
}
