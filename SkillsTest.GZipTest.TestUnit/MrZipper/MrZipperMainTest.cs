﻿using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillsTest.GZipTest.Core;

namespace SkillsTest.GZipTest.TestUnit
{
    /// <summary>
    /// Тестируем мистера зиппера
    /// </summary>
    [TestClass]
    public class MrZipperMainTest
    {
        //TODO: Сделать нормальную очередность тестов

        private MrZipper subject;

        public MrZipperMainTest() : base()
        {
            this.subject = new MrZipper();
        }

        [ClassInitialize()]
        public static void LoadTestData(TestContext context)
        {
            File.Copy(@"SampleData\sample.txt", Path.Combine(context.TestRunDirectory, @"sample.txt"));
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            TestContext.Properties.Add("rawFilePath", Path.Combine(TestContext.TestRunDirectory, @"sample.txt"));
            TestContext.Properties.Add("compressedFilePath", Path.Combine(TestContext.TestResultsDirectory, @"arch.gz"));
            TestContext.Properties.Add("decompressedFilePath", Path.Combine(TestContext.TestResultsDirectory, @"result.txt"));
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
    }
}
