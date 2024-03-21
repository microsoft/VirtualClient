// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServer;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLServerInstallationTests
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
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void PostgreSQLServerInstallationThrowsIfDistroNotSupportedForLinux(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };

            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestPostgreSQLServerInstallation installation = new TestPostgreSQLServerInstallation(this.fixture))
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
            this.SetupDefaultBehavior(platform, architecture);
            this.fixture.Parameters["Action"] = "InstallServer";
            DependencyPath dependencyPath;
            if (platform == PlatformID.Unix)
            {
                dependencyPath = new DependencyPath("postgresql", this.mockPackage.Path, null, null, new Dictionary<string, IConvertible>() { { $"InstallationPath-{platformArchitecture}", "/etc/postgresql/14/main" } });
            }
            else
            {
                dependencyPath = new DependencyPath("postgresql", this.mockPackage.Path, null, null, new Dictionary<string, IConvertible>() { { $"InstallationPath-{platformArchitecture}", "C:\\Program Files\\PostgreSQL\\14" } });
            }

            this.fixture.SetupWorkloadPackage(dependencyPath);
                
            string[] expectedCommands =
            {
                $"python3 {this.packagePath}/install-server.py",
            };

            int commandNumber = 0;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.IsNotNull(state);
            });

            using (TestPostgreSQLServerInstallation component = new TestPostgreSQLServerInstallation(this.fixture))
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
                { "ServerPassword", "postgres" },
                { "Port", 5432 }
            };

            this.mockPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.packagePath = this.fixture.ToPlatformSpecificPath(this.mockPackage, platform, architecture).Path;

            IEnumerable<Disk> disks;
            disks = this.fixture.CreateDisks(platform, true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);
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
