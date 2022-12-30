// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;

    [TestFixture]
    [Category("Functional")]
    public class SpecPowerProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("POWER-SPEC30.json")]
        [TestCase("POWER-SPEC50.json")]
        [TestCase("POWER-SPEC70.json")]
        [TestCase("POWER-SPEC100.json")]
        public void SpecPowerWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        // Add Real functional test.
    }
}