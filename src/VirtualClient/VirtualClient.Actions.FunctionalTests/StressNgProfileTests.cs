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
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-STRESSNG.json")]
        public void StressNgWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
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

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("stress-ng", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_StressNg.txt"));
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
            List<string> commands = new List<string>
            {
                $@"sudo stress-ng --timeout 60 --cpu {Environment.ProcessorCount} --metrics --yaml /home/user/tools/VirtualClient/packages/stressNg/vcStressNg.yaml"
            };

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.FileSystem.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.FileSystem.File.ReadAllText(It.IsAny<string>())).Returns(TestDependencies.GetResourceFileContents("Results_StressNg.txt"));
        }
    }
}
