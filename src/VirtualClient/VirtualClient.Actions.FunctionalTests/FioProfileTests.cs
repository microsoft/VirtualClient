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

                // Workload dependency package expectations
                // The FIO workload dependency package should have been installed at this point.
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
                WorkloadAssert.AptPackageInstalled(this.mockFixture, "libaio1");
                WorkloadAssert.AptPackageInstalled(this.mockFixture, "libaio-dev");

                // Workload dependency package expectations
                // The FIO workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "fio");
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO.json")]
        public async Task FioWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = FioProfileTests.GetFioStressProfileExpectedCommands(PlatformID.Win32NT);

            // Setup the expectations for the workload
            // - Disks are formatted and ready
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupWorkloadPackage("fio", expectedFiles: $@"win-x64\fio.exe");

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
        [TestCase("PERF-IO-FIO.json")]
        public async Task FioWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = FioProfileTests.GetFioStressProfileExpectedCommands(PlatformID.Unix);

            // Setup the expectations for the workload
            // - Disks are formatted and ready
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupWorkloadPackage("fio", expectedFiles: $@"linux-x64/fio");

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

        private static IEnumerable<string> GetFioStressProfileExpectedCommands(PlatformID platform)
        {
            string expectedIoEngine = platform == PlatformID.Win32NT ? "windowsaio" : "libaio";
            return new List<string>
            {
                // Given the test setup created 2 remote disks, we will perform a disk fill on both individually
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --overwrite=1 --thread --ioengine={expectedIoEngine}",
                $"--name=disk_fill --size=500G --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --overwrite=1 --thread --ioengine={expectedIoEngine}",

                // After the disk fill, we execute the FIO commands.
                // Random Write tests
                $"--name=fio_randwrite_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=4k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randwrite_496G_8k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=8k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randwrite_496G_12k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=12k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randwrite_496G_16k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=16k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randwrite_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randwrite --bs=1024k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",

                // Sequential Write tests
                $"--name=fio_write_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=4k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_496G_8k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=8k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_496G_12k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=12k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_496G_16k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=16k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=write --bs=1024k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",

                // Random Read tests
                $"--name=fio_randread_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=4k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randread_496G_8k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=8k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randread_496G_12k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=12k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randread_496G_16k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=16k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randread_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=randread --bs=1024k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",

                // Sequential Read tests
                $"--name=fio_read_496G_4k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=4k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_read_496G_8k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=8k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_read_496G_12k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=12k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_read_496G_16k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=16k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",
                $"--name=fio_read_496G_1024k_d[0-9]+_th[0-9]+ --size=496G --numjobs=[0-9]+ --rw=read --bs=1024k --iodepth=[0-9]+ --direct=1 --ramp_time=30 --runtime=300 --time_based .+ --ioengine={expectedIoEngine}",

                // Disk Integrity Verification tests (random writes + sequential writes)
                $"--name=fio_randwrite_4G_4k_d1_th1_verify --size=4G --numjobs=1 --rw=randwrite --bs=4k --iodepth=1 --direct=1 --overwrite=1 --verify=sha256 --do_verify=1 .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randwrite_4G_16k_d1_th1_verify --size=4G --numjobs=1 --rw=randwrite --bs=16k --iodepth=1 --direct=1 --overwrite=1 --verify=sha256 --do_verify=1 .+ --ioengine={expectedIoEngine}",
                $"--name=fio_randwrite_4G_1024k_d1_th1_verify --size=4G --numjobs=1 --rw=randwrite --bs=1024k --iodepth=1 --direct=1 --overwrite=1 --verify=sha256 --do_verify=1 .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_4G_4k_d1_th1_verify --size=4G --numjobs=1 --rw=write --bs=4k --iodepth=1 --direct=1 --overwrite=1 --verify=sha256 --do_verify=1 .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_4G_16k_d1_th1_verify --size=4G --numjobs=1 --rw=write --bs=16k --iodepth=1 --direct=1 --overwrite=1 --verify=sha256 --do_verify=1 .+ --ioengine={expectedIoEngine}",
                $"--name=fio_write_4G_1024k_d1_th1_verify --size=4G --numjobs=1 --rw=write --bs=1024k --iodepth=1 --direct=1 --overwrite=1 --verify=sha256 --do_verify=1 .+ --ioengine={expectedIoEngine}"
            };
        }
    }
}
