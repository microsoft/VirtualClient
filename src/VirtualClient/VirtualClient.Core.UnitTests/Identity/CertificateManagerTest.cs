// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;
    using VirtualClient;

    [TestFixture]
    [Category("Unit")]
    public class CertificateManagerTest
    {
        private TestCertificateManager testCertificateManager;
        private Fixture mockFixture;

        [SetUp]
        public void InitializeTest()
        {
            this.mockFixture = new Fixture();
            this.mockFixture.SetupCertificateMocks();
            this.testCertificateManager = new TestCertificateManager();
        }

        [Test]
        public void CertificateManagerThrowsWhenAValidThumbprintIsNotProvidedWhenSearchingForCertificates()
        {
            Assert.Throws<ArgumentException>(() => this.testCertificateManager.GetCertificateFromStoreAsync(null)
                .GetAwaiter().GetResult());
            Assert.Throws<ArgumentException>(() => this.testCertificateManager.GetCertificateFromStoreAsync(string.Empty)
                .GetAwaiter().GetResult());
        }

        [Test]
        public void CertificateManagerSearchesTheExpectedStoreForCertificates()
        {
            bool isExpectedStore = false;
            this.testCertificateManager.OnGetCertificateFromStoreAsync = (store, thumbprint, validOnly) =>
            {
                isExpectedStore = store.Name == StoreName.My.ToString() && store.Location == StoreLocation.LocalMachine;

                // Return a match
                return this.mockFixture.Create<X509Certificate2>();
            };

            this.testCertificateManager.GetCertificateFromStoreAsync("c3rt1f1c4t3thum6pr1nt", new List<StoreLocation>() { StoreLocation.LocalMachine })
                .GetAwaiter().GetResult();

            Assert.IsTrue(isExpectedStore);
        }

        [Test]
        public void CertificateManagerSearchesForTheExpectedCertificateInTheStore()
        {
            string expectedThumbprint = "c3rt1f1c4t3thum6pr1nt";
            List<bool> isExpectedCertificate = new List<bool>();
            this.testCertificateManager.OnGetCertificateFromStoreAsync = (store, thumbprint, validOnly) =>
            {
                isExpectedCertificate.Add(thumbprint == expectedThumbprint);

                // Return a match
                return this.mockFixture.Create<X509Certificate2>();
            };

            this.testCertificateManager.GetCertificateFromStoreAsync(expectedThumbprint)
                .GetAwaiter().GetResult();

            this.testCertificateManager.GetCertificateFromStoreAsync(expectedThumbprint, new List<StoreLocation>() { StoreLocation.CurrentUser }, StoreName.TrustedPeople)
               .GetAwaiter().GetResult();

            Assert.IsFalse(isExpectedCertificate.Where(result => result == false).Any());
        }

        [Test]
        public void CertificateManagerSearchesIssuerAndSubjectForTheExpectedCertificateInTheStore()
        {
            string expectedIssuer = "CN=test.certificate";
            string expectedSubjectName = "CN=test.certificate";

            X509Certificate2 expectedCertificate = this.mockFixture.Create<X509Certificate2>();

            List<bool> isExpectedCertificate = new List<bool>();
            this.testCertificateManager.OnGetCertificateByIssuerFromStoreAsync = (store, issuer, subjectName, validOnly) =>
            {
                isExpectedCertificate.Add(issuer == expectedIssuer);
                isExpectedCertificate.Add(subjectName == expectedSubjectName);

                // Return a match
                return expectedCertificate;
            };

            this.testCertificateManager.GetCertificateFromStoreAsync(expectedIssuer, expectedSubjectName)
                .GetAwaiter().GetResult();

            this.testCertificateManager.GetCertificateFromStoreAsync(expectedIssuer, expectedSubjectName, new List<StoreLocation>() { StoreLocation.CurrentUser }, StoreName.TrustedPeople)
               .GetAwaiter().GetResult();

            Assert.IsFalse(isExpectedCertificate.Where(result => result == false).Any());
        }

        [Test]
        public void CertificateManagerSearchesForAnyMatchingCertificatesByDefault()
        {
            List<bool> allCerts = new List<bool>();
            this.testCertificateManager.OnGetCertificateFromStoreAsync = (store, thumbprint, validOnly) =>
            {
                allCerts.Add(validOnly == false);

                return this.mockFixture.Create<X509Certificate2>();
            };

            this.testCertificateManager.GetCertificateFromStoreAsync("c3rt1f1c4t3thum6pr1nt")
                .GetAwaiter().GetResult();

            this.testCertificateManager.GetCertificateFromStoreAsync("c3rt1f1c4t3thum6pr1nt", new List<StoreLocation>() { StoreLocation.CurrentUser }, StoreName.TrustedPeople)
               .GetAwaiter().GetResult();

            Assert.IsFalse(allCerts.Where(result => result == false).Any());
        }

        [Test]
        public void CertificateManagerStoreThrowsOnCertificateNotFound()
        {
            Assert.Throws<CryptographicException>(
                () => this.testCertificateManager.GetCertificateFromStoreAsync("c3rt1f1c4t3thum6pr1nt")
                .GetAwaiter().GetResult());
        }

        [Test]
        public void CertificateManagerStoreThrowsOnCertificateNotFoundByIssuerAndSubjectName()
        {
            Assert.Throws<CryptographicException>(
                () => this.testCertificateManager.GetCertificateFromStoreAsync("c3rt1f1c4t3I5sU3R", "c3rt1f1c4t35uBJ3cT")
                .GetAwaiter().GetResult());
        }

        [Test]
        public void VerifyCertificateReturnsFalseOnNullCertificate()
        {
            Assert.IsFalse(this.testCertificateManager.VerifyCertificate(null, string.Empty, string.Empty, string.Empty));
        }

        [Test]
        public void VerifyCertificateAreFieldsEqualReturnsTrueOnExactSameAttributes()
        {
            Assert.IsTrue(TestCertificateManager.AreFieldsEqual(
                "CN=Test, OU=MockText",
                "CN=Test, OU=MockText"));
        }

        [Test]
        public void VerifyCertificateAreFieldsEqualReturnsTrueOnSameAttributesInDifferentOrder()
        {
            Assert.IsTrue(TestCertificateManager.AreFieldsEqual(
                "CN=Test, OU=MockText",
                "OU=MockText, CN=Test"));
        }

        [Test]
        public void VerifyCertificateAreFieldsEqualReturnsTrueOnSameAttributesWithSpacingDifferences()
        {
            Assert.IsTrue(TestCertificateManager.AreFieldsEqual(
                "CN=Test,OU=MockText",
                "CN=Test , OU = MockText"));
        }

        [Test]
        public void VerifyCertificateAreFieldsEqualReturnsFalseOnDifferentAttributeValue()
        {
            Assert.IsFalse(TestCertificateManager.AreFieldsEqual(
                "CN=Test, OU=MockText",
                "CN=Test2, OU=MockText"));
        }

        [Test]
        public void VerifyCertificateAreFieldsEqualReturnsFalseOnDifferentNumberOfAttributes()
        {
            Assert.IsFalse(TestCertificateManager.AreFieldsEqual(
                "CN=Test, OU=MockText",
                "CN=Test, OU=MockText, O=MSFT"));
        }

        [Test]
        public void VerifyCertificateAreFieldsEqualIsCaseInsensitive()
        {
            Assert.IsTrue(TestCertificateManager.AreFieldsEqual(
                "cn=test, Ou=MockText",
                "CN=Test, OU=mOckText"));
        }

        private class TestCertificateManager : CertificateManager
        {
            public Func<X509Store, string, bool, X509Certificate2> OnGetCertificateFromStoreAsync { get; set; }

            public Func<X509Store, string, string, bool, X509Certificate2> OnGetCertificateByIssuerFromStoreAsync { get; set; }

            internal static new bool AreFieldsEqual(string field1, string field2)
            {
                return CertificateManager.AreFieldsEqual(field1, field2);
            }

            protected override Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string thumbprint, bool validCertificateOnly = false)
            {
                X509Certificate2 cert = null;
                return this.OnGetCertificateFromStoreAsync != null
                    ? Task.FromResult(this.OnGetCertificateFromStoreAsync.Invoke(store, thumbprint, validCertificateOnly))
                    : Task.FromResult(cert);
            }

            protected override Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string issuer, string subject, bool validCertificateOnly = false)
            {
                X509Certificate2 cert = null;
                return this.OnGetCertificateByIssuerFromStoreAsync != null
                    ? Task.FromResult(this.OnGetCertificateByIssuerFromStoreAsync.Invoke(store, issuer, subject, validCertificateOnly))
                    : Task.FromResult(cert);
            }
        }
    }
}