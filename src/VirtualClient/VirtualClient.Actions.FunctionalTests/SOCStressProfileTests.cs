namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class SOCStressProfileTests
    {
        private DependencyFixture fixture;
        private List<string> expectedCommands = new List<string>
        {
            "sudo fpgafactorytester -duration 300 -verbose",
            "sysbench --test=memory --memory-block-size=1M --memory-total-size=200T --num-threads=8 --max-time=300 run > mem.txt & sysbench --test=cpu --num-threads=8 --cpu-max-prime=2000000 --max-time=300 run > cpu.txt",
            "cat mem.txt",
            "cat cpu.txt",
            "rm mem.txt & rm cpu.txt",
        };

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-SOC-STRESS.json")]
        public void SOCStressWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-SOC-STRESS.json")]
        public async Task SOCStressWorkloadProfileExecutesTheExpectedWorkloads(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.fixture.SshClientManager.OnCreateSshClient = (host, username, password) =>
            {
                InMemorySshClient sshClient = new InMemorySshClient();

                sshClient.OnCreateCommand = (commandText) =>
                {
                    InMemorySshCommand sshCommand = new InMemorySshCommand();
                    sshCommand.CommandText = commandText;

                    sshCommand.OnExecute = () =>
                    {
                        return TestDependencies.GetResourceFileContents("Results_Sysbench.txt");
                    };

                    return sshCommand;
                };

                return sshClient;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                WorkloadAssert.SSHCommandsExecuted(this.fixture, this.expectedCommands.ToArray());
            }
        }
    }
}
