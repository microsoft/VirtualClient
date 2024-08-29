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
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class RedisServerProfileTests
    {
        private DependencyFixture fixture;

        [SetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            this.fixture
                .Setup(PlatformID.Unix, Architecture.X64, "Server01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.fixture.SetupWorkloadPackage("wget", expectedFiles: "linux-x64/wget2");
            this.fixture.SetupFile("redis", "redis-6.2.1/src/redis-server", new byte[0]);
            this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("AnyName", "AnyDescription", 1, 4, 1, 0, true));
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations  
                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "redis");
            }
        }

        [Test]
        [TestCase("PERF-REDIS.json")]
        public async Task RedisMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformMultiVM(string profile)
        {
            List<string> expectedCommands = new List<string>();

            int port = 6379;
            Enumerable.Range(0, 4).ToList().ForEach(core =>
                expectedCommands.Add($"sudo bash -c \"numactl -C {core} /.+/redis-server --port {port + core} --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save &\""));

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);
            IApiClient apiClient = this.fixture.ApiClientManager.GetOrCreateApiClient("1.2.3.5", ipAddress);

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

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }
    }
}
