// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using Serilog;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SpecViewExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPstoolsPackage, mockSpecViewPackage, mockVisualStudioCRuntime;
        private string results;

        [SetUp]
        public void SetUpTests()
        {
            this.mockFixture = new MockFixture();
        }
        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestSpecViewExecutor executor = new TestSpecViewExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };
                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedScriptFilePath = this.mockFixture.PlatformSpecifics.Combine(
                    this.mockSpecViewPackage.Path, "RunViewperf.exe");

                Assert.AreEqual(expectedScriptFilePath, executor.ExecutablePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedOneViewset(PlatformID platform, Architecture architecture)
        {
            string viewsetArg = "3dsmax";
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, viewsetArg);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedMultipleViewset(PlatformID platform, Architecture architecture)
        {
            string viewsetArg = "3dsmax,catia,creo,energy,maya,medical,snx,sw";
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, viewsetArg);
        }


        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public void SpecViewExecutorThrowsWhenTheResultsDirIsNotFoundAfterExecuting(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior();

            using (TestSpecViewExecutor executor = new TestSpecViewExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    mockFixture.Directory.Setup(dir => dir.GetDirectories(this.mockSpecViewPackage.Path, "results_*", SearchOption.TopDirectoryOnly)).Returns(new string[] {});
                    return this.mockFixture.Process;
                };

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                     () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
        }

        private async Task SpecViewExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture, string viewsetArg)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.mockFixture.Parameters["Viewset"] = viewsetArg;
            var viewsets = viewsetArg.Split(",", StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);

            using (TestSpecViewExecutor executor = new TestSpecViewExecutor(this.mockFixture))
            {
                int executed = 0, renamed = 0, uploaded = 0;
                if (platform == PlatformID.Win32NT)
                {
                    string baseArg = @$"-s -i {this.mockFixture.Parameters["PsExecSession"]} -w {this.mockSpecViewPackage.Path} -accepteula -nobanner";
                    string specViewExecutablePath = this.mockFixture.Combine(this.mockSpecViewPackage.Path, "RunViewperf.exe");
                    string workingDir = this.mockSpecViewPackage.Path;
                    string specViewPerfCmd = @$"{specViewExecutablePath} -viewset {viewsetArg} {this.mockFixture.Parameters["GUIOption"]}";
                    string expectedCommandArguments =  $"{baseArg} {specViewPerfCmd}";
                    string expectedPsExecPath = this.mockFixture.Combine(this.mockFixture.ToPlatformSpecificPath(this.mockPstoolsPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path, "PsExec.exe");

                    string mockResultDir = this.mockFixture.Combine(this.mockSpecViewPackage.Path, "results_19991231T235959");
                    string mockResultFilePath = this.mockFixture.Combine(mockResultDir, "resultCSV.csv");
                    string mockHistoryResultsDir = this.mockFixture.Combine(this.mockSpecViewPackage.Path, "hist_" + Path.GetFileName(mockResultDir));

                    // Test that the result is properly renamed
                    mockFixture.Directory.Setup(dir => dir.GetDirectories(this.mockSpecViewPackage.Path, "results_*", SearchOption.TopDirectoryOnly)).Returns(new[] {mockResultDir});
                    mockFixture.Directory.Setup(dir => dir.Move(mockResultDir, mockHistoryResultsDir)).Callback(() => renamed++);

                    // Test that the log file is renamed and uploaded 
                    string mockLogFilePath, renamedMockLogFilePath;
                    foreach (var viewset in viewsets)
                    {
                        mockLogFilePath = this.mockFixture.Combine(mockHistoryResultsDir, $"{executor.ViewsetLogFileNameMapping[viewset]}", "log.txt");
                        renamedMockLogFilePath = this.mockFixture.Combine(mockHistoryResultsDir, $"{executor.ViewsetLogFileNameMapping[viewset]}", $"{viewset}-log.txt");
                        mockFixture.Directory.Setup(dir => dir.GetDirectories(mockHistoryResultsDir, "{executor.ViewsetLogFileNameMapping[viewSet]}", SearchOption.TopDirectoryOnly)).Returns(new[] { mockLogFilePath });
                        mockFixture.Directory.Setup(dir => dir.Move(mockLogFilePath, renamedMockLogFilePath)).Callback(() => uploaded++);
                    }

                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == expectedCommandArguments && command == expectedPsExecPath)
                        {
                            executed++;
                        }

                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                }

                Assert.AreEqual(1, executed);
                Assert.AreEqual(1, renamed);
                Assert.AreEqual(viewsets.Length, uploaded);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Scenario", "SPECviewperf" },
                { "PackageName", "specviewperf2020" },
                { "GUIOption", "-nogui" },
                { "Viewset", "3dsmax, catia" },
                { "PsExecPackageName", "pstools" },
                { "PsExecSession", 2 }
            };

            this.mockSpecViewPackage = new DependencyPath("specviewperf2020", this.mockFixture.GetPackagePath("specviewperf"));
            this.mockPstoolsPackage = new DependencyPath("pstools", this.mockFixture.GetPackagePath("pstools2.51"));

            this.mockVisualStudioCRuntime = new DependencyPath("visualstudiocruntime", this.mockFixture.GetPackagePath("visualstudiocruntime"));

            this.mockFixture.PackageManager.OnGetPackage("specviewperf2020").ReturnsAsync(this.mockSpecViewPackage);
            this.mockFixture.PackageManager.OnGetPackage("visualstudiocruntime").ReturnsAsync(this.mockVisualStudioCRuntime);
            this.mockFixture.PackageManager.OnGetPackage("pstools").ReturnsAsync(this.mockPstoolsPackage);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "SPECview", "resultCSV.csv"));
            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>())).Returns(this.results);
        }

        private class TestSpecViewExecutor : SpecViewExecutor
        {
            public TestSpecViewExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }
            public new string ExecutablePath => base.ExecutablePath;

            public new IDictionary<string, string> ViewsetLogFileNameMapping => base.ViewsetLogFileNameMapping;

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
