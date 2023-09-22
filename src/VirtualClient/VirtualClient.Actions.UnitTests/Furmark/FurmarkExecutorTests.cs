// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FurmarkExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockFurmarkPackage;
        private DependencyPath mockPsExecPackage;
        private string results;

        [SetUp]
        public void SetUpTests()
        {
            this.mockFixture = new MockFixture();
        }
        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task FurmarkExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedScriptFilePath = this.mockFixture.PlatformSpecifics.Combine(
                    this.mockFurmarkPackage.Path,"win-x64", "Geeks3D", "Benchmarks", "FurMark", "FurMark.exe");

                Assert.AreEqual(expectedScriptFilePath, executor.ExecutablePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task FurmarkExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.mockFixture))
            {
                int executed = 0;
                if (platform == PlatformID.Win32NT)
                {
                    string psExecPath = this.mockFixture.Combine(this.mockPsExecPackage.Path, "win-x64", "PsExec.exe");
                    string expectedFurmarkExecutablePath = this.mockFixture.Combine(this.mockFurmarkPackage.Path ,"win-x64", "Geeks3D" , "Benchmarks" ,"FurMark" ,"FurMark.exe");
                    string workingDir = this.mockFixture.Combine(this.mockFurmarkPackage.Path, "win-x64");

                    string expectedFurmarkArguments = this.mockFixture.Parameters["CommandLine"].ToString();
                    string expectedCommandArguments = $"-accepteula -s -i 1 -w {workingDir} {expectedFurmarkExecutablePath} {expectedFurmarkArguments}";

                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == expectedCommandArguments && command == psExecPath)
                        {
                            executed++;
                        }
                        
                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                }

                Assert.AreEqual(1, executed);
            }
        }

        [Test]
        public void FurmarkExecutorThrowsWhenTheResultsFileIsNotFoundAfterExecutingFurMark()
        {
            this.SetupDefaultMockBehavior();

            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(executor.ResultsFilePath)).Returns(false);
                    this.mockFixture.Process.StandardError.Append("123");
                    return this.mockFixture.Process;
                };

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                     () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
        }

        [Test]
        public void FurmarkExecutorThrowsWhenTheXmlResultsFileIsNotFoundAfterExecutingFurMark()
        {
            this.SetupDefaultMockBehavior();

            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(executor.ResultsXMLFilePath)).Returns(false);
                    this.mockFixture.Process.StandardError.Append("123");
                    return this.mockFixture.Process;
                };

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                     () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Scenario", "Resolution_VGA" },
                { "MetricScenario", "gpu_benchmarking_640x480_2x_antialiasing" },
                { "PackageName", "furmark" },
                { "PsExecPackageName", "pstools" },
                { "CommandLine", "/width=640 /height=480 /antialiasing=2 /run_mode=1 /max_time=60000 /nogui /nomenubar /noscore /log_score /disable_catalyst_warning /log_temperature" },
                { "Duration", "00:01:00" }
            };

            this.mockFurmarkPackage = new DependencyPath("furmark", this.mockFixture.GetPackagePath("furmark"));
            this.mockPsExecPackage = new DependencyPath("pstools", this.mockFixture.GetPackagePath("pstools"));

            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);

            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "Furmark", "FurmarkExampleResults.txt"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.results);

            this.mockFixture.PackageManager.OnGetPackage("furmark").ReturnsAsync(this.mockFurmarkPackage);
            this.mockFixture.PackageManager.OnGetPackage("pstools").ReturnsAsync(this.mockPsExecPackage);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        private class TestFurmarkxecutor : FurmarkExecutor
        {
            public TestFurmarkxecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestFurmarkxecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new string ExecutablePath => base.ExecutablePath;

            public new string ResultsFilePath => base.ResultsFilePath;

            public new string ResultsXMLFilePath => base.ResultsXMLFilePath;


            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
