// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class RedisServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockRedisPackage;
        private InMemoryProcess memoryProcess;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.memoryProcess = new InMemoryProcess
            {
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true
            };
            this.mockRedisPackage = new DependencyPath("redis", this.fixture.GetPackagePath("redis"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockRedisPackage.Name,
                ["CommandLine"] = "--protected-mode no --io-threads {ServerThreadCount} --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes",
                ["BindToCores"] = true,
                ["Port"] = 6379,
                ["ServerInstances"] = 1,
                ["ServerThreadCount"] = 4,
                ["IsTLSEnabled"] = false
            };

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);
            this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("AnyName", "AnyDescription", 1, 4, 1, 0, true));

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockRedisPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockRedisPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            // Setup:
            // Server saves state once it is up and running.
            this.fixture.ApiClient.OnUpdateState<ServerState>(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK));
        }

        [Test]
        public async Task RedisServerExecutorConfirmsTheExpectedPackagesOnInitialization()
        {
            using (var component = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (arguments?.Contains("redis-server") == true && arguments?.Contains("--version") == true)
                    {
                        this.memoryProcess.StandardOutput = new ConcurrentBuffer(
                            new StringBuilder("Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123")
                        );
                        return this.memoryProcess;
                    }
                    return this.memoryProcess;
                };
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockRedisPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenBindingToCores()
        {
            // OLD APPROACH: Manual command tracking with List<string>
            // This demonstrates the traditional way of tracking commands in tests
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/src/redis-server\"",

                    // Start the server binded to the logical core. Values based on the parameters set at the top.
                    $"sudo bash -c \"numactl -C 0 {this.mockRedisPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if (arguments?.Contains("redis-server") == true && arguments?.Contains("--version") == true)
                    {
                        this.memoryProcess.StandardOutput = new ConcurrentBuffer(
                            new StringBuilder("Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123")
                        );
                        return this.memoryProcess;
                    }

                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [Category("POC")]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenBindingToCores_WithTracking()
        {
            // NEW APPROACH: Automatic tracking with fluent assertions
            
            // ? STEP 1: Enable automatic tracking + setup version check output (chainable!)
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(
                    "redis-server.*--version",
                    "Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123");

            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                // ? STEP 2: Execute (no manual tracking needed!)
                await executor.ExecuteAsync(CancellationToken.None);

                // ? STEP 3: Assert with fluent, self-documenting assertions
                this.fixture.Tracking.AssertCommandsExecutedInOrder(
                    // Verify chmod command
                    $@"sudo chmod \+x ""{Regex.Escape(this.mockRedisPackage.Path)}/src/redis-server""",
                    
                    // Verify redis-server startup with numactl binding
                    $@"sudo bash -c ""numactl -C 0 {Regex.Escape(this.mockRedisPackage.Path)}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes"""
                );

                // ? OPTIONAL: Additional verification types
                this.fixture.Tracking.AssertCommandExecutedTimes("chmod", 1);
                this.fixture.Tracking.AssertCommandExecutedTimes("numactl", 1);

                // ? DEBUGGING: Detailed summary available on demand
                // TestContext.WriteLine(this.fixture.Tracking.GetDetailedSummary());
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenBindingToCores_2_Server_Instances()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.ServerInstances)] = 2;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/src/redis-server\"",

                    // Server instance #1 bound to core 0 and running on port 6379
                    $"sudo bash -c \"numactl -C 0 {this.mockRedisPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\"",

                    // Server instance #2 bound to core 1 and running on port 6380
                    $"sudo bash -c \"numactl -C 1 {this.mockRedisPackage.Path}/src/redis-server --port 6380 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if (arguments?.Contains("redis-server") == true && arguments?.Contains("--version") == true)
                    {
                        this.memoryProcess.StandardOutput = new ConcurrentBuffer(
                            new StringBuilder("Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123")
                        );
                        return this.memoryProcess;
                    }
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenNotBindingToCores()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.BindToCores)] = false;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/src/redis-server\"",

                    // Start the server binded to the logical core. Values based on the parameters set at the top.
                    $"sudo bash -c \"{this.mockRedisPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if (arguments?.Contains("redis-server") == true && arguments?.Contains("--version") == true)
                    {
                        this.memoryProcess.StandardOutput = new ConcurrentBuffer(
                            new StringBuilder("Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123")
                        );
                        return this.memoryProcess;
                    }
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task RedisServerExecutorCapturesRedisVersionSuccessfully()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (arguments?.Contains("redis-server") == true && arguments?.Contains("--version") == true)
                    {
                        this.memoryProcess.StandardOutput = new ConcurrentBuffer(
                            new StringBuilder("Redis server v=7.0.15 sha=00000000 malloc=jemalloc-5.1.0 bits=64 build=abc123")
                        );
                        return this.memoryProcess;
                    }
                    return this.memoryProcess;
                };
                // Act
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                // Assert
                var messages = this.fixture.Logger.MessagesLogged($"{nameof(TestRedisServerExecutor)}.RedisVersionCaptured");
                Assert.IsNotEmpty(messages, "Expected at least one log message indicating the Redis version was captured.");
                bool versionCapturedCorrectly = messages.Any(msg =>
                {
                    var eventContext = msg.Item3 as EventContext;
                    return eventContext != null &&
                           eventContext.Properties.ContainsKey("redisVersion") &&
                           eventContext.Properties["redisVersion"].ToString() == "7.0.15";
                });
                Assert.IsTrue(versionCapturedCorrectly, "The Redis version '7.0.15' was not captured correctly in the logs.");
            }
        }

        private class TestRedisServerExecutor : RedisServerExecutor
        {
            public TestRedisServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
