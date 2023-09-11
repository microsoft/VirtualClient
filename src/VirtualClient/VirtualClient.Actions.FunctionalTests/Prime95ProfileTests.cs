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
    public class Prime95ProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-PRIME95.json", PlatformID.Unix)]
        [TestCase("PERF-CPU-PRIME95.json", PlatformID.Win32NT)]
        public void Prime95WorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform)
        {
            this.fixture.Setup(platform);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-PRIME95.json", PlatformID.Unix)]
        [TestCase("PERF-CPU-PRIME95.json", PlatformID.Win32NT)]
        public async Task Prime95WorkloadProfileExecutesTheExpectedWorkloads(string profile, PlatformID platform)
        {
            this.SetupDefaultMockBehaviors(platform);
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platform);
            
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Prime95.txt"));

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
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
                        @$"{this.fixture.GetPackagePath()}\prime95\win-x64\prime95.exe -t"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        $"sudo chmod +x \"{this.fixture.GetPackagePath()}/prime95/linux-x64/mprime\"",
                        @$"{this.fixture.GetPackagePath()}/prime95/linux-x64/mprime -t"
                    };
                    break;
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.fixture.Setup(platform);
            if (platform == PlatformID.Win32NT)
            {
                this.fixture.SetupWorkloadPackage("prime95", expectedFiles: @"win-x64/prime95.exe");
                this.fixture.SetupFile("prime95", @"win-x64\results.txt", TestDependencies.GetResourceFileContents("Results_Prime95.txt"));
            }
            else
            {
                this.fixture.SetupWorkloadPackage("prime95", expectedFiles: @"linux-x64/mprime");
                this.fixture.SetupFile("prime95", @"linux-x64/results.txt", TestDependencies.GetResourceFileContents("Results_Prime95.txt"));
            }
        }
    }
}
