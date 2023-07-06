// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.PostgreSQLExecutor;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLServerExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPostgreSqlPackage;
        private DependencyPath mockHammerDBPackage;

        public void SetupDefaults(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("mock Makefile");

            this.mockPostgreSqlPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"), metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-linux-arm64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
                [$"{PackageMetadata.InstallationPath}-win-arm64"] = "C:\\Program Files\\PostgreSQL\\14"
            });

            this.mockHammerDBPackage = new DependencyPath("hammerdb", this.mockFixture.GetPackagePath("hammerdb"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPostgreSqlPackage.Name,
                ["HammerDBPackageName"] = this.mockHammerDBPackage.Name,
                ["Benchmark"] = "tpcc",
                ["DatabaseName"] = "tpcc",
                ["ReuseDatabase"] = true,
                ["ServerPassword"] = "anyvalue",
                ["Port"] = 5431,
                ["UserCount"] = 100,
                ["WarehouseCount"] = 100
            };

            this.mockFixture.PackageManager.OnGetPackage("postgresql").ReturnsAsync(this.mockPostgreSqlPackage);
            this.mockFixture.PackageManager.OnGetPackage("hammerdb").ReturnsAsync(this.mockHammerDBPackage);

            // Setup:
            // The server will be checking for state objects. State is how the server communicates required information
            // to the client.
            PostgreSQLServerState state = new PostgreSQLServerState();
            this.mockFixture.ApiClient.OnGetState(nameof(PostgreSQLServerState))
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, new Item<PostgreSQLServerState>(nameof(PostgreSQLServerState), state)));

            this.mockFixture.ApiClient.OnUpdateState<PostgreSQLServerState>(nameof(PostgreSQLServerState))
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task PostgreSQLServerExecutorInitializeDependenciesAsExpectedOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);
            using (TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                // createDB.tcl
                string createDBTclCopyFromPath = this.mockFixture.Combine(this.mockHammerDBPackage.Path, "linux-x64", "benchmarks", "tpcc", "postgresql", "createDB.tcl");
                string createDBTclCopyToPath = this.mockFixture.Combine(this.mockHammerDBPackage.Path, "linux-x64", "createDB.tcl");

                this.mockFixture.File.Verify(f => f.Copy(createDBTclCopyFromPath, createDBTclCopyToPath, true));
            }
        }

        [Test]
        public void PostgreSQLServerExecutorThrowsOnFailingToSaveTheServerState()
        {
            this.SetupDefaults();

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ApiClient.OnUpdateState<PostgreSQLServerState>(nameof(PostgreSQLServerState))
                    .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(HttpStatusCode.BadRequest));

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.Http400BadRequestResponse, error.Reason);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorWritesTheExpectedInformationToTheServerState()
        {
            this.SetupDefaults();

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                bool confirmed = false;
                this.mockFixture.ApiClient.OnUpdateState<PostgreSQLServerState>(nameof(PostgreSQLServerState))
                    .Callback<string, object, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((stateId, state, token, retryPolicy) =>
                    {
                        Item<PostgreSQLServerState> actualState = state as Item<PostgreSQLServerState>;

                        // Based on setup at the top. On first call, the database has not been created yet.
                        Assert.IsNotNull(actualState);
                        Assert.IsTrue(actualState.Definition.DatabaseInitialized);
                        Assert.AreEqual(100, actualState.Definition.WarehouseCount);
                        Assert.AreEqual(100, actualState.Definition.UserCount);
                        Assert.AreEqual(executor.ClientUsername, actualState.Definition.UserName);
                        Assert.AreEqual(executor.ClientPassword, actualState.Definition.Password);
                        confirmed = true;
                    })
                    .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedCommandsOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT,Architecture.X64);
            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // C:\Program Files\PostgreSQL\14
                string postgreSqlInstallationPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-win-x64"].ToString();

                // e.g.
                // C:\Users\Any\VirtualClient\packages\postgresql
                string postgreSqlPackage = this.mockPostgreSqlPackage.Path;

                // e.g. 
                // C:\Users\Any\VirtualClient\packages\hammerdb
                string hammerDBPath = this.mockHammerDBPackage.Path;

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}
                    //
                    // Configure the PostgreSQL server for transactions (e.g. port, users).
                    $"{postgreSqlPackage}\\win-x64\\configure.cmd {executor.Port} --> {postgreSqlPackage}\\win-x64",

                    // Drop the TPCC database if it already exists.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"DROP DATABASE IF EXISTS tpcc;\" --> {postgreSqlInstallationPath}\\bin",

                    // Create the database and populate it with data.
                    $"{hammerDBPath}\\win-x64\\hammerdbcli.bat auto createDB.tcl --> {hammerDBPath}\\win-x64"
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedComandsOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);
            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // /etc/postgresql/14/main
                string postgreSqlInstallationPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-linux-x64"].ToString();

                // e.g.
                // C:\Users\Any\VirtualClient\packages\postgresql
                string postgreSqlPackage = this.mockPostgreSqlPackage.Path;

                // e.g. 
                // /home/user/VirtualClient/packages/hammerdb/linux-x64
                string hammerDBPath = this.mockHammerDBPackage.Path;

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}
                    //
                    // Configure the PostgreSQL server for transactions (e.g. port, users).
                    $"sudo {postgreSqlPackage}/linux-x64/ubuntu/configure.sh {executor.Port} --> {postgreSqlPackage}/linux-x64/ubuntu",

                    // Drop the TPCC database if it already exists.
                    $"sudo -u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\" --> {postgreSqlInstallationPath}",

                    // Create the database and populate it with data.
                    $"bash -c \"{hammerDBPath}/linux-x64/hammerdbcli auto createDB.tcl\" --> {hammerDBPath}/linux-x64",
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedComandsForBalancedScenario()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["StressScenario"] = "Balanced";

            IEnumerable<Disk> disks;
            disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            disks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(vol => (vol.AccessPaths as List<string>).Clear()));

            List<Tuple<DiskVolume, string>> mountPointsCreated = new List<Tuple<DiskVolume, string>>();

            this.mockFixture.DiskManager
                .Setup(mgr => mgr.CreateMountPointAsync(It.IsAny<DiskVolume>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<DiskVolume, string, CancellationToken>((volume, mountPoint, token) =>
                {
                    (volume.AccessPaths as List<string>).Add(mountPoint);
                })
                .Returns(Task.CompletedTask);

            string mountPaths = $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdc1")} " +
                $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdd1")} " +
                $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sde1")} " +
                $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdf1")}";

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // /etc/postgresql/14/main
                string postgreSqlInstallationPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-linux-x64"].ToString();

                // e.g.
                // C:\Users\Any\VirtualClient\packages\postgresql
                string postgreSqlPackage = this.mockPostgreSqlPackage.Path;

                // e.g. 
                // /home/user/VirtualClient/packages/hammerdb/linux-x64
                string hammerDBPath = this.mockHammerDBPackage.Path;

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}
                    //
                    // Configure the PostgreSQL server for transactions (e.g. port, users).
                    $"sudo {postgreSqlPackage}/linux-x64/ubuntu/configure.sh {executor.Port} --> {postgreSqlPackage}/linux-x64/ubuntu",

                    // Drop the TPCC database if it already exists.
                    $"sudo -u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\" --> {postgreSqlInstallationPath}",

                    // Create the database and populate it with data.
                    $"bash -c \"{hammerDBPath}/linux-x64/hammerdbcli auto createDB.tcl\" --> {hammerDBPath}/linux-x64",

                    // Configure the balanced scenario.
                    $"sudo {postgreSqlPackage}/linux-x64/ubuntu/balanced.sh {mountPaths} --> {postgreSqlPackage}/linux-x64/ubuntu",
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedComandsForInMemoryScenario()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["StressScenario"] = "InMemory";

            // Mocking 8GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 8));

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // /etc/postgresql/14/main
                string postgreSqlInstallationPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-linux-x64"].ToString();

                // e.g.
                // C:\Users\Any\VirtualClient\packages\postgresql
                string postgreSqlPackage = this.mockPostgreSqlPackage.Path;

                // e.g. 
                // /home/user/VirtualClient/packages/hammerdb/linux-x64
                string hammerDBPath = this.mockHammerDBPackage.Path;

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}
                    //
                    // Configure the PostgreSQL server for transactions (e.g. port, users).
                    $"sudo {postgreSqlPackage}/linux-x64/ubuntu/configure.sh {executor.Port} --> {postgreSqlPackage}/linux-x64/ubuntu",

                    // Drop the TPCC database if it already exists.
                    $"sudo -u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\" --> {postgreSqlInstallationPath}",

                    // Create the database and populate it with data.
                    $"bash -c \"{hammerDBPath}/linux-x64/hammerdbcli auto createDB.tcl\" --> {hammerDBPath}/linux-x64",

                    // Configure the in memory scenario.
                    $"sudo {postgreSqlPackage}/linux-x64/ubuntu/inmemory.sh 6144 --> {postgreSqlPackage}/linux-x64/ubuntu",
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task PostgreSQLServerExecutorUsesTheDefaultCredentialWhenTheServerPasswordIsNotDefinedByTheUser(PlatformID platform)
        {
            this.SetupDefaults(platform, Architecture.X64);

            this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(f => f.EndsWith("superuser.txt")))).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(
                It.Is<string>(f => f.EndsWith("superuser.txt")),
                It.IsAny<CancellationToken>())).ReturnsAsync("defaultpwd");

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // The password is NOT defined.
                executor.Parameters.Remove(nameof(PostgreSQLServerExecutor.Password));

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.AreEqual("defaultpwd", executor.ClientPassword);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorByDefaultDoesNotIncurTheCostToRebuildTheDatabaseOnSubsequentRuns()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                List<string> commandsExecuted = new List<string>();

                // The default behavior is to reuse the database.
                executor.Parameters[nameof(PostgreSQLServerExecutor.ReuseDatabase)] = true;
                this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandsExecuted.Add(process.FullCommand());

                PostgreSQLServerState state = new PostgreSQLServerState();
                this.mockFixture.ApiClient.OnGetStateSequence(nameof(PostgreSQLServerState))
                    .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(
                        HttpStatusCode.OK,
                        new Item<PostgreSQLServerState>(nameof(PostgreSQLServerState), new PostgreSQLServerState())))
                    .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(
                        HttpStatusCode.OK,
                        new Item<PostgreSQLServerState>(nameof(PostgreSQLServerState), new PostgreSQLServerState() { DatabaseInitialized = true })));

                await executor.ExecuteAsync(CancellationToken.None);
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(commandsExecuted.Count(cmd => cmd == $"sudo -u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\"") == 1);
                Assert.IsTrue(commandsExecuted.Count(cmd => cmd.EndsWith($"hammerdbcli auto createDB.tcl\"")) == 1);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorWillRebuildTheDatabaseOnSubsequentRunsWhenInstructed()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                List<string> commandsExecuted = new List<string>();

                // The default behavior is to reuse the database.
                executor.Parameters[nameof(PostgreSQLServerExecutor.ReuseDatabase)] = false;
                this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandsExecuted.Add(process.FullCommand());

                PostgreSQLServerState state = new PostgreSQLServerState();
                this.mockFixture.ApiClient.OnGetStateSequence(nameof(PostgreSQLServerState))
                    .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(
                        HttpStatusCode.OK,
                        new Item<PostgreSQLServerState>(nameof(PostgreSQLServerState), new PostgreSQLServerState())))
                    .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(
                        HttpStatusCode.OK,
                        new Item<PostgreSQLServerState>(nameof(PostgreSQLServerState), new PostgreSQLServerState() { DatabaseInitialized = true })));

                await executor.ExecuteAsync(CancellationToken.None);
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(commandsExecuted.Count(cmd => cmd == $"sudo -u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\"") == 2);
                Assert.IsTrue(commandsExecuted.Count(cmd => cmd.EndsWith($"hammerdbcli auto createDB.tcl\"")) == 2);
            }
        }

        private class TestPostgreSQLServerExecutor : PostgreSQLServerExecutor
        {
            public TestPostgreSQLServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
                base.StabilizationWait = TimeSpan.Zero;
            }

            public new string HammerDBPackagePath => base.HammerDBPackagePath;

            public new int UserCount => base.UserCount;

            public new string ClientPassword => base.ClientPassword;

            public new string ClientUsername => base.ClientUsername;

            public new TimeSpan StabilizationWait => base.StabilizationWait;

            public new string PostgreSqlInstallationPath => base.PostgreSqlInstallationPath;

            public new int WarehouseCount => base.WarehouseCount;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}