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
    using static VirtualClient.Actions.CPSExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class CPSServerExecutorTests2
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(CPSServerExecutorTests2), "Examples", "CPS");

        private MockFixture mockFixture;
        private DependencyPath mockPath;

        public void SetupTest(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID, architecture);
            this.mockPath = new DependencyPath("cps", this.mockFixture.PlatformSpecifics.GetPackagePath("cps"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "cps";
            this.mockFixture.Parameters["Connections"] = 256;
            this.mockFixture.Parameters["TestDuration"] = 300;
            this.mockFixture.Parameters["WarmupTime"] = 30;
            this.mockFixture.Parameters["Delaytime"] = 30;
            this.mockFixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerReset;
            this.mockFixture.Parameters["Port"] = 5001;
            this.mockFixture.Parameters["Workload"] = "CPS";
            this.mockFixture.Parameters["Scenario"] = "CPSMock";
            this.mockFixture.Parameters["ConfidenceLevel"] = "99";

            string exampleResults = File.ReadAllText(Path.Combine(CPSServerExecutorTests2.ExamplesDirectory, "CPS_Example_Results_Server.txt"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CPSServerExecutorExecutesAsExpectedForResetInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupTest(platformID, architecture);
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestCPSServerExecutor executor = new TestCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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

            TestCPSServerExecutor component = new TestCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            string exe = platformID == PlatformID.Win32NT ? "cps.exe" : "cps";
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
                    "sudo chmod +x \"" + this.mockFixture.Combine(expectedPath, exe) + "\"",
                },
                commandsExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CPSServerExecutorExecutesAsExpectedForStartInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupTest(platformID, architecture);
            this.mockFixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerStartExecution;
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();

            TestCPSServerExecutor executor = new TestCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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

            TestCPSServerExecutor component = new TestCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            string exe = platformID == PlatformID.Win32NT ? "cps.exe" : "cps";
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
                    "sudo chmod +x \"" + this.mockFixture.Combine(expectedPath, exe) + "\""
                },
                commandsExecuted);
            }
        }

        private class TestCPSServerExecutor : CPSServerExecutor2
        {
            public TestCPSServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
