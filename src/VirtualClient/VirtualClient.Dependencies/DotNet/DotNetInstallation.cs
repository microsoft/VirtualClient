// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for installing dotnet SDK.
    /// https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
    /// </summary>
    public class DotNetInstallation : VirtualClientComponent
    {
        private const string LinuxInstallScriptName = "dotnet-install.sh";
        private const string WindowsInstallScriptName = "dotnet-install.ps1";

        private string installDirectory;
        private ISystemManagement systemManager;
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public DotNetInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();

            this.installDirectory = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "dotnet");
        }

        /// <summary>
        /// The version of the dotnet.
        /// Check these for valid versions
        /// https://dotnet.microsoft.com/en-us/download/dotnet/5.0
        /// https://dotnet.microsoft.com/en-us/download/dotnet/6.0
        /// </summary>
        public string DotNetVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DotNetInstallation.DotNetVersion), "6.0.100");
            }
        }

        /// <summary>
        /// Executes the compiler installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.fileSystem.Directory.Exists(this.installDirectory))
            {
                this.fileSystem.Directory.CreateDirectory(this.installDirectory);
            }

            string installScript = (this.Platform == PlatformID.Unix) ? LinuxInstallScriptName : WindowsInstallScriptName;
            string originFile = this.PlatformSpecifics.Combine(this.PlatformSpecifics.GetScriptPath("dotnet"), installScript);
            string destinyFile = this.PlatformSpecifics.Combine(this.installDirectory, installScript);
            this.fileSystem.File.Copy(originFile, destinyFile, true);

            if (this.Platform == PlatformID.Unix)
            {
                await this.systemManager.MakeFileExecutableAsync(destinyFile, this.Platform, cancellationToken).ConfigureAwait(false);
                await this.ExecuteCommandAsync(destinyFile, this.GetInstallArgument(), this.installDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await this.ExecuteCommandAsync("powershell", $"{destinyFile} {this.GetInstallArgument()}", this.installDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);
            }

            DependencyPath dotnetPackage = new DependencyPath(this.PackageName, this.installDirectory, "DotNet SDK", this.DotNetVersion);
            await this.systemManager.PackageManager.RegisterPackageAsync(dotnetPackage, cancellationToken).ConfigureAwait(false);
        }

        private string GetInstallArgument()
        {
            string argument = string.Empty;

            // --version 6.0.100 --install-dir /vc/packages/dotnet --architecture x64
            // The powershell on Win11 ARM has a bug where it shows amd64 as Env:PROCESSOR_ARCHITECTURE, so enforcing the architecture to be explicit.
            if (this.Platform == PlatformID.Unix)
            {
                argument = $"--version {this.DotNetVersion} --install-dir {this.installDirectory} --architecture {this.CpuArchitecture.ToString().ToLower()}";
            }
            else
            {
                argument = $"-Version {this.DotNetVersion} -InstallDir {this.installDirectory} -Architecture {this.CpuArchitecture.ToString().ToLower()}";
            }

            return argument;
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
            {
                SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.", EventContext.Persisted());

                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails<DotNetInstallation>(process, relatedContext);
                    process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }
    }
}