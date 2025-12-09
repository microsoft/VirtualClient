// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Integration")]
    public class LogProcessDetailsTests
    {
        private IServiceCollection dependencies;
        private ISystemManagement systemManagement;

        [SetUp]
        public void SetupTest()
        {
            this.dependencies = new ServiceCollection();
            this.systemManagement = DependencyFactory.CreateSystemManager(
                Environment.MachineName,
                Guid.NewGuid().ToString().ToLowerInvariant(),
                new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture));

            Mock<IBlobManager> mockContentStore = new Mock<IBlobManager>();
            mockContentStore.Setup(store => store.StoreDescription).Returns(new DependencyBlobStore(DependencyStore.Content, "any_connection_string"));

            dependencies.AddSingleton<ISystemInfo>(systemManagement);
            dependencies.AddSingleton<ISystemManagement>(systemManagement);
            dependencies.AddSingleton<IFileSystem>(systemManagement.FileSystem);
            dependencies.AddSingleton<PlatformSpecifics>(systemManagement.PlatformSpecifics);
            dependencies.AddSingleton<IEnumerable<IBlobManager>>(new List<IBlobManager>{ mockContentStore.Object });
        }

        [Test]
        [TestCase(10, 100)]
        public void LoggingHandlesConcurrentComponentsLoggingTheSameToolsetProcessInformationToSameFolderOnTheFileSystem(int totalComponents, int totalFilesToWrite)
        {
            // Scenario:
            // Write files to the same folder for the same process/toolset concurrently to ensure that all files
            // are written and that none fail to get written due to concurrency issues.

            int expectedFileCount = totalComponents * totalFilesToWrite;
            string targetLogDirectory = this.systemManagement.PlatformSpecifics.GetLogsPath("concurrent_logging_test_1", "logs");
            string targetUploadsDirectory = this.systemManagement.PlatformSpecifics.GetLogsPath("concurrent_logging_test_1", "uploads");

            this.systemManagement.PlatformSpecifics.LogsDirectory = targetLogDirectory;
            this.systemManagement.PlatformSpecifics.ContentUploadsDirectory = targetUploadsDirectory;

            // Ensure we have are starting anew with the log-related directories.
            DeletePaths(targetLogDirectory, targetUploadsDirectory);

            // Execute each component in parallel.
            Parallel.For(0, totalComponents, (componentNumber) =>
            {
                using (TestExecutor component = new TestExecutor(this.dependencies, null))
                {
                    for (int fileNum = 1; fileNum <= totalFilesToWrite; fileNum++)
                    {
                        try
                        {
                            ProcessDetails processDetails = new ProcessDetails
                            {
                                ExitCode = 0,
                                CommandLine = $"Test.exe --file-number={fileNum}",
                                Id = fileNum,
                                ToolName = "Test",
                                StandardOutput =
                                    $"Scenario:{Environment.NewLine}" +
                                    $"Components running concurrently writing log files to the same folder for the same process/toolset concurrently {Environment.NewLine}" +
                                    $"to ensure that all files are written and that none fail to get written due to concurrency issues.",
                                StartTime = DateTime.UtcNow.AddMinutes(-1),
                                ExitTime = DateTime.UtcNow,
                                WorkingDirectory = Environment.CurrentDirectory
                            };

                            EventContext telemetryContext = new EventContext(Guid.NewGuid());
                            component.LogProcessDetailsAsync(processDetails, telemetryContext, logFileName: "test.log", logToFile: true, timestamped: true, upload: true)
                                .GetAwaiter().GetResult();
                        }
                        catch (Exception exc)
                        {
                            Assert.Fail($"File logging error: {exc.Message}");
                        }
                    }
                }
            });

            string[] filesWritten = Directory.GetFiles(targetLogDirectory, "*.log", SearchOption.AllDirectories);
            Assert.IsNotEmpty(filesWritten);
            Assert.AreEqual(expectedFileCount, filesWritten.Length);

            string[] uploadFilesWritten = Directory.GetFiles(targetUploadsDirectory, "*.json", SearchOption.AllDirectories);
            Assert.IsNotEmpty(uploadFilesWritten);
            Assert.AreEqual(expectedFileCount, uploadFilesWritten.Length);
        }

        [Test]
        [TestCase(10, 100)]
        public void LoggingHandlesConcurrentComponentsLoggingDifferentToolsetProcessInformationToSameFolderOnTheFileSystem(int totalComponents, int totalFilesToWrite)
        {
            // Scenario:
            // Write files to the same folder for the same process/toolset concurrently to ensure that all files
            // are written and that none fail to get written due to concurrency issues.

            int expectedFileCount = totalComponents * totalFilesToWrite;
            string targetLogDirectory = this.systemManagement.PlatformSpecifics.GetLogsPath("concurrent_logging_test_2", "logs");
            string targetUploadsDirectory = this.systemManagement.PlatformSpecifics.GetLogsPath("concurrent_logging_test_2", "uploads");

            this.systemManagement.PlatformSpecifics.LogsDirectory = targetLogDirectory;
            this.systemManagement.PlatformSpecifics.ContentUploadsDirectory = targetUploadsDirectory;

            // Ensure we have are starting anew with the log-related directories.
            DeletePaths(targetLogDirectory, targetUploadsDirectory);

            // Execute each component in parallel.
            Parallel.For(0, totalComponents, (componentNumber) =>
            {
                using (TestExecutor component = new TestExecutor(this.dependencies, null))
                {
                    for (int fileNum = 1; fileNum <= totalFilesToWrite; fileNum++)
                    {
                        try
                        {
                            ProcessDetails processDetails = new ProcessDetails
                            {
                                ExitCode = 0,
                                CommandLine = $"Test.exe --file-number={fileNum}",
                                Id = fileNum,
                                ToolName = $"Test",
                                StandardOutput =
                                    $"Scenario:{Environment.NewLine}" +
                                    $"Components running concurrently writing log files to the same folder for the same process/toolset concurrently {Environment.NewLine}" +
                                    $"to ensure that all files are written and that none fail to get written due to concurrency issues.",
                                StartTime = DateTime.UtcNow.AddMinutes(-1),
                                ExitTime = DateTime.UtcNow,
                                WorkingDirectory = Environment.CurrentDirectory
                            };

                            EventContext telemetryContext = new EventContext(Guid.NewGuid());
                            component.LogProcessDetailsAsync(processDetails, telemetryContext, logFileName: $"test{componentNumber}.log", logToFile: true, timestamped: true, upload: true)
                                .GetAwaiter().GetResult();
                        }
                        catch (Exception exc)
                        {
                            Assert.Fail($"File logging error: {exc.Message}");
                        }
                    }
                }
            });

            string[] filesWritten = Directory.GetFiles(targetLogDirectory, "*.log", SearchOption.AllDirectories);
            Assert.IsNotEmpty(filesWritten);
            Assert.AreEqual(expectedFileCount, filesWritten.Length);

            string[] uploadFilesWritten = Directory.GetFiles(targetUploadsDirectory, "*.json", SearchOption.AllDirectories);
            Assert.IsNotEmpty(uploadFilesWritten);
            Assert.AreEqual(expectedFileCount, uploadFilesWritten.Length);
        }

        [Test]
        [TestCase(10, 100)]
        public void LoggingHandlesConcurrentComponentsLoggingDifferentToolsetProcessInformationToDifferentFoldersOnTheFileSystem(int totalComponents, int totalFilesToWrite)
        {
            // Scenario:
            // Write files to the same folder for the same process/toolset concurrently to ensure that all files
            // are written and that none fail to get written due to concurrency issues.

            int expectedFileCount = totalComponents * totalFilesToWrite;
            string targetLogDirectory = this.systemManagement.PlatformSpecifics.GetLogsPath("concurrent_logging_test_3", "logs");
            string targetUploadsDirectory = this.systemManagement.PlatformSpecifics.GetLogsPath("concurrent_logging_test_3", "uploads");

            this.systemManagement.PlatformSpecifics.LogsDirectory = targetLogDirectory;
            this.systemManagement.PlatformSpecifics.ContentUploadsDirectory = targetUploadsDirectory;

            // Ensure we have are starting anew with the log-related directories.
            DeletePaths(targetLogDirectory, targetUploadsDirectory);

            // Execute each component in parallel.
            Parallel.For(0, totalComponents, (componentNumber) =>
            {
                using (TestExecutor component = new TestExecutor(this.dependencies, null))
                {
                    for (int fileNum = 1; fileNum <= totalFilesToWrite; fileNum++)
                    {
                        try
                        {
                            ProcessDetails processDetails = new ProcessDetails
                            {
                                ExitCode = 0,
                                CommandLine = $"Test.exe --file-number={fileNum}",
                                Id = fileNum,
                                ToolName = $"Test{componentNumber}",
                                StandardOutput =
                                    $"Scenario:{Environment.NewLine}" +
                                    $"Components running concurrently writing log files to the same folder for the same process/toolset concurrently {Environment.NewLine}" +
                                    $"to ensure that all files are written and that none fail to get written due to concurrency issues.",
                                StartTime = DateTime.UtcNow.AddMinutes(-1),
                                ExitTime = DateTime.UtcNow,
                                WorkingDirectory = Environment.CurrentDirectory
                            };

                            EventContext telemetryContext = new EventContext(Guid.NewGuid());
                            component.LogProcessDetailsAsync(processDetails, telemetryContext, logFileName: $"test.log", logToFile: true, timestamped: true, upload: true)
                                .GetAwaiter().GetResult();
                        }
                        catch (Exception exc)
                        {
                            Assert.Fail($"File logging error: {exc.Message}");
                        }
                    }
                }
            });

            string[] filesWritten = Directory.GetFiles(targetLogDirectory, "*.log", SearchOption.AllDirectories);
            Assert.IsNotEmpty(filesWritten);
            Assert.AreEqual(expectedFileCount, filesWritten.Length);

            string[] uploadFilesWritten = Directory.GetFiles(targetUploadsDirectory, "*.json", SearchOption.AllDirectories);
            Assert.IsNotEmpty(uploadFilesWritten);
            Assert.AreEqual(expectedFileCount, uploadFilesWritten.Length);
        }

        private static void DeletePaths(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }
    }
}
