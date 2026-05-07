// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class CertificateInstallationTests
    {
        private MockFixture mockFixture;

        public void Setup(PlatformID platform = PlatformID.Win32NT)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFixture.SetupCertificateMocks();

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "CertificateName", "any-cert.pfx" },
                { "TenantId", Guid.NewGuid().ToString() }
            };
        }

        [Test]
        public async Task CertificateInstallationExportsTheCertificateToFileWhenAPathIsProvided()
        {
            this.Setup();

            string expectedFilePath = this.mockFixture.GetTempPath("any-certificate.pfx");
            this.mockFixture.Parameters["FilePath"] = expectedFilePath;

            using (var component = new TestCertificateInstallation(this.mockFixture))
            {
                await component.ExecuteAsync(CancellationToken.None);

                this.mockFixture.File.Verify(
                    file => file.WriteAllBytesAsync(expectedFilePath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task CertificateInstallationInstallsTheExpectedCertificateOnWindows()
        {
            this.Setup();

            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(true);
            X509Certificate2 actualCertificate = null;

            using (var component = new TestCertificateInstallation(this.mockFixture))
            {
                component.OnDownloadCertificate = () => expectedCertificate;
                component.OnInstallCertificate = (cert) => actualCertificate = cert;

                await component.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(object.ReferenceEquals(expectedCertificate, actualCertificate));
            }
        }

        [Test]
        public async Task CertificateInstallationInstallsTheExpectedCertificateOnUnix()
        {
            this.Setup(PlatformID.Unix);

            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(true);
            X509Certificate2 actualCertificate = null;

            using (var component = new TestCertificateInstallation(this.mockFixture))
            {
                component.OnDownloadCertificate = () => expectedCertificate;
                component.OnInstallCertificate = (cert) => actualCertificate = cert;

                await component.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(object.ReferenceEquals(expectedCertificate, actualCertificate));
            }
        }

        [Test]
        public async Task CertificateInstallationInstallsTheCertificateAsExpectedOnUnix()
        {
            this.Setup(PlatformID.Unix);

            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(withPrivateKey: true);

            // Setup:
            // Certificates are installed in the user home directory.
            this.mockFixture.PlatformSpecifics.EnvironmentVariables[EnvironmentVariable.USER] = "anyuser";

            using (var component = new TestCertificateInstallation(this.mockFixture))
            {
                component.OnDownloadCertificate = () => expectedCertificate;

                string expectedDirectory = "/home/anyuser/.dotnet/corefx/cryptography/x509stores/my";
                string expectedFile = this.mockFixture.Combine(expectedDirectory, $"{expectedCertificate.Thumbprint}.pfx");

                await component.ExecuteAsync(CancellationToken.None);

                // Expected:
                // The certificate should be installed/copied to the expected directory.
                this.mockFixture.Directory.Verify(fs => fs.CreateDirectory(expectedDirectory), Times.Once);
                this.mockFixture.File.Verify(fs => fs.WriteAllBytesAsync(expectedFile, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

                // Permissions should be set correctly on the certificate directory.
                this.mockFixture.ProcessManager.CommandsExecuted($"chmod -R 777 {expectedDirectory}");
            }
        }

        [Test]
        public async Task CertificateInstallationInstallsTheCertificateAsExpectedOnUnixWhenRunningAsSudo()
        {
            this.Setup(PlatformID.Unix);

            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(withPrivateKey: true);

            // Setup:
            // Certificates are installed in the user home directory when running
            // as sudo as well.
            this.mockFixture.PlatformSpecifics.EnvironmentVariables[EnvironmentVariable.USER] = "root";
            this.mockFixture.PlatformSpecifics.EnvironmentVariables[EnvironmentVariable.SUDO_USER] = "anyuser";

            using (var component = new TestCertificateInstallation(this.mockFixture))
            {
                component.OnDownloadCertificate = () => expectedCertificate;

                string expectedDirectory = "/home/anyuser/.dotnet/corefx/cryptography/x509stores/my";
                string expectedFile = this.mockFixture.Combine(expectedDirectory, $"{expectedCertificate.Thumbprint}.pfx");

                await component.ExecuteAsync(CancellationToken.None);

                // Expected:
                // The certificate should be installed/copied to the expected directory.
                this.mockFixture.Directory.Verify(fs => fs.CreateDirectory(expectedDirectory), Times.Once);
                this.mockFixture.File.Verify(fs => fs.WriteAllBytesAsync(expectedFile, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

                // Permissions should be set correctly on the certificate directory.
                this.mockFixture.ProcessManager.CommandsExecuted($"chmod -R 777 {expectedDirectory}");
            }
        }

        [Test]
        public async Task CertificateInstallationInstallsTheCertificateAsExpectedOnUnixWhenRunningAsRoot()
        {
            this.Setup(PlatformID.Unix);

            X509Certificate2 expectedCertificate = this.mockFixture.CreateCertificate(withPrivateKey: true);

            // Setup:
            // Certificates are installed in the '/root' directory.
            this.mockFixture.PlatformSpecifics.EnvironmentVariables[EnvironmentVariable.USER] = "root";

            using (var component = new TestCertificateInstallation(this.mockFixture))
            {
                component.OnDownloadCertificate = () => expectedCertificate;

                string expectedDirectory = "/root/.dotnet/corefx/cryptography/x509stores/my";
                string expectedFile = this.mockFixture.Combine(expectedDirectory, $"{expectedCertificate.Thumbprint}.pfx");

                await component.ExecuteAsync(CancellationToken.None);

                // Expected:
                // The certificate should be installed/copied to the expected directory.
                this.mockFixture.Directory.Verify(fs => fs.CreateDirectory(expectedDirectory), Times.Once);
                this.mockFixture.File.Verify(fs => fs.WriteAllBytesAsync(expectedFile, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

                // Permissions should be set correctly on the certificate directory.
                this.mockFixture.ProcessManager.CommandsExecuted($"chmod -R 777 {expectedDirectory}");
            }
        }

        private class TestCertificateInstallation : CertificateInstallation
        {
            private MockFixture mockFixture;

            public TestCertificateInstallation(MockFixture fixture)
                : base(fixture?.Dependencies, fixture.Parameters)
            {
                this.mockFixture = fixture;
            }

            public Func<X509Certificate2> OnDownloadCertificate { get; set; }

            public Action<X509Certificate2> OnInstallCertificate { get; set; }

            public new void Validate()
            {
                base.Validate();
            }

            protected override Task<X509Certificate2> DownloadCertificateAsync(CancellationToken cancellationToken)
            {
                X509Certificate2 certificate = null;
                if (this.OnDownloadCertificate != null)
                {
                    certificate = this.OnDownloadCertificate.Invoke();
                }
                else
                {
                    certificate = this.mockFixture.CreateCertificate(withPrivateKey: true);
                }

                return Task.FromResult(certificate);
            }

            protected override Task InstallCertificateAsync(X509Certificate2 certificate)
            {
                this.OnInstallCertificate?.Invoke(certificate);
                return Task.CompletedTask;
            }
        }
    }
}