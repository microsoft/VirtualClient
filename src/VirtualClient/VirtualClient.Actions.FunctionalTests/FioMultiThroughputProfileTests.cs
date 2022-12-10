// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class FioMultiThroughputProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-IO-FIO-MULTITHROUGHPUT.json")]
        public void FioWorkloadMultiThroughputProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-IO-FIO-MULTITHROUGHPUT.json")]
        public async Task FioWorkloadMultiThroughputProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
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
        [TestCase("PERF-IO-FIO-MULTITHROUGHPUT.json")]
        public async Task FioWorkloadMultiThroughputProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = FioMultiThroughputProfileTests.GetFioStressProfileExpectedCommands(PlatformID.Unix);

            // Setup the expectations for the workload
            // - Disks are formatted and ready
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupWorkloadPackage("fio", expectedFiles: $@"linux-x64/fio");
            string jobFilePath = this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.ScriptsDirectory, "fio/oltp-c.fio.jobfile");
            this.mockFixture.SetupFile(jobFilePath, Encoding.ASCII.GetBytes(TestDependencies.GetResourceFileContents("oltp-c.fio.jobfile")));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("linux-x64/fio", StringComparison.OrdinalIgnoreCase))
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
        [TestCase("PERF-IO-FIO-MULTITHROUGHPUT.json")]
        public void FioWorkloadMultiThroughputProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
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
                "/home/user/tools/VirtualClient/packages/fio/linux-x64/fio /home/user/tools/VirtualClient/packages/fio/linux-x64/FioMultiThroughputExecutoroltp-c.fio.jobfile --section initrandomio --section initsequentialio",
                "/home/user/tools/VirtualClient/packages/fio/linux-x64/fio /home/user/tools/VirtualClient/packages/fio/linux-x64/FioMultiThroughputExecutoroltp-c.fio.jobfile --section randomreader --section randomwriter --section sequentialwriter"
            };
        }
    }
}
