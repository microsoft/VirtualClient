// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
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

            this.mockPackage = new DependencyPath("workloadPackage", this.mockFixture.GetPackagePath("workloadPackage"));
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
                .Returns(this.mockPackage.Path);

            this.mockFixture.FileSystem.Setup(fe => fe.Path.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns("genericScript");

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
        public void ScriptExecutorThrowsOnInitializationWhenPackageNameIsNotProvidedAndScriptPathIsNotRooted(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.Parameters["ScriptPath"] = "script.ps1";
            this.mockFixture.Parameters["PackageName"] = string.Empty;

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
                Assert.IsTrue(exception.Message.StartsWith($"Either {nameof(executor.PackageName)} should be provided or the {nameof(executor.ScriptPath)} should be a full path with a rooted value."));
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
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.bat")]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.sh")]
        public async Task ScriptExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string platformSpecificPath, string command)
        {
            this.SetupTest(platform);
            this.mockFixture.Parameters["ScriptPath"] = command;

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"{this.mockPackage.Path}{platformSpecificPath}{command} parameter1 parameter2";
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
        [TestCase(PlatformID.Win32NT, @"\win-x64\")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void ScriptExecutorMovesTheLogFilesToCorrectDirectory_Win(PlatformID platform, string platformSpecificPath)
        {
            this.SetupTest(platform);

            bool destinitionPathCorrect = false;
            string logsDir = this.mockFixture.PlatformSpecifics.LogsDirectory.Replace(@"\", @"\\");

            this.mockFixture.File.Setup(fe => fe.Move(It.IsAny<string>(), It.IsAny<string>(), true))
                .Callback<string, string, bool>((sourcePath, destinitionPath, overwrite) =>
                {
                    if (Regex.IsMatch(
                        destinitionPath, 
                        $"{logsDir}.{this.mockFixture.Parameters["ToolName"].ToString().ToLower()}.{this.mockFixture.Parameters["Scenario"].ToString().ToLower()}"))
                    {
                        destinitionPathCorrect = true;
                    }
                    else
                    {
                        destinitionPathCorrect = false;
                    }
                });

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(destinitionPathCorrect, true);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, @"/linux-x64/")]
        public void ScriptExecutorMovesTheLogFilesToCorrectDirectory_Unix(PlatformID platform, string platformSpecificPath)
        {
            this.SetupTest(platform);

            bool destinitionPathCorrect = false;
            string logsDir = this.mockFixture.PlatformSpecifics.LogsDirectory.Replace(@"\", @"\\");

            this.mockFixture.File.Setup(fe => fe.Move(It.IsAny<string>(), It.IsAny<string>(), true))
                .Callback<string, string, bool>((sourcePath, destinitionPath, overwrite) =>
                {
                    if (Regex.IsMatch(
                        destinitionPath, 
                        $"{logsDir}.{this.mockFixture.Parameters["ToolName"].ToString().ToLower()}.{this.mockFixture.Parameters["Scenario"].ToString().ToLower()}"))
                    {
                        destinitionPathCorrect = true;
                    }
                    else
                    {
                        destinitionPathCorrect = false;
                    }
                });

            using (TestScriptExecutor executor = new TestScriptExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(destinitionPathCorrect, true);
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
