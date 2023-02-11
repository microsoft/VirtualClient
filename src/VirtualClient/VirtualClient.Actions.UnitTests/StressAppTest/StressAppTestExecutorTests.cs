// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class StressAppTestExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string rawText;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();
            this.rawText = File.ReadAllText(@"Examples\StressAppTest\stressAppTestLog_pass.txt");
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void StressAppTestExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.fixture.Setup(platform);

            using (TestStressAppTestExecutor StressAppTestExecutor = new TestStressAppTestExecutor(this.fixture))
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
            this.SetupDefaultBehavior(platform);

            using (TestStressAppTestExecutor StressAppTestExecutor = new TestStressAppTestExecutor(this.fixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $@"{this.mockPackage.Path}{command} -s 60 -l stressapptestLogs";
                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
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
        public void StressAppTestExecutorThrowsOnInvalidProfileDefinition(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);

            this.fixture.Parameters[nameof(StressAppTestExecutor.Scenario)] = string.Empty;
            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(StressAppTestExecutor.Scenario)] = "ApplyStress";
            this.fixture.Parameters[nameof(StressAppTestExecutor.CommandLine)] = "-l logfile.txt";
            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(StressAppTestExecutor.CommandLine)] = "";
            this.fixture.Parameters[nameof(StressAppTestExecutor.TimeInSeconds)] = "0";
            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void StressAppTestExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.File.Setup(fe => fe.ReadAllText(It.IsAny<string>()))
                .Returns("");

            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(exception.Message, "The StressAppTest workload did not produce valid results. The results file is blank");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, @"/linux-x64/stressapptest")]
        public void StressAppTestExecutorThrowsWhenWorkloadResultsFileNotFound(PlatformID platform, string executablePath)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>()))
                .Returns(false);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(this.mockPackage.Path + executablePath))
                .Returns(true);

            using (TestStressAppTestExecutor executor = new TestStressAppTestExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        private void SetupDefaultBehavior(PlatformID platform)
        {
            this.fixture.Setup(platform);
            this.mockPackage = new DependencyPath("StressAppTest", this.fixture.PlatformSpecifics.GetPackagePath("StressAppTest"));
            this.fixture.PackageManager.OnGetPackage("StressAppTest").ReturnsAsync(this.mockPackage);

            this.fixture.File.Reset();
            this.fixture.File.Setup(fe => fe.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawText);

            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(StressAppTestExecutor.PackageName), "StressAppTest" },
                { nameof(StressAppTestExecutor.CommandLine), "" },
                { nameof(StressAppTestExecutor.Scenario), "ApplyStress" },
                { nameof(StressAppTestExecutor.TimeInSeconds), "60" },
                { nameof(StressAppTestExecutor.UseCpuStressfulMemoryCopy), false }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
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

            public new void ValidateParameters()
            {
                base.ValidateParameters();
            }
        }
    }
}
