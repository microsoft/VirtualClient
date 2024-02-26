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
    public class PostgreSQLServerConfigurationTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        private void SetupDefaultMockBehavior(PlatformID platform, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "postgresql" },
                { "ServerPassword", "postgres" },
                {"Port", 5432 }
            };

            this.mockPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(f => f.EndsWith("superuser.txt")))).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(
                It.Is<string>(f => f.EndsWith("superuser.txt")),
                It.IsAny<CancellationToken>())).ReturnsAsync("defaultpwd");
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLServerConfigurationExecutesExpectedConfigurationCommands(PlatformID platform, Architecture architecture, string platformArchitecture)
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

            using (TestPostgreSQLServerConfiguration configuration = new TestPostgreSQLServerConfiguration(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await configuration.ExecuteAsync(CancellationToken.None);

                string configureScriptPath = this.mockFixture.Combine(this.mockPackage.Path, platformArchitecture, "configureServer.py");

                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"python3 \"{configureScriptPath}\""));
            }
        }

        private class TestPostgreSQLServerConfiguration : PostgreSQLServerConfiguration
        {
            public TestPostgreSQLServerConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
