// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Moq;
using NUnit.Framework;
using Polly;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VirtualClient.Contracts;

namespace VirtualClient
{
    [TestFixture]
    [Category("Unit")]
    public class KeyVaultManagerTests
    {
        private KeyVaultDescriptor mockDescriptor;
        private Mock<SecretClient> secretClientMock;
        private Mock<KeyClient> keyClientMock;
        private Mock<CertificateClient> certificateClientMock;
        private TestKeyVaultManager keyVaultManager;

        [SetUp]
        public void SetupDefaultBehaviors()
        {
            this.mockDescriptor = new KeyVaultDescriptor
            {
                Name = "mysecret",
                VaultUri = "https://myvault.vault.azure.net/"
            };

            // Mock the secret
            this.secretClientMock = new Mock<SecretClient>(MockBehavior.Strict, new Uri("https://myvault.vault.azure.net/"), new MockTokenCredential());
            var secret = SecretModelFactory.KeyVaultSecret(properties: SecretModelFactory.SecretProperties(
                name: "mysecret", version: "v1", vaultUri: new Uri("https://myvault.vault.azure.net/"), id: new Uri("https://myvault.vault.azure.net/")), "secret-value");

            this.secretClientMock
                .Setup(c => c.GetSecretAsync("mysecret", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(secret, Mock.Of<Response>()));

            // Mock the key
            this.keyClientMock = new Mock<KeyClient>(MockBehavior.Strict, new Uri("https://myvault.vault.azure.net/"), new MockTokenCredential());
            var key = KeyModelFactory.KeyVaultKey(properties: KeyModelFactory.KeyProperties(
                    id: new Uri($"https://myvault.vault.azure.net/keys/mykey/v2"),
                    name: "mykey",
                    version: "v2"
                ), new JsonWebKey(null));

            this.keyClientMock
                .Setup(c => c.GetKeyAsync("mykey", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(key, Mock.Of<Response>()));

            // Mock the certificates
            this.certificateClientMock = new Mock<CertificateClient>(MockBehavior.Strict, new Uri("https://myvault.vault.azure.net/"), new MockTokenCredential());
            var certificate = CertificateModelFactory.KeyVaultCertificateWithPolicy(
                properties: CertificateModelFactory.CertificateProperties(
                    id: new Uri($"https://myvault.vault.azure.net/certificates/mycert/v3"),
                    name: "mycert",
                    version: "v3",
                    vaultUri: new Uri("https://myvault.vault.azure.net/")),
                policy: CertificateModelFactory.CertificatePolicy(subject: "CN=mycert"));
            
            this.certificateClientMock
                .Setup(c => c.GetCertificateAsync("mycert", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(certificate, Mock.Of<Response>()));

            // Initialize the KeyVaultManager with the mocked clients
            this.keyVaultManager = new TestKeyVaultManager(
                new DependencyKeyVaultStore(
                    DependencyStore.KeyVault,
                    new Uri("https://myvault.vault.azure.net/"),
                    new MockTokenCredential()),
                this.secretClientMock.Object,
                this.keyClientMock.Object,
                this.certificateClientMock.Object);
        }

        [Test]
        public void KeyVaultManagerConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => new KeyVaultManager(null));
        }

        [Test]
        public async Task KeyVaultManagerReturnsExpectedSecretDescriptor()
        {
            var result = await this.keyVaultManager.GetSecretAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual("mysecret", result.Name);
            Assert.AreEqual("secret-value", result.Value);
            Assert.AreEqual("v1", result.Version);
            Assert.AreEqual(KeyVaultObjectType.Secret, result.ObjectType);
        }

        [Test]
        public async Task KeyVaultManagerReturnsExpectedKeyDescriptor()
        {
            this.mockDescriptor.Name = "mykey";
            var result = await this.keyVaultManager.GetKeyAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual("mykey", result.Name);
            Assert.AreEqual("v2", result.Version);
            Assert.AreEqual(KeyVaultObjectType.Key, result.ObjectType);
        }

        [Test]
        public async Task KeyVaultManagerReturnsExpectedCertificateDescriptor()
        {
            this.mockDescriptor.Name = "mycert";
            var result = await this.keyVaultManager.GetCertificateAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual("mycert", result.Name);
            Assert.AreEqual("v3", result.Version);
            Assert.AreEqual(KeyVaultObjectType.Certificate, result.ObjectType);
        }

        [Test]
        public void KeyVaultManagerThrowsIfDescriptorNameIsMissing()
        {
            this.mockDescriptor.Name = null;
            Assert.ThrowsAsync<DependencyException>(() =>
                this.keyVaultManager.GetSecretAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync()));
        }

        [Test]
        public void KeyVaultManagerThrowsDependencyExceptionOnForbidden()
        {
            // Arrange: Setup the mock to throw a forbidden exception
            this.secretClientMock
                .Setup(c => c.GetSecretAsync("mysecret", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.Forbidden, "Forbidden"));

            var ex = Assert.ThrowsAsync<DependencyException>(() =>
                this.keyVaultManager.GetSecretAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync()));
            Assert.AreEqual(ErrorReason.Http403ForbiddenResponse, ex.Reason);
        }

        [Test]
        public void KeyVaultManagerThrowsDependencyExceptionOnNotFound()
        {
            // Arrange: Setup the mock to throw a forbidden exception
            this.secretClientMock
                .Setup(c => c.GetSecretAsync("mysecret", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Not Found"));

            var ex = Assert.ThrowsAsync<DependencyException>(() =>
                this.keyVaultManager.GetSecretAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync()));
            Assert.AreEqual(ErrorReason.Http404NotFoundResponse, ex.Reason);
        }

        [Test]
        public void KeyVaultManagerThrowsDependencyExceptionOnOtherRequestFailed()
        {
            // Arrange: Setup the mock to throw a forbidden exception
            this.secretClientMock
                .Setup(c => c.GetSecretAsync("mysecret", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.BadRequest, "Bad Request"));

            var ex = Assert.ThrowsAsync<DependencyException>(() =>
                this.keyVaultManager.GetSecretAsync(this.mockDescriptor, CancellationToken.None, Policy.NoOpAsync()));
            Assert.AreEqual(ErrorReason.HttpNonSuccessResponse, ex.Reason);
        }

        [Test]
        public void KeyVaultManagerAppliesRetryPolicyOnTransientErrors()
        {
            int attempts = 0;

            // Arrange: Setup the mock to throw a forbidden exception
            this.secretClientMock
                .Setup(c => c.GetSecretAsync("mysecret", null, It.IsAny<CancellationToken>()))
                .Callback(() => attempts++)
                .ThrowsAsync(new RequestFailedException("Transient Error"));

            var retryPolicy = Policy.Handle<RequestFailedException>().RetryAsync(2);

            Assert.ThrowsAsync<DependencyException>(() =>
                this.keyVaultManager.GetSecretAsync(this.mockDescriptor, CancellationToken.None, retryPolicy));
            Assert.AreEqual(3, attempts);
        }

        private class TestKeyVaultManager : KeyVaultManager
        {
            private readonly SecretClient secretClient;
            private readonly KeyClient keyClient;
            private readonly CertificateClient certificateClient;

            public TestKeyVaultManager(DependencyKeyVaultStore storeDescription, SecretClient secretClient, KeyClient keyClient, CertificateClient certificateClient)
                : base(storeDescription)
            {
                this.secretClient = secretClient;
                this.keyClient = keyClient;
                this.certificateClient = certificateClient;
            }

            protected override SecretClient CreateSecretClient(Uri vaultUri, TokenCredential credential)
            {
                return this.secretClient;
            }

            protected override KeyClient CreateKeyClient(Uri vaultUri, TokenCredential credential)
            {
                return this.keyClient;
            }

            protected override CertificateClient CreateCertificateClient(Uri vaultUri, TokenCredential credential)
            {
                return this.certificateClient;
            }
        }

        private class MockTokenCredential : TokenCredential
        {
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => default;
            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => default;
        }
    }
}