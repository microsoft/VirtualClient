// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Docker runtime for executing commands inside containers.
    /// </summary>
    public class DockerRuntime
    {
        private readonly ProcessManager processManager;
        private readonly PlatformSpecifics platformSpecifics;
        private readonly ILogger logger;
        private bool dependenciesInitialized;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="processManager"></param>
        /// <param name="platformSpecifics"></param>
        /// <param name="logger"></param>
        public DockerRuntime(string imageName, ProcessManager processManager, PlatformSpecifics platformSpecifics, ILogger logger = null)
        {
            processManager.ThrowIfNull(nameof(processManager));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));
            this.processManager = processManager;
            this.platformSpecifics = platformSpecifics;
            this.logger = logger;
            this.dependenciesInitialized = false;
        }

        /// <summary>
        /// Gets platform-specific configuration settings for Docker environments.
        /// </summary>
        public PlatformSpecifics DockerPlatformSpecifics { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async InitializeDependencies(CancellationToken cancellationToken)
        {
            try
            {

            }
            catch ()
            {

            }

            this.dependenciesInitialized = true;
        }

        /// <summary>
        /// Checks if an image exists locally.
        /// </summary>
        public async Task<bool> ImageExistsAsync(string image, CancellationToken cancellationToken)
        {
            using IProcessProxy process = this.processManager.CreateProcess("docker", $"image inspect {image}");
            await process.StartAndWaitAsync(cancellationToken);
            return process.ExitCode == 0;
        }

        /// <summary>
        /// Pulls an image.
        /// </summary>
        public async Task PullImageAsync(string image, CancellationToken cancellationToken)
        {
            this.logger?.LogInformation("Pulling Docker image: {Image}", image);

            using IProcessProxy process = this.processManager.CreateProcess("docker", $"pull {image}");
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new DependencyException(
                    $"Failed to pull Docker image '{image}': {process.StandardError}",
                    ErrorReason.DependencyInstallationFailed);
            }
        }

        /// <summary>
        /// Executes a command inside a container.
        /// </summary>
        public async Task<DockerRunResult> RunAsync(
            string image,
            string command,
            ContainerConfiguration config,
            PlatformSpecifics hostPlatformSpecifics,
            CancellationToken cancellationToken)
        {
            string containerName = $"vc-{Guid.NewGuid():N}"[..32];
            string args = this.BuildDockerRunArgs(image, command, config, containerName, hostPlatformSpecifics);

            this.logger?.LogInformation("Running container: {ContainerName}", containerName);
            this.logger?.LogDebug("Docker command: docker {Args}", args);

            var result = new DockerRunResult
            {
                ContainerName = containerName,
                StartTime = DateTime.UtcNow
            };

            using IProcessProxy process = this.processManager.CreateProcess("docker", args);

            await process.StartAndWaitAsync(cancellationToken);

            result.EndTime = DateTime.UtcNow;
            result.ExitCode = process.ExitCode;
            result.StandardOutput = process.StandardOutput.ToString();
            result.StandardError = process.StandardError.ToString();

            this.logger?.LogDebug("Container {ContainerName} exited with code {ExitCode}", containerName, result.ExitCode);

            return result;
        }

        /// <summary>
        /// Builds an image from a Dockerfile in the specified directory.
        /// </summary>
        /// <param name="dockerfilePath">Full path to the Dockerfile.</param>
        /// <param name="imageName">Name and tag for the image (e.g., vc-ubuntu:22.04).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task BuildImageAsync(string dockerfilePath, string imageName, CancellationToken cancellationToken)
        {
            if (!System.IO.File.Exists(dockerfilePath))
            {
                throw new DependencyException(
                    $"Dockerfile not found at '{dockerfilePath}'",
                    ErrorReason.DependencyNotFound);
            }

            string contextDir = System.IO.Path.GetDirectoryName(dockerfilePath);
            string dockerfileName = System.IO.Path.GetFileName(dockerfilePath);

            this.logger?.LogInformation("Building Docker image '{Image}' from {Dockerfile}", imageName, dockerfilePath);

            string args = $"build -t {imageName} -f \"{dockerfileName}\" .";
            
            using IProcessProxy process = this.processManager.CreateProcess("docker", args, contextDir);
            await process.StartAndWaitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new DependencyException(
                    $"Failed to build Docker image '{imageName}': {process.StandardError}",
                    ErrorReason.DependencyInstallationFailed);
            }

            this.logger?.LogInformation("Successfully built Docker image '{Image}'", imageName);
        }

        /// <summary>
        /// Gets the path to a built-in Dockerfile for the given image name.
        /// </summary>
        /// <param name="imageName">Image name (e.g., vc-ubuntu:22.04).</param>
        /// <returns>Path to Dockerfile if found, null otherwise.</returns>
        public static string GetBuiltInDockerfilePath(string imageName)
        {
            // Extract base name (vc-ubuntu:22.04 -> ubuntu)
            string baseName = imageName.Split(':')[0].Replace("vc-", string.Empty);
            
            // Look in the Images folder relative to the executable
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string imagesDir = System.IO.Path.Combine(exeDir, "Images");
            
            string dockerfilePath = System.IO.Path.Combine(imagesDir, $"Dockerfile.{baseName}");
            
            if (System.IO.File.Exists(dockerfilePath))
            {
                return dockerfilePath;
            }

            return null;
        }

        private string BuildDockerRunArgs(
            string image,
            string command,
            ContainerConfiguration config,
            string containerName,
            PlatformSpecifics hostPlatformSpecifics)
        {
            var args = new List<string>
            {
                "run",
                "--rm",
                $"--name {containerName}"
            };

            // Working directory
            string workDir = config?.WorkingDirectory ?? "/vc";
            args.Add($"-w {workDir}");

            // Standard mounts
            ContainerMountConfig mounts = config?.Mounts ?? new ContainerMountConfig();

            if (mounts.Packages)
            {
                string hostPath = this.ToDockerPath(hostPlatformSpecifics.PackagesDirectory);
                args.Add($"-v \"{hostPath}:/vc/packages\"");
            }

            if (mounts.Logs)
            {
                string hostPath = this.ToDockerPath(hostPlatformSpecifics.LogsDirectory);
                args.Add($"-v \"{hostPath}:/vc/logs\"");
            }

            if (mounts.State)
            {
                string hostPath = this.ToDockerPath(hostPlatformSpecifics.StateDirectory);
                args.Add($"-v \"{hostPath}:/vc/state\"");
            }

            if (mounts.Temp)
            {
                string hostPath = this.ToDockerPath(hostPlatformSpecifics.TempDirectory);
                args.Add($"-v \"{hostPath}:/vc/temp\"");
            }

            // Additional mounts
            if (config?.AdditionalMounts?.Any() == true)
            {
                foreach (string mount in config.AdditionalMounts)
                {
                    args.Add($"-v \"{mount}\"");
                }
            }

            // Environment variables
            if (config?.EnvironmentVariables?.Any() == true)
            {
                foreach (KeyValuePair<string, string> env in config.EnvironmentVariables)
                {
                    args.Add($"-e \"{env.Key}={env.Value}\"");
                }
            }

            // Always pass these VC context vars
            args.Add("-e \"VC_CONTAINER_MODE=true\"");

            // Image
            args.Add(image);

            // Command (if provided)
            if (!string.IsNullOrWhiteSpace(command))
            {
                args.Add(command);
            }

            return string.Join(" ", args);
        }

        /// <summary>
        /// Converts Windows path to Docker-compatible format.
        /// C:\path\to\dir -> /c/path/to/dir
        /// </summary>
        private string ToDockerPath(string path)
        {
            if (this.platformSpecifics.Platform == PlatformID.Win32NT && path.Length >= 2 && path[1] == ':')
            {
                char drive = char.ToLower(path[0]);
                return $"/{drive}{path[2..].Replace('\\', '/')}";
            }

            return path;
        }
    }

    /// <summary>
    /// Result of a Docker run operation.
    /// </summary>
    public class DockerRunResult
    {
        /// <summary>Container name.</summary>
        public string ContainerName { get; set; }

        /// <summary>Exit code from the container.</summary>
        public int ExitCode { get; set; }

        /// <summary>Standard output from the container.</summary>
        public string StandardOutput { get; set; }

        /// <summary>Standard error from the container.</summary>
        public string StandardError { get; set; }

        /// <summary>When the container started.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>When the container exited.</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Duration of execution.</summary>
        public TimeSpan Duration => this.EndTime - this.StartTime;

        /// <summary>True if exit code was 0.</summary>
        public bool Succeeded => this.ExitCode == 0;
    }
}