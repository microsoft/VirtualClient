// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class ExecutionProfileTests
    {
        private IFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ExecutionProfileValidatesRequiredParameters(string invalidParameter)
        {
            ExecutionProfile validComponent = this.mockFixture.Create<ExecutionProfile>();
            Assert.Throws<ArgumentException>(() => new ExecutionProfile(
                invalidParameter,
                validComponent.MinimumExecutionInterval,
                validComponent.Actions,
                validComponent.Dependencies,
                validComponent.Monitors,
                validComponent.Metadata,
                validComponent.Parameters));
        }

        [Test]
        public void ExecutionProfileIsJsonSerializableByDefault()
        {
            ExecutionProfile profile = this.mockFixture.Create<ExecutionProfile>();
            SerializationAssert.IsJsonSerializable<ExecutionProfile>(profile);
        }

        [Test]
        [TestCase("TEST-PROFILE-1.json")]
        [TestCase("TEST-PROFILE-2.json")]
        public void ExecutionProfileCanDeserializeProfileFiles(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();
        }

        [Test]
        [TestCase("TEST-PROFILE-3-PARALLEL.json")]
        public void ExecutionProfileCanDeserializeProfileFilesWithParallelExecutionComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            Assert.IsNotEmpty(profile.Actions);
            Assert.IsTrue(profile.Actions.Count == 2);

            Assert.IsNotEmpty(profile.Actions[1].Components);
            Assert.IsTrue(profile.Actions[1].Components.Count() == 2);
        }

        [Test]
        [TestCase("TEST-PROFILE-1-PARALLEL-LOOP.json")]
        public void ExecutionProfileCanDeserializeProfileFilesWithParallelLoopExecutionComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            Assert.IsNotEmpty(profile.Actions);
            Assert.IsTrue(profile.Actions.Count == 2);

            Assert.IsNotEmpty(profile.Actions[1].Components);
            Assert.IsTrue(profile.Actions[1].Components.Count() == 2);
        }

        [Test]
        [TestCase("TEST-PROFILE-1-SEQUENTIAL.json")]
        public void ExecutionProfileCanDeserializeProfileFilesWithSequentialExecutionComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            Assert.IsNotEmpty(profile.Actions);
            Assert.IsTrue(profile.Actions.Count == 2);

            Assert.IsNotEmpty(profile.Actions[1].Components);
            Assert.IsTrue(profile.Actions[1].Components.Count() == 2);
        }

        [Test]
        public void ExecutionProfileImplementsHashCodeSemanticsCorrectly()
        {
            ExecutionProfile profile = this.mockFixture.Create<ExecutionProfile>();
            ExecutionProfile profile2 = this.mockFixture.Create<ExecutionProfile>();
            EqualityAssert.CorrectlyImplementsHashcodeSemantics<ExecutionProfile>(() => profile, () => profile2);
        }

        [Test]
        public void ExecutionProfileImplementsEqualitySemanticsCorrectly()
        {
            ExecutionProfile profile = this.mockFixture.Create<ExecutionProfile>();
            ExecutionProfile profile2 = this.mockFixture.Create<ExecutionProfile>();
            EqualityAssert.CorrectlyImplementsEqualitySemantics<ExecutionProfile>(() => profile, () => profile2);
        }

        [Test]
        [Platform(Include = "64-bit")] // assumes little-endian architecture for hash code generation
        [TestCase("TEST-PROFILE-1.json", "279003058894895889018486895561353871838116508168")]
        [TestCase("TEST-PROFILE-2.json", "27436979332165279148580205095431808728699242072")]
        [TestCase("TEST-PROFILE-4.json", "1326284617660594785167171193396646697439110051452")]
        [TestCase("TEST-PROFILE-5.json", "1024016195510512526363371526971708676820695027365")]
        [TestCase("TEST-PROFILE-3-PARALLEL.json", "170719133252643968401267092175966399376412682155")]
        [TestCase("TEST-PROFILE-2-PARALLEL-LOOP.json", "496529190653064795011428134491477848965424897886")]
        public void ExecutionProfileGeneratesPredictableHashCodes(string profileName, string expectedHashCode)
        {
            // The hash should not change regardless of the number of times the profile is deserialized.
            for (int check = 0; check < 10; check++)
            {
                ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                    .FromJson<ExecutionProfile>();

                string actualHashCode = profile.GetPredictableHashCode().ToString();
                Assert.AreEqual(expectedHashCode, actualHashCode);
            }
        }

        [Test]
        [Platform(Include = "64-bit")] // assumes little-endian architecture for hash code generation
        [TestCase("TEST-PROFILE-1.json", "279003058894895889018486895561353871838116508168")]
        [TestCase("TEST-PROFILE-2.json", "594761196296228877452223238967152003599764620204")]
        [TestCase("TEST-PROFILE-4.json", "922483698282800856035216844073388800671549310792")]
        [TestCase("TEST-PROFILE-5.json", "1024016195510512526363371526971708676820695027365")]
        [TestCase("TEST-PROFILE-3-PARALLEL.json", "170719133252643968401267092175966399376412682155")]
        [TestCase("TEST-PROFILE-2-PARALLEL-LOOP.json", "926161149126364949119888097719891324792221811485")]
        public void ExecutionProfileGeneratesPredictableHashCodesWhenMetadataIsIncluded(string profileName, string expectedHashCode)
        {
            // The hash should not change regardless of the number of times the profile is deserialized.
            for (int check = 0; check < 10; check++)
            {
                ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                    .FromJson<ExecutionProfile>();

                string actualHashCode = profile.GetPredictableHashCode(includeMetadata: true).ToString();
                Assert.AreEqual(expectedHashCode, actualHashCode);
            }
        }


        [Test]
        [Platform(Include = "64-bit")] // assumes little-endian architecture for hash code generation
        [TestCase("TEST-PROFILE-4.json", "1326284617660594785167171193396646697439110051452")]
        public void ExecutionProfileGeneratesPredictableHashCodesWhenMetadataIsModifiedButExcludedFromHashingMechanics(string profileName, string expectedHashCode)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            string actualHashCode = profile.GetPredictableHashCode(includeMetadata: false).ToString();
            Assert.AreEqual(expectedHashCode, actualHashCode);

            profile.Metadata[profile.Metadata.First().Key] = "ModifiedValue";

            List<ExecutionProfileElement> components = new List<ExecutionProfileElement>();
            if (profile.Actions?.Any() == true)
            {
                components.AddRange(profile.Actions);
            }

            if (profile.Dependencies?.Any() == true)
            {
                components.AddRange(profile.Dependencies);
            }

            if (profile.Monitors?.Any() == true)
            {
                components.AddRange(profile.Monitors);
            }

            if (components?.Any() == true)
            {
                foreach (var component in components)
                {
                    if (component.Metadata?.Any() == true)
                    {
                        component.Metadata[component.Metadata.First().Key] = "ModifiedValue";
                    }
                }
            }

            string modifiedHashCode = profile.GetPredictableHashCode(includeMetadata: false).ToString();
            Assert.AreEqual(expectedHashCode, modifiedHashCode);
        }
    }
}
