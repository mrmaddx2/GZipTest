using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Описывает методы и свойства, необходимые для упаковки фрагмента источника с помощью класса GZipStream
    /// </summary>
    public interface IPieceOfResult : IPieceOf
    {
        /// <summary> 
        /// Упаковывает связанный с данным экземпляром фрагмент источника
        /// </summary>
        void Compress();

        /// <summary> 
        /// Распаковывает связанный с данным экземпляром фрагмент источника
        /// </summary>
        void Decompress();
    }
}
