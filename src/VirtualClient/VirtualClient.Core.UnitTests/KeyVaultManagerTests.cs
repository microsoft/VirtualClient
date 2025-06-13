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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using VirtualClient.Contracts;

namespace VirtualClient
{
    [TestFixture]
    [Category("Unit")]
    public class KeyVaultManagerTests
    {

        private Mock<SecretClient> secretClientMock;
        private Mock<KeyClient> keyClientMock;
        private Mock<CertificateClient> certificateClientMock;
        private TestKeyVaultManager keyVaultManager;

        [SetUp]
        public void SetupDefaultBehaviors()
        {
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
            var publicKeyCertificate = CertificateModelFactory.KeyVaultCertificateWithPolicy(
                properties: CertificateModelFactory.CertificateProperties(
                    id: new Uri($"https://myvault.vault.azure.net/certificates/mycert/v3"),
                    name: "mycert",
                    version: "v3",
                    vaultUri: new Uri("https://myvault.vault.azure.net/")),
                policy: CertificateModelFactory.CertificatePolicy(subject: "CN=mycert"),
                cer: this.GenerateTestCertificateBytes());
            
            this.certificateClientMock
                .Setup(c => c.GetCertificateAsync("mycert", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(publicKeyCertificate, Mock.Of<Response>()));

            this.certificateClientMock
                .Setup(c => c.DownloadCertificateAsync("mycert", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(this.GenerateTestCertificateWithPrivateKey(), Mock.Of<Response>()));

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
        public async Task KeyVaultManagerReturnsExpectedSecretValue()
        {
            var result = await this.keyVaultManager.GetSecretAsync("mysecret", CancellationToken.None, "https://myvault.vault.azure.net/", Policy.NoOpAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual("secret-value", result);
        }

        [Test]
        public async Task KeyVaultManagerReturnsExpectedSecretValue_NoVaultUriInMethod()
        {
            var result = await this.keyVaultManager.GetSecretAsync("mysecret", CancellationToken.None, retryPolicy: Policy.NoOpAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual("secret-value", result);
        }

        [Test]
        public async Task KeyVaultManagerReturnsExpectedKey()
        {
            var result = await this.keyVaultManager.GetKeyAsync("mykey", CancellationToken.None, "https://myvault.vault.azure.net/", Policy.NoOpAsync());
            Assert.IsNotNull(result);
            Assert.AreEqual("mykey", result.Name);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task KeyVaultManagerReturnsExpectedCertificate(bool retrieveWithPrivateKey)
        {
            var result = await this.keyVaultManager.GetCertificateAsync("mycert", CancellationToken.None, "https://myvault.vault.azure.net/", retrieveWithPrivateKey);
            Assert.IsNotNull(result);
            if (retrieveWithPrivateKey)
            {
                Assert.IsTrue(result.HasPrivateKey);
            }
            else
            {
                Assert.IsFalse(result.HasPrivateKey);
            }
        }

        [Test]
        public void KeyVaultManagerThrowsIfDescriptorNameIsMissing()
        {
            Assert.ThrowsAsync<ArgumentException>(() =>
                this.keyVaultManager.GetSecretAsync(null, CancellationToken.None, retryPolicy: Policy.NoOpAsync()));
        }

        [Test]
        public void KeyVaultManagerThrowsDependencyExceptionOnForbidden()
        {
            // Arrange: Setup the mock to throw a forbidden exception
            this.secretClientMock
                .Setup(c => c.GetSecretAsync("mysecret", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.Forbidden, "Forbidden"));

            var ex = Assert.ThrowsAsync<DependencyException>(() =>
                this.keyVaultManager.GetSecretAsync("mysecret", CancellationToken.None, retryPolicy: Policy.NoOpAsync()));
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
                this.keyVaultManager.GetSecretAsync("mysecret", CancellationToken.None, "https://myvault.vault.azure.net/", Policy.NoOpAsync()));
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
                this.keyVaultManager.GetSecretAsync("mysecret", CancellationToken.None, "https://myvault.vault.azure.net/", Policy.NoOpAsync()));
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
                this.keyVaultManager.GetSecretAsync("mysecret", CancellationToken.None, "https://myvault.vault.azure.net/", retryPolicy));
            Assert.AreEqual(3, attempts);
        }

        // Create a dummy, self-signed certificate for testing
        private byte[] GenerateTestCertificateBytes()
        {
            var distinguishedName = new X500DistinguishedName("CN=TestCert");

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(1));

            // Export to DER format (byte[]) – matches cert.Cer in Azure Key Vault
            return certificate.Export(X509ContentType.Cert);
        }

        private X509Certificate2 GenerateTestCertificateWithPrivateKey()
        {
            var distinguishedName = new X500DistinguishedName("CN=TestWithPrivateKey");

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(1));

            // Export and import to ensure HasPrivateKey is true in unit tests
            var bytes = certificate.Export(X509ContentType.Pfx, "");
#pragma warning disable SYSLIB0057
            // using this obsolete method just for Unit Testing
            var cert = new X509Certificate2(bytes, "", X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
            return cert;
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