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
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class RedisServerProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            this.mockFixture
                .Setup(PlatformID.Unix, Architecture.X64, "Server01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.mockFixture.SetupPackage("wget", null, "linux-x64/wget2");
            this.mockFixture.SetupFile("redis", "redis-6.2.1/src/redis-server", new byte[0]);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("AnyName", "AnyDescription", 1, 4, 1, 0, true));
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
        {
            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    "redis-server.*--version",
                    "Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "redis");
                
                this.mockFixture.Tracking.AssertCommandsExecuted("redis-server.*--version");
            }
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformMultiVM(string profile)
        {
            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    "redis-server.*--version",
                    "Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123");

            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient("1.2.3.5", ipAddress);

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

            await apiClient.CreateStateAsync(nameof(ServerState), state, CancellationToken.None);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);

                this.mockFixture.Tracking.AssertCommandsExecutedInOrder(
                    $"sudo chmod \\+x.*redis-server",
                    $"sudo bash -c \\\"numactl -C 0.*redis-server --port 6379.*\\\"",
                    $"sudo bash -c \\\"numactl -C 1.*redis-server --port 6380.*\\\"",
                    $"sudo bash -c \\\"numactl -C 2.*redis-server --port 6381.*\\\"",
                    $"sudo bash -c \\\"numactl -C 3.*redis-server --port 6382.*\\\"");

                this.mockFixture.Tracking.AssertCommandExecutedTimes("chmod.*redis-server", 1);
                this.mockFixture.Tracking.AssertCommandExecutedTimes("numactl.*redis-server", 4);
            }
        }
    }
}
