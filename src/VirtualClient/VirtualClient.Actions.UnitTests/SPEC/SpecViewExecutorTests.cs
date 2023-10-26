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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SpecViewExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockSpecViewPackage;
        private DependencyPath mockVisualStudioCRuntime;
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

            using (TestSpecViewExecutor executor = new TestSpecViewExecutor(this.mockFixture))
            {
                int executed = 0;
                int renamed = 0;
                if (platform == PlatformID.Win32NT)
                {
                    string expectedSpecViewExecutablePath = this.mockFixture.Combine(this.mockSpecViewPackage.Path, "RunViewperf.exe");
                    string workingDir = this.mockSpecViewPackage.Path;
                    string expectedGUIOption = this.mockFixture.Parameters["GUIOption"].ToString();
                    string expectedCommandArguments = $"-viewset \"{viewsetArg}\" {expectedGUIOption}";
                    string mockResultDir = this.mockFixture.Combine(this.mockSpecViewPackage.Path, "results_19991231T235959");
                    string mockResultFilePath = this.mockFixture.Combine(mockResultDir, "resultCSV.csv");
                    string mockHistoryResultsDir = this.mockFixture.Combine(this.mockSpecViewPackage.Path, "hist_" + Path.GetFileName(mockResultDir));

                    // Test that the result is properly renamed
                    mockFixture.Directory.Setup(dir => dir.GetDirectories(this.mockSpecViewPackage.Path, "results_*", SearchOption.TopDirectoryOnly)).Returns(new[] {mockResultDir});
                    mockFixture.Directory.Setup(dir => dir.Move(mockResultDir, mockHistoryResultsDir)).Callback(() => renamed++);
                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == expectedCommandArguments && command == expectedSpecViewExecutablePath)
                        {
                            executed++;
                        }

                        return this.mockFixture.Process;
                    };

                    // Remove any mock blob managers so that we do not evaluate the code paths that
                    // upload log files by default.
                    this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
                    this.mockFixture.ProcessManager.OnGetProcess = (id) => null;

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                }

                Assert.AreEqual(1, executed);
                Assert.AreEqual(1, renamed);
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
                { "Viewset", "3dsmax" }
            };

            this.mockSpecViewPackage = new DependencyPath("specviewperf", this.mockFixture.GetPackagePath("specviewperf"));
            this.mockVisualStudioCRuntime = new DependencyPath("visualstudiocruntime", this.mockFixture.GetPackagePath("visualstudiocruntime"));

            this.mockFixture.PackageManager.OnGetPackage("specviewperf2020").ReturnsAsync(this.mockSpecViewPackage);
            this.mockFixture.PackageManager.OnGetPackage("visualstudiocruntime").ReturnsAsync(this.mockVisualStudioCRuntime);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "SPECview", "resultCSV_multipleViewSets.csv"));
            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>())).Returns(this.results);
        }

        private class TestSpecViewExecutor : SpecViewExecutor
        {
            public TestSpecViewExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }
            public new string ExecutablePath => base.ExecutablePath;

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
