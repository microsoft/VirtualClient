// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PowershellExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(PowershellExecutorTests), "Examples", "ScriptExecutor");

        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string exampleResults;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform);
            this.mockPackage = new DependencyPath("workloadPackage", this.fixture.GetPackagePath("workloadPackage"));
            this.fixture.SetupPackage(this.mockPackage);

            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

            this.exampleResults = File.ReadAllText(Path.Combine(PowershellExecutorTests.ExamplesDirectory, "validJsonExample.json"));

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.Setup(fe => fe.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.fixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.fixture.FileSystem.Setup(fe => fe.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns(this.mockPackage.Path);

            this.fixture.FileSystem.Setup(fe => fe.Path.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns("genericScript");

            this.fixture.FileSystem.Setup(fe => fe.Path.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string path1, string path2) =>
                {
                    string combinedPath = Path.Combine(path1, path2);
                    return this.fixture.Platform == PlatformID.Unix
                        ? combinedPath.Replace('\\', '/') // Convert to Unix-style path
                        : combinedPath;
                });

            this.fixture.FileSystem.Setup(fe => fe.Path.GetFullPath(It.IsAny<string>()))
                .Returns((string path1) =>
                {
                    string fullPath = Path.GetFullPath(path1);

                    // If the platform is Unix, convert to Unix-style path and remove the drive letter dynamically
                    if (this.fixture.Platform == PlatformID.Unix)
                    {
                        fullPath = fullPath.Replace('\\', '/'); // Convert to Unix-style path
                        int colonIndex = fullPath.IndexOf(':');
                        if (colonIndex != -1)
                        {
                            fullPath = fullPath.Substring(colonIndex + 1); // Remove the drive letter and colon
                        }
                    }

                    return fullPath;
                });

            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(PowershellExecutor.PackageName), "workloadPackage" },
                { nameof(PowershellExecutor.Scenario), "GenericScriptWorkload" },
                { nameof(PowershellExecutor.CommandLine), "parameter1 parameter2" },
                { nameof(PowershellExecutor.ScriptPath), "genericScript.ps1" },
                { nameof(PowershellExecutor.LogPaths), "*.log;*.txt;*.json" },
                { nameof(PowershellExecutor.ToolName), "GenericTool" }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64", @"genericScript.ps1")]
        public async Task PowershellExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string platformSpecificPath, string command)
        {
            this.SetupTest(platform);
            this.fixture.Parameters["ScriptPath"] = command;

            string workingDirectory = $"{this.mockPackage.Path}{platformSpecificPath}";
            string fullCommand = $"{this.mockPackage.Path}{platformSpecificPath}\\{command} parameter1 parameter2";

            this.fixture.FileSystem.Setup(fe => fe.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns(workingDirectory);

            using (TestPowershellExecutor executor = new TestPowershellExecutor(this.fixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"powershell -ExecutionPolicy Bypass -NoProfile -NonInteractive -WindowStyle Hidden -Command \"cd '{workingDirectory}';{fullCommand}\"";
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
        public void PowershellExecutorDoesNotThrowWhenTheWorkloadDoesNotProduceValidMetricsFile(PlatformID platform, string platformSpecificPath)
        {
            this.SetupTest(platform);
            this.fixture.File.Setup(fe => fe.Exists($"{this.mockPackage.Path}{platformSpecificPath}test-metrics.json"))
                .Returns(false);

            using (TestPowershellExecutor executor = new TestPowershellExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

               Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
            }            
        }

        private class TestPowershellExecutor : PowershellExecutor
        {
            public TestPowershellExecutor(MockFixture fixture)
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
