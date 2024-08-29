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
    using static VirtualClient.Actions.MemcachedExecutor;

    [TestFixture]
    [Category("Functional")]
    public class MemcachedServerProfileTests
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
            this.fixture.SetupFile("memcached", "memcached-1.6.17/memcached", new byte[0]);
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                    return process;
                };

                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "memcached");
            }
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformMultiVM(string profile)
        {
            this.fixture.SystemManagement.Setup(mgr => mgr.GetLoggedInUserName())
                            .Returns("mockuser");

            IEnumerable<string> expectedCommands = new List<string>
            {
                $"sudo -u mockuser bash -c \"numactl -C {string.Join(",", Enumerable.Range(0, Environment.ProcessorCount))} /.+/memcached -p 6379 -t 4 -m 30720 -c 16384 :: &\""
            };

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
                    CpuAffinity = "0,1,2,3",
                    Port = 6379
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
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }
    }
}
