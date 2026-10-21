// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class KafkaClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockKafkaPackage;
        private DependencyPath mockJdkPackage;
        private string consumerResults;
        private string producerResults;

        public void SetupTests(PlatformID platformID)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platformID);
            this.mockKafkaPackage = new DependencyPath("kafka", this.fixture.GetPackagePath("kafka"));
            this.mockJdkPackage = new DependencyPath("javadevelopmentkit", this.fixture.GetPackagePath("javadevelopmentkit"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Kafka_Client_Scenario",
                ["Port"] = 9092,
                ["PackageName"] = this.mockKafkaPackage.Name,
                ["CommandLine"] = "--create --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server {0}:{Port}",
                ["CommandType"] = KafkaCommandType.CreateTopic
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockKafkaPackage);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockJdkPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockJdkPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.consumerResults = MockFixture.ReadTestExamplesFile("Kafka", "KafkaConsumerResultExample.txt");
            this.producerResults = MockFixture.ReadTestExamplesFile("Kafka", "KafkaProducerResultExample.txt");

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
        [TestCase(PlatformID.Win32NT, KafkaCommandType.CreateTopic, "kafka-topics.bat")]
        [TestCase(PlatformID.Win32NT, KafkaCommandType.ProducerTest, "kafka-producer-perf-test.bat")]
        [TestCase(PlatformID.Win32NT, KafkaCommandType.ConsumerTest, "kafka-consumer-perf-test.bat")]
        [TestCase(PlatformID.Unix, KafkaCommandType.CreateTopic, "kafka-topics.sh")]
        [TestCase(PlatformID.Unix, KafkaCommandType.ProducerTest, "kafka-producer-perf-test.sh")]
        [TestCase(PlatformID.Unix, KafkaCommandType.ConsumerTest, "kafka-consumer-perf-test.sh")]
        public async Task KafkaClientExecutorConfirmsTheExpectedKafkaStartScript(PlatformID platformID, KafkaCommandType kafkaCommandType, string commandScript)
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
        public async Task KafkaClientExecutorExecutesWorkloadForSetupOnWindows(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.CreateTopic;
            this.fixture.Parameters["CommandLine"] = "--create --if-not-exists --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server {0}:{Port}";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-topics.bat");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Command to create topic
                    $"cmd /c {KafkaCommandScriptPath} --create --if-not-exists --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server 1.2.3.5:9092"
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
        [TestCase(PlatformID.Unix)]
        public async Task KafkaClientExecutorExecutesWorkloadForSetupOnLinux(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.CreateTopic;
            this.fixture.Parameters["CommandLine"] = "--create --if-not-exists --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server {0}:{Port}";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaPackagePath = this.mockKafkaPackage.Path;
            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "kafka-topics.sh");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Command to create topic
                    $"sudo chmod -R 2777 \"{KafkaPackagePath}\"",
                    $"sudo env JAVA_HOME={this.mockJdkPackage.Path} bash {KafkaCommandScriptPath} --create --if-not-exists --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server 1.2.3.5:9092"
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
        public async Task KafkaClientExecutorExecutesWorkloadForProducerOnWindows(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.ProducerTest;
            this.fixture.Parameters["RecordSize"] = 100;
            this.fixture.Parameters["RecordCount"] = 5000000;
            this.fixture.Parameters["CommandLine"] = "--topic sync-test-rep-one --num-records {RecordCount} --record-size {RecordSize} --throughput -1 --producer-props acks=1 bootstrap.servers={0}:{Port} buffer.memory=67108864 batch.size=8196";
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
        [TestCase(PlatformID.Unix)]
        public async Task KafkaClientExecutorExecutesWorkloadForProducerOnLinux(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.ProducerTest;
            this.fixture.Parameters["RecordSize"] = 100;
            this.fixture.Parameters["RecordCount"] = 5000000;
            this.fixture.Parameters["CommandLine"] = "--topic sync-test-rep-one --num-records {RecordCount} --record-size {RecordSize} --throughput -1 --producer-props acks=1 bootstrap.servers={0}:{Port} buffer.memory=67108864 batch.size=8196";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "kafka-producer-perf-test.sh");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {

                List<string> expectedCommands = new List<string>()
                {
                    // Command to produce records
                    $"sudo env JAVA_HOME={this.mockJdkPackage.Path} bash {KafkaCommandScriptPath} --topic sync-test-rep-one --num-records 5000000 --record-size 100 --throughput -1 --producer-props acks=1 bootstrap.servers=1.2.3.5:9092 buffer.memory=67108864 batch.size=8196"
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
        public async Task KafkaClientExecutorExecutesWorkloadForConsumerOnWindows(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.ConsumerTest;
            this.fixture.Parameters["Messages"] = 5000000;
            this.fixture.Parameters["CommandLine"] = "--topic sync-test-rep-one --messages {Messages} --bootstrap-server {0}:{Port}";
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

        [Test]
        [TestCase(PlatformID.Unix)]
        public async Task KafkaClientExecutorExecutesWorkloadForConsumerOnLinux(PlatformID platformID)
        {
            this.SetupTests(platformID);
            this.fixture.Parameters["CommandType"] = KafkaCommandType.ConsumerTest;
            this.fixture.Parameters["Messages"] = 5000000;
            this.fixture.Parameters["CommandLine"] = "--topic sync-test-rep-one --messages {Messages} --bootstrap-server {0}:{Port}";
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));

            string KafkaCommandScriptPath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "kafka-consumer-perf-test.sh");

            using (var executor = new TestKafkaClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {

                List<string> expectedCommands = new List<string>()
                {
                    // Command to consume records
                    $"sudo env JAVA_HOME={this.mockJdkPackage.Path} bash {KafkaCommandScriptPath} --topic sync-test-rep-one --messages 5000000 --bootstrap-server 1.2.3.5:9092"
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