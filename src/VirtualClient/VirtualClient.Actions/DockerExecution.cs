// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes child components within a Docker container environment.
    /// </summary>
    [SupportedPlatforms("linux-x64,linux-arm64")]
    internal class DockerExecution : VirtualClientComponentCollection
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerExecution"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">Parameters defined in the execution profile or supplied to the Virtual Client on the command line.</param>
        public DockerExecution(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The Docker image to use for container execution.
        /// </summary>
        public string Image
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Image));
            }
        }

        /// <summary>
        /// Optional volume mount paths. Format: "/host/path:/container/path".
        /// </summary>
        public string VolumeMounts
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.VolumeMounts), string.Empty);
            }
        }

        /// <summary>
        /// Optional environment variables for the container. Format: "VAR1=value1,VAR2=value2".
        /// </summary>
        public string EnvironmentVariables
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.EnvironmentVariables), string.Empty);
            }
        }

        /// <summary>
        /// Initializes Docker container execution requirements.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Validate Docker image is specified
            if (string.IsNullOrWhiteSpace(this.Image))
            {
                throw new ArgumentException("Docker image (Image parameter) is required for DockerExecution.");
            }

            // Validate that at least one child component is defined
            if (this.Count == 0)
            {
                throw new ArgumentException("DockerExecution must contain at least one child component.");
            }

            this.Logger?.LogInformation(
                $"DockerExecution: Initializing container execution. Image={this.Image}, Components={this.Count}");

            // Phase 1: Verify Docker is installed and running
            // Phase 2: Build/pull the Docker image
            // Phase 3: Prepare volume mounts
            // Future phases will implement actual container execution

            return base.InitializeAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes child components within the Docker container.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger?.LogInformation(
                $"DockerExecution: Starting container execution. Image={this.Image}");

            // MVP: For now, execute child components on the host
            // This demonstrates that DockerExecution properly wraps and delegates to child components
            // Future implementation will:
            // 1. Create a Docker container from the image
            // 2. Mount volumes for packages, logs, state
            // 3. Execute child components inside the container via 'docker exec'
            // 4. Capture container stdout/stderr for error telemetry
            // 5. Cleanup container (or keep alive if --keepContainerAlive flag set)

            // Execute each child component sequentially
            foreach (VirtualClientComponent component in this)
            {
                this.Logger?.LogInformation(
                    $"DockerExecution: Executing child component. Component={component.GetType().Name}");

                await component.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            this.Logger?.LogInformation($"DockerExecution: Container execution completed.");
        }

        /// <summary>
        /// Cleans up Docker container resources.
        /// </summary>
        protected override Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger?.LogInformation($"DockerExecution: Cleaning up container resources.");

            // Future: Remove Docker container and cleanup resources
            // await this.StopContainerAsync(cancellationToken).ConfigureAwait(false);
            // await this.RemoveContainerAsync(cancellationToken).ConfigureAwait(false);

            return base.CleanupAsync(telemetryContext, cancellationToken);
        }
    }
}
