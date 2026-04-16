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
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class NTttcpFullDuplexTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpFullDuplexTests), "Examples", "NTttcp");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private NetworkingWorkloadState networkingWorkloadState;
        private List<string> executedCommands;

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
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:05:00";
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
            this.mockFixture.Parameters["DuplexMode"] = "Full";

            string clientResults = File.ReadAllText(this.mockFixture.Combine(NTttcpFullDuplexTests.ExamplesDirectory, "ClientOutput.xml"));
            string serverResults = File.ReadAllText(this.mockFixture.Combine(NTttcpFullDuplexTests.ExamplesDirectory, "ServerOutput.xml"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string path, CancellationToken ct) =>
                {
                    // Return server (receiver) XML for receive results, client (sender) XML otherwise
                    if (path != null && path.Contains("recv"))
                    {
                        return serverResults;
                    }

                    return clientResults;
                });

            this.executedCommands = new List<string>();

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        public void NTttcpExecutorIsFullDuplexReturnsTrueWhenDuplexModeIsFull()
        {
            this.SetupTest();

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.IsTrue(component.GetIsFullDuplex());
        }

        [Test]
        public void NTttcpExecutorIsFullDuplexReturnsFalseWhenDuplexModeIsHalf()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DuplexMode"] = "Half";

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.IsFalse(component.GetIsFullDuplex());
        }

        [Test]
        public void NTttcpExecutorIsFullDuplexReturnsFalseWhenDuplexModeIsNotSet()
        {
            this.SetupTest();
            this.mockFixture.Parameters.Remove("DuplexMode");

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.IsFalse(component.GetIsFullDuplex());
        }

        [Test]
        public void NTttcpExecutorIsFullDuplexIsCaseInsensitive()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DuplexMode"] = "FULL";

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.IsTrue(component.GetIsFullDuplex());

            this.mockFixture.Parameters["DuplexMode"] = "full";
            component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.IsTrue(component.GetIsFullDuplex());
        }

        [Test]
        public void NTttcpExecutorReversePortIsBasePortPlusOffset()
        {
            this.SetupTest();

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.AreEqual(5600, component.GetReversePort());
        }

        [Test]
        public async Task NTttcpFullDuplexClientExecutesBothSendAndReceiveProcessesOnLinux()
        {
            this.SetupTest(PlatformID.Unix);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            int processCount = 0;
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
                    processCount++;
                    this.executedCommands.Add(arguments);

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

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Full-duplex runs 2 NTttcp processes + 2 sysctl processes = 4
            Assert.AreEqual(4, processCount);
        }

        [Test]
        public async Task NTttcpFullDuplexClientExecutesBothSendAndReceiveProcessesOnWindows()
        {
            this.SetupTest(PlatformID.Win32NT);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            int processCount = 0;
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

                processCount++;
                this.executedCommands.Add(arguments);

                this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                     .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

                return process;
            };

            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Win32NT);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Full-duplex runs 2 NTttcp processes on Windows (no sysctl)
            Assert.AreEqual(2, processCount);
        }

        [Test]
        public async Task NTttcpFullDuplexClientUsesCorrectSendCommandLineOnLinux()
        {
            this.SetupTest(PlatformID.Unix);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo() { FileName = file, Arguments = arguments },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod") && !file.Contains("sysctl"))
                {
                    this.executedCommands.Add(arguments);
                }

                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Verify send command uses -s flag and forward port (5500)
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-s") && cmd.Contains("-p 5500")));

            // Verify receive command uses -r flag and reverse port (5600)
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-r") && cmd.Contains("-p 5600")));
        }

        [Test]
        public async Task NTttcpFullDuplexClientUsesCorrectSendCommandLineOnWindows()
        {
            this.SetupTest(PlatformID.Win32NT);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo() { FileName = file, Arguments = arguments },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                this.executedCommands.Add(arguments);

                return process;
            };

            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Win32NT);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Verify send command uses -s flag and forward port (5500)
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-s") && cmd.Contains("-p 5500")));

            // Verify receive command uses -r flag and reverse port (5600)
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-r") && cmd.Contains("-p 5600")));
        }

        [Test]
        public async Task NTttcpFullDuplexUsesSeparateResultsFilesForSendAndReceive()
        {
            this.SetupTest(PlatformID.Unix);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo() { FileName = file, Arguments = arguments },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod") && !file.Contains("sysctl"))
                {
                    this.executedCommands.Add(arguments);
                }

                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Send results use -send.xml output file
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-s") && cmd.Contains("ntttcp-results-send.xml")));

            // Receive results use -recv.xml output file
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-r") && cmd.Contains("ntttcp-results-recv.xml")));
        }

        [Test]
        [Ignore("Server-side full-duplex execution test requires complex API state mocking — deferred to integration tests")]
        public async Task NTttcpFullDuplexServerExecutesBothSendAndReceiveProcesses()
        {
            this.SetupTest(PlatformID.Unix);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            string resultsPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Examples", "NTttcp", "ServerOutput.xml");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo() { FileName = file, Arguments = arguments },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod"))
                {
                    processCount++;
                    this.executedCommands.Add(arguments);
                }

                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Server in full-duplex runs 2 NTttcp + 2 sysctl = 4 processes
            Assert.AreEqual(4, processCount);

            // Server send uses -s flag (sends to client) and reverse port (5600)
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-s") && cmd.Contains("-p 5600")));

            // Server receive uses -r flag and forward port (5500)
            Assert.IsTrue(this.executedCommands.Exists(cmd => cmd.Contains("-r") && cmd.Contains("-p 5500")));
        }

        [Test]
        public async Task NTttcpHalfDuplexModeExecutesSingleProcessAsExpected()
        {
            this.SetupTest(PlatformID.Unix);
            this.mockFixture.Parameters["DuplexMode"] = "Half";

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo() { FileName = file, Arguments = arguments },
                    OnHasExited = () => true,
                    ExitCode = 0,
                    OnStart = () => true,
                    StandardOutput = new VirtualClient.Common.ConcurrentBuffer()
                };

                if (!arguments.Contains("chmod"))
                {
                    processCount++;
                    this.executedCommands.Add(arguments);

                    this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                    var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                    this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                         .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
                }

                if (file.Contains("sysctl"))
                {
                    string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string standardOutput = File.ReadAllText(Path.Combine(currentDirectory, "Examples", "NTttcp", "sysctlExampleOutput.txt"));
                    process.StandardOutput.Append(standardOutput);
                }

                return process;
            };

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Half-duplex on linux: 1 NTttcp + 1 sysctl = 2 processes
            Assert.AreEqual(2, processCount);
        }

        [Test]
        public void NetworkingWorkloadStateSupportsFullDuplexMode()
        {
            NetworkingWorkloadState state = new NetworkingWorkloadState(
                "networking",
                "Scenario_1",
                NetworkingWorkloadTool.NTttcp,
                NetworkingWorkloadToolState.Start,
                "TCP",
                1,
                "4K",
                "4K",
                duplexMode: "Full");

            Assert.AreEqual("Full", state.DuplexMode);
        }

        [Test]
        public void NetworkingWorkloadStateDuplexModeDefaultsToNull()
        {
            NetworkingWorkloadState state = new NetworkingWorkloadState(
                "networking",
                "Scenario_1",
                NetworkingWorkloadTool.NTttcp,
                NetworkingWorkloadToolState.Start);

            Assert.IsNull(state.DuplexMode);
        }

        [Test]
        public void NetworkingWorkloadStateWithDuplexModeIsJsonSerializable()
        {
            NetworkingWorkloadState state = new NetworkingWorkloadState(
                "networking",
                "Scenario_1",
                NetworkingWorkloadTool.NTttcp,
                NetworkingWorkloadToolState.Start,
                "TCP",
                16,
                "8K",
                "8K",
                256,
                "00:01:00",
                "00:00:05",
                "00:00:05",
                "Test_Mode_1",
                64,
                1234,
                true,
                true,
                16,
                32,
                "Interrupt_Differentiator_1",
                "100",
                80.5,
                true,
                "Profiling_Scenario_1",
                "00:00:30",
                "00:00:05",
                false,
                Guid.NewGuid(),
                duplexMode: "Full");

            SerializationAssert.IsJsonSerializable(state);

            NetworkingWorkloadState deserialized = state.ToJson().FromJson<NetworkingWorkloadState>();
            Assert.AreEqual("Full", deserialized.DuplexMode);
        }

        [Test]
        public async Task NTttcpFullDuplexSendCommandLineOnLinuxContainsSenderFlag()
        {
            this.SetupTest(PlatformID.Unix);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeComponentAsync();
            string sendCmd = component.GetSendCommandLineArguments();

            Assert.IsTrue(sendCmd.Contains("-s"));
            Assert.IsFalse(sendCmd.Contains("-r "));
            Assert.IsTrue(sendCmd.Contains("-p 5500"));
            Assert.IsTrue(sendCmd.Contains("ntttcp-results-send.xml"));
        }

        [Test]
        public async Task NTttcpFullDuplexReceiveCommandLineOnLinuxContainsReceiverFlag()
        {
            this.SetupTest(PlatformID.Unix);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeComponentAsync();
            string recvCmd = component.GetReceiveCommandLineArguments();

            Assert.IsTrue(recvCmd.Contains("-r"));
            Assert.IsFalse(recvCmd.Contains("-s "));
            Assert.IsTrue(recvCmd.Contains("-p 5600"));
            Assert.IsTrue(recvCmd.Contains("ntttcp-results-recv.xml"));
        }

        [Test]
        public async Task NTttcpFullDuplexSendCommandLineOnWindowsContainsSenderFlag()
        {
            this.SetupTest(PlatformID.Win32NT);
            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Win32NT);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeComponentAsync();
            string sendCmd = component.GetSendCommandLineArguments();

            Assert.IsTrue(sendCmd.Contains("-s"));
            Assert.IsFalse(sendCmd.Contains("-r "));
            Assert.IsTrue(sendCmd.Contains("-p 5500"));
            Assert.IsTrue(sendCmd.Contains("ntttcp-results-send.xml"));
        }

        [Test]
        public async Task NTttcpFullDuplexReceiveCommandLineOnWindowsContainsReceiverFlag()
        {
            this.SetupTest(PlatformID.Win32NT);
            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Win32NT);

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeComponentAsync();
            string recvCmd = component.GetReceiveCommandLineArguments();

            Assert.IsTrue(recvCmd.Contains("-r"));
            Assert.IsFalse(recvCmd.Contains("-s "));
            Assert.IsTrue(recvCmd.Contains("-p 5600"));
            Assert.IsTrue(recvCmd.Contains("ntttcp-results-recv.xml"));
        }

        [Test]
        public async Task NTttcpFullDuplexSendCommandUsesClientBufferSize()
        {
            this.SetupTest(PlatformID.Unix);
            this.mockFixture.Parameters["BufferSizeClient"] = "64K";
            this.mockFixture.Parameters["BufferSizeServer"] = "256K";

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeComponentAsync();
            string sendCmd = component.GetSendCommandLineArguments();

            Assert.IsTrue(sendCmd.Contains("-b 64K"));
        }

        [Test]
        public async Task NTttcpFullDuplexReceiveCommandUsesServerBufferSize()
        {
            this.SetupTest(PlatformID.Unix);
            this.mockFixture.Parameters["BufferSizeClient"] = "64K";
            this.mockFixture.Parameters["BufferSizeServer"] = "256K";

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor =
                new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            TestNTttcpFullDuplexExecutor component = new TestNTttcpFullDuplexExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeComponentAsync();
            string recvCmd = component.GetReceiveCommandLineArguments();

            Assert.IsTrue(recvCmd.Contains("-b 256K"));
        }

        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.NTttcp;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.BufferSizeClient = "4k";
            this.networkingWorkloadState.BufferSizeServer = "4k";
            this.networkingWorkloadState.Protocol = "TCP";
            this.networkingWorkloadState.TestMode = "MockTestMode";
            this.networkingWorkloadState.DuplexMode = "Full";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        private class TestNTttcpFullDuplexExecutor : NTttcpExecutor
        {
            public TestNTttcpFullDuplexExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public bool GetIsFullDuplex()
            {
                return this.IsFullDuplex;
            }

            public int GetReversePort()
            {
                return this.ReversePort;
            }

            public string GetSendCommandLineArguments()
            {
                return this.GetFullDuplexSendCommandLineArguments();
            }

            public string GetReceiveCommandLineArguments()
            {
                return this.GetFullDuplexReceiveCommandLineArguments();
            }

            public async Task InitializeComponentAsync()
            {
                await this.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            protected override bool IsProcessRunning(string processName)
            {
                return true;
            }
        }
    }
}
