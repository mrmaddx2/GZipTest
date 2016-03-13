using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkillsTest.GZipTest.Core;

namespace SkillsTest.GZipTest.App
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = args[0];
            string output = args[1];
            string secondOutput = args[2];

            MrZipper zipper = new MrZipper();

            zipper.Compress(input, output);
            zipper.Decompress(output, secondOutput);
        }
    }
}
