// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using VirtualClient.Common;
    using Moq;
    using NUnit.Framework;
    using Microsoft.Azure.Amqp.Framing;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SystemManagementTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public void SystemManagementIsComprisedOfTheExpectedDependencies_WindowsScenario(Architecture architecture)
        {
            string expectedAgentId = Guid.NewGuid().ToString();
            string expectedExperimentId = Guid.NewGuid().ToString();

            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, architecture);
            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(expectedAgentId, expectedExperimentId, platformSpecifics);

            Assert.AreEqual(PlatformID.Win32NT, systemManagement.Platform);
            Assert.AreEqual(architecture, systemManagement.CpuArchitecture);
            Assert.AreEqual(expectedAgentId, systemManagement.AgentId);
            Assert.AreEqual(expectedExperimentId, systemManagement.ExperimentId);
            Assert.IsInstanceOf<WindowsDiskManager>(systemManagement.DiskManager);
            Assert.IsInstanceOf<FileSystem>(systemManagement.FileSystem);
            Assert.IsInstanceOf<WindowsFirewallManager>(systemManagement.FirewallManager);
            Assert.IsInstanceOf<PackageManager>(systemManagement.PackageManager);
            Assert.IsInstanceOf<WindowsProcessManager>(systemManagement.ProcessManager);
            Assert.IsInstanceOf<StateManager>(systemManagement.StateManager);
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public void SystemManagementIsComprisedOfTheExpectedDependencies_UnixScenario(Architecture architecture)
        {
            string expectedAgentId = Guid.NewGuid().ToString();
            string expectedExperimentId = Guid.NewGuid().ToString();

            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, architecture);
            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(expectedAgentId, expectedExperimentId, platformSpecifics);

            Assert.AreEqual(PlatformID.Unix, systemManagement.Platform);
            Assert.AreEqual(architecture, systemManagement.CpuArchitecture);
            Assert.AreEqual(expectedAgentId, systemManagement.AgentId);
            Assert.AreEqual(expectedExperimentId, systemManagement.ExperimentId);
            Assert.IsInstanceOf<UnixDiskManager>(systemManagement.DiskManager);
            Assert.IsInstanceOf<FileSystem>(systemManagement.FileSystem);
            Assert.IsInstanceOf<UnixFirewallManager>(systemManagement.FirewallManager);
            Assert.IsInstanceOf<PackageManager>(systemManagement.PackageManager);
            Assert.IsInstanceOf<UnixProcessManager>(systemManagement.ProcessManager);
            Assert.IsInstanceOf<StateManager>(systemManagement.StateManager);
        }
    }
}
