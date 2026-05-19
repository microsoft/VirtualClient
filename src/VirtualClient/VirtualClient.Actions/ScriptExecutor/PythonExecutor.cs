// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
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
    /// The Generic Script executor for Python
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class PythonExecutor : ScriptExecutor
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="PythonExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public PythonExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The parameter specifies whether to use python3, by default it is true
        /// </summary>
        public bool UsePython3
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.UsePython3), true);
            }
        }

        /// <summary>
        /// The name of the registered Python runtime package. Default is "python3".
        /// </summary>
        public string PythonPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.PythonPackageName), "python3");
            }
        }

        /// <summary>
        /// Executes the Python script.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = this.UsePython3 ? "python3" : "python";
                command = await this.ResolvePythonExecutableAsync(command, cancellationToken);

                string commandArguments = $"{this.ExecutablePath} {this.CommandLine}";

                telemetryContext
                   .AddContext(nameof(command), command)
                   .AddContext(nameof(commandArguments), SensitiveData.ObscureSecrets(commandArguments));

                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.ExecutableDirectory, telemetryContext, cancellationToken, this.RunElevated))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, this.ToolName);
                            process.ThrowIfWorkloadFailed();
                        }
                        finally
                        {
                            await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken);
                            await this.CaptureLogsAsync(cancellationToken);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to resolve the Python executable from the registered package.
        /// Falls back to the bare command name if the package is not registered or the executable is not found.
        /// </summary>
        private async Task<string> ResolvePythonExecutableAsync(string defaultCommand, CancellationToken cancellationToken)
        {
            DependencyPath pythonPackage = await this.GetPackageAsync(this.PythonPackageName, cancellationToken, throwIfNotfound: false);

            if (pythonPackage != null)
            {
                IFileSystem fileSystem = this.systemManagement.FileSystem;
                DependencyPath platformSpecificPackage = this.ToPlatformSpecificPath(pythonPackage, this.Platform, this.CpuArchitecture);

                string packagePath = fileSystem.Directory.Exists(platformSpecificPackage.Path)
                    ? platformSpecificPackage.Path
                    : pythonPackage.Path;

                string executableName = this.Platform == PlatformID.Win32NT ? "python.exe" : defaultCommand;
                string pythonExecutable = this.Combine(packagePath, executableName);

                if (fileSystem.File.Exists(pythonExecutable))
                {
                    return pythonExecutable;
                }
            }

            return defaultCommand;
        }
    }
}