// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class CertificateManagerTests
    {
        private MockFixture mockFixture;

        public void SetupTest(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFixture.SetupCertificateMocks();
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task CertificateManagerCanLoadPrivateKeyCertificatesFromFileByThumbprint(PlatformID platform)
        {
            this.SetupTest(platform);

            string testDirectory = MockFixture.GetDirectory(typeof(CertificateManagerTests), "temp", "certs1");
            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(withPrivateKey: true);

            try
            {
                if (!Directory.Exists(testDirectory))
                {
                    Directory.CreateDirectory(testDirectory);
                }

                await File.WriteAllBytesAsync(Path.Combine(testDirectory, "test.certificate"), expectedCertificate.Export(X509ContentType.Pfx));

                CertificateManager certificateManager = new CertificateManager();
                X509Certificate2 actualCertificate = await certificateManager.GetCertificateFromPathAsync(expectedCertificate.Thumbprint, testDirectory);

                CollectionAssert.AreEqual(expectedCertificate.RawData, actualCertificate.RawData);
                Assert.IsNotNull(actualCertificate.GetRSAPrivateKey());
            }
            finally
            {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task CertificateManagerCanLoadPrivateKeyCertificatesFromFileByIssuerAndSubject(PlatformID platform)
        {
            this.SetupTest(platform);

            string testDirectory = MockFixture.GetDirectory(typeof(CertificateManagerTests), "temp", "certs2");
            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(withPrivateKey: true);

            try
            {
                if (!Directory.Exists(testDirectory))
                {
                    Directory.CreateDirectory(testDirectory);
                }

                await File.WriteAllBytesAsync(Path.Combine(testDirectory, "test.certificate"), expectedCertificate.Export(X509ContentType.Pfx));

                CertificateManager certificateManager = new CertificateManager();
                X509Certificate2 actualCertificate = await certificateManager.GetCertificateFromPathAsync(expectedCertificate.Issuer, expectedCertificate.Subject, testDirectory);

                CollectionAssert.AreEqual(expectedCertificate.RawData, actualCertificate.RawData);
                Assert.IsNotNull(actualCertificate.GetRSAPrivateKey());
            }
            finally
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }
}
