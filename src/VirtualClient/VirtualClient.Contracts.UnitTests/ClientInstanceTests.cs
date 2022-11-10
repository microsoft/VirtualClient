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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class ClientInstanceTests
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
        [TestCase("  ")]
        public void ClientInstanceConstructorsValidateRequiredParameters(string invalidParameter)
        {
            Assert.Throws<ArgumentException>(() => new ClientInstance(invalidParameter, "1.1.1.1", "ValidRole"));
            Assert.Throws<ArgumentException>(() => new ClientInstance("ValidName", invalidParameter, "ValidRole"));
            Assert.Throws<FormatException>(() => new ClientInstance("ValidName", "NotAValidIP"));
        }

        [Test]
        public void ClientInstanceConstructorsSetPropertiesToExpectedValues()
        {
            string expectedName = "AnyName";
            string expectedRole = "client";
            string expectedPrivateIp = "1.2.3.4";

            ClientInstance instance = new ClientInstance(expectedName, expectedPrivateIp, expectedRole);

            Assert.AreEqual(instance.Name, expectedName);
            Assert.AreEqual(instance.Role, expectedRole);
            Assert.AreEqual(instance.PrivateIPAddress, expectedPrivateIp);
        }

        [Test]
        public void ClientInstanceObjectsAreJsonSerializable()
        {
            SerializationAssert.IsJsonSerializable<ClientInstance>(this.mockFixture.Create<ClientInstance>());
        }

        [Test]
        public void ClientInstanceCorrectlyImplementsEqualitySemantics()
        {
            ClientInstance instance1 = this.mockFixture.Create<ClientInstance>();
            ClientInstance instance2 = this.mockFixture.Create<ClientInstance>();

            EqualityAssert.CorrectlyImplementsEqualitySemantics(() => instance1, () => instance2);
        }

        [Test]
        public void ClientInstanceCorrectlyImplementsHashcodeSemantics()
        {
            ClientInstance instance1 = this.mockFixture.Create<ClientInstance>();
            ClientInstance instance2 = this.mockFixture.Create<ClientInstance>();

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => instance1, () => instance2);
        }

        [Test]
        public void ClientInstanceHashCodesAreNotCaseSensitive()
        {
            ClientInstance template = this.mockFixture.Create<ClientInstance>();
            ClientInstance instance1 = new ClientInstance(
                template.Name.ToLowerInvariant(),
                template.PrivateIPAddress.ToLowerInvariant(),
                template.Role.ToLowerInvariant());

            ClientInstance instance2 = new ClientInstance(
                template.Name.ToUpperInvariant(),
                template.PrivateIPAddress.ToUpperInvariant(),
                template.Role.ToUpperInvariant());

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }
    }
}
