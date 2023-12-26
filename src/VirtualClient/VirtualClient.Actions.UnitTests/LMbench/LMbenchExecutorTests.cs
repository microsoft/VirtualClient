// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class LMbenchExecutorTests
    {
        private DependencyFixture fixture;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new DependencyFixture(PlatformID.Unix);
            this.disks = this.fixture.CreateDisks(PlatformID.Unix);
            this.fixture.DiskManager.AddRange(this.disks);

            // The workload requires the LMbench package to be registered (either built-in or installed). The LMbench
            // workload is compiled using Make and has a build step that runs the memory test. This uses commands in the
            // 'scripts' folder.
            this.fixture.SetupWorkloadPackage("lmbench", expectedFiles: "linux-x64/scripts/allmem");
            this.fixture.SetupWorkloadPackage("lmbench", expectedFiles: "linux-x64/scripts/build");


            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LMbenchExecutor.PackageName), "lmbench" }
            };

            this.fixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string lmbenchOutput = File.ReadAllText(@"Examples\LMbench\LMbenchExample.txt");
                process.StandardOutput.Append(lmbenchOutput);
            };
        }

        [Test]
        public async Task LMbenchExecutorExecutesTheCorrectWorkloadCommands()
        {
            this.fixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string lmbenchOutput = File.ReadAllText(@"Examples\LMbench\LMbenchExample.txt");
                process.StandardOutput.Append(lmbenchOutput);
            };

            using (TestLMbenchExecutor lmbenchExecutor = new TestLMbenchExecutor(this.fixture))
            {
                await lmbenchExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted(
                    "make results",
                    "make see"));
            }
        }

        private class TestLMbenchExecutor : LMbenchExecutor
        {
            public TestLMbenchExecutor(DependencyFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
