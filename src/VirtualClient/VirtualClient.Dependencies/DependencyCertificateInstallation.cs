// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Provides functionality for downloading and installing dependency certificates from
    /// a cloud keyvault store onto the system.
    /// </summary>
    public class DependencyCertificateInstallation : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyCertificateInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public DependencyCertificateInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The type of archival file format the blob is in.
        /// </summary>
        public ArchiveType ArchiveType
        {
            get
            {
                return this.Parameters.GetEnumValue<ArchiveType>(nameof(DependencyCertificateInstallation.ArchiveType), ArchiveType.Zip);
            }
        }

        /// <summary>
        /// The name of the blob package in the storage account.
        /// </summary>
        public string CertificateName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DependencyCertificateInstallation.CertificateName));
            }

            set
            {
                this.Parameters[nameof(DependencyCertificateInstallation.CertificateName)] = value;
            }
        }

        /// <summary>
        /// The name of the container in which the blob package exists in 
        /// the storage account.
        /// </summary>
        public string KeyVaultName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DependencyCertificateInstallation.KeyVaultName));
            }

            set
            {
                this.Parameters[nameof(DependencyCertificateInstallation.KeyVaultName)] = value;
            }
        }

        /// <summary>
        /// The path in which to install the package.
        /// </summary>
        public string InstallationPath
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DependencyCertificateInstallation.InstallationPath), string.Empty);
            }

            set
            {
                this.Parameters[nameof(DependencyCertificateInstallation.InstallationPath)] = value;
            }
        }

        /// <summary>
        /// Whether the blob should be extracted.
        /// </summary>
        public bool Extract
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(DependencyCertificateInstallation.Extract), true);
            }

            set
            {
                this.Parameters[nameof(DependencyCertificateInstallation.Extract)] = value;
            }
        }

        /// <summary>
        /// Defines an access token to use for authentication + authorization with 
        /// Azure resources.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Flag indicates whether the installer is running unattended. This scenario
        /// is used with installations via other automation to ensure the installer does
        /// not block on exiting.
        /// </summary>
        public bool Unattended { get; set; }

        /// <summary>
        /// A retry policy to apply to transient issues with accessing secrets in
        /// an Azure Key Vault.
        /// </summary>
        protected IAsyncPolicy KeyVaultAccessRetryPolicy { get; set; }

        /// <summary>
        /// Executes the blob package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            MetadataContract.Persist(
                $"package_{this.PackageName}",
                this.BlobName,
                MetadataContract.DependenciesCategory,
                true);

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            IFileSystem fileSystem = this.Dependencies.GetService<IFileSystem>();

            DependencyPath existingPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext("packageDirectory", this.PlatformSpecifics.GetPackagePath());
            telemetryContext.AddContext("packageExists", existingPackage != null);
            telemetryContext.AddContext("package", existingPackage);

            // If a built-in package exists, we do not currently override it with a Blob package
            // downloaded to the system.
            if (existingPackage == null || !fileSystem.Directory.Exists(existingPackage.Path))
            {
                BlobDescriptor packageDescription = new BlobDescriptor
                {
                    Name = this.BlobName,
                    ContainerName = this.BlobContainer,
                    PackageName = this.PackageName,
                    ArchiveType = this.ArchiveType,
                    Extract = this.Extract
                };

                telemetryContext.AddContext("package", packageDescription);

                string installationPath = null;
                if (!string.IsNullOrWhiteSpace(this.InstallationPath))
                {
                    IDiskManager diskManager = this.Dependencies.GetService<IDiskManager>();
                    IEnumerable<Disk> disks = await diskManager.GetDisksAsync(cancellationToken).ConfigureAwait(false);
                    if (!DependencyCertificateInstallation.TryResolveRelativeDiskLocation(disks, this.InstallationPath, this.Platform, out installationPath))
                    {
                        throw new WorkloadException(
                            $"The installation path provided '{this.InstallationPath}' cannot be resolved",
                            ErrorReason.DependencyInstallationFailed);
                    }
                }

                if (!this.TryGetPackageStoreManager(out IBlobManager blobManager))
                {
                    throw new DependencyException(
                        $"Package store not defined. The package '{packageDescription.Name}' cannot be installed because the package store information " +
                        $"was not provided to the application on the command line (e.g. --packages).",
                        ErrorReason.PackageStoreNotDefined);
                }

                string packageLocation = await packageManager.InstallPackageAsync(blobManager, packageDescription, cancellationToken, installationPath)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(packageLocation))
                {
                    throw new DependencyException(
                        $"Blob package installation failed for package '{this.BlobName}'.",
                        ErrorReason.DependencyInstallationFailed);
                }

                telemetryContext.AddContext("packageLocation", packageLocation);
            }
        }

        /// <summary>
        /// Downloads the dependency from the container specified to the path defined.
        /// </summary>
        /// <param name="blobManager">The blob manager to use for downloading the package dependency.</param>
        /// <param name="description">The dependency description.</param>
        /// <param name="downloadPath">Provides the location where the package should be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task<Stream> DownloadDependencyPackageAsync(IBlobManager blobManager, DependencyDescriptor description, string downloadPath, CancellationToken cancellationToken)
        {
            FileStream stream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            await blobManager.DownloadBlobAsync(description, stream, cancellationToken)
                .ConfigureAwait(false);

            return stream;
        }

        /// <summary>
        /// Installs certificates required by the CRC SDK on the local system.
        /// </summary>
        /// <param name="settings">Provides configuration settings to the installer operations.</param>
        /// <param name="dependencies">Provides dependencies required to download and install certificates.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task InstallCertificatesAsync(InstallerSettings settings, IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            this.WriteStandardOutput();
            this.WriteStandardOutput($"[Downloading Certificates]");
            this.WriteStandardOutput($"Certificate = {settings.InstallerCertificateName}");

            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();
            PlatformID platform = platformSpecifics.Platform;

            X509Certificate2 certificate = await this.DownloadCertificateAsync(settings, platform, cancellationToken);

            try
            {
                this.WriteStandardOutput();
                this.WriteStandardOutput($"[Installing Certificates]");

                if (platform == PlatformID.Unix)
                {
                    await this.InstallCertificateOnUnixAsync(certificate, dependencies, cancellationToken);
                }
                else if (platform == PlatformID.Win32NT)
                {
                    await this.InstallCertificateOnWindowsAsync(certificate, cancellationToken);
                }
                else
                {
                    throw new DependencyException(
                        $"Certificate installation for OS platform '{platform}' is not supported.",
                        ErrorReason.NotSupported);
                }
            }
            catch (CryptographicException exc) when (exc.Message.Contains("access", StringComparison.OrdinalIgnoreCase))
            {
                throw new DependencyException(
                    $"Certificate installation failed. Local certificate store access permissions denied. The CRC SDK installer must be " +
                    $"run with Administrative privileges in order to install certificates in the current context.",
                    ErrorReason.DependencyInstallationFailed);
            }
        }

        /// <summary>
        /// Installs the certificate in the appropriate certificate store on a Windows system.
        /// </summary>
        protected virtual Task InstallCertificateOnWindowsAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                this.WriteStandardOutput($"Certificate Store = CurrentUser/Personal");
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
        protected virtual async Task InstallCertificateOnUnixAsync(X509Certificate2 certificate, IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();
            ProcessManager processManager = dependencies.GetService<ProcessManager>();
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();

            // On Unix/Linux systems, we install ther certificate in the default location for the
            // user as well as in a static location. In the future we will likely use the static location
            // only.
            string certificateDirectory = null;

            try
            {
                // When "sudo" is used to run the installer, we need to know the logged
                // in user account. On Linux systems, there is an environment variable 'SUDO_USER'
                // that defines the logged in user.
                string user = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.USER);
                string sudoUser = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.SUDO_USER);
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

                this.WriteStandardOutput($"Certificate Store = {certificateDirectory}");

                if (!fileSystem.Directory.Exists(certificateDirectory))
                {
                    fileSystem.Directory.CreateDirectory(certificateDirectory);
                }

                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }

                await fileSystem.File.WriteAllBytesAsync(
                    platformSpecifics.Combine(certificateDirectory, $"{certificate.Thumbprint}.pfx"),
                    certificate.Export(X509ContentType.Pfx));

                // Permissions 777 (-rwxrwxrwx)
                // https://linuxhandbook.com/linux-file-permissions/
                //
                // User  = read, write, execute
                // Group = read, write, execute
                // Other = read, write, execute
                using (IProcessProxy process = processManager.CreateProcess("chmod", $"-R 777 {certificateDirectory}"))
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
    }
}