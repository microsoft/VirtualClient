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
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ScriptExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string rawText;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();
            this.rawText = File.ReadAllText(@"Examples\ScriptExecutor\validJsonExample.json");
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ScriptExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestScriptExecutor executor = new TestScriptExecutor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.bat")]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.sh")]
        public async Task ScriptExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string platformSpecificPath, string command)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.Parameters["ScriptPath"] = command;

            using (TestScriptExecutor executor = new TestScriptExecutor(this.fixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"{this.mockPackage.Path}{platformSpecificPath}{command} parameter1 parameter2";
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
            this.SetupDefaultBehavior(platform);
            this.fixture.File.Setup(fe => fe.Exists($"{this.mockPackage.Path}{platformSpecificPath}test-metrics.json"))
                .Returns(false);

            using (TestScriptExecutor executor = new TestScriptExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\")]
        [TestCase(PlatformID.Unix, @"/linux-x64/")]
        public void ScriptExecutorMovesTheLogFilesToCorrectDirectory(PlatformID platform, string platformSpecificPath)
        {
            this.SetupDefaultBehavior(platform);

            bool destinitionPathCorrect = false;
            string logsDir = this.fixture.PlatformSpecifics.LogsDirectory.Replace(@"\", @"\\");

            this.fixture.File.Setup(fe => fe.Move(It.IsAny<string>(), It.IsAny<string>(), true))
                .Callback<string, string, bool>((sourcePath, destinitionPath, overwrite) =>
                {
                    string sourceFileName = Path.GetFileName(sourcePath);
                    if (Regex.IsMatch(
                        destinitionPath, 
                        $"{logsDir}.{this.fixture.Parameters["ToolName"].ToString().ToLower()}.{this.fixture.Parameters["Scenario"].ToString().ToLower()}_.*{sourceFileName}"))
                    {
                        destinitionPathCorrect = true;
                    }
                    else
                    {
                        destinitionPathCorrect = false;
                    }
                });

            using (TestScriptExecutor executor = new TestScriptExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(destinitionPathCorrect, true);
            }
        }

        private void SetupDefaultBehavior(PlatformID platform)
        {
            this.fixture.Setup(platform);
            this.mockPackage = new DependencyPath("workloadPackage", this.fixture.PlatformSpecifics.GetPackagePath("workloadPackage"));
            this.fixture.PackageManager.OnGetPackage("workloadPackage").ReturnsAsync(this.mockPackage);
            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

            this.fixture.File.Reset();
            this.fixture.File.Setup(fe => fe.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawText);

            this.fixture.File.Setup(fe => fe.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(ScriptExecutor.PackageName), "workloadPackage" },
                { nameof(ScriptExecutor.Scenario), "GenericScriptWorkload" },
                { nameof(ScriptExecutor.CommandLine), "parameter1 parameter2" },
                { nameof(ScriptExecutor.ScriptPath), "genericScript.bat" },
                { nameof(ScriptExecutor.LogPaths), "*.log;*.txt;*.json" },
                { nameof(ScriptExecutor.ToolName), "GenericTool" }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
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
        }
    }
}
