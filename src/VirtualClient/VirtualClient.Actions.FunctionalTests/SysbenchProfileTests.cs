// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServerConfiguration;

    [TestFixture]
    [Category("Functional")]
    public class SysbenchProfileTests
    {
        private DependencyFixture fixture;
        private string clientAgentId;
        private string serverAgentId;
        private string sysbenchScriptPath;
        private string mysqlScriptPath;
        private string sysbenchPackagePath;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json")]
        public void SysbenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json", PlatformID.Unix, Architecture.X64)]
        public void SysbenchWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);
            this.fixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json", PlatformID.Unix, Architecture.X64)]
        public async Task SysbenchWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: false);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Sysbench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-MYSQL-OLTP-SYSBENCH.json", PlatformID.Unix, Architecture.X64)]
        public async Task SysbenchWorkloadProfileExecutesTheExpectedWorkloadsOnSingleVMUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);
            this.fixture.SetupDisks(withUnformatted: true);

            this.sysbenchScriptPath = this.fixture.PlatformSpecifics.GetScriptPath("sysbench");
            this.mysqlScriptPath = this.fixture.PlatformSpecifics.GetScriptPath("mysqlserverconfiguration");
            this.sysbenchPackagePath = this.fixture.GetPackagePath("sysbench");

            this.fixture.SetupDirectory(this.sysbenchScriptPath);
            this.fixture.SetupDirectory(this.mysqlScriptPath);

            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(singleVM: true);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.DisksAreInitialized(this.fixture);
                WorkloadAssert.DisksHaveAccessPaths(this.fixture);

                WorkloadAssert.WorkloadPackageInstalled(this.fixture, "sysbench");

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(bool singleVM)
        {
            if (singleVM) 
            {
                return new List<string>()
                {
                    "sudo apt update",
                    "apt install make  automake  libtool  pkg-config libaio-dev  libmysqlclient-dev  libssl-dev --yes --quiet",

                    "git clone https://github.com/akopytov/sysbench.git /home/user/tools/VirtualClient/packages/sysbench",

                    "sudo systemctl start mysql.service",
                    $"sudo mysql --execute=\"DROP DATABASE IF EXISTS sbtest;\"",
                    $"sudo mysql --execute=\"CREATE DATABASE sbtest;\"",
                    $"sudo mysql --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\"",
                    $"sudo mysql --execute=\"SET GLOBAL MAX_CONNECTIONS=1024;\"",
                    $"sudo chmod -R 2777 \"{this.mysqlScriptPath}\"",
                    $"sudo mysql --execute=\"DROP USER IF EXISTS 'sbtest'@'localhost'\"",
                    $"sudo mysql --execute=\"CREATE USER 'sbtest'@'localhost'\"",
                    $"sudo mysql --execute=\"GRANT ALL ON *.* TO 'sbtest'@'localhost'\"",
                    $"sudo {this.mysqlScriptPath}/set-mysql-innodb-directories.sh mountPoint0 mountPoint1 mountPoint2",

                    $"sudo sed -i \"s/CREATE TABLE/CREATE TABLE IF NOT EXISTS/g\" $sysbenchPath/src/lua/oltp_common.lua\r\n",
                    "sudo ./autogen.sh",
                    "sudo ./configure",
                    "sudo make -j",
                    "sudo make install",

                    $"sudo chmod -R 2777 \"{this.sysbenchScriptPath}\"",

                    $"sudo {this.sysbenchScriptPath}/distribute-database.sh {this.sysbenchPackagePath} sbtest 10 99999 1 mountPoint0 mountPoint1 mountPoint2",

                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_only --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_delete --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_insert --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_non_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_points --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_ranges --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=127.0.0.1 --time=300 run"
                };
            }
            else 
            { 
                return new List<string>()
                {
                    "git clone https://github.com/akopytov/sysbench.git /home/user/tools/VirtualClient/packages/sysbench",

                    "sudo ./autogen.sh",
                    "sudo ./configure",
                    "sudo make -j",
                    "sudo make install",

                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_only --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_delete --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_insert --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_update_non_index --threads=8 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_points --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run",
                    $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench select_random_ranges --threads=8 --tables=1 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=300 run"
                };
            }
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.fixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);
        }
    }
}
