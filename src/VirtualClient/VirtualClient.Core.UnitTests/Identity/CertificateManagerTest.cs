// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using AutoFixture;
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class CertificateManagerTest
    {
        private TestCertificateManager testCertificateManager;
        private MockFixture mockFixture;

        [SetUp]
        public void InitializeTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupCertificateMocks();
            this.testCertificateManager = new TestCertificateManager(this.mockFixture);
        }

        [Test]
        [TestCase("AME")]
        [TestCase("GBL")]
        [TestCase("AME Infra Test CA 7")]
        [TestCase("DC=AME")]
        [TestCase("DC=GBL")]
        [TestCase("CN=AME")]
        public void CertificateManagerSearchesSupportsARangeOfFormatsForIssuersOnCertificates(string issuer)
        {
            X509Certificate2 certificate = this.mockFixture.Create<X509Certificate2>();
            Assert.True(CertificateManager.IsMatchingIssuer(certificate, issuer));
        }

        [Test]
        [TestCase("ABC")]
        [TestCase("AME Infra Test CA 06")]
        [TestCase("DC=ABC")]
        [TestCase("DC=GBB")]
        [TestCase("DC=Infra Test CA 777")]
        [TestCase("CN=ABC")]
        [TestCase("CN=AME Infra Test CA 06")]
        [TestCase("CN=ABC Infra Test CA 777, DC=AME, DC=GBL")]
        [TestCase("CN=AME Infra Test CA 777, DC=ABC, DC=GBL")]
        [TestCase("CN=AME Infra Test CA 777, DC=AME, DC=GBB")]
        public void CertificateManagerDoesNotMismatchIssuersOnCertificates(string issuer)
        {
            X509Certificate2 certificate = this.mockFixture.Create<X509Certificate2>();
            Assert.False(CertificateManager.IsMatchingIssuer(certificate, issuer));
        }

        [Test]
        [TestCase("virtualclient")]
        [TestCase("virtualclient.test.corp")]
        [TestCase("virtualclient.test.corp.azure.com")]
        [TestCase("CN=virtualclient.test.corp")]
        [TestCase("CN=virtualclient.test.corp.azure.com")]
        public void CertificateManagerSearchesSupportsARangeOfFormatsForSubjectNamesOnCertificates(string subjectName)
        {
            X509Certificate2 certificate = this.mockFixture.Create<X509Certificate2>();
            Assert.True(CertificateManager.IsMatchingSubjectName(certificate, subjectName));
        }

        [Test]
        [TestCase("virtualclients")]
        [TestCase("virtualclient.azure.com")]
        [TestCase("CN=virtualclient.azure.com")]
        [TestCase("CN=virtualclient.other.azure.com")]
        public void CertificateManagerDoesNotMismatchSubjectNamesOnCertificates(string issuer)
        {
            X509Certificate2 certificate = this.mockFixture.Create<X509Certificate2>();
            Assert.False(CertificateManager.IsMatchingSubjectName(certificate, issuer));
        }

        [Test]
        public async Task CertificateManagerSearchesTheExpectedDirectoryForCertificates()
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.testCertificateManager = new TestCertificateManager(this.mockFixture);

            string expectedDirectory = CertificateManager.DefaultUnixCertificateDirectory;
            string expectedCertificateFile = this.mockFixture.Combine(expectedDirectory, "545AF7DD6DA3A7A78BE6D5A7A316CAD52942F949");
            bool confirmedDir = false;
            bool confirmedFile = false;

            // Issuer: AME
            // Subject Name: virtualclient.test.corp.azure.com
            // Thumbprint: 545AF7DD6DA3A7A78BE6D5A7A316CAD52942F949
            //
            // Note that this is an expired/invalid certificate so there are no security concerns. It is merely
            // used for testing purposes.
            X509Certificate2 certificate = this.mockFixture.Create<X509Certificate2>();

            // Setup:
            // The certificate directory exists.
            this.mockFixture.Directory
                .Setup(dir => dir.Exists(expectedDirectory))
                .Returns(true);

            // Setup:
            // There are certificates in the directory.
            this.mockFixture.Directory
                .Setup(dir => dir.GetFiles(It.IsAny<string>()))
                .Callback<string>(actualDirectory =>
                {
                    Assert.AreEqual(expectedDirectory, actualDirectory);
                    confirmedDir = true;
                })
                .Returns(new string[] { expectedCertificateFile });

            // Setup:
            // The certificate content/bytes.
            this.mockFixture.File
                .Setup(file => file.ReadAllBytes(expectedCertificateFile))
                .Callback<string>((actualCertificateFile) =>
                {
                    Assert.AreEqual(expectedCertificateFile, actualCertificateFile);
                    confirmedFile = true;
                })
                .Returns(certificate.Export(X509ContentType.Pfx));

            // Expectation:
            // We do not need to compare the certificate properties. We just need to ensure we attempted to
            // read from the expected directory and that the certificate deserializes without error.
            await this.testCertificateManager.GetCertificateFromPathAsync("AME", "virtualclient.test.corp.azure.com", expectedDirectory);

            Assert.IsTrue(confirmedDir);
            Assert.IsTrue(confirmedFile);
        }

        [Test]
        [TestCase("AME", "virtualclient.test.corp.azure.com")]
        [TestCase("GBL", "virtualclient.test.corp.azure.com")]
        [TestCase("AME Infra Test CA 7", "virtualclient")]
        [TestCase("DC=AME", "corp.azure.com")]
        [TestCase("DC=GBL", "azure.com")]
        [TestCase("CN=AME", "virtualclient.test.corp.azure.com")]
        public async Task CertificateManagerHandlesDifferentIssuerAndSubjectNameFormats(string issuer, string subjectName)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.testCertificateManager = new TestCertificateManager(this.mockFixture);

            string expectedDirectory = string.Format(CertificateManager.DefaultUnixCertificateDirectory, Environment.UserName.ToLowerInvariant());
            string expectedCertificateFile = this.mockFixture.Combine(expectedDirectory, "6E68322DBFF09EEB4CDB00AE94E00EF2037653EF");
            bool confirmedDir = false;
            bool confirmedFile = false;

            // Issuer: AME
            // Subject Name: virtualclient.test.corp.azure.com
            // Thumbprint: 545AF7DD6DA3A7A78BE6D5A7A316CAD52942F949
            //
            // Note that this is an expired/invalid certificate so there are no security concerns. It is merely
            // used for testing purposes.
            X509Certificate2 certificate = this.mockFixture.Create<X509Certificate2>();

            // Setup:
            // The certificate directory exists.
            this.mockFixture.Directory
                .Setup(dir => dir.Exists(expectedDirectory))
                .Returns(true);

            // Setup:
            // There are certificates in the directory.
            this.mockFixture.Directory
                .Setup(dir => dir.GetFiles(It.IsAny<string>()))
                .Callback<string>(actualDirectory =>
                {
                    Assert.AreEqual(expectedDirectory, actualDirectory);
                    confirmedDir = true;
                })
                .Returns(new string[] { expectedCertificateFile });

            // Setup:
            // The certificate content/bytes.
            this.mockFixture.File
                .Setup(file => file.ReadAllBytes(expectedCertificateFile))
                .Callback<string>((actualCertificateFile) =>
                {
                    Assert.AreEqual(expectedCertificateFile, actualCertificateFile);
                    confirmedFile = true;
                })
                .Returns(certificate.Export(X509ContentType.Pfx));

            // Expectation:
            // We do not need to compare the certificate properties. We just need to ensure we attempted to
            // read from the expected directory and that the certificate deserializes without error.
            await this.testCertificateManager.GetCertificateFromPathAsync(issuer, subjectName, expectedDirectory);

            Assert.IsTrue(confirmedDir);
            Assert.IsTrue(confirmedFile);
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
            this.testCertificateManager.OnGetCertificateFromStoreAsync = (store, thumbprint) =>
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
            this.testCertificateManager.OnGetCertificateFromStoreAsync = (store, thumbprint) =>
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
            this.testCertificateManager.OnGetCertificatesByIssuerFromStoreAsync = (store, issuer, subjectName) =>
            {
                isExpectedCertificate.Add(issuer == expectedIssuer);
                isExpectedCertificate.Add(subjectName == expectedSubjectName);

                // Return a match
                return new List<X509Certificate2> { expectedCertificate };
            };

            this.testCertificateManager.GetCertificateFromStoreAsync(expectedIssuer, expectedSubjectName)
                .GetAwaiter().GetResult();

            this.testCertificateManager.GetCertificateFromStoreAsync(expectedIssuer, expectedSubjectName, new List<StoreLocation>() { StoreLocation.CurrentUser }, StoreName.TrustedPeople)
               .GetAwaiter().GetResult();

            Assert.IsFalse(isExpectedCertificate.Where(result => result == false).Any());
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

        private class TestCertificateManager : CertificateManager
        {
            public TestCertificateManager(MockFixture mockFixture)
                : base(mockFixture.FileSystem.Object)
            {
            }

            public Func<X509Store, string, X509Certificate2> OnGetCertificateFromStoreAsync { get; set; }

            public Func<X509Store, string, string, IEnumerable<X509Certificate2>> OnGetCertificatesByIssuerFromStoreAsync { get; set; }

            protected override Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string thumbprint)
            {
                X509Certificate2 cert = null;
                return this.OnGetCertificateFromStoreAsync != null
                    ? Task.FromResult(this.OnGetCertificateFromStoreAsync.Invoke(store, thumbprint))
                    : Task.FromResult(cert);
            }

            protected override Task<IEnumerable<X509Certificate2>> GetCertificatesFromStoreAsync(X509Store store, string issuer, string subject)
            {
                return this.OnGetCertificatesByIssuerFromStoreAsync != null
                    ? Task.FromResult(this.OnGetCertificatesByIssuerFromStoreAsync.Invoke(store, issuer, subject))
                    : Task.FromResult(null as IEnumerable<X509Certificate2>);
            }
        }
    }
}