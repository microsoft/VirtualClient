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
                return new List<string>()
                {
                    "apt install python3 --yes --quiet",

                    $"python3 {this.mySQLPackagePath}/install.py --distro Ubuntu",
                    $"python3 {this.mySQLPackagePath}/configure.py --serverIp 127.0.0.1 --innoDbDirs \"mountPoint0;mountPoint1;mountPoint2;\"",
                    $"python3 {this.mySQLPackagePath}/setup-database.py --dbName sbtest",

                    $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.sysbenchPackagePath}",

                    $"sudo {this.sysbenchPackagePath}/src/sysbench oltp_common --tables=10 --table-size=1 --threads=1 --mysql-db=sbtest prepare",
                    $"python3 {this.mySQLPackagePath}/distribute-database.py --dbName sbtest --tableCount 10 --directories \"mountPoint0;mountPoint1;mountPoint2;\"",
                    $"sudo {this.sysbenchPackagePath}/src/sysbench oltp_common --tables=10 --table-size=100000 --threads=1 --mysql-db=sbtest prepare",

                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_only --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_delete --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_insert --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_non_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_points --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_ranges --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run"
                };
            }
            else 
            { 
                return new List<string>()
                {
                    "apt install python3 --yes --quiet",

                    $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.sysbenchPackagePath}",

                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_only --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_delete --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_insert --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_non_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_points --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_ranges --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run"
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
