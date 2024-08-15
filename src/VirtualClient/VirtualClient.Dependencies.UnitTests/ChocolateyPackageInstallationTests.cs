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
        private MockFixture mockFixture;

        [Test]
        public async Task ChocolateyPackageInstallationRunsTheExpectedCommandInWindows()
        {
            this.mockFixture = new MockFixture();
            this.SetupDefaultMockBehaviors();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"{this.mockFixture.GetPackagePath()}\choco\choco.exe install pack1 pack2 --yes"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestChocolateyPackageInstallation installation = new TestChocolateyPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public async Task ChocolateyPackageInstallationRunsIfPackageIsEmpty()
        {
            this.mockFixture = new MockFixture();
            this.SetupDefaultMockBehaviors();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "choco" },
                { nameof(ChocolateyPackageInstallation.Packages), " " }
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
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandExecuted);
        }

        [Test]
        public void ChocolateyPackageInstallationRunsNothingInLinux()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            using (TestChocolateyPackageInstallation installation = new TestChocolateyPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsFalse(VirtualClientComponent.IsSupported(installation));
            }
        }

        private void SetupDefaultMockBehaviors()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);

            DependencyPath package = new DependencyPath("choco", this.mockFixture.PlatformSpecifics.GetPackagePath("choco"));

            this.mockFixture.PackageManager.OnGetPackage("choco").ReturnsAsync(package);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
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