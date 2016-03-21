using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IOutputFile : IDisposable
    {
        /// <summary>
        /// Отвечает за псисоединение очередного кусочка к файлу-результату.
        /// </summary>
        /// <param name="value">Кусочек результата</param>
        void AddPiece(IPieceOfResult value);
    }
}
