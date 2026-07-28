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
        // Great examples could be found at https://github.com/chef/os_release
        private static readonly string HostnamectlExamples = MockFixture.GetDirectory(
            typeof(LinuxPackageInstallationTests),
            "TestResources",
            "Unix",
            "hostnamectl");

        private static readonly string OSReleaseExamples = MockFixture.GetDirectory(
            typeof(LinuxPackageInstallationTests),
            "TestResources",
            "Unix",
            "os-release");

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

            this.mockFixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.mockFixture.File.Object);
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.Debian, LinuxDistribution.Debian)]
        [TestCase(LinuxUpstreamDistribution.Debian, LinuxDistribution.Ubuntu)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnDebianUpstreamDistros(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Debian Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LinuxPackageInstallation.Packages), "pack1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                AptPackageInstallation installation = packageInstallation.InstantiatedInstaller as AptPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.Debian, LinuxDistribution.Debian)]
        [TestCase(LinuxUpstreamDistribution.Debian, LinuxDistribution.Ubuntu)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnDebianUpstreamDistros_WhenSpecificPackageStoreIsDefined(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Debian Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages-Apt", "pack1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                AptPackageInstallation installation = packageInstallation.InstantiatedInstaller as AptPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.Debian, LinuxDistribution.Debian)]
        [TestCase(LinuxUpstreamDistribution.Debian, LinuxDistribution.Ubuntu)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnDebianUpstreamDistros_WhenSpecificRepositoryIsDefined(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Debian Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages", "pack1" },
                { "Repositories-Apt", "repository1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                AptPackageInstallation installation = packageInstallation.InstantiatedInstaller as AptPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
                Assert.AreEqual("repository1", installation.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnDebianUpstreamDistros_Ubuntu_ComplexCase()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Ubuntu Distro",
                Distribution = LinuxDistribution.Ubuntu,
                UpstreamDistribution = LinuxUpstreamDistribution.Debian
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages", "pack1,pack2,pack3" },
                { "Packages-Apt", "extrapack1,extrapack2" },
                { "Repositories-Apt", "repo1,repo2" },
                { "Packages-Yum", "wrongpack1,wrongpack2" },
                { "Repositories-Ubuntu", "extrarepo1,extrarepo2" },
                { "Packages-Ubuntu", "morepack1,morepack2" },
                { "Packages-Debian", "wrongpack1,wrongpack2" },
                { "Packages-AzLinux", "wrongpack1,wrongpack2" },
                { "Repositories-SUSE", "wrongrepo1,wrongrepo2" },
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                AptPackageInstallation aptInstall = packageInstallation.InstantiatedInstaller as AptPackageInstallation;

                Assert.IsNotNull(aptInstall);
                Assert.AreEqual("pack1,pack2,pack3,extrapack1,extrapack2,morepack1,morepack2", aptInstall.Packages);
                Assert.AreEqual("repo1,repo2,extrarepo1,extrarepo2", aptInstall.Repositories);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.Fedora)]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.CentOS)]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.RedHat)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnFedoraUpstreamDistros(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Fedora Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LinuxPackageInstallation.Packages), "pack1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                DnfPackageInstallation installation = packageInstallation.InstantiatedInstaller as DnfPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.Fedora)]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.CentOS)]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.RedHat)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnFedoraUpstreamDistros_WhenSpecificPackageStoreIsDefined(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Fedora Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages-Dnf", "pack1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                DnfPackageInstallation installation = packageInstallation.InstantiatedInstaller as DnfPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.Fedora)]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.CentOS)]
        [TestCase(LinuxUpstreamDistribution.Fedora, LinuxDistribution.RedHat)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnFedoraUpstreamDistros_WhenSpecificRepositoryIsDefined(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Debian Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages", "pack1" },
                { "Repositories-Dnf", "repository1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                DnfPackageInstallation installation = packageInstallation.InstantiatedInstaller as DnfPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
                Assert.AreEqual("repository1", installation.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnFedoraUpstreamDistros_AzureLinux_ComplexCase()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Azure Linux 3 Distro",
                Distribution = LinuxDistribution.AzureLinux,
                UpstreamDistribution = LinuxUpstreamDistribution.Fedora
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages", "pack1,pack2,pack3" },
                { "Packages-Apt", "wrongpack1,wrongpack2" },
                { "Packages-Dnf", "extrapack1,extrapack2" },
                { "Repositories-Apt", "wrongrepo1,wrongrepo2" },
                { "Packages-Yum", "wrongpack1,wrongpack2" },
                { "Repositories-Ubuntu", "wrongpack1,wrongpack2" },
                { "Repositories-Dnf", "repo1,repo2" },
                { "Repositories-AzureLinux", "extrarepo1,extrarepo2" },
                { "Packages-Ubuntu", "morepack1,morepack2" },
                { "Packages-Debian", "wrongpack1,wrongpack2" },
                { "Packages-AzureLinux", "morepack1,morepack2" },
                { "Repositories-OpenSuse", "wrongrepo1,wrongrepo2" },
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                DnfPackageInstallation installation = packageInstallation.InstantiatedInstaller as DnfPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1,pack2,pack3,extrapack1,extrapack2,morepack1,morepack2", installation.Packages);
                Assert.AreEqual("repo1,repo2,extrarepo1,extrarepo2", installation.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnFedoraUpstreamDistros_AmazonLinux_ComplexCase()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Amazon Linux",
                Distribution = LinuxDistribution.AmazonLinux,
                UpstreamDistribution = LinuxUpstreamDistribution.Fedora
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
                { "Repositories-AmazonLinux", "extrarepo1,extrarepo2" },
                { "Packages-Ubuntu", "morepack1,morepack2" },
                { "Packages-Debian", "wrongpack1,wrongpack2" },
                { "Packages-AmazonLinux", "morepack1,morepack2" },
                { "Repositories-OpenSuse", "wrongrepo1,wrongrepo2" },
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(packageInstallation.InstantiatedInstaller is DnfPackageInstallation);
                DnfPackageInstallation aptInstall = (DnfPackageInstallation)packageInstallation.InstantiatedInstaller;
                Assert.AreEqual("pack1,pack2,pack3,extrapack1,extrapack2,morepack1,morepack2", aptInstall.Packages);
                Assert.AreEqual("repo1,repo2,extrarepo1,extrarepo2", aptInstall.Repositories);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.OpenSuse, LinuxDistribution.OpenSuse)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnOpenSuseUpstreamDistros(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "OpenSUSE Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LinuxPackageInstallation.Packages), "pack1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                ZypperPackageInstallation installation = packageInstallation.InstantiatedInstaller as ZypperPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.OpenSuse, LinuxDistribution.OpenSuse)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnOpenSuseUpstreamDistros_WhenSpecificPackageStoreIsDefined(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "OpenSUSE Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages-Zypper", "pack1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                ZypperPackageInstallation installation = packageInstallation.InstantiatedInstaller as ZypperPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
            }
        }

        [Test]
        [TestCase(LinuxUpstreamDistribution.OpenSuse, LinuxDistribution.OpenSuse)]
        public async Task LinuxPackageInstallationHandlesPackageInstallationsCorrectlyOnOpenSuseUpstreamDistros_WhenSpecificRepositoryIsDefined(LinuxUpstreamDistribution upstreamDistro, LinuxDistribution distro)
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "OpenSUSE Distro",
                Distribution = distro,
                UpstreamDistribution = upstreamDistro
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages", "pack1" },
                { "Repositories-Zypper", "repository1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                ZypperPackageInstallation installation = packageInstallation.InstantiatedInstaller as ZypperPackageInstallation;

                Assert.IsNotNull(installation);
                Assert.AreEqual("pack1", installation.Packages);
                Assert.AreEqual("repository1", installation.Repositories);
            }
        }

        [Test]
        public async Task LinuxPackageInstallationDoesNotInstallUnlessAPackageManagerIsApplicable()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "Fedora Distro",
                Distribution = LinuxDistribution.RedHat,
                UpstreamDistribution = LinuxUpstreamDistribution.Fedora
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockInfo);

            // No need to install APT package manager packages on a RedHat/Fedora distribution.
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Packages-Apt", "pack1" },
                { "Repositories-Apt", "repo1" }
            };

            using (var packageInstallation = new TestLinuxPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await packageInstallation.ExecuteAsync(CancellationToken.None);
                Assert.IsNull(packageInstallation.InstantiatedInstaller);
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