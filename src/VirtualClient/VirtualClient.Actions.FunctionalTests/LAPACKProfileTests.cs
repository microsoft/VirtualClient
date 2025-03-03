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
    public class LAPACKProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-LAPACK.json")]
        public void LAPACKWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-LAPACK.json")]
        public async Task LAPACKWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            string cygwinPath = this.mockFixture.PlatformSpecifics.Combine("C:", "tools", "cygwin");
            this.mockFixture.SetupFile(cygwinPath);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "lapack");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "cygwin");
            }
        }

        [Test]
        [TestCase("PERF-CPU-LAPACK.json")]
        public async Task LAPACKWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0");
                process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0");
                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "lapack");
            }
        }

        [Test]
        [TestCase("PERF-CPU-LAPACK.json")]
        public async Task LAPACKWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Win32NT);
            string[] expectedFiles = new string[]
                {
                    @"win-x64/cmakescript.sh",
                    @"win-x64/LapackTestScript.sh", @"win-x64/lapack_testing.py",
                    @"win-x64/TESTING/testing_results.txt"
                };
            string cygwinPath = this.mockFixture.PlatformSpecifics.Combine("C:", "tools", "cygwin");

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupFile(cygwinPath);
            this.mockFixture.SetupPackage("lapack", expectedFiles: expectedFiles);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("LapackTestScript.sh", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_LAPACK.txt"));
                    this.mockFixture.SetupPackage("lapack", expectedFiles: @"win-x64\TESTING\testing_results.txt");
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
        [TestCase("PERF-CPU-LAPACK.json")]
        public async Task LAPACKWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);
            string[] expectedFiles = new string[]
            {
                @"linux-x64/LapackTestScript.sh", 
                @"linux-x64/lapack_testing.py",
                @"linux-x64/TESTING/testing_results.txt"
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupPackage("lapack", expectedFiles: expectedFiles);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("LapackTestScript.sh", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_LAPACK.txt"));
                    this.mockFixture.SetupPackage("lapack", expectedFiles: @"linux-x64/TESTING/testing_results.txt");
                }
                else if (arguments.Contains("--version", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 10");
                    process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 10");
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            List<string> commands = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    commands = new List<string>
                    { 
                        $"./cmakescript.sh'",
                        $"./LapackTestScript.sh'"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        $"make",
                        $"LapackTestScript.sh"
                    };
                    break;
            }

            return commands;
        }
    }
}