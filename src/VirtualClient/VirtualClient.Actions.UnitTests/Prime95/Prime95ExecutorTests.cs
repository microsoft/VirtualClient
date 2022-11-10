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
            this.fixture.Setup(platform);

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
        public void Prime95ExecutorThrowsOnInvalidProfileDefinition(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            
            this.fixture.Parameters[nameof(Prime95Executor.Scenario)] = string.Empty;            
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(Prime95Executor.Scenario)] = "Prime95Workload";
            this.fixture.Parameters[nameof(Prime95Executor.CommandLine)] = null;
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(Prime95Executor.CommandLine)] = "-t";
            this.fixture.Parameters[nameof(Prime95Executor.TimeInMins)] = "0";
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(Prime95Executor.TimeInMins)] = "1";
            this.fixture.Parameters[nameof(Prime95Executor.MinTortureFFT)] = "-1";
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(Prime95Executor.MinTortureFFT)] = "8";
            this.fixture.Parameters[nameof(Prime95Executor.MaxTortureFFT)] = "4";
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(Prime95Executor.MaxTortureFFT)] = "8192";
            this.fixture.Parameters[nameof(Prime95Executor.FFTConfiguration)] = "5";
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }

            this.fixture.Parameters[nameof(Prime95Executor.FFTConfiguration)] = "0";
            this.fixture.Parameters[nameof(Prime95Executor.TortureHyperthreading)] = "5";
            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                Assert.Throws<WorkloadException>(() => executor.ValidateParameters());
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void Prime95ExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform)
        {
            this.SetupDefaultBehavior(platform);
            this.fixture.File.Setup(fe => fe.ReadAllText(It.IsAny<string>()))
                .Returns("");

            using (TestPrime95Executor executor = new TestPrime95Executor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));
                
                Assert.AreEqual(exception.Message , "The Prime95 workload did not produce valid results.");
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

                Assert.AreEqual(exception.Message, "The Prime95 results file was not found at path '" + this.mockPackage.Path + resultsPath + "'.");
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
            this.fixture.File.Setup(fe => fe.ReadAllText(It.IsAny<string>()))
                .Returns(this.rawText);
            this.fixture.File.Setup(fe => fe.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Prime95Executor.PackageName), "prime95" },
                { nameof(Prime95Executor.CommandLine), "-t" },
                { nameof(Prime95Executor.Scenario), "Prime95Workload" },
                { nameof(Prime95Executor.TimeInMins), "1" },
                { nameof(Prime95Executor.MinTortureFFT), "4" },
                { nameof(Prime95Executor.MaxTortureFFT), "8192" },
                { nameof(Prime95Executor.TortureHyperthreading), "1" },
                { nameof(Prime95Executor.NumberOfThreads), "" },
                { nameof(Prime95Executor.FFTConfiguration), "0" }
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

            public new void ValidateParameters()
            {
                base.ValidateParameters();
            }
        }
    }
}
