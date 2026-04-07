// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PythonExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(PythonExecutorTests), "Examples", "ScriptExecutor");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string exampleResults;

        public void SetupTest(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.exampleResults = File.ReadAllText(this.mockFixture.Combine(PythonExecutorTests.ExamplesDirectory, "validJsonExample.json"));

            this.mockFixture.Setup(platform);
            this.mockPackage = new DependencyPath("workloadPackage", this.mockFixture.PlatformSpecifics.GetPackagePath("workloadPackage"));
            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

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
                { nameof(PythonExecutor.PackageName), "workloadPackage" },
                { nameof(PythonExecutor.Scenario), "GenericScriptWorkload" },
                { nameof(PythonExecutor.CommandLine), "parameter1 parameter2" },
                { nameof(PythonExecutor.ScriptPath), "genericScript.py" },
                { nameof(PythonExecutor.LogPaths), "*.log;*.txt;*.json" },
                { nameof(PythonExecutor.ToolName), "GenericTool" },
                { nameof(PythonExecutor.UsePython3), true }
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", true, "python3", true)]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", true, "python3", false)]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", true, "python3", true)]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", true, "python3", false)]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", false, "python", true)]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", false, "python", false)]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", false, "python", true)]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", false, "python", false)]
        public async Task PythonExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string platformSpecificPath, string command, bool usePython3, string pythonVersion, bool runElevated)
        {
            this.SetupTest(platform);
            this.mockFixture.Parameters[nameof(PythonExecutor.RunElevated)] = runElevated;
            this.mockFixture.Parameters[nameof(PythonExecutor.ScriptPath)] = command;
            this.mockFixture.Parameters[nameof(PythonExecutor.UsePython3)] = usePython3;

            string fullCommand = $"{this.mockPackage.Path}{platformSpecificPath}{command} parameter1 parameter2";

            using (TestPythonExecutor executor = new TestPythonExecutor(this.mockFixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"{(PlatformID.Unix.Equals(platform) && runElevated ? "sudo" : string.Empty)} {pythonVersion} {fullCommand}".Trim();
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

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\")]
        [TestCase(PlatformID.Unix, @"/linux-x64/")]
        public void PythonExecutorDoesNotThrowWhenTheWorkloadDoesNotProduceValidMetricsFile(PlatformID platform, string platformSpecificPath)
        {
            this.SetupTest(platform);
            this.mockFixture.File.Setup(fe => fe.Exists($"{this.mockPackage.Path}{platformSpecificPath}test-metrics.json"))
                .Returns(false);

            using (TestPythonExecutor executor = new TestPythonExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        private class TestPythonExecutor : PythonExecutor
        {
            public TestPythonExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
