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
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class DiskMapTests
    {
        private IFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
        }

        [Test]
        public void DiskMapObjectsAreJsonSerializable()
        {
            SerializationAssert.IsJsonSerializable<DiskMap>(this.mockFixture.Create<DiskMap>());
        }

        [Test]
        public void DiskMapCorrectlyImplementsEqualitySemantics()
        {
            DiskMap instance1 = this.mockFixture.Create<DiskMap>();
            DiskMap instance2 = this.mockFixture.Create<DiskMap>();

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => instance1, () => instance2);
        }

        [Test]
        public void DiskMapCorrectlyImplementsHashcodeSemantics()
        {
            DiskMap instance1 = this.mockFixture.Create<DiskMap>();
            DiskMap instance2 = this.mockFixture.Create<DiskMap>();

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => instance1, () => instance2);
        }

        [Test]
        public void DiskMapHashCodesAreNotCaseSensitive()
        {
            DiskMap template = this.mockFixture.Create<DiskMap>();
            DiskMap instance1 = new DiskMap(
                template.Id,
                template.Name.ToLowerInvariant(),
                template.Type.ToLowerInvariant());

            DiskMap instance2 = new DiskMap(
                template.Id,
                template.Name.ToUpperInvariant(),
                template.Type.ToUpperInvariant());

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }
    }
}
