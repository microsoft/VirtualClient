// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
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

            this.mockFixture.SetupPackage("wget", null, "linux-x64/wget2");
            this.mockFixture.SetupPackage("redis", null, "src/redis-benchmark", "src/redis-server");
            this.mockFixture.SetupPackage("memtier", null, "memtier_benchmark");
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
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfClientOnUnixPlatform(string profile)
        {
            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    "memtier_benchmark.*--server",
                    TestDependencies.GetResourceFileContents("Results_RedisMemtier.txt"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                this.mockFixture.Tracking.AssertCommandsExecuted(
                    "sudo chmod \\+x.*memtier_benchmark",
                    "memtier_benchmark.*--server 1\\.2\\.3\\.5.*--port 6379.*--protocol redis",
                    "memtier_benchmark.*--server 1\\.2\\.3\\.5.*--port 6380.*--protocol redis");
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
