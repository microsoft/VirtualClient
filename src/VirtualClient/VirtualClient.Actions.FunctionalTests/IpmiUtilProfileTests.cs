// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class IpmiUtilProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        [TestCase("MONITORS-IPMIUTIL.json", PlatformID.Unix, 2, 2)]
        [TestCase("MONITORS-IPMIUTIL.json", PlatformID.Win32NT, 2, 2)]
        [TestCase("MONITORS-AZURE-HOST.json", PlatformID.Win32NT, 3, 0)]
        public void IpmiUtilProfileParametersAreInlinedCorrectly(
            string profile,
            PlatformID platform,
            int expectedMonitorCount,
            int expectedDependencyCount)
        {
            this.mockFixture.Setup(platform);

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
                Assert.AreEqual(expectedMonitorCount, executor.Profile.Monitors.Count);
                Assert.AreEqual(expectedDependencyCount, executor.Profile.Dependencies.Count);
                Assert.IsTrue(executor.Profile.Monitors.Any(monitor => monitor.Type == "IpmiUtilSensorMonitor"));
                Assert.IsTrue(executor.Profile.Monitors.Any(monitor => monitor.Type == "IpmiUtilSelMonitor"));
            }
        }
    }
}
