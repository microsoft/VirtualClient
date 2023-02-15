// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class DeathStarBenchServerProfileTests
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
        [TestCase("PERF-NETWORK-DEATHSTARBENCH.json")]
        public async Task DeathStarBenchWorkloadProfileInstallsTheExpectedDependenciesOfServerOnUnixPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.mockFixture.SystemManagement.SetupGet(sm => sm.AgentId).Returns(this.serverAgentId);
            this.mockFixture.SetupFile(@"/usr/local/bin/docker-compose");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteActions = false;

                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations  
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "deathstarbench");
            }
        }

        [Test]
        [TestCase("PERF-NETWORK-DEATHSTARBENCH.json")]
        public async Task DeathStarBenchWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnUnixPlatformSingleVM(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"docker-compose -f docker-compose.yml up -d",
                @"bash -c ""python3 scripts/init_social_graph.py --graph=socfb-Reed98 --limit=1000""",
                @"python3 scripts/write_movie_info.py -c datasets/tmdb/casts.json -m datasets/tmdb/movies.json",
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            this.mockFixture
                .Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId)
                .SetupLayout(new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.mockFixture.SystemManagement.SetupGet(sm => sm.AgentId).Returns(this.serverAgentId);

            string[] expectedFiles = new string[]
            {
                @"linux-x64/scripts",
                @"linux-x64/socialnetwork/wrk2",
                @"linux-x64/mediamicroservices/wrk2",
                @"linux-x64/mediamicroservices/wrk2/wrk",
                @"linux-x64/hotelreservation/wrk2",
            };

            this.mockFixture.SetupWorkloadPackage("deathstarbench", expectedFiles: expectedFiles);
            this.mockFixture.SetupFile(@"/usr/local/bin/docker-compose");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments == "bash -c \"docker ps | wc -l\"")
                {
                    process.StandardOutput.Append("1");
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-NETWORK-DEATHSTARBENCH.json")]
        public void DeathStarBenchWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(string profile)
        {
            this.mockFixture.PackageManager.Clear();

            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }
    }
}
