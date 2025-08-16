// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.IO;
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
    }
}
