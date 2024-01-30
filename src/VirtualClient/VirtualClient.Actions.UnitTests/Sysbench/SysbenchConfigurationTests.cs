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
    using static VirtualClient.Actions.SysbenchExecutor;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchConfigurationTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private IEnumerable<Disk> disks;
        private string scriptPath;
        private string mockPackagePath;
        private string mountPaths;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.fixture.SetupMocks();

            this.mockPackage = new DependencyPath("sysbench", this.fixture.PlatformSpecifics.GetPackagePath("sysbench"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockPackagePath = this.mockPackage.Path;
            this.scriptPath = this.fixture.PlatformSpecifics.GetScriptPath("sysbench");

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SysbenchConfiguration.DatabaseName), "sbtest" },
                { nameof(SysbenchConfiguration.PackageName), "sysbench" },
                { nameof(SysbenchConfiguration.Scenario), "populate_database" }
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 4, 8, 4, 4, false));

            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);
            this.mountPaths = "/dev/sdd1 /dev/sde1 /dev/sdf1";
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
                $"sudo chmod -R 2777 \"{this.scriptPath}\"",
                $"sudo {this.scriptPath}/distribute-database.sh {this.mockPackagePath} sbtest 10 99999 1 {this.mountPaths}",
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
                $"sudo chmod -R 2777 \"{this.scriptPath}\"",
                $"sudo sed -i \"s/CREATE TABLE/CREATE TABLE IF NOT EXISTS/g\" {this.mockPackagePath}/src/lua/oltp_common.lua",
                "sudo ./autogen.sh",
                "sudo ./configure",
                "sudo make -j",
                "sudo make install",
                $"sudo {this.scriptPath}/distribute-database.sh {this.mockPackagePath} sbtest 10 99999 1 {this.mountPaths}",
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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchConfigurationUsesDefinedParametersWhenRunningTheWorkload()
        {
            this.fixture.Parameters[nameof(SysbenchConfiguration.Threads)] = "8";
            this.fixture.Parameters[nameof(SysbenchConfiguration.RecordCount)] = "1000";
            this.fixture.Parameters[nameof(SysbenchConfiguration.NumTables)] = "40";

            string[] expectedCommands =
            {
                $"sudo chmod -R 2777 \"{this.scriptPath}\"",
                $"sudo sed -i \"s/CREATE TABLE/CREATE TABLE IF NOT EXISTS/g\" {this.mockPackagePath}/src/lua/oltp_common.lua",
                "sudo ./autogen.sh",
                "sudo ./configure",
                "sudo make -j",
                "sudo make install",
                $"sudo {this.scriptPath}/distribute-database.sh {this.mockPackagePath} sbtest 40 999 8 {this.mountPaths}",
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

            using (TestSysbenchConfiguration SysbenchExecutor = new TestSysbenchConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchOLTPClientExecutorSkipsDatabasePopulationWhenInitialized()
        {
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                DatabasePopulated = true
            }));

            string[] expectedCommands =
            {
                $"sudo chmod -R 2777 \"{this.scriptPath}\"",
                $"sudo sed -i \"s/CREATE TABLE/CREATE TABLE IF NOT EXISTS/g\" {this.mockPackagePath}/src/lua/oltp_common.lua",
                "sudo ./autogen.sh",
                "sudo ./configure",
                "sudo make -j",
                "sudo make install",
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
