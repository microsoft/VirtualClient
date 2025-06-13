// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Policy;
    using AutoFixture;
    using Azure.Identity;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;
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

        ////[Test]
        ////[TestCase(
        ////    "https://any.service.azure.com?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https",
        ////    "https://any.service.azure.com/?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https")]
        //////
        ////[TestCase(
        ////    "https://any.service.azure.com/?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https",
        ////    "https://any.service.azure.com/?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https")]
        ////public void EndpointUtilityConvertsConnectionPropertiesForEndpointsUsingSasTokensToExpectedUris(string endpoint, string expectedUri)
        ////{
        ////    IDictionary<string, string> connectionProperties = new Dictionary<string, string>
        ////    {
        ////        { ConnectionParameter.Endpoint, endpoint }
        ////    };

        ////    Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
        ////    Assert.AreEqual(expectedUri, actualUri.ToString());
        ////}

        ////[Test]
        ////[TestCase("https://any.service.azure.com", "307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf")]
        ////[TestCase("https://any.service.azure.com/", "307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf")]
        ////public void EndpointUtilityConvertsConnectionPropertiesForEndpointsReferencingManagedIdentities(string endpoint, string managedIdentity, string expectedUri)
        ////{
        ////    IDictionary<string, string> connectionProperties = new Dictionary<string, string>
        ////    {
        ////        { ConnectionParameter.Endpoint, endpoint },
        ////        { ConnectionParameter.ManagedIdentityId, managedIdentity }
        ////    };

        ////    Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
        ////    Assert.AreEqual(expectedUri, actualUri.ToString());
        ////}

        ////[Test]
        ////[TestCase(
        ////    "https://any.service.azure.com",
        ////    "307591a4-abb2-4559-af59-b47177d140cf",
        ////    "985bbc17-E3A5-4fec-b0cb-40dbb8bc5959",
        ////    "1753429a8bc4f91d",
        ////    "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&crtt=1753429a8bc4f91d")]
        ////public void EndpointUtilityConvertsConnectionPropertiesForEndpointsReferencingMicrosoftEntraApps_WithCertificateThumbprint(
        ////    string endpoint, string clientId, string tenantId, string certificateThumbprint, string expectedUri)
        ////{
        ////    IDictionary<string, string> connectionProperties = new Dictionary<string, string>
        ////    {
        ////        { ConnectionParameter.Endpoint, endpoint },
        ////        { ConnectionParameter.ClientId, clientId },
        ////        { ConnectionParameter.TenantId, tenantId },
        ////        { ConnectionParameter.CertificateThumbprint, certificateThumbprint }
        ////    };

        ////    Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
        ////    Assert.AreEqual(expectedUri, actualUri.ToString());
        ////}

        ////[Test]
        ////[TestCase(
        ////   "https://any.service.azure.com",
        ////   "307591a4-abb2-4559-af59-b47177d140cf",
        ////   "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959",
        ////   "ABC",
        ////   "any.service.com",
        ////   "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com")]
        //////
        ////[TestCase(
        ////   "https://any.service.azure.com",
        ////   "307591a4-abb2-4559-af59-b47177d140cf",
        ////   "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959",
        ////   "ABC CA 01",
        ////   "any.service.com",
        ////   "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.service.com")]
        //////
        ////[TestCase(
        ////   "https://any.service.azure.com",
        ////   "307591a4-abb2-4559-af59-b47177d140cf",
        ////   "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959",
        ////   "CN=ABC CA 01, DC=ABC, DC=COM",
        ////   "CN=any.service.com",
        ////   "https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.service.com")]
        ////public void EndpointUtilityConvertsConnectionPropertiesForEndpointsReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubjectName(
        ////   string endpoint, string clientId, string tenantId, string certificateIssuer, string certificateSubject, string expectedUri)
        ////{
        ////    IDictionary<string, string> connectionProperties = new Dictionary<string, string>
        ////    {
        ////        { ConnectionParameter.Endpoint, endpoint },
        ////        { ConnectionParameter.ClientId, clientId },
        ////        { ConnectionParameter.TenantId, tenantId },
        ////        { ConnectionParameter.CertificateIssuer, certificateIssuer },
        ////        { ConnectionParameter.CertificateSubject, certificateSubject }
        ////    };

        ////    Uri actualUri = EndpointUtility.ConvertToUri(connectionProperties);
        ////    Assert.AreEqual(expectedUri, actualUri.ToString());
        ////}

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorage.blob.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForStorageAccountConnectionStrings(string connectionString)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(connectionString, store.ConnectionString);
            Assert.IsNull(store.Credentials);
        }

        [Test]
        public void EndpointUtilityThrowsWhenCreatingABlobStoreReferenceIfTheValueProvidedIsNotAValidStorageAccountConnectionStrings()
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                "Not=A;Valid=ConnectionString",
                this.mockFixture.CertificateManager.Object));
        }

        [Test]
        [TestCase(
            "packages.virtualclient.microsoft.com",
            "https://packages.virtualclient.microsoft.com/")]
        //
        [TestCase(
            "https://packages.virtualclient.microsoft.com",
            "https://packages.virtualclient.microsoft.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForCDNUri(string uri, string expectedUri)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureCDN, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNull(store.Credentials);
        }

        [Test]
        [TestCase("https://packages.virtualclient.microsoft.com")]
        public void EndpointUtilityThrowsWhenCreatingBlobStoreReferenceForCDNUriIfUriIsValidButDependencyStoreIsContent(string uri)
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Content,
                uri,
                this.mockFixture.CertificateManager.Object));
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
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNull(store.Credentials);
        }

        [Test]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net/container")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net/container/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedUri)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com?miid=307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com")]
        [TestCase("https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf","https://any.service.azure.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingManagedIdentities(string uri, string expectedUri)
        {
            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net/container")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net/container/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateThumbprints(string connectionString, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/container/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.service.azure.com/container/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
            "https://anystorage.blob.core.windows.net")]
        //
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC CA 01;CertificateSubject=any.domain.com",
            "https://anystorage.blob.core.windows.net")]
        //
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com",
            "https://anystorage.blob.core.windows.net")]
        //
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net/container;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC CA 01;CertificateSubject=any.domain.com",
            "https://anystorage.blob.core.windows.net/container")]
        //
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net/container/;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com",
            "https://anystorage.blob.core.windows.net/container/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string connectionString, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }
       
        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.domain.com", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com", "https://any.service.azure.com/")]
        public void EndpointUtilityCreatesTheExpectedBlobStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyBlobStore store = EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Packages, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("https://any.service.com?not=valid&setof=parameters")]
        [TestCase("EndpointUrl=https://any.service.com?not=valid&setof=parameters;ClientId=11223344")]
        [TestCase("EndpointUrl=https://any.service.com?not=valid&setof=parameters;ClientId=11223344;TenantId=55667788")]
        [TestCase("InvalidParameter=https://any.service.com?not=valid&setof=parameters;ManagedIdentityId=123456789")]
        public void EndpointUtilityThrowsWhenCreatingABlobStoreReferenceIfTheValueProvidedIsNotAValidEndpointUri(string invalidEndpoint)
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateBlobStoreReference(
                DependencyStore.Packages,
                invalidEndpoint,
                this.mockFixture.CertificateManager.Object));
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123")]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123;EntityPath=telemetry")]
        public void EndpointUtilityCreatesTheExpectedEventHubNamespaceStoreReferenceForAccessPolicies(string accessPolicy)
        {
            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                accessPolicy,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(accessPolicy, store.ConnectionString);
            Assert.IsNull(store.EndpointUri);
            Assert.IsNull(store.EventHubNamespace);
            Assert.IsNull(store.Credentials);
        }

        [Test]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=any.servicebus.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=sb://any.servicebus.windows.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedUri)
        {
            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net?miid=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net/")]
        [TestCase("sb://any.servicebus.windows.net/?miid=307591a4-abb2-4559-af59-b47177d140cf", "sb://any.servicebus.windows.net/")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForUrisReferencingManagedIdentities(string uri, string expectedUri)
        {
            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "sb://any.servicebus.windows.net")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "sb://any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "sb://any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "sb://any.servicebus.windows.net")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateThumbprints(string connectionString, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
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
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase(
            "EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
            "sb://any.servicebus.windows.net")]
        //
        [TestCase(
            "EndpointUrl=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC CA 01;CertificateSubject=any.domain.com",
            "sb://any.servicebus.windows.net")]
        //
        [TestCase(
            "EndpointUrl=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com",
            "sb://any.servicebus.windows.net")]
        //
        [TestCase(
            "EventHubNamespace=any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com",
            "sb://any.servicebus.windows.net")]
        //
        [TestCase(
            "EventHubNamespace=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com",
            "sb://any.servicebus.windows.net")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string connectionString, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com", "sb://any.servicebus.windows.net")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.domain.com", "sb://any.servicebus.windows.net/")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com", "sb://any.servicebus.windows.net/")]
        public void EndpointUtilityCreatesTheExpectedEventHubStoreReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyEventHubStore store = EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.Telemetry, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureEventHubNamespace, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net?not=valid&setof=parameters")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net?not=valid&setof=parameters;ClientId=11223344")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net?not=valid&setof=parameters;ClientId=11223344;TenantId=55667788")]
        [TestCase("InvalidParameter=sb://any.servicebus.windows.net?not=valid&setof=parameters;ManagedIdentityId=123456789")]
        public void EndpointUtilityThrowsWhenCreatingAnEventHubStoreReferenceIfTheValueProvidedIsNotAValidEndpointUri(string invalidEndpoint)
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateEventHubStoreReference(
                DependencyStore.Telemetry,
                invalidEndpoint,
                this.mockFixture.CertificateManager.Object));
        }

        [Test]
        [TestCase(
           "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
           "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
           "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https",
           "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https",
            "ANY-PROFILE.json")]
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForStorageAccountSasUris(string uri, string expectedUri, string expectedProfileName)
        {
            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsNull(profileReference.Credentials);
        }

        [Test]
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf",
            "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
            "EndpointUrl=https://anystorage.blob.core.windows.net/container/any/virtual/path/ANY-PROFILE.json;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf",
            "https://anystorage.blob.core.windows.net/container/any/virtual/path/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedUri, string expectedProfileName)
        {
            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsInstanceOf<ManagedIdentityCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase(
            "https://any.service.azure.com/profiles/ANY-PROFILE.json?miid=307591a4-abb2-4559-af59-b47177d140cf",
            "https://any.service.azure.com/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
            "https://any.service.azure.com/profiles/any/virtual/path/ANY-PROFILE.json?miid=307591a4-abb2-4559-af59-b47177d140cf",
            "https://any.service.azure.com/profiles/any/virtual/path/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForUrisReferencingManagedIdentities(string uri, string expectedUri, string expectedProfileName)
        {
            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsInstanceOf<ManagedIdentityCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase(
           "EndpointUrl=https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
           "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
           "ANY-PROFILE.json")]
        //
        [TestCase(
           "EndpointUrl=https://anystorage.blob.core.windows.net/container/any/virtual/path/ANY-PROFILE.json;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
           "https://anystorage.blob.core.windows.net/container/any/virtual/path/ANY-PROFILE.json",
           "ANY-PROFILE.json")]
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateThumbprints(string connectionString, string expectedUri, string expectedProfileName)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsInstanceOf<ClientCertificateCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase(
            "https://any.service.azure.com/profiles/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789",
            "https://any.service.azure.com/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
            "https://any.service.azure.com/profiles/any/virtual/path/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789",
            "https://any.service.azure.com/profiles/any/virtual/path/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedUri, string expectedProfileName)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsNotNull(profileReference.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase(
          "EndpointUrl=https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
          "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
          "ANY-PROFILE.json")]
        //
        [TestCase(
          "EndpointUrl=https://anystorage.blob.core.windows.net/container/any/virtual/path/ANY-PROFILE.json;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
          "https://anystorage.blob.core.windows.net/container/any/virtual/path/ANY-PROFILE.json",
          "ANY-PROFILE.json")]
        //
        [TestCase(
          "EndpointUrl=https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC CA 01;CertificateSubject=any.domain.com",
          "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
          "ANY-PROFILE.json")]
        //
        [TestCase(
          "EndpointUrl=https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com",
          "https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json",
          "ANY-PROFILE.json")]
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string connectionString, string expectedUri, string expectedProfileName)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsInstanceOf<ClientCertificateCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase(
            "https://any.service.azure.com/profiles/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com",
            "https://any.service.azure.com/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
            "https://any.service.azure.com/profiles/any/virtual/path/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com",
            "https://any.service.azure.com/profiles/any/virtual/path/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
            "https://any.service.azure.com/profiles/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.domain.com",
            "https://any.service.azure.com/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        [TestCase(
            "https://any.service.azure.com/profiles/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com",
            "https://any.service.azure.com/profiles/ANY-PROFILE.json",
            "ANY-PROFILE.json")]
        //
        public void EndpointUtilityCreatesTheExpectedProfileReferenceForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string uri, string expectedUri, string expectedProfileName)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            DependencyProfileReference profileReference = EndpointUtility.CreateProfileReference(
                uri,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsNotNull(profileReference.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase("https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters")]
        [TestCase("EndpointUrl=https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters;ClientId=11223344")]
        [TestCase("EndpointUrl=https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters;ClientId=11223344;TenantId=55667788")]
        [TestCase("InvalidParameter=https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters;ManagedIdentityId=123456789")]
        public void EndpointUtilityThrowsWhenCreatingAProfileReferenceIfTheValueProvidedIsNotAValidEndpointUri(string invalidEndpoint)
        {
            Assert.Throws<SchemaException>(() => EndpointUtility.CreateProfileReference(
                invalidEndpoint,
                this.mockFixture.CertificateManager.Object));
        }

        [Test]
        [TestCase(
            "https://my-keyvault.vault.azure.net/?cid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&tid=307591a4-abb2-4559-af59-b47177d140cf&crtt=1234567",
            "https://my-keyvault.vault.azure.net/")]
        [TestCase(
            "Endpoint=https://my-keyvault.vault.azure.net/;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4",
            "https://my-keyvault.vault.azure.net/")]
        public void EndpointUtility_CreateKeyVaultStoreReference_Entra(string connectionString, string expectedUri)
        {
            // Setup: A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("1234567", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            var store = EndpointUtility.CreateKeyVaultStoreReference(
                DependencyStore.KeyVault,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.KeyVault, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(store.Credentials);
        }

        [Test]
        [TestCase(
            "Endpoint=https://my-keyvault.vault.azure.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf",
            "https://my-keyvault.vault.azure.net/")]
        [TestCase(
            "https://my-keyvault.vault.azure.net/?miid=307591a4-abb2-4559-af59-b47177d140cf",
            "https://my-keyvault.vault.azure.net/")]
        public void EndpointUtility_CreateKeyVaultStoreReference_Miid(string connectionString, string expectedUri)
        {
            var store = EndpointUtility.CreateKeyVaultStoreReference(
                DependencyStore.KeyVault,
                connectionString,
                this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(store);
            Assert.AreEqual(DependencyStore.KeyVault, store.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, store.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), store.EndpointUri.ToString());
            Assert.IsNotNull(store.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(store.Credentials);
        }

        [Test]
        public void CreateKeyVaultStoreReference_ConnectionString_ThrowsOnInvalid()
        {
            Assert.Throws<SchemaException>(() =>
                EndpointUtility.CreateKeyVaultStoreReference(
                    DependencyStore.KeyVault,
                    "InvalidConnectionString",
                    this.mockFixture.CertificateManager.Object));
        }
    }
}
