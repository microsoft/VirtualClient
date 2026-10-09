// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Monitors;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class WindowsETWMonitorProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        [TestCase("MONITORS-ETW.json", 2)]
        [TestCase("MONITORS-ETW-KERNEL.json", 1)]
        [TestCase("MONITORS-ETW-NONKERNEL.json", 1)]
        public void WindowsETWMonitorProfileParametersAreInlinedCorrectly(string profile, int expectedMonitorCount)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
                Assert.AreEqual(expectedMonitorCount, executor.Profile.Monitors.Count);
                Assert.IsTrue(executor.Profile.Monitors.All(monitor => monitor.Type == nameof(WindowsETWMonitor)));
            }
        }
    }
}
