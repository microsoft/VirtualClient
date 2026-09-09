// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    public class MockSetupExtensionsTests
    {
        private MockFixture mockFixture;

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
        }

        [Test]
        public void SetupProcessSetsPropertiesToExpectedValues_1()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>();
            string expectedCommand = "any.exe";
            string expectedArguments = "--any=arguments";
            string expectedWorkingDir = "C:\\any";
            IProcessProxy process = mockProcess.Setup(expectedCommand, expectedArguments, expectedWorkingDir).Object;

            Assert.AreEqual(0, process.ExitCode);
            Assert.IsNotNull(process.EnvironmentVariables);
            Assert.AreEqual(true, process.HasExited);
            Assert.IsTrue(process.Id > 0);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(expectedCommand, process.StartInfo.FileName);
            Assert.AreEqual(expectedArguments, process.StartInfo.Arguments);
            Assert.AreEqual(expectedWorkingDir, process.StartInfo.WorkingDirectory);
            Assert.IsNotNull(process.Name);
            Assert.IsNotNull(process.StandardError);
            Assert.IsTrue(process.StandardError.Length == 0);
            Assert.IsNotNull(process.StandardOutput);
            Assert.IsTrue(process.StandardOutput.Length == 0);
            Assert.IsNotNull(process.StandardInput);
        }

        [Test]
        public void SetupProcessSetsPropertiesToExpectedValues_2()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>();
            string expectedCommand = "any.exe";
            string expectedArguments = "--any=arguments";
            string expectedWorkingDir = "C:\\any";
            string expectedStandardOutput = "Any standard output";
            string expectedStandardError = "Any standard error";
            int expectedProcessId = 123456;
            int expectedExitCode = 98765;
            bool expectedHasExited = false;

            IProcessProxy process = mockProcess.Setup(
                expectedCommand, 
                expectedArguments, 
                expectedWorkingDir,
                expectedStandardOutput,
                expectedStandardError,
                expectedProcessId,
                expectedExitCode,
                expectedHasExited).Object;

            Assert.AreEqual(expectedExitCode, process.ExitCode);
            Assert.IsNotNull(process.EnvironmentVariables);
            Assert.AreEqual(expectedHasExited, process.HasExited);
            Assert.AreEqual(expectedProcessId, process.Id);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(expectedCommand, process.StartInfo.FileName);
            Assert.AreEqual(expectedArguments, process.StartInfo.Arguments);
            Assert.AreEqual(expectedWorkingDir, process.StartInfo.WorkingDirectory);
            Assert.IsNotNull(process.Name);
            Assert.IsNotNull(process.StandardError);
            Assert.AreEqual(expectedStandardError, process.StandardError.ToString());
            Assert.IsNotNull(process.StandardOutput);
            Assert.AreEqual(expectedStandardOutput, process.StandardOutput.ToString());
            Assert.IsNotNull(process.StandardInput);
            Assert.IsInstanceOf<StreamWriter>(process.StandardInput);
        }

        [Test]
        public void SetupProcessSupportsEnvironmentVariables()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>();
            IProcessProxy process = mockProcess.Setup("any.exe", "--any=arguments").Object;

            // First reference
            StringDictionary environmentVariables1 = process.EnvironmentVariables;
            Assert.IsNotNull(environmentVariables1);

            // Second reference (should be the same)
            StringDictionary environmentVariables2 = process.EnvironmentVariables;
            Assert.IsNotNull(environmentVariables2);
            Assert.IsTrue(object.ReferenceEquals(environmentVariables1, environmentVariables2));
        }
    }
}
