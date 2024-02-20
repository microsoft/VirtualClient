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
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PythonExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string rawText;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();
            this.rawText = File.ReadAllText(Path.Combine("Examples", "ScriptExecutor", "validJsonExample.json"));
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void PythonExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestPythonExecutor executor = new TestPythonExecutor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", true, "python3")]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", true, "python3")]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", false, "python")]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", false, "python")]
        public async Task PythonExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string platformSpecificPath, string command, bool usePython3, string pythonVersion)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.Parameters["ScriptPath"] = command;
            this.fixture.Parameters["UsePython3"] = usePython3;

            string fullCommand = $"{this.mockPackage.Path}{platformSpecificPath}{command} parameter1 parameter2";

            using (TestPythonExecutor executor = new TestPythonExecutor(this.fixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"{pythonVersion} {fullCommand}";
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
        public void PythonExecutorDoesNotThrowWhenTheWorkloadDoesNotProduceValidMetricsFile(PlatformID platform, string platformSpecificPath)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.File.Setup(fe => fe.Exists($"{this.mockPackage.Path}{platformSpecificPath}test-metrics.json"))
                .Returns(false);

            using (TestPythonExecutor executor = new TestPythonExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
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
                { nameof(PythonExecutor.PackageName), "workloadPackage" },
                { nameof(PythonExecutor.Scenario), "GenericScriptWorkload" },
                { nameof(PythonExecutor.CommandLine), "parameter1 parameter2" },
                { nameof(PythonExecutor.ScriptPath), "genericScript.py" },
                { nameof(PythonExecutor.LogPaths), "*.log;*.txt;*.json" },
                { nameof(PythonExecutor.ToolName), "GenericTool" },
                { nameof(PythonExecutor.UsePython3), true }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
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
