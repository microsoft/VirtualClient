// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.MySqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MySQLServerConfigurationTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string packagePath;

        [SetUp]
        public void SetUpDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.fixture.Parameters["PackageName"] = "mysql-server";
            this.fixture.Parameters["Benchmark"] = "OLTP";

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.mockPackage = new DependencyPath("mysql-server", this.fixture.GetPackagePath("mysql-server"));
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.packagePath = this.fixture.ToPlatformSpecificPath(this.mockPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            IEnumerable<Disk> disks;
            disks = this.fixture.CreateDisks(PlatformID.Unix, true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForConfigureServerCommand()
        {
            this.fixture.Parameters["Action"] = "ConfigureServer";

            string[] expectedCommands =
            {
                $"python3 {this.packagePath}/configure.py --serverIp 1.2.3.4 --innoDbDirs \"/home/user/mnt_dev_sdc1/mysql:/home/user/mnt_dev_sdd1/mysql:/home/user/mnt_dev_sde1/mysql\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber++;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForConfigureServerCommandInMemoryScenario()
        {
            this.fixture.Parameters["Action"] = "ConfigureServer";
            this.fixture.Parameters["InMemory"] = true;

            // Mocking 8GB of memory
            this.fixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 8));

            string[] expectedCommands =
            {
                $"python3 {this.packagePath}/configure.py --serverIp 1.2.3.4 --innoDbDirs \"/home/user/mnt_dev_sdc1/mysql:/home/user/mnt_dev_sdd1/mysql:/home/user/mnt_dev_sde1/mysql\" " +
                    $"--inMemory 8192",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber++;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public void MySQLServerConfigurationThrowsExceptionWhenConfigureServerProcessIsErrored()
        {
            this.fixture.Parameters["Action"] = "ConfigureServer";
            this.fixture.Parameters["DatabaseName"] = "mysqltest";

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.OnHasExited = () => true;

            using TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationSkipsDatabaseCreationWhenOneExists()
        {
            this.fixture.Parameters["Action"] = "CreateDatabase";

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(
                new MySQLServerConfiguration.ConfigurationState(MySQLServerConfiguration.ConfigurationAction.CreateDatabase)));

            int commandsExecuted = 0;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                commandsExecuted++;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForCreateDatabaseCommand()
        {
            this.fixture.Parameters["Action"] = "CreateDatabase";
            this.fixture.Parameters["DatabaseName"] = "mysql-test";

            string[] expectedCommands =
            {
                $"python3 {this.packagePath}/setup-database.py --dbName mysql-test --clientIps \"1.2.3.5;\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber++;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForRaiseMaxStatementCountCommand()
        {
            this.fixture.Parameters["Action"] = "SetGlobalVariables";
            this.fixture.Parameters["Variables"] = "MAX_PREPARED_STMT_COUNT=100000;MAX_CONNECTIONS=1024";

            string[] expectedCommands =
            {
                $"python3 {this.packagePath}/set-global-variables.py --variables \"MAX_PREPARED_STMT_COUNT=100000;MAX_CONNECTIONS=1024\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber++;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForDistributeDatabaseCommand()
        {
            this.fixture.Parameters["Action"] = "DistributeDatabase";
            this.fixture.Parameters["DatabaseName"] = "mysql-test";
            this.fixture.Parameters["TableCount"] = "10";

            string[] expectedCommands =
            {
                $"python3 {this.packagePath}/distribute-database.py --dbName mysql-test --directories \"/home/user/mnt_dev_sdc1/mysql:/home/user/mnt_dev_sdd1/mysql:/home/user/mnt_dev_sde1/mysql\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber++;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                return process;
            };

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private class TestMySQLServerConfiguration : MySQLServerConfiguration
        {
            public TestMySQLServerConfiguration(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}