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
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.MemcachedExecutor;

    [TestFixture]
    [Category("Functional")]
    public class MemcachedServerProfileTests
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
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
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
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "MemcachedPackage");
            }
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformMultiVM(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"sudo -u username bash -c \"numactl -C 0 /home/user/tools/VirtualClient/packages/memcached-1.6.17/memcached -d -p 6379 -t 4 -m 1024",
                $"sudo -u username /home/user/tools/VirtualClient/packages/memtier/memtier_benchmark --protocol=memcache_text --server localhost --port=6379 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys"
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

            ServerState state = new ServerState(new Dictionary<string, IConvertible>
            {
                [nameof(ServerState.Ports)] = 6379
            });

            await apiClient.CreateStateAsync(nameof(ServerState), state, CancellationToken.None);

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
