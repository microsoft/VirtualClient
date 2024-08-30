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
    public class MsmpiInstallationTests
    {
        private MockFixture fixture;

        [Test]
        public async Task MsmpiInstallationRunsTheExpectedCommandInWindows()
        {
            this.SetupMockFixture(PlatformID.Win32NT);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"msiexec.exe /i ""{this.fixture.GetPackagePath()}\msmpi\win-x64\msmpisdk.msi"" /qn"
            };

            int commandExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
                return process;
            };

            using (TestMsmpiInstallation installation = new TestMsmpiInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public void MsmpiInstallationDoesNotExecuteOnLinuxSystems()
        {
            this.SetupMockFixture(PlatformID.Unix);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                commandExecuted++;

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
                
                return process;
            };

            using (TestMsmpiInstallation installation = new TestMsmpiInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsFalse(VirtualClientComponent.IsSupported(installation));
            }
        }

        private void SetupMockFixture(PlatformID platform)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform);
            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsRegex(".*msmpisuccess.lock")))
                .Returns(false);

            DependencyPath package = new DependencyPath("msmpi", this.fixture.PlatformSpecifics.GetPackagePath("msmpi"));
            this.fixture.PackageManager.OnGetPackage("msmpi").ReturnsAsync(package);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MsmpiInstallation.PackageName), "msmpi" }
            };
        }

        private class TestMsmpiInstallation : MsmpiInstallation
        {
            public TestMsmpiInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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