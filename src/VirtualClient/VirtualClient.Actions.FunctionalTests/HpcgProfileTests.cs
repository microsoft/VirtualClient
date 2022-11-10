﻿// Copyright (c) Microsoft Corporation.
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
    public class HpcgProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-HPCG.json")]
        public void HpcgWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-HPCG.json")]
        public async Task HpcgWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("runhpcg.sh", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Hpcg.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            List<string> commands = null;
            switch (platform)
            {
                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        @"sudo chmod \+x ""/home/user/tools/VirtualClient/packages/hpcg/runhpcg.sh""",
                        @"sudo bash /home/user/tools/VirtualClient/packages/hpcg/runhpcg.sh"
                    };
                    break;
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
            {
                { PackageMetadata.ExecutablePath, "spack" }
            };
            this.mockFixture.SetupWorkloadPackage("spack", specifics, expectedFiles: @"spack");

            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);
        }
    }
}
