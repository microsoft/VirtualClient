// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.CtsTrafficExecutor;

    [TestFixture]
    [Category("Unit")]
    public class CtsTrafficClientExecutorTests : MockFixture
    {
        private DependencyPath mockCtsTrafficPackage;
        private string mockResults;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64, int numaNodeIndex = 0)
        {
            this.Setup(platform, architecture);
            this.mockCtsTrafficPackage = new DependencyPath("ctstraffic", this.GetPackagePath("ctstraffic"));

            this.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockCtsTrafficPackage.Name,
                ["PrimaryPort"] = "4445",
                ["SecondaryPort"] = "4444",
                ["NumaNodeIndex"] = numaNodeIndex,
                ["BufferInBytes"] = 36654,
                ["Pattern"] = "Duplex",
                ["BytesToTransfer"] = "0x400000000",
                ["Connections"] = 1,
                ["Iterations"] = 1
            };

            // Setup: Required packages exist on the system.
            this.SetupPackage(this.mockCtsTrafficPackage);            

            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, @"CtsTraffic", "CtsTrafficResultsExample.csv");

            this.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockResults);

            // Setup: Server state
            var expectedState = new Item<State>(nameof(CtsTrafficServerState), new CtsTrafficServerState
            {
                ServerSetupCompleted = true
            });

            this.ApiClient.OnGetState(nameof(CtsTrafficServerState))
                .ReturnsAsync(() => this.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedState));

            this.ApiClient.OnGetServerOnline()
                .ReturnsAsync(() => this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns(this.ApiClient.Object);
        }

        [Test]
        public void CtsTrafficClientExecutorThrowsIfTheServerDoesNotHaveSetupCompletedeBeforePollingTimeout()
        {
            this.SetupTest();

            this.ApiClient
                .OnGetState(nameof(CtsTrafficServerState))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            using (var executor = new TestCtsTrafficClientExecutor(this.Dependencies, this.Parameters))
            {
                // Cause a polling timeout
                executor.PollingTimeout = TimeSpan.Zero;

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task CtsTrafficServerExecutorExecutesExpectedCommandsOnWindowsSystemsWithNumaNodeProvided(Architecture architecture)
        {
            this.SetupTest(PlatformID.Win32NT, architecture);
            using (var executor = new TestCtsTrafficClientExecutor(this.Dependencies, this.Parameters))
            {
                // e.g.
                // C:\Users\Any\VirtualClient\packages\ctstraffic
                string ctsTrafficPackage = this.mockCtsTrafficPackage.Path;
                string arch = architecture.ToString().ToLower();

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}

                    $"{ctsTrafficPackage}\\win-{arch}\\StartProcessInNumaNode.exe {executor.NumaNodeIndex} " +
                    $"\"{ctsTrafficPackage}\\win-{arch}\\ctsTraffic.exe -Target:1.2.3.5 -Consoleverbosity:1 " +
                    $"-StatusFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Status.csv -ConnectionFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Connections.csv " +
                    $"-ErrorFileName:{ctsTrafficPackage}\\win-{arch}\\Results\\Errors.txt -Port:{executor.Port} -Connections:{executor.Connections} -Pattern:{executor.Pattern} " +
                    $"-Iterations:{executor.Iterations} -Transfer:{executor.BytesToTransfer} -Buffer:{executor.BufferInBytes} -TimeLimit:150000\" --> {ctsTrafficPackage}\\win-{arch}"
                };

                this.ProcessManager.OnProcessCreated = (process) =>
                {
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
            using (var executor = new TestCtsTrafficClientExecutor(this.Dependencies, this.Parameters))
            {
                // e.g.
                // C:\Users\Any\VirtualClient\packages\ctstraffic
                string ctsTrafficPackage = this.mockCtsTrafficPackage.Path;
                string arch = architecture.ToString().ToLower();

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}

                    $"{ctsTrafficPackage}\\win-{arch}\\ctsTraffic.exe -Target:1.2.3.5 -Consoleverbosity:1 " +
                    $"-StatusFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Status.csv -ConnectionFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Connections.csv " +
                    $"-ErrorFileName:{ctsTrafficPackage}\\win-{arch}\\Results\\Errors.txt -Port:{executor.Port} -Connections:{executor.Connections} -Pattern:{executor.Pattern} " +
                    $"-Iterations:{executor.Iterations} -Transfer:{executor.BytesToTransfer} -Buffer:{executor.BufferInBytes} -TimeLimit:150000 --> {ctsTrafficPackage}\\win-{arch}"
                };

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} --> {process.StartInfo.WorkingDirectory}");
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestCtsTrafficClientExecutor : CtsTrafficClientExecutor
        {
            public TestCtsTrafficClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new string CtsTrafficPackagePath => base.CtsTrafficPackagePath;

            public new TimeSpan PollingTimeout
            {
                get
                {
                    return base.PollingTimeout;
                }
                set
                {
                    base.PollingTimeout = value;
                }
            }

            public new string CtsTrafficExe => base.CtsTrafficExe;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}