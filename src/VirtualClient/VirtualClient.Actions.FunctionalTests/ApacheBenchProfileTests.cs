// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class ApacheBenchProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-APACHEBENCH.json")]
        public void ApacheBenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-APACHEBENCH.json")]
        public async Task ApacheBenchWorkloadProfileInstallsTheExpectedDependenciesOnLinuxPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.AptPackageInstalled(this.mockFixture, "unzip");
                WorkloadAssert.AptPackageInstalled(this.mockFixture, "apache2");
            }
        }

        [Test]
        [TestCase("PERF-APACHEBENCH.json")]
        public async Task ApacheBenchWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "apachehttpserver");
            }
        }

        [Test]
        [TestCase("PERF-APACHEBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-APACHEBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task ApacheBenchWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                "ufw allow 'Apache'",
                "systemctl start apache2"
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - The workload generates valid results.
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.SetupWorkloadPackage("apachehttpserver");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("/usr/bin/ab -k -n", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_ApacheBench.txt"));
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
        [TestCase("PERF-APACHEBENCH.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-APACHEBENCH.json", PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ApacheBenchWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                "-k install"
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - The workload generates valid results.
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.SetupWorkloadPackage("apachehttpserver");

            this.mockFixture
                .SetupFile(this.mockFixture.PlatformSpecifics.Combine(
                    this.mockFixture.PlatformSpecifics.PackagesDirectory,
                    "apachehttpserver",
                    $"win-{architecture.ToString().ToLower()}",
                    "Apache24",
                    "conf",
                    "httpd.conf"));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (command.Contains("ab.exe") && arguments.Contains("-k -n", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_ApacheBench.txt"));
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
        [TestCase("PERF-APACHEBENCH.json", PlatformID.Win32NT)]
        public void ApacheBenchWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_ApacheBench.txt"));
                
                return process;
            };

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("apachehttpserver"));
            }
        }
    }
}
