// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class NetworkConfigurationSetupTests
    {
        private MockFixture mockFixture;
        private string[] exampleLimitsConfigFile;
        private string[] exampleRcLocalFile;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture().Setup(PlatformID.Unix);

            this.exampleLimitsConfigFile = new string[]
            {
                "# /etc/security/limits.conf",
                "#",
                "#Each line describes a limit for a user in the form:",
                "#",
                "#<domain>        <type>  <item>  <value>",
                "#",
                "#Where:",
                "#<domain> can be:",
                "#        - a user name",
                "#        - a group name, with @group syntax",
                "#        - the wildcard *, for default entry",
                "#        - the wildcard %, can be also used with %group syntax,",
                "#                 for maxlogin limit",
                "#        - NOTE: group and wildcard limits are not applied to root.",
                "#          To apply a limit to the root user, <domain> must be",
                "#          the literal username root.",
                "#",
                "#<type> can have the two values:",
                "#        - \"soft\" for enforcing the soft limits",
                "#        - \"hard\" for enforcing hard limits",
                "#",
                "#<item> can be one of the following:",
                "#        - core - limits the core file size (KB)",
                "#        - data - max data size (KB)",
                "#        - fsize - maximum filesize (KB)",
                "#        - memlock - max locked-in-memory address space (KB)",
                "#        - nofile - max number of open file descriptors",
                "#        - rss - max resident set size (KB)",
                "#        - stack - max stack size (KB)",
                "#        - cpu - max CPU time (MIN)",
                "#        - nproc - max number of processes",
                "#        - as - address space limit (KB)",
                "#        - maxlogins - max number of logins for this user",
                "#        - maxsyslogins - max number of logins on the system",
                "#        - priority - the priority to run user process with",
                "#        - locks - max number of file locks the user can hold",
                "#        - sigpending - max number of pending signals",
                "#        - msgqueue - max memory used by POSIX message queues (bytes)",
                "#        - nice - max nice priority allowed to raise to values: [-20, 19]",
                "#        - rtprio - max realtime priority",
                "#        - chroot - change root to directory (Debian-specific)",
                "#",
                "#<domain>      <type>  <item>         <value>",
                "#",
                "#*               soft    core            0",
                "#root            hard    core            100000",
                "#*               hard    rss             10000",
                "#@student        hard    nproc           20",
                "#@faculty        soft    nproc           20",
                "#@faculty        hard    nproc           50",
                "#ftp             hard    nproc           0",
                "#ftp             -       chroot          /ftp",
                "#@student        -       maxlogins       4",
                "# End of file"
            };

            this.exampleRcLocalFile = new string[]
            {
                "# /etc/rc.local"
            };
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheSecurityLimitsConfigurationFileOnUnixSystems_Scenario1()
        {
            // Scenario
            // Settings at the very end of the file.

            List<string> previousContent = new List<string>(this.exampleLimitsConfigFile);

            previousContent.AddRange(new string[]
            {
                "# VC Settings Begin",
                "*   soft    nofile  1048575",
                "*   hard    nofile  1048575",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleLimitsConfigFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheSecurityLimitsConfigurationFileOnUnixSystems_Scenario2()
        {
            // Scenario
            // Settings at the very beginning of the file.

            List<string> previousContent = new List<string>(this.exampleLimitsConfigFile);

            previousContent.InsertRange(0, new string[]
            {
                "# VC Settings Begin",
                "*   soft    nofile  1048575",
                "*   hard    nofile  1048575",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleLimitsConfigFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheSecurityLimitsConfigurationFileOnUnixSystems_Scenario3()
        {
            // Scenario
            // Settings in the middle of the file

            List<string> previousContent = new List<string>(this.exampleLimitsConfigFile);

            previousContent.InsertRange(10, new string[]
            {
                "# VC Settings Begin",
                "*   soft    nofile  1048575",
                "*   hard    nofile  1048575",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleLimitsConfigFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheSecurityLimitsConfigurationFileOnUnixSystems_Scenario4()
        {
            // Scenario
            // Settings have extra lines within.

            List<string> previousContent = new List<string>(this.exampleLimitsConfigFile);

            previousContent.InsertRange(10, new string[]
            {
                "# VC Settings Begin",
                " ",
                "*   soft    nofile  1048575",
                " ",
                " ",
                "*   hard    nofile  1048575",
                " ",
                " ",
                " ",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleLimitsConfigFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheRCLocalFileOnUnixSystems_Scenario1()
        {
            // Scenario
            // Settings at the very end of the file.

            List<string> previousContent = new List<string>(this.exampleRcLocalFile);

            previousContent.AddRange(new string[]
            {
                "# VC Settings Begin",
                "#!/bin/sh",
                "sysctl -w net.ipv4.tcp_tw_reuse=1 # TIME_WAIT work-around",
                "sysctl -w net.ipv4.ip_local_port_range=\"10000 60000\"  # ephemeral ports increased",
                "iptables --flush  # flush the current firewall settings",
                "iptables -I INPUT -j ACCEPT  # accept all inbound traffic",
                "iptables -I OUTPUT -j NOTRACK  # disable connection tracking",
                "iptables -I PREROUTING -j NOTRACK  # disable connection tracking",
                "sysctl -w net.core.busy_poll=50",
                "sysctl -w net.core.busy_read=50",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleRcLocalFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheRCLocalFileOnUnixSystems_Scenario2()
        {
            // Scenario
            // Settings at the very beginning of the file.

            List<string> previousContent = new List<string>(this.exampleRcLocalFile);

            previousContent.InsertRange(0, new string[]
            {
                "# VC Settings Begin",
                "#!/bin/sh",
                "sysctl -w net.ipv4.tcp_tw_reuse=1 # TIME_WAIT work-around",
                "sysctl -w net.ipv4.ip_local_port_range=\"10000 60000\"  # ephemeral ports increased",
                "iptables --flush  # flush the current firewall settings",
                "iptables -I INPUT -j ACCEPT  # accept all inbound traffic",
                "iptables -I OUTPUT -j NOTRACK  # disable connection tracking",
                "iptables -I PREROUTING -j NOTRACK  # disable connection tracking",
                "sysctl -w net.core.busy_poll=50",
                "sysctl -w net.core.busy_read=50",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleRcLocalFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupRemovesPreviouslyAppliedSettingsFromTheRCLocalFileOnUnixSystems_Scenario3()
        {
            // Scenario
            // Settings have extra line breaks in between

            List<string> previousContent = new List<string>(this.exampleRcLocalFile);

            previousContent.AddRange(new string[]
            {
                "# VC Settings Begin",
                "#!/bin/sh",
                " ",
                "sysctl -w net.ipv4.tcp_tw_reuse=1 # TIME_WAIT work-around",
                "sysctl -w net.ipv4.ip_local_port_range=\"10000 60000\"  # ephemeral ports increased",
                " ",
                " ",
                "iptables -I OUTPUT -j NOTRACK  # disable connection tracking",
                "iptables -I PREROUTING -j NOTRACK  # disable connection tracking",
                "iptables -P INPUT ACCEPT  # flush the current firewall settings",
                "iptables -P OUTPUT ACCEPT  # flush the current firewall settings",
                "iptables -P FORWARD ACCEPT  # flush the current firewall settings",
                "iptables --flush  # flush the current firewall settings",
                " ",
                " ",
                "sysctl -w net.core.busy_poll=50",
                "sysctl -w net.core.busy_read=50",
                " ",
                "# VC Settings End",
            });

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleRcLocalFile, cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupHandlesEmptyContentOrFileswWhenRemovingPreviouslyAppliedSettings()
        {
            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(null);
                Assert.IsNotNull(cleanedSettings);
                Assert.IsEmpty(cleanedSettings);

                cleanedSettings = setup.RemovePreviouslyAppliedSettings(new string[0]);
                Assert.IsNotNull(cleanedSettings);
                Assert.IsEmpty(cleanedSettings);
            }
        }

        [Test]
        public void NetworkConfigurationSetupDoesNotMakeAnyChangesToSettingsNotAddedByTheVirtualClient()
        {
            List<string> previousContent = new List<string>(this.exampleLimitsConfigFile);

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                IList<string> cleanedSettings = setup.RemovePreviouslyAppliedSettings(previousContent);
                CollectionAssert.AreEqual(this.exampleLimitsConfigFile, cleanedSettings);
            }
        }

        [Test]
        public async Task NetworkConfigurationSetsTheExpectedSettingsInTheSystemConfigFile_UnixSystems()
        {
            this.mockFixture.Setup(PlatformID.Unix);
            string systemConf = File.ReadAllText(MockFixture.GetDirectory(typeof(NetworkConfigurationSetupTests), "Examples", "example-system.conf"));

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith("system.conf"))))
                    .Returns(true);

                this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(path => path.EndsWith("system.conf")), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(systemConf);

                bool verified = false;
                this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.Is<string>(path => path.EndsWith("system.conf")), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        Assert.IsTrue(Regex.IsMatch(content, "(?<!#)DefaultLimitNOFILE=1048575"));
                        verified = true;
                    });

                await setup.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(verified);
            }
        }

        [Test]
        public async Task NetworkConfigurationSetsTheExpectedSettingsInTheUserConfigFile_UnixSystems()
        {
            this.mockFixture.Setup(PlatformID.Unix);
            string systemConf = File.ReadAllText(MockFixture.GetDirectory(typeof(NetworkConfigurationSetupTests), "Examples", "example-system.conf"));

            using (TestNetworkConfigurationSetup setup = new TestNetworkConfigurationSetup(this.mockFixture))
            {
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith("user.conf"))))
                    .Returns(true);

                this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(path => path.EndsWith("user.conf")), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(systemConf);

                bool verified = false;
                this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.Is<string>(path => path.EndsWith("user.conf")), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        Assert.IsTrue(Regex.IsMatch(content, "(?<!#)DefaultLimitNOFILE=1048575"));
                        verified = true;
                    });

                await setup.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(verified);
            }
        }

        private class TestNetworkConfigurationSetup : NetworkConfigurationSetup
        {
            public TestNetworkConfigurationSetup(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public new IList<string> RemovePreviouslyAppliedSettings(IEnumerable<string> content)
            {
                return base.RemovePreviouslyAppliedSettings(content);
            }
        }
    }
}
