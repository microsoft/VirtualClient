namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    /// <summary>
    /// Virtual Client component that acquires an Azure access token for the specified Key Vault
    /// using interactive browser authentication with a device-code fallback.
    /// </summary>
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
        /// Gets the Azure tenant ID used to acquire an access token.
        /// </summary>
        protected string TenantId
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TenantId));
            }
        }

        /// <summary>
        /// Gets the Azure Key Vault URI for which the access token will be requested.
        /// Example: https://anyvault.vault.azure.net/
        /// </summary>
        protected string KeyVaultUri
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.KeyVaultUri));
            }
        }

        /// <summary>
        /// The name of the certificate to be retrieved
        /// </summary>
        protected string CertificateName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.KeyVaultUri));
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
                IKeyVaultManager keyVault = await this.GetKeyVaultManager(cancellationToken);
                X509Certificate2 certificate = await keyVault.GetCertificateAsync(this.CertificateName, cancellationToken);

                if (this.Platform == PlatformID.Win32NT)
                {
                    await this.InstallCertificateOnWindowsAsync(certificate, cancellationToken);
                }
                else if (this.Platform == PlatformID.Unix)
                {
                    await this.InstallCertificateOnUnixAsync(certificate, cancellationToken);
                }
                else
                {
                    throw new PlatformNotSupportedException($"The '{nameof(CertificateInstallation)}' component is not supported on platform '{this.Platform}'.");
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
        /// Installs the certificate in the appropriate certificate store on a Windows system.
        /// </summary>
        protected virtual Task InstallCertificateOnWindowsAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
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
        /// Installs the certificate in the appropriate certificate store on a Unix/Linux system.
        /// </summary>
        protected virtual async Task InstallCertificateOnUnixAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
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

                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }

                await this.fileSystem.File.WriteAllBytesAsync(
                    this.Combine(certificateDirectory, $"{certificate.Thumbprint}.pfx"),
                    certificate.Export(X509ContentType.Pfx));

                // Permissions 777 (-rwxrwxrwx)
                // https://linuxhandbook.com/linux-file-permissions/
                //
                // User  = read, write, execute
                // Group = read, write, execute
                // Other = read, write, execute+
                using (IProcessProxy process = this.processManager.CreateProcess("chmod", $"-R 777 {certificateDirectory}"))
                {
                    await process.StartAndWaitAsync(cancellationToken);
                    process.ThrowIfErrored<DependencyException>();
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException(
                    $"Access permissions denied for certificate directory '{certificateDirectory}'. Execute the installer with " +
                    $"sudo/root privileges to install SDK certificates in privileged locations.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<IKeyVaultManager> GetKeyVaultManager(CancellationToken cancellationToken)
        {
            IKeyVaultManager keyVaultManager = this.Dependencies.GetService<IKeyVaultManager>();
            keyVaultManager.ThrowIfNull(nameof(keyVaultManager));

            // need better if condition.
            if (keyVaultManager.StoreDescription != null)
            {
                return keyVaultManager;
            }
            else if (!string.IsNullOrWhiteSpace(this.LogFileName))
            {
                string directory = !string.IsNullOrWhiteSpace(this.LogFolderName)
                    ? this.LogFolderName
                    : this.fileSystem.Directory.GetCurrentDirectory();

                string accessTokenPath = this.Combine(directory, this.LogFileName);

                string keyVaultUri = string.IsNullOrWhiteSpace(this.KeyVaultUri) 
                    ? ((DependencyKeyVaultStore)keyVaultManager.StoreDescription).EndpointUri.ToString()
                    : this.KeyVaultUri;

                keyVaultUri.ThrowIfNullOrWhiteSpace(nameof(this.KeyVaultUri));

                string token_ = await this.fileSystem.File.ReadAllTextAsync(accessTokenPath, cancellationToken).ConfigureAwait(false);
                AccessTokenCredential tokenCredential = new AccessTokenCredential(token_);

                DependencyKeyVaultStore dependencyKeyVault = new DependencyKeyVaultStore(DependencyStore.KeyVault, new Uri(this.KeyVaultUri), tokenCredential);
                return new KeyVaultManager(dependencyKeyVault);
            }
            else
            {
                throw new InvalidOperationException($"The Key Vault manager has not been properly initialized. The '{nameof(this.LogFileName)}' parameter must be provided to read the access token from file.");
            }
        }
    }
}