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
    public class PostgreSQLInstallationTests
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

                string installScriptPath = this.mockFixture.Combine(this.mockPackage.Path, platformArchitecture, "ubuntu", "install.sh");

                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"sudo chmod +x \"{installScriptPath}\""));
                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"sudo bash -c \"VC_PASSWORD={installation.SuperuserPassword} sh {installScriptPath}\""));
            }
        }

        [Test]
        [TestCase(Architecture.X64, "win-x64")]
        [TestCase(Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLInstallationExecutesExpectedInstallationCommandsOnWindows(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT, architecture);

            using (TestPostgreSQLInstallation testPostgresqlInstallation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                testPostgresqlInstallation.Parameters[nameof(PostgreSQLInstallation.Password)] = "postgres";
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(this.mockFixture.ProcessManager.Commands.Contains(
                $"{this.mockPackage.Path}\\{platformArchitecture}\\postgresql.exe --mode \"unattended\" --serverport \"5432\" --superpassword \"postgres\""));
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task PostgreSQLInstallationUsesTheDefaultCredentialWhenTheServerPasswordIsNotDefinedByTheUser(PlatformID platform)
        {
            this.SetupDefaultMockBehavior(platform, Architecture.X64);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(f => f.EndsWith("superuser.txt")))).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(
                It.Is<string>(f => f.EndsWith("superuser.txt")),
                It.IsAny<CancellationToken>())).ReturnsAsync("defaultpwd");

            using (TestPostgreSQLInstallation installation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // The password is NOT defined.
                installation.Parameters.Remove(nameof(PostgreSQLInstallation.Password));

                await installation.ExecuteAsync(CancellationToken.None);
                Assert.AreEqual("defaultpwd", installation.SuperuserPassword);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "postgresql" },
                { "ServerPassword", "postgres" }
            };

            this.mockPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(f => f.EndsWith("superuser.txt")))).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(
                It.Is<string>(f => f.EndsWith("superuser.txt")),
                It.IsAny<CancellationToken>())).ReturnsAsync("defaultpwd");
        }

        private class TestPostgreSQLInstallation : PostgreSQLInstallation
        {
            public TestPostgreSQLInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new string SuperuserPassword => base.SuperuserPassword;

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
