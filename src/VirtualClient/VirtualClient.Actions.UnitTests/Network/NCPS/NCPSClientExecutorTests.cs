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
    public class NCPSClientExecutorTests
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
            this.mockFixture.Parameters["ThreadCount"] = "16";
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:00:30";
            this.mockFixture.Parameters["Delaytime"] = "00:00:00";
            this.mockFixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "NCPS", "NCPS_Example_Results_Server.txt");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.Process.StandardOutput.Append(results);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        public void NCPSClientExecutorThrowsOnUnsupportedOS()
        {
            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Other);
            TestNCPSClientExecutor component = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        public async Task NCPSClientExecutorExecutesAsExpected()
        {
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

            TestNCPSClientExecutor component = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        public async Task NCPSClientExecutorExecutesAsExpectedWithIntegerTimeParameters()
        {
            this.mockFixture.Parameters["TestDuration"] = 120;
            this.mockFixture.Parameters["WarmupTime"] = 30;
            this.mockFixture.Parameters["Delaytime"] = 15;

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

            TestNCPSClientExecutor component = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.AreEqual(TimeSpan.FromSeconds(120), component.TestDuration);
            Assert.AreEqual(TimeSpan.FromSeconds(30), component.WarmupTime);
            Assert.AreEqual(TimeSpan.FromSeconds(15), component.DelayTime);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        public void NCPSClientExecutorSupportsIntegerAndTimeSpanTimeFormats()
        {
            this.mockFixture.Parameters["TestDuration"] = 300;
            this.mockFixture.Parameters["WarmupTime"] = 60;
            this.mockFixture.Parameters["Delaytime"] = 30;

            TestNCPSClientExecutor executor = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.AreEqual(TimeSpan.FromSeconds(300), executor.TestDuration);
            Assert.AreEqual(TimeSpan.FromSeconds(60), executor.WarmupTime);
            Assert.AreEqual(TimeSpan.FromSeconds(30), executor.DelayTime);

            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:01:00";
            this.mockFixture.Parameters["Delaytime"] = "00:00:30";

            executor = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.AreEqual(TimeSpan.FromMinutes(5), executor.TestDuration);
            Assert.AreEqual(TimeSpan.FromMinutes(1), executor.WarmupTime);
            Assert.AreEqual(TimeSpan.FromSeconds(30), executor.DelayTime);

            this.mockFixture.Parameters["TestDuration"] = 180;
            executor = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            TimeSpan integerBasedDuration = executor.TestDuration;

            this.mockFixture.Parameters["TestDuration"] = "00:03:00";
            executor = new TestNCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            TimeSpan timespanBasedDuration = executor.TestDuration;

            Assert.AreEqual(integerBasedDuration, timespanBasedDuration);
        }

        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.NCPS;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.Protocol = "TCP";
            this.networkingWorkloadState.TestMode = "MockTestMode";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        private class TestNCPSClientExecutor : NCPSClientExecutor
        {
            public TestNCPSClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }
        }
    }
}
