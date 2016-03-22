using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Наметка в исходном файле
    /// </summary>
    public class PieceOfSource : PieceOf, IPieceOfSource
    {
        /// <param name="startIndex">Позиция в исходном файле, с которой начинается область проассоциированная с данным экземпляром</param>
        public PieceOfSource(long startIndex) : base(startIndex)
        {

        }
    }
}
