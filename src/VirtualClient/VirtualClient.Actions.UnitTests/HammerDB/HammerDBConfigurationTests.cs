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
    using System.Runtime.InteropServices;
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
        public void SetupDefaultMockBehavior()
        {
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task HammerDBConfigurationSkipsHammerDBInitialization(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
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
                $"python3 {this.mockPackagePath}/populate-database.py --databaseName hammerdbtest --createDBTCLPath createDB.tcl"
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task HammerDBConfigurationPreparesDatabase(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
            string password = new TestHammerDBConfiguration(this.fixture.Dependencies, this.fixture.Parameters).SuperUserPassword;
            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --createDBTCLPath createDB.tcl --port 5432 --virtualUsers 1 --warehouseCount 1 --password {password} --databaseName hammerdbtest",
                $"python3 {this.mockPackagePath}/populate-database.py --databaseName hammerdbtest --createDBTCLPath createDB.tcl",
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task HammerDBConfigurationSkipsDatabasePopulationWhenInitialized(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new HammerDBExecutor.HammerDBState()
            {
                DatabasePopulated = true
            }));

            string password = new TestHammerDBConfiguration(this.fixture.Dependencies, this.fixture.Parameters).SuperUserPassword;
            string[] expectedCommands =
            {
                $"python3 {this.mockPackagePath}/configure-workload-generator.py --createDBTCLPath createDB.tcl --port 5432 --virtualUsers 1 --warehouseCount 1 --password {password} --databaseName hammerdbtest"
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

        public void SetupDefaultBehavior(PlatformID platform, Architecture architecture)
        {
             this.fixture = new MockFixture();
             this.fixture.Setup(platform, architecture);
             this.fixture.SetupMocks();

            this.mockPackage = new DependencyPath("HammerDB", this.fixture.PlatformSpecifics.GetPackagePath("HammerDB"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockPackagePath = this.mockPackage.Path;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(HammerDBConfiguration.DatabaseName), "hammerdbtest" },
                { nameof(HammerDBConfiguration.PackageName), "HammerDB" },
                { nameof(HammerDBConfiguration.Scenario), "populate_database" }
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.Parameters["Port"] = 5432;
            this.fixture.Parameters["VirtualUsers"] = 1;
            this.fixture.Parameters["warehouseCount"] = 1;
            this.fixture.Parameters["ServerPassword"] = "postgresqlpassword";
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
