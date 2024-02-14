// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class ApptainerInstallationTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        [TestCase("20.10.17")]
        [TestCase(null)]
        public async Task ApptainerInstallationRunsTheExpectedCommandOnLinux(string version)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            if (version == null)
            {
                version = "1.1.6";
            }

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", version }
            };

            using (TestApptainerInstallation apptainerInstallation = new TestApptainerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                string installationFile = $"apptainer_{version}_amd64.deb";

                List<string> expectedCommands = new List<string>()
                {
                    $"sudo wget https://github.com/apptainer/apptainer/releases/download/v{version}/{installationFile}",
                    $"sudo dpkg -i {installationFile}", 
                    $"sudo rm {installationFile}",
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
                
                await apptainerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(3, commandExecuted);
            }
        }

        private class TestApptainerInstallation : ApptainerInstallation
        {
            public TestApptainerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
