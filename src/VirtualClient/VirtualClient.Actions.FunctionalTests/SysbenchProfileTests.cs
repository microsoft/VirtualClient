// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    [NonParallelizable]
    public class SysbenchProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverAgentId;
        private string originalPassword;

        [SetUp]
        public void SetupFixture()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            Environment.SetEnvironmentVariable("PGPASSWORD", "test-postgresql-password");
            this.fixture = new DependencyFixture();
            this.fixture.Setup(PlatformID.Unix, Architecture.X64);
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [TearDown]
        public void RestorePasswordEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
        }

        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json")]
        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json")]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-OLTP.json")]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json")]
        public void SysbenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json")]
        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json")]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-OLTP.json")]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json")]
        public void SysbenchWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            this.fixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [TestCase("PERF-MYSQL-SYSBENCH-TPCC.json", "MySQL")]
        [TestCase("PERF-POSTGRESQL-SYSBENCH-TPCC.json", "PostgreSQL")]
        public async Task SysbenchTPCCProfileExecutesExpectedClientCommands(string profile, string databaseSystem)
        {
            this.fixture.Setup(PlatformID.Unix, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.fixture.ApiClientManager.GetOrCreateApiClient("1.2.3.5", IPAddress.Parse("1.2.3.5"));

            string sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");
            this.fixture.SetupPackage("sysbench");
            this.fixture.SetupDirectory(sysbenchPackagePath);

            string[] expectedCommands =
            {
                "apt install python3 --yes --quiet",
                $"python3 {sysbenchPackagePath}/configure-workload-generator.py --distro Ubuntu --databaseSystem {databaseSystem} --packagePath {sysbenchPackagePath}",
                $"python3 {sysbenchPackagePath}/run-workload.py --dbName sbtest --databaseSystem {databaseSystem} --benchmark TPCC --threadCount 8 --tableCount 10 --warehouses 1 --workload tpcc --hostIpAddress 1.2.3.5 --durationSecs 1800"
            };

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                StringAssert.DoesNotContain("--password", arguments);
                StringAssert.DoesNotContain("test-postgresql-password", arguments);
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run-workload.py", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(MockFixture.ReadTestResourcesFile("Results_Sysbench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands);
            }
        }
    }
}
