// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.CPSExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class CPSClientExecutorTests2
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpExecutorTests2), "Examples", "CPS");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            VirtualClientApiClient.DefaultPollingWaitTime = TimeSpan.Zero;
        }

        private void SetupTest(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID, architecture);
            this.mockPackage = new DependencyPath("cps", this.mockFixture.PlatformSpecifics.GetPackagePath("cps"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "cps";
            this.mockFixture.Parameters["Port"] = 3001;
            this.mockFixture.Parameters["Connections"] = 256;
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:00:44";
            this.mockFixture.Parameters["Delaytime"] = "00:00:30";
            this.mockFixture.Parameters["ConfidenceLevel"] = "99";

            string exampleResults = File.ReadAllText(Path.Combine(CPSClientExecutorTests2.ExamplesDirectory, "CPS_Example_Results_Server.txt"));

            this.mockFixture.Process.StandardOutput.Append(exampleResults);

            CPSWorkloadState executionStartedState = new CPSWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<CPSWorkloadState> expectedStateItem = new Item<CPSWorkloadState>(nameof(CPSWorkloadState), executionStartedState);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(CPSWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CPSClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
        {
            int sendInstructionsExecuted = 0;
            this.SetupTest(platformID, architecture);
            this.mockFixture.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((obj, can, pol) =>
                {
                    Item<Instructions> stateItem = obj.ToObject<Item<Instructions>>();
                    if (stateItem.Definition.Type == InstructionsType.ClientServerReset)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 0);

                        sendInstructionsExecuted++;
                    }

                    if (stateItem.Definition.Type == InstructionsType.ClientServerStartExecution)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 1);
                        sendInstructionsExecuted++;
                    }

                    Assert.AreEqual(stateItem.Definition.Properties["Scenario"], "AnyScenario");
                    Assert.AreEqual(stateItem.Definition.Properties["Type"], typeof(CPSServerExecutor2).Name);
                    Assert.AreEqual(stateItem.Definition.Properties["Connections"], 256);
                    Assert.AreEqual(stateItem.Definition.Properties["Port"], 3001);
                    Assert.AreEqual(stateItem.Definition.Properties["TestDuration"], "00:05:00");
                    Assert.AreEqual(stateItem.Definition.Properties["WarmupTime"], "00:00:44");
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                return this.mockFixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(sendInstructionsExecuted, 2);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CPSClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
        {
            int processExecuted = 0;
            this.SetupTest(platformID, architecture);
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            string exe = platformID == PlatformID.Win32NT ? "cps.exe" : "cps";
            if (platformID == PlatformID.Win32NT)
            {
                Assert.AreEqual(1, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    this.mockFixture.Combine(expectedPath, exe) + " -c -r 256 1.2.3.4,0,1.2.3.5,3001,100,100,0,1 -i 10 -wt 44 -t 300 -ds 30"
                },
                commandsExecuted);
            }
            else
            {
                Assert.AreEqual(2, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    "sudo chmod +x \"" + this.mockFixture.Combine(expectedPath, exe) + "\"",
                    this.mockFixture.Combine(expectedPath, exe) + " -c -r 256 1.2.3.4,0,1.2.3.5,3001,100,100,0,1 -i 10 -wt 44 -t 300 -ds 30"
                },
                commandsExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task CPSClientExecutor2ExecutesAsExpectedWithIntegerBasedTimeParameters(PlatformID platformID, Architecture architecture)
        {
            // This test verifies that the executor runs correctly when time parameters are provided as integers
            // (legacy format), ensuring backward compatibility for partner teams' existing profiles.

            int processExecuted = 0;
            this.SetupTest(platformID, architecture);

            // Override with integer-based time parameters
            this.mockFixture.Parameters["TestDuration"] = 180;      // 180 seconds (integer)
            this.mockFixture.Parameters["WarmupTime"] = 45;         // 45 seconds (integer)
            this.mockFixture.Parameters["Delaytime"] = 20;          // 20 seconds (integer)

            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Verify the parameters are correctly converted to TimeSpan
            Assert.AreEqual(TimeSpan.FromSeconds(180), component.TestDuration);
            Assert.AreEqual(TimeSpan.FromSeconds(45), component.WarmupTime);
            Assert.AreEqual(TimeSpan.FromSeconds(20), component.DelayTime);

            // Verify the executor runs successfully
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            string exe = platformID == PlatformID.Win32NT ? "cps.exe" : "cps";
            // Verify the command line includes the integer values (180, 45, 20)
            if (platformID == PlatformID.Win32NT)
            {
                Assert.AreEqual(1, processExecuted);
                Assert.IsTrue(commandsExecuted[0].Contains("-t 180"), "Command should contain TestDuration of 180 seconds");
                Assert.IsTrue(commandsExecuted[0].Contains("-wt 45"), "Command should contain WarmupTime of 45 seconds");
                Assert.IsTrue(commandsExecuted[0].Contains("-ds 20"), "Command should contain DelayTime of 20 seconds");
            }
            else
            {
                Assert.AreEqual(2, processExecuted);
                Assert.IsTrue(commandsExecuted[1].Contains("-t 180"), "Command should contain TestDuration of 180 seconds");
                Assert.IsTrue(commandsExecuted[1].Contains("-wt 45"), "Command should contain WarmupTime of 45 seconds");
                Assert.IsTrue(commandsExecuted[1].Contains("-ds 20"), "Command should contain DelayTime of 20 seconds");
            }
        }

        [Test]
        public void CPSClientExecutor2SupportsBackwardCompatibilityWithIntegerBasedTimeParameters()
        {
            // This test ensures backward compatibility: partners' profiles using integer-based time parameters
            // (representing seconds) will continue to work after the conversion to TimeSpan-based parameters.

            this.SetupTest(PlatformID.Unix, Architecture.X64);

            // Test 1: Integer format (legacy - seconds as integers)
            this.mockFixture.Parameters["TestDuration"] = 300;      // 300 seconds (integer)
            this.mockFixture.Parameters["WarmupTime"] = 60;         // 60 seconds (integer)
            this.mockFixture.Parameters["Delaytime"] = 30;          // 30 seconds (integer)

            TestCPSClientExecutor executor = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Verify integer values are correctly converted to TimeSpan
            Assert.AreEqual(TimeSpan.FromSeconds(300), executor.TestDuration, 
                "TestDuration should accept integer (300 seconds) and convert to TimeSpan");
            Assert.AreEqual(TimeSpan.FromSeconds(60), executor.WarmupTime, 
                "WarmupTime should accept integer (60 seconds) and convert to TimeSpan");
            Assert.AreEqual(TimeSpan.FromSeconds(30), executor.DelayTime, 
                "DelayTime should accept integer (30 seconds) and convert to TimeSpan");

            // Test 2: TimeSpan string format (new format)
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";    // 5 minutes (TimeSpan format)
            this.mockFixture.Parameters["WarmupTime"] = "00:01:00";      // 1 minute (TimeSpan format)
            this.mockFixture.Parameters["Delaytime"] = "00:00:30";       // 30 seconds (TimeSpan format)

            executor = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Verify TimeSpan string values work correctly
            Assert.AreEqual(TimeSpan.FromMinutes(5), executor.TestDuration, 
                "TestDuration should accept TimeSpan string format");
            Assert.AreEqual(TimeSpan.FromMinutes(1), executor.WarmupTime, 
                "WarmupTime should accept TimeSpan string format");
            Assert.AreEqual(TimeSpan.FromSeconds(30), executor.DelayTime, 
                "DelayTime should accept TimeSpan string format");

            // Test 3: Verify both formats produce equivalent results
            this.mockFixture.Parameters["TestDuration"] = 180;  // 180 seconds (integer)
            executor = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            TimeSpan integerBasedDuration = executor.TestDuration;

            this.mockFixture.Parameters["TestDuration"] = "00:03:00";  // 3 minutes (TimeSpan format)
            executor = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            TimeSpan timespanBasedDuration = executor.TestDuration;

            Assert.AreEqual(integerBasedDuration, timespanBasedDuration, 
                "Integer-based (180) and TimeSpan-based ('00:03:00') parameters must produce identical TimeSpan values");
        }

        private class TestCPSClientExecutor : CPSClientExecutor2
        {
            public TestCPSClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
