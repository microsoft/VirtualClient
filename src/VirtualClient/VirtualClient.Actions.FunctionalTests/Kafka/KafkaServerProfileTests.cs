// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class KafkaServerProfileTests
    {
        private DependencyFixture mockFixture;

        public void SetupFixture(PlatformID platformID)
        {
            this.mockFixture = new DependencyFixture();
            this.mockFixture
                .Setup(platformID, Architecture.X64, "Server01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
            this.SetupDefaults(platformID);
            this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "config/kraft/server.properties");
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task KafkaWorkloadProfileInstallsTheExpectedDependenciesOfServerOnGivenPlatform(PlatformID platformID)
        {
            this.SetupFixture(platformID);
            string profile = "PERF-KAFKA.json";
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations  
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "kafka");
            }
        }

        [Test]
        public async Task KafkaWorkloadProfileExecutesTheWorkloadAsExpectedOfServerOnGivenPlatform()
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(PlatformID.Win32NT);
            List<string> expectedCommands = new List<string>();
            
            string kafkaStartBatFilePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-server-start.bat");
            string kafkaStorageBatFilePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "windows", "kafka-storage.bat");
            string kafkaKraftDirectoryPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "config", "kraft");
            string serveFilePath = this.mockFixture.Combine(kafkaKraftDirectoryPath, $"server-1.properties");

            // Command to create clusterId
            expectedCommands.Add($"cmd /c {kafkaStorageBatFilePath} random-uuid");
            // Command to format server instance file
            expectedCommands.Add($"cmd /c {kafkaStorageBatFilePath} format -t  -c {serveFilePath}");
            // Start server instance
            expectedCommands.Add($"cmd /c {kafkaStartBatFilePath} {serveFilePath}");

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);
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
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-server-start.bat");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-server-stop.bat");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/windows/kafka-storage.bat");
                this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/win-x64/bin/java.exe");
            }
            else
            {
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/kafka-server-start.sh");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/kafka-server-stop.sh");
                this.mockFixture.SetupWorkloadPackage("kafka", expectedFiles: "bin/kafka-storage.sh");
                this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/linux-x64/bin/java");
            }
        }
    }
}
