// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class RedisClientProfileTests
    {
        private DependencyFixture mockFixture;
        private string clientAgentId;
        private string serverAgentId;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.mockFixture.SetupWorkloadPackage("wget", expectedFiles: "linux-x64/wget2");
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public void RedisMemtierWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(string profile)
        {
            this.mockFixture.PackageManager.Clear();
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [Test]
        [Ignore("We need to completely refactor the functional tests for Memcached and Redis to consolidate and cleanup.")]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfClientOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
             $"--protocol redis --clients 1 --threads 4 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 180 --print-percentile 50,90,95,99,99.9 --random-data",
             $" -h 1.2.3.5 -p 6379 -c 1 -n 10000 -P 32 -q --csv\""
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            this.mockFixture.SetupFile(@"/home/user/tools/VirtualClient/scripts/Redis/RunClient.sh");
            this.mockFixture.SetupFile(@"/home/user/tools/VirtualClient/packages/redis-6.2.1/src/redis-benchmark");

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("memtier_benchmark --server", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_RedisMemtier.txt"));
                }
                else if (arguments.Contains("redis-benchmark", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_RedisBenchmark.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverName, ipAddress);

            ServerState state = new ServerState(new List<PortDescription>
            {
                new PortDescription
                {
                    CpuAffinity = "0",
                    Port = 6379
                },
                new PortDescription
                {
                    CpuAffinity = "1",
                    Port = 6380
                }
            });

            apiClient.CreateStateAsync(nameof(ServerState), state, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }
}
