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
            Assert.AreNotEqual(process1.Results, process2.Results);

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
