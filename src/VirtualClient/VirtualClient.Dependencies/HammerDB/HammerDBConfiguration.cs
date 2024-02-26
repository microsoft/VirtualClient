// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MathNet.Numerics.Distributions;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for configuring PostgreSQL Server.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class HammerDBConfiguration : ExecuteCommand
    {
        private ISystemManagement systemManager;
        private IPackageManager packageManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="HammerDBConfiguration"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public HammerDBConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.systemManager.ThrowIfNull(nameof(this.systemManager));
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
        }

        /// <summary>
        /// Parameter defines the benchmark to use.
        /// </summary>
        public string Benchmark
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Benchmark), out IConvertible benchmark);
                return benchmark?.ToString();
            }
        }

        /// <summary>
        /// Parameter defines the SQLServer to use.
        /// </summary>
        public string SQLServer
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.SQLServer), out IConvertible sqlServer);
                return sqlServer?.ToString();
            }
        }

        /// <summary>
        /// The path to the PostgreSQL package for configuration.
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// Initializes PostgreSQL configuration requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.PackagePath = package.Path;
        }

        /// <summary>
        /// Executes PostgreSQL configuration steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ConfigurationState state = await this.stateManager.GetStateAsync<ConfigurationState>(nameof(HammerDBConfiguration), cancellationToken);

            if (state == null)
            {
                await this.ConfigureWorkloadAsync(telemetryContext, cancellationToken);

                await this.stateManager.SaveStateAsync(
                    nameof(HammerDBConfiguration),
                    new Item<ConfigurationState>(nameof(HammerDBConfiguration), new ConfigurationState()),
                    cancellationToken);
            }
        }

        private async Task ConfigureWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string configurationScript = this.Combine(this.PackagePath, "configureWorkloadGenerator.py");
            string tclPath = this.Combine(this.PackagePath, "benchmarks", this.Benchmark.ToLowerInvariant(), this.SQLServer.ToLowerInvariant(), "createDB.tcl");

            string command = "python3";
            string commandArguments = $"{configurationScript} --tclPath {tclPath}";
            string workingDirectory = this.Combine(this.PackagePath);

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", command)
                .AddContext("commandArguments", commandArguments)
                .AddContext("workingDirectory", workingDirectory);

            await this.systemManager.MakeFileExecutableAsync(configurationScript, this.Platform, cancellationToken);

            await this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, relatedContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }

        internal class ConfigurationState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ConfigurationState"/> object.
            /// </summary>
            public ConfigurationState()
                : base()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfigurationState"/> object.
            /// </summary>
            [JsonConstructor]
            public ConfigurationState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }
        }
    }
}
