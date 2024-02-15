// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NASParallelBenchExecutorTests
    {
        private const string ExampleBenchmark = "bt.S.x";
        private const string ExampleUsername = "my-username";
        private MockFixture fixture;
        private DependencyPath mockPath;
        private State expectedState;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();

            this.SetupDefaultMockBehavior(PlatformID.Unix);
        }

        [Test]
        public void NASParallelBenchExecutorThrowsOnUnsupportedLinuxDistro()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            using (TestNASParallelBenchExecutor NASParallelBenchExecutor = new TestNASParallelBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestOS",
                    LinuxDistribution = LinuxDistribution.CentOS7
                };
                this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => NASParallelBenchExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.LinuxDistributionNotSupported);
            }
        }

        [Test]
        public async Task NASParallelBenchExecutorRunsExpectedCommandForBuild()
        {
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<String>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("sudo", cmd);
                Assert.AreEqual(args, "bash -c \"make suite\"");
                return this.fixture.Process;
            };

            using (TestNASParallelBenchExecutor executor = new TestNASParallelBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);
            }
        }
        [Test]
        public async Task NASParallelBenchExecutorCreatesExpectedStateOnSuccessfullBuild()
        {
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<String>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            int executed = 0;
            this.fixture.ApiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<string, JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((str, obj, can, pol) =>
                {
                    executed++;
                    Assert.IsTrue(obj.ToString() == JObject.FromObject(expectedState).ToString());
                })
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            using (TestNASParallelBenchExecutor executor = new TestNASParallelBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                Assert.AreEqual(executed, 1);
            }
        }

        [Test]
        public async Task NASParallelBenchExecutorExecutesTheExpectedLogicWhenASpecificRoleIsNotDefined()
        {
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();

            using (TestNASParallelBenchExecutor component = new TestNASParallelBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(component.IsNASParallelBenchClientExecuted);
                Assert.IsTrue(component.IsNASParallelBenchServerExecuted);
            }
        }

        [Test]
        public async Task NASParallelBenchExecutorExecutesTheExpectedLogicForTheServerRole()
        {
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestNASParallelBenchExecutor component = new TestNASParallelBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!component.IsNASParallelBenchClientExecuted);
                Assert.IsTrue(component.IsNASParallelBenchServerExecuted);
            }
        }

        [Test]
        public async Task NASParallelBenchExecutorExecutesTheExpectedLogicForTheClientRole()
        {
            TestNASParallelBenchExecutor component = new TestNASParallelBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(component.IsNASParallelBenchClientExecuted);
            Assert.IsTrue(!component.IsNASParallelBenchServerExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {

            this.fixture.Setup(platformID);

            this.mockPath = this.fixture.Create<DependencyPath>();
            DependencyPath mockPackage = new DependencyPath("nasparallelbench", this.fixture.PlatformSpecifics.GetPackagePath("nasparallelbench"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.fixture.Process;
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);

            this.expectedState = new State(new Dictionary<string, IConvertible>
            {
                ["NpbBuildState"] = "completed"
            });

            this.fixture.ApiClient.OnGetState().ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, this.expectedState));
        }

        private class TestNASParallelBenchExecutor : NASParallelBenchExecutor
        {
            public TestNASParallelBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null) 
                : base(dependencies, parameters)
            {
            }

            public bool IsNASParallelBenchServerExecuted { get; set; } = false;
            public bool IsNASParallelBenchClientExecuted { get; set; } = false;
            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            protected override VirtualClientComponent CreateNASParallelBenchClient()
            {
                var mockNASParallelBenchClientExecutor = new MockNASParallelBenchClientExecutor(this.Dependencies, this.Parameters);
                mockNASParallelBenchClientExecutor.OnExecuteAsync = () =>
                {
                    this.IsNASParallelBenchClientExecuted = true;
                };
                return mockNASParallelBenchClientExecutor;
            }
            protected override VirtualClientComponent CreateNASParallelBenchServer()
            {
                var mockNASParallelBenchServerExecutor = new MockNASParallelBenchServerExecutor(this.Dependencies, this.Parameters);
                mockNASParallelBenchServerExecutor.OnExecuteAsync = () =>
                {
                    this.IsNASParallelBenchServerExecuted = true;
                };
                return mockNASParallelBenchServerExecutor;
            }


        }

        private class MockNASParallelBenchServerExecutor : VirtualClientComponent
        {
            public MockNASParallelBenchServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }
            public Action OnExecuteAsync { get; set; }
            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() =>
                {
                    this.OnExecuteAsync?.Invoke();
                });
            }
        }

        private class MockNASParallelBenchClientExecutor : VirtualClientComponent
        {
            public MockNASParallelBenchClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }
            public Action OnExecuteAsync { get; set; }
            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() =>
                {
                    this.OnExecuteAsync?.Invoke();
                });
            }
        }
    }
}
