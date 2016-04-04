using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class PieceOf : IDisposable
    {

        private byte[] Body { get; set; }

        public long SeqNo { get; protected set; }
        public decimal PercentOfSource { get; set; }

        public ulong Length()
        {
            return (ulong) (Body == null ? 0 : Body.Length);
        }

        public PieceOf(long seqNo)
        {
            this.SeqNo = seqNo;
            this.ResetBody(new MemoryStream());
        }

        /// <summary>
        /// Служит для получения результатов работы/>
        /// </summary>
        /// <param name="cleanBodyAfter">Снимать ли лок с результата</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком источника работы</returns>
        public byte[] GetBodyBuffer(bool cleanBodyAfter)
        {
            if (this.Body == null)
            {
                this.ResetBody(new MemoryStream());
            }

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
                this.Body = null;
            }
        }

        public void ResetBody(MemoryStream value)
        {
            this.Body = value.ToArray();
        }

        public void ResetBody(byte[] value)
        {
            this.Body = value.ToArray();
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
