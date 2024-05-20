// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.Packaging;

    /// <summary>
    /// Provides functionality for installing the JDK on the system.
    /// </summary>
    public class JDKPackageDependencyInstallation : DependencyPackageInstallation
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JDKPackageDependencyInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public JDKPackageDependencyInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes the Java Runtime installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Download and Install JDK package from blob container
            if (this.Parameters.ContainsKey(nameof(this.BlobName)) && 
                this.Parameters.ContainsKey(nameof(this.BlobContainer)))
            {
                await base.ExecuteAsync(telemetryContext, cancellationToken);
            }

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath jdkPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            if (jdkPackage == null)
            {
                throw new DependencyException(
                    $"The JDK package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }
            else if (!jdkPackage.Metadata.ContainsKey(PackageMetadata.ExecutablePath))
            {
                // If the JDK package state.json has ExecutablePath, it means it was already registered. Skipping saving twice.
                jdkPackage = this.PlatformSpecifics.ToPlatformSpecificPath(jdkPackage, this.Platform, this.CpuArchitecture);

                string javaDirectory = this.PlatformSpecifics.Combine(jdkPackage.Path, "bin");
                string javaExecutable;

                if (this.Platform == PlatformID.Unix)
                {
                    javaExecutable = this.PlatformSpecifics.Combine(javaDirectory, "java");
                }
                else
                {
                    javaExecutable = this.PlatformSpecifics.Combine(javaDirectory, "java.exe");
                }

                this.SetEnvironmentVariable(EnvironmentVariable.JAVA_HOME, jdkPackage.Path, EnvironmentVariableTarget.Process);
                this.SetEnvironmentVariable(EnvironmentVariable.JAVA_EXE, javaExecutable, EnvironmentVariableTarget.Process);
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, javaDirectory, append: true);

                // Ensure the binary can execute (e.g. chmod +x)
                await this.systemManager.MakeFileExecutableAsync(javaExecutable, this.Platform, cancellationToken)
                    .ConfigureAwait(false);

                DependencyPath javaExecutablePackage = new DependencyPath(
                    this.PackageName,
                    jdkPackage.Path,
                    "Java Development Kit",
                    metadata: new Dictionary<string, IConvertible>()
                    {
                        { PackageMetadata.ExecutablePath, javaExecutable }
                    });

                telemetryContext.AddContext("package", javaExecutable);

                await packageManager.RegisterPackageAsync(javaExecutablePackage, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
