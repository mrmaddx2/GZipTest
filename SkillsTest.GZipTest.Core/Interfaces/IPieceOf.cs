using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IPieceOf : IDisposable
    {
        /// <summary>
        /// Позиция с которой начинается область проассоциированная с экземпляром
        /// </summary>
        long StartIndex { get; }
        /// <summary>
        /// Позиция на которой заканчивается область проассоциированная с экземпляром
        /// </summary>
        long EndIndex { get; }
        /// <summary>
        /// Продолжительность области источника проассоциированной с данным экземпляром
        /// </summary>
        long Length { get; }
        /// <summary>
        /// Служит для получения результатов работы/>
        /// </summary>
        /// <param name="cleanBodyAfter">Снимать ли лок с результата</param>
        /// <returns>Массив байт с результатом проделанной над исходным кусочком источника работы</returns>
        byte[] GetBodyBuffer(bool cleanBodyAfter);

        /// <summary>
        /// Дописывает к результату целый кусок данных
        /// </summary>
        /// <param name="value">кусок данных</param>
        void AddToBody(byte[] value);
        /// <summary>
        /// Дописывает к результату кусок данных
        /// </summary>
        /// <param name="value">кусок данных</param>
        /// <param name="offset">смещение</param>
        /// <param name="count">сколько байт читать из <see cref="value"/></param>
        void AddToBody(byte[] value, int offset, int count);
        MemoryStream GetBodyStream(bool cleanBodyAfter = false);
    }
}
