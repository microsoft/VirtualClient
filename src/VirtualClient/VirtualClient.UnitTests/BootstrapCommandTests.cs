// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class BootstrapCommandTests
    {
        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetupAccessToken_Throws_WhenKeyVaultIsInvalid(string keyVaultName)
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                KeyVault = keyVaultName
            };

            var ex = Assert.Throws<ArgumentException>(() => command.SetupAccessTokenPublic());
            StringAssert.Contains("--key-vault", ex!.Message);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetupAccessToken_Throws_WhenTenantIdIsInvalid(string tenantId)
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = tenantId
            };

            var ex = Assert.Throws<ArgumentException>(() => command.SetupAccessTokenPublic());
            StringAssert.Contains("--tenant-id", ex!.Message);
        }

        [Test]
        public void SetupAccessToken_SetsExpectedParameters()
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = "mypackage.zip",
                Name = "customname",
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001",
                AccessToken = "token",
            };

            command.SetupAccessTokenPublic();

            Assert.AreEqual(command.Parameters["KeyVaultUri"], command.KeyVault);
            Assert.AreEqual(command.Parameters["TenantId"], command.TenantId);
            Assert.AreEqual(command.Parameters["LogFileName"], "AccessToken.txt");
            Assert.AreEqual(command.Parameters.Count, 3);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetupCertificateInstallation_Throws_WhenCertNameMissing(string certName)
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                CertificateName = certName,
                KeyVault = "https://myvault.vault.azure.net/"
            };

            var ex = Assert.Throws<ArgumentException>(() => command.SetupCertificateInstallationPublic());
            StringAssert.Contains("--cert-name", ex!.Message);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetupCertificateInstallation_Throws_WhenKeyVaultMissing(string keyVaultName)
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                CertificateName = "mycert",
                KeyVault = keyVaultName
            };

            var ex = Assert.Throws<ArgumentException>(() => command.SetupCertificateInstallationPublic());
            StringAssert.Contains("--key-vault", ex!.Message);
        }

        [Test]
        public void SetupCertificateInstallation_SetsUpOnlyExpectedParameters()
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                AccessToken = "token123",
                TenantId = "00000000-0000-0000-0000-000000000001",
            };

            command.SetupCertificateInstallationPublic();

            Assert.AreEqual(command.Parameters["KeyVaultUri"], command.KeyVault);
            Assert.AreEqual(command.Parameters["CertificateName"], command.CertificateName);
            Assert.AreEqual(command.Parameters.Count, 2);

            Assert.IsFalse(command.Parameters.ContainsKey("AccessToken"));
            Assert.IsFalse(command.Parameters.ContainsKey("TenantId"));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetupPackageInstallation_Throws_WhenPackageNameIsInvalid(string value)
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = value
            };

            var ex = Assert.Throws<ArgumentException>(() => command.SetupPackageInstallationPublic());
            StringAssert.Contains("--package", ex!.Message);
        }

        [Test]
        public void SetupPackageInstallation_SetsRegisterAsNameToName_WhenProvided()
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = "mypackage.zip",
                Name = "customname"
            };

            command.SetupPackageInstallationPublic();

            Assert.AreEqual(command.Parameters["Package"], command.PackageName);
            Assert.AreEqual(command.Parameters["RegisterAsName"], "customname");
        }

        [Test]
        public void SetupPackageInstallation_SetsRegisterAsNameToPackageFileName_WhenNameNotProvided()
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = Path.Combine("C:\\packages", "mypackage.zip"),
                Name = null
            };

            command.SetupPackageInstallationPublic();

            Assert.AreEqual(command.Parameters["Package"], command.PackageName);
            Assert.AreEqual(command.Parameters["RegisterAsName"], "mypackage");
        }

        [Test]
        public void SetupPackageInstallation_SetsUpOnlyExpectedParameters()
        {
            var command = new TestBootstrapCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = "mypackage.zip",
                Name = "customname",
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                AccessToken = "token123",
                TenantId = "00000000-0000-0000-0000-000000000001",
            };

            command.SetupPackageInstallationPublic();

            Assert.AreEqual(command.Parameters["Package"], command.PackageName);
            Assert.AreEqual(command.Parameters["RegisterAsName"], "customname");
            Assert.AreEqual(command.Parameters.Count, 2);
        }

        [Test]
        public void ExecuteAsync_Throws_WhenNoOperationsSpecified()
        {
            var command = new TestBootstrapCommand();

            var ex = Assert.ThrowsAsync<ArgumentException>(
                () => command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource()));

            StringAssert.Contains("At least one operation must be specified", ex!.Message);
            StringAssert.Contains("--package", ex!.Message);
            StringAssert.Contains("--cert-name", ex!.Message);
        }

        [Test]
        public async Task ExecuteAsync_InstallsCertificateOnlyWhenTokenIsProvided()
        {
            var command = new TestBootstrapCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                PackageName = null,
                AccessToken = "token"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            CollectionAssert.AreEqual(new[] { "BOOTSTRAP-CERTIFICATE.json" }, command.Profiles.Select(p => p.ProfileName).ToArray());

            Assert.AreEqual(command.Parameters["KeyVaultUri"], command.KeyVault);
            Assert.AreEqual(command.Parameters["CertificateName"], command.CertificateName);
        }

        [Test]
        public async Task ExecuteAsync_InstallsCertificateUsingGetAccessTokenWhenTokenNotIsProvided()
        {
            var command = new TestBootstrapCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                TenantId = "00000000-0000-0000-0000-000000000001",
                AccessToken = null,
                PackageName = null
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            CollectionAssert.AreEqual(new[] { "GET-ACCESS-TOKEN.json", "BOOTSTRAP-CERTIFICATE.json" }, command.Profiles.Select(p => p.ProfileName).ToArray());

            // From SetupAccessToken()
            Assert.AreEqual(command.Parameters["KeyVaultUri"], command.KeyVault);
            Assert.AreEqual(command.Parameters["TenantId"], command.TenantId);

            // From SetupCertificateInstallation()
            Assert.AreEqual(command.Parameters["CertificateName"], command.CertificateName);
        }

        [Test]
        public async Task ExecuteAsync_InstallsPackageOnlyWhenPackageIsProvided()
        {
            var command = new TestBootstrapCommand
            {
                PackageName = Path.Combine("C:\\packages", "mypackage.zip"),
                Name = null
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());
            CollectionAssert.AreEqual(new[] { "BOOTSTRAP-PACKAGE.json" }, command.Profiles.Select(p => p.ProfileName).ToArray());

            Assert.AreEqual(command.Parameters["Package"], command.PackageName);
            Assert.AreEqual(command.Parameters["RegisterAsName"], "mypackage");
        }

        [Test]
        public async Task ExecuteAsync_InstallsCertificateThenPackage()
        {
            var command = new TestBootstrapCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                PackageName = "mypackage.zip",
                Name = "regname",
                AccessToken = "token",
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            CollectionAssert.AreEqual(new[] { "BOOTSTRAP-CERTIFICATE.json", "BOOTSTRAP-PACKAGE.json" }, command.Profiles.Select(p => p.ProfileName).ToArray());

            Assert.AreEqual(command.Parameters["KeyVaultUri"], command.KeyVault);
            Assert.AreEqual(command.Parameters["CertificateName"], command.CertificateName);
            Assert.AreEqual(command.Parameters["Package"], command.PackageName);
            Assert.AreEqual(command.Parameters["RegisterAsName"], "regname");
            Assert.IsFalse(command.Parameters.ContainsKey("TenantId"));
            Assert.AreEqual(command.Parameters.Count, 5);
        }

        [Test]
        public async Task ExecuteAsync_GetTokenThenInstallsCertificateThenPackage()
        {
            var command = new TestBootstrapCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                PackageName = "mypackage.zip",
                Name = "regname",
                AccessToken = null,
                TenantId = "00000000-0000-0000-0000-000000000001"
            };

            await command.ExecuteAsync(Array.Empty<string>(), new CancellationTokenSource());

            CollectionAssert.AreEqual(new[] { "GET-ACCESS-TOKEN.json", "BOOTSTRAP-CERTIFICATE.json", "BOOTSTRAP-PACKAGE.json" }, command.Profiles.Select(p => p.ProfileName).ToArray());

            Assert.AreEqual(command.Parameters["KeyVaultUri"], command.KeyVault);
            Assert.AreEqual(command.Parameters["CertificateName"], command.CertificateName);
            Assert.AreEqual(command.Parameters["Package"], command.PackageName);
            Assert.AreEqual(command.Parameters["RegisterAsName"], "regname");
            Assert.AreEqual(command.Parameters["TenantId"], "00000000-0000-0000-0000-000000000001");
            Assert.IsTrue(command.Parameters.ContainsKey("LogFileName"));
            Assert.AreEqual(command.Parameters.Count, 6);
        }

        [Test]
        public void ShouldInitializeKeyVault_IsTrue_WhenAccessTokenNotProvided()
        {
            var command = new TestBootstrapCommand { AccessToken = null };
            Assert.IsTrue(command.ShouldInitializeKeyVaultPublic());
        }

        [Test]
        public void ShouldInitializeKeyVault_IsTrue_WhenTenantIdIsNotProvided()
        {
            var command = new TestBootstrapCommand { TenantId = null };
            Assert.IsTrue(command.ShouldInitializeKeyVaultPublic());
        }

        [Test]
        public void ShouldInitializeKeyVault_IsTrue_WhenTenantIdAndAccessTokenAreNotProvided()
        {
            var command = new TestBootstrapCommand { TenantId = " ", AccessToken = " "};
            Assert.IsTrue(command.ShouldInitializeKeyVaultPublic());
        }

        [Test]
        public void ShouldInitializeKeyVault_IsFalse_WhenAccessTokenIsProvided()
        {
            var command = new TestBootstrapCommand { AccessToken = "helloWorld" };
            Assert.IsFalse(command.ShouldInitializeKeyVaultPublic());
        }

        [Test]
        public void ShouldInitializeKeyVault_IsFalse_WhenTenantIdIsProvided()
        {
            var command = new TestBootstrapCommand { TenantId = "helloWorld" };
            Assert.IsFalse(command.ShouldInitializeKeyVaultPublic());
        }

        [Test]
        public void ShouldInitializeKeyVault_IsFalse_WhenTenantIdAndAccessTokenAreProvided()
        {
            var command = new TestBootstrapCommand { TenantId = "helloWorld", AccessToken = "helloworld" };
            Assert.IsFalse(command.ShouldInitializeKeyVaultPublic());
        }

        internal class TestBootstrapCommand : BootstrapCommand
        {
            public void SetupAccessTokenPublic() => this.SetupAccessToken();

            public void SetupCertificateInstallationPublic() => this.SetupCertificateInstallation();

            public void SetupPackageInstallationPublic() => this.SetupPackageInstallation();

            public bool ShouldInitializeKeyVaultPublic() => this.ShouldInitializeKeyVault;

            /// <summary>
            /// Prevents the ExecuteProfileCommand pipeline from running. We only want to validate
            /// the side-effects of BootstrapCommand.ExecuteAsync: profiles computed + parameters set.
            /// </summary>
            public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                base.ExecuteAsync(args, cancellationTokenSource);
                return Task.FromResult(0);
            }
        }
    }
}