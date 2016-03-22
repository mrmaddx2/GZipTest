using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Класс-декоратор для предоставления расширенного функционала
    /// </summary>
    public class StatusedPieceOfResult : PieceOfResult, IStatusedPieceOfResult
    {
        public StatusedPieceOfResult(IPieceOfSource pieceOfSource) : base(pieceOfSource)
        {
            this.Status = PieceOfResultStatusEnum.Unknown;
        }

        public PieceOfResultStatusEnum Status { get; set; }

        public override void Compress()
        {
            base.Compress();
            this.Status = PieceOfResultStatusEnum.Ready;
        }

        public override void Decompress()
        {
            base.Decompress();
            this.Status = PieceOfResultStatusEnum.Ready;
        }
    }
}
