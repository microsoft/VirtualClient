// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class DiskTests
    {
        private IFixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new Fixture().SetupMocks(true);
        }

        [Test]
        public void DiskObjectsAreJsonSerializable()
        {
            SerializationAssert.IsJsonSerializable<Disk>(this.fixture.Create<Disk>());
        }
    }
}
