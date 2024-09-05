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
    public class FioDiscoveryProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-IO-FIO-DISCOVERY.json")]
        public void FioDiscoveryWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-DISCOVERY.json")]
        public async Task FioDiscoveryWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
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
        [TestCase("PERF-IO-FIO-DISCOVERY.json")]
        public async Task FioDiscoveryWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = FioDiscoveryProfileTests.GetFioStressProfileExpectedCommands(PlatformID.Unix);

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
                if (arguments.Contains("fio", StringComparison.OrdinalIgnoreCase))
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
            return new List<string>
            {
                "fio --direct=1 --overwrite=1 --output-format=json --rw=write --bs=256K --numjobs=1 --iodepth=64 --fallocate=none --refill_buffers=1 --name=DiskFill --size=134G --ioengine=libaio",
                "fio --name=fio_discovery_randread_134G_4k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_4k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_4k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randread --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_4k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randread --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_4k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randread --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_4k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randread --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_8k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_8k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_8k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randread --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_8k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randread --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_8k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randread --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_8k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randread --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_16k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_16k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_16k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randread --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_16k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randread --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_16k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randread --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_16k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randread --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_64k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_64k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_64k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randread --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_64k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randread --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_64k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randread --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_64k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randread --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_256k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_256k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_256k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randread --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_256k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randread --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_256k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randread --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_256k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randread --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_1024k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_1024k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randread --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_1024k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randread --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_1024k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randread --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_1024k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randread --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randread_134G_1024k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randread --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_4k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_4k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_4k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_4k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randwrite --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_4k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randwrite --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_4k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randwrite --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_8k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_8k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_8k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_8k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randwrite --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_8k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randwrite --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_8k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randwrite --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_16k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_16k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_16k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_16k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randwrite --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_16k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randwrite --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_16k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randwrite --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_64k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_64k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_64k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_64k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randwrite --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_64k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randwrite --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_64k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randwrite --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_256k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_256k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_256k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_256k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randwrite --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_256k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randwrite --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_256k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randwrite --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_1024k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_1024k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_1024k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_1024k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=randwrite --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_1024k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=randwrite --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_randwrite_134G_1024k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=randwrite --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_4k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_4k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_4k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=read --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_4k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=read --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_4k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=read --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_4k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=read --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_8k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_8k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_8k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=read --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_8k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=read --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_8k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=read --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_8k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=read --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_16k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_16k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_16k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=read --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_16k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=read --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_16k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=read --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_16k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=read --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_64k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_64k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_64k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=read --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_64k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=read --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_64k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=read --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_64k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=read --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_256k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_256k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_256k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=read --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_256k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=read --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_256k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=read --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_256k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=read --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_1024k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_1024k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=read --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_1024k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=read --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_1024k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=read --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_1024k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=read --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_read_134G_1024k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=read --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_4k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_4k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_4k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=write --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_4k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=write --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_4k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=write --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_4k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=write --bs=4k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_8k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_8k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_8k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=write --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_8k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=write --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_8k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=write --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_8k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=write --bs=8k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_16k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_16k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_16k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=write --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_16k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=write --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_16k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=write --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_16k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=write --bs=16k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_64k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_64k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_64k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=write --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_64k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=write --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_64k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=write --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_64k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=write --bs=64k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_256k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_256k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_256k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=write --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_256k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=write --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_256k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=write --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_256k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=write --bs=256k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_1024k_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_1024k_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=write --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_1024k_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=write --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_1024k_d8_th8 --numjobs=8 --iodepth=8 --ioengine=libaio --size=134G --rw=write --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_1024k_d32_th8 --numjobs=8 --iodepth=32 --ioengine=libaio --size=134G --rw=write --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                "fio --name=fio_discovery_write_134G_1024k_d128_th8 --numjobs=8 --iodepth=128 --ioengine=libaio --size=134G --rw=write --bs=1024k --direct=1 --ramp_time=15 --runtime=180 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]"
            };
        }
    }
}
