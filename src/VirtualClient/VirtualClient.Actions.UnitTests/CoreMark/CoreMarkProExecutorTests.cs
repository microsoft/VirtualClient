// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using VirtualClient.Common;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CoreMarkProExecutorTests
    {
        private MockFixture mockFixture;
        private ConcurrentBuffer coremarkProOutput = new ConcurrentBuffer();

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.Parameters["PackageName"] = "CoreMarkPro";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "CoreMark", "CoreMarkProExample1.txt");
            string results = File.ReadAllText(resultsPath);
            this.coremarkProOutput.Clear();
            this.coremarkProOutput.Append(results);
        }

        [Test]
        public void CoreMarkProExecutorThrowsOnNonSupportedPlatform()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters["PackageName"] = "CoreMarkPro";

            using (var executor = new CoreMarkProExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.PlatformNotSupported);
            }
        }

        [Test]
        public async Task CoreMarkProExecutorExcutesAsExpected()
        {
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual(args, $"TARGET=linux64 XCMD='-c{Environment.ProcessorCount}' certify-all");
                this.mockFixture.Process.StandardOutput = this.coremarkProOutput;
                return this.mockFixture.Process;
            };

            using (CoreMarkProExecutor executor = new CoreMarkProExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
