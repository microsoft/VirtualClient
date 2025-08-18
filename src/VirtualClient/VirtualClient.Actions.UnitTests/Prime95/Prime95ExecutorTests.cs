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
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(Prime95ExecutorTests), "Examples", "Prime95");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string exampleResults;

        public void SetupTest(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockPackage = new DependencyPath("prime95", this.mockFixture.GetPackagePath("prime95"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.exampleResults = File.ReadAllText(Path.Combine(Prime95ExecutorTests.ExamplesDirectory, "prime95_results_example_pass.txt"));

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(fe => fe.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.mockFixture.File.Setup(fe => fe.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.mockFixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Prime95Executor.PackageName), "prime95" },
                { nameof(Prime95Executor.Scenario), "Prime95Workload" },
                { nameof(Prime95Executor.Duration), "00:30:00" },
                { nameof(Prime95Executor.MinTortureFFT), "4" },
                { nameof(Prime95Executor.MaxTortureFFT), "8192" },
                { nameof(Prime95Executor.UseHyperthreading), true },
                { nameof(Prime95Executor.ThreadCount), 2 }
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestPrime95Executor prime95Executor = new TestPrime95Executor(this.mockFixture))
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
            this.SetupTest(platform);

            using (TestPrime95Executor prime95Executor = new TestPrime95Executor(this.mockFixture))
            {
                bool commandExecuted = false;
                string expectedCommand = $@"{this.mockPackage.Path}{command} -t";
                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
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
            this.SetupTest(platform);

            this.mockFixture.Parameters[nameof(Prime95Executor.Duration)] = TimeSpan.Zero.ToString();
            using (TestPrime95Executor executor = new TestPrime95Executor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            using (TestPrime95Executor executor = new TestPrime95Executor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.mockFixture.Parameters[nameof(Prime95Executor.MinTortureFFT)] = -1;
            using (TestPrime95Executor executor = new TestPrime95Executor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.mockFixture.Parameters[nameof(Prime95Executor.MinTortureFFT)] = 100;
            this.mockFixture.Parameters[nameof(Prime95Executor.MaxTortureFFT)] = 1;
            using (TestPrime95Executor executor = new TestPrime95Executor(this.mockFixture))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("");

            using (TestPrime95Executor executor = new TestPrime95Executor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));
                
                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorThrowsWhenWorkloadResultsFileNotFound(PlatformID platform)
        {
            this.SetupTest(platform);
            this.mockFixture.File.Setup(fe => fe.Exists(It.Is<string>(file => file.EndsWith("results.txt"))))
                .Returns(false);

            using (TestPrime95Executor executor = new TestPrime95Executor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
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
