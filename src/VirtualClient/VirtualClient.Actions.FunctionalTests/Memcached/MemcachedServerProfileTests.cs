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

            this.mockFixture.SetupWorkloadPackage("wget", expectedFiles: "linux-x64/wget2");
            this.mockFixture.SetupFile("memcached", "memcached-1.6.17/memcached", new byte[0]);
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "memcached");
            }
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformMultiVM(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"sudo bash -c \"numactl -C {string.Join(",", Enumerable.Range(0, Environment.ProcessorCount))} /.+/memcached -p 6379 -t 4 -c 1000000 -m 4096\""
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient("1.2.3.5", ipAddress);

            ServerState state = new ServerState(new Dictionary<string, IConvertible>
            {
                [nameof(ServerState.Ports)] = 6379
            });

            await apiClient.CreateStateAsync(nameof(ServerState), state, CancellationToken.None);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) => this.mockFixture.CreateProcess(command, arguments, workingDir);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }
    }
}
