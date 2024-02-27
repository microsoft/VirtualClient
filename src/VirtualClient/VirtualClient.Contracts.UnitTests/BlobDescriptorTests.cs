// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Text;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class BlobDescriptorTests
    {
        private BlobDescriptor description;
        private BlobDescriptor description2;

        [SetUp]
        public void SetupTest()
        {
            this.description = new BlobDescriptor
            {
                Name = "AnyName",
                ContainerName = "AnyContainer",
                PackageName = "AnyPackage",
                ArchiveType = ArchiveType.Zip,
                Extract = true
            };

            this.description2 = new BlobDescriptor
            {
                Name = "AnyOtherName",
                ContainerName = "AnyOtherContainer",
                PackageName = "AnyOtherPackage",
                ArchiveType = ArchiveType.Tgz,
                Extract = true
            };
        }

        [Test]
        public void BlobDescriptorSupportsTheFullRangeOfDifferentEncodings()
        {
            this.description.ContentEncoding = Encoding.UTF8;
            Assert.AreEqual(Encoding.UTF8, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.UTF32;
            Assert.AreEqual(Encoding.UTF32, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.ASCII;
            Assert.AreEqual(Encoding.ASCII, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.BigEndianUnicode;
            Assert.AreEqual(Encoding.BigEndianUnicode, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.Unicode;
            Assert.AreEqual(Encoding.Unicode, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.Latin1;
            Assert.AreEqual(Encoding.Latin1, this.description.ContentEncoding);
        }

        [Test]
        public void BlobDescriptorCorrectlyImplementsEqualitySemantics()
        {
            EqualityAssert.CorrectlyImplementsEqualitySemantics<DependencyDescriptor>(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorEqualitySemanticsAreNotAffectedByNullPropertyValues()
        {
            this.description.PackageName = null;
            this.description2.PackageName = null;

            EqualityAssert.CorrectlyImplementsEqualitySemantics<DependencyDescriptor>(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorCorrectlyImplementsHashcodeSemantics()
        {
            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorHashcodeSemanticsAreNotAffectedByNullPropertyValues()
        {
            this.description.PackageName = null;
            this.description2.PackageName = null;

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorHashCodesAreNotCaseSensitive()
        {
            BlobDescriptor instance1 = new BlobDescriptor(this.description);
            instance1.Name = instance1.Name.ToLowerInvariant();
            instance1.PackageName = instance1.PackageName.ToLowerInvariant();

            BlobDescriptor instance2 = new BlobDescriptor(this.description);
            instance1.Name = instance1.Name.ToUpperInvariant();
            instance1.PackageName = instance1.PackageName.ToUpperInvariant();

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }
    }
}