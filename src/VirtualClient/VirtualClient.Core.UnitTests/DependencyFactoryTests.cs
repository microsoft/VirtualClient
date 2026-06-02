// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Core.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using AutoFixture;
    using Azure.Identity;
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Configuration;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class DependencyFactoryTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Initialize()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupCertificateMocks();
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorage.blob.core.windows.net/;SharedAccessSignature=sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForStorageAccountConnectionStrings(string connectionString)
        {
            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages, 
                connectionString, 
                this.mockFixture.CertificateManager.Object, 
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(connectionString, actualStoreDescription.ConnectionString);
            Assert.IsNull(actualStoreDescription.Credentials);
            Assert.IsNull(actualStoreDescription.EndpointUri);
        }

        [Test]
        public void DependencyFactoryThrowsWhenAnInvalidStorageAccountConnectionStringIsProvidedToCreateABlobManager()
        {
            Assert.Throws<SchemaException>(() => DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                "Not=A;Valid=ConnectionString",
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics));
        }

        [Test]
        [TestCase("packages.virtualclient.microsoft.com", "https://packages.virtualclient.microsoft.com/")]
        [TestCase("https://packages.virtualclient.microsoft.com", "https://packages.virtualclient.microsoft.com/")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForThePublicCDNEndpoint(string uri, string expectedUri)
        {
            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureCDN, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNull(actualStoreDescription.Credentials);
            Assert.IsNull(actualStoreDescription.ConnectionString);
        }

        [Test]
        [TestCase("https://packages.virtualclient.microsoft.com")]
        public void DependencyFactoryThrowsIfThePublicCDNEndpointIsUsedAsAContentStoreEndpointWhenCreatingABlobManager(string uri)
        {
            Assert.Throws<NotSupportedException>(() => DependencyFactory.CreateBlobManager(
                DependencyStore.Content,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics));
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
        public void DependencyFactoryCreatesTheExpectedBlobManagerForStorageAccountSasUris(string uri, string expectedUri)
        {
            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNull(actualStoreDescription.Credentials);
            Assert.IsNull(actualStoreDescription.ConnectionString);
        }

        [Test]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net/container")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "https://anystorage.blob.core.windows.net/container/")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedUri)
        {
            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com?miid=307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com")]
        [TestCase("https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf", "https://any.service.azure.com/")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForUrisReferencingManagedIdentities(string uri, string expectedUri)
        {
            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net/container")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net/container/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://anystorage.blob.core.windows.net/container/")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateThumbprints(string connectionString, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/container/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.service.azure.com/container/")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedUri)
        {
            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
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
        public void DependencyFactoryCreatesTheExpectedBlobManagerForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string connectionString, string expectedUri)
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

            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                connectionString,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.domain.com", "https://any.service.azure.com/")]
        [TestCase("https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com", "https://any.service.azure.com/")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string uri, string expectedUri)
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

            IBlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureStorageBlob, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase("https://any.azure-api.net", "https://any.azure-api.net/")]
        [TestCase("https://any.azure-api.net:9876", "https://any.azure-api.net:9876/")]
        [TestCase("https://any.azure-api.net?any=query&string=parameters", "https://any.azure-api.net?any=query&string=parameters")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForApiManagementEndpoints(string uri, string expectedUri)
        {
            // Setup:
            // API management subscription key is defined.
            string expectedSubscriptionKey = Guid.NewGuid().ToString();
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add(EnvironmentVariable.VC_APIM_SUBSCRIPTION_KEY, expectedSubscriptionKey);

            BlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics) as BlobManager;

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeProxy, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNull(actualStoreDescription.Credentials);
            Assert.IsNotNull(blobManager.RestClient);
            Assert.IsNotEmpty(blobManager.RestClient.DefaultRequestHeaders);
            Assert.AreEqual(blobManager.RestClient.DefaultRequestHeaders.GetValues(RequestHeader.OcpApimSubscriptionKey).FirstOrDefault(), expectedSubscriptionKey);
        }

        [Test]
        [TestCase("https://any.api.proxy", "https://any.api.proxy/")]
        [TestCase("https://any.api.proxy:9876", "https://any.api.proxy:9876/")]
        [TestCase("https://any.api.proxy?any=query&string=parameters", "https://any.api.proxy?any=query&string=parameters")]
        public void DependencyFactoryCreatesTheExpectedBlobManagerForWebProxyEndpoints(string uri, string expectedUri)
        {
            BlobManager blobManager = DependencyFactory.CreateBlobManager(
                DependencyStore.Packages,
                uri,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics) as BlobManager;

            DependencyBlobStore actualStoreDescription = blobManager.StoreDescription as DependencyBlobStore;

            Assert.IsNotNull(blobManager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.Packages, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeProxy, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNull(actualStoreDescription.Credentials);
            Assert.IsNotNull(blobManager.RestClient);
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123", "any.servicebus.windows.net")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForStandardAccessPolicies(string accessPolicy, string expectedEventHubNamespace)
        {
            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                accessPolicy,
                settings.TracesHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.TracesHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "any.servicebus.windows.net")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=any.servicebus.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=sb://any.servicebus.windows.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf", "any.servicebus.windows.net")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedEventHubNamespace)
        {
            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                connectionString,
                settings.EventsHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.EventsHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net?miid=307591a4-abb2-4559-af59-b47177d140cf", "any.servicebus.windows.net")]
        [TestCase("sb://any.servicebus.windows.net/?miid=307591a4-abb2-4559-af59-b47177d140cf", "any.servicebus.windows.net")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForUrisReferencingManagedIdentities(string uri, string expectedEventHubNamespace)
        {
            // Note that we don't have access to the TokenCredential in the underlying AMQP client/EventHubProducerClient. The token credential
            // usage must be confirmed manually.

            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                uri,
                settings.MetricsHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.MetricsHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "any.servicebus.windows.net")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "any.servicebus.windows.net")]
        [TestCase("EventHubNamespace=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "any.servicebus.windows.net")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateThumbprints(string connectionString, string expectedEventHubNamespace)
        {
            // Note that we don't have access to the TokenCredential in the underlying AMQP client/EventHubProducerClient. The token credential
            // usage must be confirmed manually.

            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                connectionString,
                settings.TracesHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.TracesHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "any.servicebus.windows.net")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedEventHubNamespace)
        {
            // Note that we don't have access to the TokenCredential in the underlying AMQP client/EventHubProducerClient. The token credential
            // usage must be confirmed manually.

            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                uri,
                settings.MetricsHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.MetricsHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com", "any.servicebus.windows.net")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC CA 01&crts=any.domain.com", "any.servicebus.windows.net")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com", "any.servicebus.windows.net")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForUrisReferencingMicrosoftEntraApps_WithCertificateIssuerAndSubject(string uri, string expectedEventHubNamespace)
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

            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                uri,
                settings.MetricsHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.MetricsHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("https://any.azure-api.net", "https://any.azure-api.net/any-hub")]
        [TestCase("https://any.azure-api.net:9876", "https://any.azure-api.net:9876/any-hub")]
        [TestCase("https://any.azure-api.net/", "https://any.azure-api.net/any-hub")]
        [TestCase("https://any.azure-api.net:9876/", "https://any.azure-api.net:9876/any-hub")]
        [TestCase("https://any.azure-api.net?any=query&string=parameters", "https://any.azure-api.net/any-hub?any=query&string=parameters")]
        [TestCase("https://any.azure-api.net:9876?any=query&string=parameters", "https://any.azure-api.net:9876/any-hub?any=query&string=parameters")]
        [TestCase("https://any.azure-api.net/?any=query&string=parameters", "https://any.azure-api.net/any-hub?any=query&string=parameters")]
        [TestCase("https://any.azure-api.net:9876/?any=query&string=parameters", "https://any.azure-api.net:9876/any-hub?any=query&string=parameters")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForApiManagementEndpoints(string uri, string expectedBaseUri)
        {
            // Setup:
            // API management subscription key is defined.
            string expectedSubscriptionKey = Guid.NewGuid().ToString();
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add(EnvironmentVariable.VC_APIM_SUBSCRIPTION_KEY, expectedSubscriptionKey);

            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                uri,
                "any-hub",
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: HTTP/REST client is used.
            Assert.IsTrue(telemetryChannel.RestClient != null);
            Assert.IsTrue(telemetryChannel.AmqpClient == null);
            Assert.IsNotNull(telemetryChannel.RestClient.BaseAddress);
            Assert.AreEqual(expectedBaseUri, telemetryChannel.RestClient.BaseAddress.ToString());
            Assert.IsNotEmpty(telemetryChannel.RestClient.DefaultRequestHeaders);
            Assert.AreEqual(telemetryChannel.RestClient.DefaultRequestHeaders.GetValues(RequestHeader.OcpApimSubscriptionKey).FirstOrDefault(), expectedSubscriptionKey);
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123", "https://any.proxy.net")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://any.azure-api.net:9876")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.azure-api.net?any=query&string=parameters")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com", "https://any.azure-api.net?any=query&string=parameters")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForWebProxyEndpoints(string endpoint, string expectedProxyUri)
        {
            string expectedEventHubNamespace = "any.servicebus.windows.net";

            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            // Note that we cannot confirm the web proxy in the underlying AMQP client because it is not
            // exposed. The settings must be confirmed manually.
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add(EnvironmentVariable.VC_EVENT_HUB_PROXY, expectedProxyUri);

            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                endpoint,
                settings.MetricsHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used. Web proxy set on the AMQP client.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.MetricsHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123", "https://any.proxy.net")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789", "https://any.azure-api.net:9876")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789", "https://any.azure-api.net?any=query&string=parameters")]
        [TestCase("sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com", "https://any.azure-api.net?any=query&string=parameters")]
        public void DependencyFactoryCreatesTheExpectedEventHubTelemetryChannelForWebProxyEndpoints_Username_Password_Credentials(string endpoint, string expectedProxyUri)
        {
            string expectedEventHubNamespace = "any.servicebus.windows.net";

            // Setup:
            // A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("123456789", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            // Note that we cannot confirm the web proxy in the underlying AMQP client because it is not
            // exposed. The settings must be confirmed manually.
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add(EnvironmentVariable.VC_EVENT_HUB_PROXY, expectedProxyUri);
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add(EnvironmentVariable.VC_EVENT_HUB_PROXY_USERNAME, "any-username");
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add(EnvironmentVariable.VC_EVENT_HUB_PROXY_PASSWORD, "any-password");

            EventHubLogSettings settings = EventHubLogSettings.Default();
            EventHubTelemetryChannel telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                endpoint,
                settings.MetricsHubName,
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics);

            Assert.IsNotNull(telemetryChannel);

            // Expect: AMQP client is used. Web proxy set on the AMQP client.
            Assert.IsTrue(telemetryChannel.AmqpClient != null);
            Assert.IsTrue(telemetryChannel.RestClient == null);

            // Expect: Event Hub name matches expected
            Assert.AreEqual(settings.MetricsHubName, telemetryChannel.AmqpClient.EventHubName);

            // Expect: Event Hub namespace matches expected
            Assert.AreEqual(expectedEventHubNamespace, telemetryChannel.AmqpClient.FullyQualifiedNamespace);
        }

        [Test]
        [TestCase("sb://any.servicebus.windows.net?not=valid&setof=parameters")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net?not=valid&setof=parameters;ClientId=11223344")]
        [TestCase("EndpointUrl=sb://any.servicebus.windows.net?not=valid&setof=parameters;ClientId=11223344;TenantId=55667788")]
        [TestCase("InvalidParameter=sb://any.servicebus.windows.net?not=valid&setof=parameters;ManagedIdentityId=123456789")]
        public void DependencyFactoryThrowsWhenCreatingEventHubTelemetryChannelsIfTheValueProvidedIsNotAValidEndpoint(string invalidEndpoint)
        {
            Assert.Throws<SchemaException>(() => DependencyFactory.CreateEventHubTelemetryChannel(
                invalidEndpoint,
                "any-eventhub",
                this.mockFixture.CertificateManager.Object,
                this.mockFixture.PlatformSpecifics));
        }

        [Test]
        [TestCase("https://my-keyvault.vault.azure.net", "https://my-keyvault.vault.azure.net/")]
        [TestCase("https://my-keyvault.vault.azure.net/", "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForBasicUris(string uri, string expectedUri)
        {
            var manager = DependencyFactory.CreateKeyVaultManager(uri, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;

            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNull(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase(
          "Endpoint=https://my-keyvault.vault.azure.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf",
          "https://my-keyvault.vault.azure.net/")]
        //
        [TestCase(
          "EndpointUrl=https://my-keyvault.vault.azure.net/;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf",
          "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedUri)
        {
            var manager = DependencyFactory.CreateKeyVaultManager(connectionString, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;

            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase(
           "Endpoint=https://my-keyvault.vault.azure.net/;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4",
           "https://my-keyvault.vault.azure.net/")]
        //
        [TestCase(
           "EndpointUrl=https://my-keyvault.vault.azure.net/;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4",
           "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateThumbprints(string connectionString, string expectedUri)
        {
            // Setup: A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("1234567", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            var manager = DependencyFactory.CreateKeyVaultManager(connectionString, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;
            
            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase(
            "Endpoint=https://my-keyvault.vault.azure.net/;ClientId=985bbc17;TenantId=307591a4;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
            "https://my-keyvault.vault.azure.net/")]
        //
        [TestCase(
            "EndpointUrl=https://my-keyvault.vault.azure.net/;ClientId=985bbc17;TenantId=307591a4;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
            "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForConnectionStringsReferencingMicrosoftEntraApps_WithCertificateSubjectNameAndIssuer(string connectionString, string expectedUri)
        {
            // Setup: A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            var manager = DependencyFactory.CreateKeyVaultManager(connectionString, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;

            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase(
          "https://my-keyvault.vault.azure.net/?miid=307591a4-abb2-4559-af59-b47177d140cf",
          "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForUrisReferencingManagedIdentities(string uri, string expectedUri)
        {
            var manager = DependencyFactory.CreateKeyVaultManager(uri, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;

            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ManagedIdentityCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase(
            "https://my-keyvault.vault.azure.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=1234567",
            "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForUrisReferencingMicrosoftEntraApps_WithCertificateThumbprints(string uri, string expectedUri)
        {
            // Setup: A matching certificate is found in the local store.
            this.mockFixture.CertificateManager.Setup(mgr => mgr.GetCertificateFromStoreAsync("1234567", It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            var manager = DependencyFactory.CreateKeyVaultManager(uri, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;

            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase(
            "https://my-keyvault.vault.azure.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com",
            "https://my-keyvault.vault.azure.net/")]
        public void DependencyFactoryCreatesTheExpectedKeyVaultManagerForUrisReferencingMicrosoftEntraApps_WithCertificateSubjectNameAndIssuer(string uri, string expectedUri)
        {
            // Setup: A matching certificate is found in the local store.
            this.mockFixture.CertificateManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(this.mockFixture.Create<X509Certificate2>());

            var manager = DependencyFactory.CreateKeyVaultManager(uri, this.mockFixture.CertificateManager.Object) as KeyVaultManager;
            DependencyKeyVaultStore actualStoreDescription = manager.StoreDescription as DependencyKeyVaultStore;

            Assert.IsNotNull(manager);
            Assert.IsNotNull(actualStoreDescription);
            Assert.AreEqual(DependencyStore.KeyVault, actualStoreDescription.StoreName);
            Assert.AreEqual(DependencyStore.StoreTypeAzureKeyVault, actualStoreDescription.StoreType);
            Assert.AreEqual(new Uri(expectedUri).ToString(), actualStoreDescription.EndpointUri.ToString());
            Assert.IsNotNull(actualStoreDescription.Credentials);
            Assert.IsInstanceOf<ClientCertificateCredential>(actualStoreDescription.Credentials);
        }

        [Test]
        [TestCase("https://myblob.azure.net/")]
        [TestCase("https://myblob.azure.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.domain.com")]
        public void DependencyFactoryThrowsIfAnInvalidUriIsProvidedWhenCreatingAKeyVaultManager(string invalidUri)
        {
            Assert.Throws<SchemaException>(() => 
                DependencyFactory.CreateKeyVaultManager(invalidUri, this.mockFixture.CertificateManager.Object));
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
        public void DependencyFactoryCreatesTheExpectedProfileReferenceForStorageAccountSasUris(string uri, string expectedUri, string expectedProfileName)
        {
            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
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
        public void DependencyFactoryCreatesTheExpectedProfileReferenceForConnectionStringsReferencingManagedIdentities(string connectionString, string expectedUri, string expectedProfileName)
        {
            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
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

            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
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

            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
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
            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
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

            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
               uri,
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

            DependencyProfileReference profileReference = DependencyFactory.CreateProfileReference(
               uri,
               this.mockFixture.CertificateManager.Object);

            Assert.IsNotNull(profileReference);
            Assert.AreEqual(new Uri(expectedUri).ToString(), profileReference.ProfileUri.ToString());
            Assert.IsNotNull(profileReference.Credentials);
            Assert.AreEqual(expectedProfileName, profileReference.ProfileName);
            Assert.IsInstanceOf<ClientCertificateCredential>(profileReference.Credentials);
        }

        [Test]
        [TestCase("https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters")]
        [TestCase("EndpointUrl=https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters;ClientId=11223344")]
        [TestCase("EndpointUrl=https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters;ClientId=11223344;TenantId=55667788")]
        [TestCase("InvalidParameter=https://any.service.com/profiles/ANY-PROFILE.json?not=valid&setof=parameters;ManagedIdentityId=123456789")]
        public void EndpointUtilityThrowsWhenCreatingAProfileReferenceIfTheValueProvidedIsNotAValidEndpointUri(string invalidEndpoint)
        {
            Assert.Throws<SchemaException>(() => DependencyFactory.CreateProfileReference(
                invalidEndpoint,
                this.mockFixture.CertificateManager.Object));
        }
    }
}