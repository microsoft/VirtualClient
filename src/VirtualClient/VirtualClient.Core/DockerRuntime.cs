// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Docker runtime for executing commands inside containers.
    /// </summary>
    public class DockerRuntime
    {
        private static readonly Regex DockerfilePattern = new Regex(
            @"^Dockerfile(\.[a-zA-Z0-9_-]+)?$|\.dockerfile$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ProcessManager processManager;
        private readonly PlatformSpecifics platformSpecifics;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerRuntime"/> class.
        /// </summary>
        /// <param name="processManager">The process manager for executing Docker commands.</param>
        /// <param name="platformSpecifics">Platform-specific configuration settings.</param>
        /// <param name="fileSystem"></param>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        public DockerRuntime(ProcessManager processManager, PlatformSpecifics platformSpecifics, IFileSystem fileSystem, ILogger logger = null)
        {
            processManager.ThrowIfNull(nameof(processManager));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));
            fileSystem.ThrowIfNull(nameof(fileSystem));
            this.processManager = processManager;
            this.platformSpecifics = platformSpecifics;
            this.fileSystem = fileSystem;
            this.logger = logger;
        }

        /// <summary>
        /// Determines if the provided value is a Dockerfile path rather than an image name.
        /// </summary>
        /// <param name="imageOrPath">The image name or Dockerfile path.</param>
        /// <returns>True if the value appears to be a Dockerfile path.</returns>
        public static bool IsDockerfilePath(string imageOrPath)
        {
            if (string.IsNullOrWhiteSpace(imageOrPath))
            {
                return false;
            }

            // Check if it's a full path that exists
            if (File.Exists(imageOrPath))
            {
                string fileName = Path.GetFileName(imageOrPath);
                return DockerfilePattern.IsMatch(fileName);
            }

            // Check if the filename matches Dockerfile pattern
            string name = Path.GetFileName(imageOrPath);
            return DockerfilePattern.IsMatch(name);
        }

        /// <summary>
        /// Generates an image name from a Dockerfile path.
        /// </summary>
        /// <param name="dockerfilePath">The path to the Dockerfile.</param>
        /// <returns>A generated image name (e.g., "vc-ubuntu:latest" from "Dockerfile.ubuntu").</returns>
        public static string GenerateImageNameFromDockerfile(string dockerfilePath)
        {
            string fileName = Path.GetFileName(dockerfilePath);
            string baseName;

            if (fileName.StartsWith("Dockerfile.", StringComparison.OrdinalIgnoreCase))
            {
                // Dockerfile.ubuntu -> ubuntu
                baseName = fileName.Substring("Dockerfile.".Length);
            }
            else if (fileName.EndsWith(".dockerfile", StringComparison.OrdinalIgnoreCase))
            {
                // ubuntu.dockerfile -> ubuntu
                baseName = fileName.Substring(0, fileName.Length - ".dockerfile".Length);
            }
            else
            {
                // Dockerfile -> default
                baseName = "custom";
            }

            // Sanitize the name for Docker (lowercase, alphanumeric and hyphens)
            baseName = Regex.Replace(baseName.ToLowerInvariant(), @"[^a-z0-9-]", "-");

            return $"vc-{baseName}:latest";
        }

        /// <summary>
        /// Resolves a Dockerfile path to search in standard locations.
        /// </summary>
        /// <param name="dockerfileReference">The Dockerfile reference (can be filename or full path).</param>
        /// <returns>The full path to the Dockerfile, or null if not found.</returns>
        public string ResolveDockerfilePath(string dockerfileReference)
        {
            // If it's already a full path and exists, return it
            if (Path.IsPathRooted(dockerfileReference) && File.Exists(dockerfileReference))
            {
                return dockerfileReference;
            }

            // Search in standard locations
            string[] searchPaths = new[]
            {
                // Current directory
                Path.Combine(Environment.CurrentDirectory, dockerfileReference),
                // Images folder in executable directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", dockerfileReference),
                // Images folder relative to current directory
                Path.Combine(Environment.CurrentDirectory, "Images", dockerfileReference)
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the image reference - builds from Dockerfile if needed, returns image name.
        /// </summary>
        /// <param name="imageOrDockerfilePath">Either an image name or a Dockerfile path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The image name to use for container execution.</returns>
        public async Task<string> ResolveImageAsync(string imageOrDockerfilePath, CancellationToken cancellationToken)
        {
            if (!IsDockerfilePath(imageOrDockerfilePath))
            {
                // It's already an image name
                return imageOrDockerfilePath;
            }

            // It's a Dockerfile path - resolve and build
            string dockerfilePath = this.ResolveDockerfilePath(imageOrDockerfilePath);

            if (string.IsNullOrEmpty(dockerfilePath))
            {
                throw new DependencyException(
                    $"Dockerfile not found: '{imageOrDockerfilePath}'. Searched in current directory and Images folder.",
                    ErrorReason.DependencyNotFound);
            }

            string imageName = GenerateImageNameFromDockerfile(dockerfilePath);

            this.logger?.LogInformation(
                "Building image '{ImageName}' from Dockerfile: {DockerfilePath}",
                imageName,
                dockerfilePath);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Container] Building image '{imageName}' from {dockerfilePath}...");
            Console.ResetColor();

            await this.BuildImageAsync(dockerfilePath, imageName, cancellationToken).ConfigureAwait(false);

            return imageName;
        }

        /// <summary>
        /// Checks if Docker is available and running.
        /// </summary>
        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IProcessProxy process = this.processManager.CreateProcess("docker", "version");
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an image exists locally.
        /// </summary>
        public async Task<bool> ImageExistsAsync(string image, CancellationToken cancellationToken)
        {
            using IProcessProxy process = this.processManager.CreateProcess("docker", $"image inspect {image}");
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }

        /// <summary>
        /// Pulls an image from a registry.
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
        /// Builds an image from a Dockerfile.
        /// </summary>
        public async Task BuildImageAsync(string dockerfilePath, string imageName, CancellationToken cancellationToken)
        {
            if (!File.Exists(dockerfilePath))
            {
                throw new DependencyException(
                    $"Dockerfile not found at '{dockerfilePath}'",
                    ErrorReason.DependencyNotFound);
            }

            string contextDir = Path.GetDirectoryName(dockerfilePath);
            string dockerfileName = Path.GetFileName(dockerfilePath);

            this.logger?.LogInformation("Building Docker image '{Image}' from {Dockerfile}", imageName, dockerfilePath);

            using IProcessProxy process = this.processManager.CreateProcess(
                "docker",
                $"build -t {imageName} -f \"{dockerfileName}\" .",
                contextDir);

            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new DependencyException(
                    $"Failed to build Docker image '{imageName}': {process.StandardError}",
                    ErrorReason.DependencyInstallationFailed);
            }

            this.logger?.LogInformation("Successfully built Docker image '{Image}'", imageName);
        }

        /// <summary>
        /// Gets platform information from a Docker image.
        /// </summary>
        public async Task<(PlatformID Platform, Architecture Architecture)> GetImagePlatformAsync(string imageName, CancellationToken cancellationToken)
        {
            using IProcessProxy process = this.processManager.CreateProcess("docker", $"image inspect {imageName}");
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new DependencyException(
                    $"Failed to inspect Docker image '{imageName}': {process.StandardError}",
                    ErrorReason.DependencyNotFound);
            }

            return DockerRuntime.ParsePlatformFromInspectJson(process.StandardOutput.ToString());
        }

        /// <summary>
        /// Parses platform info from 'docker image inspect' JSON output.
        /// </summary>
        public static (PlatformID Platform, Architecture Architecture) ParsePlatformFromInspectJson(string inspectJson)
        {
            var array = JsonNode.Parse(inspectJson)?.AsArray()
                ?? throw new ArgumentException("Invalid docker inspect JSON output.");

            var root = array[0]
                ?? throw new ArgumentException("Docker inspect output is empty.");

            string os = root["Os"]?.GetValue<string>() ?? string.Empty;
            string arch = root["Architecture"]?.GetValue<string>() ?? string.Empty;
            string variant = root["Variant"]?.GetValue<string>() ?? string.Empty;

            PlatformID platform = os.ToLowerInvariant() switch
            {
                "linux" => PlatformID.Unix,
                "windows" => PlatformID.Win32NT,
                _ => throw new NotSupportedException($"Unsupported container OS: '{os}'")
            };

            Architecture architecture = arch.ToLowerInvariant() switch
            {
                "amd64" or "x86_64" => Architecture.X64,
                "arm64" or "aarch64" => Architecture.Arm64,
                "arm" when variant.ToLowerInvariant() == "v8" => Architecture.Arm64,
                "arm" => Architecture.Arm,
                "386" or "i386" => Architecture.X86,
                _ => throw new NotSupportedException($"Unsupported architecture: '{arch}'")
            };

            return (platform, architecture);
        }

        /// <summary>
        /// Gets the path to a built-in Dockerfile for the given image name.
        /// </summary>
        public static string GetBuiltInDockerfilePath(string imageName)
        {
            string baseName = imageName.Split(':')[0].Replace("vc-", string.Empty);
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string imagesDir = Path.Combine(exeDir, "Images");
            string dockerfilePath = Path.Combine(imagesDir, $"Dockerfile.{baseName}");

            return File.Exists(dockerfilePath) ? dockerfilePath : null;
        }

        /// <summary>
        /// Stops a running container by ID or name. 
        /// </summary>
        public async Task StopContainer(string containerId, CancellationToken cancellationToken)
        {
            // Graceful => 'docker stop {containerId}'
            // Forceful => 'docker kill {containerId}'
            using IProcessProxy process = this.processManager.CreateProcess("docker", $"stop {containerId}");
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Start a container in detached mode with the specified image and mounting paths. Returns the container ID.
        /// mountingPaths format: each entry is "C:\host\path:/container/path" or "C:\host\path:/container/path:ro"
        /// </summary>
        public async Task<string> StartContainerInDetachedMode(string imageName, CancellationToken cancellationToken, string workingDirectory, params string[] mountPaths)
        {
            string readonlyMounts = $"-v \"{workingDirectory}:/agent:ro\"";

            // example: -v "C:\repos\VirtualClient\out\bin\Release\x64\VirtualClient.Main\net9.0\win-x64\state:/agent/state"
            IEnumerable<string> readWriteMounts = mountPaths.Select(p => $"-v \"{p}:/{this.fileSystem.Path.GetFileName(p)}:rw\"");

            string args = string.Join(
                " ",
                new[] { "run", "-d", "--rm", "--name vc-container", readonlyMounts, string.Join(" ", readWriteMounts), imageName, "sleep infinity" });

            using IProcessProxy process = this.processManager.CreateProcess("docker", args);
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

            // 'docker run -d' outputs the full container ID on stdout
            string containerId = process.StandardOutput.ToString().Trim();
            return containerId;
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

            this.logger?.LogDebug("Docker command: docker {Args}", args);

            var result = new DockerRunResult { ContainerName = containerName, StartTime = DateTime.UtcNow };

            using IProcessProxy process = this.processManager.CreateProcess("docker", args);
            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

            result.EndTime = DateTime.UtcNow;
            result.ExitCode = process.ExitCode;
            result.StandardOutput = process.StandardOutput.ToString();
            result.StandardError = process.StandardError.ToString();

            return result;
        }

        private string BuildDockerRunArgs(string image, string command, ContainerConfiguration config, string containerName, PlatformSpecifics hostPlatformSpecifics)
        {
            var args = new List<string> { "run", "--rm", $"--name {containerName}" };

            args.Add($"-w {config?.WorkingDirectory ?? "/vc"}");

            ContainerMountConfig mounts = config?.Mounts ?? new ContainerMountConfig();
            if (mounts.Packages)
            {
                args.Add($"-v \"{this.ToDockerPath(hostPlatformSpecifics.PackagesDirectory)}:/vc/packages\"");
            }

            if (mounts.Logs)
            {
                args.Add($"-v \"{this.ToDockerPath(hostPlatformSpecifics.LogsDirectory)}:/vc/logs\"");
            }

            if (mounts.State)
            {
                args.Add($"-v \"{this.ToDockerPath(hostPlatformSpecifics.StateDirectory)}:/vc/state\"");
            }

            if (mounts.Temp)
            {
                args.Add($"-v \"{this.ToDockerPath(hostPlatformSpecifics.TempDirectory)}:/vc/temp\"");
            }

            config?.AdditionalMounts?.ToList().ForEach(m => args.Add($"-v \"{m}\""));
            config?.EnvironmentVariables?.ToList().ForEach(e => args.Add($"-e \"{e.Key}={e.Value}\""));

            args.Add("-e \"VC_CONTAINER_MODE=true\"");
            args.Add(image);
            if (!string.IsNullOrWhiteSpace(command))
            {
                args.Add(command);
            }

            return string.Join(" ", args);
        }

        private string ToDockerPath(string path)
        {
            if (this.platformSpecifics.Platform == PlatformID.Win32NT && path.Length >= 2 && path[1] == ':')
            {
                return $"/{char.ToLower(path[0])}{path[2..].Replace('\\', '/')}";
            }

            return path;
        }
    }

    /// <summary>
    /// Result of a Docker run operation.
    /// </summary>
    public class DockerRunResult
    {
        /// <summary>
        /// Gets or sets the name of the container.
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Gets or sets the exit code returned by the process.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the standard output from the process.
        /// </summary>
        public string StandardOutput { get; set; }

        /// <summary>
        /// Gets or sets the standard error output.
        /// </summary>
        public string StandardError { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the duration.
        /// </summary>
        public TimeSpan Duration => this.EndTime - this.StartTime;

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded => this.ExitCode == 0;
    }
}