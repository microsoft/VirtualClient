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
        private MockFixture fixture;
        private DependencyPath mockPath;
        private State expectedState;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();

            this.SetupDefaultMockBehavior(PlatformID.Unix);
        }

        [Test]
        public void NASParallelBenchClientExecutorThrowsWhenABenchmarkIsNotFound()
        {
            this.fixture.FileSystem.Setup(d => d.File.Exists(It.IsAny<string>()))
                .Returns(false);

            using (NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.fixture.Dependencies, this.fixture.Parameters))
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
        public async Task NasParallelBenchClientExecutorDoesNotExecuteUnsupportedBenchmarks(
            string Benchmark, bool MultiTierScenario)
        {
            if (!MultiTierScenario)
            {
                this.fixture.Layout = null;
            }
            this.fixture.Parameters["Benchmark"] = Benchmark;

            int executionCount = 0;

            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                executionCount++;
                return this.fixture.Process;
            };

            using (NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }

            Assert.AreEqual(executionCount, 0);
        }

        [Test]
        public async Task NASParallelBenchClientExecutorUsesMPIOptionInMultiSystemScenarios()
        {
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(cmd, "sudo");
                Assert.AreEqual(args, $"bash -c \"runuser -l my-username -c 'export OMP_NUM_THREADS" +
                    $"={Environment.ProcessorCount} && export NPB_NPROCS_STRICT=off && mpiexec -np 2 " +
                    $"--host 1.2.3.4,1.2.3.5 {this.fixture.GetPackagePath()}/nasparallelbench/" +
                    $"linux-x64/NPB-MPI/bin/bt.S.x'\"");
                return this.fixture.Process;
            };

            using (NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task NASParallelBenchClientExecutorUsesOMPOptionInSingleSystemScenarios()
        {
            this.fixture.Layout = null;
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(cmd, "sudo");
                Assert.AreEqual(args, $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} " +
                    $"&& {this.fixture.GetPackagePath()}/nasparallelbench/linux-x64/NPB-OMP/bin/" +
                    $"bt.S.x\"");
                return this.fixture.Process;
            };

            using NASParallelBenchClientExecutor executor = new NASParallelBenchClientExecutor(
                this.fixture.Dependencies, this.fixture.Parameters);
            {
                await executor.ExecuteAsync(CancellationToken.None);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.fixture.Setup(platformID);

            this.mockPath = this.fixture.Create<DependencyPath>();
            DependencyPath mockPackage = new DependencyPath(
                "nasparallelbench", this.fixture.PlatformSpecifics.GetPackagePath("nasparallelbench"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name,
                ["Benchmark"] = NASParallelBenchClientExecutorTests.ExampleBenchmark,
                ["Username"] = NASParallelBenchClientExecutorTests.ExampleUsername
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.fixture.Process;
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);

            string npbBuildState = "NpbBuildState";
            this.expectedState = new State(new Dictionary<string, IConvertible>
            {
                [npbBuildState] = "completed"
            });

            Item<JObject> expectedStateItem = new Item<JObject>(npbBuildState, JObject.FromObject(this.expectedState));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(
                It.IsAny<String>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem));
        }
    }
}
