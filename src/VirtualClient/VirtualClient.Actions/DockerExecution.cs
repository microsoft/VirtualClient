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
    using VirtualClient.Common.Docker;
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
        private DockerContainerClient dockerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerExecution"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">Parameters defined in the execution profile or supplied to the Virtual Client on the command line.</param>
        public DockerExecution(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.dockerClient = new DockerContainerClient(this.Logger);
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

            return base.InitializeAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes child components within the Docker container via docker exec.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger?.LogInformation(
                $"DockerExecution: Starting container execution. Image={this.Image}");

            // Get container ID from environment (set by DockerCommand)
            string containerId = Environment.GetEnvironmentVariable("VC_DOCKER_CONTAINER_ID");

            if (string.IsNullOrWhiteSpace(containerId))
            {
                this.Logger?.LogWarning(
                    "DockerExecution: No container ID found. Executing components on host (non-container mode).");

                // Fallback: execute on host if no container context
                await this.ExecuteComponentsOnHostAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Execute components inside container via docker exec
                await this.ExecuteComponentsInContainerAsync(containerId, cancellationToken).ConfigureAwait(false);
            }

            this.Logger?.LogInformation($"DockerExecution: Container execution completed.");
        }

        /// <summary>
        /// Cleans up Docker container resources.
        /// </summary>
        protected override Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger?.LogInformation($"DockerExecution: Cleaning up container resources.");
            return base.CleanupAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes components on the host (fallback mode).
        /// </summary>
        private async Task ExecuteComponentsOnHostAsync(CancellationToken cancellationToken)
        {
            foreach (VirtualClientComponent component in this)
            {
                this.Logger?.LogInformation(
                    $"DockerExecution: Executing child component on host. Component={component.GetType().Name}");

                await component.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Executes components inside the Docker container.
        /// </summary>
        private async Task ExecuteComponentsInContainerAsync(string containerId, CancellationToken cancellationToken)
        {
            foreach (VirtualClientComponent component in this)
            {
                string componentName = component.GetType().Name;
                this.Logger?.LogInformation(
                    $"DockerExecution: Executing child component in container. Component={componentName}, Container={containerId}");

                try
                {
                    // Build command to execute component in container
                    // Note: For MVP, we execute components individually
                    // Full implementation would handle parameters and profiles
                    string command = $"/app/VirtualClient.Main --component={componentName}";

                    this.Logger?.LogInformation($"DockerExecution: Running docker exec: {command}");

                    var execResult = await this.dockerClient.ExecuteInContainerAsync(
                        containerId,
                        command,
                        cancellationToken).ConfigureAwait(false);

                    // Capture output for logging and telemetry
                    if (!execResult.Success)
                    {
                        this.Logger?.LogError(
                            $"DockerExecution: Component execution failed in container. " +
                            $"Component={componentName}, ExitCode={execResult.ExitCode}, " +
                            $"Error={execResult.StandardError}");

                        // Emit telemetry event for error
                        this.Logger?.LogError($"Container Error Output: {execResult.StandardError}");

                        throw new InvalidOperationException(
                            $"Component {componentName} failed inside container. Exit code: {execResult.ExitCode}. " +
                            $"Error: {execResult.StandardError}");
                    }

                    if (!string.IsNullOrWhiteSpace(execResult.StandardOutput))
                    {
                        this.Logger?.LogInformation($"Container Output: {execResult.StandardOutput}");
                    }
                }
                catch (Exception ex)
                {
                    this.Logger?.LogError($"Exception executing component in container: {ex.Message}");
                    throw;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
