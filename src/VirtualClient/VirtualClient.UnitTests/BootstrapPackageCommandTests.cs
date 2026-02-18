// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class BootstrapPackageCommandTests
    {
        [Test]
        public void ValidateParameters_InitializesParametersDictionary_WhenNull()
        {
            var command = new TestBootstrapPackageCommand
            {
                Parameters = null,
                PackageName = "anypackage.zip"
            };

            command.ValidateParametersPublic();

            Assert.That(command.Parameters, Is.Not.Null);
            Assert.That(command.Parameters, Is.InstanceOf<Dictionary<string, IConvertible>>());
        }

        [Test]
        public void ValidateParameters_Throws_WhenNoOperationsSpecified()
        {
            var command = new TestBootstrapPackageCommand
            {
                PackageName = null,
                CertificateName = null
            };

            var ex = Assert.Throws<ArgumentException>(() => command.ValidateParametersPublic());
            StringAssert.Contains("At least one operation must be specified", ex!.Message);
        }

        [Test]
        public void ValidateParameters_Throws_WhenCertNameProvidedWithoutKeyVault()
        {
            var command = new TestBootstrapPackageCommand
            {
                CertificateName = "mycert",
                KeyVault = null
            };

            var ex = Assert.Throws<ArgumentException>(() => command.ValidateParametersPublic());
            StringAssert.Contains("The Key Vault URI must be provided (--key-vault)", ex!.Message);
        }

        [Test]
        public void ValidateParameters_DoesNotThrow_WhenPackageProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                PackageName = "anypackage.zip",
                CertificateName = null
            };

            Assert.DoesNotThrow(() => command.ValidateParametersPublic());
        }

        [Test]
        public void ValidateParameters_DoesNotThrow_WhenCertNameAndKeyVaultProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/"
            };

            Assert.DoesNotThrow(() => command.ValidateParametersPublic());
        }

        [Test]
        public void SetupCertificateInstallation_SetsExpectedParameters_WhenNoAccessToken()
        {
            var command = new TestBootstrapPackageCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                AccessToken = null
            };

            command.SetupCertificateInstallationPublic();

            Assert.That(command.Parameters["KeyVaultUri"], Is.EqualTo(command.KeyVault));
            Assert.That(command.Parameters["CertificateName"], Is.EqualTo(command.CertificateName));
            Assert.That(command.Parameters.ContainsKey("AccessToken"), Is.False);
        }

        [Test]
        public void SetupCertificateInstallation_SetsExpectedParameters_WhenAccessTokenProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                AccessToken = "token"
            };

            command.SetupCertificateInstallationPublic();

            Assert.That(command.Parameters["KeyVaultUri"], Is.EqualTo(command.KeyVault));
            Assert.That(command.Parameters["CertificateName"], Is.EqualTo(command.CertificateName));
            Assert.That(command.Parameters["AccessToken"], Is.EqualTo(command.AccessToken));
        }

        [Test]
        public void SetupPackageInstallation_SetsRegisterAsNameToName_WhenProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = "mypackage.zip",
                Name = "customname"
            };

            command.SetupPackageInstallationPublic();

            Assert.That(command.Parameters["Package"], Is.EqualTo(command.PackageName));
            Assert.That(command.Parameters["RegisterAsName"], Is.EqualTo("customname"));
        }

        [Test]
        public void SetupPackageInstallation_SetsRegisterAsNameToPackageFileName_WhenNameNotProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                PackageName = Path.Combine("C:\\packages", "mypackage.zip"),
                Name = null
            };

            command.SetupPackageInstallationPublic();

            Assert.That(command.Parameters["Package"], Is.EqualTo(command.PackageName));
            Assert.That(command.Parameters["RegisterAsName"], Is.EqualTo("mypackage"));
        }

        [Test]
        public void ScenarioSelection_CertificateOnly_SetsInstallCertificateOnly()
        {
            var command = new TestBootstrapPackageCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                PackageName = null
            };

            var scenarios = command.ComputeScenariosAndSetupParameters();

            CollectionAssert.AreEqual(new[] { "InstallCertificate" }, scenarios);
            Assert.That(command.Parameters["KeyVaultUri"], Is.EqualTo(command.KeyVault));
            Assert.That(command.Parameters["CertificateName"], Is.EqualTo(command.CertificateName));
        }

        [Test]
        public void ScenarioSelection_PackageOnly_SetsInstallDependenciesOnly()
        {
            var command = new TestBootstrapPackageCommand
            {
                CertificateName = null,
                PackageName = "mypackage.zip",
                Name = null
            };

            var scenarios = command.ComputeScenariosAndSetupParameters();

            CollectionAssert.AreEqual(new[] { "InstallDependencies" }, scenarios);
            Assert.That(command.Parameters["Package"], Is.EqualTo(command.PackageName));
            Assert.That(command.Parameters["RegisterAsName"], Is.EqualTo("mypackage"));
        }

        [Test]
        public void ScenarioSelection_CertificateThenPackage_SetsScenariosInOrder()
        {
            var command = new TestBootstrapPackageCommand
            {
                CertificateName = "mycert",
                KeyVault = "https://myvault.vault.azure.net/",
                PackageName = "mypackage.zip",
                Name = "regname"
            };

            var scenarios = command.ComputeScenariosAndSetupParameters();

            CollectionAssert.AreEqual(new[] { "InstallCertificate", "InstallDependencies" }, scenarios);

            // Certificate side effects
            Assert.That(command.Parameters["KeyVaultUri"], Is.EqualTo(command.KeyVault));
            Assert.That(command.Parameters["CertificateName"], Is.EqualTo(command.CertificateName));

            // Package side effects
            Assert.That(command.Parameters["Package"], Is.EqualTo(command.PackageName));
            Assert.That(command.Parameters["RegisterAsName"], Is.EqualTo("regname"));
        }

        [Test]
        public void ShouldInitializeKeyVault_IsTrue_WhenAccessTokenNotProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                AccessToken = null
            };

            Assert.That(command.ShouldInitializeKeyVaultPublic(), Is.True);
        }

        [Test]
        public void ShouldInitializeKeyVault_IsFalse_WhenAccessTokenIsProvided()
        {
            var command = new TestBootstrapPackageCommand
            {
                AccessToken = "helloWorld"
            };

            Assert.That(command.ShouldInitializeKeyVaultPublic(), Is.False);
        }

        internal sealed class TestBootstrapPackageCommand : BootstrapPackageCommand
        {
            public void ValidateParametersPublic() => this.ValidateParameters();

            public void SetupCertificateInstallationPublic() => this.SetupCertificateInstallation();

            public void SetupPackageInstallationPublic() => this.SetupPackageInstallation();

            public bool ShouldInitializeKeyVaultPublic() => this.ShouldInitializeKeyVault;

            public IReadOnlyList<string> ComputeScenariosAndSetupParameters()
            {
                this.ValidateParameters();

                var scenariosToExecute = new List<string>();

                if (!string.IsNullOrWhiteSpace(this.CertificateName))
                {
                    scenariosToExecute.Add("InstallCertificate");
                    this.SetupCertificateInstallation();
                }

                if (!string.IsNullOrWhiteSpace(this.PackageName))
                {
                    scenariosToExecute.Add("InstallDependencies");
                    this.SetupPackageInstallation();
                }

                return scenariosToExecute;
            }
        }
    }
}