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
    public class HadoopTerasortProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-TERASORT.json", PlatformID.Unix)]
        public void HadoopTerasortWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform)
        {
            this.fixture.Setup(platform);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-TERASORT.json", PlatformID.Unix)]
        public async Task HadoopTerasortWorkloadProfileExecutesTheExpectedWorkloads(string profile, PlatformID platform)
        {
            string timestamp = DateTime.Now.ToString("ddMMyyHHmmss");

            this.SetupDefaultMockBehaviors(platform);
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platform);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("teragen", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardError.Append(TestDependencies.GetResourceFileContents("Results_HadoopTeragen.txt"));
                } 
                else if (arguments.Contains("terasort", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardError.Append(TestDependencies.GetResourceFileContents("Results_HadoopTerasort.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);
                executor.Dispose();
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
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
                        $"bash -c \"echo y | ssh-keygen -t rsa -P '' -f ~/.ssh/id_rsa\"",
                        $"bash -c \"cat ~/.ssh/id_rsa.pub >> ~/.ssh/authorized_keys\"",
                        $"bash -c \"chmod 0600 ~/.ssh/authorized_keys\"",
                        $"bash -c \"echo y | bin/hdfs namenode -format\"",
                        $"bash -c \"bin/hdfs dfs -mkdir /user\"",
                        $"bash -c \"bin/hdfs dfs -mkdir /user/azureuser\"",
                        $"bash -c sbin/start-dfs.sh",
                        $"bash -c sbin/start-yarn.sh",
                        $"bash -c \"bin/hadoop jar share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.5.jar teragen 100000 /inp-.+-.+\"",
                        $"bash -c \"bin/hadoop jar share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.5.jar terasort /inp-.+-1 /out-.+-.+\"",
                        $"bash -c sbin/stop-dfs.sh",
                        $"bash -c sbin/stop-yarn.sh"
                    };
                    break;
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform = PlatformID.Unix)
        {
            this.fixture.Setup(platform);

            if (platform == PlatformID.Unix)
            {
                string[] paths =
                {
                    "linux-x64/bin/hadoop",
                    "linux-x64/bin/hdfs",
                    "linux-x64/sbin/start-dfs.sh",
                    "linux-x64/sbin/stop-dfs.sh",
                    "linux-x64/bin/yarn",
                    "linux-x64/sbin/start-yarn.sh",
                    "linux-x64/sbin/stop-yarn.sh"
                };
                
                this.fixture.SetupWorkloadPackage("hadoop-3.3.5", expectedFiles: paths);
                this.fixture.SetupWorkloadPackage("microsoft-jdk-11.0.19", expectedFiles: "linux-x64/bin/java");
            }
        }
    }
}
