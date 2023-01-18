using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VirtualClient.Common.Contracts;
using VirtualClient.Contracts;

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Functional")]
    public class PostgreSQLServerProfileTests
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
        [TestCase("PERF-POSTGRESQL.json")]
        public async Task POSTGRESQLWorkloadProfileInstallsTheExpectedDependenciesOfClientOnWindowsPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT, Architecture.X64, this.serverAgentId).SetupLayout(
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
        [TestCase("PERF-POSTGRESQL.json")]
        public async Task POSTGRESQLWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatformOfClient(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));
            this.SetupApiClient(serverIPAddress: "1.2.3.5");
            this.mockFixture.SetupWorkloadPackage("postgresql");
            this.mockFixture.SetupWorkloadPackage("PostgresqlPackage");
            this.mockFixture.SetupWorkloadPackage("HammerDbPackage");
            string hammerDbPath = this.mockFixture.GetPackagePath("HammerDbPackage");

            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(hammerDbPath, "createDB.tcl"));

            List<string> commands = new List<string>
            {
                $"{this.mockFixture.PlatformSpecifics.Combine(hammerDbPath, "hammerdbcli.bat")} auto createDB.tcl"
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL.json")]
        public async Task POSTGRESQLWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatformOfClient(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, this.serverAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));
            this.mockFixture.SetupWorkloadPackage("postgresql");
            this.mockFixture.SetupWorkloadPackage("PostgresqlPackage");
            this.mockFixture.SetupWorkloadPackage("HammerDbPackage");
            string hammerDbPath = this.mockFixture.GetPackagePath("HammerDbPackage");

            this.SetupApiClient(serverIPAddress: "1.2.3.5");

            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(hammerDbPath, "createDB.tcl"));

            List<string> commands = new List<string>
            {
                $"sudo bash createDBScript.sh"
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-POSTGRESQL.json")]
        public void POSTGRESQLWorkloadProfileActionsWillNotBeExecutedIfTheClientWorkloadPackageDoesNotExist(string profile)
        {
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
            InMemoryApiClient inMemApiClient = (InMemoryApiClient)apiClient;

            inMemApiClient.OnDeleteState = (stateId) =>
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            };
        }
    }
}
