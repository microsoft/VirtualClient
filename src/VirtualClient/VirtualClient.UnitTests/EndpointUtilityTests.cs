// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using AutoFixture;
    using Azure.Identity;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    internal class EndpointUtilityTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Initialize()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupCertificateMocks();
        }

        [Test]
        [TestCase(
            "https://any.service.azure.com?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https",
            "https://any.service.azure.com/?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https")]
        //
        [TestCase(
            "https://any.service.azure.com/?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https",
            "https://any.service.azure.com/?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https")]
        public void EndpointUtilityConvertsConnectionPropertiesForEndpointsUsingSasTokensToExpectedUris(string endpoint, string expectedUri)
        {
            IDictionary<string, string> connectionProperties = new Dictionary<string, string>
            {
                { ConnectionParameter.Endpoint, endpoint }
            };

            Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
            Assert.AreEqual(expectedUri, actualUri.ToString());
        }

        [Test]
        [TestCase("https://any.service.azure.com", "307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("https://any.service.azure.com/", "307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf")]
        public void EndpointUtilityConvertsConnectionPropertiesForEndpointsReferencingManagedIdentities(string endpoint, string managedIdentity, string expectedUri)
        {
            IDictionary<string, string> connectionProperties = new Dictionary<string, string>
            {
                { ConnectionParameter.Endpoint, endpoint },
                { ConnectionParameter.ManagedIdentityId, managedIdentity }
            };

            Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
            Assert.AreEqual(expectedUri, actualUri.ToString());
        }

        [Test]
        [TestCase(
            "https://any.service.azure.com",
            "307591a4-abb2-4559-af59-b47177d140cf",
            "985bbc17-E3A5-4fec-b0cb-40dbb8bc5959",
            "1753429a8bc4f91d",
            "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&crtt=1753429a8bc4f91d")]
        public void EndpointUtilityConvertsConnectionPropertiesForEndpointsReferencingMicrosoftEntraApps_WithCertificateThumbprint(
            string endpoint, string clientId, string tenantId, string certificateThumbprint, string expectedUri)
        {
            IDictionary<string, string> connectionProperties = new Dictionary<string, string>
            {
                { ConnectionParameter.Endpoint, endpoint },
                { ConnectionParameter.ClientId, clientId },
                { ConnectionParameter.TenantId, tenantId },
                { ConnectionParameter.CertificateThumbprint, certificateThumbprint }
            };

            Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
            Assert.AreEqual(expectedUri, actualUri.ToString());
        }

        [Test]
        [TestCase(
           "https://any.service.azure.com",
           "307591a4-abb2-4559-af59-b47177d140cf",
           "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959",
           "ABC",
           "any.service.com",
           "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com")]
        //
        [TestCase(
           "https://any.service.azure.com",
           "307591a4-abb2-4559-af59-b47177d140cf",
           "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959",
           "ABC CA 01",
           "any.service.com",
           "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.service.com")]
        //
        [TestCase(
           "https://any.service.azure.com",
           "307591a4-abb2-4559-af59-b47177d140cf",
           "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959",
           "CN=ABC CA 01, DC=ABC, DC=COM",
           "CN=any.service.com",
           "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.service.com")]
        public void EndpointUtilityConvertsConnectionPropertiesForEndpointsReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubjectName(
           string endpoint, string clientId, string tenantId, string certificateIssuer, string certificateSubject, string expectedUri)
        {
            IDictionary<string, string> connectionProperties = new Dictionary<string, string>
            {
                { ConnectionParameter.Endpoint, endpoint },
                { ConnectionParameter.ClientId, clientId },
                { ConnectionParameter.TenantId, tenantId },
                { ConnectionParameter.CertificateIssuer, certificateIssuer },
                { ConnectionParameter.CertificateSubject, certificateSubject }
            };

            Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
            Assert.AreEqual(expectedUri, actualUri.ToString());
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorage.blob.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForStorageAccountConnectionStrings(string connectionString)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(DependencyStore.Packages, connectionString);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(connectionString, store.ConnectionString);
            Assert.IsNull(store.Credentials);
        }

        [Test]
        public void EndpointUtilityThrowsWhenCreatingABlobStoreReferenceIfTheValueProvidedIsNotAValidStorageAccountConnectionStrings()
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateBlobStoreReference(DependencyStore.Packages, "Not=A;Valid=ConnectionString"));
        }

        [Test]
        [TestCase(
            "https://anystorage.blob.core.windows.net",
            "https://anystorage.blob.core.windows.net/")]
        //
        [TestCase(
            "https://anystorage.blob.core.windows.net?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https", 
            "https://anystorage.blob.core.windows.net/?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https")]
        //
        [TestCase(
            "https://anystorage.blob.core.windows.net/?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https",
            "https://anystorage.blob.core.windows.net/?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForStorageAccountSasUris(string uri, string expectedUri)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNull(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com?miid=307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com")]
        [TestCase("https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf","https://any.service.azure.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingManagedIdentities(string uri, string expectedUri)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.service.azure.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com", "https://any.service.azure.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject_1(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("ABC", "any.service.com", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.service.com", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.service.com", "https://any.service.azure.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject_2(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.service.com" || subject == "CN=any.service.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        public void EndpointUtilityThrowsWhenCreatingABlobStoreReferenceIfTheValueProvidedIsNotAValidEndpointUri()
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                new Uri("https://any.service.com?not=valid&setof=parameters"),
                this.mockFixture.CertificateManager.Object));
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123")]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123;EntityPath=telemetry")]
        public void EndpointUtilityCreatesTheExpectedEventHubNamespaceStoreReferenceForEventHubNamespaceForAccessPolicies(string accessPolicy)
        {
            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(DependencyStore.Telemetry, accessPolicy);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(accessPolicy, store.ConnectionString);
            Assert.IsNull(store.EndpointUri);
            Assert.IsNull(store.EventHubNamespace);
            Assert.IsNull(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net?miid=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net/")]
        [TestCase("sb://any.servicebus.windows.net/?miid=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net/")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForUrisReferencingManagedIdentities(string uri, string expectedUri)
        {
            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "sb://any.servicebus.windows.net/")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com", "sb://any.servicebus.windows.net")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject_1(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("ABC", "any.service.com", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.service.com", "sb://any.servicebus.windows.net/")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.service.com", "sb://any.servicebus.windows.net/")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject_2(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.service.com" || subject == "CN=any.service.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                new Uri(uri),
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        public void EndpointUtilityThrowsWhenCreatingAnEventHubStoreReferenceIfTheValueProvidedIsNotAValidEndpointUri()
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                new Uri("sb://any.servicebus.windows.net?not=valid&setof=parameters"),
                this.mockFixture.CertificateManager.Object));
        }
    }
}
