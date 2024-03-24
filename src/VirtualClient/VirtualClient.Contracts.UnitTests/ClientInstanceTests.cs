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
    using VirtualClient.Common.Contracts;

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
            Assert.AreEqual(instance.IPAddress, expectedPrivateIp);
        }

        [Test]
        public void ClientInstanceObjectsAreJsonSerializable()
        {
            SerializationAssert.IsJsonSerializable<ClientInstance>(this.mockFixture.Create<ClientInstance>());
        }

        [Test]
        public void ClientInstanceObjectsAreJsonSerializable_2()
        {
            string originalSchema = "{ \"name\": \"name01\", \"ipAddress\": \"1.2.3.4\", \"role\": \"Client\" }";

            ClientInstance instance = null;
            Assert.DoesNotThrow(() => instance = originalSchema.FromJson<ClientInstance>());
            Assert.IsNotNull(instance);
            Assert.AreEqual("name01", instance.Name);
            Assert.AreEqual("1.2.3.4", instance.IPAddress);
            Assert.AreEqual("Client", instance.Role);

            string serialized = instance.ToJson();
            Assert.DoesNotThrow(() => instance = serialized.FromJson<ClientInstance>());
            Assert.IsNotNull(instance);
            Assert.AreEqual("name01", instance.Name);
            Assert.AreEqual("1.2.3.4", instance.IPAddress);
            Assert.AreEqual("Client", instance.Role);
        }

        [Test]
        public void ClientInstanceObjectsAreBackwardsCompatible()
        {
            string originalSchema = "{ \"name\": \"name01\", \"privateIPAddress\": \"1.2.3.4\", \"role\": \"Client\" }";

            ClientInstance instance = null;
            Assert.DoesNotThrow(() => instance = originalSchema.FromJson<ClientInstance>());
            Assert.IsNotNull(instance);
            Assert.AreEqual("name01", instance.Name);
            Assert.AreEqual("1.2.3.4", instance.IPAddress);
            Assert.AreEqual("Client", instance.Role);

            string serialized = instance.ToJson();
            Assert.DoesNotThrow(() => instance = serialized.FromJson<ClientInstance>());
            Assert.IsNotNull(instance);
            Assert.AreEqual("name01", instance.Name);
            Assert.AreEqual("1.2.3.4", instance.IPAddress);
            Assert.AreEqual("Client", instance.Role);
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
                template.IPAddress.ToLowerInvariant(),
                template.Role.ToLowerInvariant());

            ClientInstance instance2 = new ClientInstance(
                template.Name.ToUpperInvariant(),
                template.IPAddress.ToUpperInvariant(),
                template.Role.ToUpperInvariant());

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }
    }
}
