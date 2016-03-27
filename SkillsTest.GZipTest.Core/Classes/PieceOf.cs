using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class PieceOf
    {
        //public PieceOfResultStatusEnum Status { get; set; }

        protected MemoryStream Body { get; set; }

        public int SeqNo { get; protected set; }
        public decimal PercentOfSource { get; set; }

        public virtual long Length()
        {
            return this.Body.Length;
        }

        public PieceOf(int seqNo)
        {
            this.Body = new MemoryStream();
            this.SeqNo = seqNo;
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
                if (this.Body == null)
                {
                    ResetBody(new MemoryStream());
                }

                this.Body.Position = this.Body.Length;
                this.Body.Write(value, offset, count);
            }
            catch (Exception exception)
            {
                throw new Exception(
                    string.Format("Добавление данных к телу кусочка {0}", this.SeqNo),
                    exception);
            }
        }

        public void ResetBody(MemoryStream value)
        {
            this.Body = value;

            this.Body.Position = 0;
        }

        public MemoryStream GetBodyStream(bool cleanBodyAfter)
        {
            return new MemoryStream(GetBodyBuffer(cleanBodyAfter)) {Position = 0};
        }

        public virtual void Dispose()
        {
            this.ReleaseResources();
        }

        public override int GetHashCode()
        {
            return this.SeqNo;
        }

        public override bool Equals(object obj)
        {
            return obj is PieceOf && (obj as PieceOf).SeqNo == this.SeqNo;
        }
    }
}
