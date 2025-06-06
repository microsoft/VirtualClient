// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NUnit.Framework;
using VirtualClient.Contracts;
using VirtualClient;

namespace VirtualClient.Core.UnitTests
{
    [TestFixture]
    [Category("Unit")]
    public class DependencyFactoryTests
    {
        [Test]
        public void CreateKeyVaultManager_ThrowsIfDependencyStoreIsNotKeyVaultStore()
        {
            // Arrange: Use a base DependencyBlobStore, not a DependencyKeyVaultStore
            var store = new DependencyBlobStore("KeyVault", new Uri("https://myblob.azure.net/"));

            // Act & Assert
            var ex = Assert.Throws<DependencyException>(() => DependencyFactory.CreateKeyVaultManager(store));
            StringAssert.Contains("Required Key Vault information not provided", ex.Message);
        }

        [Test]
        public void CreateKeyVaultManager_ReturnsKeyVaultManagerForValidStore()
        {
            // Arrange: Use a valid DependencyKeyVaultStore
            var keyVaultStore = new DependencyKeyVaultStore("KeyVault", new Uri("https://myvault.vault.azure.net/"));

            // Act
            var manager = DependencyFactory.CreateKeyVaultManager(keyVaultStore);

            // Assert
            Assert.IsNotNull(manager);
            Assert.IsInstanceOf<IKeyVaultManager>(manager);
        }

        [Test]
        public void CreateKeyVaultManager_ThrowsIfNullPassed()
        {
            // Act & Assert
            Assert.Throws<DependencyException>(() => DependencyFactory.CreateKeyVaultManager(null));
        }
    }
}