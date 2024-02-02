// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Services.Description;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.Kafka;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.KafkaExecutor;

    [TestFixture]
    [Category("Unit")]
    public class KafkaClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockKafkaPackage;
        private string consumerResults;
        private string producerResults;

        public void SetupTests(PlatformID platformID)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platformID);
            this.mockKafkaPackage = new DependencyPath("kafka", this.fixture.GetPackagePath("kafka"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Kafka_Client_Scenario",
                ["Port"] = 9092,
                ["PackageName"] = this.mockKafkaPackage.Name,
                ["CommandLine"] = "--create --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server {0}:{Port}",
                ["CommandType"] = KafkaCommandType.Setup
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockKafkaPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.consumerResults = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, @"Examples\Kafka\KafkaConsumerResultExample.txt"));
            this.producerResults = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, @"Examples\Kafka\KafkaProducerResultExample.txt"));

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task KafkaClientExecutorConfirmsTheExpectedPackagesOnInitialization(PlatformID platformID)
        {
            this.SetupTests(platformID);
            using (var component = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockKafkaPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, KafkaCommandType.Setup, "kafka-topics.bat")]
        [TestCase(PlatformID.Win32NT, KafkaCommandType.ProducerTest, "kafka-producer-perf-test.bat")]
        [TestCase(PlatformID.Win32NT, KafkaCommandType.ConsumerTest, "kafka-consumer-perf-test.bat")]
        [TestCase(PlatformID.Unix, KafkaCommandType.Setup, "kafka-topics.sh")]
        [TestCase(PlatformID.Unix, KafkaCommandType.ProducerTest, "kafka-producer-perf-test.sh")]
        [TestCase(PlatformID.Unix, KafkaCommandType.ConsumerTest, "kafka-consumer-perf-test.sh")]
        public async Task KafkaClientExecutorConfirmsTheExpectedKafkStartaScript(PlatformID platformID, KafkaCommandType kafkaCommandType, string commandScript)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = kafkaCommandType.ToString();
            using (var component = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                Assert.IsEmpty(component.TestKafkaCommandScriptPath);
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.IsTrue(component.TestKafkaCommandScriptPath.Contains(commandScript));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        public async Task KafkaClientExecutorExecutesWorkloadForSetup(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.Setup;
            this.fixture.Parameters["CommandLine"] = "--create --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server {0}:{Port}";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-topics.bat");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {

                List<string> expectedCommands = new List<string>()
                {
                    // Command to create topic
                    $"cmd /c {KafkaCommandScriptPath} --create --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server 1.2.3.5:9092"
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        public async Task KafkaClientExecutorExecutesWorkloadForProducer(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.ProducerTest;
            this.fixture.Parameters["CommandLine"] = "--topic sync-test-rep-one --num-records 5000000 --record-size 100 --throughput -1 --producer-props acks=1 bootstrap.servers={0}:{Port} buffer.memory=67108864 batch.size=8196";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-producer-perf-test.bat");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {

                List<string> expectedCommands = new List<string>()
                {
                    // Command to produce records
                    $"cmd /c {KafkaCommandScriptPath} --topic sync-test-rep-one --num-records 5000000 --record-size 100 --throughput -1 --producer-props acks=1 bootstrap.servers=1.2.3.5:9092 buffer.memory=67108864 batch.size=8196"
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    this.fixture.Process.StandardOutput.Append(this.producerResults);
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        public async Task KafkaClientExecutorExecutesWorkloadForConsumer(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.ConsumerTest;
            this.fixture.Parameters["CommandLine"] = "--topic sync-test-rep-one --messages 5000000 --bootstrap-server {0}:{Port}";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-consumer-perf-test.bat");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {

                List<string> expectedCommands = new List<string>()
                {
                    // Command to consume records
                    $"cmd /c {KafkaCommandScriptPath} --topic sync-test-rep-one --messages 5000000 --bootstrap-server 1.2.3.5:9092"
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    this.fixture.Process.StandardOutput.Append(this.consumerResults);
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestKafkaClientExecutor : KafkaClientExecutor
        {
            public string TestKafkaCommandScriptPath
            {
                get
                {
                    return this.KafkaCommandScriptPath ?? string.Empty;
                }
                set { }
            }

            public TestKafkaClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
