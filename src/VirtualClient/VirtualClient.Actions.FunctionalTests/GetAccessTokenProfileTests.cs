// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class GetAccessTokenProfileTests
    {
        private DependencyFixture dependencyFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.dependencyFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        [TestCase("GET-ACCESS-TOKEN.json", PlatformID.Unix)]
        [TestCase("GET-ACCESS-TOKEN.json", PlatformID.Win32NT)]
        public void GetAccessTokenProfileParametersAreInlinedCorrectly(string profile, PlatformID platform)
        {
            this.dependencyFixture.Setup(platform);
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.dependencyFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("GET-ACCESS-TOKEN.json", PlatformID.Unix)]
        [TestCase("GET-ACCESS-TOKEN.json", PlatformID.Win32NT)]
        public void GetAccessTokenProfileParametersAreAvailable(string profile, PlatformID platform)
        {
            this.dependencyFixture.Setup(platform);

            var mandatoryParameters = new List<string> { "TenantId" };
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.dependencyFixture.Dependencies))
            {
                Assert.IsEmpty(executor.Profile.Actions);
                Assert.AreEqual(1, executor.Profile.Dependencies.Count);

                var dependencyBlock = executor.Profile.Dependencies.FirstOrDefault();

                foreach (var parameters in mandatoryParameters)
                {
                    Assert.IsTrue(dependencyBlock.Parameters.ContainsKey(parameters));
                }
            }
        }
    }
}