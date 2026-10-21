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
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class KafkaServerProfileTests
    {
        private DependencyFixture mockFixture;

        public void SetupFixture(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture = new DependencyFixture();
            this.mockFixture
                .Setup(platformID, architecture, "Server01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
            this.SetupDefaults(platformID);
            this.mockFixture.SetupPackage("kafka", expectedFiles: "config/kraft/server.properties");
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileInstallsTheExpectedServerDependenciesOnWindows(PlatformID platformID, Architecture architecture)
        {
            this.SetupFixture(platformID, architecture);
            string profile = "PERF-KAFKA.json";

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "kafka");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileInstallsTheExpectedServerDependenciesOnLinux(PlatformID platformID, Architecture architecture)
        {
            this.SetupFixture(platformID, architecture);
            string profile = "PERF-KAFKA.json";

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("format"))
                {
                    this.mockFixture.SetupDirectory("/tmp/kraft-combined-logs-1");
                    this.mockFixture.SetupDirectory("/tmp/kraft-combined-logs-2");
                    this.mockFixture.SetupDirectory("/tmp/kraft-combined-logs-3");
                }

                return process;
            };

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "kafka");
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileExecutesTheWorkloadAsExpectedForServerOnWindows(PlatformID platformID, Architecture architecture)
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(platformID, architecture);
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

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileExecutesTheWorkloadAsExpectedForServerOnLinux(PlatformID platformID, Architecture architecture)
        {
            string profile = "PERF-KAFKA.json";
            this.SetupFixture(platformID, architecture);
            List<string> expectedCommands = new List<string>();

            string kafkaPackagePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka");
            string jdkPackagePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "javadevelopmentkit");
            string kafkaStartBatFilePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "kafka-server-start.sh");
            string kafkaStorageBatFilePath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "bin", "kafka-storage.sh");
            string kafkaKraftDirectoryPath = this.mockFixture.Combine(this.mockFixture.PackagesDirectory, "kafka", "config", "kraft");
            string serverFilePath = this.mockFixture.Combine(kafkaKraftDirectoryPath, $"server-1.properties");

            // Command to give permissions to directory
            expectedCommands.Add($"sudo chmod -R 2777 \"{kafkaPackagePath}\"");
            // Command to create clusterId
            expectedCommands.Add($"sudo env JAVA_HOME={jdkPackagePath} bash {kafkaStorageBatFilePath} random-uuid");
            // Command to format server instance file
            expectedCommands.Add($"sudo env JAVA_HOME={jdkPackagePath} bash {kafkaStorageBatFilePath} format -t  -c {serverFilePath}");
            // Start server instance
            expectedCommands.Add($"sudo env JAVA_HOME={jdkPackagePath} bash {kafkaStartBatFilePath} {serverFilePath}");

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("format"))
                {
                    this.mockFixture.SetupDirectory("/tmp/kraft-combined-logs-1");
                    this.mockFixture.SetupDirectory("/tmp/kraft-combined-logs-2");
                    this.mockFixture.SetupDirectory("/tmp/kraft-combined-logs-3");
                }

                return process;
            };

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
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
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-server-start.bat");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-server-stop.bat");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/windows/kafka-storage.bat");
                this.mockFixture.SetupPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/win-x64/bin/java.exe");
            }
            else
            {
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/kafka-server-start.sh");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/kafka-server-stop.sh");
                this.mockFixture.SetupPackage("kafka", expectedFiles: "bin/kafka-storage.sh");
                this.mockFixture.SetupPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/linux-x64/bin/java");
            }
        }
    }
}