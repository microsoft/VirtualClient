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
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MySQLServerConfigurationTests
    {
        private MockFixture mockFixture;
        private MySQLServerConfiguration.ConfigurationState mockState;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetUpDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockState = new MySQLServerConfiguration.ConfigurationState("StartServer");
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MySQLServerConfiguration.Scenario), "StartMySQLServer" },
                { nameof(MySQLServerConfiguration.Action), "StartServer" },
                { nameof(MySQLServerConfiguration.DatabaseName), "mysqltest" },
            };

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForStartServerCommand()
        {
            this.SetUpDefaultBehavior();
            string expectedCommand = "sudo systemctl start mysql.service";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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
            this.SetUpDefaultBehavior();
            this.mockFixture.Parameters["Action"] = "StartServer";

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.OnHasExited = () => true;

            using TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture);
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForCreateDatabaseCommand()
        {
            this.SetUpDefaultBehavior();
            this.mockFixture.Parameters["Action"] = "CreateDatabase";
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
        public void MySQLServerConfigurationThrowsExceptionWhenCreateDatabaseProcessIsErrored()
        {
            this.SetUpDefaultBehavior();
            this.mockFixture.Parameters["Action"] = "CreateDatabase";

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.OnHasExited = () => true;

            using TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture);
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForRaiseMaxStatementCountCommand()
        {
            this.SetUpDefaultBehavior();
            this.mockFixture.Parameters["Action"] = "RaisedStatementCount";

            string expectedCommand = $"sudo mysql --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\"";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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
            this.SetUpDefaultBehavior();
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
            this.SetUpDefaultBehavior();
            this.mockFixture.Parameters["Action"] = "CreateUser";

            string[] expectedCommands =
            {
                $"sudo mysql --execute=\"DROP USER IF EXISTS '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'127.0.0.1'\"",
                $"sudo mysql --execute=\"CREATE USER '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'127.0.0.1'\"",
                $"sudo mysql --execute=\"GRANT ALL ON {this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}.* TO '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'127.0.0.1'\"",
                $"sudo mysql --execute=\"DROP USER IF EXISTS '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'1.2.3.5'\"",
                $"sudo mysql --execute=\"CREATE USER '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'1.2.3.5'\"",
                $"sudo mysql --execute=\"GRANT ALL ON {this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}.* TO '{this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]}'@'1.2.3.5'\""
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
        public async Task MySQLConfigurationExecutesTheExpectedProcessForChangeDataDirectoryCommand()
        {
            this.SetUpDefaultBehavior();
            this.mockFixture.Parameters["Action"] = "ChangeDataDirectory";

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);

            string[] expectedCommands =
            {
                $"sudo systemctl stop mysql.service",
                $"sudo rsync -av /var/lib/mysql /dev/sdd1",
                $"sudo mv /var/lib/mysql /var/lib/mysql.bak",
                $"sudo sed -i \"s|.*# datadir.*|datadir = /dev/sdd1/mysql/|\" /etc/mysql/mysql.conf.d/mysqld.cnf",
                $"sudo sed -i \"s|.*# alias.*|alias /var/lib/mysql -> /dev/sdd1/mysql/|\" /etc/mysql/mysql.conf.d/mysqld.cnf",
                $"sudo systemctl restart apparmor",
                $"sudo mkdir /var/lib/mysql/mysql -p",
                $"sudo rm -Rf /var/lib/mysql.bak",
                $"sudo systemctl start mysql.service",
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