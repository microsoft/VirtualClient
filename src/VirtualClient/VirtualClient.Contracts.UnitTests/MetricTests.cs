// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class MetricTests
    {
        private IFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
        }

        [Test]
        public void MetricConstructorsSetPropertiesToExpectedValues()
        {
            string expectedName = "AnyName";
            double expectedValue = 1234;
            string expectedUnit = "AnyUnit";
            string expectedDescription = "Represents an important measurement.";
            MetricRelativity expectedRelativity = MetricRelativity.LowerIsBetter;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };

            Metric instance = new Metric(expectedName, expectedValue);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Value, expectedValue);
            Assert.IsNull(instance.Unit);
            Assert.IsNull(instance.Description);
            Assert.AreEqual(MetricRelativity.Undefined, instance.Relativity);
            Assert.IsNotNull(instance.Tags);
            Assert.IsEmpty(instance.Tags);

            instance = new Metric(expectedName, expectedValue, tags: expectedTags, description: expectedDescription);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Value, expectedValue);
            Assert.AreEqual(instance.Description, expectedDescription);
            Assert.IsNull(instance.Unit);
            Assert.IsNotNull(instance.Tags);
            CollectionAssert.AreEquivalent(expectedTags, instance.Tags);


            instance = new Metric(expectedName, expectedValue, expectedRelativity, tags: expectedTags, description: expectedDescription);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Value, expectedValue);
            Assert.AreEqual(instance.Description, expectedDescription);
            Assert.AreEqual(expectedRelativity, instance.Relativity);
            Assert.IsNull(instance.Unit);
            Assert.IsNotNull(instance.Tags);
            CollectionAssert.AreEquivalent(expectedTags, instance.Tags);

            instance = new Metric(expectedName, expectedValue, expectedUnit);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Value, expectedValue);
            Assert.AreEqual(instance.Unit, expectedUnit);
            Assert.IsNull(instance.Description);
            Assert.AreEqual(MetricRelativity.Undefined, instance.Relativity);
            Assert.IsNotNull(instance.Tags);
            Assert.IsEmpty(instance.Tags);

            instance = new Metric(expectedName, expectedValue, expectedUnit, tags: expectedTags, description: expectedDescription);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Value, expectedValue);
            Assert.AreEqual(instance.Unit, expectedUnit);
            Assert.AreEqual(instance.Description, expectedDescription);
            Assert.AreEqual(MetricRelativity.Undefined, instance.Relativity);
            Assert.IsNotNull(instance.Tags);
            CollectionAssert.AreEquivalent(expectedTags, instance.Tags);

            instance = new Metric(expectedName, expectedValue, expectedUnit, expectedRelativity, tags: expectedTags, description: expectedDescription);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Value, expectedValue);
            Assert.AreEqual(instance.Unit, expectedUnit);
            Assert.AreEqual(instance.Description, expectedDescription);
            Assert.AreEqual(expectedRelativity, instance.Relativity);
            Assert.IsNotNull(instance.Tags);
            CollectionAssert.AreEquivalent(expectedTags, instance.Tags);
        }

        [Test]
        public void MetricCorrectlyImplementsEqualitySemantics()
        {
            Metric instance1 = this.mockFixture.Create<Metric>();
            Metric instance2 = this.mockFixture.Create<Metric>();

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => instance1, () => instance2);
        }

        [Test]
        public void MetricCorrectlyImplementsHashcodeSemantics()
        {
            Metric instance1 = this.mockFixture.Create<Metric>();
            Metric instance2 = this.mockFixture.Create<Metric>();

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => instance1, () => instance2);
        }

        [Test]
        public void MetricHashCodesAreNotCaseSensitive()
        {
            Metric template = this.mockFixture.Create<Metric>();
            Metric instance1 = new Metric(
                template.Name.ToLowerInvariant(),
                template.Value,
                template.Unit.ToLowerInvariant());

            Metric instance2 = new Metric(
               template.Name.ToUpperInvariant(),
               template.Value,
               template.Unit.ToUpperInvariant());

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }
    }
}
