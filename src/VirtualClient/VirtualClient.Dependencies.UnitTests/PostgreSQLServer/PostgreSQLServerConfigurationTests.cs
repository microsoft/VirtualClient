// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServer;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class PostgreSQLServerConfigurationTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string packagePath;
        private string originalPassword;

        [SetUp]
        public void SetPasswordEnvironmentVariable()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            Environment.SetEnvironmentVariable("PGPASSWORD", "test-password with 'quotes' and $symbols");
        }

        [TearDown]
        public void RestorePasswordEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
        }

        public void SetupTest(PlatformID platform, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "postgresql" },
                { "Port", 5432 },
                { "DatabaseName", "hammerdbtest" },
                { "SharedMemoryBuffer", "454567" }
            };

            this.mockPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));

            this.mockFixture.FileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.packagePath = this.mockFixture.ToPlatformSpecificPath(this.mockPackage, platform, architecture).Path;

            // Simulate the LVM striped volume that StripeDisks creates, with a raid0 access path.
            Disk stripedDisk = new Disk(
                4,
                "/dev/dm-0",
                new List<DiskVolume>
                {
                    new DiskVolume(
                        0,
                        "/dev/dm-0",
                        new List<string> { "/home/user/mnt_raid0" },
                        properties: new Dictionary<string, IConvertible>
                        {
                            { "size", "1234567890123" }
                        })
                });

            List<Disk> disks = new List<Disk>(this.mockFixture.CreateDisks(platform, true));
            disks.Add(stripedDisk);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task PostgreSQLServerConfigurationExecutesTheExpectedProcessForConfigureServerCommand(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["Action"] = "ConfigureServer";

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
                $"python3 {tempPackagePath}/configure-server.py --dbName hammerdbtest --serverIp 1.2.3.5 --port 5432 --inMemory [0-9]+ --directory \\S+",
            };

            int commandNumber = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                string executedCommand = $"{exe} {arguments}";

                Assert.IsTrue(Regex.IsMatch(executedCommand, expectedCommand));
                StringAssert.DoesNotContain("--password", arguments);
                StringAssert.DoesNotContain(Environment.GetEnvironmentVariable("PGPASSWORD"), arguments);
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

            using (TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public void PostgreSQLServerConfigurationThrowsExceptionWhenConfigureServerProcessIsErrored(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["Action"] = "ConfigureServer";

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                this.mockFixture.Process.ExitCode = 1;
                this.mockFixture.Process.OnHasExited = () => true;
                return this.mockFixture.Process;
            };

            using TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task PostgreSQLConfigurationSkipsDatabaseCreationWhenOneExists(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["Action"] = "CreateDatabase";

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

            using (TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        [Test]
        public void PostgreSQLServerConfigurationReadsPasswordFromEnvironment()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);

            using TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture);
            Assert.AreEqual(Environment.GetEnvironmentVariable("PGPASSWORD"), component.GetSuperUserPassword());
        }

        [Test]
        [TestCase("ConfigureServer")]
        [TestCase("SetupDatabase")]
        public void PostgreSQLServerConfigurationRejectsMissingPasswordBeforeStartingProcess(string action)
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["Action"] = action;
            Environment.SetEnvironmentVariable("PGPASSWORD", null);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                Assert.Fail("No process should start without PGPASSWORD.");
                return this.mockFixture.Process;
            };

            using TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.Contains("PGPASSWORD", exception.Message);
        }

        [Test]
        public async Task PostgreSQLServerConfigurationOmitsPasswordFromSetupDatabaseCommand()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["Action"] = "SetupDatabase";
            int commandsExecuted = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                Assert.AreEqual("python3", command);
                Assert.AreEqual($"{this.packagePath}/setup-database.py --dbName hammerdbtest --port 5432", arguments);
                StringAssert.DoesNotContain(Environment.GetEnvironmentVariable("PGPASSWORD"), arguments);
                commandsExecuted++;
                this.mockFixture.Process.ExitCode = 0;
                this.mockFixture.Process.OnHasExited = () => true;
                return this.mockFixture.Process;
            };

            using TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, commandsExecuted);
        }

        [Test]
        [TestCase("ConfigureServer")]
        [TestCase("SetupDatabase")]
        public async Task PostgreSQLServerConfigurationPassesFilePasswordOnlyToChildEnvironment(string action)
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["Action"] = action;
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = "/secure/Password.json";
            const string password = "file password with 'quotes' and $symbols";
            string originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync("/secure/Password.json", It.IsAny<CancellationToken>()))
                .ReturnsAsync(JObject.FromObject(new { Password = password }).ToString());
            int commandsStarted = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                StringAssert.DoesNotContain(password, arguments);
                StringAssert.DoesNotContain("--password", arguments);
                InMemoryProcess childProcess = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = command, Arguments = arguments },
                    ExitCode = 0,
                    OnHasExited = () => true
                };
                childProcess.OnStart = () =>
                {
                    Assert.AreEqual(password, childProcess.EnvironmentVariables["PGPASSWORD"]);
                    commandsStarted++;
                    return true;
                };
                return childProcess;
            };

            using TestPostgreSQLServerConfiguration component = new TestPostgreSQLServerConfiguration(this.mockFixture);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, commandsStarted);
            Assert.AreEqual(originalPassword, Environment.GetEnvironmentVariable("PGPASSWORD"));
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
