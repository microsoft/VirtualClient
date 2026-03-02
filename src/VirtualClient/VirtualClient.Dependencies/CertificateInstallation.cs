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
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64", true)]
    public class CertificateInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManagement;
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
        }

        /// <summary>
        /// Gets the Azure Key Vault URI for which the access token will be requested.
        /// Example: https://anyvault.vault.azure.net/
        /// </summary>
        public string KeyVaultUri
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.KeyVaultUri));
            }
        }

        /// <summary>
        /// The name of the certificate to be retrieved
        /// </summary>
        public string CertificateName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CertificateName));
            }
        }

        /// <summary>
        /// Gets the path to the file where the access token is saved.
        /// </summary>
        public string AccessTokenPath
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.AccessTokenPath));
            }
        }

        /// <summary>
        /// Flag to decode whether to retrieve certificate with private key
        /// </summary>
        public bool WithPrivateKey
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.WithPrivateKey), true);
            }
        }

        /// <summary>
        /// Gets the directory where the certificate will be exported. If not provided, the certificate will not be exported to a file.
        /// </summary>
        public string CertificateInstallationDir
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CertificateInstallationDir), string.Empty);
            }
        }

        /// <summary>
        /// Gets the access token used to authenticate with Azure services.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Initializes the component by resolving the access token from parameters or, if necessary, from a file.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.AccessToken = this.Parameters.GetValue<string>(nameof(this.AccessToken), string.Empty);

            if (string.IsNullOrWhiteSpace(this.AccessToken) && !string.IsNullOrWhiteSpace(this.AccessTokenPath))
            {
                this.AccessToken = await this.fileSystem.File.ReadAllTextAsync(this.AccessTokenPath);
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
            this.CertificateName.ThrowIfNullOrWhiteSpace(nameof(this.CertificateName));

            try
            {
                IKeyVaultManager keyVault = this.GetKeyVaultManager();
                X509Certificate2 certificate = await keyVault.GetCertificateAsync(this.CertificateName, cancellationToken, null, this.WithPrivateKey);

                await this.InstallCertificateOnMachineAsync(certificate, cancellationToken);

                if (!string.IsNullOrWhiteSpace(this.CertificateInstallationDir))
                {
                    await this.InstallCertificateLocallyAsync(certificate, cancellationToken);
                }
            }
            catch (Exception exc)
            {
                throw new DependencyException(
                    $"An error occurred installing the certificate '{this.CertificateName}' from Key Vault. See inner exception for details.",
                    exc);
            }
        }

        /// <summary>
        /// Installs the certificate in the appropriate certificate store.
        /// </summary>
        protected virtual Task InstallCertificateOnMachineAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"Certificate Store = CurrentUser/Personal");
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }
            });
        }

        /// <summary>
        /// Gets the Key Vault manager to use to retrieve certificates from Key Vault.
        /// </summary>
        protected IKeyVaultManager GetKeyVaultManager()
        {
            IKeyVaultManager keyVaultManager = this.Dependencies.GetService<IKeyVaultManager>();
            keyVaultManager.ThrowIfNull(nameof(keyVaultManager));

            if (keyVaultManager.StoreDescription != null)
            {
                return keyVaultManager;
            }
            else if (!string.IsNullOrWhiteSpace(this.AccessToken))
            {
                this.KeyVaultUri.ThrowIfNullOrWhiteSpace(nameof(this.KeyVaultUri), "The KeyVaultUri parameter is required when authenticating with Key Vault using an access token.");

                AccessTokenCredential tokenCredential = new AccessTokenCredential(this.AccessToken);

                DependencyKeyVaultStore dependencyKeyVault = new DependencyKeyVaultStore(DependencyStore.KeyVault, new Uri(this.KeyVaultUri), tokenCredential);
                return new KeyVaultManager(dependencyKeyVault);
            }
            else
            {
                throw new InvalidOperationException($"The Key Vault manager has not been properly initialized. " +
                    $"Either valid --KeyVault or --Token or --TokenPath must be passed in order to set up authentication with Key Vault.");
            }
        }

        /// <summary>
        /// Installs the certificate in static location
        /// </summary>
        protected async Task InstallCertificateLocallyAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            try
            {
                string certificateFileName = this.WithPrivateKey
                    ? $"{this.CertificateName}.pfx"
                    : $"{this.CertificateName}.cer";

                X509ContentType contentType = this.WithPrivateKey
                    ? X509ContentType.Pfx
                    : X509ContentType.Cert;

                byte[] certBytes = certificate.Export(contentType, string.Empty);

                string certificatePath = this.Combine(this.CertificateInstallationDir, certificateFileName);

                if (!this.fileSystem.Directory.Exists(this.CertificateInstallationDir))
                {
                    this.fileSystem.Directory.CreateDirectory(this.CertificateInstallationDir);
                }
                
                await this.fileSystem.File.WriteAllBytesAsync(certificatePath, certBytes);

                if (this.Platform == PlatformID.Unix)
                {
                    // Permissions 777 (-rwxrwxrwx)
                    // https://linuxhandbook.com/linux-file-permissions/
                    using (IProcessProxy process = this.processManager.CreateProcess("chmod", $"-R 777 {this.CertificateInstallationDir}"))
                    {
                        await process.StartAndWaitAsync(cancellationToken);
                        process.ThrowIfErrored<DependencyException>();
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException(
                    $"Access permissions denied for certificate directory '{this.CertificateInstallationDir}'. Execute the installer with " +
                    $"admin/sudo/root privileges to install certificates in privileged locations.");
            }
        }
    }
}