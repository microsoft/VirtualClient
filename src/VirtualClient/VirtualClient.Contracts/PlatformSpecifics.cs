// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines platform-specific information and properties for dependencies and
    /// system/OS paths.
    /// </summary>
    public class PlatformSpecifics
    {
        /// <summary>
        /// Initializes a new version of the <see cref="PlatformSpecifics"/> class.
        /// </summary>
        public PlatformSpecifics(PlatformID platform, Architecture architecture)
            : this(platform, architecture, Path.GetDirectoryName(Assembly.GetAssembly(typeof(PlatformSpecifics)).Location))
        {
        }

        /// <summary>
        /// Initializes a new version of the <see cref="PlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <param name="currentDirectory">The directory to use as the current working directory.</param>
        /// <remarks>
        /// This constructor is largely used to address challenges with testing code that references
        /// paths on a system that are expected to be in a different format than is typical for the
        /// system on which the test is running. For example, Linux paths use forward slashes. When
        /// testing components on a Windows system, the typical path semantics have to be modified.
        /// </remarks>
        protected PlatformSpecifics(PlatformID platform, Architecture architecture, string currentDirectory)
        {
            this.Platform = platform;
            this.CpuArchitecture = architecture;
            this.CurrentDirectory = currentDirectory;
            this.LogsDirectory = this.Combine(currentDirectory, "logs");
            this.PackagesDirectory = this.Combine(currentDirectory, "packages");
            this.ProfilesDirectory = this.Combine(currentDirectory, "profiles");
            this.ScriptsDirectory = this.Combine(currentDirectory, "scripts");
            this.StateDirectory = this.Combine(currentDirectory, "state");
        }

        /// <summary>
        /// The CPU architecture (e.g. x64, arm64).
        /// </summary>
        public Architecture CpuArchitecture { get; }

        /// <summary>
        /// The current/working directory for the application.
        /// </summary>
        public string CurrentDirectory { get; }

        /// <summary>
        /// The directory where log files are are written.
        /// </summary>
        public string LogsDirectory { get; }

        /// <summary>
        /// The directory where packages are stored.
        /// </summary>
        public string PackagesDirectory { get; }

        /// <summary>
        /// The directory where profiles are stored.
        /// </summary>
        public string ProfilesDirectory { get; }

        /// <summary>
        /// The OS platform (e.g. Windows, Unix).
        /// </summary>
        public PlatformID Platform { get; }

        /// <summary>
        /// The directory where scripts related to workloads exist.
        /// </summary>
        public string ScriptsDirectory { get; }

        /// <summary>
        /// The directory where state objects are stored.
        /// </summary>
        public string StateDirectory { get; }

        /// <summary>
        /// Returns the platform + architecture name used by the Virtual Client to represent a
        /// common supported platform and architecture (e.g. win-x64, win-arm64, linux-x64, linux-arm64);
        /// </summary>
        /// <param name="platform">The OS/system platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU/processor architecture (e.g. amd64, arm).</param>
        /// <param name="throwIfNotSupported">True to throw an exception if the platform/architecture is not supported.</param>
        /// <returns></returns>
        public static string GetPlatformArchitectureName(PlatformID platform, Architecture? architecture = null, bool throwIfNotSupported = true)
        {
            string platformArchitectureName = null;

            if (throwIfNotSupported)
            {
                PlatformSpecifics.ThrowIfNotSupported(platform);
            }

            if (throwIfNotSupported && architecture != null)
            {
                PlatformSpecifics.ThrowIfNotSupported(architecture.Value);
            }

            switch (platform)
            {
                case PlatformID.Win32NT:
                    platformArchitectureName = "win";
                    break;

                case PlatformID.Unix:
                    platformArchitectureName = "linux";
                    break;
            }

            if (architecture != null && platformArchitectureName != null)
            {
                switch (architecture.Value)
                {
                    case Architecture.X64:
                        platformArchitectureName = $"{platformArchitectureName}-x64";
                        break;

                    case Architecture.Arm64:
                        platformArchitectureName = $"{platformArchitectureName}-arm64";
                        break;
                }
            }

            return platformArchitectureName;
        }

        /// <summary>
        /// Returns a profile name that includes the platform and architecture description
        /// (e.g. PERF-IO-FIO.json -> "PERF-IO-FIO (win-x64)", "PERF-IO-FIO (win-arm64)", "PERF-IO-FIO (linux-x64)").
        /// </summary>
        /// <param name="profileName">The full profile name (e.g. PERF-IO-FIO.json)</param>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <returns>
        /// The profile name containing the platform-specific information (e.g. PERF-IO-FIO (win-x64) ).
        /// </returns>
        public static string GetProfileName(string profileName, PlatformID platform, Architecture architecture)
        {
            profileName.ThrowIfNullOrWhiteSpace(nameof(profileName));

            string platformSpecificName = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);
            string platformSpecificProfileName = Regex.Replace(profileName, ".json", string.Empty, RegexOptions.IgnoreCase);

            platformSpecificProfileName = $"{platformSpecificProfileName} ({platformSpecificName})";

            return platformSpecificProfileName;
        }

        /// <summary>
        /// Return whether the VC is running in container.
        /// </summary>
        /// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_running_in_container-and-dotnet_running_in_containers
        /// <returns></returns>
        public static bool IsRunningInContainer()
        {
            return (Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")) == true);
        }

        /// <summary>
        /// Throws an exception if the platform provided is not supported.
        /// </summary>
        /// <param name="platform">The OS/system platform to validate as supported.</param>
        /// <exception cref="NotSupportedException">The platform is not supported.</exception>
        public static void ThrowIfNotSupported(PlatformID platform)
        {
            switch (platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Unix:
                    return;

                default:
                    throw new NotSupportedException($"The OS/system platform '{platform}' is not supported.");
            }
        }

        /// <summary>
        /// Throws an exception if the CPU/processor architecture provided is not supported.
        /// </summary>
        /// <param name="architecture">The CPU/processor architecture to validate as supported.</param>
        /// <exception cref="NotSupportedException">The CPU/processor architecture is not supported.</exception>
        public static void ThrowIfNotSupported(Architecture architecture)
        {
            switch (architecture)
            {
                case Architecture.X64:
                case Architecture.Arm64:
                    return;

                default:
                    throw new NotSupportedException($"The CPU/processor architecture '{architecture}' is not supported.");
            }
        }

        /// <summary>
        /// Combines the path segments into a valid path for the platform/OS.
        /// </summary>
        /// <param name="pathSegments">Individual segments of a full path.</param>
        public string Combine(params string[] pathSegments)
        {
            pathSegments.ThrowIfNullOrEmpty(nameof(pathSegments));

            string fullPath = null;
            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    fullPath = this.StandardizePath(string.Join('\\', pathSegments));
                    break;

                case PlatformID.Unix:
                    fullPath = this.StandardizePath(string.Join('/', pathSegments));
                    break;
            }

            return fullPath;
        }

        /// <summary>
        /// Returns the value of the environment variable as defined for the current process.
        /// </summary>
        /// <param name="variableName">The name of the environment variable.</param>
        /// <returns>The value of the environment variable</returns>
        public virtual string GetEnvironmentVariableValue(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName);
        }

        /// <summary>
        /// Combines the path segments provided with path where the log files/objects are stored.
        /// </summary>
        public string GetLogsPath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.LogsDirectory
                : this.Combine(this.LogsDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Combines the path segments provided with path to the directory where packages
        /// downloaded exist.
        /// </summary>
        public string GetPackagePath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.PackagesDirectory
                : this.Combine(this.PackagesDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Combines the path segments provided with path to the directory where packages
        /// downloaded exist.
        /// </summary>
        public string GetProfilePath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.ProfilesDirectory
                : this.Combine(this.ProfilesDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Combines the path segments provided with path where scripts are stored.
        /// </summary>
        public string GetScriptPath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.ScriptsDirectory
                : this.Combine(this.ScriptsDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Return the common path prefix of all the paths.
        /// </summary>
        public string GetCommonDirectory(params string[] filePaths)
        {
            // Strategy to find the common path:
            // Find the common string of all paths, then truncate at the last slash.
            List<string> allPaths = filePaths.Select(p => this.StandardizePath(p)).ToList();

            // Iterate through first string from 0 to min length of all strings
            // Take while all path share charaters at index.
            string commonPrefix = new string(
                allPaths.First().Substring(0, allPaths.Min(s => s.Length))
                .TakeWhile((c, i) => allPaths.All(p => p[i] == c)).ToArray());

            if (commonPrefix.Contains('\\') || commonPrefix.Contains('/'))
            {
                commonPrefix = commonPrefix.Substring(0, commonPrefix.LastIndexOfAny(new char[] { '\\', '/' }) + 1);
            }
            else
            {
                // In rare cases Windows drive can have two letters.
                // C:// and CC:// should not be common.
                commonPrefix = string.Empty;
            }

            return commonPrefix;
        }

        /// <summary>
        /// Combines the path segments provided with path where state files/objects are stored.
        /// </summary>
        public string GetStatePath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.StateDirectory
                : this.Combine(this.StateDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Standardizes/normalizes the path based upon the platform/OS ensuring
        /// a valid path is 
        /// </summary>
        /// <param name="path">The path to standardize.</param>
        /// <returns>A path standardized for the OS platform.</returns>
        public string StandardizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            string standardizedPath = path?.Trim();
            if (this.Platform == PlatformID.Unix && standardizedPath == "/")
            {
                return standardizedPath;
            }

            standardizedPath = path.TrimEnd('\\', '/');
            if (this.Platform == PlatformID.Unix)
            {
                standardizedPath = Regex.Replace(standardizedPath.Replace('\\', '/'), "/{2,}", "/");
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                standardizedPath = Regex.Replace(standardizedPath.Replace('/', '\\'), @"\\{2,}", @"\");
            }

            return standardizedPath;
        }

        /// <summary>
        /// Returns the path for the dependency/package given a specific platform and CPU architecture.
        /// </summary>
        /// <param name="dependency">The dependency path.</param>
        /// <param name="platform">The OS/system platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <returns>
        /// The dependency/package path given the specific platform and CPU architecture
        /// (e.g. /home/any/path/virtualclient/1.2.3.4/Packages/geekbench5.1.0.0/linux-x64)
        /// </returns>
        public DependencyPath ToPlatformSpecificPath(DependencyPath dependency, PlatformID platform, Architecture? architecture = null)
        {
            dependency.ThrowIfNull(nameof(dependency));

            string platformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);

            return new DependencyPath(
                dependency.Name,
                this.Combine(dependency.Path, platformArchitecture),
                dependency.Description,
                dependency.Version,
                dependency.Metadata);
        }
    }
}
