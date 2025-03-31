// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SockPerfServerExecutorTests2 : MockFixture
    {
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platformID, Architecture architecture)
        {
            this.Setup(platformID, architecture);
            this.mockPackage = new DependencyPath("sockperf", this.GetPackagePath("sockperf"));
            this.SetupPackage(this.mockPackage);
            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.Parameters["PackageName"] = "sockperf";
            this.Parameters["Protocol"] = "TCP";
            this.Parameters["Port"] = 5001;
            this.Parameters["Workload"] = "CPS";
            this.Parameters["Scenario"] = "SockPerfMock";
            this.Parameters["MessagesPerSecond"] = "max";
            this.Parameters["ConfidenceLevel"] = "99";

            string exampleResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "SockPerf", "SockPerfClientExample1.txt");

            this.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfServerExecutorExecutesAsExpectedForStartInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupTest(platformID, architecture);
            this.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerStartExecution;
            string expectedPath = this.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestSockPerfServerExecutor executor = new TestSockPerfServerExecutor(this.Dependencies, this.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.Process;
            };

            TestSockPerfServerExecutor component = new TestSockPerfServerExecutor(this.Dependencies, this.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
            CollectionAssert.AreEqual(
            new List<string>
            {
                "sudo chmod +x \"" + this.Combine(expectedPath, "sockperf") + "\"",
            },
            commandsExecuted); ;
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfServerExecutorExecutesAsExpectedForResetInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupTest(platformID, architecture);
            this.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerReset;
            string expectedPath = this.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestSockPerfServerExecutor executor = new TestSockPerfServerExecutor(this.Dependencies, this.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.Process;
            };

            TestSockPerfServerExecutor component = new TestSockPerfServerExecutor(this.Dependencies, this.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
            CollectionAssert.AreEqual(
            new List<string>
            {
                "sudo chmod +x \"" + this.Combine(expectedPath, "sockperf") + "\"",
            },
            commandsExecuted); ;
        }

        private class TestSockPerfServerExecutor : SockPerfServerExecutor2
        {
            public TestSockPerfServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
