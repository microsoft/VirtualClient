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
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class RallyProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverOneAgentId;
        private string serverTwoAgentId;
        private string packagePath;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();

            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverOneAgentId = $"{Environment.MachineName}-Server1";
            this.serverTwoAgentId = $"{Environment.MachineName}-Server2";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-ELASTICSEARCH-RALLY.json")]
        public void ElasticsearchRallyWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-ELASTICSEARCH-RALLY.json", PlatformID.Unix, Architecture.X64)]
        public void ElasticsearchRallyWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);
            this.fixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        [Test]
        [TestCase("PERF-ELASTICSEARCH-RALLY.json", PlatformID.Unix, Architecture.X64)]
        public async Task ElasticsearchRallyWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Server"));

            this.fixture.SystemManagement.Setup(mgr => mgr.GetLoggedInUserName())
                .Returns("mockuser");

            this.fixture.SetupDisks(withUnformatted: true);

            DependencyPath rallyPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.packagePath = this.fixture.ToPlatformSpecificPath(rallyPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("esrally");
            this.fixture.SetupDirectory(this.packagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(numServers: 0);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.DisksAreInitialized(this.fixture);
                WorkloadAssert.DisksHaveAccessPaths(this.fixture);

                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "esrally");

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-ELASTICSEARCH-RALLY.json", PlatformID.Unix, Architecture.X64)]
        public async Task ElasticsearchRallyWorkloadProfileExecutesTheExpectedWorkloadsOnNodeUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverOneAgentId, "1.2.3.5", "Server"));

            this.fixture.SystemManagement.Setup(mgr => mgr.GetLoggedInUserName())
                .Returns("mockuser");

            this.SetupApiClient(this.serverOneAgentId, serverIPAddress: "1.2.3.5");

            this.fixture.SetupDisks(withUnformatted: true);

            DependencyPath rallyPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.packagePath = this.fixture.ToPlatformSpecificPath(rallyPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("esrally");
            this.fixture.SetupDirectory(this.packagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(numServers: 1);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-ELASTICSEARCH-RALLY.json", PlatformID.Unix, Architecture.X64)]
        public async Task ElasticsearchRallyWorkloadProfileExecutesTheExpectedWorkloadsOnClusterUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverOneAgentId, "1.2.3.5", "Server"),
                new ClientInstance(this.serverTwoAgentId, "1.2.3.6", "Server"));

            this.fixture.SystemManagement.Setup(mgr => mgr.GetLoggedInUserName())
                .Returns("mockuser");

            this.SetupApiClient(this.serverOneAgentId, serverIPAddress: "1.2.3.5");
            this.SetupApiClient(this.serverTwoAgentId, serverIPAddress: "1.2.3.6");

            this.fixture.SetupDisks(withUnformatted: true);

            DependencyPath rallyPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.packagePath = this.fixture.ToPlatformSpecificPath(rallyPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("esrally");
            this.fixture.SetupDirectory(this.packagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(numServers: 2);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(int numServers)
        {
            if (numServers == 0)
            {
                return new List<string>()
                {
                    "sudo apt update",
                    "sudo apt install python3 python3-pip git pbzip2 python3.10-venv --yes --quiet",
                    "sudo apt list python3",

                    $"python3 {this.packagePath}/install.py",
                    $"python3 {this.packagePath}/configure-server.py --directory /home/user/tools/VirtualClient/mnt_vc_0 --user mockuser --clientIp 127.0.0.1 --serverIp 127.0.0.1",
                    $"esrally race --track=geonames --distribution-version=8.0.0 --target-hosts=127.0.0.1:39200 --race-id=[0-9A-Fa-f]{{8}}-([0-9A-Fa-f]{{4}}-){{3}}[0-9A-Fa-f]{{12}}",
                };
            }
            else if (numServers == 1)
            {
                string currentDirectory = this.fixture.PlatformSpecifics.CurrentDirectory;

                return new List<string>()
                {
                    "sudo apt update",
                    "sudo apt install python3 python3-pip git pbzip2 python3.10-venv --yes --quiet",
                    "sudo apt list python3",

                    $"python3 {this.packagePath}/install.py",
                    $"python3 {this.packagePath}/configure-client.py --directory /home/user/tools/VirtualClient/mnt_vc_0 --user mockuser --clientIp 1.2.3.4",
                    $"esrally race --track=geonames --distribution-version=8.0.0 --target-hosts=1.2.3.5:39200 --race-id=[0-9A-Fa-f]{{8}}-([0-9A-Fa-f]{{4}}-){{3}}[0-9A-Fa-f]{{12}}",
                };
            }
            else
            {
                return new List<string>()
                {
                    "sudo apt update",
                    "sudo apt install python3 python3-pip git pbzip2 python3.10-venv --yes --quiet",
                    "sudo apt list python3",

                    $"python3 {this.packagePath}/install.py",
                    $"python3 {this.packagePath}/configure-client.py --directory /home/user/tools/VirtualClient/mnt_vc_0 --user mockuser --clientIp 1.2.3.4",
                    $"esrally race --track=geonames --distribution-version=8.0.0 --target-hosts=1.2.3.5:39200,1.2.3.6:39200 --race-id=[0-9A-Fa-f]{{8}}-([0-9A-Fa-f]{{4}}-){{3}}[0-9A-Fa-f]{{12}}",
                };
            }
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.fixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);
        }
    }
}
