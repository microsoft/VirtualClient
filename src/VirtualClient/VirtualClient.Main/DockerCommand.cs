// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Docker;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes a workload profile inside a Docker container.
    /// </summary>
    internal class DockerCommand : ExecuteProfileCommand
    {
        private DockerContainerClient dockerClient;
        private string containerId;

        /// <summary>
        /// The Docker image to use for container execution (e.g. ubuntu:noble, redis:7.0-alpine).
        /// </summary>
        public string DockerImage { get; set; }

        /// <summary>
        /// Whether to keep the container alive after execution for debugging.
        /// </summary>
        public bool KeepContainerAlive { get; set; }

        /// <summary>
        /// Initializes the command runtime before dependency initialization and execution.
        /// </summary>
        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            // Validate docker image is provided
            if (string.IsNullOrWhiteSpace(this.DockerImage))
            {
                throw new ArgumentException("Docker image (--image) is required for docker command execution.");
            }

            // Validate that at least one profile is specified
            if (this.Profiles == null || !this.Profiles.Any())
            {
                throw new ArgumentException("At least one profile (--profile) is required for docker command execution.");
            }

            // Set timeout to reasonable default if not specified
            if (this.Timeout == null)
            {
                this.Timeout = new ProfileTiming(TimeSpan.FromMinutes(30));
            }

            // Call parent initialization
            base.Initialize(args, platformSpecifics);
        }

        /// <summary>
        /// Executes the docker command: creates container, runs profile, cleans up, then flushes telemetry.
        /// </summary>
        protected override async Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            ILogger logger = dependencies.GetService<ILogger>();
            this.dockerClient = new DockerContainerClient(logger);
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            int exitCode = 0;

            try
            {
                // Step 1: Check Docker availability
                this.LogDockerInfo(logger, "Checking Docker availability...");
                bool dockerAvailable = await this.dockerClient.IsDockerAvailableAsync(cancellationToken).ConfigureAwait(false);

                if (!dockerAvailable)
                {
                    throw new InvalidOperationException(
                        "Docker is not available. Please ensure Docker is installed and the daemon is running. " +
                        "Run 'docker version' to verify your Docker installation.");
                }

                this.LogDockerInfo(logger, "Docker is available and running.");

                // Step 2: Validate profile — fail immediately if DockerExecution is in any action (double Docker)
                await this.ValidateProfileForDockerSubcommandAsync(dependencies, cancellationToken).ConfigureAwait(false);

                // Step 3: Create Docker container with volume mounts
                this.LogDockerInfo(logger, $"Creating Docker container from image: {this.DockerImage}");

                var volumeMounts = this.PrepareVolumeMounts(logger);
                var environmentVariables = new Dictionary<string, string>();

                this.containerId = await this.dockerClient.CreateContainerAsync(
                    this.DockerImage,
                    volumeMounts,
                    environmentVariables,
                    cancellationToken).ConfigureAwait(false);

                this.LogDockerInfo(logger, $"Docker container created successfully. Container ID: {this.containerId}");

                // Step 4: Set container ID in environment for child components
                Environment.SetEnvironmentVariable("VC_DOCKER_CONTAINER_ID", this.containerId);

                // Step 5: Inspect image to detect container platform and set env vars for package resolution
                this.LogDockerInfo(logger, $"Detecting container platform via: docker image inspect {this.DockerImage}");

                var (containerPlatform, containerArch) = await this.dockerClient.InspectImageAsync(
                    this.DockerImage, cancellationToken).ConfigureAwait(false);

                Environment.SetEnvironmentVariable("VC_DOCKER_PLATFORM", containerPlatform.ToString());
                Environment.SetEnvironmentVariable("VC_DOCKER_ARCH", containerArch.ToString());

                this.LogDockerInfo(logger,
                    $"Container platform detected: {containerPlatform}/{containerArch}. " +
                    $"Package resolution will use container platform.");

                // Step 6: Install profile dependencies on host (packages are volume-mounted into container)
                this.LogDockerInfo(logger, "Installing profile dependencies on host...");
                this.InstallDependencies = true;
                await base.ExecuteAsync(args, dependencies, cancellationTokenSource).ConfigureAwait(false);
                this.InstallDependencies = false;

                // Step 7: Execute profile actions inside container via docker exec
                this.LogDockerInfo(logger, "Executing profile actions inside container...");
                exitCode = await this.ExecuteActionsInContainerAsync(dependencies, logger, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Step 8: Cleanup container
                if (!string.IsNullOrWhiteSpace(this.containerId))
                {
                    await this.CleanupContainerAsync(logger, cancellationToken).ConfigureAwait(false);
                }
            }

            return exitCode;
        }

        /// <summary>
        /// Validates that none of the profile actions use DockerExecution, which conflicts with the docker subcommand.
        /// </summary>
        private async Task ValidateProfileForDockerSubcommandAsync(IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            IEnumerable<string> profilePaths = await this.EvaluateProfilesAsync(dependencies);

            foreach (string profilePath in profilePaths)
            {
                if (!File.Exists(profilePath))
                    continue;

                string json = await File.ReadAllTextAsync(profilePath, cancellationToken).ConfigureAwait(false);
                JsonNode root = JsonNode.Parse(json);
                JsonArray actions = root?["Actions"]?.AsArray();

                if (actions == null)
                    continue;

                foreach (JsonNode action in actions)
                {
                    string type = action?["Type"]?.GetValue<string>() ?? string.Empty;
                    if (type.Equals("DockerExecution", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Profile '{Path.GetFileName(profilePath)}' contains a 'DockerExecution' component " +
                            $"which conflicts with the 'docker' subcommand (double Docker). " +
                            $"Remove DockerExecution from the profile when using the docker subcommand.");
                    }
                }
            }
        }

        /// <summary>
        /// Executes each action component from the profiles inside the container via docker exec.
        /// </summary>
        private async Task<int> ExecuteActionsInContainerAsync(
            IServiceCollection dependencies, ILogger logger, CancellationToken cancellationToken)
        {
            IEnumerable<string> profilePaths = await this.EvaluateProfilesAsync(dependencies);

            foreach (string profilePath in profilePaths)
            {
                if (!File.Exists(profilePath))
                    continue;

                string json = await File.ReadAllTextAsync(profilePath, cancellationToken).ConfigureAwait(false);
                JsonNode root = JsonNode.Parse(json);
                JsonArray actions = root?["Actions"]?.AsArray();

                if (actions == null)
                    continue;

                foreach (JsonNode action in actions)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return 1;

                    string componentType = action?["Type"]?.GetValue<string>() ?? string.Empty;
                    if (string.IsNullOrEmpty(componentType))
                        continue;

                    this.LogDockerInfo(logger, $"Executing component in container: {componentType}");
                    string command = $"/app/VirtualClient.Main --component={componentType}";

                    var result = await this.dockerClient.ExecuteInContainerAsync(
                        this.containerId, command, cancellationToken).ConfigureAwait(false);

                    if (!result.Success)
                    {
                        throw new InvalidOperationException(
                            $"Component '{componentType}' failed in container. " +
                            $"Exit code: {result.ExitCode}. Error: {result.StandardError}");
                    }

                    if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                        this.LogDockerInfo(logger, $"Container output: {result.StandardOutput}");
                }
            }

            return 0;
        }

        /// <summary>
        /// Prepares volume mounts for container.
        /// </summary>
        private Dictionary<string, string> PrepareVolumeMounts(ILogger logger)
        {
            var volumeMounts = new Dictionary<string, string>();
            var currentDirectory = Environment.CurrentDirectory;

            // Standard mount points
            volumeMounts[Path.Combine(currentDirectory, "packages")] = "/mnt/packages";
            volumeMounts[Path.Combine(currentDirectory, "logs")] = "/mnt/logs";
            volumeMounts[Path.Combine(currentDirectory, "state")] = "/mnt/state";

            this.LogDockerInfo(logger,
                $"Volume mounts configured: packages, logs, state directories mounted at /mnt/");

            return volumeMounts;
        }

        /// <summary>
        /// Cleans up Docker container after execution.
        /// </summary>
        private async Task CleanupContainerAsync(ILogger logger, CancellationToken cancellationToken)
        {
            try
            {
                if (this.KeepContainerAlive)
                {
                    this.LogDockerInfo(logger,
                        $"Container is being kept alive for debugging. Container ID: {this.containerId}. " +
                        $"To manually inspect: docker exec -it {this.containerId} bash. " +
                        $"To cleanup: docker stop {this.containerId} && docker rm {this.containerId}");
                }
                else
                {
                    this.LogDockerInfo(logger, $"Stopping container: {this.containerId}");
                    await this.dockerClient.StopContainerAsync(this.containerId, cancellationToken).ConfigureAwait(false);

                    this.LogDockerInfo(logger, $"Removing container: {this.containerId}");
                    await this.dockerClient.RemoveContainerAsync(this.containerId, cancellationToken).ConfigureAwait(false);

                    this.LogDockerInfo(logger, "Container cleaned up successfully.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Container cleanup encountered an error: {ex.Message}");
                if (!this.KeepContainerAlive)
                {
                    logger?.LogWarning(
                        $"Manual cleanup may be needed. Container ID: {this.containerId}. " +
                        $"Run: docker stop {this.containerId} && docker rm {this.containerId}");
                }
            }
        }

        /// <summary>
        /// Logs docker-related information with yellow color.
        /// </summary>
        private void LogDockerInfo(ILogger logger, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            logger?.LogInformation(message);
            Console.ResetColor();
        }
    }
}
