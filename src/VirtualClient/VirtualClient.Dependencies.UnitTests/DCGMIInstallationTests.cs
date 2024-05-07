// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
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
    public class DCGMIInstallationTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public void DCGMIInstallationThrowsIfDistroNotSupported()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.AzLinux
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestDCGMIInstallation testDCGMIInstallation = new TestDCGMIInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => testDCGMIInstallation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        public void DCGMIInstallationThrowsIfPlatformNotSupported()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            using (TestDCGMIInstallation testDCGMIInstallation = new TestDCGMIInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => testDCGMIInstallation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.PlatformNotSupported, exception.Reason);
            }
        }

        [Test]
        public async Task DCGMIInstallationExecutesExpectedCommandsOnUbuntu()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            List<string> expectedCommands = new List<string>()
            {
                "sudo apt-key del 7fa2af80",
                $@"sudo bash -c ""wget https://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release; echo $ID$VERSION_ID) | sed -e 's/\.//g')/x86_64/cuda-keyring_1.0-1_all.deb""",
                "sudo dpkg -i cuda-keyring_1.0-1_all.deb",
                "sudo apt-get update",
                "sudo apt-get install -y datacenter-gpu-manager",
                "sudo systemctl --now enable nvidia-dcgm"
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

            using (TestDCGMIInstallation testDCGMIInstallation = new TestDCGMIInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(6, commandExecuted);
        }

        private class TestDCGMIInstallation : DCGMIInstallation
        {
            public TestDCGMIInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public VirtualClientComponent InstantiatedInstaller { get; set; }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
