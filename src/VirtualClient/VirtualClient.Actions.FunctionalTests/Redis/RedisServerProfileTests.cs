// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class RedisServerProfileTests
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

            DeathStarBenchExecutor.StateConfirmationPollingTimeout = TimeSpan.FromMilliseconds(1);
            DeathStarBenchExecutor.ServerWarmUpTime = TimeSpan.FromMilliseconds(1);
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.mockFixture.SystemManagement.SetupGet(sm => sm.AgentId).Returns(this.serverAgentId);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations  
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "MemtierPackage");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "RedisPackage");
            }
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformMultiVM(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
             $"sudo pkill -f redis-server",
             $"bash -c \"numactl -C 0 /home/user/tools/VirtualClient/packages/redis-6.2.1/src/redis-server --port 6379 --protected-mode no --ignore-warnings ARM64-COW-BUG --save  --io-threads 4 --maxmemory-policy noeviction\"",
             $"/home/user/tools/VirtualClient/packages/memtier/memtier_benchmark --protocol=redis --server localhost --port=6379 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.mockFixture.SystemManagement.SetupGet(sm => sm.AgentId).Returns(this.serverAgentId);

            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient("1.2.3.5", ipAddress);

            await apiClient.UpdateStateAsync(
                "ServerState",
                new Item<State>("ServerState", new State(new Dictionary<string, IConvertible>
                {
                    [nameof(RedisExecutor.ServerCopiesCount)] = 2
                })),
                CancellationToken.None);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }
    }
}
