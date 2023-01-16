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
    public class PostgresqlInstallationTests
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
        public void PostgresqlInstallationThrowsIfDistroNotSupportedForLinux()
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
                { "Version", "14" }
            };

            using (TestPostgresqlInstallation testPostgresqlInstallation = new TestPostgresqlInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => testPostgresqlInstallation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        public async Task PostresqlInstallationExecutesExpectedCommandsOnUbuntu()
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
                @"sudo sh -c ""echo """"deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main"""" | sudo tee /etc/apt/sources.list.d/docker.list""",
                @"sudo bash -c ""wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -""",
                @"sudo apt-get update",
                @$"sudo apt-get -y install postgresql-14",
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

            using (TestPostgresqlInstallation testPostgresqlInstallation = new TestPostgresqlInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(4, commandExecuted);
        }

        [Test]
        public async Task PostresqlInstallationExecutesExpectedCommandsOnCentOSX64()
        {
            this.SetupDefaultMockBehavior();

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestCentOS",
                LinuxDistribution = LinuxDistribution.CentOS7
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "14" }
            };

            List<string> expectedCommands = new List<string>()
            {
                @"sudo yum install -y https://download.postgresql.org/pub/repos/yum/reporpms/EL-7-x86_64/pgdg-redhat-repo-latest.noarch.rpm",
                @$"sudo yum install -y postgresql14-server"
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

            using (TestPostgresqlInstallation testPostgresqlInstallation = new TestPostgresqlInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(2, commandExecuted);
        }

        [Test]
        public async Task PostresqlInstallationExecutesExpectedCommandsOnWindows()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT, Architecture.X64);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "14" },
                { "PackageName", "postgresql" }
            };

            List<string> expectedCommands = new List<string>()
            {
                $"{this.mockPath.Path}\\win-x64\\postgresql-{this.mockFixture.Parameters["Version"]}.exe --mode \"unattended\" --serverport \"5432\" --superpassword \"postgres\""
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

            using (TestPostgresqlInstallation testPostgresqlInstallation = new TestPostgresqlInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testPostgresqlInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "14" },
                { "PackageName", "postgresql" }
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // this.currentDirectoryPath = new DependencyPath("LAPACK", currentDirectory);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            // this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);
            /*resultsPath = this.mockFixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\LAPACK\LAPACKResultsExample.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>()))
                .Returns(this.rawString);*/

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        private class TestPostgresqlInstallation : PostgresqlInstallation
        {
            public TestPostgresqlInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
