// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class Example2WorkloadExecutorTests_DependencyFixture
    {
        private DependencyFixture fixture;
        private DependencyPath workloadPackage;
        private string validResults;

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "SomeWorkload.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "SomeTool1.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "SomeTool2.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "SomeWorkload.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "SomeTool1.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "SomeTool2.exe")]
        [TestCase(PlatformID.Unix, Architecture.X64, "SomeWorkload")]
        [TestCase(PlatformID.Unix, Architecture.X64, "SomeTool1")]
        [TestCase(PlatformID.Unix, Architecture.X64, "SomeTool2")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "SomeWorkload")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "SomeTool1")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "SomeTool2")]
        public async Task ExampleWorkloadExecutorVerifiesTheExpectedWorkloadPackageBinaryDependencies(PlatformID platform, Architecture architecture, string expectedBinary)
        {
            this.SetupDefaultBehaviors(platform, architecture);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // The expected workload files are verified to exist in the package
                // on the file system. Similarly to the Moq/mock framework we use, we enable a set of 
                // delegates/actions that can be setup and invoked to confirm certain behaviors.
                HashSet<string> filesConfirmed = new HashSet<string>();
                this.fixture.FileSystem.OnFileExists = (filePath) => filesConfirmed.Add(filePath);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                DependencyPath platformSpecificWorkloadPath = this.fixture.ToPlatformSpecificPath(this.workloadPackage);
                string expectedBinaryPath = this.fixture.Combine(platformSpecificWorkloadPath.Path, expectedBinary);

                CollectionAssert.Contains(filesConfirmed, expectedBinaryPath);
            }
        }

        [Test]
        [TestCase("SomeWorkload")]
        [TestCase("SomeTool1")]
        [TestCase("SomeTool2")]
        public async Task ExampleWorkloadExecutorEnablesTheExpectedWorkloadPackageBinariesAsExecutableOnUnixSystems(string expectedBinary)
        {
            this.SetupDefaultBehaviors(PlatformID.Unix, Architecture.X64);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                // Each of the binaries are attributed as executable on Unix systems using the 'chmod +x' command.
                DependencyPath platformSpecificWorkloadPath = this.fixture.ToPlatformSpecificPath(this.workloadPackage);
                string expectedBinaryPath = this.fixture.Combine(platformSpecificWorkloadPath.Path, expectedBinary);

                Assert.IsTrue(
                    this.fixture.IsChmodAttributed(expectedBinaryPath),
                    $"Binary '{expectedBinaryPath}' was not attributed as executable.");
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ExampleWorkloadExecutorThrowsIfTheWorkoadPackageDoesNotExist(PlatformID platform)
        {
            this.SetupDefaultBehaviors(platform);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // Ensure there are no workload packages registered/existing on the system.
                this.fixture.PackageManager.Clear();

                DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exc.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "configureSystem.exe")]
        [TestCase(PlatformID.Unix, "sudo configureSystem")]
        public async Task ExampleWorkloadExecutorAppliesExpectedSystemSettingsOnFirstRun(PlatformID platform, string expectedCommand)
        {
            this.SetupDefaultBehaviors(platform);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // Setup the scenario where a state object indicating the executor has run before
                // a first time does not exist. This is how the executor determines that it has not
                // performed a first run and thus that it needs to apply the system settings.
                this.fixture.StateManager.Clear();

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(this.fixture.ProcessManager.CommandsExecuted(expectedCommand));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "SomeWorkload.exe Run")]
        [TestCase(PlatformID.Unix, "SomeWorkload Run")]
        public async Task ExampleWorkloadExecutorExecutesTheExpectedWorkloadCommand(PlatformID platform, string expectedExecutable)
        {
            this.SetupDefaultBehaviors(platform);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(this.fixture.ProcessManager.CommandsExecuted(expectedExecutable));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ExampleWorkloadExecutorThrowsIfTheWorkloadCommandFails(PlatformID platform)
        {
            this.SetupDefaultBehaviors(platform);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    InMemoryProcess workloadProcess = this.fixture.CreateProcess(command, arguments, workingDir);
                    if (command.Contains("SomeWorkload"))
                    {
                        workloadProcess.ExitCode = 123;
                        workloadProcess.StandardError.Append("Workload process failed");
                    }

                    return workloadProcess;
                };

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadFailed, error.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void ExampleWorkloadExecutorLogsWorkloadCommandOutput(PlatformID platform)
        {
            this.SetupDefaultBehaviors(platform);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    InMemoryProcess workloadProcess = this.fixture.CreateProcess(command, arguments, workingDir);
                    if (command.Contains("SomeWorkload"))
                    {
                        workloadProcess.ExitCode = 123;
                        workloadProcess.StandardError.Append("Workload process failed");
                    }

                    return workloadProcess;
                };

                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                var metricsLogged = this.fixture.Logger.MessagesLogged($"{nameof(ExampleWorkloadExecutor)}.WorkloadOutput");
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task ExampleWorkloadExecutorCapturesTheExpectedWorkloadMetrics(PlatformID platform)
        {
            this.SetupDefaultBehaviors(platform, Architecture.X64);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                var metricsLogged = this.fixture.Logger.MessagesLogged("SomeWorkload.ScenarioResult");
                Assert.IsNotEmpty(metricsLogged);
                Assert.IsTrue(metricsLogged.Count() == 3);
                Assert.IsTrue(metricsLogged.All(msg => msg.Item3 is EventContext));

                IEnumerable<EventContext> metricsInfo = metricsLogged.Select(msg => msg.Item3 as EventContext);
                
                Assert.IsTrue(metricsInfo.Count() == 3);
                Assert.IsTrue(metricsInfo.ElementAt(0).AreMetricsCaptured(
                    scenarioName: "ExampleTest",
                    metricName: "calculations/sec",
                    toolName: "SomeWorkload",
                    metricCategorization: string.Empty));

                Assert.IsTrue(metricsInfo.ElementAt(1).AreMetricsCaptured(
                   scenarioName: "ExampleTest",
                   metricName: "avg. latency",
                   toolName: "SomeWorkload",
                   metricCategorization: string.Empty));

                Assert.IsTrue(metricsInfo.ElementAt(2).AreMetricsCaptured(
                   scenarioName: "ExampleTest",
                   metricName: "score",
                   toolName: "SomeWorkload",
                   metricCategorization: string.Empty));
            }
        }

        private void SetupDefaultBehaviors(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            // Test Setup Methodology:
            // ----------------------------------------------------------------------------------------
            // Setup all dependencies that are expected by the class under test that represent
            // what would be the "happy path". This is the path where every dependency expected to
            // exist and to be defined correctly actually exists. The class under test is expected to
            // complete its operations successfully in this case. Then in individual tests, one of the
            // dependency behaviors will be modified. This allows all different kinds of variations in
            // potential behaviors to be tested simply and thoroughly.
            //
            // ** See the TESTING_README.md in the root of the solution directory. **
            //
            //
            // What does the flow of the workload executor look like:
            // ----------------------------------------------------------------------------------------
            // The workload executor flow describes the ordered steps required to initialize, configure
            // and execute the workload followed by capturing the metrics/results.
            //
            // 1) Install and initialize the workload package.
            //    The workload package contains the workload executables/binaries/scripts and any other
            //    files or content necessary to run the workload itself.
            //
            // 2) Check that all required workload binaries/scripts exist on the file system.
            //
            // 3) Ensure all required workload binaries/scripts are marked/attributed as executables (for Linux systems).
            //
            // 4) Save an initial state object mimicking workload executors that need to preserve state
            //    information (e.g. client/server interactions).
            //
            // 5) Execute the workload itself.
            //
            // 6) Capture/emit the workload results metrics.
            //
            //
            // What are the dependencies:
            // ----------------------------------------------------------------------------------------
            // The ExampleWorkloadExecutor class has a number of different dependencies that must all be
            // setup correctly in order to fully test the class.
            //
            //   o File System Integration
            //     The file system integration dependency provides read/write access to the file system (e.g.
            //     directories, files).
            //
            //   o Package Manager
            //     The package manager dependency provides the functionality for installing/downloading workload
            //     packages to the system as well as for finding their location.
            //
            //   o State Manager
            //     The state manager dependency provides access to saving and retrieving state objects/definitions.
            //     Workload executors use state objects to maintain information over long periods of time in-between
            //     individual executions (where the information could be lost if maintained only in memory).
            //
            //   o Process Manager
            //     The process manager dependency is used to create isolated processes on the system for execution
            //     of the workload executables and scripts.
            //
            //
            // ...Setting up the Happy Path
            // ----------------------------------------------------------------------------------------
            // Setup the fixture itself to target the platform provided (e.g. Windows, Unix). This ensures the platform
            // specifics are made relevant to that platform (e.g. file system paths, path structures).
            this.fixture = new DependencyFixture();
            this.fixture.Setup(platform, architecture);

            // Expectation 1: The expected parameters are defined
            string workloadName = "SomeWorkload";
            this.fixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                { nameof(Example2WorkloadExecutor.PackageName), workloadName },
                { nameof(Example2WorkloadExecutor.CommandLine), "Run" },
                { nameof(Example2WorkloadExecutor.TestName), "ExampleTest" }
            });

            // Expectation 2: The expected workload package actually exists.
            // Expectation 3: The expected workload binaries/scripts within the workload package exist. Workload packages
            // follow a strict schema allowing for toolset versions that support different platforms and architectures
            // (e.g. linux-x64, linux-arm64, win-x64, win-arm64).
            string platformArch = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);
            List<string> expectedBinaries = platform == PlatformID.Win32NT
                ? new List<string>
                {
                    // Expected binaries on Windows
                    this.fixture.Combine(platformArch, $"{workloadName}.exe"),
                    this.fixture.Combine(platformArch, "SomeTool1.exe"),
                    this.fixture.Combine(platformArch, "SomeTool2.exe")
                }
                : new List<string>
                {
                    // Expected binaries on Linux
                    this.fixture.Combine(platformArch, $"{workloadName}"),
                    this.fixture.Combine(platformArch, "SomeTool1"),
                    this.fixture.Combine(platformArch, "SomeTool2")
                };

            this.fixture.SetupWorkloadPackage(workloadName, expectedFiles: expectedBinaries.ToArray());
            this.workloadPackage = this.fixture.PackageManager.First();

            // Expectation 4: The executor already executed once and applied expected settings. This requires a reboot.
            // The executor uses a state object (typically saved on the file system) to pick back up where it left off
            // once it reboots and is running again. By default we want to test the part of the code after this happens.
            this.fixture.StateManager.Add(
                $"{nameof(Example2WorkloadExecutor)}-state",
                new Example2WorkloadExecutor.WorkloadState { IsFirstRun = false });

            // Expectation 5: The workload runs and produces valid results
            this.validResults = "{ \"calculationsPerSec\": 123456789, \"avgLatency\": 98, \"score\": 450 }";
            this.fixture.ProcessManager.OnProcessCreated = (process) =>
            {
                // e.g. ExampleWorkload.exe Run
                if (process.FullCommand().Contains("Run"))
                {
                    // Mimic the workload process having written valid results.
                    process.StandardOutput.Append(this.validResults);
                }
            };
        }

        // A private class inside of the unit test class is often used to enable the developer
        // to expose protected members of the class under test. This is a simple technique to
        // allow those methods to be tested directly.
        private class TestExample2WorkloadExecutor : Example2WorkloadExecutor
        {
            public TestExample2WorkloadExecutor(DependencyFixture fixture)
            : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            // Use the access modifier "public new" in order to expose the underlying protected method
            // without overriding it. This technique is recommended ONLY for testing scenarios but is very
            // helpful for those.
            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
