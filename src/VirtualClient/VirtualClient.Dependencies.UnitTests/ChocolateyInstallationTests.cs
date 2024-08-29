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
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class ChocolateyInstallationTests
    {
        private MockFixture fixture;

        [Test]
        public async Task ChocolateyInstallationRunsTheExpectedCommandInWindows()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "powershell.exe -Command Set-ExecutionPolicy Bypass -Scope Process -Force; " +
                    "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; " +
                    "iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"
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

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "choco" }
            };

            using (TestChocolateyInstallation chocolateyInstallation = new TestChocolateyInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await chocolateyInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public async Task ChocolateyInstallationRunsNothingInLinux()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

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

            using (TestChocolateyInstallation chocolateyInstallation = new TestChocolateyInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await chocolateyInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandExecuted);
        }

        private class TestChocolateyInstallation : ChocolateyInstallation
        {
            public TestChocolateyInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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