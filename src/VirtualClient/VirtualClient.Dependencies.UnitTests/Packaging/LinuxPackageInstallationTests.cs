// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LinuxPackageInstallationTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public async Task LinuxPackageInstallationInstantiateCorrectAptInstalltionUbuntuSimpleCase()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LinuxPackageInstallation.Packages), "pack1" },
                { "Repositories-Apt", "repo1" }
            };

            using (TestLinuxPackageInstallation packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(packageInstallation.InstantiatedInstaller is AptPackageInstallation);
                AptPackageInstallation aptInstall = (AptPackageInstallation)packageInstallation.InstantiatedInstaller;
                Assert.AreEqual("pack1", aptInstall.Packages);
                Assert.AreEqual("repo1", aptInstall.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationStillInstallsIfOnlyRepositoryNeedsToBeAdded()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Repositories-Apt", "repo1" }
            };

            using (TestLinuxPackageInstallation packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(packageInstallation.InstantiatedInstaller is AptPackageInstallation);
                AptPackageInstallation aptInstall = (AptPackageInstallation)packageInstallation.InstantiatedInstaller;
                Assert.AreEqual("repo1", aptInstall.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationDoesNotInstallIfNoPackageIsApplicable()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestCentOs",
                LinuxDistribution = LinuxDistribution.CentOS8
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages-Apt", "pack1" },
                { "Repositories-Apt", "repo1" }
            };

            using (TestLinuxPackageInstallation packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNull(packageInstallation.InstantiatedInstaller);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationInstantiateCorrectAptInstalltionUbuntuComplexCase()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LinuxPackageInstallation.Packages), "pack1,pack2,pack3" },
                { "Packages-Apt", "extrapack1,extrapack2" },
                { "Repositories-Apt", "repo1,repo2" },
                { "Packages-Yum", "wrongpack1,wrongpack2" },
                { "Repositories-Ubuntu", "extrarepo1,extrarepo2" },
                { "Packages-Ubuntu", "morepack1,morepack2" },
                { "Packages-Debian", "wrongpack1,wrongpack2" },
                { "Packages-Mariner", "wrongpack1,wrongpack2" },
                { "Repositories-SUSE", "wrongrepo1,wrongrepo2" },
            };

            using (TestLinuxPackageInstallation packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(packageInstallation.InstantiatedInstaller is AptPackageInstallation);
                AptPackageInstallation aptInstall = (AptPackageInstallation)packageInstallation.InstantiatedInstaller;
                Assert.AreEqual("pack1,pack2,pack3,extrapack1,extrapack2,morepack1,morepack2", aptInstall.Packages);
                Assert.AreEqual("repo1,repo2,extrarepo1,extrarepo2", aptInstall.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationInstantiateCorrectDnfInstalltionMarinerComplexCase()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestMariner",
                LinuxDistribution = LinuxDistribution.Mariner
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LinuxPackageInstallation.Packages), "pack1,pack2,pack3" },
                { "Packages-Apt", "wrongpack1,wrongpack2" },
                { "Packages-Dnf", "extrapack1,extrapack2" },
                { "Repositories-Apt", "wrongrepo1,wrongrepo2" },
                { "Packages-Yum", "wrongpack1,wrongpack2" },
                { "Repositories-Ubuntu", "wrongpack1,wrongpack2" },
                { "Repositories-Dnf", "repo1,repo2" },
                { "Repositories-Mariner", "extrarepo1,extrarepo2" },
                { "Packages-Ubuntu", "morepack1,morepack2" },
                { "Packages-Debian", "wrongpack1,wrongpack2" },
                { "Packages-Mariner", "morepack1,morepack2" },
                { "Repositories-SUSE", "wrongrepo1,wrongrepo2" },
            };

            using (TestLinuxPackageInstallation packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(packageInstallation.InstantiatedInstaller is DnfPackageInstallation);
                DnfPackageInstallation aptInstall = (DnfPackageInstallation)packageInstallation.InstantiatedInstaller;
                Assert.AreEqual("pack1,pack2,pack3,extrapack1,extrapack2,morepack1,morepack2", aptInstall.Packages);
                Assert.AreEqual("repo1,repo2,extrarepo1,extrarepo2", aptInstall.Repositories);
            }
        }

        private class TestLinuxPackageInstallation : LinuxPackageInstallation
        {
            public TestLinuxPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public VirtualClientComponent InstantiatedInstaller { get; set; }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            protected override Task InstallPackageAsync(VirtualClientComponent installer, CancellationToken cancellationToken)
            {
                this.InstantiatedInstaller = installer;

                return Task.CompletedTask;
            }
        }
    }
}