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
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class Example2WorkloadExecutorTests_MockFixture
    {
        private MockFixture fixture;
        private DependencyPath mockWorkloadPackage;
        private string validResults;

        [Test]
        public async Task ExampleWorkloadExecutorInstallsTheExpectedWorkloadPackageOnWindowsSystems()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // The following extension method usage illustrates how the use of an extension method
                // (OnGetPackageLocation) can keep the code both readable and minimal for setting up mock
                // behaviors.
                this.fixture.PackageManager.OnGetPackage()
                    .Callback<string, CancellationToken>((packageName, token) => Assert.AreEqual(packageName, "SomeWorkload"))
                    .ReturnsAsync(this.mockWorkloadPackage);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task ExampleWorkloadExecutorInstallsTheExpectedWorkloadPackageOnUnixSystems()
        {
            this.SetupDefaultBehaviors(PlatformID.Unix);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // The following extension method usage illustrates how the use of an extension method
                // (OnGetPackageLocation) can keep the code both readable and minimal for setting up mock
                // behaviors.
                this.fixture.PackageManager.OnGetPackage()
                    .Callback<string, CancellationToken>((packageName, token) => Assert.AreEqual(packageName, "SomeWorkload"))
                    .ReturnsAsync(this.mockWorkloadPackage);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ExampleWorkloadExecutorVerifiesAndInitializesTheExpectedWorkloadPackageBinariesExistOnWindowsSystems(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // The actual workload binaries will exist in the workload package in the "platform-specific" path.
                // Each workload package can have binaries that support different platforms/architectures.
                // (e.g. \packages\workload\1.0.0\win-x64\workload.exe, \packages\workload\1.0.0\win-arm64\workload.exe).
                DependencyPath workloadPlatformSpecificPackage = this.fixture.ToPlatformSpecificPath(
                    this.mockWorkloadPackage,
                    platform,
                    architecture);

                // The executor will verify the expected workload binaries exist on the file system.
                List<string> expectedBinaries = new List<string>
                {
                    this.fixture.Combine(workloadPlatformSpecificPackage.Path, "SomeWorkload.exe"),
                    this.fixture.Combine(workloadPlatformSpecificPackage.Path, "SomeTool1.exe"),
                    this.fixture.Combine(workloadPlatformSpecificPackage.Path, "SomeTool2.exe")
                };

                this.fixture.File.Setup(file => file.Exists(It.IsAny<string>()))
                    .Callback<string>(path => expectedBinaries.Remove(path)) // Remove the path as it is confirmed
                    .Returns(true);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsEmpty(expectedBinaries);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ExampleWorkloadExecutorVerifiesAndInitializesTheExpectedWorkloadPackageBinariesExistOnLinuxSystems(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // The actual workload binaries will exist in the workload package in the "platform-specific" path.
                // Each workload package can have binaries that support different platforms/architectures.
                // (e.g. \packages\workload\1.0.0\win-x64\workload.exe, \packages\workload\1.0.0\win-arm64\workload.exe).
                DependencyPath workloadPlatformSpecificPackage = this.fixture.ToPlatformSpecificPath(
                    this.mockWorkloadPackage,
                    platform,
                    architecture);

                // The executor will verify the expected workload binaries exist on the file system.
                List<string> expectedBinaries = new List<string>
                {
                    this.fixture.Combine(workloadPlatformSpecificPackage.Path, "SomeWorkload"),
                    this.fixture.Combine(workloadPlatformSpecificPackage.Path, "SomeTool1"),
                    this.fixture.Combine(workloadPlatformSpecificPackage.Path, "SomeTool2")
                };

                this.fixture.File.Setup(file => file.Exists(It.IsAny<string>()))
                    .Callback<string>(path => expectedBinaries.Remove(path)) // Remove the path as it is confirmed
                    .Returns(true);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsEmpty(expectedBinaries);
            }
        }

        [Test]
        public async Task ExampleWorkloadExecutorAppliesExpectedSystemSettingsOnFirstRun()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT, Architecture.X64);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // Setup the scenario where a state object indicating the executor has run before
                // a first time does not exist. This is how the executor determines that it has not
                // performed a first run and thus that it needs to apply the system settings.
                this.fixture.StateManager.OnGetState().ReturnsAsync(null as JObject);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(this.fixture.ProcessManager.Processes.Count() == 1);
                Assert.IsNotNull(this.fixture.ProcessManager.Commands.FirstOrDefault(proc => proc.EndsWith("configureSystem.exe")));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "SomeWorkload.exe Run")]
        [TestCase(PlatformID.Unix, "SomeWorkload Run")]
        public async Task ExampleWorkloadExecutorExecutesTheExpectedWorkloadCommand(PlatformID platform, string expectedExecutable)
        {
            this.SetupDefaultBehaviors(platform, Architecture.X64);
            using (TestExample2WorkloadExecutor executor = new TestExample2WorkloadExecutor(this.fixture))
            {
                // The actual workload binaries will exist in the workload package in the "platform-specific" path.
                // Each workload package can have binaries that support different platforms/architectures.
                // (e.g. \packages\workload\1.0.0\win-x64\workload.exe, \packages\workload\1.0.0\win-arm64\workload.exe).
                DependencyPath workloadPlatformSpecificPackage = this.fixture.ToPlatformSpecificPath(
                    this.mockWorkloadPackage,
                    platform,
                    Architecture.X64);

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedWorkloadProcess = this.fixture.Combine(workloadPlatformSpecificPackage.Path, expectedExecutable);
                Assert.IsTrue(this.fixture.ProcessManager.Commands.Contains(expectedWorkloadProcess));
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
            this.fixture = new MockFixture();
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
            this.mockWorkloadPackage = new DependencyPath(
                workloadName,
                this.fixture.PlatformSpecifics.GetPackagePath(workloadName));

            this.fixture.PackageManager
                .Setup(mgr => mgr.GetPackageAsync(workloadName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockWorkloadPackage);

            // Expectation 3: The expected workload binaries/scripts within the workload package exist. Workload packages
            // follow a strict schema allowing for toolset versions that support different platforms and architectures
            // (e.g. linux-x64, linux-arm64, win-x64, win-arm64).
            DependencyPath platformSpecificWorkloadPackage = this.fixture.ToPlatformSpecificPath(
                this.mockWorkloadPackage,
                platform,
                architecture);

            List<string> expectedBinaries = platform == PlatformID.Win32NT
                ? new List<string>
                {
                    // Expected binaries on Windows
                    this.fixture.Combine(platformSpecificWorkloadPackage.Path, $"{workloadName}.exe"),
                    this.fixture.Combine(platformSpecificWorkloadPackage.Path, "SomeTool1.exe"),
                    this.fixture.Combine(platformSpecificWorkloadPackage.Path, "SomeTool2.exe")
                }
                : new List<string>
                {
                    // Expected binaries on Linux/Unix
                    this.fixture.Combine(platformSpecificWorkloadPackage.Path, $"{workloadName}"),
                    this.fixture.Combine(platformSpecificWorkloadPackage.Path, "SomeTool1"),
                    this.fixture.Combine(platformSpecificWorkloadPackage.Path, "SomeTool2")
                };

            expectedBinaries.ForEach(binary => this.fixture.File.Setup(file => file.Exists(binary)).Returns(true));

            // Expectation 4: The executor already executed once and applied expected settings. This requires a reboot.
            // The executor uses a state object (typically saved on the file system) to pick back up where it left off
            // once it reboots and is running again. By default we want to test the part of the code after this happens.
            this.fixture.StateManager
                .Setup(mgr => mgr.GetStateAsync(
                    $"{nameof(Example2WorkloadExecutor)}-state",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy>()))
                .ReturnsAsync(JObject.FromObject(new Example2WorkloadExecutor.WorkloadState { IsFirstRun = false }));

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
            public TestExample2WorkloadExecutor(MockFixture fixture)
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
