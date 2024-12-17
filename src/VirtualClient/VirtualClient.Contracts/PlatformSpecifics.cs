// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines platform-specific information and properties for dependencies and
    /// system/OS paths.
    /// </summary>
    public class PlatformSpecifics
    {
        /// <summary>
        /// LinuxX64 platformArchitecture Name.
        /// </summary>
        public static readonly string LinuxX64 = PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64);

        /// <summary>
        /// LinuxArm64 platformArchitecture Name.
        /// </summary>
        public static readonly string LinuxArm64 = PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64);

        /// <summary>
        /// WinX64 platformArchitecture Name.
        /// </summary>
        public static readonly string WinX64 = PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, Architecture.X64);

        /// <summary>
        /// WinArm64 platformArchitecture Name.
        /// </summary>
        public static readonly string WinArm64 = PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, Architecture.Arm64);

        /// <summary>
        /// Initializes a new version of the <see cref="PlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <param name="useUnixStylePathsOnly">True to use Unix-style paths only (e.g. w/forward slashes). False to apply the conventions for the OS platform targeted.</param>
        public PlatformSpecifics(PlatformID platform, Architecture architecture, bool useUnixStylePathsOnly = false)
            : this(platform, architecture, AppDomain.CurrentDomain.BaseDirectory, useUnixStylePathsOnly)
        {
        }

        /// <summary>
        /// Initializes a new version of the <see cref="PlatformSpecifics"/> class.
        /// </summary>
        /// <param name="platform">The OS platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <param name="workingDirectory">
        /// The directory to use as the current working directory. This is the directory where tools, scripts, packages and logs exist 
        /// and is typically the same directory as the Virtual Client application binaries.
        /// </param>
        /// <param name="useUnixStylePathsOnly">True to use Unix-style paths only (e.g. w/forward slashes). False to apply the conventions for the OS platform targeted.</param>
        /// <remarks>
        /// This constructor is largely used to address challenges with testing code that references
        /// paths on a system that are expected to be in a different format than is typical for the
        /// system on which the test is running. For example, Linux paths use forward slashes. When
        /// testing components on a Windows system, the typical path semantics have to be modified.
        /// </remarks>
        public PlatformSpecifics(PlatformID platform, Architecture architecture, string workingDirectory, bool useUnixStylePathsOnly = false)
        {
            this.Platform = platform;
            this.PlatformArchitectureName = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);
            this.CpuArchitecture = architecture;
            this.UseUnixStylePathsOnly = useUnixStylePathsOnly;

            string standardizedCurrentDirectory = this.StandardizePath(workingDirectory);
            this.CurrentDirectory = standardizedCurrentDirectory;
            this.LogsDirectory = this.Combine(standardizedCurrentDirectory, "logs");
            this.ContentUploadsDirectory = this.Combine(standardizedCurrentDirectory, "contentuploads");
            this.PackagesDirectory = this.Combine(standardizedCurrentDirectory, "packages");
            this.ProfilesDirectory = this.Combine(standardizedCurrentDirectory, "profiles");
            this.ProfileDownloadsDirectory = this.Combine(standardizedCurrentDirectory, "profiles", "downloads");
            this.ScriptsDirectory = this.Combine(standardizedCurrentDirectory, "scripts");
            this.StateDirectory = this.Combine(standardizedCurrentDirectory, "state");
            this.ToolsDirectory = this.Combine(standardizedCurrentDirectory, "tools");
        }

        /// <summary>
        /// The directory for file/content upload notifications (e.g. /logs/contentuploads).
        /// </summary>
        public string ContentUploadsDirectory { get; }

        /// <summary>
        /// The CPU architecture (e.g. x64, arm64).
        /// </summary>
        public Architecture CpuArchitecture { get; }

        /// <summary>
        /// The current/working directory for the application.
        /// </summary>
        public string CurrentDirectory { get; }

        /// <summary>
        /// The directory where log files are are written. Overridable.
        /// </summary>
        public string LogsDirectory { get; set; }

        /// <summary>
        /// The directory where packages are stored. Overridable.
        /// </summary>
        public string PackagesDirectory { get; set; }

        /// <summary>
        /// The directory where profiles are stored.
        /// </summary>
        public string ProfilesDirectory { get; }

        /// <summary>
        /// The directory where profiles downloaded are stored.
        /// </summary>
        public string ProfileDownloadsDirectory { get; }

        /// <summary>
        /// The OS platform (e.g. Windows, Unix).
        /// </summary>
        public PlatformID Platform { get; }

        /// <summary>
        /// The name of the platform + architecture name for the system on which Virtual Client
        /// is running (e.g. win-x64, win-arm64, linux-x64, linux-arm64).
        /// </summary>
        public string PlatformArchitectureName { get; }

        /// <summary>
        /// The directory where scripts related to workloads exist.
        /// </summary>
        public string ScriptsDirectory { get; }

        /// <summary>
        /// The directory where state objects are stored.
        /// </summary>
        public string StateDirectory { get; }

        /// <summary>
        /// The directory where built-in tools/toolsets are stored.
        /// </summary>
        public string ToolsDirectory { get; }

        /// <summary>
        /// True to standardize paths using Unix-style conventions (e.g. forward slashes '/')
        /// only. When 'true' all paths (including Windows-formatted) will use forward slashes.
        /// </summary>
        public bool UseUnixStylePathsOnly { get; }

        /// <summary>
        /// Whether VC is running in the context of docker container.
        /// </summary>
        internal static bool RunningInContainer { get; set; } = PlatformSpecifics.IsRunningInContainer();

        /// <summary>
        /// Get the logged in user/username. On Windows systems, the user is discoverable even when running as Administrator.
        /// On Linux systems, the user can be discovered using certain environment variables when running under sudo/root.
        /// </summary>
        public string GetLoggedInUser()
        {
            string loggedInUserName = Environment.UserName;
            if (string.Equals(loggedInUserName, "root"))
            {
                loggedInUserName = this.GetEnvironmentVariable(EnvironmentVariable.SUDO_USER);
                if (string.Equals(loggedInUserName, "root") || string.IsNullOrEmpty(loggedInUserName))
                {
                    loggedInUserName = this.GetEnvironmentVariable(EnvironmentVariable.VC_SUDO_USER);
                    if (string.IsNullOrEmpty(loggedInUserName))
                    {
                        throw new EnvironmentSetupException(
                            $"Unable to determine logged in username. The expected environment variables '{EnvironmentVariable.SUDO_USER}' and " +
                            $"'{EnvironmentVariable.VC_SUDO_USER}' do not exist or are set to 'root' (i.e. potentially when running as sudo/root).",
                            ErrorReason.EnvironmentIsInsufficent);
                    }
                }
            }

            return loggedInUserName;
        }

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
        /// Returns true/false whether the path provided is a full path location on the
        /// local file system.
        /// </summary>
        /// <param name="path">The path to evaluate.</param>
        /// <returns>True if the path is a fully qualified path (e.g. C:\Users\any\path, home/user/any/path). False if not.</returns>
        public static bool IsFullyQualifiedPath(string path)
        {
            path.ThrowIfNull(nameof(path));
            return Regex.IsMatch(path, "[A-Z]+:\\\\|^/", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Return whether the VC is running in container.
        /// </summary>
        /// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_running_in_container-and-dotnet_running_in_containers
        /// <returns></returns>
        public static bool IsRunningInContainer()
        {
            // DOTNET does not properly recognize some containers. Adding /.dockerenv file as back up.
            return (Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")) == true || File.Exists("/.dockerenv"));
        }

        /// <summary>
        /// Standardizes/normalizes the path based upon the platform/OS ensuring
        /// a valid path is 
        /// </summary>
        /// <param name="platform">The platform for which to standardize the path.</param>
        /// <param name="path">The path to standardize.</param>
        /// <param name="useUnixStylePathsOnly">True to use Unix-style paths only (e.g. w/forward slashes). False to apply the conventions for the OS platform targeted.</param>
        /// <returns>A path standardized for the OS platform.</returns>
        public static string StandardizePath(PlatformID platform, string path, bool useUnixStylePathsOnly = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            string standardizedPath = path?.Trim();
            if ((platform == PlatformID.Unix && standardizedPath == "/") || (platform == PlatformID.Win32NT && standardizedPath == @"\"))
            {
                if (useUnixStylePathsOnly)
                {
                    standardizedPath = "/";
                }

                return standardizedPath;
            }

            standardizedPath = path.TrimEnd('\\', '/');
            if (platform == PlatformID.Unix || useUnixStylePathsOnly)
            {
                standardizedPath = Regex.Replace(standardizedPath.Replace('\\', '/'), "/{2,}", "/");
            }
            else if (platform == PlatformID.Win32NT)
            {
                standardizedPath = Regex.Replace(standardizedPath.Replace('/', '\\'), @"\\{2,}", @"\");
            }

            return standardizedPath;
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
                    fullPath = this.StandardizePath(string.Join('\\', pathSegments.Where(p => !string.IsNullOrWhiteSpace(p))));
                    break;

                case PlatformID.Unix:
                    fullPath = this.StandardizePath(string.Join('/', pathSegments.Where(p => !string.IsNullOrWhiteSpace(p))));
                    break;
            }

            return fullPath;
        }

        /// <summary>
        /// Returns the value of the environment variable as defined for the current process.
        /// </summary>
        /// <param name="variableName">The name of the environment variable.</param>
        /// <param name="target">The environment variable scope (e.g. Machine, User, Process).</param>
        /// <returns>The value of the environment variable</returns>
        public virtual string GetEnvironmentVariable(string variableName, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            return Environment.GetEnvironmentVariable(variableName, target);
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
        /// Combines the path segments provided with path to the directory where profiles exist.
        /// </summary>
        public string GetProfilePath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.ProfilesDirectory
                : this.Combine(this.ProfilesDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Combines the path segments provided with path to the directory where profiles downloaded exist.
        /// </summary>
        public string GetProfileDownloadsPath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.ProfileDownloadsDirectory
                : this.Combine(this.ProfileDownloadsDirectory, this.Combine(additionalPathSegments));
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
        /// Combines the path segments provided with path where built-in tools/toolsets are stored.
        /// </summary>
        public string GetToolsPath(params string[] additionalPathSegments)
        {
            return additionalPathSegments?.Any() != true
                ? this.ToolsDirectory
                : this.Combine(this.ToolsDirectory, this.Combine(additionalPathSegments));
        }

        /// <summary>
        /// Sets the value of the environment variable or appends a value to the end of it.
        /// </summary>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="value">The value to which to set the environment variable or append to the end of the existing value.</param>
        /// <param name="target">The environment variable scope (e.g. Machine, User, Process).</param>
        /// <param name="append">True to append the value to the end of the existing environment variable value. False to replace the existing value.</param>
        public virtual void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process, bool append = false)
        {
            string originalValue = Environment.GetEnvironmentVariable(name, target);
            string newValue = value ?? string.Empty;
            bool commitChange = true;

            if (!string.IsNullOrWhiteSpace(originalValue) && append)
            {
                commitChange = false;
                char delimiter = this.Platform == PlatformID.Unix ? ':' : ';';

                originalValue = originalValue?.TrimEnd(delimiter);
                if (string.IsNullOrWhiteSpace(originalValue))
                {
                    commitChange = true;
                }
                else if (!originalValue.EndsWith(value))
                {
                    commitChange = true;
                    newValue = $"{originalValue}{delimiter}{newValue}";
                }
            }

            if (commitChange)
            {
                Environment.SetEnvironmentVariable(name, newValue, target);
            }
        }

        /// <summary>
        /// Standardizes/normalizes the path based upon the platform/OS ensuring
        /// a valid path is 
        /// </summary>
        /// <param name="path">The path to standardize.</param>
        /// <returns>A path standardized for the OS platform.</returns>
        public string StandardizePath(string path)
        {
            return PlatformSpecifics.StandardizePath(this.Platform, path, this.UseUnixStylePathsOnly);
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
