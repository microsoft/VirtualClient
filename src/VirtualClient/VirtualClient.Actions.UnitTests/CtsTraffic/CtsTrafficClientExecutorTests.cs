// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.CtsTrafficExecutor;

    [TestFixture]
    [Category("Unit")]
    public class CtsTrafficClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockCtsTrafficPackage;
        private string mockResults;

        public void SetupDefaults(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockCtsTrafficPackage = new DependencyPath("ctstraffic", this.mockFixture.GetPackagePath("ctstraffic"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockCtsTrafficPackage.Name,
                ["PrimaryPort"] = "4445",
                ["SecondaryPort"] = "4444",
                ["NumaNode"] = 0,
                ["BufferInBytes"] = 36654,
                ["Pattern"] = "Duplex",
                ["BytesToTransfer"] = "0x400000000",
                ["Connections"] = 1,
                ["Iterations"] = 1
            };

            // Setup: Required packages exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("ctstraffic").ReturnsAsync(this.mockCtsTrafficPackage);            

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockResults = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"CtsTraffic", "CtsTrafficResultsExample.csv"));

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockResults);

            // Setup: Server state
            var expectedState = new Item<State>(nameof(CtsTrafficServerState), new CtsTrafficServerState
            {
                ServerSetupCompleted = true
            });

            this.mockFixture.ApiClient.OnGetState(nameof(CtsTrafficServerState))
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedState));

            this.mockFixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns(this.mockFixture.ApiClient.Object);
        }

        [Test]
        public void CtsTrafficClientExecutorThrowsIfTheServerDoesNotHaveSetupCompletedeBeforePollingTimeout()
        {
            this.SetupDefaults();

            this.mockFixture.ApiClient
                .OnGetState(nameof(CtsTrafficServerState))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            using (var executor = new TestCtsTrafficClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public async Task CtsTrafficServerExecutorExecutesExpectedCommandsOnWindowsSystems(Architecture architecture)
        {
            this.SetupDefaults(PlatformID.Win32NT, architecture);
            using (var executor = new TestCtsTrafficClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // C:\Users\Any\VirtualClient\packages\ctstraffic
                string ctsTrafficPackage = this.mockCtsTrafficPackage.Path;
                string arch = architecture.ToString().ToLower();

                List<string> expectedCommands = new List<string>()
                {
                    // Format:
                    // {command} {command_arguments} --> {working_dir}

                    $"{ctsTrafficPackage}\\win-{arch}\\StartProcessInNumaNode.exe {executor.NumaNode} " +
                    $"\"{ctsTrafficPackage}\\win-{arch}\\ctsTraffic.exe -Target:1.2.3.5 -Consoleverbosity:1 " +
                    $"-StatusFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Status.csv -ConnectionFilename:{ctsTrafficPackage}\\win-{arch}\\Results\\Connections.csv " +
                    $"-ErrorFileName:{ctsTrafficPackage}\\win-{arch}\\Results\\Errors.txt -Port:{executor.Port} -Connections:{executor.Connections} -Pattern:{executor.Pattern} " +
                    $"-Iterations:{executor.Iterations} -Transfer:{executor.BytesToTransfer} -Buffer:{executor.BufferInBytes} -TimeLimit:150000\" --> {ctsTrafficPackage}\\win-{arch}"
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
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