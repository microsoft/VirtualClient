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
        private MockFixture fixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaultMockBehavior()
        {
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        public async Task PostgreSQLServerConfigurationExecutesExpectedConfigurationCommands(PlatformID platform, Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultBehavior(platform, architecture);

            if (platform == PlatformID.Unix)
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestUbuntu",
                    LinuxDistribution = LinuxDistribution.Ubuntu
                };

                this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(mockInfo);
            }

            using (TestPostgreSQLServerConfiguration configuration = new TestPostgreSQLServerConfiguration(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await configuration.ExecuteAsync(CancellationToken.None);

                string configureScriptPath = this.fixture.Combine(this.mockPackage.Path, platformArchitecture, "configureServer.py");

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"python3 \"{configureScriptPath}\""));
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
        }

        private class TestPostgreSQLServerConfiguration : PostgreSQLServerConfiguration
        {
            public TestPostgreSQLServerConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
