using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    class StreamHelper
    {
        public static void CopyTo(Stream input, Stream output, long? lenght = null)
        {
            if (lenght == null)
            {
                lenght = input.Length - input.Position;
            }

            byte[] buffer = new byte[(long)lenght]; // Fairly arbitrary size
            int bytesRead = input.Read(buffer, 0, buffer.Length);
            output.Write(buffer, 0, bytesRead);
        }
    }
}
