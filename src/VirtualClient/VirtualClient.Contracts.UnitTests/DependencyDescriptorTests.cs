// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class DependencyDescriptorTests
    {
        private DependencyDescriptor description;
        private DependencyDescriptor description2;

        [SetUp]
        public void SetupTest()
        {
            this.description = new DependencyDescriptor
            {
                Name = "AnyName",
                PackageName = "AnyPackage",
                ArchiveType = ArchiveType.Zip,
                Extract = true
            };

            this.description2 = new DependencyDescriptor
            {
                Name = "AnyOtherName",
                PackageName = "AnyOtherPackage",
                ArchiveType = ArchiveType.Tgz,
                Extract = true
            };
        }

        [Test]
        public void DependencyDescriptorCorrectlyImplementsEqualitySemantics()
        {
            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void DependencyDescriptorEqualitySemanticsAreNotAffectedByNullPropertyValues()
        {
            this.description.PackageName = null;
            this.description2.PackageName = null;

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void DependencyDescriptorCorrectlyImplementsHashcodeSemantics()
        {
            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void DependencyDescriptorHashcodeSemanticsAreNotAffectedByNullPropertyValues()
        {
            this.description.PackageName = null;
            this.description2.PackageName = null;

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void DependencyDescriptorHashCodesAreNotCaseSensitive()
        {
            DependencyDescriptor instance1 = new DependencyDescriptor(this.description);
            instance1.Name = instance1.Name.ToLowerInvariant();
            instance1.PackageName = instance1.PackageName.ToLowerInvariant();

            DependencyDescriptor instance2 = new DependencyDescriptor(this.description);
            instance1.Name = instance1.Name.ToUpperInvariant();
            instance1.PackageName = instance1.PackageName.ToUpperInvariant();

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }
    }
}