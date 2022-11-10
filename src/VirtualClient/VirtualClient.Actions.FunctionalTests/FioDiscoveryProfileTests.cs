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

        [Test]
        [TestCase("PERF-IO-FIO-DISCOVERY.json")]
        public void FioDiscoveryWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            // Setup disks the expected scenarios:
            // - Disks are formatted and ready
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withUnformatted: false);

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("fio"));
            }
        }

        private static IEnumerable<string> GetFioStressProfileExpectedCommands(PlatformID platform)
        {
            return new List<string>
            {
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --overwrite=1 --output-format=json --rw=write --bs=256K --numjobs=[0-9]+ --iodepth=64 --fallocate=none --refill_buffers=1 --name=fiodiscoverydiskfill --size=134G --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randread --bs=4k --name=fiodiscovery_randread_4k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randread --bs=8k --name=fiodiscovery_randread_8k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randread --bs=16k --name=fiodiscovery_randread_16k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randread --bs=64k --name=fiodiscovery_randread_64k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randread --bs=256k --name=fiodiscovery_randread_256k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randread --bs=1024k --name=fiodiscovery_randread_1024k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randwrite --bs=4k --name=fiodiscovery_randwrite_4k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randwrite --bs=8k --name=fiodiscovery_randwrite_8k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randwrite --bs=16k --name=fiodiscovery_randwrite_16k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randwrite --bs=64k --name=fiodiscovery_randwrite_64k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randwrite --bs=256k --name=fiodiscovery_randwrite_256k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=randwrite --bs=1024k --name=fiodiscovery_randwrite_1024k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=read --bs=4k --name=fiodiscovery_read_4k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=read --bs=8k --name=fiodiscovery_read_8k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=read --bs=16k --name=fiodiscovery_read_16k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=read --bs=64k --name=fiodiscovery_read_64k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=read --bs=256k --name=fiodiscovery_read_256k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=read --bs=1024k --name=fiodiscovery_read_1024k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=write --bs=4k --name=fiodiscovery_write_4k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=write --bs=8k --name=fiodiscovery_write_8k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=write --bs=16k --name=fiodiscovery_write_16k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=write --bs=64k --name=fiodiscovery_write_64k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=write --bs=256k --name=fiodiscovery_write_256k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename=",
                $"/home/user/tools/VirtualClient/packages/fio/linux-x64/fio --direct=0 --runtime=300 --overwrite=1 --output-format=json --size=134G --rw=write --bs=1024k --name=fiodiscovery_write_1024k_d[0-9]+_th[0-9]+ --numjobs=[0-9]+ --iodepth=[0-9]+ --ioengine=libaio --filename="
            };
        }
    }
}
