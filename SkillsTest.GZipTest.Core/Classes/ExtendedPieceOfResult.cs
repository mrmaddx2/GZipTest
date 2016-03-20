using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Класс-декоратор для предоставления расширенного функционала
    /// </summary>
    public class ExtendedPieceOfResult : IExtendedPieceOfResult
    {
        public long InputEndIndex { get { return this.InputStartIndex + this.InputLength; } }

        public ExtendedPieceOfResultStatus Status { get; set; }

        protected IPieceOfResult pieceOfResult { get; set; }

        public ExtendedPieceOfResult(IPieceOfResult inputPieceOfResult)
        {
            this.pieceOfResult = inputPieceOfResult;
            if (this.pieceOfResult.GetOutputBuffer(false).Length > 0)
            {
                this.Status = ExtendedPieceOfResultStatus.Ready;
            }
            else
            {
                this.Status = ExtendedPieceOfResultStatus.Unknown;
            }
            
        }

        public long InputStartIndex
        {
            get { return this.pieceOfResult.InputStartIndex; }
        }

        public long InputLength
        {
            get { return this.pieceOfResult.InputLength; }
        }

        public virtual void Compress()
        {
            throw new NotImplementedException();
        }

        public virtual void Decompress()
        {
            throw new NotImplementedException();
        }

        public virtual byte[] GetOutputBuffer(bool releaseLock = true)
        {
            return this.pieceOfResult.GetOutputBuffer(releaseLock);
        }

        public virtual void Dispose()
        {
            if (this.pieceOfResult != null)
            {
                this.pieceOfResult.Dispose();
            }
        }
    }
}
