// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DeathStarBenchExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.SetupDefaultMockBehavior(PlatformID.Unix);
        }

        [Test]
        public async Task DeathStarBenchExecutorDoesNotRunOnUnsupportedPlatformAsync()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT);

            using (TestDeathStarBenchExecutor deathStarBenchExecutor = new TestDeathStarBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await deathStarBenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!deathStarBenchExecutor.IsDeathStarBenchClientExecuted);
                Assert.IsTrue(!deathStarBenchExecutor.IsDeathStarBenchServerExecuted);
            }
        }

        [Test]
        public void DeathStarBenchExecutorThrowsOnUnsupportedLinuxDistro()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            using (TestDeathStarBenchExecutor DeathStarBenchExecutor = new TestDeathStarBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestOS",
                    LinuxDistribution = LinuxDistribution.Debian
                };
                this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => DeathStarBenchExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.LinuxDistributionNotSupported);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutorServerDoesNotStartItselfOnMultiVMScenario()
        {
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            TestDeathStarBenchExecutor component = new TestDeathStarBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(!component.IsDeathStarBenchServerExecuted);
        }

        [Test]
        public async Task DeathStarBenchExecutorInstallsDepedenciesAsExpected()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            IEnumerable<string> expectedCommands = new List<string>
            {
                @"bash /home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/scripts/dockerComposeScript.sh",
                @"chmod +x ""/usr/local/bin/docker-compose""",
                @"python3 -m pip install -U pip",
                @"python3 -m pip install -U setuptools",
                @"-H python3 -m pip install aiohttp asyncio",
                @"luarocks install luasocket",
            };

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                int executed = 0;
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (expectedCommands.Contains(arguments))
                    {
                        executed++;
                    }
                    return this.fixture.Process;
                };

                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                Assert.AreEqual(6, executed);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutorExecutesTheExpectedLogicWhenInNoRoleIsSpecified()
        {
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            IEnumerable<string> expectedCommands = new List<string>
            {
                @"bash -c ""docker ps | wc -l"""
            };

            using (TestDeathStarBenchExecutor component = new TestDeathStarBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (expectedCommands.Contains(arguments))
                    {
                        this.fixture.Process.StandardOutput.Append("3");
                    }
                    return this.fixture.Process;
                };
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(component.IsDeathStarBenchClientExecuted);
                Assert.IsTrue(component.IsDeathStarBenchServerExecuted);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutesTheExpectedLogicForTheClientRole()
        {
            using (TestDeathStarBenchExecutor component = new TestDeathStarBenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!component.IsDeathStarBenchServerExecuted);
                Assert.IsTrue(component.IsDeathStarBenchClientExecuted);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {

            this.fixture.Setup(platformID);

            this.mockPath = this.fixture.Create<DependencyPath>();
            DependencyPath mockPackage = new DependencyPath("deathstarbench", this.fixture.PlatformSpecifics.GetPackagePath("deathstarbench"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                [nameof(DeathStarBenchExecutor.PackageName)] = this.mockPath.Name,
                [nameof(DeathStarBenchExecutor.ServiceName)] = "socialnetwork"
            };

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.fixture.Process;
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);
        }

        private class TestDeathStarBenchExecutor : DeathStarBenchExecutor
        {
            public TestDeathStarBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public bool IsDeathStarBenchServerExecuted { get; set; } = false;

            public bool IsDeathStarBenchClientExecuted { get; set; } = false;

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            protected override VirtualClientComponent CreateWorkloadClient()
            {
                var mockDeathStarBenchClientExecutor = new MockDeathStarBenchClientExecutor(this.Dependencies, this.Parameters);
                mockDeathStarBenchClientExecutor.OnExecuteAsync = () =>
                {
                    this.IsDeathStarBenchClientExecuted = true;
                };
                return mockDeathStarBenchClientExecutor;
            }
            protected override VirtualClientComponent CreateWorkloadServer()
            {
                var mockDeathStarBenchServerExecutor = new MockDeathStarBenchServerExecutor(this.Dependencies, this.Parameters);
                mockDeathStarBenchServerExecutor.OnExecuteAsync = () =>
                {
                    this.IsDeathStarBenchServerExecuted = true;
                };
                return mockDeathStarBenchServerExecutor;
            }


        }

        private class MockDeathStarBenchServerExecutor : VirtualClientComponent
        {
            public MockDeathStarBenchServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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

        private class MockDeathStarBenchClientExecutor : VirtualClientComponent
        {
            public MockDeathStarBenchClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
