// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServerConfiguration;

    [TestFixture]
    [Category("Unit")]
    public class MySQLServerConfigurationTests
    {
        private MockFixture mockFixture;
        private string mysqlScriptPath;

        [SetUp]
        public void SetUpDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

            this.mysqlScriptPath = this.mockFixture.PlatformSpecifics.GetScriptPath("mysqlserverconfiguration");
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            // this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForStartServerCommand()
        {
            this.mockFixture.Parameters["Action"] = "StartServer";

            string[] expectedCommands =
            {
                "sudo systemctl start mysql.service"
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public void MySQLServerConfigurationThrowsExceptionWhenStartServerProcessIsErrored()
        {
            this.mockFixture.Parameters["Action"] = "StartServer";

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.OnHasExited = () => true;

            using TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForCreateDatabaseCommand()
        {
            this.mockFixture.Parameters["Action"] = "CreateDatabase";
            this.mockFixture.Parameters["DatabaseName"] = "mysqltest";

            string[] expectedCommands =
            {
                $"sudo mysql --execute=\"DROP DATABASE IF EXISTS {this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]};\"",
                $"sudo mysql --execute=\"CREATE DATABASE {this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]};\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationSkipsDatabaseCreationWhenOneExists()
        {
            this.mockFixture.Parameters["Action"] = "CreateDatabase";
            this.mockFixture.Parameters["DatabaseName"] = "mysqltest";

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(
                new MySQLServerConfiguration.ConfigurationState(MySQLServerConfiguration.ConfigurationAction.CreateDatabase)));

            int commandsExecuted = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        [Test]
        public void MySQLServerConfigurationThrowsExceptionWhenCreateDatabaseProcessIsErrored()
        {
            this.mockFixture.Parameters["Action"] = "CreateDatabase";
            this.mockFixture.Parameters["DatabaseName"] = "mysqltest";

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.OnHasExited = () => true;

            using TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForRaiseMaxStatementCountCommand()
        {
            this.mockFixture.Parameters["Action"] = "RaisedMaxStatementCount";

            string[] expectedCommands =
            {
                $"sudo mysql --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\""
            };
            
            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForRaiseMaxConnectionCountCommand()
        {
            this.mockFixture.Parameters["Action"] = "RaisedMaxConnectionCount";

            string[] expectedCommands =
            {
                $"sudo mysql --execute=\"SET GLOBAL MAX_CONNECTIONS=1024;\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForConfigureNetworkCommand()
        {
            this.mockFixture.Parameters["Action"] = "ConfigureNetwork";

            string[] expectedCommands =
            {
                $"sudo sed -i \"s/.*bind-address.*/bind-address = 1.2.3.4/\" /etc/mysql/mysql.conf.d/mysqld.cnf",
                $"sudo systemctl restart mysql.service"
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForCreateUserCommand()
        {
            this.mockFixture.Parameters["Action"] = "CreateUser";
            this.mockFixture.Parameters["DatabaseName"] = "mysqltest";

            string[] expectedCommands =
            {
                $"sudo mysql --execute=\"DROP USER IF EXISTS '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'localhost'\"",
                $"sudo mysql --execute=\"CREATE USER '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'localhost'\"",
                $"sudo mysql --execute=\"GRANT ALL ON *.* TO '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'localhost'\"",
                $"sudo mysql --execute=\"DROP USER IF EXISTS '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'1.2.3.5'\"",
                $"sudo mysql --execute=\"CREATE USER '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'1.2.3.5'\"",
                $"sudo mysql --execute=\"GRANT ALL ON *.* TO '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'1.2.3.5'\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForSetInnodbDirectoriesCommand()
        {
            this.mockFixture.Parameters["Action"] = "SetInnodbDirectories";

            IEnumerable<Disk> disks;
            disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            string[] expectedCommands =
            {
                $"sudo chmod -R 2777 \"{this.mysqlScriptPath}\"",
                $"sudo {this.mysqlScriptPath}/set-mysql-innodb-directories.sh /dev/sdd1 /dev/sde1 /dev/sdf1",   
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForPrepareInMemoryScenarioCommand()
        {
            this.mockFixture.Parameters["Action"] = "PrepareInMemoryScenario";

            // Mocking 8GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 8));

            string[] expectedCommands =
            {
                $"sudo sed -i \"s|.*key_buffer_size.*|key_buffer_size = 8192M|\" /etc/mysql/mysql.conf.d/mysqld.cnf",
                $"sudo systemctl restart mysql.service"
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private class TestMySQLServerConfiguration : MySQLServerConfiguration
        {
            public TestMySQLServerConfiguration(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}