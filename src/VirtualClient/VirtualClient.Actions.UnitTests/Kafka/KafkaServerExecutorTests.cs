// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.Kafka;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.RedisExecutor;
    using static VirtualClient.KafkaExecutor;

    [TestFixture]
    [Category("Unit")]
    public class KafkaServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockKafkaPackage;

        public void SetupTests(PlatformID platformID)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platformID);

            this.mockKafkaPackage = new DependencyPath("kafka", this.fixture.GetPackagePath("kafka"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Kafka_Scenario",
                ["PackageName"] = this.mockKafkaPackage.Name,
                ["ServerInstances"] = 1,
                ["Port"] = 9092
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockKafkaPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockKafkaPackage);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            // Setup:
            // Server saves state once it is up and running.
            this.fixture.ApiClient.OnUpdateState<ServerState>(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.UpdateStateAsync<KafkaServerState>(nameof(KafkaServerState), It.IsAny<Item<KafkaServerState>>(),
                It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK));
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task KafkaServerExecutorConfirmsTheExpectedPackagesOnInitialization(PlatformID platformID)
        {
            this.SetupTests(platformID);
            using (var component = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockKafkaPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "kafka-server-start.bat")]
        [TestCase(PlatformID.Unix, "kafka-server-start.sh")]
        public async Task KafkaServerExecutorConfirmsTheExpectedKafkStartaScript(PlatformID platformID, string scriptName)
        {
            this.SetupTests(platformID);
            using (var component = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                Assert.IsEmpty(component.TestKafkaStartScriptPath);
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.IsTrue(component.TestKafkaStartScriptPath.Contains(scriptName));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        // [TestCase(PlatformID.Unix)]
        public async Task KafkaServerExecutorExecutesWorkloadAsExpected(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));
            this.fixture.FileSystem.Setup(fe => fe.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("node.id");
            this.fixture.FileSystem.Setup(fe => fe.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
            string kafkaStorageBatFilePath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-storage.bat");
            using (var executor = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.ServerInstances)] = 1;

                
                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"cmd /c {kafkaStorageBatFilePath} random-uuid",

                    /*// Server instance #1 bound to core 0 and running on port 6379
                    $"sudo bash -c \"numactl -C 0 {this.mockKafkaPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\"",

                    // Server instance #2 bound to core 1 and running on port 6380
                    $"sudo bash -c \"numactl -C 1 {this.mockKafkaPackage.Path}/src/redis-server --port 6380 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""*/

                    /*"/c C:\\users\\any\\tools\\VirtualClient\\packages\\kafka\\bin\\windows\\kafka-storage.bat format -t  -c C:\\users\\any\\tools\\VirtualClient\\packages\\kafka\\config\\kraft\\server-1.properties"

"/c C:\\users\\any\\tools\\VirtualClient\\packages\\kafka\\bin\\windows\\kafka-server-start.bat C:\\users\\any\\tools\\VirtualClient\\packages\\kafka\\config\\kraft\\server-1.properties"*/
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    Console.WriteLine(exe);
                    Console.WriteLine(arguments);
                    string command = $"{exe} {arguments}";
                    expectedCommands.Remove(command);
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestKafkaServerExecutor : KafkaServerExecutor
        {
            public string TestKafkaStartScriptPath
            {
                get
                {
                    return this.KafkaStartScriptPath ?? string.Empty;
                }
                set { }
            }
            public TestKafkaServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
