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
                .Returns(this.mockPackage.Path);

            this.mockFixture.FileSystem.Setup(fe => fe.Path.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns("genericScript");

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
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", true, "python3")]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", true, "python3")]
        [TestCase(PlatformID.Win32NT, @"\win-x64\", @"genericScript.py", false, "python")]
        [TestCase(PlatformID.Unix, @"/linux-x64/", @"genericScript.py", false, "python")]
        public async Task PythonExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string platformSpecificPath, string command, bool usePython3, string pythonVersion)
        {
            this.SetupTest(platform);
            this.mockFixture.Parameters["ScriptPath"] = command;
            this.mockFixture.Parameters["UsePython3"] = usePython3;

            string fullCommand = $"{this.mockPackage.Path}{platformSpecificPath}{command} parameter1 parameter2";

            using (TestPythonExecutor executor = new TestPythonExecutor(this.mockFixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $"{pythonVersion} {fullCommand}";
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
