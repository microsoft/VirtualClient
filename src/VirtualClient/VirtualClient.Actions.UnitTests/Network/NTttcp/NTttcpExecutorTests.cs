// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NTttcpExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpExecutorTests), "Examples", "NTttcp");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private NetworkingWorkloadState networkingWorkloadState;

        public void SetupTest(PlatformID platform = PlatformID.Unix)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockPackage = new DependencyPath("networking", this.mockFixture.PlatformSpecifics.GetPackagePath("networking"));
            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "networking";
            this.mockFixture.Parameters["Connections"] = "256";
            this.mockFixture.Parameters["TestDuration"] = "300";
            this.mockFixture.Parameters["WarmupTime"] = "300";
            this.mockFixture.Parameters["Protocol"] = "TCP";
            this.mockFixture.Parameters["ThreadCount"] = "1";
            this.mockFixture.Parameters["BufferSizeClient"] = "4k";
            this.mockFixture.Parameters["BufferSizeServer"] = "4k";
            this.mockFixture.Parameters["Port"] = 5500;
            this.mockFixture.Parameters["ReceiverMultiClientMode"] = true;
            this.mockFixture.Parameters["SenderLastClient"] = true;
            this.mockFixture.Parameters["ThreadsPerServerPort"] = 2;
            this.mockFixture.Parameters["ConnectionsPerThread"] = 2;
            this.mockFixture.Parameters["DevInterruptsDifferentiator"] = "mlx";

            string exampleResults = File.ReadAllText(this.mockFixture.Combine(NTttcpExecutorTests.ExamplesDirectory, "ClientOutput.xml"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        public void NTttcpExecutorThrowsOnUnsupportedOS()
        {
            this.SetupTest();

            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Other);
            TestNTttcpExecutor component = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        public async Task NTttcpExecutorClientExecutesAsExpected()
        {
            this.SetupTest();

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = file,
                        Arguments = arguments,
                    },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod"))
                {
                    processExecuted++;
                    this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                    var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                    this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                         .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
                }

                string standardOutput = null;
                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpExecutor component = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            //Process 1: Ntttcp, Process 2: Sysctl
            Assert.AreEqual(2, processExecuted);
        }

        [Test]
        public async Task NTttcpExecutorClientWillNotExitOnCancellationRequestUntilTheResultsAreCaptured()
        {
            this.SetupTest();

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = file,
                        Arguments = arguments,
                    },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod"))
                {
                    processExecuted++;
                    this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                    var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                    this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                         .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
                }

                string standardOutput = null;
                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpExecutor component = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            //Process 1: Ntttcp, Process 2: Sysctl
            Assert.AreEqual(2, processExecuted);
        }

        [Test]
        public async Task NTttcpExecutorServerExecutesAsExpected()
        {
            this.SetupTest();

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "NTttcp", "ServerOutput.xml");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = file,
                        Arguments = arguments,
                    },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod"))
                {
                    processExecuted++;
                }

                string standardOutput = null;
                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpExecutor component = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            //Process 1: Ntttcp, Process 2: Sysctl
            Assert.AreEqual(2, processExecuted);
        }

        [Test]
        public async Task NTttcpExecutorExecutesTheExpectedCommandToExecuteSysctl()
        {
            this.SetupTest();

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = file,
                        Arguments = arguments,
                    },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                string standardOutput = null;
                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpExecutor component = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted("sysctl net.ipv4.tcp_rmem net.ipv4.tcp_wmem"));
        }

        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.NTttcp;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.BufferSizeClient = "4k";
            this.networkingWorkloadState.BufferSizeServer = "4k";
            this.networkingWorkloadState.Protocol = "UDP";
            this.networkingWorkloadState.TestMode = "MockTestMode";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        private class TestNTttcpExecutor : NTttcpExecutor
        {
            public TestNTttcpExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            protected override bool IsProcessRunning(string processName)
            {
                return true;
            }
        }
    }
}
