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
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json")]
        public void HammerDBWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Unix, Architecture.Arm64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Win32NT, Architecture.Arm64)]
        public void HammerDBWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
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
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Unix, Architecture.Arm64)]

        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.hammerdbPackagePath = this.fixture.GetPackagePath("hammerdb");
            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupDirectory(this.hammerdbPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false);

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
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.hammerdbPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("hammerdb");
            DependencyPath mySqlPackage = new DependencyPath("postgresql-server", this.fixture.GetPackagePath("postgresql-server"));
            this.mySQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupWorkloadPackage("postgresql-server");

            this.fixture.SetupDirectory(this.hammerdbPackagePath);
            this.fixture.SetupDirectory(this.mySQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true);

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
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Win32NT, Architecture.Arm64)]

        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.hammerdbPackagePath = this.fixture.GetPackagePath("hammerdb");
            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupDirectory(this.hammerdbPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false);

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
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-PostgreSQL-HammerDB-TPCC.json", PlatformID.Win32NT, Architecture.Arm64)]
        public async Task HammerDBWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMWindowsPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.hammerdbPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("hammerdb");
            DependencyPath mySqlPackage = new DependencyPath("postgresql-server", this.fixture.GetPackagePath("postgresql-server"));
            this.mySQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.SetupWorkloadPackage("hammerdb");
            this.fixture.SetupWorkloadPackage("postgresql-server");

            this.fixture.SetupDirectory(this.hammerdbPackagePath);
            this.fixture.SetupDirectory(this.mySQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true);

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

        private IEnumerable<string> GetProfileExpectedCommands(bool singleVM)
        {
            int password = this.fixture.SystemManagement.Object.ExperimentId.GetHashCode();

            if (singleVM) 
            {
                string currentDirectory = this.fixture.PlatformSpecifics.CurrentDirectory;

                return new List<string>()
                {
                    "apt install python3 --yes --quiet",

                    $"python3 {this.mySQLPackagePath}/install.py",
                    $"python3 {this.mySQLPackagePath}/configure.py --port 5432",
                    $"python3 {this.mySQLPackagePath}/setup-database.py --databaseName hammerdbtest",

                    $"python3 {this.hammerdbPackagePath}/configure-workload-generator.py --pot 5432 --virtualUsers 1 --warehouseCount 1 --password {password}",

                    $"python3 {this.mySQLPackagePath}/distribute-database.py --dbName hammerdbtest --directories \"{currentDirectory}/mnt_vc_0;{currentDirectory}/mnt_vc_1;{currentDirectory}/mnt_vc_2;\"",
                    $"python3 {this.hammerdbPackagePath}/populate-database.py --databaseName hammerdbtest --createDBTCLPath createDB.tcl",

                    $"python3 {this.hammerdbPackagePath}/run-workload.py --dbName sbtest --runTransactionsTCLFilePath runTransactions.tcl",
                };
            }
            else 
            { 
                return new List<string>()
                {
                    "apt install python3 --yes --quiet",

                    $"python3 {this.hammerdbPackagePath}/configure-workload-generator.py --pot 5432 --virtualUsers 1 --warehouseCount 1 --password {password}",

                    $"python3 {this.hammerdbPackagePath}/run-workload.py --dbName sbtest --runTransactionsTCLFilePath runTransactions.tcl",
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
