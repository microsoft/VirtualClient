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
    public class DCGMIProftesterExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public async Task TestDCGMIProftesterExecutesExpectedCommandsOnUbuntu()
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
                $"sudo /usr/bin/dcgmproftester11 --no-dcgm-validation -t {this.mockFixture.Parameters["FieldIDProftester"]} -d 10",
                $"sudo dcgmi dmon -e {this.mockFixture.Parameters["ListOfFieldIDsDmon"]} -c 30"
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
                }
                if (arguments == $"dcgmi dmon -e {this.mockFixture.Parameters["ListOfFieldIDsDmon"]} -c 30")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIProftesterExecutor testDCGMIProftesterExecutor = new TestDCGMIProftesterExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIProftesterExecutor.ExecuteAsync(cancellationtoken).ConfigureAwait(false);
            }

            Assert.AreEqual(2, commandExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Username", "anyuser" },
                { "FieldIDProftester", "1004" },
                {"ListOfFieldIDsDmon", "1004,1005,1001"}
            };           
        }

        private class TestDCGMIProftesterExecutor : DCGMIProftesterExecutor
        {
            public TestDCGMIProftesterExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
