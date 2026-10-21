// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class KafkaServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockKafkaPackage;
        private DependencyPath mockJdkPackage;

        public void SetupTests(PlatformID platformID)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platformID);
            this.mockKafkaPackage = new DependencyPath("kafka", this.fixture.GetPackagePath("kafka"));
            this.mockJdkPackage = new DependencyPath("javadevelopmentkit", this.fixture.GetPackagePath("javadevelopmentkit"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Kafka_Scenario",
                ["PackageName"] = this.mockKafkaPackage.Name,
                ["ServerInstances"] = 1,
                ["Port"] = 9092
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockKafkaPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockKafkaPackage);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockJdkPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockJdkPackage);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, 1)]
        [TestCase(PlatformID.Win32NT, 2)]
        [TestCase(PlatformID.Win32NT, 3)]
        public async Task KafkaServerExecutorExecutesWorkloadAsExpectedForMultipleInstancesOnWindows(PlatformID platformID, int serverInstanceCount)
        {
            this.SetupTests(platformID);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));
            this.fixture.FileSystem.Setup(fe => fe.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("node.id");
            this.fixture.FileSystem.Setup(fe => fe.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            string kafkaStartBatFilePath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-server-start.bat");
            string kafkaStorageBatFilePath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "windows", "kafka-storage.bat");
            string kafkaKraftDirectoryPath = this.fixture.Combine(this.mockKafkaPackage.Path, "config", "kraft");

            using (var executor = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.ServerInstanceCount)] = serverInstanceCount;

                List<string> expectedCommands = new List<string>()
                {
                    // Command to create clusterId
                    $"cmd /c {kafkaStorageBatFilePath} random-uuid"
                };

                for (int server = 0; server < serverInstanceCount; server++)
                {
                    string serverFilePath = this.fixture.Combine(kafkaKraftDirectoryPath, $"server-{server+1}.properties");
                    // Command to format server instance file
                    expectedCommands.Add($"cmd /c {kafkaStorageBatFilePath} format -t  -c {serverFilePath}");
                    // Start server instance
                    expectedCommands.Add($"cmd /c {kafkaStartBatFilePath} {serverFilePath}");
                }

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
        [TestCase(PlatformID.Unix, 1)]
        [TestCase(PlatformID.Unix, 2)]
        [TestCase(PlatformID.Unix, 3)]
        public async Task KafkaServerExecutorExecutesWorkloadAsExpectedForMultipleInstancesOnLinux(PlatformID platformID, int serverInstanceCount)
        {
            this.SetupTests(platformID);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Delete(It.IsAny<string>()));
            this.fixture.FileSystem.Setup(fe => fe.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("node.id");
            this.fixture.FileSystem.Setup(fe => fe.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            string kafkaStartBatFilePath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "kafka-server-start.sh");
            string kafkaStorageBatFilePath = this.fixture.Combine(this.mockKafkaPackage.Path, "bin", "kafka-storage.sh");
            string kafkaKraftDirectoryPath = this.fixture.Combine(this.mockKafkaPackage.Path, "config", "kraft");

            using (var executor = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.ServerInstanceCount)] = serverInstanceCount;

                List<string> expectedCommands = new List<string>()
                {
                    // Command to create clusterId
                    $"sudo env JAVA_HOME={this.mockJdkPackage.Path} bash {kafkaStorageBatFilePath} random-uuid"
                };

                for (int server = 0; server < serverInstanceCount; server++)
                {
                    string serverFilePath = this.fixture.Combine(kafkaKraftDirectoryPath, $"server-{server + 1}.properties");
                    // Command to format server instance file
                    expectedCommands.Add($"sudo env JAVA_HOME={this.mockJdkPackage.Path} bash {kafkaStorageBatFilePath} format -t  -c {serverFilePath}");
                    // Start server instance
                    expectedCommands.Add($"sudo env JAVA_HOME={this.mockJdkPackage.Path} bash {kafkaStartBatFilePath} {serverFilePath}");
                }

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
        [TestCase(PlatformID.Win32NT)]
        public async Task KafkaServerExecutorPreservesKraftStateAcrossInitializations(PlatformID platform)
        {
            this.SetupTests(platform);
            this.fixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("node.id=1");

            using (var executor = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                this.fixture.FileSystem.Verify(fs => fs.Directory.Delete("/tmp/kraft-combined-logs-1", true), Times.Once);
                this.fixture.FileSystem.Verify(fs => fs.File.Copy(It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task KafkaServerExecutorKeepsLiveBrokersAcrossIterationsAndStopsOwnedProcessesOnDisposal(PlatformID platform)
        {
            this.SetupTests(platform);
            this.fixture.Layout = null;
            this.fixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("node.id=1");
            RunningBrokerProcess broker = null;
            int starts = 0;
            int formats = 0;
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                if (arguments.Contains("kafka-server-start"))
                {
                    starts++;
                    broker = new RunningBrokerProcess
                    {
                        StartInfo = new ProcessStartInfo(command, arguments)
                    };
                    return broker;
                }

                if (arguments.Contains(" format "))
                {
                    formats++;
                }

                return this.fixture.Process;
            };

            using (var executor = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.AreEqual(1, starts);
                Assert.AreEqual(1, formats);
                Assert.IsFalse(broker.HasExited);
                this.fixture.FileSystem.Verify(fs => fs.Directory.Delete("/tmp/kraft-combined-logs-1", true), Times.Once);
            }

            Assert.IsTrue(broker.HasExited);
            Assert.IsTrue(broker.WasDisposed);
        }

        [Test]
        public async Task KafkaServerExecutorDoesNotReformatStorageWhenABrokerRestarts()
        {
            this.SetupTests(PlatformID.Unix);
            this.fixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("node.id=1");
            int starts = 0;
            int formats = 0;
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                if (arguments.Contains("kafka-server-start"))
                {
                    starts++;
                }

                if (arguments.Contains(" format "))
                {
                    formats++;
                }

                return this.fixture.Process;
            };

            using (var executor = new TestKafkaServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.AreEqual(2, starts);
                Assert.AreEqual(1, formats);
            }
        }

        private class RunningBrokerProcess : InMemoryProcess, IProcessProxy
        {
            private readonly TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public RunningBrokerProcess()
            {
                this.OnHasExited = () => this.exited.Task.IsCompleted;
                this.OnKill = () => this.exited.TrySetResult(true);
                this.OnDispose = () => this.WasDisposed = true;
            }

            public bool WasDisposed { get; private set; }

            async Task IProcessProxy.WaitForExitAsync(CancellationToken cancellationToken, TimeSpan? timeout)
            {
                if (timeout != null)
                {
                    await this.exited.Task.WaitAsync(timeout.Value).ConfigureAwait(false);
                }
                else
                {
                    await this.exited.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
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
            public string TestKafkaPackagePath
            {
                get
                {
                    return this.KafkaPackagePath ?? string.Empty;
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