// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.PostgreSQLExecutor;

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
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
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
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
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
        [TestCase("PERF-SQL-POSTGRESQL.json")]
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
        [TestCase("PERF-SQL-POSTGRESQL.json")]
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
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);
            PostgreSQLServerState expectedState = new PostgreSQLServerState
            {
                DatabaseCreated = true,
                InitialSetupComplete = true,
                UserName = "anyUser",
                Password = "anyValue",
                NumOfVirtualUsers = 100,
                WarehouseCount = 100
            };

            apiClient.CreateStateAsync(nameof(PostgreSQLServerState), expectedState, CancellationToken.None)
                .GetAwaiter().GetResult();

            InMemoryApiClient inMemApiClient = (InMemoryApiClient)apiClient;

            inMemApiClient.OnGetState = (stateId) =>
            {
                HttpResponseMessage response;
                Item<JObject> stateItem = null;

                switch (stateId)
                {
                    case nameof(PostgreSQLServerState):
                        stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedState));
                        break;
                }

                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(stateItem.ToJson());
                return response;
            };
        }
    }
}
