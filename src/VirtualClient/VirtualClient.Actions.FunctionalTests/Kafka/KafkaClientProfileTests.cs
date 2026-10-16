// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class KafkaClientProfileTests
    {
        private DependencyFixture mockFixture;
        private string clientAgentId;
        private string serverAgentId;

        public void SetupFixture(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);

            this.mockFixture.Setup(platformID, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupDefaults(platformID);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void KafkaWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(PlatformID platformID, Architecture architecture)
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(platformID, architecture);
            this.mockFixture.PackageManager.Clear();
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileExecutesTheWorkloadAsExpectedForClientOnWindows(PlatformID platformID, Architecture architecture)
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(platformID, architecture);
            List<string> expectedCommands = new List<string>();

            string kafkaTopicScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-topics.bat");
            string kafkaProducerScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-producer-perf-test.bat");
            string kafkaConsumerScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-consumer-perf-test.bat");

            // Command to create topic
            expectedCommands.Add($"cmd /c {kafkaTopicScriptPath} --create --if-not-exists --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server 1.2.3.5:9092");
            // Command to produce records
            expectedCommands.Add($"cmd /c {kafkaProducerScriptPath} --topic sync-test-rep-one --num-records 5000000 --record-size 100 --throughput -1 --producer-props acks=1 bootstrap.servers=1.2.3.5:9092 buffer.memory=67108864 batch.size=8196");
            // Command to consume records
            expectedCommands.Add($"cmd /c {kafkaConsumerScriptPath} --topic sync-test-rep-one --messages 5000000 --bootstrap-server 1.2.3.5:9092");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--messages", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(MockFixture.ReadTestResourcesFile("Results_KafkaConsumer.txt"));
                }
                else if (arguments.Contains("--producer-props", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(MockFixture.ReadTestResourcesFile("Results_KafkaProducer.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileExecutesTheWorkloadAsExpectedForClientOnLinux(PlatformID platformID, Architecture architecture)
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(platformID, architecture);
            List<string> expectedCommands = new List<string>();

            string kafkaPackagePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka");
            string jdkPackagePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "javadevelopmentkit");
            string kafkaTopicScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "kafka-topics.sh");
            string kafkaProducerScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "kafka-producer-perf-test.sh");
            string kafkaConsumerScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "kafka-consumer-perf-test.sh");

            // Command to give permissions to directory
            expectedCommands.Add($"sudo chmod -R 2777 \"{kafkaPackagePath}\"");
            // Command to create topic
            expectedCommands.Add($"sudo env JAVA_HOME={jdkPackagePath} bash {kafkaTopicScriptPath} --create --if-not-exists --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server 1.2.3.5:9092");
            // Command to produce records
            expectedCommands.Add($"sudo env JAVA_HOME={jdkPackagePath} bash {kafkaProducerScriptPath} --topic sync-test-rep-one --num-records 5000000 --record-size 100 --throughput -1 --producer-props acks=1 bootstrap.servers=1.2.3.5:9092 buffer.memory=67108864 batch.size=8196");
            // Command to consume records
            expectedCommands.Add($"sudo env JAVA_HOME={jdkPackagePath} bash {kafkaConsumerScriptPath} --topic sync-test-rep-one --messages 5000000 --bootstrap-server 1.2.3.5:9092");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--messages", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(MockFixture.ReadTestResourcesFile("Results_KafkaConsumer.txt"));
                }
                else if (arguments.Contains("--producer-props", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(MockFixture.ReadTestResourcesFile("Results_KafkaProducer.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private void SetupDefaults(PlatformID platformID)
        {
            Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java.exe" }
                };

            if (platformID == PlatformID.Win32NT)
            {
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-topics.bat");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-producer-perf-test.bat");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-consumer-perf-test.bat");
                this.mockFixture.SetupPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/win-x64/bin/java.exe");
            }
            else
            {
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-topics.sh");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-producer-perf-test.sh");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-consumer-perf-test.sh");
                this.mockFixture.SetupPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/linux-x64/bin/java");
            }
        }
    }
}