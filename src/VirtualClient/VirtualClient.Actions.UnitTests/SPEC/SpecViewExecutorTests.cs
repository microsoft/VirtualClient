// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SpecViewExecutorTests
    {
        private MockFixture fixture;
        private string mockPstoolsExeDir, mockSpecViewExeDir, mockVisualStudioCRuntimeDllDir;
        private string mockSpecViewExePath, mockPstoolsExePath;
        private string results;
        private string[] viewsets
        {
            get
            {
                return this.fixture.Parameters["Viewsets"].ToString().Split(',', StringSplitOptions.TrimEntries);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            TestSpecViewExecutor executor = new TestSpecViewExecutor(this.fixture);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(this.mockSpecViewExePath, executor.SpecviewExecutablePath);
            Assert.AreEqual(this.mockPstoolsExePath, executor.PsExecExecutablePath);

        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedWithPsExec(PlatformID platform, Architecture architecture)
        {
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, psExecSession: 2);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task SpecViewExecutorExecutesWorkloadAsExpectedWithoutPsExec(PlatformID platform, Architecture architecture)
        {
            await this.SpecViewExecutorExecutesWorkloadAsExpected(platform, architecture, psExecSession: -1);
        }


        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public void SpecViewExecutorThrowsWhenTheResultsDirIsNotFoundAfterExecuting(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior();
            this.fixture.Directory.Setup(dir => dir.GetDirectories(this.mockSpecViewExeDir, "results_*", SearchOption.TopDirectoryOnly)).Returns(new string[] { });

            TestSpecViewExecutor executor = new TestSpecViewExecutor(this.fixture);

            WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(async () => await executor.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task SpecViewExecutorRenamedResultsDirAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            int resultRenamed = 0, logRenamed = 0;

            // Test that the result directories are renamed
            string mockResultDir = this.fixture.Combine(this.mockSpecViewExeDir, "results_19991231T235959");
            string mockHistoryResultsDir = this.fixture.Combine(this.mockSpecViewExeDir, "hist_" + Path.GetFileName(mockResultDir));

            // Set up the mock directory to return the mock result dir when the test executor tries to find a dir that starts wth "results_"
            fixture.Directory.Setup(dir => dir.GetDirectories(this.mockSpecViewExeDir, "results_*", SearchOption.TopDirectoryOnly)).Returns(new[] { mockResultDir });

            // Set up the mock directory to increment resultRenamed by 1 when the test executor tries to rename the result directory
            fixture.Directory.Setup(dir => dir.Move(mockResultDir, mockHistoryResultsDir)).Callback(() => resultRenamed++);

            // Test that the log files are renamed
            string mockLogFilePath = this.fixture.Combine(mockHistoryResultsDir, "SomeViewset", "log.txt");

            // Set up the mock directory to return the mock log file when the test executor tries to find a file called "log.txt"
            fixture.Directory.Setup(dir => dir.GetFiles(mockHistoryResultsDir, "log.txt", SearchOption.AllDirectories)).Returns(new[] { mockLogFilePath });

            // Set up the mock directory to increment logRenamed by 1 when the mock log file is renamed to {viewset}-log.txt
            string renamedMockLogFilePath;
            foreach (string viewset in this.viewsets)
            {
                renamedMockLogFilePath = this.fixture.Combine(mockHistoryResultsDir, "SomeViewset", $"{viewset}-log.txt");
                fixture.Directory.Setup(dir => dir.Move(mockLogFilePath, renamedMockLogFilePath)).Callback(() => logRenamed++);
            }

            TestSpecViewExecutor executor = new TestSpecViewExecutor(this.fixture);
            await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(this.viewsets.Length, resultRenamed);
            Assert.AreEqual(this.viewsets.Length, logRenamed);
        }

        private async Task SpecViewExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture, int psExecSession = -1)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.Parameters["PsExecSession"] = psExecSession;

            TestSpecViewExecutor executor = new TestSpecViewExecutor(this.fixture);
            await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

            IList<string> commands = new List<string>();

            string expectedCommandArgumentsFormat, expectedCommandArguments, expectedExePath, expectedWorkingDir;
            string specViewPerfCmd = "-viewset {0} -nogui";

            if (psExecSession == -1)
            {
                // Execute SPECviewperf directly
                expectedCommandArgumentsFormat = specViewPerfCmd;
                expectedExePath = this.mockSpecViewExePath;
                expectedWorkingDir = this.mockSpecViewExeDir;
            }
            else
            {
                // Use PsExec to execute SPECviewperf
                string baseArg = $"-s -i {psExecSession} -w {this.mockSpecViewExeDir} -accepteula -nobanner";
                expectedCommandArgumentsFormat = $"{baseArg} {this.mockSpecViewExePath} {specViewPerfCmd}";
                expectedExePath = this.mockPstoolsExePath;
                expectedWorkingDir = this.mockPstoolsExeDir;
            }

            foreach (string viewset in this.viewsets)
            {
                expectedCommandArguments = string.Format(expectedCommandArgumentsFormat, viewset);
                commands.Add($"{expectedExePath} {expectedCommandArguments}");
            }

            Assert.IsTrue(this.fixture.ProcessManager.CommandsExecutedInWorkingDirectory(expectedWorkingDir, commands.ToArray<string>()));
        }



        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform, architecture);
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Scenario", "SPECviewperf" },
                { "PackageName", "specviewperf2020" },
                { "Viewsets", "3dsmax,catia,creo,energy,maya,medical,snx,sw" },
                { "PsExecPackageName", "pstools" },
                { "PsExecSession", -1 }
            };

            DependencyPath mockSpecViewPackage = new DependencyPath("specviewperf2020", this.fixture.GetPackagePath("specviewperf"));
            DependencyPath mockPstoolsPackage = new DependencyPath("pstools", this.fixture.GetPackagePath("pstools2.51"));
            DependencyPath mockVisualStudioCRuntime = new DependencyPath("visualstudiocruntime", this.fixture.GetPackagePath("visualstudiocruntime"));

            this.fixture.PackageManager.OnGetPackage("specviewperf2020").ReturnsAsync(mockSpecViewPackage);
            this.fixture.PackageManager.OnGetPackage("visualstudiocruntime").ReturnsAsync(mockVisualStudioCRuntime);
            this.fixture.PackageManager.OnGetPackage("pstools").ReturnsAsync(mockPstoolsPackage);

            this.mockSpecViewExeDir = this.fixture.ToPlatformSpecificPath(mockSpecViewPackage, this.fixture.Platform, this.fixture.CpuArchitecture).Path;
            this.mockPstoolsExeDir = this.fixture.ToPlatformSpecificPath(mockPstoolsPackage, this.fixture.Platform, this.fixture.CpuArchitecture).Path;
            this.mockVisualStudioCRuntimeDllDir = this.fixture.ToPlatformSpecificPath(mockVisualStudioCRuntime, this.fixture.Platform, this.fixture.CpuArchitecture).Path;

            this.mockSpecViewExePath = this.fixture.Combine(this.mockSpecViewExeDir, "RunViewperf.exe");
            this.mockPstoolsExePath  = this.fixture.Combine(this.mockPstoolsExeDir, "PsExec.exe");

            string mockResultPath = MockFixture.GetDirectory(typeof(SpecViewExecutorTests), "Examples", "SPECview", "3dsmaxResultCSV.csv");
            this.results = File.ReadAllText(mockResultPath);
            this.fixture.FileSystem.Setup(rt => rt.Directory.GetDirectories(this.mockSpecViewExeDir, "results_*", SearchOption.TopDirectoryOnly)).Returns(new string[] { mockResultPath });
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>())).Returns(this.results);
        }

        private class TestSpecViewExecutor : SpecViewExecutor
        {
            public TestSpecViewExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }
            public new string SpecviewExecutablePath => base.SpecviewExecutablePath;

            public new string PsExecExecutablePath => base.PsExecExecutablePath;

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
