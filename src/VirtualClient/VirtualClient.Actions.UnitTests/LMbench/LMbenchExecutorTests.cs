// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LMbenchExecutorTests : MockFixture
    {
        private static string Examples = MockFixture.GetDirectory(typeof(LMbenchExecutorTests), "Examples", "LMbench");
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);

            // The workload requires the LMbench package to be registered (either built-in or installed). The LMbench
            // workload is compiled using Make and has a build step that runs the memory test. This uses commands in the
            // 'scripts' folder.
            this.mockPackage = new DependencyPath("lmbench", this.PlatformSpecifics.GetPackagePath("lmbench"));
            this.SetupPackage(this.mockPackage);
            this.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);

            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LMbenchExecutor.PackageName), "lmbench" },
                { nameof(LMbenchExecutor.CompilerFlags), "CPPFLAGS=\"-I /usr/include/tirpc\"" },
                { nameof(LMbenchExecutor.Scenario), "Scenario" }
            };

            this.ProcessManager.OnProcessCreated = (process) =>
            {
                string lmbenchOutput = System.IO.File.ReadAllText(this.Combine(LMbenchExecutorTests.Examples, "lmbench_example_results_1.txt"));
                process.StandardOutput.Append(lmbenchOutput);
            };
        }

        [Test]
        public async Task LMbenchExecutorExecutesTheExpectedWorkloadCommands()
        {
            using (TestLMbenchExecutor lmbenchExecutor = new TestLMbenchExecutor(this.Dependencies, this.Parameters))
            {
                await lmbenchExecutor.ExecuteAsync(EventContext.None, CancellationToken.None);

                Assert.IsTrue(this.ProcessManager.CommandsExecuted(
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/scripts\"",
                    $"make build CPPFLAGS=\"-I /usr/include/tirpc\"",
                    $"bash -c \"echo -e '\n\n\n\n\n\n\n\n\n\n\n\n\nnone' | make results\"",
                    $"make summary"));
            }
        }

        [Test]
        public async Task LMbenchExecutorExecutesTheExpectedLMbenchBenchmarks()
        {
            List<string> expectedBenchmarks = new List<string>
            {
                "BENCHMARK_BCOPY",
                "BENCHMARK_MEM",
                "BENCHMARK_MMAP",
                "BENCHMARK_FILE"
            };

            using (TestLMbenchExecutor lmbenchExecutor = new TestLMbenchExecutor(this.Dependencies, this.Parameters))
            {
                await lmbenchExecutor.ExecuteAsync(EventContext.None, CancellationToken.None);

                CollectionAssert.AreEquivalent(expectedBenchmarks, lmbenchExecutor.Benchmarks);
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
