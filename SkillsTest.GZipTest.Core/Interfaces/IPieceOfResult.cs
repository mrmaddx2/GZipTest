using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    /// <summary>
    /// Описывает методы и свойства, необходимые для упаковки фрагмента источника с помощью класса GZipStream
    /// </summary>
    public interface IPieceOfResult : IDisposable
    {
        /// <summary>
        /// Позиция в источнике, с которой начинается область проассоциированная с данным экземпляром
        /// </summary>
        long InputStartIndex { get; }
        /// <summary>
        /// Продолжительность области источника проассоциированной с данным экземпляром
        /// </summary>
        long InputLength { get; }

        /// <summary> 
        /// Упаковывает связанный с данным экземпляром фрагмент источника
        /// </summary>
        void Compress();

        /// <summary> 
        /// Распаковывает связанный с данным экземпляром фрагмент источника
        /// </summary>
        void Decompress();

        /// <summary>
        /// Служит для получения результатов работы методов <see cref="Compress"/> и <see cref="Decompress"/>
        /// </summary>
        /// <param name="releaseLock">Снимать ли лок с результата</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком источника работы</returns>
        byte[] GetOutputBuffer(bool releaseLock = true);
    }
}
