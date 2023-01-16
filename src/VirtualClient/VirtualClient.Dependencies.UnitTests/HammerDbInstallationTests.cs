using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using VirtualClient.Common;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

namespace VirtualClient.Dependencies
{
    [TestFixture]
    [Category("Unit")]
    public class HammerDbInstallationTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockPath = this.mockFixture.Create<DependencyPath>();
            // this.mockFixture.Setup(PlatformID.Unix);

            // this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public void HammerDbInstallationThrowsIfDistroNotSupportedForLinux()
        {
            this.SetupDefaultMockBehavior();
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "4.6" }
            };

            using (TestHammerDbInstallation testHammerDbInstallation = new TestHammerDbInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => testHammerDbInstallation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        public async Task HammerDbInstallationExecutesExpectedCommandsOnUbuntu()
        {
            this.SetupDefaultMockBehavior();

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "14" }
            };

            List<string> expectedCommands = new List<string>()
            {
                $@"sudo wget https://github.com/TPC-Council/HammerDB/releases/download/v{this.mockFixture.Parameters["Version"]}/HammerDB-{this.mockFixture.Parameters["Version"]}-Linux.tar.gz",
                @"sudo tar -xvzf HammerDB-14-Linux.tar.gz"
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

            using (TestHammerDbInstallation testHammerDbInstallation = new TestHammerDbInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testHammerDbInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(2, commandExecuted);
        }

        [Test]
        public async Task HammerDbInstallationExecutesExpectedCommandsOnWindows()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT, Architecture.X64);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "4.6" },
                { "PackageName", "postgresql" }
            };

            List<string> expectedCommands = new List<string>()
            {
                $"{this.mockPath.Path}\\win-x64\\HammerDB-{this.mockFixture.Parameters["Version"]}.exe --mode \"unattended\""
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

            using (TestHammerDbInstallation testHammerDbInstallation = new TestHammerDbInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testHammerDbInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "4.6" },
                { "PackageName", "postgresql" }
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        private class TestHammerDbInstallation : HammerDbInstallation
        {
            public TestHammerDbInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            /*
                        public VirtualClientComponent InstantiatedInstaller { get; set; }*/

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
