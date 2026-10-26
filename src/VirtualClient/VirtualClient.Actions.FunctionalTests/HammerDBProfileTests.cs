// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class HammerDBProfileTests
    {
        private DependencyFixture fixture;

        [SetUp]
        public void SetupMockFixture()
        {
            this.fixture = new DependencyFixture();
            this.fixture.Setup(PlatformID.Unix, Architecture.X64);
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json")]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCH.json")]
        public void HammerDBWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCC.json")]
        [TestCase("PERF-POSTGRESQL-HAMMERDB-TPCH.json")]
        public void HammerDBWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            this.fixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }
    }
}
