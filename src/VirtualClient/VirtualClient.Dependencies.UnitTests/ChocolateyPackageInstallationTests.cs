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
    public class ChocolateyPackageInstallationTests
    {
        private MockFixture fixture;

        [Test]
        public async Task ChocolateyPackageInstallationRunsTheExpectedCommandInWindows()
        {
            this.fixture = new MockFixture();
            this.SetupDefaultMockBehaviors();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"{this.fixture.GetPackagePath()}\choco\choco.exe install pack1 pack2 --yes"
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

            using (TestChocolateyPackageInstallation installation = new TestChocolateyPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public async Task ChocolateyPackageInstallationRunsIfPackageIsEmpty()
        {
            this.fixture = new MockFixture();
            this.SetupDefaultMockBehaviors();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "choco" },
                { nameof(ChocolateyPackageInstallation.Packages), " " }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            int commandExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestChocolateyPackageInstallation installation = new TestChocolateyPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandExecuted);
        }

        [Test]
        public void ChocolateyPackageInstallationRunsNothingInLinux()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "choco" },
                { nameof(ChocolateyPackageInstallation.Packages), "pack1,pack2" }
            };

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

            using (TestChocolateyPackageInstallation installation = new TestChocolateyPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsFalse(VirtualClientComponent.IsSupported(installation));
            }
        }

        private void SetupDefaultMockBehaviors()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);

            DependencyPath package = new DependencyPath("choco", this.fixture.PlatformSpecifics.GetPackagePath("choco"));

            this.fixture.PackageManager.OnGetPackage("choco").ReturnsAsync(package);

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "choco" },
                { nameof(ChocolateyPackageInstallation.Packages), "pack1,pack2" }
            };
        }

        private class TestChocolateyPackageInstallation : ChocolateyPackageInstallation
        {
            public TestChocolateyPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
                parameters = new Dictionary<string, IConvertible>()
                {
                    { "PackageName", "choco" }
                };
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}