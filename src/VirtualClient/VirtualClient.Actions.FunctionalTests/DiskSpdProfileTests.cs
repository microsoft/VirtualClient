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
    public class DiskSpdProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-IO-DISKSPD.json")]
        public void DiskSpdWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-IO-DISKSPD.json")]
        public async Task DiskSpdWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatform(string profile)
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
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "diskspd");
            }
        }

        [Test]
        [TestCase("PERF-IO-DISKSPD-RANDWRITE.json", TestName = "DiskSpdRandomWriteProfileExecutes")]
        public async Task DiskSpdRandomWriteWorkloadProfileExecutes(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                // Disk fill (2 disks)
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                // Random write tests
                $"-c496G -b4K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext"
            };

            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupPackage("diskspd", expectedFiles: $@"win-x64\diskspd.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("diskspd", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_DiskSpd.txt"));
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
        [TestCase("PERF-IO-DISKSPD-SEQWRITE.json", TestName = "DiskSpdSequentialWriteProfileExecutes")]
        public async Task DiskSpdSequentialWriteWorkloadProfileExecutes(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                // Disk fill (2 disks)
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                // Sequential write tests
                $"-c496G -b4K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext"
            };

            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupPackage("diskspd", expectedFiles: $@"win-x64\diskspd.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("diskspd", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_DiskSpd.txt"));
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
        [TestCase("PERF-IO-DISKSPD-RANDREAD.json", TestName = "DiskSpdRandomReadProfileExecutes")]
        public async Task DiskSpdRandomReadWorkloadProfileExecutes(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                // Disk fill (2 disks)
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                // Random read tests
                $"-c496G -b4K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext"
            };

            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupPackage("diskspd", expectedFiles: $@"win-x64\diskspd.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("diskspd", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_DiskSpd.txt"));
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
        [TestCase("PERF-IO-DISKSPD-SEQREAD.json", TestName = "DiskSpdSequentialReadProfileExecutes")]
        public async Task DiskSpdSequentialReadWorkloadProfileExecutes(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                // Disk fill (2 disks)
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                // Sequential read tests
                $"-c496G -b4K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext"
            };

            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupPackage("diskspd", expectedFiles: $@"win-x64\diskspd.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("diskspd", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_DiskSpd.txt"));
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
        [TestCase("PERF-IO-DISKSPD-FUNCTIONAL.json", TestName = "DiskSpdFunctionalProfileExecutes")]
        public async Task DiskSpdWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = DiskSpdProfileTests.GetDiskSpdStressProfileExpectedCommands(functional: true);

            // Setup the expectations for the workload
            // - Disks are formatted and ready
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);
            this.mockFixture.SetupPackage("diskspd", expectedFiles: $@"win-x64\diskspd.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("diskspd", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_DiskSpd.txt"));
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
        [TestCase("PERF-IO-DISKSPD.json")]
        public void DiskSpdWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            // Setup disks the expected scenarios:
            // - Disks are formatted and ready
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withUnformatted: false);

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("diskspd.exe"));
            }
        }

        private static IEnumerable<string> GetDiskSpdStressProfileExpectedCommands(bool functional = false)
        {
            var commands = new List<string>
            {
                // Given the test setup created 2 remote disks, we will perform a disk fill on both individually
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                $"-c500G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",

                // After the disk fill, we execute the DiskSpd commands.
                // Random Write tests
                // e.g. -c496G -b4K -r4K -t4 -o128 -w100 -d300 -Suw -W15 -D -L -Rtext D:\\diskspd-test.dat E:\\diskspd-test.dat F:\\diskspd-test.dat
                $"-c496G -b4K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b8K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b12K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b16K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -r4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",

                // Sequential Write tests
                $"-c496G -b4K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b8K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b12K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b16K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -si4K -t[0-9]+ -o[0-9]+ -w100 -d300 -Suw -W15 -D -L -Rtext",

                // Random Read tests
                $"-c496G -b4K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b8K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b12K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b16K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -r4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",

                // Sequential Read tests
                $"-c496G -b4K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b8K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b12K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b16K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext",
                $"-c496G -b1024K -si4K -t[0-9]+ -o[0-9]+ -w0 -d300 -Suw -W15 -D -L -Rtext"
            };
            
            // For functional testing profile, return only the representative subset (6 actions total)
            if (functional)
            {
                return new List<string>
                {
                    // Disk fill (2 disks)
                    commands[0],
                    commands[1],
                    // Random write (small and large block)
                    commands[2], // 4k
                    commands[6], // 1024k
                    // Sequential write (small block)
                    commands[7], // 4k
                    // Random read (small block)
                    commands[12], // 4k
                    // Sequential read (small block)
                    commands[17] // 4k
                };
            }
            
            return commands;
        }
    }
}
