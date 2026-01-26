// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.SysbenchExecutor;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchConfigurationTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string mockPackagePath;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.fixture.SetupMocks();

            this.mockPackage = new DependencyPath("sysbench", this.fixture.PlatformSpecifics.GetPackagePath("sysbench"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockPackagePath = this.mockPackage.Path;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SysbenchConfiguration.DatabaseSystem), "MySQL" },
                { nameof(SysbenchConfiguration.Action), "PopulateTables" },
                { nameof(SysbenchConfiguration.Benchmark), "OLTP" },
                { nameof(SysbenchConfiguration.DatabaseName), "sbtest" },
                { nameof(SysbenchConfiguration.PackageName), "sysbench" },
                { nameof(SysbenchConfiguration.Scenario), "populate_database" }
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task SysbenchConfigurationSkipsSysbenchInitialization()
        {
            SysbenchState expectedState = new SysbenchState(new Dictionary<string, IConvertible>
            {
                [nameof(SysbenchState.SysbenchInitialized)] = true,
            });

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(SysbenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedState));

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                SysbenchInitialized = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --threadCount 8 --tableCount 10 --recordCount 1000 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchConfigurationPreparesDatabase()
        {
            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.mockPackagePath}",
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --threadCount 8 --tableCount 10 --recordCount 1000 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchConfigurationUsesDefinedParametersWhenRunningTheWorkload()
        {
            this.fixture.Parameters[nameof(SysbenchConfiguration.Threads)] = "16";
            this.fixture.Parameters[nameof(SysbenchConfiguration.RecordCount)] = "1000";
            this.fixture.Parameters[nameof(SysbenchConfiguration.TableCount)] = "40";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "Configure";

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.mockPackagePath}",
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --threadCount 16 --tableCount 40 --recordCount 1000 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\"",
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchConfigurationThrowsErrorWhenDatabasePopulated()
        {
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                DatabasePopulated = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem MySQL --packagePath {this.mockPackagePath}",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => SysbenchExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.NotSupported);
            }
        }

        [Test]
        public async Task SysbenchConfigurationProperlyExecutesTPCCPreparation()
        {
            this.fixture.Parameters[nameof(SysbenchConfiguration.Benchmark)] = "TPCC";

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                SysbenchInitialized = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --threadCount 8 --tableCount 10 --warehouses 100 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SysbenchConfigurationProperlyExecutesTPCCConfigurablePreparation()
        {
            this.fixture.Parameters[nameof(SysbenchConfiguration.Benchmark)] = "TPCC";
            this.fixture.Parameters[nameof(SysbenchConfiguration.Threads)] = "16";
            this.fixture.Parameters[nameof(SysbenchConfiguration.WarehouseCount)] = "1000";
            this.fixture.Parameters[nameof(SysbenchConfiguration.TableCount)] = "40";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "Configure";

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                SysbenchInitialized = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --threadCount 16 --tableCount 40 --warehouses 1000 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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
            
            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SysbenchConfigurationProperlyExecutesPostgreSQLOLTPConfigurablePreparation()
        {
            this.fixture.Parameters[nameof(SysbenchConfiguration.DatabaseSystem)] = "PostgreSQL";
            this.fixture.Parameters[nameof(SysbenchConfiguration.Threads)] = "16";
            this.fixture.Parameters[nameof(SysbenchConfiguration.RecordCount)] = "1000";
            this.fixture.Parameters[nameof(SysbenchConfiguration.TableCount)] = "40";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "Configure";

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                SysbenchInitialized = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --threadCount 16 --tableCount 40 --recordCount 1000 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

        }

        [Test]
        public async Task SysbenchConfigurationProperlyExecutesPostgreSQLTPCCConfigurablePreparation()
        {
            this.fixture.Parameters[nameof(SysbenchConfiguration.DatabaseSystem)] = "PostgreSQL";
            this.fixture.Parameters[nameof(SysbenchConfiguration.Benchmark)] = "TPCC";
            this.fixture.Parameters[nameof(SysbenchConfiguration.Threads)] = "16";
            this.fixture.Parameters[nameof(SysbenchConfiguration.WarehouseCount)] = "1000";
            this.fixture.Parameters[nameof(SysbenchConfiguration.TableCount)] = "40";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "Configure";

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                SysbenchInitialized = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --databaseSystem PostgreSQL --benchmark TPCC --threadCount 16 --tableCount 40 --warehouses 1000 --password [A-Za-z0-9+/=]+ --host \"1.2.3.5\""
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
                commandNumber += 1;

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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

        }

        private class TestSysbenchConfiguration : SysbenchConfiguration
        {
            public TestSysbenchConfiguration(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
