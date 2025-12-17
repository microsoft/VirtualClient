// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class FioProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-IO-FIO.json")]
        public void FioWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO.json")]
        public async Task FioWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatform(string profile)
        {
            // The disks are setup in a typical Azure VM scenario
            // (e.g. 1 OS disk, 1 temp/local disk, multiple remote disks that are unformatted).
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: true);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Format disk dependency expectations.
                // By the time the dependencies have been installed, all disks should be formatted and ready for
                // operations against the file system.
                WorkloadAssert.DisksAreInitialized(this.mockFixture);
                WorkloadAssert.DisksHaveAccessPaths(this.mockFixture);

                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "fio");
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO.json")]
        public async Task FioWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
        {
            // The disks are setup in a typical Azure VM scenario
            // (e.g. 1 OS disk, 1 temp/local disk, multiple remote disks that are unformatted).
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withUnformatted: true);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Format disk dependency expectations.
                // By the time the dependencies have been installed, all disks should be formatted and ready for
                // operations against the file system.
                WorkloadAssert.DisksAreInitialized(this.mockFixture);

                // Apt packages expectations
                // There are a few Apt packages that must be installed for the FIO workload to run.
                WorkloadAssert.AptPackageInstalled(this.mockFixture, "fio");
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-RANDWRITE.json", PlatformID.Win32NT, TestName = "FioRandomWriteProfileExecutesOnWindows")]
        [TestCase("PERF-IO-FIO-RANDWRITE.json", PlatformID.Unix, TestName = "FioRandomWriteProfileExecutesOnUnix")]
        public async Task FioRandomWriteWorkloadProfileExecutes(string profile, PlatformID platform)
        {
            string expectedIoEngine = platform == PlatformID.Win32NT ? "windowsaio" : "libaio";
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread ",
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread",
                $"--name=fio_randwrite_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=4k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based",
                $"--name=fio_randwrite_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=1024k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based"
            };

            this.mockFixture.Setup(platform);
            this.mockFixture.SetupDisks(withUnformatted: false);
            string expectedFiles = platform == PlatformID.Win32NT ? $@"win-x64\fio.exe" : $@"linux-x64/fio";
            this.mockFixture.SetupPackage("fio", expectedFiles: expectedFiles);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--name=fio", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_FIO.json"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-SEQWRITE.json", PlatformID.Win32NT, TestName = "FioSequentialWriteProfileExecutesOnWindows")]
        [TestCase("PERF-IO-FIO-SEQWRITE.json", PlatformID.Unix, TestName = "FioSequentialWriteProfileExecutesOnUnix")]
        public async Task FioSequentialWriteWorkloadProfileExecutes(string profile, PlatformID platform)
        {
            string expectedIoEngine = platform == PlatformID.Win32NT ? "windowsaio" : "libaio";
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread ",
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread",
                $"--name=fio_write_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=4k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based",
                $"--name=fio_write_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=1024k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based"
            };

            this.mockFixture.Setup(platform);
            this.mockFixture.SetupDisks(withUnformatted: false);
            string expectedFiles = platform == PlatformID.Win32NT ? $@"win-x64\fio.exe" : $@"linux-x64/fio";
            this.mockFixture.SetupPackage("fio", expectedFiles: expectedFiles);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--name=fio", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_FIO.json"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-RANDREAD.json", PlatformID.Win32NT, TestName = "FioRandomReadProfileExecutesOnWindows")]
        [TestCase("PERF-IO-FIO-RANDREAD.json", PlatformID.Unix, TestName = "FioRandomReadProfileExecutesOnUnix")]
        public async Task FioRandomReadWorkloadProfileExecutes(string profile, PlatformID platform)
        {
            string expectedIoEngine = platform == PlatformID.Win32NT ? "windowsaio" : "libaio";
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread ",
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread",
                $"--name=fio_randread_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=4k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based",
                $"--name=fio_randread_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=1024k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based"
            };

            this.mockFixture.Setup(platform);
            this.mockFixture.SetupDisks(withUnformatted: false);
            string expectedFiles = platform == PlatformID.Win32NT ? $@"win-x64\fio.exe" : $@"linux-x64/fio";
            this.mockFixture.SetupPackage("fio", expectedFiles: expectedFiles);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--name=fio", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_FIO.json"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-SEQREAD.json", PlatformID.Win32NT, TestName = "FioSequentialReadProfileExecutesOnWindows")]
        [TestCase("PERF-IO-FIO-SEQREAD.json", PlatformID.Unix, TestName = "FioSequentialReadProfileExecutesOnUnix")]
        public async Task FioSequentialReadWorkloadProfileExecutes(string profile, PlatformID platform)
        {
            string expectedIoEngine = platform == PlatformID.Win32NT ? "windowsaio" : "libaio";
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread ",
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread",
                $"--name=fio_read_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=4k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based",
                $"--name=fio_read_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=1024k --iodepth=[0-9]+ --ioengine={expectedIoEngine} --direct=1 --ramp_time=30 --runtime=300 --time_based"
            };

            this.mockFixture.Setup(platform);
            this.mockFixture.SetupDisks(withUnformatted: false);
            string expectedFiles = platform == PlatformID.Win32NT ? $@"win-x64\fio.exe" : $@"linux-x64/fio";
            this.mockFixture.SetupPackage("fio", expectedFiles: expectedFiles);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--name=fio", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_FIO.json"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-INTEGRITY.json", PlatformID.Win32NT, TestName = "FioDataIntegrityProfileExecutesOnWindows")]
        [TestCase("PERF-IO-FIO-INTEGRITY.json", PlatformID.Unix, TestName = "FioDataIntegrityProfileExecutesOnUnix")]
        public async Task FioDataIntegrityWorkloadProfileExecutes(string profile, PlatformID platform)
        {
            string expectedIoEngine = platform == PlatformID.Win32NT ? "windowsaio" : "libaio";
            IEnumerable<string> expectedCommands = new List<string>
            {
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread ",
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --thread",
                $"--name=fio_randwrite_4G_4k_d1_th1_verify --size=4G --numjobs=1 --rw=randwrite --bs=4k --iodepth=1 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --verify=sha256 --do_verify=1",
                $"--name=fio_write_4G_4k_d1_th1_verify --size=4G --numjobs=1 --rw=write --bs=4k --iodepth=1 --ioengine={expectedIoEngine} --direct=1 --overwrite=1 --verify=sha256 --do_verify=1"
            };

            this.mockFixture.Setup(platform);
            this.mockFixture.SetupDisks(withUnformatted: false);
            string expectedFiles = platform == PlatformID.Win32NT ? $@"win-x64\fio.exe" : $@"linux-x64/fio";
            this.mockFixture.SetupPackage("fio", expectedFiles: expectedFiles);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("--name=fio", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_FIO.json"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }
    }
}
