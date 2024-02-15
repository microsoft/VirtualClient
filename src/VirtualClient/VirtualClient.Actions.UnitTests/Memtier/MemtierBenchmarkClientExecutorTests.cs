// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemtierBenchmarkClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string results;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("memtier", this.mockFixture.GetPackagePath("memtier"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Memtier_Scenario",
                ["PackageName"] = this.mockPackage.Name,
                ["CommandLine"] = "--protocol memcache_text --threads 8 --clients 32 --ratio 1:1 --data-size 32 --pipeline 100 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern R:R",
                ["ClientInstances"] = 1,
                ["Duration"] = "00:03:00",
                ["Username"] = "testuser"
            };

            this.mockFixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPackage);

            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.results = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierBenchmarkClientExecutorTests), "Examples", "Memtier", "Memtier_Memcached_Results_1.txt"));

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.results);

            // Setup:
            // A single Memcached server instance running on port 6379 with affinity to 4 logical processors
            this.mockFixture.ApiClient.OnGetState(nameof(ServerState))
               .ReturnsAsync(this.mockFixture.CreateHttpResponse(
                   HttpStatusCode.OK,
                   new Item<ServerState>(nameof(ServerState), new ServerState(new List<PortDescription>
                   {
                        new PortDescription
                        {
                            CpuAffinity = "0,1,2,3",
                            Port = 6379
                        }
                   }))));

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) => this.mockFixture.ApiClient.Object);

            this.mockFixture.ApiClient.OnGetHeartbeat()
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorOnInitializationGetsTheExpectedPackage()
        {
            using (var component = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.mockFixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public void MemtierBenchmarkClientExecutorHandlesDurationsAsBothIntegerAndTimeSpanFormats()
        {
            this.mockFixture.Parameters["Duration"] = 30;
            using (var component = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual(30, component.Duration);
            }

            this.mockFixture.Parameters["Duration"] = TimeSpan.FromMinutes(1).ToString();
            using (var component = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.AreEqual(60, component.Duration);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Run the Memtier benchmark. Values based on the default parameter values set at the top
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    this.mockFixture.Process.StandardOutput.Append(this.results);

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommandsWhenAUsernameIsDefined()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                executor.Parameters[nameof(executor.Username)] = "testuser";

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Run the Memtier benchmark. Values based on the default parameter values set at the top
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    this.mockFixture.Process.StandardOutput.Append(this.results);

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands_2_Client_Instances()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // 2 client instances running in-parallel to target the server.
                executor.Parameters[nameof(executor.ClientInstances)] = 2;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Client instance #1
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}",

                     // Client instance #2
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    this.mockFixture.Process.StandardOutput.Append(this.results);

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands_4_Client_Instances()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // 4 client instances running in-parallel to target the server.
                executor.Parameters[nameof(executor.ClientInstances)] = 4;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Client instance #1
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}",

                     // Client instance #2
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}",

                    // Client instance #3
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}",

                     // Client instance #4
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    this.mockFixture.Process.StandardOutput.Append(this.results);

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorEmitsTheExpectedMetrics_Raw_Metrics_Scenario()
        {
            this.mockFixture.Parameters[nameof(MemtierBenchmarkClientExecutor.EmitRawMetrics)] = true;
            this.mockFixture.Parameters[nameof(MemtierBenchmarkClientExecutor.EmitAggregateMetrics)] = false;

            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // Setup:
                // Set the standard output for the Memtier process to valid results.
                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    if (process.FullCommand().Contains("memtier_benchmark"))
                    {
                        process.StandardOutput.Append(this.results);
                    }
                };

                await executor.ExecuteAsync(CancellationToken.None);

                IEnumerable<Tuple<LogLevel, EventId, object, Exception>> metricsEmitted = this.mockFixture.Logger.MessagesLogged(new Regex("ScenarioResult"));
                Assert.AreEqual(30, metricsEmitted.Count());

                IEnumerable<string> expectedMetrics = new List<string>
                {
                    "Throughput",
                    "Hits/sec",
                    "Misses/sec",
                    "Latency-Avg",
                    "Latency-P50",
                    "Latency-P90",
                    "Latency-P95",
                    "Latency-P99",
                    "Latency-P99.9",
                    "Bandwidth",
                    "GET_Throughput",
                    "GET_Latency-Avg",
                    "GET_Latency-P50",
                    "GET_Latency-P90",
                    "GET_Latency-P95",
                    "GET_Latency-P99",
                    "GET_Latency-P99.9",
                    "GET_Bandwidth",
                    "SET_Throughput",
                    "SET_Latency-Avg",
                    "SET_Latency-P50",
                    "SET_Latency-P90",
                    "SET_Latency-P95",
                    "SET_Latency-P99",
                    "SET_Latency-P99.9",
                    "SET_Bandwidth",
                    "GET_Latency-P80",
                    "SET_Latency-P80",
                    "Latency-P80",
                    "Succeeded"
                };

                IEnumerable<string> actualMetrics = metricsEmitted.Select(e => (e.Item3 as EventContext).Properties["metricName"].ToString());
                CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorEmitsTheExpectedMetrics_Aggregate_Metrics_Scenario()
        {
            this.mockFixture.Parameters[nameof(MemtierBenchmarkClientExecutor.EmitRawMetrics)] = false;
            this.mockFixture.Parameters[nameof(MemtierBenchmarkClientExecutor.EmitAggregateMetrics)] = true;

            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // Setup:
                // Set the standard output for the Memtier process to valid results.
                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    if (process.FullCommand().Contains("memtier_benchmark"))
                    {
                        process.StandardOutput.Append(this.results);
                    }
                };

                await executor.ExecuteAsync(CancellationToken.None);

                IEnumerable<Tuple<LogLevel, EventId, object, Exception>> metricsEmitted = this.mockFixture.Logger.MessagesLogged(new Regex("ScenarioResult"));
                Assert.AreEqual(159, metricsEmitted.Count());

                IEnumerable<string> expectedMetrics = new List<string>
                {
                    "Throughput-Avg",
                    "Throughput-Min",
                    "Throughput-Max",
                    "Throughput-Stddev",
                    "Throughput-P50",
                    "Throughput-P80",
                    "Throughput-P90",
                    "Throughput-P95",
                    "Throughput-P99",
                    "Throughput-P99.9",
                    "Throughput-Total",
                    "Hits/sec-Avg",
                    "Hits/sec-Min",
                    "Hits/sec-Max",
                    "Hits/sec-Stddev",
                    "Misses/sec-Avg",
                    "Misses/sec-Min",
                    "Misses/sec-Max",
                    "Misses/sec-Stddev",
                    "Latency-Avg-Avg",
                    "Latency-Avg-Min",
                    "Latency-Avg-Max",
                    "Latency-Avg-Stddev",
                    "Latency-P50-Avg",
                    "Latency-P50-Min",
                    "Latency-P50-Max",
                    "Latency-P50-Stddev",
                    "Latency-P90-Avg",
                    "Latency-P90-Min",
                    "Latency-P90-Max",
                    "Latency-P90-Stddev",
                    "Latency-P95-Avg",
                    "Latency-P95-Min",
                    "Latency-P95-Max",
                    "Latency-P95-Stddev",
                    "Latency-P99-Avg",
                    "Latency-P99-Min",
                    "Latency-P99-Max",
                    "Latency-P99-Stddev",
                    "Latency-P99.9-Avg",
                    "Latency-P99.9-Min",
                    "Latency-P99.9-Max",
                    "Latency-P99.9-Stddev",
                    "Bandwidth-Avg",
                    "Bandwidth-Min",
                    "Bandwidth-Max",
                    "Bandwidth-Stddev",
                    "Bandwidth-P50",
                    "Bandwidth-P80",
                    "Bandwidth-P90",
                    "Bandwidth-P95",
                    "Bandwidth-P99",
                    "Bandwidth-P99.9",
                    "Bandwidth-Total",
                    "GET_Throughput-Avg",
                    "GET_Throughput-Min",
                    "GET_Throughput-Max",
                    "GET_Throughput-Stddev",
                    "GET_Throughput-P50",
                    "GET_Throughput-P80",
                    "GET_Throughput-P90",
                    "GET_Throughput-P95",
                    "GET_Throughput-P99",
                    "GET_Throughput-P99.9",
                    "GET_Throughput-Total",
                    "GET_Latency-Avg-Avg",
                    "GET_Latency-Avg-Min",
                    "GET_Latency-Avg-Max",
                    "GET_Latency-Avg-Stddev",
                    "GET_Latency-P50-Avg",
                    "GET_Latency-P50-Min",
                    "GET_Latency-P50-Max",
                    "GET_Latency-P50-Stddev",
                    "GET_Latency-P90-Avg",
                    "GET_Latency-P90-Min",
                    "GET_Latency-P90-Max",
                    "GET_Latency-P90-Stddev",
                    "GET_Latency-P95-Avg",
                    "GET_Latency-P95-Min",
                    "GET_Latency-P95-Max",
                    "GET_Latency-P95-Stddev",
                    "GET_Latency-P99-Avg",
                    "GET_Latency-P99-Min",
                    "GET_Latency-P99-Max",
                    "GET_Latency-P99-Stddev",
                    "GET_Latency-P99.9-Avg",
                    "GET_Latency-P99.9-Min",
                    "GET_Latency-P99.9-Max",
                    "GET_Latency-P99.9-Stddev",
                    "GET_Bandwidth-Avg",
                    "GET_Bandwidth-Min",
                    "GET_Bandwidth-Max",
                    "GET_Bandwidth-Stddev",
                    "GET_Bandwidth-P50",
                    "GET_Bandwidth-P80",
                    "GET_Bandwidth-P90",
                    "GET_Bandwidth-P95",
                    "GET_Bandwidth-P99",
                    "GET_Bandwidth-P99.9",
                    "GET_Bandwidth-Total",
                    "SET_Throughput-Avg",
                    "SET_Throughput-Min",
                    "SET_Throughput-Max",
                    "SET_Throughput-Stddev",
                    "SET_Throughput-P50",
                    "SET_Throughput-P80",
                    "SET_Throughput-P90",
                    "SET_Throughput-P95",
                    "SET_Throughput-P99",
                    "SET_Throughput-P99.9",
                    "SET_Throughput-Total",
                    "SET_Latency-Avg-Avg",
                    "SET_Latency-Avg-Min",
                    "SET_Latency-Avg-Max",
                    "SET_Latency-Avg-Stddev",
                    "SET_Latency-P50-Avg",
                    "SET_Latency-P50-Min",
                    "SET_Latency-P50-Max",
                    "SET_Latency-P50-Stddev",
                    "SET_Latency-P90-Avg",
                    "SET_Latency-P90-Min",
                    "SET_Latency-P90-Max",
                    "SET_Latency-P90-Stddev",
                    "SET_Latency-P95-Avg",
                    "SET_Latency-P95-Min",
                    "SET_Latency-P95-Max",
                    "SET_Latency-P95-Stddev",
                    "SET_Latency-P99-Avg",
                    "SET_Latency-P99-Min",
                    "SET_Latency-P99-Max",
                    "SET_Latency-P99-Stddev",
                    "SET_Latency-P99.9-Avg",
                    "SET_Latency-P99.9-Min",
                    "SET_Latency-P99.9-Max",
                    "SET_Latency-P99.9-Stddev",
                    "SET_Bandwidth-Avg",
                    "SET_Bandwidth-Min",
                    "SET_Bandwidth-Max",
                    "SET_Bandwidth-Stddev",
                    "SET_Bandwidth-P50",
                    "SET_Bandwidth-P80",
                    "SET_Bandwidth-P90",
                    "SET_Bandwidth-P95",
                    "SET_Bandwidth-P99",
                    "SET_Bandwidth-P99.9",
                    "SET_Bandwidth-Total",
                    "GET_Latency-P80-Avg",
                    "GET_Latency-P80-Min",
                    "GET_Latency-P80-Max",
                    "GET_Latency-P80-Stddev",
                    "SET_Latency-P80-Avg",
                    "SET_Latency-P80-Min",
                    "SET_Latency-P80-Max",
                    "SET_Latency-P80-Stddev",
                    "Latency-P80-Avg",
                    "Latency-P80-Min",
                    "Latency-P80-Max",
                    "Latency-P80-Stddev",
                    "Succeeded"
                };

                IEnumerable<string> actualMetrics = metricsEmitted.Select(e => (e.Item3 as EventContext).Properties["metricName"].ToString());
                CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
            }
        }

        private class TestMemtierBenchmarkClientExecutor : MemtierBenchmarkClientExecutor
        {
            public TestMemtierBenchmarkClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
                this.ClientFlowRetryPolicy = Policy.NoOpAsync();
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

        }
    }
}
