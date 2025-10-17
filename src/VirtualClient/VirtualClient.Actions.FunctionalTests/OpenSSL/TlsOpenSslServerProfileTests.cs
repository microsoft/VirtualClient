// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class TlsOpenSslServerProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            this.mockFixture
                .Setup(PlatformID.Unix, Architecture.X64, "Server01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-OPENSSL-TLS.json")]
        public async Task TlsOpenSslServerWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            IPAddress.TryParse("1.2.3.5", out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient("1.2.3.5", ipAddress);

            State state = new State();

            await apiClient.CreateStateAsync(nameof(State), state, CancellationToken.None);
          
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "openssl");
            }
        }

        [Test]
        [TestCase("PERF-CPU-OPENSSL-TLS.json")]
        public void TlsOpenSslServerWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("openssl s_server"));
            }
        }
    }
}
