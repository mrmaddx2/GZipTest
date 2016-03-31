using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class PieceOf : IDisposable
    {

        private MemoryStream Body { get; set; }

        public uint SeqNo { get; protected set; }
        public decimal PercentOfSource { get; set; }

        public ulong Length()
        {
            return (ulong) this.Body.Length;
        }

        public PieceOf(int seqNo)
        {
            this.SeqNo = (uint)seqNo;
            this.ResetBody(new MemoryStream());
        }

        /// <summary>
        /// Служит для получения результатов работы/>
        /// </summary>
        /// <param name="cleanBodyAfter">Снимать ли лок с результата</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком источника работы</returns>
        public byte[] GetBodyBuffer(bool cleanBodyAfter)
        {
            var result = this.Body.ToArray();

            if (cleanBodyAfter)
            {
                ReleaseResources();
            }

            return result;
        }

        private void ReleaseResources()
        {
            if (this.Body != null)
            {
                this.Body.Dispose();
                this.Body = null;
            }
        }

        
        public void AddToBody(byte[] value)
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

        public void Dispose()
        {
            this.ReleaseResources();
        }

        public override int GetHashCode()
        {
            return (int)this.SeqNo;
        }
    }
}
