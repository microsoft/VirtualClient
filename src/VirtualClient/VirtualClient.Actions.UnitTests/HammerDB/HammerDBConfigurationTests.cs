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
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.HammerDBExecutor;

    [TestFixture]
    [Category("Unit")]
    public class HammerDBConfigurationTests
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

            this.mockPackage = new DependencyPath("HammerDB", this.fixture.PlatformSpecifics.GetPackagePath("HammerDB"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockPackagePath = this.mockPackage.Path;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(HammerDBConfiguration.DatabaseName), "sbtest" },
                { nameof(HammerDBConfiguration.PackageName), "HammerDB" },
                { nameof(HammerDBConfiguration.Scenario), "populate_database" }
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task HammerDBConfigurationSkipsHammerDBInitialization()
        {
            HammerDBState expectedState = new HammerDBState(new Dictionary<string, IConvertible>
            {
                [nameof(HammerDBState.HammerDBInitialized)] = true,
            });

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(HammerDBState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedState));

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new HammerDBExecutor.HammerDBState()
            {
                HammerDBInitialized = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --tableCount 10 --recordCount 1000 --threadCount 8",
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

            using (TestHammerDBConfiguration HammerDBExecutor = new TestHammerDBConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task HammerDBConfigurationPreparesDatabase()
        {
            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.mockPackagePath}",
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --tableCount 10 --recordCount 1000 --threadCount 8",
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

            using (TestHammerDBConfiguration HammerDBExecutor = new TestHammerDBConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task HammerDBConfigurationUsesDefinedParametersWhenRunningTheWorkload()
        {
            this.fixture.Parameters[nameof(HammerDBConfiguration.Threads)] = "16";
            this.fixture.Parameters[nameof(HammerDBClientExecutor.Scenario)] = "Configure";

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.mockPackagePath}",
                $"python3 {this.mockPackagePath}/populate-database.py --dbName sbtest --tableCount 40 --recordCount 1000 --threadCount 16",
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

            using (TestHammerDBConfiguration HammerDBExecutor = new TestHammerDBConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task HammerDBConfigurationSkipsDatabasePopulationWhenInitialized()
        {
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new HammerDBExecutor.HammerDBState()
            {
                DatabasePopulated = true
            }));

            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --distro Ubuntu --packagePath {this.mockPackagePath}",
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

            using (TestHammerDBConfiguration HammerDBExecutor = new TestHammerDBConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private class TestHammerDBConfiguration : HammerDBConfiguration
        {
            public TestHammerDBConfiguration(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
