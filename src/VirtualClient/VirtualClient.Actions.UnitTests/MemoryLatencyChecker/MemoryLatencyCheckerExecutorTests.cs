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
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Telemetry;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    [Platform(Exclude = "Unix,Linux,MacOsX")]
    public class MemoryLatencyCheckerExecutorTests
    {
        private DependencyFixture fixture;
        private string results;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new DependencyFixture();
            this.results = File.ReadAllText(Path.Combine("test_examples", "MemoryLatencyChecker", "mlc-latency-matrix-single.txt"));
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public void MemoryLatencyCheckerExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture, "win-x64/mlc.exe");
            this.fixture.PackageManager.Clear();

            using (TestMemoryLatencyCheckerExecutor memoryLatencyCheckerExecutor = new TestMemoryLatencyCheckerExecutor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => memoryLatencyCheckerExecutor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64/mlc.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64/memLat.exe")]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/mlc")]
        public async Task MemoryLatencyCheckerExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, Architecture architecture, string expectedBinary)
        {
            this.SetupDefaultBehavior(platform, architecture, expectedBinary);

            string command = expectedBinary.Split("/")[1];

            using (TestMemoryLatencyCheckerExecutor memoryLatencyCheckerExecutor = new TestMemoryLatencyCheckerExecutor(this.fixture))
            {
                await memoryLatencyCheckerExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted(command));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64/mlc.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64/memLat.exe")]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/mlc")]
        public void MemoryLatencyCheckerExecutorThrowsOnInvalidProfileDefinition(PlatformID platform, Architecture architecture, string expectedBinary)
        {
            this.SetupDefaultBehavior(platform, architecture, expectedBinary);

            this.fixture.Parameters[nameof(MemoryLatencyCheckerExecutor.Scenario)] = "MLCWorkload";
            this.fixture.Parameters[nameof(MemoryLatencyCheckerExecutor.Benchmark)] = null;

            using (TestMemoryLatencyCheckerExecutor memoryLatencyCheckerExecutor = new TestMemoryLatencyCheckerExecutor(this.fixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => memoryLatencyCheckerExecutor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64/mlc.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64/memLat.exe")]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/mlc")]
        public void MemoryLatencyCheckerExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform, Architecture architecture, string expectedBinary)
        {
            this.SetupDefaultBehavior(platform, architecture, expectedBinary);

            using (TestMemoryLatencyCheckerExecutor executor = new TestMemoryLatencyCheckerExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                    process.StandardOutput.Clear();

                    return process;
                };

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(exception.Message, "The MLC workload did not produce valid results.");
            }
        }

        private void SetupDefaultBehavior(PlatformID platform,Architecture architecture, string expectedBinary)
        {
            this.fixture.Setup(platform, architecture);
            this.fixture.SetupPackage("mlc", expectedFiles: expectedBinary);
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MemoryLatencyCheckerExecutor.PackageName), "mlc" },
                { nameof(MemoryLatencyCheckerExecutor.Benchmark), "latency_matrix" },
                { nameof(MemoryLatencyCheckerExecutor.Scenario), "MLCWorkload" }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append(this.results);

                return process;
            };
        }

        private class TestMemoryLatencyCheckerExecutor : MemoryLatencyCheckerExecutor
        {
            public TestMemoryLatencyCheckerExecutor(DependencyFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}