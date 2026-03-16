// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class BootstrapCommandTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Setup()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        public void BootstrapCommandValidatesRequiredParameters()
        {
            var command = new TestBootstrapCommand();
            var error = Assert.Throws<ArgumentException>(() => command.Validate());

            Assert.AreEqual(
                "At least one operation must be specified. Use --package for package installation from remote store or --cert-name for certificate installation.",
                error.Message);
        }

        [Test]
        public void BootstrapCommandValidatesRequiredParametersForCertificateInstallation()
        {
            var command = new TestBootstrapCommand();
            command.CertificateName = "any-cert";
            Exception error = Assert.Throws<ArgumentException>(() => command.Validate());

            Assert.AreEqual(
                "A Key Vault URI must be provided on the command line (--key-vault) to install a certificate.",
                error.Message);

            command.KeyVaultStore = new DependencyKeyVaultStore(DependencyStore.KeyVault, new Uri("https://any.vault"));
            error = Assert.Throws<ArgumentException>(() => command.Validate());

            Assert.AreEqual(
                "The Azure tenant ID must be provided on the command line (--tenant-id) to install a certificate.",
                error.Message);
        }

        [Test]
        public void BootstrapCommandDoesNotRequirePackageStoredParameterForPackageDownloads()
        {
            // It will use default vc package store.
            var command = new TestBootstrapCommand();
            command.PackageName = "any-package.zip";
            Assert.DoesNotThrow(() => command.Validate());
        }

        [Test]
        public void BootstrapCommandExecutesTheExpectedProfileToBootrapCertificates()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var command = new TestBootstrapCommand
                {
                    CertificateName = "any-cert",
                    KeyVaultStore = new DependencyKeyVaultStore(DependencyStore.KeyVault, new Uri("https://any.vault.azure.net")),
                    TenantId = Guid.NewGuid().ToString()
                };

                command.Initialize(Array.Empty<string>(), this.mockFixture.PlatformSpecifics);

                Assert.IsNotEmpty(command.Profiles);
                Assert.IsTrue(command.Profiles.Count() == 1);
                Assert.IsTrue(command.Profiles.Count(p => p.ProfileName == "BOOTSTRAP-CERTIFICATE.json") == 1);
            }
        }

        [Test]
        public void BootstrapCommandProvidesTheExpectedParametersToTheProfileToBootrapCertificates()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var command = new TestBootstrapCommand
                {
                    AccessToken = "anyT@k3n",
                    CertificateName = "any-cert",
                    KeyVaultStore = new DependencyKeyVaultStore(DependencyStore.KeyVault, new Uri("https://any.vault.azure.net")),
                    TenantId = Guid.NewGuid().ToString()
                };

                command.Initialize(Array.Empty<string>(), this.mockFixture.PlatformSpecifics);

                Assert.IsNotEmpty(command.Parameters);
                Assert.IsTrue(command.Parameters.TryGetValue("AccessToken", out IConvertible accessToken) && accessToken.ToString() == command.AccessToken);
                Assert.IsTrue(command.Parameters.TryGetValue("CertificateName", out IConvertible certificateName) && certificateName.ToString() == command.CertificateName);
                Assert.IsTrue(command.Parameters.TryGetValue("TenantId", out IConvertible tenantId) && tenantId.ToString() == command.TenantId);
            }
        }

        [Test]
        public void BootstrapCommandExecutesTheExpectedProfileToBootstrapPackages()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var command = new TestBootstrapCommand
                {
                    PackageName = "any-package.zip",
                    // PackageStore is no longer required
                    Name = "any-package"
                };

                command.Initialize(Array.Empty<string>(), this.mockFixture.PlatformSpecifics);

                Assert.IsNotEmpty(command.Profiles);
                Assert.IsTrue(command.Profiles.Count() == 1);
                Assert.IsTrue(command.Profiles.Count(p => p.ProfileName == "BOOTSTRAP-PACKAGE.json") == 1);
            }
        }

        [Test]
        public void BootstrapCommandProvidesTheExpectedParametersToTheProfileToBootstrapPackages()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var command = new TestBootstrapCommand
                {
                    PackageName = "any-package.zip",
                    PackageStore = new DependencyBlobStore(DependencyStore.Packages, "https://any.storage"),
                    Name = "any-package"
                };

                command.Initialize(Array.Empty<string>(), this.mockFixture.PlatformSpecifics);

                Assert.IsNotEmpty(command.Parameters);
                Assert.IsTrue(command.Parameters.TryGetValue("Package", out IConvertible packageName) && packageName.ToString() == command.PackageName);
                Assert.IsTrue(command.Parameters.TryGetValue("RegisterAsName", out IConvertible name) && name.ToString() == command.Name);
            }
        }

        [Test]
        public void BootstrapCommandDoesNotRequirePackageStoreForPackageInstallation()
        {
            // Arrange - Create command with only package name, no package store
            var command = new TestBootstrapCommand
            {
                PackageName = "any-package.zip"
            };

            // Act & Assert - Should not throw ArgumentException
            Assert.DoesNotThrow(() => command.Validate());
        }

        [Test]
        public void BootstrapCommandExecutesTheExpectedProfileToBootstrapPackagesWithoutPackageStore()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Arrange - No PackageStore provided
                var command = new TestBootstrapCommand
                {
                    PackageName = "any-package.zip",
                    Name = "any-package"
                };

                // Act
                command.Initialize(Array.Empty<string>(), this.mockFixture.PlatformSpecifics);

                // Assert - Profile should still be set up correctly
                Assert.IsNotEmpty(command.Profiles);
                Assert.AreEqual(1, command.Profiles.Count());
                Assert.AreEqual(1, command.Profiles.Count(p => p.ProfileName == "BOOTSTRAP-PACKAGE.json"));
            }
        }

        internal class TestBootstrapCommand : BootstrapCommand
        {
            /// <summary>
            /// Prevents the ExecuteProfileCommand pipeline from running. We only want to validate
            /// the side-effects of BootstrapCommand.ExecuteAsync: profiles computed + parameters set.
            /// </summary>
            protected override Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
            {
                return Task.FromResult(0);
            }

            public new void Initialize(string[] args, PlatformSpecifics platformSpecifics)
            {
                base.Initialize(args, platformSpecifics);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}