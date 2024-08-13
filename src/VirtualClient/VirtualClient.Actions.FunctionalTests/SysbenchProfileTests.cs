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
    public class SysbenchProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverAgentId;
        private string sysbenchPackagePath;
        private string mySQLPackagePath;
        private string postgreSQLPackagePath;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json")]
        public void MySQLSysbenchOLTPWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public void MySQLSysbenchOLTPWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
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
        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public async Task MySQLSysbenchOLTPWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");
            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupDirectory(this.sysbenchPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false, benchmark: "OLTP", databaseSystem: "MySQL");

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
        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public async Task MySQLSysbenchOLTPWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Server"));

            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.sysbenchPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("sysbench");
            DependencyPath mySqlPackage = new DependencyPath("mysql-server", this.fixture.GetPackagePath("mysql-server"));
            this.mySQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupWorkloadPackage("mysql-server");

            this.fixture.SetupDirectory(this.sysbenchPackagePath);
            this.fixture.SetupDirectory(this.mySQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true, benchmark: "OLTP", databaseSystem: "MySQL");

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

        [Test]
        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json")]
        public void MySQLSysbenchTPCCWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public void MySQLSysbenchTPCCWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
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
        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public async Task MySQLSysbenchTPCCWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");
            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupDirectory(this.sysbenchPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false, benchmark: "TPCC", databaseSystem: "MySQL");

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
        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public async Task MySQLSysbenchTPCCWorkloadProfileExecutesTheExpectedWorkloadsOnMySQLSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Server"));

            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.sysbenchPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("sysbench");
            DependencyPath mySqlPackage = new DependencyPath("mysql-server", this.fixture.GetPackagePath("mysql-server"));
            this.mySQLPackagePath = this.fixture.ToPlatformSpecificPath(mySqlPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupWorkloadPackage("mysql-server");

            this.fixture.SetupDirectory(this.sysbenchPackagePath);
            this.fixture.SetupDirectory(this.mySQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true, benchmark: "TPCC", databaseSystem: "MySQL");

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

        [Test]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-OLTP.json")]
        public void PostgreSQLSysbenchOLTPWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public void PostgreSQLSysbenchOLTPWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
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
        [TestCase("PERF-POSTGRESQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public async Task PostgreSQLSysbenchOLTPWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");
            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupDirectory(this.sysbenchPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false, benchmark: "OLTP", databaseSystem: "PostgreSQL");

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
        [TestCase("PERF-POSTGRESQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public async Task PostgreSQLSysbenchOLTPWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Server"));

            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.sysbenchPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("sysbench");
            DependencyPath postgreSQLPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.postgreSQLPackagePath = this.fixture.ToPlatformSpecificPath(postgreSQLPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupWorkloadPackage("postgresql", new Dictionary<string, IConvertible>() { { $"InstallationPath-{this.fixture.PlatformSpecifics.PlatformArchitectureName}", "/etc/postgresql/14/main" } });

            this.fixture.SetupDirectory(this.sysbenchPackagePath);
            this.fixture.SetupDirectory(this.postgreSQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true, benchmark: "OLTP", databaseSystem: "PostgreSQL");

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

        [Test]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json")]
        public void PostgreSQLSysbenchTPCCWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public void PostgreSQLSysbenchTPCCWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
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
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public async Task PostgreSQLSysbenchTPCCWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");
            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupDirectory(this.sysbenchPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false, benchmark: "TPCC", databaseSystem: "PostgreSQL");

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
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json", PlatformID.Unix, Architecture.X64)]
        public async Task PostgreSQLSysbenchTPCCWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Server"));

            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.sysbenchPackagePath = this.fixture.PlatformSpecifics.GetPackagePath("sysbench");
            DependencyPath postgreSQLPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.postgreSQLPackagePath = this.fixture.ToPlatformSpecificPath(postgreSQLPackage, platform, architecture).Path;

            this.fixture.SetupWorkloadPackage("sysbench");
            this.fixture.SetupWorkloadPackage("postgresql", new Dictionary<string, IConvertible>() { { $"InstallationPath-{this.fixture.PlatformSpecifics.PlatformArchitectureName}", "/etc/postgresql/14/main" } });

            this.fixture.SetupDirectory(this.sysbenchPackagePath);
            this.fixture.SetupDirectory(this.postgreSQLPackagePath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true, benchmark: "TPCC", databaseSystem: "PostgreSQL");

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

        private IEnumerable<string> GetProfileExpectedCommands(bool singleVM, string databaseSystem, string benchmark)
        {
            if (databaseSystem == "MySQL")
            {
                if (singleVM)
                {
                    string currentDirectory = this.fixture.PlatformSpecifics.CurrentDirectory;

                    if (benchmark == "OLTP")
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",

                            $"python3 {this.mySQLPackagePath}/install.py --distro Ubuntu",
                            $"python3 {this.mySQLPackagePath}/configure.py --serverIp 127.0.0.1 --innoDbDirs \"/mnt_vc_0;/mnt_vc_1;/mnt_vc_2;\"",
                            $"python3 {this.mySQLPackagePath}/setup-database.py --dbName sbtest",

                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.sysbenchPackagePath}",

                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --tableCount 10 --recordCount 1 --threadCount 8 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.mySQLPackagePath}/distribute-database.py --dbName sbtest --directories \"/mnt_vc_0;/mnt_vc_1;/mnt_vc_2;\"",
                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --tableCount 10 --recordCount 1000 --threadCount 8",

                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_only --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_delete --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_insert --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_update_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_update_non_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload select_random_points --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload select_random_ranges --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+"
                        };
                    }
                    else
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",

                            $"python3 {this.mySQLPackagePath}/install.py --distro Ubuntu",
                            $"python3 {this.mySQLPackagePath}/configure.py --serverIp 127.0.0.1 --innoDbDirs \"/mnt_vc_0;/mnt_vc_1;/mnt_vc_2;\"",
                            $"python3 {this.mySQLPackagePath}/setup-database.py --dbName sbtest",

                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.sysbenchPackagePath}",

                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --tableCount 10 --warehouses 1 --threadCount 8 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.mySQLPackagePath}/distribute-database.py --dbName sbtest --directories \"/mnt_vc_0;/mnt_vc_1;/mnt_vc_2;\"",
                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --tableCount 10 --warehouses 100 --threadCount 8 --password [A-Za-z0-9+/=]+",

                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --workload tpcc --threadCount 8 --tableCount 10 --warehouses 100 --hostIpAddress 127.0.0.1 --durationSecs 1800 --password [A-Za-z0-9+/=]+",
                        };
                    }
                }
                else
                {
                    if (benchmark == "OLTP")
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",

                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.sysbenchPackagePath}",

                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_only --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_delete --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_insert --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_update_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_update_non_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload select_random_points --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload select_random_ranges --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+"
                        };
                    }
                    else
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",
                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.sysbenchPackagePath}",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --workload tpcc --threadCount 8 --tableCount 10 --warehouses 100 --hostIpAddress 1.2.3.5 --durationSecs 1800 --password [A-Za-z0-9+/=]+",
                        };
                    }
                }
            }
            else
            {
                if (singleVM)
                {
                    string currentDirectory = this.fixture.PlatformSpecifics.CurrentDirectory;

                    if (benchmark == "OLTP")
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",

                            $"python3 {this.postgreSQLPackagePath}/install-server.py",
                            $"python3 {this.postgreSQLPackagePath}/configure-server.py --dbName sbtest --serverIp 127.0.0.1 --password [A-Za-z0-9+/=]+ --port 5432 --inMemory [0-9]+",
                            $"python3 {this.postgreSQLPackagePath}/setup-database.py --dbName sbtest --password [A-Za-z0-9+/=]+ --port 5432",

                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem PostgreSQL --packagePath {this.sysbenchPackagePath}",

                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --tableCount 10 --recordCount 1 --threadCount 8 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.postgreSQLPackagePath}/distribute-database.py --dbName sbtest --directories \"/mnt_vc_0;/mnt_vc_1;/mnt_vc_2;\" --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --tableCount 10 --recordCount 1000 --threadCount 8 --password [A-Za-z0-9+/=]+",

                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_read_only --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_delete --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_insert --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_update_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_update_non_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload select_random_points --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload select_random_ranges --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 127.0.0.1 --durationSecs 300 --password [A-Za-z0-9+/=]+"
                        };
                    }
                    else
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",

                            $"python3 {this.postgreSQLPackagePath}/install-server.py",
                            $"python3 {this.postgreSQLPackagePath}/configure-server.py --dbName sbtest --serverIp 127.0.0.1 --password [A-Za-z0-9+/=]+ --port 5432 --inMemory [0-9]+",
                            $"python3 {this.postgreSQLPackagePath}/setup-database.py --dbName sbtest --password [A-Za-z0-9+/=]+ --port 5432",

                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem PostgreSQL --packagePath {this.sysbenchPackagePath}",

                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem PostgreSQL --benchmark TPCC --tableCount 10 --warehouses 1 --threadCount 8 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.postgreSQLPackagePath}/distribute-database.py --dbName sbtest --directories \"/mnt_vc_0;/mnt_vc_1;/mnt_vc_2;\" --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/populate-database.py --dbName sbtest --databaseSystem PostgreSQL --benchmark TPCC --tableCount 10 --warehouses 100 --threadCount 8 --password [A-Za-z0-9+/=]+",

                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark TPCC --workload tpcc --threadCount 8 --tableCount 10 --warehouses 100 --hostIpAddress 127.0.0.1 --durationSecs 1800 --password [A-Za-z0-9+/=]+",
                        };
                    }
                }
                else
                {
                    if (benchmark == "OLTP")
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",

                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem PostgreSQL --packagePath {this.sysbenchPackagePath}",

                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_read_only --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_delete --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_insert --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_update_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_update_non_index --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload select_random_points --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload select_random_ranges --threadCount 8 --tableCount 1 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 300 --password [A-Za-z0-9+/=]+"
                        };
                    }
                    else
                    {
                        return new List<string>()
                        {
                            "apt install python3 --yes --quiet",
                            $"python3 {this.sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem PostgreSQL --packagePath {this.sysbenchPackagePath}",
                            $"python3 {this.sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark TPCC --workload tpcc --threadCount 8 --tableCount 10 --warehouses 100 --hostIpAddress 1.2.3.5 --durationSecs 1800 --password [A-Za-z0-9+/=]+",
                        };
                    }
                }
            }
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.fixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);
        }
    }
}
