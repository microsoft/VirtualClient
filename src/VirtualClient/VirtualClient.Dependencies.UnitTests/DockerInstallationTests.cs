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
    public class DockerInstallationTests
    {
        private const string RequiredPackagesCommand = "apt-get install ca-certificates curl gnupg lsb-release --yes --quiet";
        private const string AddOfficialGPGKeyCommand = @"bash -c ""curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg --batch --yes""";
        private const string SetUpRepositoryCommand = @"bash -c ""echo """"deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"""" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null""";

        private MockFixture mockFixture;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        [TestCase("20.10.17")]
        [TestCase(null)]
        public async Task DockerInstallationRunsTheExpectedCommandOnLinux(string version)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", version }
            };

            using (TestDockerInstallation dockerInstallation = new TestDockerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo {RequiredPackagesCommand}",
                    $"sudo {AddOfficialGPGKeyCommand}", 
                    $"sudo {SetUpRepositoryCommand}",
                    @$"sudo bash -c ""apt-get install docker-ce=$(apt-cache  madison docker-ce | grep {dockerInstallation.Version} | awk '{{print $3}}') docker-ce-cli=$(apt-cache madison docker-ce | grep {dockerInstallation.Version} | awk '{{print $3}}') containerd.io docker-compose-plugin --yes --quiet""",
                    @$"sudo bash -c ""apt-get install docker-ce docker-ce-cli containerd.io docker-compose-plugin --yes --quiet"""
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
                
                await dockerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(4, commandExecuted);
            }
        }

        private class TestDockerInstallation : DockerInstallation
        {
            public TestDockerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
