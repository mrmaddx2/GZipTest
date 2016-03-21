using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Интерфейс-декоратор для <see cref="IPieceOfResult"/>
    /// </summary>
    public interface IStatusedPieceOfResult : IPieceOfResult
    {
        /// <summary>
        /// Статус кусочка
        /// </summary>
        PieceOfResultStatusEnum Status { get; set; }
    }
}
