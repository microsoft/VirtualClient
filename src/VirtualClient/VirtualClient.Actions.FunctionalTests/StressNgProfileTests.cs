// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class StressNgProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-STRESSNG.json")]
        public void StressNgWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.fixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-STRESSNG.json")]
        public async Task StressNgWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.StartsWith("stress-ng", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_StressNg.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecutionMinimumInterval = TimeSpan.Zero;
                await executor.ExecuteAsync(ProfileTiming.Iterations(2), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            List<string> commands = new List<string>
            {
                $@"sudo stress-ng --timeout 60 --cpu {Environment.ProcessorCount} --metrics --yaml /home/user/tools/VirtualClient/packages/stressNg/vcStressNg.yaml"
            };

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.fixture.Setup(PlatformID.Unix);
            this.fixture.SetupDisks(withRemoteDisks: false);
            this.fixture.SetupFile(
                $"{this.fixture.GetPackagePath("stressNg")}/vcStressNg.yaml",
                System.Text.Encoding.UTF8.GetBytes(TestDependencies.GetResourceFileContents("Results_StressNg.txt")));

            ////this.fixture.SystemManagement.Setup(mgr => mgr.FileSystem.Directory.Exists(It.IsAny<string>())).Returns(true);
            ////this.fixture.SystemManagement.Setup(mgr => mgr.FileSystem.File.Exists(It.Is<string>(file => file.EndsWith("yaml")))).Returns(true);
            ////this.fixture.SystemManagement.Setup(mgr => mgr.FileSystem.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            ////    .ReturnsAsync(TestDependencies.GetResourceFileContents("Results_StressNg.txt"));
        }
    }
}
