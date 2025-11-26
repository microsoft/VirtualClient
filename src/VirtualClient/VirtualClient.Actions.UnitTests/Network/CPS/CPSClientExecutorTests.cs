// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Contracts;
    using Polly;
    using System.Net.Http;
    using System.Net;
    using System.IO;
    using System.Reflection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CPSClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;
        private NetworkingWorkloadState networkingWorkloadState;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "Networking";
            this.mockFixture.Parameters["Connections"] = "256";
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:00:30";
            this.mockFixture.Parameters["Delaytime"] = "00:00:00";
            this.mockFixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "CPS", "CPS_Example_Results_Server.txt");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.Process.StandardOutput.Append(results);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        public void CPSClientExecutorThrowsOnUnsupportedOS()
        {
            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Other);
            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        public async Task CPSClientExecutorExecutesAsExpected()
        {
            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None,CancellationToken.None);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                     .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

                return this.mockFixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        public async Task CPSClientExecutorExecutesAsExpectedWithIntegerBasedTimeParameters()
        {
            // This test verifies that the executor runs correctly when time parameters are provided as integers
            // (legacy format), ensuring backward compatibility for partner teams' existing profiles.

            // Override with integer-based time parameters
            this.mockFixture.Parameters["TestDuration"] = 120;      // 120 seconds (integer)
            this.mockFixture.Parameters["WarmupTime"] = 30;         // 30 seconds (integer)
            this.mockFixture.Parameters["Delaytime"] = 15;          // 15 seconds (integer)

            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                     .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

                return this.mockFixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Verify the parameters are correctly converted to TimeSpan
            Assert.AreEqual(TimeSpan.FromSeconds(120), component.TestDuration);
            Assert.AreEqual(TimeSpan.FromSeconds(30), component.WarmupTime);
            Assert.AreEqual(TimeSpan.FromSeconds(15), component.DelayTime);

            // Verify the executor runs successfully
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.CPS;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.Protocol = "UDP";
            this.networkingWorkloadState.TestMode = "MockTestMode";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        [Test]
        public void CPSClientExecutorSupportsBackwardCompatibilityWithIntegerBasedTimeParameters()
        {
            // This test ensures backward compatibility: partners' profiles using integer-based time parameters
            // (representing seconds) will continue to work after the conversion to TimeSpan-based parameters.

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

        private class TestCPSClientExecutor : CPSClientExecutor
        {
            public TestCPSClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }
        }
    }
}
