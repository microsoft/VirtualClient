// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class VirtualClientLoggingExtensionsTests : MockFixture
    {
        [SetUp]
        public void Setup()
        {
            this.Setup(PlatformID.Unix);
            this.Parameters[nameof(TestExecutor.LogToFile)] = true;
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsTheExpectedTelemetryEvents_1()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: true, logToFile: false, upload: false);
                Assert.IsTrue(this.Logger.MessagesLogged("TestExecutor.ProcessDetails")?.Count() == 1);
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsTheExpectedTelemetryEvents_2()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "bash", logToTelemetry: true, logToFile: false, upload: false);
                Assert.IsTrue(this.Logger.MessagesLogged("TestExecutor.bash.ProcessDetails")?.Count() == 1);
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsTheExpectedTelemetryEvents_3()
        {
            this.Logger.Clear();

            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                IEnumerable<string> toolsetResults = new List<string>
                {
                    "Any results from the execution of a toolset."
                };

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, results: toolsetResults, logToTelemetry: true, logToFile: false, upload: false);

                Assert.IsTrue(this.Logger.MessagesLogged("TestExecutor.ProcessDetails")?.Count() == 1, "Process details telemetry missing");
                Assert.IsTrue(this.Logger.MessagesLogged("TestExecutor.ProcessResults")?.Count() == 1, "Process results telemetry missing");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsTheExpectedTelemetryEvents_4()
        {
            this.Logger.Clear();

            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                IEnumerable<string> toolsetResults = new List<string>
                {
                    "Any results from the execution of a toolset."
                };

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "bash", results: toolsetResults, logToTelemetry: true, logToFile: false, upload: false);

                Assert.IsTrue(this.Logger.MessagesLogged("TestExecutor.bash.ProcessDetails")?.Count() == 1, "Process details telemetry missing");
                Assert.IsTrue(this.Logger.MessagesLogged("TestExecutor.bash.ProcessResults")?.Count() == 1, "Process results telemetry missing");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Timestamped_1()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/testexecutor/2025-07-20-193948162-anyscenario.log
                string expectedLogDirectory = this.GetLogsPath(component.TypeName.ToLowerInvariant());
                string expectedLogFile = $@"[\\/][0-9]{{4}}-[0-9]{{2}}-[0-9]{{2}}-[0-9]{{12}}-{component.Scenario}.log".ToLowerInvariant();

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: false, logToFile: true, upload: false, timestamped: true);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Timestamped_2()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/bash/2025-07-20-193948162-anyscenario.log
                string expectedLogDirectory = this.GetLogsPath("bash");
                string expectedLogFile = $@"[\\/][0-9]{{4}}-[0-9]{{2}}-[0-9]{{2}}-[0-9]{{12}}-{component.Scenario}.log".ToLowerInvariant();

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "bash", logToTelemetry: false, logToFile: true, upload: false, timestamped: true);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Not_Timestamped_1()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/testexecutor/anyscenario.log
                string expectedLogDirectory = this.GetLogsPath(component.TypeName.ToLowerInvariant());
                string expectedLogFile = $@"[\\/]{component.Scenario}.log".ToLowerInvariant();

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: false, logToFile: true, upload: false, timestamped: false);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Not_Timestamped_2()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/bash/anyscenario.log
                string expectedLogDirectory = this.GetLogsPath("bash");
                string expectedLogFile = $@"[\\/]{component.Scenario}.log".ToLowerInvariant();

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "bash", logToTelemetry: false, logToFile: true, upload: false, timestamped: false);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Not_Timestamped_3()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/bash/anyscenario.log
                string expectedLogDirectory = this.GetLogsPath("bash");
                string expectedLogFile = $@"[\\/]bash.log";

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "bash", logFileName: "bash.log", logToTelemetry: false, logToFile: true, upload: false, timestamped: false);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Not_Timestamped_5()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/bash/anyscenario.log
                string expectedLogDirectory = this.GetLogsPath("bash");
                string expectedLogFile = $@"[\\/]bash.log";

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "bash", logFileName: "bash", logToTelemetry: false, logToFile: true, upload: false, timestamped: false);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Component_LogFolderName_Defined()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            this.Parameters[nameof(TestExecutor.LogFolderName)] = "workloads";

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/workloads/bash.log
                string expectedLogDirectory = this.GetLogsPath("workloads");
                string expectedLogFile = $@"[\\/]{component.Scenario}.log".ToLowerInvariant();

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: false, logToFile: true, upload: false, timestamped: false);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedLogFile_Component_LogFolderName_Defined_And_Timestamped_1()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            this.Parameters[nameof(TestExecutor.LogFolderName)] = "workloads";

            using (TestExecutor component = new TestExecutor(this))
            {
                bool directoryCreationConfirmed = false;
                bool logFileConfirmed = false;

                // e.g.
                // /home/user/virtualclient/logs/workloads/bash.log
                string expectedLogDirectory = this.GetLogsPath("workloads");
                string expectedLogFile = $@"[\\/][0-9]{{4}}-[0-9]{{2}}-[0-9]{{2}}-[0-9]{{12}}-{component.Scenario.ToLowerInvariant()}.log";

                this.FileSystem
                    .Setup(fs => fs.Directory.CreateDirectory(expectedLogDirectory))
                    .Returns(() =>
                    {
                        directoryCreationConfirmed = true;
                        return new Mock<IDirectoryInfo>().Object;
                    });

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsRegex(expectedLogFile), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        logFileConfirmed = true;
                        return Task.CompletedTask;
                    });

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: false, logToFile: true, upload: false, timestamped: true);

                Assert.IsTrue(directoryCreationConfirmed, "Log directory not confirmed.");
                Assert.IsTrue(logFileConfirmed, "Log file not confirmed.");
            }
        }


        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedMetadataToFile()
        {
            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient");

            DateTime now = DateTime.UtcNow;
            mockProcess.Setup(proc => proc.StartTime).Returns(now.AddMinutes(-5));
            mockProcess.Setup(proc => proc.ExitTime).Returns(now);

            using (TestExecutor component = new TestExecutor(this))
            {
                bool metadataConfirmed = false;

                component.Metadata["Metadata1"] = "Value1";
                component.Metadata["Metadata2"] = "Value2";

                IDictionary<string, IConvertible> expectedMetadata = new SortedDictionary<string, IConvertible>
                {
                    { "command", "bash -c \"execute_workload.sh --logdir=/home/user/logs\"" },
                    { "workingDirectory", "/home/user/virtualclient" },
                    { "elapsedTime", @"[0-9]{2}\:[0-9]{2}\:[0-9]{2}" },
                    { "startTime", @"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}\:[0-9]{2}\:[0-9]{2}\.[0-9]{3,}Z" },
                    { "exitTime", @"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}\:[0-9]{2}\:[0-9]{2}\.[0-9]{3,}Z" },
                    { "exitCode", 0 },
                    { "experimentId", component.ExperimentId },
                    { "clientId", component.AgentId },
                    { "componentType", component.ComponentType },
                    { "machineName", Environment.MachineName },
                    { "platformArchitecture", component.PlatformSpecifics.PlatformArchitectureName },
                    { "operatingSystemVersion", Environment.OSVersion.ToString() },
                    { "operatingSystemDescription", RuntimeInformation.OSDescription },
                    { "role", component.Roles?.FirstOrDefault() },
                    { "scenario", component.Scenario },
                    { "timezone", TimeZoneInfo.Local.StandardName },
                    { "toolName", "execute_workload" },
                    { "metadata1", "Value1" },
                    { "metadata2", "Value2" }
                };

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        foreach (var entry in expectedMetadata)
                        {
                            Assert.IsTrue(Regex.IsMatch(content, $@"{entry.Key}\s*\:\s*{entry.Value}"), $"Metadata property '{entry.Key}', not found.");
                        }

                        metadataConfirmed = true;
                    })
                    .Returns(Task.CompletedTask);

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, toolName: "execute_workload", logToTelemetry: false, logToFile: true, upload: false);

                Assert.IsTrue(metadataConfirmed, "Log metadata properties not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedStandardOutputToFile()
        {
            string expectedStandardOutput = "This should be logged to file.";

            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient",
                standardOutput: expectedStandardOutput);

            using (TestExecutor component = new TestExecutor(this))
            {
                bool standardOutputConfirmed = false;

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        Assert.IsTrue(Regex.IsMatch(content, $@"##StandardOutput##[\r\n]+{expectedStandardOutput}"));
                        standardOutputConfirmed = true;
                    })
                    .Returns(Task.CompletedTask);

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: false, logToFile: true, upload: false);

                Assert.IsTrue(standardOutputConfirmed, "Standard output not confirmed.");
            }
        }

        [Test]
        public async Task LogProcessDetailsExtensionLogsToTheExpectedStandardErrorToFile()
        {
            string expectedStandardError = "This error should be logged to file.";

            Mock<IProcessProxy> mockProcess = new Mock<IProcessProxy>().Setup(
                "bash",
                "-c \"execute_workload.sh --logdir=/home/user/logs\"",
                "/home/user/virtualclient",
                standardError: expectedStandardError);

            using (TestExecutor component = new TestExecutor(this))
            {
                bool standardErrorConfirmed = false;

                this.FileSystem
                    .Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        Assert.IsTrue(Regex.IsMatch(content, $@"##StandardError##[\r\n]+{expectedStandardError}"));
                        standardErrorConfirmed = true;
                    })
                    .Returns(Task.CompletedTask);

                await component.LogProcessDetailsAsync(mockProcess.Object, EventContext.None, logToTelemetry: false, logToFile: true, upload: false);

                Assert.IsTrue(standardErrorConfirmed, "Standard error not confirmed.");
            }
        }
    }
}
