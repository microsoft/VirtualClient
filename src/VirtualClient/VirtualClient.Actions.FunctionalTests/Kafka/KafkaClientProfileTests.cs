// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Differencing;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.RedisExecutor;

    [TestFixture]
    [Category("Functional")]
    public class KafkaClientProfileTests
    {
        private DependencyFixture mockFixture;
        private string clientAgentId;
        private string serverAgentId;

        public void SetupFixture(PlatformID platformID)
        {
            this.mockFixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.mockFixture.Setup(platformID, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupDefaults(platformID);
        }

        [Test]
        public void KafkaWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist()
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(PlatformID.Win32NT);
            this.mockFixture.PackageManager.Clear();
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [Test]
        public async Task KafkaWorkloadProfileExecutesTheWorkloadAsExpectedOfClientOnWindowsPlatform()
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(PlatformID.Win32NT);
            List<string> expectedCommands = new List<string>();

            string kafkaTopicScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-topics.bat");
            string kafkaProducerScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-producer-perf-test.bat");
            string kafkaConsumerScriptPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-consumer-perf-test.bat");

            // Command to create topic
            expectedCommands.Add($"cmd /c {kafkaTopicScriptPath} --create --topic sync-test-rep-one --partitions 6 --replication-factor 1 --bootstrap-server 1.2.3.5:9092");
            // Command to produce records
            expectedCommands.Add($"cmd /c {kafkaProducerScriptPath} --topic sync-test-rep-one --num-records 5000000 --record-size 100 --throughput -1 --producer-props acks=1 bootstrap.servers=1.2.3.5:9092 buffer.memory=67108864 batch.size=8196");
            // Command to consume records
            expectedCommands.Add($"cmd /c {kafkaConsumerScriptPath} --topic sync-test-rep-one --messages 5000000 --bootstrap-server 1.2.3.5:9092");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("--messages", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_KafkaConsumer.txt"));
                }
                else if (arguments.Contains("--producer-props", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_KafkaProducer.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
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
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-topics.bat");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-producer-perf-test.bat");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-consumer-perf-test.bat");
                this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/win-x64/bin/java.exe");
            }
            else
            {
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-topics.sh");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-producer-perf-test.sh");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-consumer-perf-test.sh");
                this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/linux-x64/bin/java");
            }
        }
    }
}
