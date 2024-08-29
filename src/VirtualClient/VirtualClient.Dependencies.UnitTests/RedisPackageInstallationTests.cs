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
    public class RedisPackageInstallationTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public void RedisPackageInstallationThrowsIfDistroNotSupportedForLinux()
        {
            this.SetupDefaultMockBehavior();
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };

            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestRedisPackageInstallation installation = new TestRedisPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => installation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        [TestCase(Architecture.X64, "linux-x64")]
        [TestCase(Architecture.Arm64, "linux-arm64")]
        public async Task RedisPackageInstallationExecutesExpectedInstallationCommandsOnUbuntu(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };

            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestRedisPackageInstallation installation = new TestRedisPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"apt update"));
                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"apt install redis -y"));
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "redis" }
            };

            this.mockPackage = new DependencyPath("redis", this.fixture.GetPackagePath("redis"));
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
        }

        private class TestRedisPackageInstallation : RedisPackageInstallation
        {
            public TestRedisPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
