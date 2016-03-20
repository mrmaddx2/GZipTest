using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Интерфейс-декоратор для <see cref="IPieceOfResult"/>
    /// </summary>
    public interface IExtendedPieceOfResult : IPieceOfResult
    {
        /// <summary>
        /// Статус кусочка
        /// </summary>
        ExtendedPieceOfResultStatus Status { get; set; }
        /// <summary>
        /// Позиция в источнике, на которой заканчивается область проассоциированная с данным экземпляром
        /// </summary>
        long InputEndIndex { get; }
    }
}
