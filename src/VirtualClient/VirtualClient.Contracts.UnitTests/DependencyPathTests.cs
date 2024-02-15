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
    public class DependencyPathTests
    {
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void DependencyPathConstructorsValidateRequiredParameters(string invalidValue)
        {
            Assert.Throws<ArgumentException>(() => new DependencyPath(invalidValue, @"C:\valid\path"));
            Assert.Throws<ArgumentException>(() => new DependencyPath("validName", invalidValue));
        }

        [Test]
        public void DependencyPathConstructorsSetPropertiesToExpectedValues()
        {
            string expectedName = "anyDependency";
            string expectedPath = "/any/path/to/dependency";
            string expectedDescription = "anyDescription";

            DependencyPath dependency = new DependencyPath(expectedName, expectedPath, expectedDescription);

            Assert.AreEqual(expectedName, dependency.Name);
            Assert.AreEqual(expectedPath, dependency.Path);
            Assert.AreEqual(expectedDescription, dependency.Description);
        }


        [Test]
        public void DependencyPathIsJsonSerializable()
        {
            DependencyPath dependency = new DependencyPath("anyName", "anyPath", "anyDescription");
            SerializationAssert.IsJsonSerializable(dependency);
        }

        [Test]
        public void DependencyPathCorrectlyImplementsEqualitySemantics()
        {
            DependencyPath dependency1 = new DependencyPath("anyName1", "anyPath1", "anyDescription1");
            DependencyPath dependency2 = new DependencyPath("anyName2", "anyPath2", "anyDescription2");

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => dependency1, () => dependency2);
        }

        [Test]
        public void DependencyPathCorrectlyImplementsHashcodeSemantics()
        {
            DependencyPath dependency1 = new DependencyPath("anyName1", "anyPath1", "anyDescription1");
            DependencyPath dependency2 = new DependencyPath("anyName2", "anyPath2", "anyDescription2");

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => dependency1, () => dependency2);
        }
    }
}
