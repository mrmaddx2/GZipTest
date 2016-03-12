using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class StringHelper
    {
        public static bool IsNullOrWhiteSpace(string input, char whiteSpaceChar = ' ')
        {
            return string.IsNullOrEmpty(input) || input.Replace(whiteSpaceChar, Convert.ToChar(string.Empty)).Length == 0;
        }
    }
}
