using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IOutputFile : IProjectFile
    {
        /// <summary>
        /// Отвечает за присоединение очередного кусочка к файлу-результату.
        /// </summary>
        /// <param name="value">Кусочек результата</param>
        void AddPiece(IStatusedPieceOfResult value);
    }
}
