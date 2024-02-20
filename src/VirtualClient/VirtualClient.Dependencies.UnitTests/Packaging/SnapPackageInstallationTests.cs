// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SnapPackageInstallationTests
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
        public async Task SnapPackageInstallationRunsTheExpectedCommandForSinglePackage()
        {
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SnapPackageInstallation.Packages), "pack1" },
                { nameof(SnapPackageInstallation.AllowUpgrades), "true" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo snap refresh",
                "sudo snap install pack1",
                "sudo snap list pack1"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

            using (TestSnapPackageInstallation snapPackageInstallation = new TestSnapPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await snapPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        [Test]
        public async Task SnapPackageInstallationRunsTheExpectedCommandForMultiplePackages()
        {
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SnapPackageInstallation.Packages), "pack1,pack2" },
                { nameof(SnapPackageInstallation.AllowUpgrades), "true" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo snap refresh",
                "sudo snap install pack1 pack2",
                "sudo snap list pack1",
                "sudo snap list pack2",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

            using (TestSnapPackageInstallation snapPackageInstallation = new TestSnapPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await snapPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(4, commandExecuted);
        }

        [Test]
        public async Task SnapPackageInstallationRunsTheExpectedCommandForNullPackages()
        {
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SnapPackageInstallation.Packages), null },
                { nameof(SnapPackageInstallation.AllowUpgrades), "true" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo snap refresh",
                "sudo snap install pack1",
                "sudo snap list pack1",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

            using (TestSnapPackageInstallation snapPackageInstallation = new TestSnapPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await snapPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandExecuted);
        }

        [Test]
        public async Task SnapPackageInstallationRunsTheExpectedCommandForDuplicatePackages()
        {
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SnapPackageInstallation.Packages), "pack1,pack1" },
                { nameof(SnapPackageInstallation.AllowUpgrades), "true" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo snap refresh",
                "sudo snap install pack1",
                "sudo snap list pack1",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

            using (TestSnapPackageInstallation snapPackageInstallation = new TestSnapPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await snapPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        [Test]
        public async Task SnapPackageInstallationRunsTheExpectedCommandForCentOSDistro()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestCentOS7",
                LinuxDistribution = LinuxDistribution.CentOS7
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SnapPackageInstallation.Packages), "pack1" },
                { nameof(SnapPackageInstallation.AllowUpgrades), "true" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo systemctl enable --now snapd.socket",
                "sudo snap refresh",
                "sudo snap install pack1",
                "sudo snap list pack1",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

            using (TestSnapPackageInstallation snapPackageInstallation = new TestSnapPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await snapPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(4, commandExecuted);
        }

        [Test]
        public async Task SnapPackageInstallationRunsTheExpectedCommandForSUSEDistro()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestSUSE",
                LinuxDistribution = LinuxDistribution.SUSE
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SnapPackageInstallation.Packages), "pack1" },
                { nameof(SnapPackageInstallation.AllowUpgrades), "true" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo systemctl enable --now snapd",
                "sudo systemctl enable --now snapd.apparmor",
                "sudo snap refresh",
                "sudo snap install pack1",
                "sudo snap list pack1",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

            using (TestSnapPackageInstallation snapPackageInstallation = new TestSnapPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await snapPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(5, commandExecuted);
        }

        private class TestSnapPackageInstallation : SnapPackageInstallation
        {
            public TestSnapPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}