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
    public class MemcachedClientProfileTests
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

            this.mockFixture.SetupWorkloadPackage("memtier", expectedFiles: "memtier_benchmark");
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public void MemcachedMemtierWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(string profile)
        {
            // We ensure the Client workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [Test]
        [TestCase("PERF-MEMCACHED.json")]
        public async Task MemcachedMemtierWorkloadProfileExecutesTheWorkloadAsExpectedOfClientOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                "--protocol memcache_text --threads 64 --clients 64 --ratio 1:0 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern S:S --run-count 1",
                "--protocol memcache_text --threads 64 --clients 64 --ratio 1:0 --data-size 1024 --pipeline 32 --key-minimum 1 --key-maximum 1000000 --key-prefix med --key-pattern S:S --run-count 1",
                "--protocol memcache_text --threads 64 --clients 64 --ratio 1:0 --data-size 10240 --pipeline 32 --key-minimum 1 --key-maximum 10000 --key-prefix lg --key-pattern S:S --run-count 1",
                "--protocol memcache_text --threads 8 --clients 32 --ratio 1:1 --data-size 32 --pipeline 100 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 8 --clients 32 --ratio 1:1 --data-size 1024 --pipeline 100 --key-minimum 1 --key-maximum 1000000 --key-prefix med --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 8 --clients 32 --ratio 1:1 --data-size 10240 --pipeline 100 --key-minimum 1 --key-maximum 10000 --key-prefix lg --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 16 --clients 32 --ratio 1:1 --data-size 32 --pipeline 100 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 16 --clients 32 --ratio 1:1 --data-size 1024 --pipeline 100 --key-minimum 1 --key-maximum 1000000 --key-prefix med --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 16 --clients 32 --ratio 1:1 --data-size 10240 --pipeline 100 --key-minimum 1 --key-maximum 10000 --key-prefix lg --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 32 --clients 32 --ratio 1:1 --data-size 32 --pipeline 100 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 32 --clients 32 --ratio 1:1 --data-size 1024 --pipeline 100 --key-minimum 1 --key-maximum 1000000 --key-prefix med --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 32 --clients 32 --ratio 1:10 --data-size 1024 --pipeline 100 --key-minimum 1 --key-maximum 1000000 --key-prefix med --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data",
                "--protocol memcache_text --threads 32 --clients 32 --ratio 1:1 --data-size 10240 --pipeline 100 --key-minimum 1 --key-maximum 10000 --key-prefix lg --key-pattern R:R --run-count 1 --print-percentiles 50,90,95,99,99.9 --random-data"
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments?.Contains("memtier_benchmark", StringComparison.OrdinalIgnoreCase) == true)
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_MemcachedMemtier.txt"));
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

            ServerState state = new ServerState(new Dictionary<string, IConvertible>
            {
                [nameof(ServerState.Ports)] = 6379
            });

            apiClient.CreateStateAsync(nameof(ServerState), state, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }
}
