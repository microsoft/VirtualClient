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
    public class StressAppTestProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MEM-STRESSAPPTEST.json", PlatformID.Unix)]
        public void StressAppTestWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform)
        {
            this.fixture.Setup(platform);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MEM-STRESSAPPTEST.json", PlatformID.Unix)]
        public async Task StressAppTestWorkloadProfileExecutesTheExpectedWorkloads(string profile, PlatformID platform)
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
            if (platform == PlatformID.Unix)
            {
                commands = new List<string>
                {
                    $"sudo chmod +x \"{this.fixture.GetPackagePath()}/stressapptest/linux-x64/stressapptest\"",
                    @$"{this.fixture.GetPackagePath()}/stressapptest/linux-x64/stressapptest -s 60 -l stressapptestLogs.*\.txt"
                };
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.fixture.Setup(platform);
            if (platform == PlatformID.Unix)
            {
                this.fixture.SetupWorkloadPackage("stressapptest", expectedFiles: @"linux-x64/stressapptest");
                this.fixture.SetupFile("stressapptest", @"linux-x64/stressapptestLogs_1.txt", TestDependencies.GetResourceFileContents("Results_StressAppTest.txt"));                
            }
        }
    }
}
