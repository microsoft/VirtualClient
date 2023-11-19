// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using YamlDotNet.Core;

    [TestFixture]
    [Category("Unit")]
    public class SpecViewExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPlatformSpecificPstoolsPackage, mockPlatformSpecificSpecViewPackage, mockPlatformSpecificVisualStudioCRuntime;
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

                string expectedSpecviewExecutablePath = this.mockFixture.PlatformSpecifics.Combine(this.mockPlatformSpecificSpecViewPackage.Path, "RunViewperf.exe");

                string expectedPsExecExecutablePath = this.mockFixture.PlatformSpecifics.Combine(this.mockPlatformSpecificPstoolsPackage.Path, "PsExec.exe");

                Assert.AreEqual(expectedSpecviewExecutablePath, executor.SpecviewExecutablePath);
                Assert.AreEqual(expectedPsExecExecutablePath, executor.PsExecExecutablePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedOneViewsettPsExec(PlatformID platform, Architecture architecture)
        {
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, 2);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedMultipleViewsetPsExec(PlatformID platform, Architecture architecture)
        {
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, 2);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedMultipleViewsetNoPsExec(PlatformID platform, Architecture architecture)
        {
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, -1);
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
                    mockFixture.Directory.Setup(dir => dir.GetDirectories(this.mockPlatformSpecificSpecViewPackage.Path, "results_*", SearchOption.TopDirectoryOnly)).Returns(new string[] { });
                    return this.mockFixture.Process;
                };

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                     () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
        }

        private async Task SpecViewExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture, int psExecSession = -1)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.mockFixture.Parameters["PsExecSession"] = psExecSession;
            string[] viewsets = this.mockFixture.Parameters["Viewsets"].ToString().Split(',', StringSplitOptions.TrimEntries);

            using (TestSpecViewExecutor executor = new TestSpecViewExecutor(this.mockFixture))
            {
                int executed = 0, renamed = 0, uploaded = 0;
                if (platform == PlatformID.Win32NT)
                {
                    string expectedCommandArgumentsFormat, expectedCommandArguments, expectedExePath;
                    string mockResultDir = this.mockFixture.Combine(this.mockPlatformSpecificSpecViewPackage.Path, "results_19991231T235959");
                    string mockResultFilePath = this.mockFixture.Combine(mockResultDir, "resultCSV.csv");
                    string mockHistoryResultsDir = this.mockFixture.Combine(this.mockPlatformSpecificSpecViewPackage.Path, "hist_" + Path.GetFileName(mockResultDir));
                    string specViewExecutablePath = this.mockFixture.Combine(this.mockPlatformSpecificSpecViewPackage.Path, "RunViewperf.exe");

                    if (psExecSession == -1)
                    {
                        expectedCommandArgumentsFormat = $"-viewset {0} -nogui";
                        expectedExePath = specViewExecutablePath;
                    }
                    else
                    {
                        string baseArg = $"-s -i {this.mockFixture.Parameters["PsExecSession"]} -w {this.mockPlatformSpecificSpecViewPackage.Path} -accepteula -nobanner";
                        string specViewPerfCmd = $"{specViewExecutablePath} -viewset {0} -nogui";
                        expectedCommandArgumentsFormat = $"{baseArg} {specViewPerfCmd}";
                        expectedExePath = this.mockFixture.Combine(this.mockPlatformSpecificPstoolsPackage.Path, "PsExec.exe");
                    }

                    foreach (string viewset in viewsets)
                    {
                        expectedCommandArguments = string.Format(expectedCommandArgumentsFormat, viewset);

                        this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                        {
                            if (arguments == expectedCommandArguments && command == expectedExePath)
                            {
                                executed++;
                            }

                            return this.mockFixture.Process;
                        };

                        // Test that the result is properly renamed
                        mockFixture.Directory.Setup(dir => dir.GetDirectories(this.mockPlatformSpecificSpecViewPackage.Path, "results_*", SearchOption.TopDirectoryOnly)).Returns(new[] { mockResultDir });
                        mockFixture.Directory.Setup(dir => dir.Move(mockResultDir, mockHistoryResultsDir)).Callback(() => renamed++);

                        // Test that the log file is renamed and uploaded 
                        string mockLogFilePath, renamedMockLogFilePath;
                        mockLogFilePath = this.mockFixture.Combine(mockHistoryResultsDir, $"SomeViewset", "log.txt");
                        renamedMockLogFilePath = this.mockFixture.Combine(mockHistoryResultsDir, $"SomeViewset", $"{viewset}-log.txt");
                        mockFixture.Directory.Setup(dir => dir.GetFiles(mockHistoryResultsDir, "log.txt", SearchOption.AllDirectories)).Returns(new[] { mockLogFilePath });
                        mockFixture.Directory.Setup(dir => dir.Move(mockLogFilePath, renamedMockLogFilePath)).Callback(() => uploaded++);
                    }

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
                { "Viewsets", "3dsmax, catia" },
                { "PsExecPackageName", "pstools" },
                { "PsExecSession", -1 }
            };

            DependencyPath mockSpecViewPackage = new DependencyPath("specviewperf2020", this.mockFixture.GetPackagePath("specviewperf"));
            DependencyPath mockPstoolsPackage = new DependencyPath("pstools", this.mockFixture.GetPackagePath("pstools2.51"));
            DependencyPath mockVisualStudioCRuntime = new DependencyPath("visualstudiocruntime", this.mockFixture.GetPackagePath("visualstudiocruntime"));

            this.mockFixture.PackageManager.OnGetPackage("specviewperf2020").ReturnsAsync(mockSpecViewPackage);
            this.mockFixture.PackageManager.OnGetPackage("visualstudiocruntime").ReturnsAsync(mockVisualStudioCRuntime);
            this.mockFixture.PackageManager.OnGetPackage("pstools").ReturnsAsync(mockPstoolsPackage);

            this.mockPlatformSpecificSpecViewPackage = this.mockFixture.ToPlatformSpecificPath(mockSpecViewPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture);
            this.mockPlatformSpecificPstoolsPackage = this.mockFixture.ToPlatformSpecificPath(mockPstoolsPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture);
            this.mockPlatformSpecificVisualStudioCRuntime = this.mockFixture.ToPlatformSpecificPath(mockVisualStudioCRuntime, this.mockFixture.Platform, this.mockFixture.CpuArchitecture);

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
            public new string SpecviewExecutablePath => base.SpecviewExecutablePath;

            public new string PsExecExecutablePath => base.PsExecExecutablePath;

            //internal readonly IDictionary<string, string> viewsetLogFileNameMapping = new Dictionary<string, string>()
            //{
            //    { "3dsmax", "3dsmax-07" },
            //    { "catia", "catia-06" },
            //    { "creo", "creo-03" },
            //    { "energy", "energy-03" },
            //    { "maya", "maya-06" },
            //    { "medical", "medical-03" },
            //    { "snx", "snx-04" },
            //    { "sw", "solidworks-07" }
            //};

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
