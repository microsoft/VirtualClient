// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class KafkaProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();

            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, "ClientAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", "Client"),
                new ClientInstance("ServerAgent", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-KAFKA.json")]
        public void KafkaWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-KAFKA.json")]
        public void KafkaWorkloadProfileActionsWillNotBeExecutedIfTheDependencyPackagesDoesNotExist(string profile)
        {
            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Any());
            }
        }
    }
}
