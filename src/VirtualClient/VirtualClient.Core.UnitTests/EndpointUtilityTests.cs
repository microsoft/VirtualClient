// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class EndpointUtilityTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Initialize()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupCertificateMocks();
        }

        [Test]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("EventHubNamespace=any.servicebus.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ClientId=11223344;TenantId=55667788")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;CertificateIssuer=ABC CA 01;CertificateSubject=any.domain.com")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;CertificateThumbprint=123456789")]
        public void EndpointUtilityConfirmsCustomConnectionStrings(string connectionString)
        {
            Assert.IsTrue(EndpointUtility.IsCustomConnectionString(connectionString));
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorage.blob.core.windows.net/;SharedAccessSignature=sv=2022-11-02")]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123")]
        [TestCase("https://anystorage.blob.core.windows.net")]
        public void EndpointUtilityConfirmsNonCustomConnectionStrings(string connectionString)
        {
            Assert.IsFalse(EndpointUtility.IsCustomConnectionString(connectionString));
        }

        [Test]
        [TestCase("https://any.service.azure.com?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959")]
        [TestCase("https://any.service.azure.com?miid=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("https://any.service.azure.com?crti=ABC CA 01&crts=any.domain.com")]
        [TestCase("https://any.service.azure.com?crtt=123456789")]
        public void EndpointUtilityConfirmsCustomUris(string uri)
        {
            Assert.IsTrue(EndpointUtility.IsCustomUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://anystorage.blob.core.windows.net")]
        [TestCase("https://anystorage.blob.core.windows.net?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&spr=https")]
        [TestCase("https://any.vault.azure.net")]
        public void EndpointUtilityConfirmsNonCustomUris(string uri)
        {
            Assert.IsFalse(EndpointUtility.IsCustomUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://packages.virtualclient.microsoft.com")]
        [TestCase("https://packages.virtualclient.microsoft.com/")]
        [TestCase("https://packages.virtualclient.microsoft.com/any/path")]
        public void EndpointUtilityConfirmsDefaultPackageStoreUris(string uri)
        {
            Assert.IsTrue(EndpointUtility.IsDefaultPackageStore(new Uri(uri)));
        }

        [Test]
        [TestCase("https://anystorage.blob.core.windows.net")]
        [TestCase("https://any.vault.azure.net")]
        [TestCase("https://any.servicebus.windows.net")]
        public void EndpointUtilityConfirmsNonDefaultPackageStoreUris(string uri)
        {
            Assert.IsFalse(EndpointUtility.IsDefaultPackageStore(new Uri(uri)));
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123")]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKey=123")]
        [TestCase("SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123")]
        public void EndpointUtilityConfirmsEventHubConnectionStrings(string connectionString)
        {
            Assert.IsTrue(EndpointUtility.IsEventHubConnectionString(connectionString));
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("https://any.servicebus.windows.net")]
        public void EndpointUtilityConfirmsNonEventHubConnectionStrings(string connectionString)
        {
            Assert.IsFalse(EndpointUtility.IsEventHubConnectionString(connectionString));
        }

        [Test]
        [TestCase("https://any.vault.azure.net")]
        [TestCase("https://any.vault.azure.net/")]
        [TestCase("https://any.vault.azure.net/secrets/mysecret")]
        public void EndpointUtilityConfirmsKeyVaultUris(string uri)
        {
            Assert.IsTrue(EndpointUtility.IsKeyVaultUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://anystorage.blob.core.windows.net")]
        [TestCase("https://any.servicebus.windows.net")]
        [TestCase("https://packages.virtualclient.microsoft.com")]
        public void EndpointUtilityConfirmsNonKeyVaultUris(string uri)
        {
            Assert.IsFalse(EndpointUtility.IsKeyVaultUri(new Uri(uri)));
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorage.blob.core.windows.net/;SharedAccessSignature=sv=2022-11-02")]
        public void EndpointUtilityConfirmsStorageAccountConnectionStrings(string connectionString)
        {
            Assert.IsTrue(EndpointUtility.IsStorageAccountConnectionString(connectionString));
        }

        [Test]
        [TestCase("Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=123")]
        [TestCase("EndpointUrl=https://anystorage.blob.core.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("https://anystorage.blob.core.windows.net")]
        public void EndpointUtilityConfirmsNonStorageAccountConnectionStrings(string connectionString)
        {
            Assert.IsFalse(EndpointUtility.IsStorageAccountConnectionString(connectionString));
        }

        [Test]
        [TestCase("https://anystorage.blob.core.windows.net")]
        [TestCase("https://anystorage.blob.core.windows.net/")]
        [TestCase("https://anystorage.blob.core.windows.net/container/blob")]
        public void EndpointUtilityConfirmsStorageAccountUris(string uri)
        {
            Assert.IsTrue(EndpointUtility.IsStorageAccountUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://any.vault.azure.net")]
        [TestCase("https://any.servicebus.windows.net")]
        [TestCase("https://packages.virtualclient.microsoft.com")]
        public void EndpointUtilityConfirmsNonStorageAccountUris(string uri)
        {
            Assert.IsFalse(EndpointUtility.IsStorageAccountUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://anystorage.blob.core.windows.net?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https")]
        [TestCase("https://anystorage.blob.core.windows.net/?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&spr=https&sig=abc")]
        public void EndpointUtilityConfirmsStorageAccountSasUris(string uri)
        {
            Assert.IsTrue(EndpointUtility.IsStorageAccountSasUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://anystorage.blob.core.windows.net")]
        [TestCase("https://anystorage.blob.core.windows.net/")]
        [TestCase("https://any.vault.azure.net?sv=2022-11-02&se=2024-07-02T05:15:29Z&spr=https")]
        public void EndpointUtilityConfirmsNonStorageAccountSasUris(string uri)
        {
            Assert.IsFalse(EndpointUtility.IsStorageAccountSasUri(new Uri(uri)));
        }

        [Test]
        [TestCase("https://any.service.azure.com?crti=ABC CA 01&crts=any.domain.com", "ABC CA 01", "any.domain.com")]
        public void EndpointUtilityParsesCertificateIssuerAndSubjectFromUri(string uri, string expectedIssuer, string expectedSubject)
        {
            bool result = EndpointUtility.TryParseCertificateReference(new Uri(uri), out string actualIssuer, out string actualSubject);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedIssuer, actualIssuer);
            Assert.AreEqual(expectedSubject, actualSubject);
        }

        [Test]
        [TestCase("https://any.service.azure.com?miid=307591a4-abb2-4559-af59-b47177d140cf")]
        [TestCase("https://any.service.azure.com")]
        public void EndpointUtilityReturnsFalseWhenNoCertificateIssuerAndSubjectInUri(string uri)
        {
            bool result = EndpointUtility.TryParseCertificateReference(new Uri(uri), out string actualIssuer, out string actualSubject);

            Assert.IsFalse(result);
            Assert.IsNull(actualIssuer);
            Assert.IsNull(actualSubject);
        }

        [Test]
        public void EndpointUtilityGetsCertificateThumbprintFromConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.CertificateThumbprint, "123456789ABCDEF" }
            };

            bool result = EndpointUtility.TryGetConnectionStringCertificateReference(parameters, out string actualThumbprint);

            Assert.IsTrue(result);
            Assert.AreEqual("123456789ABCDEF", actualThumbprint);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenNoCertificateThumbprintInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.ManagedIdentityId, "307591a4-abb2-4559-af59-b47177d140cf" }
            };

            bool result = EndpointUtility.TryGetConnectionStringCertificateReference(parameters, out string actualThumbprint);

            Assert.IsFalse(result);
            Assert.IsNull(actualThumbprint);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenConnectionStringParametersAreEmptyForCertificateThumbprintLookup()
        {
            bool result = EndpointUtility.TryGetConnectionStringCertificateReference(
                new Dictionary<string, string>(),
                out string actualThumbprint);

            Assert.IsFalse(result);
            Assert.IsNull(actualThumbprint);
        }

        [Test]
        public void EndpointUtilityGetsCertificateIssuerAndSubjectFromConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.CertificateIssuer, "ABC CA 01" },
                { ConnectionParameter.CertificateSubject, "any.domain.com" }
            };

            bool result = EndpointUtility.TryGetConnectionStringCertificateReference(parameters, out string actualIssuer, out string actualSubject);

            Assert.IsTrue(result);
            Assert.AreEqual("ABC CA 01", actualIssuer);
            Assert.AreEqual("any.domain.com", actualSubject);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlyIssuerDefinedInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.CertificateIssuer, "ABC CA 01" }
            };

            bool result = EndpointUtility.TryGetConnectionStringCertificateReference(parameters, out string actualIssuer, out string actualSubject);

            Assert.IsFalse(result);
            Assert.IsNull(actualIssuer);
            Assert.IsNull(actualSubject);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlySubjectDefinedInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.CertificateSubject, "any.domain.com" }
            };

            bool result = EndpointUtility.TryGetConnectionStringCertificateReference(parameters, out string actualIssuer, out string actualSubject);

            Assert.IsFalse(result);
            Assert.IsNull(actualIssuer);
            Assert.IsNull(actualSubject);
        }

        [Test]
        [TestCase("EndpointUrl", "https://anystorage.blob.core.windows.net")]
        [TestCase("Endpoint", "sb://any.servicebus.windows.net/")]
        public void EndpointUtilityGetsEndpointFromConnectionStringParameters(string parameterName, string expectedEndpoint)
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { parameterName, expectedEndpoint }
            };

            bool result = EndpointUtility.TryGetConnectionStringEndpoint(parameters, out string actualEndpoint);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedEndpoint, actualEndpoint);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenNoEndpointInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.ManagedIdentityId, "307591a4-abb2-4559-af59-b47177d140cf" }
            };

            bool result = EndpointUtility.TryGetConnectionStringEndpoint(parameters, out string actualEndpoint);

            Assert.IsFalse(result);
            Assert.IsNull(actualEndpoint);
        }

        [Test]
        public void EndpointUtilityGetsManagedIdentityIdFromConnectionStringParameters()
        {
            string expectedManagedIdentityId = "307591a4-abb2-4559-af59-b47177d140cf";
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.ManagedIdentityId, expectedManagedIdentityId }
            };

            bool result = EndpointUtility.TryGetConnectionStringManagedIdentityReference(parameters, out string actualManagedIdentityId);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedManagedIdentityId, actualManagedIdentityId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenNoManagedIdentityIdInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.ClientId, "11223344" }
            };

            bool result = EndpointUtility.TryGetConnectionStringManagedIdentityReference(parameters, out string actualManagedIdentityId);

            Assert.IsFalse(result);
            Assert.IsNull(actualManagedIdentityId);
        }

        [Test]
        public void EndpointUtilityGetsMicrosoftEntraClientIdAndTenantIdFromConnectionStringParameters()
        {
            string expectedClientId = "307591a4-abb2-4559-af59-b47177d140cf";
            string expectedTenantId = "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959";
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.ClientId, expectedClientId },
                { ConnectionParameter.TenantId, expectedTenantId }
            };

            bool result = EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(parameters, out string actualClientId, out string actualTenantId);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedClientId, actualClientId);
            Assert.AreEqual(expectedTenantId, actualTenantId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlyClientIdDefinedInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.ClientId, "307591a4-abb2-4559-af59-b47177d140cf" }
            };

            bool result = EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(parameters, out string actualClientId, out string actualTenantId);

            Assert.IsFalse(result);
            Assert.IsNull(actualClientId);
            Assert.IsNull(actualTenantId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlyTenantIdDefinedInConnectionStringParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { ConnectionParameter.TenantId, "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959" }
            };

            bool result = EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(parameters, out string actualClientId, out string actualTenantId);

            Assert.IsFalse(result);
            Assert.IsNull(actualClientId);
            Assert.IsNull(actualTenantId);
        }

        [Test]
        public void EndpointUtilityGetsCertificateThumbprintFromUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.CertificateThumbprint, "123456789ABCDEF" }
            };

            bool result = EndpointUtility.TryGetUriCertificateReference(parameters, out string actualThumbprint);

            Assert.IsTrue(result);
            Assert.AreEqual("123456789ABCDEF", actualThumbprint);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenNoCertificateThumbprintInUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.ManagedIdentityId, "307591a4-abb2-4559-af59-b47177d140cf" }
            };

            bool result = EndpointUtility.TryGetUriCertificateReference(parameters, out string actualThumbprint);

            Assert.IsFalse(result);
            Assert.IsNull(actualThumbprint);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenUriParametersAreEmptyForCertificateThumbprintLookup()
        {
            bool result = EndpointUtility.TryGetUriCertificateReference(
                new Dictionary<string, string>(),
                out string actualThumbprint);

            Assert.IsFalse(result);
            Assert.IsNull(actualThumbprint);
        }

        [Test]
        public void EndpointUtilityGetsCertificateIssuerAndSubjectFromUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.CertificateIssuer, "ABC CA 01" },
                { UriParameter.CertificateSubject, "any.domain.com" }
            };

            bool result = EndpointUtility.TryGetUriCertificateReference(parameters, out string actualIssuer, out string actualSubject);

            Assert.IsTrue(result);
            Assert.AreEqual("ABC CA 01", actualIssuer);
            Assert.AreEqual("any.domain.com", actualSubject);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlyIssuerDefinedInUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.CertificateIssuer, "ABC CA 01" }
            };

            bool result = EndpointUtility.TryGetUriCertificateReference(parameters, out string actualIssuer, out string actualSubject);

            Assert.IsFalse(result);
            Assert.IsNull(actualIssuer);
            Assert.IsNull(actualSubject);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlySubjectDefinedInUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.CertificateSubject, "any.domain.com" }
            };

            bool result = EndpointUtility.TryGetUriCertificateReference(parameters, out string actualIssuer, out string actualSubject);

            Assert.IsFalse(result);
            Assert.IsNull(actualIssuer);
            Assert.IsNull(actualSubject);
        }

        [Test]
        public void EndpointUtilityGetsManagedIdentityIdFromUriParameters()
        {
            string expectedManagedIdentityId = "307591a4-abb2-4559-af59-b47177d140cf";
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.ManagedIdentityId, expectedManagedIdentityId }
            };

            bool result = EndpointUtility.TryGetUriManagedIdentityReference(parameters, out string actualManagedIdentityId);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedManagedIdentityId, actualManagedIdentityId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenNoManagedIdentityIdInUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.ClientId, "307591a4-abb2-4559-af59-b47177d140cf" }
            };

            bool result = EndpointUtility.TryGetUriManagedIdentityReference(parameters, out string actualManagedIdentityId);

            Assert.IsFalse(result);
            Assert.IsNull(actualManagedIdentityId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenUriParametersAreEmptyForManagedIdentityLookup()
        {
            bool result = EndpointUtility.TryGetUriManagedIdentityReference(
                new Dictionary<string, string>(),
                out string actualManagedIdentityId);

            Assert.IsFalse(result);
            Assert.IsNull(actualManagedIdentityId);
        }

        [Test]
        public void EndpointUtilityGetsMicrosoftEntraClientIdAndTenantIdFromUriParameters()
        {
            string expectedClientId = "307591a4-abb2-4559-af59-b47177d140cf";
            string expectedTenantId = "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959";
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.ClientId, expectedClientId },
                { UriParameter.TenantId, expectedTenantId }
            };

            bool result = EndpointUtility.TryGetUriMicrosoftEntraReference(parameters, out string actualClientId, out string actualTenantId);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedClientId, actualClientId);
            Assert.AreEqual(expectedTenantId, actualTenantId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlyClientIdDefinedInUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.ClientId, "307591a4-abb2-4559-af59-b47177d140cf" }
            };

            bool result = EndpointUtility.TryGetUriMicrosoftEntraReference(parameters, out string actualClientId, out string actualTenantId);

            Assert.IsFalse(result);
            Assert.IsNull(actualClientId);
            Assert.IsNull(actualTenantId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenOnlyTenantIdDefinedInUriParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { UriParameter.TenantId, "985bbc17-e3a5-4fec-b0cb-40dbb8bc5959" }
            };

            bool result = EndpointUtility.TryGetUriMicrosoftEntraReference(parameters, out string actualClientId, out string actualTenantId);

            Assert.IsFalse(result);
            Assert.IsNull(actualClientId);
            Assert.IsNull(actualTenantId);
        }

        [Test]
        public void EndpointUtilityReturnsFalseWhenUriParametersAreEmptyForMicrosoftEntraLookup()
        {
            bool result = EndpointUtility.TryGetUriMicrosoftEntraReference(
                new Dictionary<string, string>(),
                out string actualClientId,
                out string actualTenantId);

            Assert.IsFalse(result);
            Assert.IsNull(actualClientId);
            Assert.IsNull(actualTenantId);
        }
    }
}