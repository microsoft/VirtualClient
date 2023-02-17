// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using System.Runtime.InteropServices;
    public class DCGMIDiagExecutorTests
    {
        private MockFixture mockFixture;
        private State mockState;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public async Task TestDCGMIDiagMonitorExecutesExpectedCommandsOnUbuntu()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, Architecture.X64);
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationtoken = cancellationTokenSource.Token;

            List<string> expectedCommands = new List<string>()
            {
                "sudo nvidia-smi -pm 1",
                "sudo nvidia-smi -e 1",
                $"sudo dcgmi diag -r {this.mockFixture.Parameters["Level"]} -j"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
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

                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                    if (arguments == $"dcgmi diag -r {this.mockFixture.Parameters["Level"]} -j")
                    {
                        cancellationTokenSource.Cancel();
                        return process;
                    }
                    else if (arguments == $"nvidia-smi -e 1")
                    {
                        this.mockFixture.StateManager.OnGetState(nameof(DCGMIDiscoveryExecutor)).ReturnsAsync(JObject.FromObject(this.mockState));
                    }
                }
                return process;
            };

            using (TestDCGMIDiagExecutor testDCGMIDiagExecutor = new TestDCGMIDiagExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIDiagExecutor.ExecuteAsync(cancellationtoken).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Level", "1" },
                { "Username", "anyuser" },
                { "LocalRunFile", "https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run" },
                {"MonitorFrequency", "00:00:02"},
                {"MonitorWarmupPeriod", "00:00:02"}
            };

            this.mockState = new State();
            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            this.mockFixture.ApiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        private class TestDCGMIDiagExecutor : DCGMIDiscoveryExecutor
        {
            public TestDCGMIDiagExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context,CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
