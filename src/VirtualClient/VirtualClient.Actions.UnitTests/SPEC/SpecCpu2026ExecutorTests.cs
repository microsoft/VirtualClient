// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Contracts;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SpecCpu2026ExecutorTests : MockFixture
    {
        [Test]
        public void SpecCpuStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["SpecCpuInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            SpecCpuExecutor.SpecCpuState result = deserializedState?.ToObject<SpecCpuExecutor.SpecCpuState>();
            Assert.AreEqual(true, result.SpecCpuInitialized);
        }

        [Test]
        public async Task SpecCpu2026ExecutorExecutesTheCorrectCommandsWithInstallationInLinux()
        {
            this.Setup(PlatformID.Unix);
            DependencyPath mockPackage = new DependencyPath("SPECcpu2026", this.PlatformSpecifics.GetPackagePath("speccpu2026", "1.0.1"));

            this.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);
            this.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new[] { this.Combine(mockPackage.Path, "speccpu2026.iso") });

            this.File.Reset();
            this.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            string mockProfileText = System.IO.File.ReadAllText(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockProfileText);
            this.FileSystem.SetupGet(fileSystem => fileSystem.File).Returns(this.File.Object);
            this.FileInfo.Setup(file => file.New(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            Dictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.Benchmarks), "intrate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu2026" },
                { nameof(SpecCpuExecutor.RunPeak), false },
                { nameof(SpecCpuExecutor.Iterations), 2 },
                { nameof(SpecCpuExecutor.Threads), 8 },
                { nameof(SpecCpuExecutor.Copies), 4 }
            };

            List<string> expectedCommands = new List<string>
            {
                $"sudo mount -t iso9660 -o ro,exec,loop {mockPackage.Path}/speccpu2026.iso {this.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {mockPackage.Path}",
                $"sudo chmod -R ugo=rwx {mockPackage.Path}",
                $"sudo umount {this.GetPackagePath()}/speccpu_mount",
                $"bash runspeccpu.sh \"--config vc-linux-x64-2026.cfg --iterations 2 --copies 4 --threads 8 --tune base --reportable intrate\""
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands[processCount], $"{exe} {arguments}");
                processCount++;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    StandardOutput = new ConcurrentBuffer(new StringBuilder()),
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(
                this.Dependencies,
                parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, processCount);
        }

        [Test]
        public async Task SpecCpuExecutorRoutesAndExecutesThe2026Workload()
        {
            DependencyPath mockPackage = this.SetupExecutor(PlatformID.Unix);
            Dictionary<string, IConvertible> parameters = this.CreateParameters();

            List<string> expectedCommands = new List<string>
            {
                $"sudo mount -t iso9660 -o ro,exec,loop {mockPackage.Path}/speccpu2026.iso {this.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {mockPackage.Path}",
                $"sudo chmod -R ugo=rwx {mockPackage.Path}",
                $"sudo umount {this.GetPackagePath()}/speccpu_mount",
                $"bash runspeccpu.sh \"--config vc-linux-x64-2026.cfg --iterations 2 --copies 4 --threads 8 --tune base --reportable intrate\""
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands[processCount], $"{exe} {arguments}");
                processCount++;
                return this.CreateSuccessfulProcess(exe, arguments);
            };

            using (TestSpecCpuExecutor executor = new TestSpecCpuExecutor(this.Dependencies, parameters))
            {
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, processCount);
        }

        [Test]
        public async Task SpecCpu2026ExecutorExecutesTheCorrectCommandsWithInstallationInWindows()
        {
            DependencyPath mockPackage = this.SetupExecutor(PlatformID.Win32NT);
            Dictionary<string, IConvertible> parameters = this.CreateParameters();

            List<string> expectedCommands = new List<string>
            {
                $"powershell -Command \"Mount-DiskImage -ImagePath {mockPackage.Path}\\speccpu2026.iso\"",
                $"powershell -Command \"(Get-DiskImage -ImagePath {mockPackage.Path}\\speccpu2026.iso| Get-Volume).DriveLetter\"",
                $"cmd /c echo 1 | X:\\install.bat {mockPackage.Path}",
                $"powershell -Command \"Dismount-DiskImage -ImagePath {mockPackage.Path}\\speccpu2026.iso\"",
                $"cmd /c runspeccpu.bat --config vc-win-x64-2026.cfg --iterations 2 --copies 4 --threads 8 --tune base --noreportable intrate"
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands[processCount], $"{exe} {arguments}");
                processCount++;
                return this.CreateSuccessfulProcess(exe, arguments);
            };

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(this.Dependencies, parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, processCount);
        }

        [Test]
        public async Task SpecCpu2026ExecutorExecutesTheCorrectCommandsWithSpecificBenchmarksInLinux()
        {
            DependencyPath mockPackage = this.SetupExecutor(PlatformID.Unix);
            Dictionary<string, IConvertible> parameters = this.CreateParameters(
                benchmarks: "748.flightdm_r",
                runPeak: true);

            List<string> expectedCommands = new List<string>
            {
                $"sudo mount -t iso9660 -o ro,exec,loop {mockPackage.Path}/speccpu2026.iso {this.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {mockPackage.Path}",
                $"sudo chmod -R ugo=rwx {mockPackage.Path}",
                $"sudo umount {this.GetPackagePath()}/speccpu_mount",
                $"bash runspeccpu.sh \"--config vc-linux-x64-2026.cfg --iterations 2 --copies 4 --threads 8 --tune all --noreportable 748.flightdm_r\""
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands[processCount], $"{exe} {arguments}");
                processCount++;
                return this.CreateSuccessfulProcess(exe, arguments);
            };

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(this.Dependencies, parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, processCount);
        }

        [TestCase("fprate", false, 2, "base", "reportable")]
        [TestCase("intspeed", true, 2, "all", "reportable")]
        [TestCase("intspeed", true, 1, "all", "noreportable")]
        public async Task SpecCpu2026ExecutorExecutesTheCorrectCommandsWithDifferentProfilesInLinux(
            string profile,
            bool runPeak,
            int iterations,
            string expectedTuning,
            string expectedReportability)
        {
            this.SetupExecutor(PlatformID.Unix);
            Dictionary<string, IConvertible> parameters = this.CreateParameters(
                profile,
                profile,
                runPeak,
                iterations,
                Environment.ProcessorCount,
                Environment.ProcessorCount);

            bool commandCalled = false;
            string expectedArguments =
                $"runspeccpu.sh \"--config vc-linux-x64-2026.cfg --iterations {iterations} --copies {Environment.ProcessorCount} --threads {Environment.ProcessorCount} --tune {expectedTuning} --{expectedReportability} {profile}\"";

            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                commandCalled |= arguments == expectedArguments;
                return this.CreateSuccessfulProcess(exe, arguments);
            };

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(this.Dependencies, parameters))
            {
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);
        }

        [TestCase("fprate", false, "base")]
        [TestCase("intspeed", true, "all")]
        public async Task SpecCpu2026ExecutorExecutesTheCorrectCommandsWithDifferentProfilesInWindows(
            string profile,
            bool runPeak,
            string expectedTuning)
        {
            this.SetupExecutor(PlatformID.Win32NT);
            Dictionary<string, IConvertible> parameters = this.CreateParameters(
                profile,
                profile,
                runPeak,
                2,
                Environment.ProcessorCount,
                Environment.ProcessorCount);

            bool commandCalled = false;
            string expectedArguments =
                $"/c runspeccpu.bat --config vc-win-x64-2026.cfg --iterations 2 --copies {Environment.ProcessorCount} --threads {Environment.ProcessorCount} --tune {expectedTuning} --noreportable {profile}";

            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                commandCalled |= arguments == expectedArguments;
                return this.CreateSuccessfulProcess(exe, arguments);
            };

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(this.Dependencies, parameters))
            {
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);
        }

        [TestCase(null, "CPU2026.*")]
        [TestCase("csv", "CPU2026.*.csv")]
        [TestCase("txt", "CPU2026.*.txt")]
        public void SpecCpu2026ExecutorUsesVersionSpecificResultFilePatterns(string extension, string expectedPattern)
        {
            this.Setup(PlatformID.Unix);

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(
                this.Dependencies,
                new Dictionary<string, IConvertible>
                {
                    { nameof(SpecCpuExecutor.RunPeak), false }
                }))
            {
                Assert.AreEqual(expectedPattern, executor.GetResultsFileSearchPattern(extension));
            }
        }

        [Test]
        public void SpecCpu2026ExecutorScopesInstallationStateByPackageAndVersion()
        {
            this.Setup(PlatformID.Unix);

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(
                this.Dependencies,
                new Dictionary<string, IConvertible>
                {
                    { nameof(SpecCpuExecutor.PackageName), "SPECCPU2026-Custom" },
                    { nameof(SpecCpuExecutor.RunPeak), false }
                }))
            {
                Assert.AreEqual("SpecCpuState-2026-speccpu2026-custom", executor.GetInstallationStateId());
            }
        }

        [TestCase("SpecCpu2026RateBaseExample.csv", "SPECrate(R)2026_int_base")]
        [TestCase("SpecCpu2026RatePeakExample.csv", "SPECrate(R)2026_int_peak")]
        [TestCase("SpecCpu2026SpeedBaseExample.csv", "SPECspeed(R)2026_fp_base")]
        [TestCase("SpecCpu2026SpeedPeakExample.csv", "SPECspeed(R)2026_fp_peak")]
        public void SpecCpu2026ExecutorCreatesAParserForCpu2026CsvResults(string fixture, string expectedSummaryMetric)
        {
            this.Setup(PlatformID.Unix);
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string results = System.IO.File.ReadAllText(Path.Combine(workingDirectory, "test_examples", "SpecCpu", fixture));

            using (TestSpecCpu2026Executor executor = new TestSpecCpu2026Executor(
                this.Dependencies,
                new Dictionary<string, IConvertible>
                {
                    { nameof(SpecCpuExecutor.RunPeak), false }
                }))
            {
                MetricsParser parser = executor.CreateMetricsParser(results, csv: true);
                IList<Metric> metrics = parser.Parse();

                Assert.IsInstanceOf<SpecCpuMetricsParser>(parser);
                Assert.IsTrue(metrics.Any(metric => metric.Name == expectedSummaryMetric));
            }
        }

        private Dictionary<string, IConvertible> CreateParameters(
            string profile = "intrate",
            string benchmarks = "intrate",
            bool runPeak = false,
            int iterations = 2,
            int threads = 8,
            int copies = 4)
        {
            return new Dictionary<string, IConvertible>
            {
                { nameof(SpecCpuExecutor.SpecProfile), profile },
                { nameof(SpecCpuExecutor.Benchmarks), benchmarks },
                { nameof(SpecCpuExecutor.PackageName), "speccpu2026" },
                { nameof(SpecCpuExecutor.RunPeak), runPeak },
                { nameof(SpecCpuExecutor.Iterations), iterations },
                { nameof(SpecCpuExecutor.Threads), threads },
                { nameof(SpecCpuExecutor.Copies), copies }
            };
        }

        private InMemoryProcess CreateSuccessfulProcess(string executable, string arguments)
        {
            return new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments
                },
                StandardOutput = arguments.Contains("Get-DiskImage")
                    ? new ConcurrentBuffer(new StringBuilder("X"))
                    : new ConcurrentBuffer(),
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true
            };
        }

        private DependencyPath SetupExecutor(PlatformID platform)
        {
            this.Setup(platform);
            DependencyPath mockPackage = new DependencyPath(
                "SPECcpu2026",
                this.PlatformSpecifics.GetPackagePath("speccpu2026", "1.0.1"));

            this.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);
            this.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new[] { this.Combine(mockPackage.Path, "speccpu2026.iso") });

            this.File.Reset();
            this.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            string mockProfileText = System.IO.File.ReadAllText(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockProfileText);
            this.FileSystem.SetupGet(fileSystem => fileSystem.File).Returns(this.File.Object);
            this.FileInfo.Setup(file => file.New(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            return mockPackage;
        }

        private class TestSpecCpu2026Executor : SpecCpu2026Executor
        {
            public TestSpecCpu2026Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new string GetResultsFileSearchPattern(string extension = null)
            {
                return base.GetResultsFileSearchPattern(extension);
            }

            public new string GetInstallationStateId()
            {
                return base.GetInstallationStateId();
            }

            public new MetricsParser CreateMetricsParser(string results, bool csv)
            {
                return base.CreateMetricsParser(results, csv);
            }
        }

        private class TestSpecCpuExecutor : SpecCpuExecutor
        {
            public TestSpecCpuExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
