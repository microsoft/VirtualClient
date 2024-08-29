// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class InstructionsTests
    {
        private IFixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new Fixture().SetupMocks(true);
        }

        [Test]
        public void InstructionsObjectsAreJsonSerializable()
        {
            Instructions instructions = new Instructions(InstructionsType.Profiling, new Dictionary<string, IConvertible>
            {
                ["property1"] = "value1",
                ["property2"] = 12345,
                ["property3"] = true
            });

            string serializedInstructions = instructions.ToJson();
            Instructions deserializedInstructions = serializedInstructions.FromJson<Instructions>();

            Assert.AreEqual(instructions.Type, deserializedInstructions.Type);
            Assert.IsTrue(instructions.Properties.Count == deserializedInstructions.Properties.Count);
            CollectionAssert.AreEquivalent(instructions.Properties.Keys, deserializedInstructions.Properties.Keys);
            CollectionAssert.AreEquivalent(instructions.Properties.Values.Select(v => v.ToString()), deserializedInstructions.Properties.Values.Select(v => v.ToString()));
        }

        [Test]
        public void InstructionsObjectInstancesAreJsonSerializable_2()
        {
            Item<Instructions> instructions = new Item<Instructions>(
                Guid.NewGuid().ToString(),
                new Instructions(InstructionsType.Profiling, new Dictionary<string, IConvertible>
                {
                    ["property1"] = "value1",
                    ["property2"] = 12345,
                    ["property3"] = true
                }));

            string serializedInstructions = instructions.ToJson();
            Item<Instructions> deserializedInstructions = serializedInstructions.FromJson<Item<Instructions>>();

            Assert.AreEqual(instructions.Id, deserializedInstructions.Id);
            Assert.AreEqual(instructions.Definition.Type, deserializedInstructions.Definition.Type);
            Assert.IsTrue(instructions.Definition.Properties.Count == deserializedInstructions.Definition.Properties.Count);
            CollectionAssert.AreEquivalent(instructions.Definition.Properties.Keys, deserializedInstructions.Definition.Properties.Keys);
            CollectionAssert.AreEquivalent(instructions.Definition.Properties.Values.Select(v => v.ToString()), deserializedInstructions.Definition.Properties.Values.Select(v => v.ToString()));
        }

        [Test]
        public void InstructionsObjectsCorrectlyImplementsEqualitySemantics()
        {
            Instructions instance1 = this.fixture.Create<Instructions>();
            Instructions instance2 = this.fixture.Create<Instructions>();

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => instance1, () => instance2);
        }

        [Test]
        public void InstructionsObjectsCorrectlyImplementsHashcodeSemantics()
        {
            Instructions instance1 = this.fixture.Create<Instructions>();
            Instructions instance2 = this.fixture.Create<Instructions>();

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => instance1, () => instance2);
        }

        [Test]
        public void InstructionsObjectsHashCodesAreNotCaseSensitive()
        {
            Instructions template = new Instructions(InstructionsType.Profiling, new Dictionary<string, IConvertible>
            {
                ["property1"] = "value1",
                ["property2"] = "value2",
            });

            Instructions instance1 = new Instructions(template.Type, template.Properties);
            Instructions instance2 = new Instructions(template.Type, template.Properties.ToDictionary(k => k.Key, k => k.Value.ToString().ToUpperInvariant() as IConvertible));
            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());

            Instructions instance3 = new Instructions(template.Type, template.Properties.ToDictionary(k => k.Key, k => k.Value.ToString().ToLowerInvariant() as IConvertible));
            Assert.AreEqual(instance1.GetHashCode(), instance3.GetHashCode());
        }
    }
}
