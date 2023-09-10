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
    public class Prime95ExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string rawText;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();
            this.rawText = File.ReadAllText(@"Examples\Prime95\prime95_results_example_pass.txt");
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestPrime95Executor prime95Executor = new TestPrime95Executor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => prime95Executor.InitializeAsync(EventContext.None, CancellationToken.None));
                
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\prime95.exe")]
        [TestCase(PlatformID.Unix, @"/linux-x64/mprime")]
        public async Task Prime95ExecutorExecutesTheCorrectWorkloadCommands(PlatformID platform, string command)
        {
            this.SetupDefaultBehavior(platform);

            using (TestPrime95Executor prime95Executor = new TestPrime95Executor(this.fixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $@"{this.mockPackage.Path}{command} -t";
                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    if(expectedCommand == $"{exe} {arguments}")
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

                await prime95Executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorValidatesProfileParameters(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);

            this.fixture.Parameters[nameof(Prime95Executor.Duration)] = TimeSpan.Zero.ToString();
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.fixture.Parameters[nameof(Prime95Executor.MinTortureFFT)] = -1;
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.fixture.Parameters[nameof(Prime95Executor.MinTortureFFT)] = 100;
            this.fixture.Parameters[nameof(Prime95Executor.MaxTortureFFT)] = 1;
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));
                
                Assert.AreEqual("Invalid results. The Prime95 workload did not produce valid results.", exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, @"\win-x64\results.txt")]
        [TestCase(PlatformID.Unix, @"/linux-x64/results.txt")]
        public void Prime95ExecutorThrowsWhenWorkloadResultsFileNotFound(PlatformID platform, string resultsPath)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.File.Setup(fe => fe.Exists(this.mockPackage.Path + resultsPath))
                .Returns(false);

            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual($"Expected results file '{this.mockPackage.Path + resultsPath}' not found.", exception.Message);
            }
        }

        private void SetupDefaultBehavior(PlatformID platform)
        {
            this.fixture.Setup(platform);
            this.mockPackage = new DependencyPath("prime95", this.fixture.PlatformSpecifics.GetPackagePath("prime95"));
            this.fixture.PackageManager.OnGetPackage("prime95").ReturnsAsync(this.mockPackage);

            this.fixture.File.Reset();
            this.fixture.File.Setup(fe => fe.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawText);

            this.fixture.File.Setup(fe => fe.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Prime95Executor.PackageName), "prime95" },
                { nameof(Prime95Executor.Scenario), "Prime95Workload" },
                { nameof(Prime95Executor.Duration), "00:30:00" },
                { nameof(Prime95Executor.MinTortureFFT), "4" },
                { nameof(Prime95Executor.MaxTortureFFT), "8192" },
                { nameof(Prime95Executor.UseHyperthreading), true },
                { nameof(Prime95Executor.ThreadCount), 2 }
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
        }

        private class TestPrime95Executor : Prime95Executor
        {
            public TestPrime95Executor(MockFixture fixture)
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
