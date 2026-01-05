// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DeathStarBenchExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        private void SetupTest(PlatformID platformID)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);

            this.mockPackage = new DependencyPath("deathstarbench", this.mockFixture.PlatformSpecifics.GetPackagePath("deathstarbench"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                [nameof(DeathStarBenchExecutor.PackageName)] = this.mockPackage.Name,
                [nameof(DeathStarBenchExecutor.ServiceName)] = "socialnetwork"
            };

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.mockFixture.Process;
            
        }

        [Test]
        public void DeathStarBenchExecutorDoesNotRunOnUnsupportedPlatformAsync()
        {
            this.SetupTest(PlatformID.Win32NT);

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.IsFalse(VirtualClientComponent.IsSupported(executor));
            }
        }

        [Test]
        public void DeathStarBenchExecutorThrowsOnUnsupportedLinuxDistro()
        {
            this.SetupTest(PlatformID.Unix);

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestOS",
                    LinuxDistribution = LinuxDistribution.Debian
                };

                this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.LinuxDistributionNotSupported);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutorServerDoesNotStartItselfOnMultiVMScenario()
        {
            this.SetupTest(PlatformID.Unix);

            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(!executor.IsDeathStarBenchServerExecuted);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutorInstallsDepedenciesAsExpected()
        {
            this.SetupTest(PlatformID.Unix);

            IEnumerable<string> expectedCommands = new List<string>
            {
                @"bash /home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/scripts/dockerComposeScript.sh",
                @"chmod +x ""/usr/local/bin/docker-compose""",
                @"apt install python3-venv -y",
                @"python3 -m venv /home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/venv",
                @"/home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/venv/bin/pip install -U pip",
                @"/home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/venv/bin/pip install -U setuptools",
                @"/home/user/tools/VirtualClient/packages/deathstarbench/linux-x64/venv/bin/pip install aiohttp asyncio",
                @"luarocks install luasocket",
            };

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                int executed = 0;
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (expectedCommands.Contains(arguments))
                    {
                        executed++;
                    }
                    return this.mockFixture.Process;
                };

                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                Assert.AreEqual(8, executed);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutorExecutesTheExpectedLogicWhenInNoRoleIsSpecified()
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();
            IEnumerable<string> expectedCommands = new List<string>
            {
                @"bash -c ""docker ps | wc -l"""
            };

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (expectedCommands.Contains(arguments))
                    {
                        this.mockFixture.Process.StandardOutput.Append("3");
                    }
                    return this.mockFixture.Process;
                };
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(executor.IsDeathStarBenchClientExecuted);
                Assert.IsTrue(executor.IsDeathStarBenchServerExecuted);
            }
        }

        [Test]
        public async Task DeathStarBenchExecutesTheExpectedLogicForTheClientRole()
        {
            this.SetupTest(PlatformID.Unix);

            using (TestDeathStarBenchExecutor executor = new TestDeathStarBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!executor.IsDeathStarBenchServerExecuted);
                Assert.IsTrue(executor.IsDeathStarBenchClientExecuted);
            }
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
