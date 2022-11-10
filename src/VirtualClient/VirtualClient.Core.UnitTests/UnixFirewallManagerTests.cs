// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    class UnixFirewallManagerTests
    {   
        private MockFixture fixture;
        private IProcessProxy defaultMemoryProcess;
        private UnixFirewallManager firewallManager;
        

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
            
            this.firewallManager = new UnixFirewallManager(this.fixture.ProcessManager);

            this.defaultMemoryProcess = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "exe",
                    Arguments = "args"
                },
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true
            };
        }

        [Test]
        public void UnixFirewallManagerConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => new UnixFirewallManager(null));
        }

        [Test]
        [TestCase("Any Name", "Any Description", "any", "1234")]
        [TestCase("Any Name", "Any Description", "tcp", "1234")]
        [TestCase("Any Name", "Any Description", "tcp,udp", "1234")]
        [TestCase("Any Name", "Any Description", "tcp", "1234,1235")]
        [TestCase("Any Name", "Any Description", "tcp", "1234,1235,1236")]
        public void UnixFirewallManagerCreatesTheExpectedInboundFirewallRuleForAGivenPort(
            string expectedName, string expectedDescription, string expectedProtocol, string expectedPorts)
        {
            FirewallEntry expectedRule = new FirewallEntry(
                expectedName,
                expectedDescription,
                expectedProtocol,
                expectedPorts.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(port => int.Parse(port)));

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.IsTrue(exe == "sudo");
                Assert.IsTrue(arguments == $"iptables -A INPUT -p {expectedRule.Protocol} --match multiport --dports {expectedPorts} -j ACCEPT");
                return defaultMemoryProcess;
            };

            this.firewallManager.EnableInboundConnectionAsync(expectedRule, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Test]
        [TestCase("Any Name", "Any Description", "any", 1200, 1300)]
        [TestCase("Any Name", "Any Description", "tcp", 1200, 1300)]
        [TestCase("Any Name", "Any Description", "tcp,udp", 1200,1300)]
        public void UnixFirewallManagerCreatesTheExpectedInboundFirewallRuleForAGivenPortRange(
            string expectedName, string expectedDescription, string expectedProtocol, int expectedPortRangeStart, int expectedPortRangeEnd)
        {
            FirewallEntry expectedRule = new FirewallEntry(
                expectedName,
                expectedDescription,
                expectedProtocol,
                new System.Range(expectedPortRangeStart, expectedPortRangeEnd));

          
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.IsTrue(exe == "sudo");
                Assert.IsTrue(arguments == $"iptables -A INPUT -p {expectedRule.Protocol} --match multiport --dports {expectedPortRangeStart}:{expectedPortRangeEnd} -j ACCEPT");
                return defaultMemoryProcess;
            };
            
            this.firewallManager.EnableInboundConnectionAsync(expectedRule, CancellationToken.None).GetAwaiter().GetResult();
        }

        [Test]
        public void UnixFirewallManagerThrowsIfTheAttemptToCreateTheFirewallRuleFails()
        {
            FirewallEntry expectedRule = new FirewallEntry(
                "Any Name",
                "Any Description",
                "Any Protocol",
                new System.Range(100, 200));

            var failedMemoryProcess = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "exe",
                    Arguments = "args"
                },
                ExitCode = -1,
                OnStart = () => true,
                OnHasExited = () => true

            };
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                return failedMemoryProcess;
            };


            DependencyException error = Assert.Throws<DependencyException>(() => this.firewallManager.EnableInboundConnectionAsync(expectedRule, CancellationToken.None)
                .GetAwaiter().GetResult());

            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, error.Reason);
        }
    }
}
