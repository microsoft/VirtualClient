// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class BootstrapCommandTests
    {
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
        public void BootstrapCommandValidatesRequiredParametersForPackageDownloads()
        {
            var command = new TestBootstrapCommand();
            command.PackageName = "any-package.zip";
            Exception error = Assert.Throws<ArgumentException>(() => command.Validate());

            Assert.AreEqual(
                "A package store must be provided on the command line (--package-store) when installing packages.",
                error.Message);
        }

        [Test]
        public void BootstrapCommandExecutesTheExpectedProfileToBootrapCertificates()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                var command = new TestBootstrapCommand
                {
                    CertificateName = "any-cert",
                    TenantId = Guid.NewGuid().ToString()
                };

                command.Initialize();

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
                    TenantId = Guid.NewGuid().ToString()
                };

                command.Initialize();

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
                    Name = "any-package"
                };

                command.Initialize();

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
                    Name = "any-package"
                };

                command.Initialize();

                Assert.IsNotEmpty(command.Parameters);
                Assert.IsTrue(command.Parameters.TryGetValue("Package", out IConvertible packageName) && packageName.ToString() == command.PackageName);
                Assert.IsTrue(command.Parameters.TryGetValue("RegisterAsName", out IConvertible name) && name.ToString() == command.Name);
            }
        }

        internal class TestBootstrapCommand : BootstrapCommand
        {
            /// <summary>
            /// Prevents the ExecuteProfileCommand pipeline from running. We only want to validate
            /// the side-effects of BootstrapCommand.ExecuteAsync: profiles computed + parameters set.
            /// </summary>
            public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                return Task.FromResult(0);
            }

            public new void Initialize()
            {
                base.Initialize();
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}