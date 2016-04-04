using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SkillsTest.GZipTest.TestUnit
{
    /// <summary>
    /// Тестируем приложение
    /// </summary>
    [TestClass]
    public class ConsoleAppTest
    {
        [ClassInitialize()]
        public static void LoadTestData(TestContext context)
        {
            File.Copy(@"SampleData\sample.txt", Path.Combine(context.TestRunDirectory, @"sample.txt"));
            File.Copy(@"GZipTest.exe", Path.Combine(context.TestRunDirectory, @"GZipTest.exe"));
            File.Copy(@"SkillsTest.GZipTest.Core.dll", Path.Combine(context.TestRunDirectory, @"SkillsTest.GZipTest.Core.dll"));
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            TestContext.Properties.Add("rawFilePath", Path.Combine(TestContext.TestRunDirectory, @"sample.txt"));
            TestContext.Properties.Add("compressedFilePath", Path.Combine(TestContext.TestResultsDirectory, @"arch.gz"));
            TestContext.Properties.Add("decompressedFilePath", Path.Combine(TestContext.TestResultsDirectory, @"result.txt"));
            TestContext.Properties.Add("appPath", Path.Combine(TestContext.TestRunDirectory, @"GZipTest.exe"));
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void Compress()
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo(TestContext.Properties["appPath"].ToString(), string.Format("compress \"{0}\" \"{1}\"", TestContext.Properties["rawFilePath"].ToString(), TestContext.Properties["compressedFilePath"].ToString()));
            process.Start();
            process.WaitForExit();

            Assert.IsTrue(process.ExitCode == 0);
        }

        [TestMethod]
        public void Decompress()
        {
            this.Compress();

            Process decompress = new Process();
            decompress.StartInfo = new ProcessStartInfo(TestContext.Properties["appPath"].ToString(), string.Format("decompress \"{0}\" \"{1}\"", TestContext.Properties["compressedFilePath"].ToString(), TestContext.Properties["decompressedFilePath"].ToString()));
            decompress.Start();
            decompress.WaitForExit();

            Assert.IsTrue(decompress.ExitCode == 0);
        }


        [TestMethod]
        public void CheckEquality()
        {
            this.Decompress();

            var rawFile = File.ReadAllBytes(TestContext.Properties["rawFilePath"].ToString());
            var decompressedFile = File.ReadAllBytes(TestContext.Properties["decompressedFilePath"].ToString());

            //Грязный хак
            Assert.IsTrue(rawFile.SequenceEqual(decompressedFile));
        }
    }
}
