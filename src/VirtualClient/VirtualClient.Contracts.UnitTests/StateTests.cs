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
    using VirtualClient.Common;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class StateTests
    {
        private IFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
        }

        [Test]
        public void StateObjectsAreJsonSerializable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["property1"] = "value1",
                ["property2"] = 12345,
                ["property3"] = true
            });

            string serializedState = state.ToJson();
            State deserializedState = serializedState.FromJson<State>();

            Assert.IsTrue(state.Properties.Count == deserializedState.Properties.Count);
            CollectionAssert.AreEquivalent(state.Properties.Keys, deserializedState.Properties.Keys);
            CollectionAssert.AreEquivalent(state.Properties.Values.Select(v => v.ToString()), deserializedState.Properties.Values.Select(v => v.ToString()));
        }

        [Test]
        public void StateObjectInstancesAreJsonSerializable_2()
        {
            Item<State> stateInstance = new Item<State>(
                Guid.NewGuid().ToString(),
                new State(new Dictionary<string, IConvertible>
                {
                    ["property1"] = "value1",
                    ["property2"] = 12345,
                    ["property3"] = true
                }));

            string serializedState = stateInstance.ToJson();
            Item<State> deserializedState = serializedState.FromJson<Item<State>>();

            Assert.IsTrue(stateInstance.Definition.Properties.Count == deserializedState.Definition.Properties.Count);
            CollectionAssert.AreEquivalent(
                stateInstance.Definition.Properties.Keys,
                deserializedState.Definition.Properties.Keys);

            CollectionAssert.AreEquivalent(
                stateInstance.Definition.Properties.Values.Select(v => v.ToString()),
                deserializedState.Definition.Properties.Values.Select(v => v.ToString()));
        }


        [Test]
        public void StateObjectsCorrectlyImplementsEqualitySemantics()
        {
            State instance1 = this.mockFixture.Create<State>();
            State instance2 = this.mockFixture.Create<State>();

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => instance1, () => instance2);
        }

        [Test]
        public void StateObjectsCorrectlyImplementsHashcodeSemantics()
        {
            State instance1 = this.mockFixture.Create<State>();
            State instance2 = this.mockFixture.Create<State>();

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => instance1, () => instance2);
        }

        [Test]
        public void StateObjectsHashCodesAreNotCaseSensitive()
        {
            State template = new State(new Dictionary<string, IConvertible>
            {
                ["property1"] = "value1",
                ["property2"] = "value2",
            });

            State instance1 = new State(template.Properties);
            State instance2 = new State(template.Properties.ToDictionary(k => k.Key, k => k.Value.ToString().ToUpperInvariant() as IConvertible));
            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());

            State instance3 = new State(template.Properties.ToDictionary(k => k.Key, k => k.Value.ToString().ToLowerInvariant() as IConvertible));
            Assert.AreEqual(instance1.GetHashCode(), instance3.GetHashCode());
        }
    }
}
