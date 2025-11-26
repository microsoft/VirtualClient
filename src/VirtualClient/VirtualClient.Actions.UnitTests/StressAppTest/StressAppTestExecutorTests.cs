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
    public class StressAppTestExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(ScriptExecutorTests), "Examples", "StressAppTest");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string exampleResults;

        public void SetupTest(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);

            this.mockPackage = new DependencyPath("stressapptest", this.mockFixture.GetPackagePath("stressapptest"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.exampleResults = File.ReadAllText(this.mockFixture.Combine(StressAppTestExecutorTests.ExamplesDirectory, "stressAppTestLog_pass.txt"));

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(fe => fe.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(StressAppTestExecutor.PackageName), "stressapptest" },
                { nameof(StressAppTestExecutor.CommandLine), "" },
                { nameof(StressAppTestExecutor.Scenario), "ApplyStress" },
                { nameof(StressAppTestExecutor.Duration), "00:01:00" },
                { nameof(StressAppTestExecutor.UseCpuStressfulMemoryCopy), false }
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void StressAppTestExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestStressAppTestExecutor StressAppTestExecutor = new TestStressAppTestExecutor(this.mockFixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => StressAppTestExecutor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, @"/linux-x64/stressapptest")]
        public async Task StressAppTestExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string command)
        {
            this.SetupTest(platform);

            using (TestStressAppTestExecutor StressAppTestExecutor = new TestStressAppTestExecutor(this.mockFixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $@"{this.mockPackage.Path}{command} -s 60 -l stressapptestLogs";
                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if ($"{exe} {arguments}".Contains(expectedCommand))
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

                await StressAppTestExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public async Task StressAppTestExecutorExecutesAsExpectedWithIntegerBasedTimeParameters(PlatformID platform)
        {
            // This test verifies that the executor runs correctly when Duration is provided as an integer
            // (legacy format), ensuring backward compatibility for partner teams' existing profiles.

            this.SetupTest(platform);

            // Override with integer-based time parameter
            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Duration)] = 120;  // 120 seconds (integer)

            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture))
            {
                // Verify the parameter is correctly converted to TimeSpan
                Assert.AreEqual(TimeSpan.FromSeconds(120), executor.Duration);

                bool commandExecuted = false;
                string expectedCommand = $@"{this.mockPackage.Path}/linux-x64/stressapptest -s 120 -l stressapptestLogs";
                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if ($"{exe} {arguments}".Contains(expectedCommand))
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

                // Verify the executor runs successfully
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void StressAppTestExecutorThrowsOnInvalidProfileDefinition(PlatformID platform)
        {
            this.SetupTest(platform);

            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Scenario)] = string.Empty;
            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Scenario)] = "ApplyStress";
            this.mockFixture.Parameters[nameof(StressAppTestExecutor.CommandLine)] = "-l logfile.txt";
            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.mockFixture.Parameters[nameof(StressAppTestExecutor.CommandLine)] = "";
            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Duration)] = "00:00:00";
            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void StressAppTestExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual("Invalid results. The StressAppTest workload did not produce valid results.", exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, @"/linux-x64/stressapptest")]
        public void StressAppTestExecutorThrowsWhenWorkloadResultsFileNotFound(PlatformID platform, string executablePath)
        {
            this.SetupTest(platform);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>()))
                .Returns(false);

            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(this.mockPackage.Path + executablePath))
                .Returns(true);

            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void StressAppTestExecutorSupportsBackwardCompatibilityWithIntegerBasedTimeParameters(PlatformID platform)
        {
            // This test ensures backward compatibility: partners' profiles using integer-based time parameters
            // (representing seconds) will continue to work after the conversion to TimeSpan-based parameters.

            this.SetupTest(platform);

            // Test 1: Integer format (legacy - seconds as integers)
            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Duration)] = 300;  // 300 seconds (integer)

            TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.mockFixture);

            // Verify integer value is correctly converted to TimeSpan
            Assert.AreEqual(TimeSpan.FromSeconds(300), executor.Duration, 
                "Duration should accept integer (300 seconds) and convert to TimeSpan");

            // Test 2: TimeSpan string format (new format)
            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Duration)] = "00:05:00";  // 5 minutes (TimeSpan format)

            executor = new TestStressAppTestExecutor(this.mockFixture);

            // Verify TimeSpan string value works correctly
            Assert.AreEqual(TimeSpan.FromMinutes(5), executor.Duration, 
                "Duration should accept TimeSpan string format");

            // Test 3: Verify both formats produce equivalent results
            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Duration)] = 180;  // 180 seconds (integer)
            executor = new TestStressAppTestExecutor(this.mockFixture);
            TimeSpan integerBasedDuration = executor.Duration;

            this.mockFixture.Parameters[nameof(StressAppTestExecutor.Duration)] = "00:03:00";  // 3 minutes (TimeSpan format)
            executor = new TestStressAppTestExecutor(this.mockFixture);
            TimeSpan timespanBasedDuration = executor.Duration;

            Assert.AreEqual(integerBasedDuration, timespanBasedDuration, 
                "Integer-based (180) and TimeSpan-based ('00:03:00') parameters must produce identical TimeSpan values");
        }

        private class TestStressAppTestExecutor : StressAppTestExecutor
        {
            public TestStressAppTestExecutor(MockFixture fixture)
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
