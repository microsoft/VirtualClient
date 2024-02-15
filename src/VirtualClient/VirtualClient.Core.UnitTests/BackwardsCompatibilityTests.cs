// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class BackwardsCompatibilityTests
    {
        private MockFixture fixture;
        private IDictionary<string, string> expectedProfileMappings;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new MockFixture();
            this.expectedProfileMappings = new Dictionary<string, string>
            {
                { "PERF-IO-FIO-STRESS.json", "PERF-IO-FIO.json" },
                { "PERF-IO-DISKSPD-STRESS.json", "PERF-IO-DISKSPD.json" },
                { "PERF-CPU-SPEC-FPRATE.json", "PERF-SPECCPU-FPRATE.json" },
                { "PERF-CPU-SPEC-FPSPEED.json", "PERF-SPECCPU-FPSPEED.json" },
                { "PERF-CPU-SPEC-INTRATE.json", "PERF-SPECCPU-INTRATE.json" },
                { "PERF-CPU-SPEC-INTSPEED.json", "PERF-SPECCPU-INTSPEED.json" }
            };
        }

        [Test]
        public void GetAgentIdReturnsTheExpectedId_Scenario1()
        {
            // Scenario:
            // An agent ID was not supplied on the command line but was supplied in the metadata. When an
            // agent ID is not supplied on the command line, VC will use the name of the machine.
            string agentId = Environment.MachineName;
            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "agentId", $"cluster01,b506ae6a - 1b1a-44e7-99b8-baa243ef6693,vm02,9eb464aa-e869-440f-a5d1-7a53b9c0cd98" }
            };

            string actualAgentId = BackwardsCompatibility.GetAgentId(agentId, metadata);

            Assert.AreNotEqual(agentId, actualAgentId);
            Assert.AreEqual(metadata["agentId"], actualAgentId);
        }

        [Test]
        public void GetAgentIdReturnsTheExpectedId_Scenario2()
        {
            // Scenario:
            // An agent ID was supplied on the command line but not supplied in the metadata. The
            // one on the command line should be used.
            string agentId = $"cluster01,b506ae6a - 1b1a-44e7-99b8-baa243ef6693,vm01,9eb464aa-e869-440f-a5d1-7a53b9c0cd98";
            IDictionary<string, IConvertible> metadata = null;

            string actualAgentId = BackwardsCompatibility.GetAgentId(agentId, metadata);
            Assert.AreEqual(agentId, actualAgentId);

            metadata = new Dictionary<string, IConvertible>();
            actualAgentId = BackwardsCompatibility.GetAgentId(agentId, metadata);
            Assert.AreEqual(agentId, actualAgentId);
        }

        [Test]
        public void GetAgentIdReturnsTheExpectedId_Scenario3()
        {
            // Scenario:
            // An agent ID was supplied on the command line and was supplied in the metadata. The
            // one on the command line matches the one in the metadata.
            string agentId = $"cluster01,b506ae6a - 1b1a-44e7-99b8-baa243ef6693,vm01,9eb464aa-e869-440f-a5d1-7a53b9c0cd98";
            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "agentId", agentId }
            };

            string actualAgentId = BackwardsCompatibility.GetAgentId(agentId, metadata);

            Assert.AreEqual(agentId, actualAgentId);
            Assert.AreEqual(metadata["agentId"], actualAgentId);
        }

        [Test]
        public void GetExperimentIdReturnsTheExpectedId_Scenario1()
        {
            // Scenario:
            // An experiment ID was not supplied on the command line but was supplied in the metadata. When an
            // experiment ID is not supplied on the command line, VC will create a random Guid as the ID.
            string experimentId = Guid.NewGuid().ToString();
            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() }
            };

            string actualExperimentId = BackwardsCompatibility.GetExperimentId(experimentId, metadata);

            Assert.AreNotEqual(experimentId, actualExperimentId);
            Assert.AreEqual(metadata["experimentId"], actualExperimentId);
        }

        [Test]
        public void GetExperimentIdReturnsTheExpectedId_Scenario2()
        {
            // Scenario:
            // An experiment ID was supplied on the command line but not supplied in the metadata. The
            // one on the command line should be used.
            string experimentId = Guid.NewGuid().ToString();
            IDictionary<string, IConvertible> metadata = null;

            string actualExperimentId = BackwardsCompatibility.GetExperimentId(experimentId, metadata);
            Assert.AreEqual(experimentId, actualExperimentId);

            metadata = new Dictionary<string, IConvertible>();
            actualExperimentId = BackwardsCompatibility.GetExperimentId(experimentId, metadata);
            Assert.AreEqual(experimentId, actualExperimentId);
        }

        [Test]
        public void GetExperimentIdReturnsTheExpectedId_Scenario3()
        {
            // Scenario:
            // An experiment ID was supplied on the command line and was supplied in the metadata. The
            // one on the command line matches the one in the metadata.
            string experimentId = Guid.NewGuid().ToString();
            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "experimentId", experimentId }
            };

            string actualExperimentId = BackwardsCompatibility.GetExperimentId(experimentId, metadata);

            Assert.AreEqual(experimentId, actualExperimentId);
            Assert.AreEqual(metadata["experimentId"], actualExperimentId);
        }

        [Test]
        public void TryMapProfileHandlesProfilesByNameOnly()
        {
            foreach (var entry in this.expectedProfileMappings)
            {
                string profile = entry.Key;
                string mappedProfile = entry.Value;

                Assert.IsTrue(BackwardsCompatibility.TryMapProfile(profile, out string otherProfile));
                Assert.IsNotNull(otherProfile);
                Assert.AreEqual(mappedProfile, otherProfile);
            }
        }

        [Test]
        public void TryMapProfileHandlesProfilesInPathsOnUnixPlatforms()
        {
            this.fixture.Setup(PlatformID.Unix);
            foreach (var entry in this.expectedProfileMappings)
            {
                string profile = this.fixture.Combine("/any/path/to/profile", entry.Key);
                string expectedProfile = this.fixture.Combine("/any/path/to/profile", entry.Value);

                Assert.IsTrue(BackwardsCompatibility.TryMapProfile(profile, out string otherProfile));
                Assert.IsNotNull(otherProfile);
                Assert.AreEqual(expectedProfile, otherProfile);
            }
        }

        [Test]
        public void TryMapProfileHandlesProfilesInPathsOnWindowsPlatforms()
        {
            this.fixture.Setup(PlatformID.Win32NT);
            foreach (var entry in this.expectedProfileMappings)
            {
                string profile = this.fixture.Combine(@"C:\any\path\to\profile", entry.Key);
                string expectedProfile = this.fixture.Combine(@"C:\any\path\to\profile", entry.Value);

                Assert.IsTrue(BackwardsCompatibility.TryMapProfile(profile, out string otherProfile));
                Assert.IsNotNull(otherProfile);
                Assert.AreEqual(expectedProfile, otherProfile);
            }
        }

        [Test]
        [TestCase("AZPERF-NETWORK.json")]
        [TestCase("MONITORS-AZURE-HOST.json")]
        [TestCase("MONITORS-DEFAULT.json")]
        [TestCase("MONITORS-NONE.json")]
        [TestCase("PERF-CPU-COREMARK.json")]
        [TestCase("PERF-CPU-OPENSSL.json")]
        [TestCase("PERF-CPU-OPENSSL.json")]
        [TestCase("PERF-GPU-SUPERBENCH.json")]
        [TestCase("PERF-IO-FIO.json")]
        [TestCase("PERF-MEM-LMBENCH.json")]
        [TestCase("PERF-NETWORK.json")]
        [TestCase("POWER-SPEC100.json")]
        public void TryMapProfileHandlesOriginalProfileNamesNotContainingDashLinuxPostfixes(string profile)
        {
            this.fixture.Setup(PlatformID.Win32NT);

            Assert.IsFalse(BackwardsCompatibility.TryMapProfile(profile, out string otherProfile));
            Assert.IsNull(otherProfile);
        }
    }
}
