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

    [TestFixture]
    [Category("Functional")]
    public class DeathStarBenchClientProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverAgentId;

        [SetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.fixture.Setup(PlatformID.Unix, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            DeathStarBenchExecutor.StateConfirmationPollingTimeout = TimeSpan.FromSeconds(1);

            this.fixture.SetupWorkloadPackage("deathstarbench");
            this.fixture.SetupFile(@"/usr/local/bin/docker-compose");
        }

        [Test]
        [TestCase("PERF-NETWORK-DEATHSTARBENCH.json")]
        public async Task DeathStarBenchWorkloadProfileInstallsTheExpectedDependenciesOfClientOnUnixPlatform(string profile)
        {
            this.fixture.SystemManagement.SetupGet(sm => sm.AgentId).Returns(this.serverAgentId);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteActions = false;

                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations  
                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "deathstarbench");
            }
        }

        [Test]
        [Ignore("The complexity of this function test needs to be revisited")]
        [TestCase("PERF-NETWORK-DEATHSTARBENCH.json")]
        public async Task DeathStarBenchWorkloadProfileExecutesTheWorkloadAsExpectedOfClientOnUnixPlatformSingleVM(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                "make",
                @"bash -c ""./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/social-network/compose-post.lua http://localhost:8080/wrk2-api/post/compose -R 1000 >> results.txt""",
                @"bash -c ""./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/media-microservices/compose-review.lua http://localhost:8080/wrk2-api/review/compose -R 1000 >> results.txt""",
                @"bash -c ""./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/hotel-reservation/mixed-workload_type_1.lua http://0.0.0.0:5000 -R 1000 >> results.txt""",
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            this.fixture.Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.fixture.SystemManagement.SetupGet(sm => sm.AgentId).Returns(this.serverAgentId);
            this.SetupApiClient(serverIPAddress: "1.2.3.5");
            string[] expectedFiles = new string[]
            {
                @"Scripts",
                @"linux-x64/socialnetwork/wrk2",
                @"linux-x64/mediamicroservices/wrk2/wrk",
                @"linux-x64/hotelreservation/wrk2",
                @"linux-x64/socialnetwork/wrk2/results.txt",
                @"linux-x64/mediamicroservices/wrk2/results.txt",
                @"linux-x64/hotelreservation/wrk2/results.txt",
            };

            this.fixture.SetupWorkloadPackage("deathstarbench", expectedFiles: expectedFiles);
            this.fixture.SetupFile("deathstarbench/linux-x64/socialnetwork/wrk2", "results.txt", TestDependencies.GetResourceFileContents("Results_DeathStarBench.txt"));
            this.fixture.SetupFile(@"/usr/local/bin/docker-compose");

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);

                if (arguments == "bash /home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/scripts/isSwarmNode.sh")
                {
                    this.SetupApiClient(serverIPAddress: "1.2.3.5");
                }

                if (arguments.Contains("./wrk -D exp -t 20 -c 1000 -d 300s -L -s", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_DeathStarBench.txt"));
                }

                if (arguments == "bash -c \"docker ps | wc -l\"")
                {
                    process.StandardOutput.Append("1");
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-NETWORK-DEATHSTARBENCH.json")]
        public void DeathStarBenchWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(string profile)
        {
            this.fixture.PackageManager.Clear();
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        private void SetupApiClient(string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.fixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);

            DeathStarBenchState expectedState = new DeathStarBenchState("socialNetwork", true);

            apiClient.CreateStateAsync(nameof(DeathStarBenchState), expectedState, CancellationToken.None)
                .GetAwaiter().GetResult();

            State swarmCommand = new State(new Dictionary<string, IConvertible>
            {
                [nameof(DeathStarBenchExecutor.SwarmCommand)] = "mock command"
            });

            apiClient.CreateStateAsync(nameof(DeathStarBenchExecutor.SwarmCommand), swarmCommand, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }
}
