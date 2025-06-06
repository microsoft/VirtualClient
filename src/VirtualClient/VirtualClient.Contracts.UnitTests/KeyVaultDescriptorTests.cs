// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using NUnit.Framework;
using VirtualClient.Contracts;
using VirtualClient.TestExtensions;

namespace VirtualClient
{
    [TestFixture]
    [Category("Unit")]
    public class KeyVaultDescriptorTests
    {
        private KeyVaultDescriptor descriptor;
        private KeyVaultDescriptor descriptor2;

        [SetUp]
        public void SetupTest()
        {
            this.descriptor = new KeyVaultDescriptor
            {
                ObjectType = KeyVaultObjectType.Secret,
                VaultUri = "https://myvault.vault.azure.net/",
                Name = "mysecret",
                Version = "v1",
                Value = "secret-value",
                ObjectId = "object-id-1",
                Policy = "policy-1"
            };

            this.descriptor2 = new KeyVaultDescriptor
            {
                ObjectType = KeyVaultObjectType.Key,
                VaultUri = "https://othervault.vault.azure.net/",
                Name = "mykey",
                Version = "v2",
                Value = "key-value",
                ObjectId = "object-id-2",
                Policy = "policy-2"
            };
        }

        [Test]
        public void KeyVaultDescriptorCorrectlyImplementsEqualitySemantics()
        {
            EqualityAssert.CorrectlyImplementsEqualitySemantics<DependencyDescriptor>(() => this.descriptor, () => this.descriptor2);
        }

        [Test]
        public void KeyVaultDescriptorEqualitySemanticsAreNotAffectedByNullPropertyValues()
        {
            this.descriptor.VaultUri = null;
            this.descriptor2.VaultUri = null;
            this.descriptor.Version = null;
            this.descriptor2.Version = null;
            this.descriptor.Value = null;
            this.descriptor2.Value = null;

            EqualityAssert.CorrectlyImplementsEqualitySemantics<DependencyDescriptor>(() => this.descriptor, () => this.descriptor2);
        }

        [Test]
        public void KeyVaultDescriptorCorrectlyImplementsHashcodeSemantics()
        {
            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.descriptor, () => this.descriptor2);
        }

        [Test]
        public void KeyVaultDescriptorHashcodeSemanticsAreNotAffectedByNullPropertyValues()
        {
            this.descriptor.VaultUri = null;
            this.descriptor2.VaultUri = null;
            this.descriptor.Value = null;
            this.descriptor2.Value = null;
            this.descriptor.ObjectId = null;
            this.descriptor2.ObjectId = null;

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.descriptor, () => this.descriptor2);
        }

        [Test]
        public void KeyVaultDescriptorPropertiesAreSetAndGetCorrectly()
        {
            var descriptor = new KeyVaultDescriptor
            {
                ObjectType = KeyVaultObjectType.Certificate,
                VaultUri = "https://vault.uri/",
                Name = "cert",
                Version = "v3",
                Value = "cert-value",
                ObjectId = "object-id-3",
                Policy = "policy-3"
            };

            Assert.AreEqual(KeyVaultObjectType.Certificate, descriptor.ObjectType);
            Assert.AreEqual("https://vault.uri/", descriptor.VaultUri);
            Assert.AreEqual("cert", descriptor.Name);
            Assert.AreEqual("v3", descriptor.Version);
            Assert.AreEqual("cert-value", descriptor.Value);
            Assert.AreEqual("object-id-3", descriptor.ObjectId);
            Assert.AreEqual("policy-3", descriptor.Policy);
        }
    }
}