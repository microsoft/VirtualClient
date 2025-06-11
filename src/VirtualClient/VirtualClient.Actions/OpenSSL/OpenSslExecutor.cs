// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Executes the OpenSSL workload.
    /// <list type="bullet">
    /// <item>
    /// <a href='https://www.openssl.org/docs/manmaster/'>OpenSSL manual</a>
    /// </item>
    /// <item>
    /// <a href='https://www.openssl.org/docs/manmaster/man1/openssl-speed.html'>OpenSSL speed command options</a>
    /// </item>
    /// </list>
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-x64")]
    public class OpenSslExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public OpenSslExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandArguments
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(OpenSslExecutor.CommandArguments));
            }
        }

        /// <summary>
        /// The path to the OpenSSL executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the OpenSSL package that contains the workload
        /// executable/binaries.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Executes the OpenSSL workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the OpenSSL workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            await this.InitializeWorkloadToolsetsAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets openssl version by running openssl version command.
        /// </summary>
        private async Task GetOpenSslVersionAsync(string toolCommand, CancellationToken cancellationToken)
        {
            // The OpenSSL version is not available in the workload output. We need to run a separate command to get the version.   
            // The command 'openssl version' will return the version of OpenSSL installed on the system.
            string opensslVersion = "Unknown";
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process 'openssl version' at directory '{this.ExecutablePath}'.");
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(this.ExecutablePath, "version"))
                {
                    this.SetEnvironmentVariables(process);
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                    process.ThrowIfWorkloadFailed();

                    opensslVersion = process.StandardOutput?.ToString().Trim() ?? "Unknown";
                    if (string.IsNullOrWhiteSpace(opensslVersion))
                    {
                        opensslVersion = "Unknown";
                    }

                    this.MetadataContract.Add("OpenSSLVersion", opensslVersion, MetadataContractCategory.Dependencies);
                    this.Logger.LogMessage($"{nameof(OpenSslExecutor)}.GetOpenSslVersionAsync", LogLevel.Information, EventContext.Persisted().AddContext("opensslVersion", opensslVersion));

                    this.MetadataContract.AddForScenario(
                       "OpenSSL Speed",
                       toolCommand,
                       toolVersion: opensslVersion);
                }
            }
            
        }

        private async Task CaptureMetricsAsync(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    // Retrieve OpenSSL version
                    await this.GetOpenSslVersionAsync(workloadProcess.FullCommand(), cancellationToken);
                
                    this.MetadataContract.Apply(telemetryContext);

                    OpenSslMetricsParser resultsParser = new OpenSslMetricsParser(workloadProcess.StandardOutput.ToString(), commandArguments);
                    IList<Metric> metrics = resultsParser.Parse();

                    this.Logger.LogMetrics(
                        "OpenSSL",
                        this.MetricScenario ?? this.Scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);

                    metrics.LogConsole(this.Scenario, "OpenSSL");
                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(OpenSslExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                }
            }
        }

        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandArguments = this.GetCommandArguments();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);       

            return this.Logger.LogMessageAsync($"{nameof(OpenSslExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                    {
                        this.SetEnvironmentVariables(process);
                        this.CleanupTasks.Add(() => process.SafeKill());

                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "OpenSSL", logToFile: true);

                                process.ThrowIfWorkloadFailed();
                                await this.CaptureMetricsAsync(process, commandArguments, telemetryContext, cancellationToken);
                            }
                        }
                        finally
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                            }
                        }
                    }
                }
            });
        }

        private string GetCommandArguments()
        {
            string commandArguments = this.CommandArguments;
            if (this.Platform == PlatformID.Unix)
            {
                int indexOfCommand = commandArguments.IndexOf("speed", StringComparison.OrdinalIgnoreCase);

                // We want to utilize ALL vCPUs/cores on the system in order to work the processor hard. However,
                // the -multi flag is NOT supported on Windows builds of the OpenSSL toolset for some reason. To make things
                // even more challenging, there is a bug in OpenSSL that will cause it to use the parameter name 'evp' for the
                // name of the cipher (instead of the cipher name itself) in the output when the '-evp' parameter is used. As
                // such, we are just ensuring the cipher name is at the end of the command arguments. The name of the cipher
                // MUST be defined last in the profile.
                //
                // Example:
                // profile: speed -elapsed -seconds 10 aes-256-cbc  ->  speed -multi 4 -elapsed -seconds 10 aes-256-cbc
                commandArguments = commandArguments.Insert(indexOfCommand + 6, $"-multi {Environment.ProcessorCount} ");
            }

            return commandArguments;
        }

        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false);

                if (workloadPackage == null)
                {
                    throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                }

                workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

                this.Package = workloadPackage;
            }
        }

        private async Task InitializeWorkloadToolsetsAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, "bin", "openssl");

                    this.fileSystem.File.ThrowIfFileDoesNotExist(
                        this.ExecutablePath,
                        $"OpenSSL executable not found at path '{this.ExecutablePath}'");

                    await this.systemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (this.Platform == PlatformID.Win32NT)
                {
                    this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, "bin", "openssl.exe");

                    this.fileSystem.File.ThrowIfFileDoesNotExist(
                       this.ExecutablePath,
                       $"OpenSSL executable not found at path '{this.ExecutablePath}'");
                }
            }
        }

        private void SetEnvironmentVariables(IProcessProxy process)
        {
            if (this.Platform == PlatformID.Win32NT)
            {
                // The OpenSSL toolset we use was compiled using the Visual Studio 2019 C++ compiler. This creates a runtime
                // dependency on the VC runtime .dlls. These are packaged with the OpenSSL package itself in the 'vcruntime'
                // folder. We append the location of these VC runtime .dlls to the PATH environment variable so they can be
                // found.
                string vcRuntimeDllPath = this.PlatformSpecifics.Combine(this.Package.Path, "vcruntime");
                string currentPathValue = process.EnvironmentVariables["Path"]?.TrimEnd(';');

                this.Logger.LogTraceMessage($"Setting Environment Variable:Path={currentPathValue};{vcRuntimeDllPath}", EventContext.Persisted());
                process.EnvironmentVariables["Path"] = $"{currentPathValue};{vcRuntimeDllPath}";
            }
            else if (this.Platform == PlatformID.Unix)
            {
                // OpenSSL is typically installed on Linux systems by default. However, we want to use the package version that
                // we compiled to ensure we are always running the exact same toolset across Windows and Linux systems. To ensure
                // the package version of OpenSSL we use can find its lib/library files required to run, we have to set a special
                // environment variable $LD_LIBRARY_PATH
                string libPath = this.PlatformSpecifics.Combine(this.Package.Path, "lib64");

                this.Logger.LogTraceMessage($"Setting Environment Variable:LD_LIBRARY_PATH={libPath}", EventContext.Persisted());
                process.EnvironmentVariables["LD_LIBRARY_PATH"] = libPath;
            }
        }
    }
}