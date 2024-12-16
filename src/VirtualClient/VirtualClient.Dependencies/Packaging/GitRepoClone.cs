// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for cloning a git repo.
    /// on the system.
    /// </summary>
    public class GitRepoClone : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitRepoClone"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public GitRepoClone(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The Git repo URI.
        /// </summary>
        public Uri RepoUri
        {
            get
            {
                return new Uri(this.Parameters.GetValue<string>(nameof(GitRepoClone.RepoUri)));
            }

            set
            {
                this.Parameters[nameof(GitRepoClone.RepoUri)] = value?.AbsoluteUri;
            }
        }

        /// <summary>
        /// Git checkout
        /// </summary>
        public string Checkout
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(GitRepoClone.Checkout), string.Empty);
            }

            set
            {
                this.Parameters[nameof(GitRepoClone.Checkout)] = value;
            }
        }

        /// <summary>
        /// Executes the git clone operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("repoUri", this.RepoUri);
            telemetryContext.AddContext("packagesDirectory", this.PlatformSpecifics.PackagesDirectory);
            telemetryContext.AddContext("checkout", this.Checkout);

            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            ProcessManager processManager = systemManagement.ProcessManager;

            string cloneDirectory = this.PlatformSpecifics.GetPackagePath(this.PackageName.ToLowerInvariant());

            if (systemManagement.FileSystem.Directory.Exists(cloneDirectory))
            {
                this.Logger.LogTraceMessage($"Git repo: '{this.PackageName}' already exist in directory '{cloneDirectory}'.", telemetryContext);
            }
            else
            {
                this.Logger.LogTraceMessage($"Clone repo '{this.RepoUri}' to directory '{cloneDirectory}'.", telemetryContext);

                if (!systemManagement.FileSystem.Directory.Exists(this.PlatformSpecifics.PackagesDirectory))
                {
                    systemManagement.FileSystem.Directory.CreateDirectory(this.PlatformSpecifics.PackagesDirectory);
                }

                using (IProcessProxy process = processManager.CreateProcess("git", $"clone {this.RepoUri} {cloneDirectory}", this.PlatformSpecifics.PackagesDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());

                    await process.StartAndWaitAsync(cancellationToken)
                       .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Git")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }

                    await systemManagement.PackageManager.RegisterPackageAsync(
                        new DependencyPath(
                            this.PackageName.ToLowerInvariant(),
                            cloneDirectory,
                            $"'{this.PackageName}' Git repo."),
                        cancellationToken).ConfigureAwait(false);
                }

                DependencyPath package = new DependencyPath(this.PackageName, this.Combine(this.PlatformSpecifics.PackagesDirectory, this.PackageName));
                await systemManagement.PackageManager.RegisterPackageAsync(package, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (this.Checkout != string.Empty)
            {
                using (IProcessProxy checkoutProcess = processManager.CreateProcess("git", $"-C {cloneDirectory} checkout {this.Checkout}", this.PlatformSpecifics.PackagesDirectory))
                {
                    this.CleanupTasks.Add(() => checkoutProcess.SafeKill());

                    await checkoutProcess.StartAndWaitAsync(cancellationToken)
                       .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(checkoutProcess, telemetryContext, "Git")
                            .ConfigureAwait(false);

                        checkoutProcess.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            }
        }
    }
}
