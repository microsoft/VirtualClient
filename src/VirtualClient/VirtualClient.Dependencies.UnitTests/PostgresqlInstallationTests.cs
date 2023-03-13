// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PostgresqlInstallationTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        public void PostgreSQLInstallationThrowsIfDistroNotSupportedForLinux()
        {
            this.SetupDefaultMockBehavior();
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestPostgreSQLInstallation installation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => installation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        [TestCase(Architecture.X64, "linux-x64")]
        [TestCase(Architecture.Arm64, "linux-arm64")]
        public async Task PostgreSQLInstallationExecutesExpectedInstallationCommandsOnUbuntu(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestPostgreSQLInstallation installation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None);
            }

            Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted(
                $@"sudo bash {this.mockFixture.Combine(this.mockPackage.Path, platformArchitecture, "install_postgresql_ubuntu.sh")}"));
        }

        [Test]
        [TestCase(Architecture.X64, "linux-x64")]
        [TestCase(Architecture.Arm64, "linux-arm64")]
        public async Task PostgreSQLInstallationExecutesExpectedInstallationCommandsOnCentOS(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestCentOS",
                LinuxDistribution = LinuxDistribution.CentOS7
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestPostgreSQLInstallation testPostgresqlInstallation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted(
                $@"sudo bash {this.mockFixture.Combine(this.mockPackage.Path, platformArchitecture, "install_postgresql_rhel_centos.sh")}"));
        }

        [Test]
        [TestCase(Architecture.X64, "linux-x64")]
        [TestCase(Architecture.Arm64, "linux-arm64")]
        public async Task PostgreSQLInstallationExecutesExpectedInstallationCommandsOnRedHat(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestRedHat",
                LinuxDistribution = LinuxDistribution.RHEL7
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestPostgreSQLInstallation testPostgresqlInstallation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted(
                $@"sudo bash {this.mockFixture.Combine(this.mockPackage.Path, platformArchitecture, "install_postgresql_rhel_centos.sh")}"));
        }

        [Test]
        [TestCase(Architecture.X64, "win-x64")]
        [TestCase(Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLInstallationExecutesExpectedInstallationCommandsOnWindows(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT, architecture);

            using (TestPostgreSQLInstallation testPostgresqlInstallation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            IConvertible expectedVersion = this.mockFixture.Parameters["Version"];

            Assert.IsTrue(this.mockFixture.ProcessManager.Commands.Contains(
                $"{this.mockPackage.Path}\\{platformArchitecture}\\postgresql-{expectedVersion}.exe --mode \"unattended\" --serverport \"5432\" --superpassword \"postgres\""));
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", 14 },
                { "PackageName", "postgresql" }
            };

            this.mockPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
        }

        private class TestPostgreSQLInstallation : PostgreSQLInstallation
        {
            public TestPostgreSQLInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
