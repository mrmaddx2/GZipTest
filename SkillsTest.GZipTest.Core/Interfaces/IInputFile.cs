using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public interface IInputFile : IProjectFile
    {
        /// <summary>
        /// Служит для получения очередного необработанного фрагмента файла-источника.
        /// </summary>
        /// <returns>Фрагмент файла-источника или null если все обработаны</returns>
        IPieceOfSource Fetch(long? inputFragmentSize = null);
    }
}
