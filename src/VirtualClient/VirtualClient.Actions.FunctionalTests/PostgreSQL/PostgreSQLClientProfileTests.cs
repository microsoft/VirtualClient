using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Amqp.Framing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VirtualClient.Common.Contracts;
using VirtualClient.Contracts;
using static VirtualClient.Actions.CPSExecutor2;
using static VirtualClient.Actions.LatteExecutor2;
using static VirtualClient.Actions.NTttcpExecutor2;
using static VirtualClient.Actions.SockPerfExecutor2;

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Functional")]
    public class PostgreSQLClientProfileTests
    {
        private DependencyFixture mockFixture;
        private string clientAgentId;
        private string serverAgentId;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            /*this.mockFixture.SetupDisks(withRemoteDisks: true, withUnformatted: true);
            this.mockFixture.SetupWorkloadPackage("sqlserver2019");
            this.mockFixture.SetupWorkloadPackage("sqlbackupfiles", metadata: new Dictionary<string, IConvertible>
            {
                ["databaseName"] = "anydb",
                ["databaseDataFileName"] = "anydb_root",
                ["databaseLogFileName"] = "anydb_log"
            });

            string packagePath = this.mockFixture.GetPackagePath("sqlserver2019", "win-x64");
            string installationScriptsPath = this.mockFixture.Combine(packagePath, "scripts");

            this.actionInstallConfigFile = this.mockFixture.Combine(installationScriptsPath, "SystemConfig-Install.json");
            this.actionConfigureConfigFile = this.mockFixture.Combine(installationScriptsPath, "SystemConfig-Configure.json");
            this.actionRestoreDatabaseConfigFile = this.mockFixture.Combine(installationScriptsPath, "SystemConfig-RestoreDatabase.json");
            this.actionCreateTempDBConfigFile = this.mockFixture.Combine(installationScriptsPath, "SystemConfig-CreateTempDB.json");
            this.configureScript = this.mockFixture.Combine(installationScriptsPath, "ConfigureSUT.ps1");
            this.isoFile = this.mockFixture.Combine(packagePath, "SQLServer-2019-123.iso");

            byte[] mockActionParameters = Encoding.ASCII.GetBytes("{\"key\":\"value\"}");
            this.mockFixture.SetupFile(this.actionInstallConfigFile, mockActionParameters);
            this.mockFixture.SetupFile(this.actionConfigureConfigFile, mockActionParameters);
            this.mockFixture.SetupFile(this.actionRestoreDatabaseConfigFile, mockActionParameters);
            this.mockFixture.SetupFile(this.actionCreateTempDBConfigFile, mockActionParameters);
            this.mockFixture.SetupFile(this.configureScript);
            this.mockFixture.SetupFile(this.isoFile);*/
        }

        [Test]
        [TestCase("PERF-PostgreSQL.json")]
        public async Task PostgreSQLWorkloadProfileInstallsTheExpectedDependenciesOfClientOnWindowsPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "postgresql");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "PostgresqlPackage");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "HammerDbPackage");

                /*List<string> commands = new List<string>
                {
                    $"-ExecutionPolicy unrestricted -File \\\"{this.configureScript.Replace("\\", "\\\\")}\\\" -systemConfigFile {this.actionInstallConfigFile.Replace("\\", "\\\\")}",
                    $"-ExecutionPolicy unrestricted -File \\\"{this.configureScript.Replace("\\", "\\\\")}\\\" -systemConfigFile {this.actionConfigureConfigFile.Replace("\\", "\\\\")}",
                    $"-ExecutionPolicy unrestricted -File \\\"{this.configureScript.Replace("\\", "\\\\")}\\\" -systemConfigFile {this.actionRestoreDatabaseConfigFile.Replace("\\", "\\\\")}",
                    $"-ExecutionPolicy unrestricted -File \\\"{this.configureScript.Replace("\\", "\\\\")}\\\" -systemConfigFile {this.actionCreateTempDBConfigFile.Replace("\\", "\\\\")}"
                };

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
                WorkloadAssert.DisksAreInitialized(this.mockFixture);*/
            }
        }

        [Test]
        [TestCase("PERF-PostgreSQL.json")]
        public async Task PostgreSQLWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatformOfClient(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));
            this.mockFixture.SetupWorkloadPackage("postgresql");
            this.mockFixture.SetupWorkloadPackage("PostgresqlPackage");
            this.mockFixture.SetupWorkloadPackage("HammerDbPackage");
            string hammerDbPath = this.mockFixture.GetPackagePath("HammerDbPackage");

            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(hammerDbPath, "runTransactions.tcl"));

            this.SetupApiClient(serverIPAddress: "1.2.3.5");

            List<string> commands = new List<string>
            {
                $"{this.mockFixture.PlatformSpecifics.Combine(hammerDbPath, "hammerdbcli.bat")} auto runTransactions.tcl"
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-PostgreSQL.json")]
        public async Task PostgreSQLWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatformOfClient(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));
            this.mockFixture.SetupWorkloadPackage("postgresql");
            this.mockFixture.SetupWorkloadPackage("PostgresqlPackage");
            this.mockFixture.SetupWorkloadPackage("HammerDbPackage");
            string hammerDbPath = this.mockFixture.GetPackagePath("HammerDbPackage");

            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(hammerDbPath, "runTransactions.tcl"));

            this.SetupApiClient(serverIPAddress: "1.2.3.5");

            List<string> commands = new List<string>
            {
                $"sudo bash runTransactionsScript.sh"
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-PostgreSQL.json")]
        public void PostgreSQLWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT, Architecture.X64);

            this.mockFixture.PackageManager.Clear();
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        private void SetupApiClient(string serverIPAddress)
        {
            // this.SetupApiClient(serverIPAddress: "1.2.3.5");
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);

            // IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);

            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(PostgreSQLState)] = PostgreSQLState.DBCreated
            });

            /*State expectedPostgresqlParameterState = new State(new Dictionary<string, IConvertible>
            {
                
            });*/

            apiClient.CreateStateAsync(nameof(PostgreSQLState), expectedState, CancellationToken.None)
                .GetAwaiter().GetResult();

            InMemoryApiClient inMemApiClient = (InMemoryApiClient)apiClient;

            inMemApiClient.OnGetState = (stateId) =>
            {
                HttpResponseMessage response;
                Item<JObject> stateItem = null;

                switch (stateId)
                {
                    case nameof(PostgreSQLState):
                        stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedState));
                        break;

                    case nameof(PostgreSQLParameters):
                        stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedState));
                        break;
                }

                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(stateItem.ToJson());
                return response;
            };

            /*apiClient.GetStateAsync(nameof(PostgreSQLParameters), CancellationToken.None)
                .GetAwaiter().GetResult();*/

            /*State swarmCommand = new State(new Dictionary<string, IConvertible>
            {
                [nameof(DeathStarBenchExecutor.SwarmCommand)] = "mock command"
            });

            apiClient.CreateStateAsync(nameof(DeathStarBenchExecutor.SwarmCommand), swarmCommand, CancellationToken.None)
                .GetAwaiter().GetResult();*/
        }
    }
}
