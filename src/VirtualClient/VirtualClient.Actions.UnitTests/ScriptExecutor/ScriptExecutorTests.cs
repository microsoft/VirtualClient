// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ScriptExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(ScriptExecutorTests), "Examples", "ScriptExecutor");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string exampleResults;

        public void SetupTest(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

            this.mockPackage = new DependencyPath("workloadPackage", this.mockFixture.PlatformSpecifics.GetPackagePath("workloadPackage"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.exampleResults = File.ReadAllText(Path.Combine(ScriptExecutorTests.ExamplesDirectory, "validJsonExample.json"));

            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.Setup(fe => fe.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.mockFixture.FileSystem.Setup(fe => fe.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns((string filePath) =>
                {
                    if (this.mockFixture.Platform == PlatformID.Unix)
                    {
                        // Handle Unix-style paths explicitly
                        filePath = filePath.Replace('\\', '/'); // Normalize to Unix-style path
                        int lastSlashIndex = filePath.LastIndexOf('/');
                        return lastSlashIndex > 0 ? filePath.Substring(0, lastSlashIndex) : null;
                    }
                    else
                    {
                        // Handle Windows-style paths explicitly
                        filePath = filePath.Replace('/', '\\'); // Normalize to Windows-style path
                        int lastBackslashIndex = filePath.LastIndexOf('\\');
                        return lastBackslashIndex > 1 ? filePath.Substring(0, lastBackslashIndex) : null;
                    }
                });

            this.mockFixture.FileSystem.Setup(fe => fe.Path.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns("genericScript");

            this.mockFixture.FileSystem.Setup(fe => fe.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string path1, string path2) =>
                {
                    if (this.mockFixture.Platform == PlatformID.Unix)
                    {
                        // Normalize to Unix-style path
                        path1 = path1.Replace('\\', '/').TrimEnd('/');
                        path2 = path2.Replace('\\', '/').TrimStart('/');
                        return $"{path1}/{path2}";
                    }
                    else
                    {
                        // Normalize to Windows-style path
                        path1 = path1.Replace('/', '\\').TrimEnd('\\');
                        path2 = path2.Replace('/', '\\').TrimStart('\\');
                        return $"{path1}\\{path2}";
                    }
                });

            this.mockFixture.FileSystem.Setup(fe => fe.Path.GetFullPath(It.IsAny<string>()))
                .Returns((string path1) =>
                {
                    if (this.mockFixture.Platform == PlatformID.Unix)
                    {
                        // Simulate Unix-style behavior
                        string fullPath = Path.GetFullPath(path1).Replace('\\', '/'); // Normalize to Unix-style path
                        int colonIndex = fullPath.IndexOf(':');
                        if (colonIndex != -1)
                        {
                            fullPath = fullPath.Substring(colonIndex + 1); // Remove the drive letter and colon
                        }
                        return fullPath;
                    }
                    else
                    {
                        string winPath = path1.Replace('/', '\\');
                        string drive = winPath.Length >= 2 && winPath[1] == ':' ? winPath.Substring(0, 2) : "";
                        string[] segments = winPath.Substring(drive.Length)
                                                 .Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                        var pathParts = new Stack<string>();
                        foreach (var segment in segments)
                        {
                            if (segment == ".." && pathParts.Count > 0) pathParts.Pop();
                            else if (segment != "." && segment != "..") pathParts.Push(segment);
                        }

                        var resolved = string.Join("\\", pathParts.Reverse());
                        return string.IsNullOrEmpty(drive) ? resolved : $"{drive}\\{resolved}";
                    }
                });

            this.mockFixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(ScriptExecutor.PackageName), "workloadPackage" },
                { nameof(ScriptExecutor.Scenario), "GenericScriptWorkload" },
                { nameof(ScriptExecutor.CommandLine), "parameter1 parameter2" },
                { nameof(ScriptExecutor.ScriptPath), "genericScript.bat" },
                { nameof(ScriptExecutor.LogPaths), "*.log;*.txt;*.json" },
                { nameof(ScriptExecutor.ToolName), "GenericTool" }
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ScriptExecutorThrowsOnInitializationWhenPackageNameIsProvidedButTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ScriptExecutorThrowsOnInitializationWhenNoFileExistsAtExecutablePath(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>()))
                .Returns(false);

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
                Assert.IsTrue(exception.Message.StartsWith($"The expected workload script was not found at '{executor.ExecutablePath}'"));
            }
        }

        [Test]
        [TestCase(@"genericScript.bat", true, true)]
        [TestCase(@"genericScript.bat", true, false)]
        [TestCase(@"..\..\..\subfolder1\genericScript.bat", true, true)]
        [TestCase(@"..\..\..\subfolder1\genericScript.bat", true, false)]
        [TestCase(@"..\..\..\subfolder1\genericScript.bat", false, true)]
        [TestCase(@"..\..\..\subfolder1\genericScript.bat", false, false)]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task ScriptExecutorExecutesTheCorrectWorkloadCommandsInWindows(string command, bool packageAvailable, bool runElevated)
        {
            this.SetupTest(PlatformID.Win32NT);
            this.mockFixture.Parameters[nameof(ScriptExecutor.RunElevated)] = runElevated;
            this.mockFixture.Parameters[nameof(ScriptExecutor.ScriptPath)] = command;

            string platformSpecificPath = packageAvailable ? Path.Combine("win-x64") : string.Empty;
            this.mockFixture.Parameters[nameof(ScriptExecutor.PackageName)] = packageAvailable ? "workloadPackage" : string.Empty;
            string workingDir = packageAvailable ? this.mockPackage.Path : this.mockFixture.PlatformSpecifics.CurrentDirectory;

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"{Path.GetFullPath(Path.Combine(workingDir, platformSpecificPath, command))} parameter1 parameter2";
                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if (expectedCommand == $"{exe} {arguments}")
                    {
                        commandExecuted = true;
                    }

                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(@"genericScript.sh", true, true)]
        [TestCase(@"genericScript.sh", true, false)]
        [TestCase(@"../../../subfolder1/genericScript.sh", true, true)]
        [TestCase(@"../../../subfolder1/genericScript.sh", true, false)]
        [TestCase(@"../../../subfolder1/genericScript.sh", false, true)]
        [TestCase(@"../../../subfolder1/genericScript.sh", false, false)]
        public async Task ScriptExecutorExecutesTheCorrectWorkloadCommandsInUnix(string command, bool packageAvailable, bool runElevated)
        {
            this.SetupTest(PlatformID.Unix);
            this.mockFixture.Parameters[nameof(ScriptExecutor.RunElevated)] = runElevated;
            this.mockFixture.Parameters[nameof(ScriptExecutor.ScriptPath)] = command;

            string platformSpecificPath = packageAvailable ? Path.Combine("linux-x64") : string.Empty;
            this.mockFixture.Parameters[nameof(ScriptExecutor.PackageName)] = packageAvailable ? "workloadPackage" : string.Empty;
            string workingDir = packageAvailable ? this.mockPackage.Path : this.mockFixture.PlatformSpecifics.CurrentDirectory;

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                bool commandExecuted = false;

                // Construct the expected command and remove the drive letter for Unix-style paths
                string fullPath = Path.GetFullPath(Path.Combine(workingDir, platformSpecificPath, command));
                string unixStylePath = fullPath.Replace('\\', '/'); // Convert to Unix-style path
                if (unixStylePath.Contains(":"))
                {
                    unixStylePath = unixStylePath.Substring(unixStylePath.IndexOf(':') + 1); // Remove drive letter
                }

                string expectedCommand = $"{(runElevated ? "sudo" : string.Empty)} {unixStylePath} parameter1 parameter2".Trim();

                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if (expectedCommand == $"{exe} {arguments}")
                    {
                        commandExecuted = true;
                    }

                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\")]
        [TestCase(PlatformID.Unix, @"/linux-x64/")]
        public void ScriptExecutorDoesNotThrowWhenTheWorkloadDoesNotProduceValidMetricsFile(PlatformID platform, string platformSpecificPath)
        {
            this.SetupTest(platform);
            this.mockFixture.File.Setup(fe => fe.Exists($"{this.mockPackage.Path}{platformSpecificPath}test-metrics.json"))
                .Returns(false);

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, true)]
        [TestCase(PlatformID.Win32NT, false)]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void ScriptExecutorMovesTheLogFilesToCorrectDirectory_Win(PlatformID platform, bool packageAvailable)
        {
            this.SetupTest(platform);
            string platformSpecificPath = packageAvailable ? Path.Combine("win-x64") : string.Empty;
            this.mockFixture.Parameters["PackageName"] = packageAvailable ? "workloadPackage" : string.Empty;
            string workingDir = packageAvailable ? this.mockPackage.Path : this.mockFixture.PlatformSpecifics.CurrentDirectory;

            bool destinitionPathCorrect = false;
            bool sourcePathCorrect = false;
            string logsDir = this.mockFixture.PlatformSpecifics.LogsDirectory.Replace(@"\", @"\\");

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                this.mockFixture.File.Setup(fe => fe.Move(It.IsAny<string>(), It.IsAny<string>(), true))
                .Callback<string, string, bool>((sourcePath, destinitionPath, overwrite) =>
                {
                    destinitionPathCorrect = Regex.IsMatch(
                        destinitionPath,
                        $"{logsDir}.{this.mockFixture.Parameters["ToolName"].ToString().ToLower()}.{this.mockFixture.Parameters["Scenario"].ToString().ToLower()}");

                    sourcePathCorrect = Regex.IsMatch(
                        sourcePath,
                        $"{Regex.Escape(executor.ExecutableDirectory)}");
                });

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(destinitionPathCorrect, true);
                Assert.AreEqual(sourcePathCorrect, true);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, true)]
        [TestCase(PlatformID.Unix, false)]
        public void ScriptExecutorMovesTheLogFilesToCorrectDirectory_Unix(PlatformID platform, bool packageAvailable)
        {
            this.SetupTest(platform);
            string platformSpecificPath = packageAvailable ? Path.Combine("linux-x64") : string.Empty;
            this.mockFixture.Parameters["PackageName"] = packageAvailable ? "workloadPackage" : string.Empty;
            string workingDir = packageAvailable ? this.mockPackage.Path : this.mockFixture.PlatformSpecifics.CurrentDirectory;

            bool destinitionPathCorrect = false;
            bool sourcePathCorrect = false;
            string logsDir = this.mockFixture.PlatformSpecifics.LogsDirectory.Replace(@"\", @"\\");

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                this.mockFixture.File.Setup(fe => fe.Move(It.IsAny<string>(), It.IsAny<string>(), true))
                .Callback<string, string, bool>((sourcePath, destinitionPath, overwrite) =>
                {
                    destinitionPathCorrect = Regex.IsMatch(
                        destinitionPath,
                        $"{logsDir}.{this.mockFixture.Parameters["ToolName"].ToString().ToLower()}.{this.mockFixture.Parameters["Scenario"].ToString().ToLower()}");

                    sourcePathCorrect = Regex.IsMatch(
                        sourcePath,
                        $"{Regex.Escape(executor.ExecutableDirectory)}");
                });

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(destinitionPathCorrect, true);
                Assert.AreEqual(sourcePathCorrect, true);
            }
        }

        private class TestScriptExecutor : ScriptExecutor
        {
            public TestScriptExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
