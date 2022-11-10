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
    using VirtualClient.Contracts;

    [TestFixture]
    [Ignore("There are some intermittent issue preventing the FunctionTests to return on GitHub Actions.")]
    [Category("Functional")]
    public class NetworkingWorkloadProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();         
        }

        public void SetupFixtureBasedOnPlatformAndArchitecture(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(platformID, architecture, "ClientAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", "Client"),
                new ClientInstance("ServerAgent", "1.2.3.5", "Server"));

            this.mockFixture.SetupDisks(withRemoteDisks: false);           

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.mockFixture.SetupWorkloadPackage("visualstudiocruntime");

            if (platformID == PlatformID.Unix)
            {
                this.mockFixture.SetupFile("/etc/rc.local");
                this.mockFixture.SetupFile("/etc/security/limits.conf");
            }          
        }

        [Test]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.Arm64)]
        public void NetworkingWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platformID, Architecture architecture)
        {
            this.SetupFixtureBasedOnPlatformAndArchitecture(platformID, architecture);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.Arm64)]
        public void NetworkingWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platformID, Architecture architecture)
        {
            // We ensure the workload package does not exist.
            this.SetupFixtureBasedOnPlatformAndArchitecture(platformID, architecture);
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("networking"));
            }
        }

        [Test]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task NetworkingWorkloadProfileInstallsTheExpectedDependencies(string profile, PlatformID platformID, Architecture architecture)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platformID);

            this.SetupFixtureBasedOnPlatformAndArchitecture(platformID, architecture);       

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "networking");
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());

                SystemManagement.IsRebootRequested = false;
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
                        @"powershell Set-NetTCPSetting -AutoReusePortRangeStartPort 10000 -AutoReusePortRangeNumberOfPorts 50000",
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                    };
                    break;
            }

            return commands;
        }
    }
}
