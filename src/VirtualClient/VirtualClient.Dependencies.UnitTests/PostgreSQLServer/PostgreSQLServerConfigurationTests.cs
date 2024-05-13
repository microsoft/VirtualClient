// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServer;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLServerConfigurationTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string packagePath;

        [SetUp]
        public void SetupDefaultMockBehavior()
        {
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task PostgreSQLServerConfigurationExecutesTheExpectedProcessForConfigureServerCommand(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
            this.fixture.Parameters["Action"] = "ConfigureServer";

            string tempPackagePath;

            if (platform == PlatformID.Win32NT)
            {
                tempPackagePath = this.packagePath.Replace(@"\", @"\\");
            }
            else
            {
                tempPackagePath = this.packagePath;
            }

            string[] expectedCommands =
            {
                $"python3 {tempPackagePath}/configure-server.py --dbName hammerdbtest --password [A-Za-z0-9+/=]+ --port 5432 --inMemory [0-9]+",
            };

            int commandNumber = 0;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                string executedCommand = $"{exe} {arguments}";

                Assert.IsTrue(Regex.IsMatch(executedCommand, expectedCommand));
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

            using (TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public void PostgreSQLServerConfigurationThrowsExceptionWhenConfigureServerProcessIsErrored(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
            this.fixture.Parameters["Action"] = "ConfigureServer";

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                this.fixture.Process.ExitCode = 1;
                this.fixture.Process.OnHasExited = () => true;
                return this.fixture.Process;
            };

            using TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.fixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task PostgreSQLConfigurationSkipsDatabaseCreationWhenOneExists(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
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

            using (TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task PostgreSQLServerConfigurationExecutesTheExpectedProcessForDistributeDatabaseCommand(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);
            this.fixture.Parameters["Action"] = "DistributeDatabase";
            string expectedCommand;

            if (platform == PlatformID.Unix)
            {
                expectedCommand = $"python3 {this.packagePath}/distribute-database.py --dbName hammerdbtest --directories \"/dev/sdd1;/dev/sde1;/dev/sdf1;\" --password [A-Za-z0-9+/=]+";
            }
            else
            {
                string tempPackagePath = this.packagePath.Replace(@"\", @"\\");
                expectedCommand = $"python3 {tempPackagePath}/distribute-database.py --dbName hammerdbtest --directories \"D:\\\\;E:\\\\;F:\\\\;\" --password [A-Za-z0-9+/=]+";
            }

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string executedCommand = $"{exe} {arguments}";
                Assert.IsTrue(Regex.IsMatch(executedCommand, expectedCommand));

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

            using (TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.fixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void SetupDefaultBehavior(PlatformID platform, Architecture architecture)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform, architecture);
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "postgresql" },
                { "ServerPassword", "postgresqlpassword" },
                { "Port", 5432 },
                { "DatabaseName", "hammerdbtest" },
                { "SharedMemoryBuffer", "454567" }
            };

            this.mockPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.packagePath = this.fixture.ToPlatformSpecificPath(this.mockPackage, platform, architecture).Path;

            IEnumerable<Disk> disks;
            disks = this.fixture.CreateDisks(platform, true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);
        }

        private class TestPostgreSQLServerConfiguration : PostgreSQLServerConfiguration
        {
            public TestPostgreSQLServerConfiguration(MockFixture fixture)
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
