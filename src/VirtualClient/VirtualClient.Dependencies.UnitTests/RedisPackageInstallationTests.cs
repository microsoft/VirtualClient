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
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
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

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestRedisPackageInstallation installation = new TestRedisPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestRedisPackageInstallation installation = new TestRedisPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"apt update"));
                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"apt install redis -y"));
            }
        }

        [Test]
        [TestCase(Architecture.X64, "linux-x64")]
        [TestCase(Architecture.Arm64, "linux-arm64")]
        public async Task RedisPackageInstallationExecutesExpectedInstallationCommandsOnAzLinux(Architecture architecture, string platformArchitecture)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, architecture);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestAzLinux",
                LinuxDistribution = LinuxDistribution.AzLinux
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            using (TestRedisPackageInstallation installation = new TestRedisPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"dnf update"));
                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted($"dnf install redis -y"));
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "redis" }
            };

            this.mockPackage = new DependencyPath("redis", this.mockFixture.GetPackagePath("redis"));
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
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
