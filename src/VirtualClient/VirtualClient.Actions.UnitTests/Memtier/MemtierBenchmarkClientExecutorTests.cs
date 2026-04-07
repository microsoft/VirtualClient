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

        private void SetMultipleServerInstances()
        {
            // Setup:
            // Two Memcached server instances running on ports 6379 and 6380 with affinity to 4 logical processors
            this.mockFixture.ApiClient.OnGetState(nameof(ServerState))
               .ReturnsAsync(this.mockFixture.CreateHttpResponse(
                   HttpStatusCode.OK,
                   new Item<ServerState>(nameof(ServerState), new ServerState(new List<PortDescription>
                   {
                        new PortDescription
                        {
                            CpuAffinity = "0,1,2,3",
                            Port = 6379
                        },
                        new PortDescription
                        {
                            CpuAffinity = "0,1,2,3",
                            Port = 6380
                        }
                   }))));

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) => this.mockFixture.ApiClient.Object);

            this.mockFixture.ApiClient.OnGetHeartbeat()
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("memtier", this.mockFixture.GetPackagePath("memtier"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Memtier-Scenario",
                ["PackageName"] = this.mockPackage.Name,
                ["CommandLine"] = "--protocol memcache-text --threads 8 --clients 32 --ratio 1:1 --data-size 32 --pipeline 100 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern R:R",
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
                    // $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}",
                    $"sudo bash -c \"numactl -C 1 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\""
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
                    $"sudo bash -c \"numactl -C 1 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\""
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
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands2ClientInstances()
        {
            this.SetMultipleServerInstances();

            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // 2 client instances running in-parallel to target each of the 2 servers
                executor.Parameters[nameof(executor.ClientInstances)] = 2;
                executor.Parameters[nameof(executor.MaxClients)] = 6;
                executor.Parameters[nameof(executor.MemtierCpuAffinityDelta)] = 1;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Client instance #1 hitting server #1
                    $"sudo bash -c \"numactl -C 1 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\"",

                     // Client instance #2 hitting server #1
                    $"sudo bash -c \"numactl -C 2 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\"",

                    // Client instance #1 hitting server #2
                    $"sudo bash -c \"numactl -C 3 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6380 {executor.CommandLine}\"",

                     // Client instance #2 hitting server #2
                    $"sudo bash -c \"numactl -C 4 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6380 {executor.CommandLine}\""
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
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands4ClientInstances()
        {
           
            this.SetMultipleServerInstances();

            using (var executor = new TestMemtierBenchmarkClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // 4 client instances running in-parallel to target 1 server as the other server will sit idle because MaxClients = 4, If we Set MaxClients >= 8 both the servers will be engaged . 
                executor.Parameters[nameof(executor.ClientInstances)] = 4;
                executor.Parameters[nameof(executor.MaxClients)] = 4;
                executor.Parameters[nameof(executor.MemtierCpuAffinityDelta)] = 1;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Client instance #1
                    $"sudo bash -c \"numactl -C 1 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\"",

                     // Client instance #2
                    $"sudo bash -c \"numactl -C 2 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\"",

                    // Client instance #3
                    $"sudo bash -c \"numactl -C 3 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\"",

                     // Client instance #4
                    $"sudo bash -c \"numactl -C 4 {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine}\""
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
        public async Task MemtierBenchmarkClientExecutorEmitsTheExpectedMetricsRawMetricsScenario()
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

                IEnumerable<Tuple<LogLevel, EventId, object, Exception>> metricsEmitted = this.mockFixture.Logger.MessagesLogged(new Regex("(ScenarioResult)|(SucceededOrFailed)"));
                Assert.AreEqual(30, metricsEmitted.Count());

                IEnumerable<string> expectedMetrics = new List<string>
                {
                    "Throughput",
                    "Hits/sec",
                    "Misses/sec",
                    "Latency-Avg",
                    "Latency-P50",
                    "Latency-P80",
                    "Latency-P90",
                    "Latency-P95",
                    "Latency-P99",
                    "Latency-P99.9",
                    "Bandwidth",
                    "GET-Bandwidth",
                    "SET-Throughput",
                    "GET-Throughput",
                    "GET-Latency-Avg",
                    "GET-Latency-P50",
                    "GET-Latency-P80",
                    "GET-Latency-P90",
                    "GET-Latency-P95",
                    "GET-Latency-P99",
                    "GET-Latency-P99.9",
                    "SET-Latency-Avg",
                    "SET-Latency-P50",
                    "SET-Latency-P80",
                    "SET-Latency-P90",
                    "SET-Latency-P95",
                    "SET-Latency-P99",
                    "SET-Latency-P99.9",
                    "SET-Bandwidth",
                    "Succeeded"
                };

                IEnumerable<string> actualMetrics = metricsEmitted.Select(e => (e.Item3 as EventContext).Properties["metricName"].ToString());
                CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorEmitsTheExpectedMetricsAggregateMetricsScenario()
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

                IEnumerable<Tuple<LogLevel, EventId, object, Exception>> metricsEmitted = this.mockFixture.Logger.MessagesLogged(new Regex("(ScenarioResult)|(SucceededOrFailed)"));
                Assert.AreEqual(141, metricsEmitted.Count());

                IEnumerable<string> expectedMetrics = new List<string>
                {
                    "Bandwidth Avg",
                    "Bandwidth Min",
                    "Bandwidth Max",
                    "Bandwidth Stddev",
                    "Bandwidth P20",
                    "Bandwidth P50",
                    "Bandwidth P80",
                    "Bandwidth Total",
                    "Throughput Avg",
                    "Throughput Min",
                    "Throughput Max",
                    "Throughput Stddev",
                    "Throughput P20",
                    "Throughput P50",
                    "Throughput P80",
                    "Throughput Total",
                    "Hits/sec Avg",
                    "Hits/sec Min",
                    "Hits/sec Max",
                    "Hits/sec Stddev",
                    "Misses/sec Avg",
                    "Misses/sec Min",
                    "Misses/sec Max",
                    "Misses/sec Stddev",
                    "Latency-Avg Avg",
                    "Latency-Avg Min",
                    "Latency-Avg Max",
                    "Latency-Avg Stddev",
                    "Latency-P50 Avg",
                    "Latency-P50 Min",
                    "Latency-P50 Max",
                    "Latency-P50 Stddev",
                    "Latency-P80 Avg",
                    "Latency-P80 Min",
                    "Latency-P80 Max",
                    "Latency-P80 Stddev",
                    "Latency-P90 Avg",
                    "Latency-P90 Min",
                    "Latency-P90 Max",
                    "Latency-P90 Stddev",
                    "Latency-P95 Avg",
                    "Latency-P95 Min",
                    "Latency-P95 Max",
                    "Latency-P95 Stddev",
                    "Latency-P99 Avg",
                    "Latency-P99 Min",
                    "Latency-P99 Max",
                    "Latency-P99 Stddev",
                    "Latency-P99.9 Avg",
                    "Latency-P99.9 Min",
                    "Latency-P99.9 Max",
                    "Latency-P99.9 Stddev",
                    "GET-Throughput Avg",
                    "GET-Throughput Min",
                    "GET-Throughput Max",
                    "GET-Throughput Stddev",
                    "GET-Throughput P20",
                    "GET-Throughput P50",
                    "GET-Throughput P80",
                    "GET-Throughput Total",
                    "GET-Latency-Avg Avg",
                    "GET-Latency-Avg Min",
                    "GET-Latency-Avg Max",
                    "GET-Latency-Avg Stddev",
                    "GET-Latency-P50 Avg",
                    "GET-Latency-P50 Min",
                    "GET-Latency-P50 Max",
                    "GET-Latency-P50 Stddev",
                    "GET-Latency-P80 Avg",
                    "GET-Latency-P80 Min",
                    "GET-Latency-P80 Max",
                    "GET-Latency-P80 Stddev",
                    "SET-Latency-P80 Avg",
                    "SET-Latency-P80 Min",
                    "SET-Latency-P80 Max",
                    "SET-Latency-P80 Stddev",
                    "GET-Latency-P90 Avg",
                    "GET-Latency-P90 Min",
                    "GET-Latency-P90 Max",
                    "GET-Latency-P90 Stddev",
                    "GET-Latency-P95 Avg",
                    "GET-Latency-P95 Min",
                    "GET-Latency-P95 Max",
                    "GET-Latency-P95 Stddev",
                    "GET-Latency-P99 Avg",
                    "GET-Latency-P99 Min",
                    "GET-Latency-P99 Max",
                    "GET-Latency-P99 Stddev",
                    "GET-Latency-P99.9 Avg",
                    "GET-Latency-P99.9 Min",
                    "GET-Latency-P99.9 Max",
                    "GET-Latency-P99.9 Stddev",
                    "GET-Bandwidth Avg",
                    "GET-Bandwidth Min",
                    "GET-Bandwidth Max",
                    "GET-Bandwidth Stddev",
                    "GET-Bandwidth P20",
                    "GET-Bandwidth P50",
                    "GET-Bandwidth P80",
                    "GET-Bandwidth Total",
                    "SET-Throughput Avg",
                    "SET-Throughput Min",
                    "SET-Throughput Max",
                    "SET-Throughput Stddev",
                    "SET-Throughput P20",
                    "SET-Throughput P50",
                    "SET-Throughput P80",
                    "SET-Throughput Total",
                    "SET-Latency-Avg Avg",
                    "SET-Latency-Avg Min",
                    "SET-Latency-Avg Max",
                    "SET-Latency-Avg Stddev",
                    "SET-Latency-P50 Avg",
                    "SET-Latency-P50 Min",
                    "SET-Latency-P50 Max",
                    "SET-Latency-P50 Stddev",
                    "SET-Latency-P90 Avg",
                    "SET-Latency-P90 Min",
                    "SET-Latency-P90 Max",
                    "SET-Latency-P90 Stddev",
                    "SET-Latency-P95 Avg",
                    "SET-Latency-P95 Min",
                    "SET-Latency-P95 Max",
                    "SET-Latency-P95 Stddev",
                    "SET-Latency-P99 Avg",
                    "SET-Latency-P99 Min",
                    "SET-Latency-P99 Max",
                    "SET-Latency-P99 Stddev",
                    "SET-Latency-P99.9 Avg",
                    "SET-Latency-P99.9 Min",
                    "SET-Latency-P99.9 Max",
                    "SET-Latency-P99.9 Stddev",
                    "SET-Bandwidth Avg",
                    "SET-Bandwidth Min",
                    "SET-Bandwidth Max",
                    "SET-Bandwidth Stddev",
                    "SET-Bandwidth P20",
                    "SET-Bandwidth P50",
                    "SET-Bandwidth P80",
                    "SET-Bandwidth Total",
                    "Succeeded"
                };

                IEnumerable<string> actualMetrics = metricsEmitted.Select(e => (e.Item3 as EventContext).Properties["metricName"].ToString());
                foreach (string expectedMetric in expectedMetrics)
                {
                    Assert.IsTrue(
                        metricsEmitted.Count(e => (e.Item3 as EventContext).Properties["metricName"].ToString() == expectedMetric) == 1,
                        $"Expected metric '{expectedMetric}' not found in metrics emitted.");
                }
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
