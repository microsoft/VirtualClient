// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.CtsTrafficExecutor;

    [TestFixture]
    [Category("Unit")]
    public class CtsTrafficServerExecutorTests : MockFixture
    {
        private DependencyPath mockPackage;
        private string mockResults;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64, int numaNodeIndex = 0)
        {
            this.Setup(platform, architecture);

            this.mockPackage = new DependencyPath("ctstraffic", this.GetPackagePath("ctstraffic"));

            this.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPackage.Name,
                ["PrimaryPort"] = "4445",
                ["SecondaryPort"] = "4444",
                ["NumaNodeIndex"] = numaNodeIndex,
                ["BufferInBytes"] = 36654,
                ["Pattern"] = "Duplex",
                ["BytesToTransfer"]= "0x400000000",
                ["ServerExitLimit"] = 1
            };

            this.SetupPackage(this.mockPackage);

            // Setup:
            // The server will be checking for state objects. State is how the server communicates required information
            // to the client.
            CtsTrafficServerState state = new CtsTrafficServerState();
            this.ApiClient.OnGetState(nameof(CtsTrafficServerState))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK, new Item<CtsTrafficServerState>(nameof(CtsTrafficServerState), state)));

            this.ApiClient.OnUpdateState<CtsTrafficServerState>(nameof(CtsTrafficServerState))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            this.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.mockResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, @"CtsTraffic", "CtsTrafficResultsExample.csv");

            this.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockResults);

        }

        [Test]
        public async Task CtsTrafficServerExecutorWritesTheExpectedInformationToTheServerState()
        {
            this.SetupTest();

            using (var executor = new TestCtsTrafficServerExecutor(this.Dependencies, this.Parameters))
            {
                bool confirmed_setup = true;
                this.ApiClient.OnUpdateState<CtsTrafficServerState>(nameof(CtsTrafficServerState))
                    .Callback<string, object, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((stateId, state, token, retryPolicy) =>
                    {
                        Item<CtsTrafficServerState> actualState = state as Item<CtsTrafficServerState>;

                        // Based on setup at the top. On first call, the database has not been created yet.
                        Assert.IsNotNull(actualState);
                        if (actualState.Definition.ServerSetupCompleted)
                        {
                            confirmed_setup = true;
                        }
                    })
                    .ReturnsAsync(this.CreateHttpResponse(HttpStatusCode.OK));

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(confirmed_setup);
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task CtsTrafficServerExecutorExecutesExpectedCommandsOnWindowsSystemsWithNumaNodeProvided(Architecture architecture)
        {
            this.SetupTest(PlatformID.Win32NT,architecture);
            using (var executor = new TestCtsTrafficServerExecutor(this.Dependencies, this.Parameters))
            {
                // e.g.
                // C:\Users\Any\VirtualClient\packages\ctstraffic
                string ctsTrafficPackage = this.mockPackage.Path;
                string arch = architecture.ToString().ToLower();

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}
                    $"{ctsTrafficPackage}\\win-{arch}\\StartProcessInNumaNode.exe {executor.NumaNodeIndex} " +
                    $"\"{ctsTrafficPackage}\\win-{arch}\\ctsTraffic.exe -Listen:* -Consoleverbosity:1 " +
                    $"-StatusFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Status.csv -ConnectionFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Connections.csv " +
                    $"-ErrorFileName:{ctsTrafficPackage}\\win-{arch}\\Results\\Errors.txt -Port:{executor.Port} -Pattern:{executor.Pattern} " +
                    $"-Transfer:{executor.BytesToTransfer} -ServerExitLimit:{executor.ServerExitLimit} -Buffer:{executor.BufferInBytes} -TimeLimit:150000\" --> {ctsTrafficPackage}\\win-{arch}"
                };

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    process.StandardOutput.Clear();
                    if (process.FullCommand().Contains("ctsTraffic.exe"))
                    {
                        process.StandardOutput.Append(this.mockResults);
                    }
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task CtsTrafficServerExecutorExecutesExpectedCommandsOnWindowsSystemsWithNumaNodeNotProvided(Architecture architecture)
        {
            this.SetupTest(PlatformID.Win32NT, architecture, -1);
            using (var executor = new TestCtsTrafficServerExecutor(this.Dependencies, this.Parameters))
            {
                // e.g.
                // C:\Users\Any\VirtualClient\packages\ctstraffic
                string ctsTrafficPackage = this.mockPackage.Path;
                string arch = architecture.ToString().ToLower();

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}
                    $"{ctsTrafficPackage}\\win-{arch}\\ctsTraffic.exe -Listen:* -Consoleverbosity:1 " +
                    $"-StatusFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Status.csv -ConnectionFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Connections.csv " +
                    $"-ErrorFileName:{ctsTrafficPackage}\\win-{arch}\\Results\\Errors.txt -Port:{executor.Port} -Pattern:{executor.Pattern} " +
                    $"-Transfer:{executor.BytesToTransfer} -ServerExitLimit:{executor.ServerExitLimit} -Buffer:{executor.BufferInBytes} -TimeLimit:150000 --> {ctsTrafficPackage}\\win-{arch}"
                };

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    process.StandardOutput.Clear();
                    if (process.FullCommand().Contains("ctsTraffic.exe"))
                    {
                        process.StandardOutput.Append(this.mockResults);
                    }
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestCtsTrafficServerExecutor : CtsTrafficServerExecutor
        {
            public TestCtsTrafficServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
                
            }

            public new string CtsTrafficPackagePath => base.CtsTrafficPackagePath;

            public new string CtsTrafficExe => base.CtsTrafficExe;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}