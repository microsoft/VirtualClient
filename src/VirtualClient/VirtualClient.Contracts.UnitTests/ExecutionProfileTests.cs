// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Common;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
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
        [TestCase("TEST-PROFILE-3.json")]
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
    }
}
