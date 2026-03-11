// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    /// <summary>
    /// Virtual Client component that installs certificates from Azure Key Vault
    /// into the appropriate certificate store for the operating system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class CertificateInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManagement;
        private IAuthorizationManager authorizationManager;
        private IFileSystem fileSystem;
        private ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">Parameters to the Virtual Client component.</param>
        public CertificateInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.processManager = this.systemManagement.ProcessManager;
            this.authorizationManager = dependencies.GetService<IAuthorizationManager>();
        }

        /// <summary>
        /// Gets an access token to use to authenticate with the target Key Vault.
        /// </summary>
        public string AccessToken
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.AccessToken), out IConvertible accessToken);
                return accessToken?.ToString();
            }
        }

        /// <summary>
        /// The name of the certificate to be downloaded from the Key Vault to install.
        /// </summary>
        public string CertificateName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CertificateName));
            }
        }

        /// <summary>
        /// Gets the path to the file to which the certificate will be written.
        /// </summary>
        public string FilePath
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.FilePath), out IConvertible filePath);
                return filePath?.ToString();
            }
        }

        /// <summary>
        /// The ID of the Azure tenant in which the Key Vault exists.
        /// </summary>
        public string TenantId
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.TenantId), out IConvertible tenantId);
                return tenantId?.ToString();
            }
        }

        /// <summary>
        /// Acquires an access token for the configured Key Vault URI using Azure Identity.
        /// The component attempts interactive browser authentication first and falls back to
        /// device-code authentication when a browser is not available (e.g. headless Linux).
        /// The token is always written to standard output. Token is also written to a file if AccessTokenPath is resolved.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                X509Certificate2 certificate = await this.DownloadCertificateAsync(cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    if (!string.IsNullOrWhiteSpace(this.FilePath))
                    {
                        await this.ExportCertificate(certificate, cancellationToken);
                    }
                    else
                    {
                        if (this.Platform == PlatformID.Win32NT)
                        {
                            await this.InstallCertificateOnWindowsAsync(certificate, cancellationToken);
                        }
                        else if (this.Platform == PlatformID.Unix)
                        {
                            await this.InstallCertificateOnUnixAsync(certificate, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                throw new DependencyException(
                    $"An error occurred installing the certificate '{this.CertificateName}' from the Key Vault.",
                    exc,
                    ErrorReason.DependencyInstallationFailed);
            }
        }

        /// <summary>
        /// Exports/writes the certificate to file.
        /// </summary>
        protected virtual Task ExportCertificate(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            // 1. Export the certificate to a byte array (PKCS #12 / PFX format)
            // We include the private key and set the Exportable flag if necessary.
            byte[] pfxBytes = certificate.Export(X509ContentType.Pfx, string.Empty);

            // 2. Write the bytes to the file system
            return this.fileSystem.File.WriteAllBytesAsync(this.FilePath, pfxBytes);
        }

        /// <summary>
        /// Gets the Key Vault manager to use to retrieve certificates from Key Vault.
        /// </summary>
        protected virtual async Task<X509Certificate2> DownloadCertificateAsync(CancellationToken cancellationToken)
        {
            X509Certificate2 certificate = null;
            IKeyVaultManager keyVaultManager = this.Dependencies.GetService<IKeyVaultManager>();
            DependencyKeyVaultStore keyVaultStore = keyVaultManager.StoreDescription as DependencyKeyVaultStore;

            // Order of priority:
            // 1) access token
            // 2) access token file
            // 3) Key Vault URI + tenant ID.
            // 4) Key Vault info provided on command line (URI + connection details).
            string accessToken = null;
            if (!string.IsNullOrWhiteSpace(this.AccessToken))
            {
                accessToken = this.AccessToken;
            }
            else if (keyVaultStore.Credentials == null)
            {
                Uri resourceUri = new Uri(keyVaultManager.StoreDescription.ToString());
                accessToken = await this.authorizationManager.GetAccessTokenAsync(resourceUri, this.TenantId, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var tokenCredential = new AccessTokenCredential(accessToken);
                var store = new DependencyKeyVaultStore(
                    DependencyStore.KeyVault,
                    new Uri(keyVaultStore.ToString()),
                    tokenCredential);

                certificate = await (new KeyVaultManager(store)).GetCertificateAsync(this.CertificateName, cancellationToken, null, withPrivateKey: true);
            }
            else
            {
                certificate = await keyVaultManager.GetCertificateAsync(this.CertificateName, cancellationToken, null, withPrivateKey: true);
            }

            return certificate;
        }

        /// <summary>
        /// Installs the certificate to the local certificate store.
        /// </summary>
        /// <param name="certificate">The certificate to install locally.</param>
        protected virtual Task InstallCertificateAsync(X509Certificate2 certificate)
        {
            return Task.Run(() =>
            {
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }
            });
        }

        private Task InstallCertificateOnWindowsAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            return this.InstallCertificateAsync(certificate);
        }

        private async Task InstallCertificateOnUnixAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            // On Unix/Linux systems, we install the certificate in the default location for the
            // user as well as in a static location. In the future we will likely use the static location
            // only.
            string certificateDirectory = null;

            try
            {
                // When "sudo" is used to run the installer, we need to know the logged
                // in user account. On Linux systems, there is an environment variable 'SUDO_USER'
                // that defines the logged in user.

                string user = this.GetEnvironmentVariable(EnvironmentVariable.USER);
                string sudoUser = this.GetEnvironmentVariable(EnvironmentVariable.SUDO_USER);
                certificateDirectory = $"/home/{user}/.dotnet/corefx/cryptography/x509stores/my";

                if (!string.IsNullOrWhiteSpace(sudoUser))
                {
                    // The installer is being executed with "sudo" privileges. We want to use the
                    // logged in user profile vs. "root".
                    certificateDirectory = $"/home/{sudoUser}/.dotnet/corefx/cryptography/x509stores/my";
                }
                else if (user == "root")
                {
                    // The installer is being executed from the "root" account on Linux.
                    certificateDirectory = $"/root/.dotnet/corefx/cryptography/x509stores/my";
                }

                Console.WriteLine($"Certificate Store = {certificateDirectory}");

                if (!this.fileSystem.Directory.Exists(certificateDirectory))
                {
                    this.fileSystem.Directory.CreateDirectory(certificateDirectory);
                }

                await this.InstallCertificateAsync(certificate);

                await this.fileSystem.File.WriteAllBytesAsync(
                    this.Combine(certificateDirectory, $"{certificate.Thumbprint}.pfx"),
                    certificate.Export(X509ContentType.Pfx));

                // Permissions 777 (-rwxrwxrwx)
                // https://linuxhandbook.com/linux-file-permissions/
                using (IProcessProxy process = this.processManager.CreateProcess("chmod", $"-R 777 {certificateDirectory}"))
                {
                    await process.StartAndWaitAsync(cancellationToken);
                    process.ThrowIfErrored<DependencyException>();
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException(
                    $"Access permissions denied for certificate directory '{certificateDirectory}'. Execute the application with " +
                    $"sudo/root privileges to install certificates in privileged locations.");
            }
        }
    }
}