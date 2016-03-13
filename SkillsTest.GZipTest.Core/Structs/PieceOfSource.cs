using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Наметка в исходном файле
    /// </summary>
    public struct PieceOfSource
    {
        /// <summary>
        /// Позиция в исходном файле, с которой начинается область проассоциированная с данной структурой
        /// </summary>
        public long StartIndex { get; private set; }
        /// <summary>
        /// Продолжительность проассоциированной с данной структурой области исходного файла
        /// </summary>
        public long Length { get; private set; }

        /// <param name="startIndex">Позиция в исходном файле, с которой начинается область проассоциированная с данной структурой</param>
        /// <param name="length">Продолжительность проассоциированной с данной структурой области исходного файла</param>
        public PieceOfSource(long startIndex, long length) : this()
        {
            this.StartIndex = startIndex;
            this.Length = length;
        }
    }
}
