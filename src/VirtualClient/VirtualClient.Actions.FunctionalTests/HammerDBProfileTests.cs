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
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class HammerDBProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverAgentId;
        private string hammerdbPackagePath;
        private string postgreSQLPackagePath;

        [Test]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]
        public void HammerDBWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupMockFixture(platform, architecture);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]
        public void HammerDBWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupMockFixture(platform, architecture);
            this.fixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Unix, Architecture.X64)]

        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupMockFixture(platform, architecture);
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            DependencyPath hammerdbPackage = new DependencyPath("hammerdb", this.fixture.GetPackagePath("hammerdb"));
            this.hammerdbPackagePath = this.fixture.ToPlatformSpecificPath(hammerdbPackage, platform, architecture).Path;
            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupDirectory(this.hammerdbPackagePath);

            IEnumerable<string> expectedCommands = this.GetUnixProfileExpectedCommands(singleVM: false);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_HammerDB.txt"));
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
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupMockFixture(platform, architecture);
            this.fixture.SetupDisks(withUnformatted: true);

            DependencyPath hammerdbPackage = new DependencyPath("hammerdb", this.fixture.GetPackagePath("hammerdb"));
            this.hammerdbPackagePath = this.fixture.ToPlatformSpecificPath(hammerdbPackage, platform, architecture).Path;
            DependencyPath mySqlPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.postgreSQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupWorkloadPackage("postgresql", new Dictionary<string, IConvertible>() { { $"InstallationPath-{this.fixture.PlatformSpecifics.PlatformArchitectureName}", "/etc/postgresql/14/main" } });

            this.fixture.SetupDirectory(this.hammerdbPackagePath);
            this.fixture.SetupDirectory(this.postgreSQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetUnixProfileExpectedCommands(singleVM: true);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_HammerDB.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.DisksAreInitialized(this.fixture);
                WorkloadAssert.DisksHaveAccessPaths(this.fixture);

                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "hammerdb");

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]

        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupMockFixture(platform, architecture);
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            DependencyPath hammerdbPackage = new DependencyPath("hammerdb", this.fixture.GetPackagePath("hammerdb"));
            this.hammerdbPackagePath = this.fixture.ToPlatformSpecificPath(hammerdbPackage, platform, architecture).Path;
            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupDirectory(this.hammerdbPackagePath);

            IEnumerable<string> expectedCommands = this.GetWindowsProfileExpectedCommands(singleVM: false);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_HammerDB.txt"));
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
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]
        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMWindowsPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupMockFixture(platform, architecture);
            this.fixture.SetupDisks(withUnformatted: true);

            DependencyPath hammerdbPackage = new DependencyPath("hammerdb", this.fixture.GetPackagePath("hammerdb"));
            this.hammerdbPackagePath = this.fixture.ToPlatformSpecificPath(hammerdbPackage, platform, architecture).Path;
            DependencyPath mySqlPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.postgreSQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupWorkloadPackage("postgresql", new Dictionary<string, IConvertible>() { { $"InstallationPath-{this.fixture.PlatformSpecifics.PlatformArchitectureName}", "C:\\Program Files\\PostgreSQL\\14" } });

            this.fixture.SetupDirectory(this.hammerdbPackagePath);
            this.fixture.SetupDirectory(this.postgreSQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetWindowsProfileExpectedCommands(singleVM: true);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_HammerDB.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.DisksAreInitialized(this.fixture);   
                WorkloadAssert.DisksHaveAccessPaths(this.fixture);

                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "hammerdb");

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetUnixProfileExpectedCommands(bool singleVM)
        {
            if (singleVM) 
            {
                string currentDirectory = this.fixture.PlatformSpecifics.CurrentDirectory;

                return new List<string>()
                {
                    "sudo apt update",
                    "sudo apt install python3 --yes --quiet",
                    "sudo apt list python3",

                    $"python3 {this.postgreSQLPackagePath}/install-server.py",
                    $"python3 {this.postgreSQLPackagePath}/configure-server.py --dbName hammerdbtest --password [A-Za-z0-9+/=]+ --port 5432 --sharedMemoryBuffer [0-9]+",

                    $"python3 {this.hammerdbPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers [0-9]+ --warehouseCount [0-9]+ --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress",
                    $"python3 {this.hammerdbPackagePath}/populate-database.py --createDBTCLPath createDB.tcl",
                    
                    $"python3 {this.postgreSQLPackagePath}/distribute-database.py --dbName hammerdbtest --directories {currentDirectory}/mnt_vc_0;{currentDirectory}/mnt_vc_1;{currentDirectory}/mnt_vc_2; --password [A-Za-z0-9+/=]+",

                    $"python3 {this.hammerdbPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers [0-9]+ --warehouseCount [0-9]+ --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress",
                    $"python3 {this.hammerdbPackagePath}/populate-database.py --createDBTCLPath createDB.tcl",

                    $"python3 {this.hammerdbPackagePath}/run-workload.py --runTransactionsTCLFilePath runTransactions.tcl",
                };
            }
            else 
            { 
                return new List<string>()
                {
                    "sudo apt update",
                    "apt install python3 --yes --quiet",
                    "sudo apt list python3",

                    $"python3 {this.hammerdbPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers [0-9]+ --warehouseCount [0-9]+ --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress",

                    $"python3 {this.hammerdbPackagePath}/run-workload.py --runTransactionsTCLFilePath runTransactions.tcl",
                };
            }
        }

        private IEnumerable<string> GetWindowsProfileExpectedCommands(bool singleVM)
        {
            string temphammerdbPackagePath = this.hammerdbPackagePath.Replace(@"\", @"\\");
            if (singleVM)
            {
                string tempPostgreSqlPackagePath = this.postgreSQLPackagePath.Replace(@"\", @"\\");
                return new List<string>()
                {
                    $"python3 {this.postgreSQLPackagePath}/install-server.py",
                    $"python3 {tempPostgreSqlPackagePath}/configure-server.py --dbName hammerdbtest --password [A-Za-z0-9+/=]+ --port 5432 --sharedMemoryBuffer [0-9]+",

                    $"python3 {temphammerdbPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers [0-9]+ --warehouseCount [0-9]+ --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress",
                    $"python3 {this.hammerdbPackagePath}/populate-database.py --createDBTCLPath createDB.tcl",

                    $"python3 {tempPostgreSqlPackagePath}/distribute-database.py --dbName hammerdbtest --directories E:\\\\;F:\\\\;G:\\\\; --password [A-Za-z0-9+/=]+",

                    $"python3 {temphammerdbPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers [0-9]+ --warehouseCount [0-9]+ --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress",
                    $"python3 {this.hammerdbPackagePath}/populate-database.py --createDBTCLPath createDB.tcl",

                    $"python3 {this.hammerdbPackagePath}/run-workload.py --runTransactionsTCLFilePath runTransactions.tcl",
                };
            }
            else
            {
                return new List<string>()
                {
                    $"python3 {temphammerdbPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers [0-9]+ --warehouseCount [0-9]+ --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress",

                    $"python3 {this.hammerdbPackagePath}/run-workload.py --runTransactionsTCLFilePath runTransactions.tcl",
                };
            }
        }

        private void SetupMockFixture(PlatformID platform, Architecture architecture)
        {
            this.fixture = new DependencyFixture();
            this.fixture.Setup(platform, architecture);
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.fixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);
        }
    }
}
