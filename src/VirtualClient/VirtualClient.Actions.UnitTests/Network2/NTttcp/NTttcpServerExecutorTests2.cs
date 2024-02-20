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
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.NTttcpExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class NTttcpServerExecutorTests2
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();

        }

        public void SetupDefaultMockApiBehavior(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(platformID, architecture);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "Networking";
            this.mockFixture.Parameters["Protocol"] = "TCP";
            this.mockFixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerReset;
            this.mockFixture.Parameters["Port"] = 5001;
            this.mockFixture.Parameters["Workload"] = "CPS";
            this.mockFixture.Parameters["Scenario"] = "NTttcpMock";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "NTttcp", "ClientOutput.xml");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task NTttcpServerExecutorExecutesAsExpectedForResetInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestNTttcpServerExecutor executor = new TestNTttcpServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            TestNTttcpServerExecutor component = new TestNTttcpServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            if (platformID == PlatformID.Win32NT)
            {
                Assert.AreEqual(0, processExecuted);
            }
            else
            {
                Assert.AreEqual(1, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    "sudo chmod +x \"" + this.mockFixture.Combine(expectedPath, "ntttcp") + "\"",
                },
                commandsExecuted); ;
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task NTttcpServerExecutorExecutesAsExpectedForStartInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            this.mockFixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerStartExecution;
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestNTttcpServerExecutor executor = new TestNTttcpServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            TestNTttcpServerExecutor component = new TestNTttcpServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            if (platformID == PlatformID.Win32NT)
            {
                Assert.AreEqual(0, processExecuted);
            }
            else
            {
                Assert.AreEqual(1, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    "sudo chmod +x \"" + this.mockFixture.Combine(expectedPath, "ntttcp") + "\"",
                },
                commandsExecuted); ;
            }
        }

        private class TestNTttcpServerExecutor : NTttcpServerExecutor2
        {
            public TestNTttcpServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public new CancellationTokenSource ServerCancellationSource
            {
                get
                {
                    return base.ServerCancellationSource;
                }

                set
                {
                    base.ServerCancellationSource = value;
                }
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            protected override bool IsProcessRunning(string processName)
            {
                return true;
            }
        }
    }
}
