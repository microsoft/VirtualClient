// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class ProcessExtensionsTests
    {
        private InMemoryProcess mockProcess;
        private MemoryStream standardInput;

        [SetUp]
        public void SetupTest()
        {
            this.standardInput = new MemoryStream();
            this.mockProcess = new InMemoryProcess(this.standardInput);
        }

        [TearDown]
        public void CleanupTest()
        {
            this.mockProcess.Dispose();
        }

        [Test]
        public void InteractiveExtensionSetsUpTheExpectedRequirementsForInterfacingWithTheCommandline()
        {
            IProcessProxy process = this.mockProcess.Interactive();
            Assert.IsNotNull(process);
            Assert.IsTrue(object.ReferenceEquals(process, this.mockProcess));
            Assert.IsTrue(process.RedirectStandardInput);
            Assert.IsTrue(process.RedirectStandardOutput);
        }

        [Test]
        public void ToProcessDetailsReturnsTheExpectedProcessInformation()
        {
            this.mockProcess.StandardOutput.Clear();
            this.mockProcess.StandardOutput.Append(Guid.NewGuid().ToString());
            this.mockProcess.StandardError.Clear();
            this.mockProcess.StandardError.Append(Guid.NewGuid().ToString());
            this.mockProcess.StartTime = DateTime.UtcNow.AddMinutes(-2);
            this.mockProcess.ExitTime = DateTime.UtcNow;
            this.mockProcess.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            ProcessDetails processDetails = this.mockProcess.ToProcessDetails("NTttcp");
            Assert.IsNotNull(processDetails);
            Assert.AreEqual(this.mockProcess.FullCommand(), processDetails.CommandLine);
            Assert.AreEqual(this.mockProcess.ExitCode, processDetails.ExitCode);
            Assert.AreEqual(this.mockProcess.ExitTime, processDetails.ExitTime);
            Assert.AreEqual(this.mockProcess.Id, processDetails.Id);
            Assert.AreEqual(this.mockProcess.StandardError.ToString(), processDetails.StandardError);
            Assert.AreEqual(this.mockProcess.StandardOutput.ToString(), processDetails.StandardOutput);
            Assert.AreEqual(this.mockProcess.StartInfo.WorkingDirectory, processDetails.WorkingDirectory);
        }

        [Test]
        public void ToProcessDetailsHandlesRaceConditionCasesWhereTheProcessExitCodeIsUnavailable()
        {
            // There is a race condition-style flaw in the .NET implementation of the
            // WaitForExit() method. The race condition allows for the process to exit after
            // completion but for a period of time to pass before the kernel completes all finalization
            // and cleanup steps (e.g. setting an exit code). To help prevent downstream issues that
            // happen when attempting to access properties on the process during this race condition period
            // of time, we are adding in an extra check on the process HasExited.
            //
            // Example of error hit during race condition period of time:
            // Process must exit before requested information can be determined.
            Mock<IProcessProxy> process = new Mock<IProcessProxy>();
            process.Setup(
                "/any/command.sh",
                "--any-args=true",
                "/any/working/dir",
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                1234);

            process.Setup(p => p.ExitCode).Throws(new InvalidOperationException("Process must exit before requested information can be determined."));

            ProcessDetails processDetails = null;
            Assert.DoesNotThrow(() => processDetails = process.Object.ToProcessDetails("NTttcp"));

            Assert.IsNotNull(processDetails);
            Assert.AreEqual(-1, processDetails.ExitCode);
            Assert.AreEqual(process.Object.FullCommand(), processDetails.CommandLine);
            Assert.AreEqual(process.Object.ExitTime, processDetails.ExitTime);
            Assert.AreEqual(process.Object.Id, processDetails.Id);
            Assert.AreEqual(process.Object.StandardError.ToString(), processDetails.StandardError);
            Assert.AreEqual(process.Object.StandardOutput.ToString(), processDetails.StandardOutput);
            Assert.AreEqual(process.Object.StartInfo.WorkingDirectory, processDetails.WorkingDirectory);
        }

        [Test]
        public void WaitForResponseAsyncExtensionExitsAsSoonAsTheExpectedResponseIsReceived()
        {
            int iterations = 0;
            Task allowIterations = Task.Run(() =>
            {
                while (iterations < 5)
                {
                    iterations++;
                    this.mockProcess.StandardOutput.AppendLine($"Iteration{iterations}");
                    Task.Delay(50).GetAwaiter().GetResult();
                }
            });

            this.mockProcess.RedirectStandardOutput = true;
            Task waitTask = this.mockProcess.WaitForResponseAsync("Iteration5", CancellationToken.None, timeout: TimeSpan.FromSeconds(10));
            allowIterations.GetAwaiter().GetResult();
            waitTask.GetAwaiter().GetResult();
        }

        [Test]
        public void WaitForResponseAsyncExtensionThrowsIfTheExpectedResponseIsNotReceivedWithinTheSpecifiedTimeout()
        {
            this.mockProcess.RedirectStandardOutput = true;
            Assert.ThrowsAsync<TimeoutException>(
                () => this.mockProcess.WaitForResponseAsync("NeverGonnaShow", CancellationToken.None, timeout: TimeSpan.Zero));
        }

        [Test]
        public void ProcessDetailsCloneCreatesANewInstanceWithTheSameValues()
        {
            ProcessDetails process1 = new ProcessDetails
            {
                Id = -1,
                CommandLine = Guid.NewGuid().ToString(),
                ExitTime = DateTime.UtcNow,
                ExitCode = -2,
                StandardOutput = Guid.NewGuid().ToString(),
                StandardError = Guid.NewGuid().ToString(),
                StartTime = DateTime.MinValue,
                ToolName = Guid.NewGuid().ToString(),
                WorkingDirectory = Guid.NewGuid().ToString(),
                Results = new[]
                {
                    new KeyValuePair<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                }
            };

            ProcessDetails process2 = process1.Clone();

            Assert.AreNotEqual(process1, process2);
            
            Assert.AreEqual(process1.Id, process2.Id);
            Assert.AreEqual(process1.CommandLine, process2.CommandLine);
            Assert.AreEqual(process1.ExitTime, process2.ExitTime);
            Assert.AreEqual(process1.ExitCode, process2.ExitCode);
            Assert.AreEqual(process1.StandardOutput, process2.StandardOutput);
            Assert.AreEqual(process1.StandardError, process2.StandardError);
            Assert.AreEqual(process1.StartTime, process2.StartTime);
            Assert.AreEqual(process1.ToolName, process2.ToolName);
            Assert.AreEqual(process1.WorkingDirectory, process2.WorkingDirectory);
            Assert.AreEqual(process1.Results.Count(), process2.Results.Count());
        }
    }
}
