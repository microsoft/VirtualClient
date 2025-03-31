// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NASParallelBench
{
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using AutoFixture;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NASParallelBenchClientExecutorTests
    {
        private const string ExampleBenchmark = "bt.S.x";
        private const string ExampleUsername = "my-username";
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private State expectedState;

        public void SetupTest(PlatformID platformID)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);

            this.mockPackage = new DependencyPath("nasparallelbench", this.mockFixture.PlatformSpecifics.GetPackagePath("nasparallelbench"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPackage.Name,
                ["Benchmark"] = NASParallelBenchClientExecutorTests.ExampleBenchmark,
                ["Username"] = NASParallelBenchClientExecutorTests.ExampleUsername
            };

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.mockFixture.Process;

            string npbBuildState = "NpbBuildState";
            this.expectedState = new State(new Dictionary<string, IConvertible>
            {
                [npbBuildState] = "completed"
            });

            Item<JObject> expectedStateItem = new Item<JObject>(npbBuildState, JObject.FromObject(this.expectedState));

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(
                It.IsAny<String>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem));
        }

        [Test]
        public void NASParallelBenchClientExecutorThrowsWhenABenchmarkIsNotFound()
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.FileSystem.Setup(d => d.File.Exists(It.IsAny<string>()))
                .Returns(false);

            using (NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exc = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exc.Reason);
            }
        }

        [Test]
        [TestCase("dc.S.x", true)]
        [TestCase("ua.S.x", true)]
        [TestCase("dt.S.x WH", false)]
        [TestCase("dt.S.x SH", true)]
        [TestCase("dt.S.x BH", true)]
        [TestCase("dt.S.x WH", true)]
        public async Task NasParallelBenchClientExecutorDoesNotExecuteUnsupportedBenchmarks(string Benchmark, bool MultiTierScenario)
        {
            this.SetupTest(PlatformID.Unix);

            if (!MultiTierScenario)
            {
                this.mockFixture.Layout = null;
            }
            this.mockFixture.Parameters["Benchmark"] = Benchmark;

            int executionCount = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                executionCount++;
                return this.mockFixture.Process;
            };

            using (NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }

            Assert.AreEqual(executionCount, 0);
        }

        [Test]
        public async Task NASParallelBenchClientExecutorUsesMPIOptionInMultiSystemScenarios()
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(cmd, "sudo");
                Assert.AreEqual(args, $"bash -c \"runuser -l my-username -c 'export OMP_NUM_THREADS" +
                    $"={Environment.ProcessorCount} && export NPB_NPROCS_STRICT=off && mpiexec -np 2 " +
                    $"--host 1.2.3.4,1.2.3.5 {this.mockFixture.GetPackagePath()}/nasparallelbench/" +
                    $"linux-x64/NPB-MPI/bin/bt.S.x'\"");
                return this.mockFixture.Process;
            };

            using (NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task NASParallelBenchClientExecutorUsesOMPOptionInSingleSystemScenarios()
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.Layout = null;
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(cmd, "sudo");
                Assert.AreEqual(args, $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} " +
                    $"&& {this.mockFixture.GetPackagePath()}/nasparallelbench/linux-x64/NPB-OMP/bin/" +
                    $"bt.S.x\"");
                return this.mockFixture.Process;
            };

            using NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.mockFixture.Dependencies, this.mockFixture.Parameters);
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }
        }
    }
}
