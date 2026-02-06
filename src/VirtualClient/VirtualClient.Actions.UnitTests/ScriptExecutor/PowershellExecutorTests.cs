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
    public class PowerShellExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(PowerShellExecutorTests), "Examples", "ScriptExecutor");

        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string exampleResults;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform);
            this.mockPackage = new DependencyPath("workloadPackage", this.fixture.PlatformSpecifics.GetPackagePath("workloadPackage"));
            this.fixture.SetupPackage(this.mockPackage);

            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

            this.exampleResults = File.ReadAllText(Path.Combine(PowerShellExecutorTests.ExamplesDirectory, "validJsonExample.json"));

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.Setup(fe => fe.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.fixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.fixture.FileSystem.Setup(fe => fe.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns((string filePath) =>
                {
                    if (this.fixture.Platform == PlatformID.Unix)
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

            this.fixture.FileSystem.Setup(fe => fe.Path.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns("genericScript");

            this.fixture.FileSystem.Setup(fe => fe.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string path1, string path2) =>
                {
                    if (this.fixture.Platform == PlatformID.Unix)
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

            this.fixture.FileSystem.Setup(fe => fe.Path.GetFullPath(It.IsAny<string>()))
                .Returns((string path1) =>
                {
                    if (this.fixture.Platform == PlatformID.Unix)
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

            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(PowerShellExecutor.PackageName), "workloadPackage" },
                { nameof(PowerShellExecutor.Scenario), "GenericScriptWorkload" },
                { nameof(PowerShellExecutor.CommandLine), "parameter1 parameter2" },
                { nameof(PowerShellExecutor.ScriptPath), "genericScript.ps1" },
                { nameof(PowerShellExecutor.LogPaths), "*.log;*.txt;*.json" },
                { nameof(PowerShellExecutor.ToolName), "GenericTool" }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
        }

        [Test]
        [TestCase(@"genericScript.ps1", "powershell")]
        [TestCase(@"genericScript.ps1", "powershell")]
        [TestCase(@"genericScript.ps1", "powershell")]
        [TestCase(@"genericScript.ps1", "powershell.exe")]
        [TestCase(@"genericScript.ps1", "PowerShell.exe")]
        [TestCase(@"genericScript.ps1", @"C:\Any\Custom\Location\powershell.exe")]
        [TestCase(@"genericScript.ps1", "pwsh")]
        [TestCase(@"genericScript.ps1", "pwsh.exe")]
        [TestCase(@"genericScript.ps1", @"C:\Any\Custom\Location\pwsh.exe")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PowershellExecutorExecutesTheCorrectWorkloadCommands_Windows_Scenarios(string command, string executable)
        {
            this.SetupTest(PlatformID.Win32NT);
            this.fixture.Parameters[nameof(PowerShellExecutor.ScriptPath)] = command;
            this.fixture.Parameters[nameof(PowerShellExecutor.Executable)] = executable;

            string fullCommand = $"{this.mockPackage.Path}\\win-x64\\{command} parameter1 parameter2";

            using (TestPowerShellExecutor executor = new TestPowerShellExecutor(this.fixture))
            {
                bool commandExecuted = false;
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                string workingDirectory = executor.ExecutableDirectory;

                string expectedCommand = $"{executable} -ExecutionPolicy Bypass -NoProfile -NonInteractive -WindowStyle Hidden -Command \"cd '{workingDirectory}';{fullCommand}\"";
                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if(expectedCommand == $"{exe} {arguments}")
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

                await executor.ExecuteAsync(CancellationToken.None);

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase("genericScript.ps1", "pwsh")]
        [TestCase("genericScript.ps1", @"/home/any/custom/location/pwsh")]
        [TestCase("genericScript.ps1", "sudo pwsh")]
        public async Task PowershellExecutorExecutesTheCorrectWorkloadCommands_Unix_Scenarios(string command, string executable)
        {
            this.SetupTest(PlatformID.Unix);
            this.fixture.Parameters[nameof(PowerShellExecutor.ScriptPath)] = command;
            this.fixture.Parameters[nameof(PowerShellExecutor.Executable)] = executable;

            string fullCommand = $"{this.mockPackage.Path}/linux-x64/{command} parameter1 parameter2";

            using (TestPowerShellExecutor executor = new TestPowerShellExecutor(this.fixture))
            {
                bool commandExecuted = false;
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                string workingDirectory = executor.ExecutableDirectory;

                string expectedCommand = $"{executable} -ExecutionPolicy Bypass -NoProfile -NonInteractive -WindowStyle Hidden -Command \"cd '{workingDirectory}';{fullCommand}\"";
                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
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

                await executor.ExecuteAsync(CancellationToken.None);

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        public void PowershellExecutorDoesNotThrowWhenTheWorkloadDoesNotProduceValidMetricsFile()
        {
            this.SetupTest(PlatformID.Win32NT);
            this.fixture.File.Setup(fe => fe.Exists($@"{this.mockPackage.Path}\win-x64\test-metrics.json"))
                .Returns(false);

            using (TestPowerShellExecutor executor = new TestPowerShellExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

               Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
            }            
        }

        private class TestPowerShellExecutor : PowerShellExecutor
        {
            public TestPowerShellExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new string ExecutablePath
            {
                get
                {
                    return base.ExecutablePath;
                }
            }

            public new string ExecutableDirectory
            {
                get
                {
                    return base.ExecutableDirectory;
                }
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
