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
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class SysbenchProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverAgentId;
        private string sysbenchPackagePath;
        private string mySQLPackagePath;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json")]
        public void SysbenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json", PlatformID.Unix, Architecture.X64)]
        public void SysbenchWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
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
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json", PlatformID.Unix, Architecture.X64)]
        public async Task SysbenchWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");
            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupDirectory(this.sysbenchPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Sysbench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json", PlatformID.Unix, Architecture.X64)]
        public async Task SysbenchWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.sysbenchPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("sysbench");
            DependencyPath mySqlPackage = new DependencyPath("mysql-server", this.fixture.GetPackagePath("mysql-server"));
            this.mySQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupWorkloadPackage("mysql-server");

            this.fixture.SetupDirectory(this.sysbenchPackagePath);
            this.fixture.SetupDirectory(this.mySQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Sysbench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.DisksAreInitialized(this.fixture);
                WorkloadAssert.DisksHaveAccessPaths(this.fixture);

                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "sysbench");

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(bool singleVM)
        {
            if (singleVM) 
            {
                string currentDirectory = this.fixture.PlatformSpecifics.CurrentDirectory;

                return new List<string>()
                {
                    "apt install python3 --yes --quiet",

                    $"python3 {this.mySQLPackagePath}/install.py --distro Ubuntu",
                    $"python3 {this.mySQLPackagePath}/configure.py --serverIp 127.0.0.1 --innoDbDirs \"{currentDirectory}/mnt_vc_0;{currentDirectory}/mnt_vc_1;{currentDirectory}/mnt_vc_2;\"",
                    $"python3 {this.mySQLPackagePath}/setup-database.py --dbName sbtest",

                    $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.sysbenchPackagePath}",

                    $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --tableCount 10 --recordCount 1 --threadCount 8",
                    $"python3 {this.mySQLPackagePath}/distribute-database.py --dbName sbtest --directories \"{currentDirectory}/mnt_vc_0;{currentDirectory}/mnt_vc_1;{currentDirectory}/mnt_vc_2;\"",
                    $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --tableCount 10 --recordCount 1000 --threadCount 8",

                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_only --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_delete --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_insert --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_update_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_update_non_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload select_random_points --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload select_random_ranges --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300"
                };
            }
            else 
            { 
                return new List<string>()
                {
                    "apt install python3 --yes --quiet",

                    $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.sysbenchPackagePath}",

                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_only --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_delete --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_insert --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_update_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload oltp_update_non_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload select_random_points --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300",
                    $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --workload select_random_ranges --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300"
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
