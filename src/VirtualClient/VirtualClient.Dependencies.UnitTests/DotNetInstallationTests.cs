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

    [TestFixture]
    [Category("Unit")]
    public class DotNetInstallationTests
    {
        private MockFixture fixture;

        [Test]
        public async Task DotNetInstallationRunsTheExpectedCommandInWindows()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);
            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(DotNetInstallation.PackageName), "dotnetsdk" },
                { nameof(DotNetInstallation.DotNetVersion), "7.8.9" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"powershell {this.fixture.GetPackagePath()}\dotnet\dotnet-install.ps1 -Version 7.8.9 -InstallDir {this.fixture.GetPackagePath()}\dotnet -Architecture x64"
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

            using (TestDotNetInstallation installation = new TestDotNetInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public async Task DotNetInstallationRunsTheExpectedCommandInLinux()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(DotNetInstallation.PackageName), "dotnetsdk" },
                { nameof(DotNetInstallation.DotNetVersion), "7.8.9" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"sudo chmod +x  ""{this.fixture.GetPackagePath()}/dotnet/dotnet-install.sh""",
                $@"sudo {this.fixture.GetPackagePath()}/dotnet/dotnet-install.sh --version 7.8.9 --install-dir {this.fixture.GetPackagePath()}/dotnet --architecture x64"
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

            using (TestDotNetInstallation installation = new TestDotNetInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await installation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        private class TestDotNetInstallation : DotNetInstallation
        {
            public TestDotNetInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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