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
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;
    using System.Reflection;

    [TestFixture]
    [Category("Unit")]
    public class LMbenchExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);

            // The workload requires the LMbench package to be registered (either built-in or installed). The LMbench
            // workload is compiled using Make and has a build step that runs the memory test. This uses commands in the
            // 'scripts' folder.
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);
            this.mockPackage = new DependencyPath("sysbench", this.fixture.PlatformSpecifics.GetPackagePath("lmbench"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.fixture.Directory.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "LMbenchX64Example.txt");
            string output = File.ReadAllText(outputPath);
            this.fixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => output);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LMbenchExecutor.PackageName), "lmbench" }
            };
        }

        [Test]
        public async Task LMbenchExecutorExecutesTheCorrectWorkloadCommands()
        {
            this.fixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string lmbenchOutput = File.ReadAllText(Path.Combine("Examples", "LMbench", "LMbenchExample.txt"));
            };

            using (TestLMbenchExecutor lmbenchExecutor = new TestLMbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await lmbenchExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted(
                    "make build",
                    "bash -c \"echo -e '\n\n\n\n\n\n\n\n\n\n\n\n\nnone' | make results\"",
                    "make see"));
            }
        }

        private class TestLMbenchExecutor : LMbenchExecutor
        {
            public TestLMbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
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
