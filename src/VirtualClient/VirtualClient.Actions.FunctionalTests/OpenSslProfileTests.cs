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
    public class OpenSslProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-OPENSSL.json")]
        public void OpenSslWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-OPENSSL.json")]
        public async Task OpenSslWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withRemoteDisks: false);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "openssl");
            }
        }

        [Test]
        [TestCase("PERF-CPU-OPENSSL.json")]
        public async Task OpenSslWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "openssl");
            }
        }

        [Test]
        [TestCase("PERF-CPU-OPENSSL.json")]
        public async Task OpenSslWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = OpenSslProfileTests.GetProfileExpectedCommands(PlatformID.Win32NT);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupPackage("openssl", expectedFiles: @"win-x64\bin\openssl.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("openssl", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results-OpenSSL-Speed.txt"));
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
        [TestCase("PERF-CPU-OPENSSL.json")]
        public async Task OpenSslWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = OpenSslProfileTests.GetProfileExpectedCommands(PlatformID.Unix);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupPackage("openssl", expectedFiles: @"linux-x64/bin/openssl");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("openssl", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results-OpenSSL-Speed.txt"));
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
        [TestCase("PERF-CPU-OPENSSL.json")]
        public void OpenSslWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            // Setup disks the expected scenarios:
            // - Disks are formatted and ready
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("openssl"));
            }
        }

        private static IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            List<string> commands = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    commands = new List<string>
                    {
                        $"openssl.exe speed -elapsed -seconds 100 md5",
                        $"openssl.exe speed -elapsed -seconds 100 sha1",
                        $"openssl.exe speed -elapsed -seconds 100 sha256",
                        $"openssl.exe speed -elapsed -seconds 100 sha512",
                        $"openssl.exe speed -elapsed -seconds 100 des-ede3",
                        $"openssl.exe speed -elapsed -seconds 100 aes-128-cbc",
                        $"openssl.exe speed -elapsed -seconds 100 aes-192-cbc",
                        $"openssl.exe speed -elapsed -seconds 100 aes-256-cbc",
                        $"openssl.exe speed -elapsed -seconds 100 camellia-128-cbc",
                        $"openssl.exe speed -elapsed -seconds 100 camellia-192-cbc",
                        $"openssl.exe speed -elapsed -seconds 100 camellia-256-cbc",
                        $"openssl.exe speed -elapsed -seconds 100 rsa2048",
                        $"openssl.exe speed -elapsed -seconds 100 rsa4096"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 md5",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 sha1",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 sha256",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 sha512",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 des-ede3",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 aes-128-cbc",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 aes-192-cbc",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 aes-256-cbc",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 camellia-128-cbc",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 camellia-192-cbc",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 camellia-256-cbc",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 rsa2048",
                        $"openssl speed -multi [0-9]+ -elapsed -seconds 100 rsa4096",
                    };
                    break;
            }

            return commands;
        }
    }
}
