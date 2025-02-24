// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Moq.Language.Flow;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in tear down.")]
    public class OpenFOAMInstallationTests
    {
        private const string AddPublicKeyCommand = "sh -c \"wget -O - https://dl.openfoam.org/gpg.key | apt-key add -\"";
        private const string UpdateSoftwareRepositoriesCommand = "add-apt-repository http://dl.openfoam.org/ubuntu --yes";
        private const string UpdateAptPackageCommand = "apt update";

        private const string InstallOpenFOAMx64Command = "apt -y install openfoam9";
        private const string InstallOpenFOAMarm64Command = "apt install openfoam --yes --quiet";

        private MockFixture fixture;
        private TestComponent component;
        private Mock<ProcessManager> mockProcessManager;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();                  
        }

        [TearDown]
        public void TearDown()
        {
            this.component.Dispose();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task OpenFOAMInstallationDependencyStartsCorrectProcessesOnExecute(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            List<string> expectedCommands = new List<string>();

            if (architecture == Architecture.X64)
            {
                expectedCommands = new List<string>
                {
                    $"sudo {AddPublicKeyCommand}",
                    $"sudo {UpdateSoftwareRepositoriesCommand}",
                    $"sudo {UpdateAptPackageCommand}",
                    $"sudo {InstallOpenFOAMx64Command}"
                };
            }
            else
            {
                expectedCommands = new List<string>
                {
                    $"sudo {UpdateAptPackageCommand}",
                    $"sudo {InstallOpenFOAMarm64Command}"
                };
            }

            int commandExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands[commandExecuted] == $"{exe} {arguments}")
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
            using (TestComponent toolkitInstallation = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await toolkitInstallation.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, commandExecuted);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void OpenFOAMInstallationDependencySurfacesExceptionWhenProcessDoesNotExitSuccessfullyOnExecute(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                this.fixture.Process.ExitCode = 1;
                this.fixture.Process.OnHasExited = () => true;
                return this.fixture.Process;
            };

            using TestComponent component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            this.mockProcessManager = new Mock<ProcessManager>();
            this.component = new TestComponent(this.fixture.Dependencies);
            this.component.Parameters[nameof(OpenFOAMInstallation.PackageName)] = "OpenFOAM";
            this.mockPath = new DependencyPath("OpenFOAM", "C:\\");
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            ISetup<ProcessManager, IProcessProxy> setup = this.mockProcessManager.Setup(mgr => mgr.CreateProcess(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()));
            setup.Callback<string, string, string>((cmd, args, wd) =>
            {
                throw new Exception($"A setup is not registered for cmd: '{cmd}' args: '{args}' wd: '{wd}'");
            });

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.fixture.SystemManagement.SetupGet(mgr => mgr.ProcessManager).Returns(this.mockProcessManager.Object);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(false);
        }

        private class TestComponent : OpenFOAMInstallation
        {
            public TestComponent(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnExecuteAsync => this.ExecuteAsync;

            public Func<EventContext, CancellationToken, Task> OnInitializeAsync => this.InitializeAsync;
        }
    }
}
