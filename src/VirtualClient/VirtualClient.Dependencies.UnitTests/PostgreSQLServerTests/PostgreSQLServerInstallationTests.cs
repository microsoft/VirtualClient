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
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
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

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void PostgreSQLInstallationThrowsIfDistroNotSupportedForLinux(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
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
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLInstallationExecutesExpectedInstallationCommands(PlatformID platform, Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            if (platform == PlatformID.Unix)
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestUbuntu",
                    LinuxDistribution = LinuxDistribution.Ubuntu
                };

                this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(mockInfo);
            }

            using (TestPostgreSQLInstallation installation = new TestPostgreSQLInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None);

                string installScriptPath = this.mockFixture.Combine(this.mockPackage.Path, platformArchitecture, "installServer.py");

                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"python3 \"{installScriptPath}\""));
            }
        }

        [Test]
        [TestCase(Architecture.X64, "linux-x64")]
        [TestCase(Architecture.Arm64, "linux-arm64")]
        [TestCase(Architecture.X64, "win-x64")]
        [TestCase(Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLInstallationUsesTheDefaultCredentialWhenTheServerPasswordIsNotDefinedByTheUser(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

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

        private void SetupDefaultMockBehavior(PlatformID platform, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
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
