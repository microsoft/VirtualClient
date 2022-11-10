// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class DependencyMetadataTests
    {
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void DependencyMetadataConstructorsValidateRequiredParameters(string invalidValue)
        {
            Assert.Throws<ArgumentException>(() => new DependencyMetadata(invalidValue));
        }

        [Test]
        public void DependencyMetadataConstructorsSetPropertiesToExpectedValues()
        {
            string expectedName = "anyDependency";
            string expectedVersion = "1.0.0";
            string expectedDescription = "anyDescription";
            IDictionary<string, IConvertible> expectedSpecifics = new Dictionary<string, IConvertible>
            {
                { "InstallerPath", @"install\installer.exe" }
            };

            DependencyMetadata dependency = new DependencyMetadata(expectedName);

            Assert.AreEqual(expectedName, dependency.Name);
            Assert.IsNull(dependency.Description);
            Assert.IsNull(dependency.Version);
            Assert.IsNotNull(dependency.Metadata);
            Assert.IsEmpty(dependency.Metadata);

            dependency = new DependencyMetadata(expectedName, expectedDescription, expectedVersion, expectedSpecifics);

            Assert.AreEqual(expectedName, dependency.Name);
            Assert.AreEqual(expectedDescription, dependency.Description);
            Assert.AreEqual(expectedVersion, dependency.Version);
            Assert.IsNotNull(dependency.Metadata);
            CollectionAssert.AreEquivalent(
                expectedSpecifics.Select(entry => $"{entry.Key}={entry.Value}"),
                dependency.Metadata.Select(entry => $"{entry.Key}={entry.Value}"));
        }


        [Test]
        public void DependencyMetadataIsJsonSerializable()
        {
            DependencyMetadata dependency = new DependencyMetadata("anyName", "anyDescription", "anyVersion", new Dictionary<string, IConvertible>
            {
                { "Property1", "Value1" },
                { "Property2", 12345 },
                { "Property3", true }
            });

            SerializationAssert.IsJsonSerializable(dependency);
        }

        [Test]
        public void DependencyMetadataCorrectlyImplementsEqualitySemantics()
        {
            DependencyMetadata dependency1 = new DependencyMetadata("anyName1", "anyDescription1", "anyVersion1", new Dictionary<string, IConvertible>
            {
                { "Property1", "Value1" },
                { "Property2", 12345 },
                { "Property3", true }
            });

            DependencyMetadata dependency2 = new DependencyMetadata("anyName2", "anyDescription2", "anyVersion2", new Dictionary<string, IConvertible>
            {
                { "Property4", "Value1" },
                { "Property5", 12345 },
                { "Property6", true }
            });

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => dependency1, () => dependency2);
        }

        [Test]
        public void DependencyMetadataCorrectlyImplementsHashcodeSemantics()
        {
            DependencyMetadata dependency1 = new DependencyMetadata("anyName1", "anyDescription1", "anyVersion1", new Dictionary<string, IConvertible>
            {
                { "Property1", "Value1" },
                { "Property2", 12345 },
                { "Property3", true }
            });

            DependencyMetadata dependency2 = new DependencyMetadata("anyName2", "anyDescription2", "anyVersion2", new Dictionary<string, IConvertible>
            {
                { "Property4", "Value1" },
                { "Property5", 12345 },
                { "Property6", true }
            });

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => dependency1, () => dependency2);
        }
    }
}
