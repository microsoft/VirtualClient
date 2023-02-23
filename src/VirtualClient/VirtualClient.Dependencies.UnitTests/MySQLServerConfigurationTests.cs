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
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MySQLServerConfigurationTests
    {
        private MockFixture mockFixture;
        private MySQLServerConfiguration.ConfigurationState mockState;

        [SetUp]
        public void SetUpDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockState = new MySQLServerConfiguration.ConfigurationState("StartServer");
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MySQLServerConfiguration.Scenario), "StartMySQLServer" },
                { nameof(MySQLServerConfiguration.Action), "StartServer" },
                { nameof(MySQLServerConfiguration.DatabaseName), "sbtest" }
            };
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForStartServerCommand()
        {
            string[] expectedCommands =
            {
                "net start mysql",
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe}")
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
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForCreateDatabaseCommand()
        {
            this.mockFixture.Parameters["Action"] = "CreateDatabase";
            string[] expectedCommands =
            {
                $"C:\\tools\\mysql\\current\\bin\\mysql.exe --execute=\"DROP DATABASE IF EXISTS {this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]};\" --user=root",
                $"C:\\tools\\mysql\\current\\bin\\mysql.exe --execute=\"CREATE DATABASE {this.mockFixture.Parameters[nameof(MySQLServerConfiguration.DatabaseName)]};\" --user=root"
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe}")
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
            this.mockFixture.Parameters["Action"] = "CreateDatabase";

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.OnHasExited = () => true;

            using TestMySQLServerConfiguration component = new TestMySQLServerConfiguration(this.mockFixture);
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public async Task MySQLConfigurationExecutesTheExpectedProcessForCreateUserCommand()
        {
            this.mockFixture.Parameters["Action"] = "CreateUser";
            string[] expectedCommands =
            {
                $"C:\\tools\\mysql\\current\\bin\\mysql.exe --execute=\"DROP USER 'sbtest'@'1.2.3.5'\"",
                $"C:\\tools\\mysql\\current\\bin\\mysql.exe --execute=\"CREATE USER 'sbtest'@'1.2.3.5'\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe}")
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
        public async Task MySQLConfigurationExecutesTheExpectedProcessForRaiseMaxStatementCountCommand()
        {
            this.mockFixture.Parameters["Action"] = "RaisedStatementCount";
            string expectedCommand = $"C:\\tools\\mysql\\current\\bin\\mysql.exe --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\" --user=root";

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe}")
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
                $"sed -i \"s/.*bind-address.*/bind-address = 1.2.3.4/\" /etc/mysql/mysql.conf.d/mysqld.cnf",
                $"net stop mysql",
                $"net start mysql"
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe}")
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
        public async Task MySQLConfigurationExecutesTheExpectedProcessForGrantPrivilegesCommand()
        {
            this.mockFixture.Parameters["Action"] = "GrantPrivileges";
            string expectedCommand = $"C:\\tools\\mysql\\current\\bin\\mysql.exe --execute=\"GRANT ALL ON sbtest.* TO 'sbtest'@'1.2.3.5'\"";

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe}")
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