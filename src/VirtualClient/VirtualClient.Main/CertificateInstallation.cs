// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using System.IO;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Azure;
    using Azure.Security.KeyVault.Secrets;
    using Polly;
    using VirtualClient;
    using VirtualClient.Identity;
    using Serilog.Core;
    using VirtualClient.Common.Telemetry;
    using Azure.Core;
    using Azure.Identity;

    /// <summary>
    /// Command resets the Virtual Client environment for "first time" run scenarios.
    /// </summary>
    public class CertificateInstallation : CommandBase
    {
        /// <summary>
        /// Logger
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Certificate Name.
        /// </summary>
        protected string CertificateName { get; set; }

        /// <summary>
        /// Executes the operations to reset the environment.
        /// Installs certificates required by Virtual Client on the local system.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IServiceCollection dependencies = this.InitializeDependencies(args);
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();
            PlatformID platform = platformSpecifics.Platform;

            IKeyVaultManager keyVault = dependencies.GetService<IKeyVaultManager>();

            if (keyVault == null)
            {
                throw new DependencyException("Key Vault manager is not available.", ErrorReason.DependencyNotFound);
            }

            X509Certificate2 certificate = await keyVault.GetCertificateAsync(this.CertificateName, cancellationToken);

            try
            {
                Program.LogMessage(this.Logger, $"[Installing Certificates]", EventContext.Persisted());

                if (platform == PlatformID.Unix)
                {
                    await this.InstallCertificateOnUnixAsync(certificate, dependencies, cancellationToken);
                }
                else if (platform == PlatformID.Win32NT)
                {
                    await this.InstallCertificateOnWindowsAsync(certificate);
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
                    $"Certificate installation failed. Local certificate store access permissions denied. Virtual Client must be " +
                    $"running with Administrative privileges in order to install certificates in the current context.",
                    ErrorReason.DependencyInstallationFailed);
            }

            return 0;
        }

        /// <summary>
        /// Installs the certificate in the appropriate certificate store on a Unix/Linux system.
        /// Handles both root and sudo scenarios.
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

                Program.LogMessage(this.Logger, $"Certificate Store = {certificateDirectory}", EventContext.Persisted());

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
                    $"Access permissions denied for certificate directory '{certificateDirectory}'. Run Virtual Client with " +
                    $"sudo/root privileges to install certificates in privileged locations.");
            }
        }

        /// <summary>
        /// Installs the certificate in the appropriate certificate store on a Windows system.
        /// </summary>
        protected virtual Task InstallCertificateOnWindowsAsync(X509Certificate2 certificate)
        {
            return Task.Run(() =>
            {
                Program.LogMessage(this.Logger, $"Certificate Store = CurrentUser/Personal", EventContext.Persisted());
                using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadWrite))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }
            });
        }
    }
}