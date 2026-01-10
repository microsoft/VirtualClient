// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    public class LatteServerExecutorTests2
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(ScriptExecutorTests), "Examples", "Latte");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID, architecture);
            this.mockPackage = new DependencyPath("latte", this.mockFixture.PlatformSpecifics.GetPackagePath("latte"));
            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "latte";
            this.mockFixture.Parameters["Connections"] = "256";
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:05:00";
            this.mockFixture.Parameters["Protocol"] = "Tcp";
            this.mockFixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerReset;
            this.mockFixture.Parameters["Port"] = 5001;
            this.mockFixture.Parameters["Workload"] = "CPS";
            this.mockFixture.Parameters["Scenario"] = "LatteMock";

            string exampleResults = File.ReadAllText(Path.Combine(LatteServerExecutorTests2.ExamplesDirectory, "Latte_Results_Example.txt"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task LatteServerExecutorExecutesAsExpectedForResetInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupTest(platformID, architecture);
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();

            using (TestLatteServerExecutor executor = new TestLatteServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
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

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.AreEqual(0, processExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task LatteServerExecutorExecutesAsExpectedForStartInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupTest(platformID, architecture);
            this.mockFixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerStartExecution;
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, Architecture.X64).Path;
            List<string> commandsExecuted = new List<string>();

            using (TestLatteServerExecutor executor = new TestLatteServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
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

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.AreEqual(0, processExecuted);
            }
        }

        private class TestLatteServerExecutor : LatteServerExecutor2
        {
            public TestLatteServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
