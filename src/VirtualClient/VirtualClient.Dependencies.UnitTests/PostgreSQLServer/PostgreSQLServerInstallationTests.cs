// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLServerInstallationTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string packagePath;

        public void SetupTest(PlatformID platform, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "postgresql" },
                { "ServerPassword", "postgres" },
                { "Port", 5432 }
            };

            this.mockPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));

            this.mockFixture.FileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.packagePath = this.mockFixture.ToPlatformSpecificPath(this.mockPackage, platform, architecture).Path;

            IEnumerable<Disk> disks;
            disks = this.mockFixture.CreateDisks(platform, true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void PostgreSQLServerInstallationThrowsIfDistroNotSupportedForLinux(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestPostgreSQLServerInstallation installation = new TestPostgreSQLServerInstallation(this.mockFixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => installation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLServerInstallationExecutesExpectedInstallationCommands(PlatformID platform, Architecture architecture, string platformArchitecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["Action"] = "InstallServer";
            DependencyPath dependencyPath;

            List<string> expectedCommands = new List<string>
            {
                $"python3 {this.packagePath}/install-server.py"
            };

            if (platform == PlatformID.Unix)
            {
                dependencyPath = new DependencyPath("postgresql", this.mockPackage.Path, null, null, new Dictionary<string, IConvertible>() { { $"InstallationPath-{platformArchitecture}", "/etc/postgresql/14/main" } });
            }
            else
            {
                dependencyPath = new DependencyPath("postgresql", this.mockPackage.Path, null, null, new Dictionary<string, IConvertible>() { { $"InstallationPath-{platformArchitecture}", "C:\\Program Files\\PostgreSQL\\14" } });
            }

            this.mockFixture.SetupPackage(dependencyPath);

            int commandNumber = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                Assert.IsTrue(expectedCommand == $"{exe} {arguments}");
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

            using (TestPostgreSQLServerInstallation component = new TestPostgreSQLServerInstallation(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private class TestPostgreSQLServerInstallation : PostgreSQLServerInstallation
        {
            public TestPostgreSQLServerInstallation(MockFixture fixture)
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
