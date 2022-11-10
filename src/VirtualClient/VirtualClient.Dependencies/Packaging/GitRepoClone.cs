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
        /// Executes the git clone operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("repoUri", this.RepoUri);
            telemetryContext.AddContext("packagesDirectory", this.PlatformSpecifics.PackagesDirectory);

            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            string cloneDirectory = this.PlatformSpecifics.GetPackagePath(this.PackageName.ToLowerInvariant());

            if (systemManagement.FileSystem.Directory.Exists(cloneDirectory))
            {
                this.Logger.LogTraceMessage($"Git repo: '{this.PackageName}' already exist on directory '{cloneDirectory}'.", EventContext.Persisted());
            }
            else
            {
                this.Logger.LogTraceMessage($"Beginning Git clone repo: '{this.RepoUri}' to directory '{cloneDirectory}'.", EventContext.Persisted());

                if (!systemManagement.FileSystem.Directory.Exists(this.PlatformSpecifics.PackagesDirectory))
                {
                    systemManagement.FileSystem.Directory.CreateDirectory(this.PlatformSpecifics.PackagesDirectory);
                }

                using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(
                    this.Platform, "git", $"clone {this.RepoUri} {cloneDirectory}", this.PlatformSpecifics.PackagesDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());

                    await process.StartAndWaitAsync(cancellationToken)
                       .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.Logger.LogProcessDetails<GitRepoClone>(process, EventContext.Persisted());
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }

                    await systemManagement.PackageManager.RegisterPackageAsync(
                        new DependencyPath(
                            this.PackageName.ToLowerInvariant(),
                            cloneDirectory,
                            $"'{this.PackageName}' Git repo."),
                        cancellationToken).ConfigureAwait(false);

                    this.Logger.LogTraceMessage($"Git clone output: {process.StandardOutput}.", EventContext.Persisted());
                }

                DependencyPath package = new DependencyPath(this.PackageName, this.Combine(this.PlatformSpecifics.PackagesDirectory, this.PackageName));
                await systemManagement.PackageManager.RegisterPackageAsync(package, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
