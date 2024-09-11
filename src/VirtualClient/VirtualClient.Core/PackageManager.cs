// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides support for installing and managing packages on the system.
    /// </summary>
    public class PackageManager : IPackageManager, IDisposable
    {
        /// <summary>
        /// The metadata key for built-it toolsets/packages.
        /// </summary>
        public const string BuiltIn = "built-in";

        /// <summary>
        /// The name of the built-in package containing the lshw toolset.
        /// </summary>
        public const string BuiltInLshwPackageName = "lshw";

        /// <summary>
        /// The name of the built-in package containing the system tools/toolsets.
        /// </summary>
        public const string BuiltInSystemToolsPackageName = "systemtools";

        /// <summary>
        /// The name of the built-in package containing the wget toolsets.
        /// </summary>
        public const string BuiltInWgetPackageName = "wget";

        /// <summary>
        /// Custom extension used for Virtual Client package descriptions.
        /// </summary>
        public const string VCPkgExtension = ".vcpkg";

        /// <summary>
        /// Custom extension used for Virtual Client package registration descriptions.
        /// </summary>
        public const string VCPkgRegExtension = ".vcpkgreg";

        private static char[] pathTrimChars = new char[] { '\\', '/' };
        private static IDictionary<string, ArchiveType> archiveFileTypeMappings = new Dictionary<string, ArchiveType>(StringComparer.OrdinalIgnoreCase)
        {
            { ".zip", ArchiveType.Zip },
            { ".tar.gz", ArchiveType.Tgz },
            { ".tgz", ArchiveType.Tgz },
            { ".tar.gzip", ArchiveType.Tgz },
            { ".tar", ArchiveType.Tar }
        };

        private static Regex profileExtensionExpression = new Regex(@"\.json|\.yml|\.yaml", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private List<string> packagePaths;
        private Semaphore semaphore;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageManager"/> class.
        /// </summary>
        /// <param name="platformSpecifics">Provides platform-specific path information.</param>
        /// <param name="fileSystem">Provides features for interacting with the file system.</param>
        /// <param name="logger">A logger to use to capture telemetry.</param>
        public PackageManager(PlatformSpecifics platformSpecifics, IFileSystem fileSystem, ILogger logger = null)
        {
            fileSystem.ThrowIfNull(nameof(fileSystem));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));

            this.FileSystem = fileSystem;
            this.PlatformSpecifics = platformSpecifics;
            this.Logger = logger ?? NullLogger.Instance;
            this.semaphore = new Semaphore(1, 1);

            this.packagePaths = new List<string>
            {
                // e.g.
                // virtualclient/win-x64/packages
                this.PlatformSpecifics.GetPackagePath(),

                // e.g.
                // virtualclient/win-x64/tools
                this.PlatformSpecifics.GetToolsPath()
            };
        }

        /// <summary>
        /// Provides features for interacting with the file system
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// The logger used to capture telemetry.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// The directory where packages are stored.
        /// </summary>
        public string PackagesDirectory
        {
            get
            {
                return this.PlatformSpecifics.PackagesDirectory;
            }
        }

        /// <summary>
        /// Provides access to the Azure Blob storage account that contains the workload
        /// and dependency packages.
        /// </summary>
        public IBlobManager PackageStoreManager { get; }

        /// <summary>
        /// Provides platform-specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Returns the name of the archive file without its file extension
        /// (e.g. anyarchive.zip, anyarchive.tgz, anyarchive.tar.gz -> anyarchive).
        /// </summary>
        /// <param name="filePath">The archive file path or name.</param>
        public static string GetArchiveFileNameWithoutExtension(string filePath)
        {
            StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            string archiveFileName = Path.GetFileName(filePath);
            if (archiveFileName.EndsWith(".tar.gz", ignoreCase))
            {
                archiveFileName = archiveFileName.Substring(0, archiveFileName.Length - 7);
            }
            else if (archiveFileName.EndsWith(".tar.gzip", ignoreCase))
            {
                archiveFileName = archiveFileName.Substring(0, archiveFileName.Length - 9);
            }
            else
            {
                archiveFileName = Path.GetFileNameWithoutExtension(archiveFileName);
            }

            return archiveFileName;
        }

        /// <summary>
        /// Returns the name that will be used for the package installation directory for the
        /// descriptor provided.
        /// </summary>
        public static string GetPackageDirectoryName(DependencyDescriptor description)
        {
            // Account for blob dependencies that might have a path structure (e.g. /virtual/path/to/dependency.zip
            return description.Name.Split(PackageManager.pathTrimChars, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        /// <summary>
        /// Returns true if the archive file type is recognized from the file path and outputs
        /// the type (e.g. Zip, Tgz)
        /// </summary>
        /// <param name="filePath">The archive file path or name.</param>
        /// <param name="archiveType">The type of archive file (e.g. Zip, Tgz).</param>
        public static bool TryGetArchiveFileType(string filePath, out ArchiveType archiveType)
        {
            archiveType = ArchiveType.Undefined;
            StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            string archiveFilePath = filePath.Trim();

            foreach (var entry in PackageManager.archiveFileTypeMappings)
            {
                if (archiveFilePath.EndsWith(entry.Key, ignoreCase))
                {
                    archiveType = entry.Value;
                    break;
                }
            }

            return archiveType != ArchiveType.Undefined;
        }

        /// <summary>
        /// Performs extensions package discovery on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public Task<PlatformExtensions> DiscoverExtensionsAsync(CancellationToken cancellationToken)
        {
            EventContext telemetryContext = EventContext.Persisted();

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.DiscoverExtensions", LogLevel.Trace, telemetryContext, async () =>
            {
                this.semaphore.WaitOne();

                try
                {
                    List<IFileInfo> profileExtensions = new List<IFileInfo>();
                    List<IFileInfo> binaryExtensions = new List<IFileInfo>();

                    // User-defined binaries locations. Similar to PATH environment variable, multiple directories can be
                    // defined separated by a semi-colon.
                    string userDefinedBinariesPath = this.PlatformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_LIBRARY_PATH);
                    string userDefinedPackagesDir = this.PlatformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR);

                    if (!string.IsNullOrWhiteSpace(userDefinedBinariesPath))
                    {
                        telemetryContext.AddContext("userDefinedBinariesPath", userDefinedBinariesPath);
                    }

                    if (!string.IsNullOrWhiteSpace(userDefinedPackagesDir))
                    {
                        telemetryContext.AddContext("userDefinedPackagesDirectory", userDefinedPackagesDir);
                    }

                    List<string> binaryDirectories = new List<string>();
                    List<string> profileDirectories = new List<string>();
                    List<DependencyPath> extensionsPackages = new List<DependencyPath>();

                    // 1) Packages location. This could be the default 'packages' directory or one defined
                    //    by the user via the 'VC_PACKAGES_DIR' environment variable.
                    IEnumerable<DependencyPath> defaultExtensionsPackages = await this.DiscoverPackagesAsync(
                        this.PlatformSpecifics.GetPackagePath(),
                        cancellationToken,
                        extensionsOnly: true);

                    if (defaultExtensionsPackages?.Any() == true)
                    {
                        extensionsPackages.AddRange(defaultExtensionsPackages);
                    }

                    // 2) User-defined binary locations.
                    if (!string.IsNullOrWhiteSpace(userDefinedBinariesPath))
                    {
                        string[] userDefinedBinaries = userDefinedBinariesPath
                            .Split(VirtualClientComponent.CommonDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        binaryDirectories.AddRange(userDefinedBinaries);
                    }

                    // 3) Discover binaries/.dlls and profiles in the extensions packages.
                    if (extensionsPackages?.Any() == true)
                    {
                        foreach (DependencyPath extensionsPackage in extensionsPackages)
                        {
                            DependencyPath platformSpecificExtensions = this.PlatformSpecifics.ToPlatformSpecificPath(extensionsPackage, this.PlatformSpecifics.Platform, this.PlatformSpecifics.CpuArchitecture);

                            string binaryExtensionsPath = platformSpecificExtensions.Path;
                            if (this.FileSystem.Directory.Exists(binaryExtensionsPath))
                            {
                                binaryDirectories.Add(binaryExtensionsPath);

                                string profileExtensionsPath = this.PlatformSpecifics.Combine(platformSpecificExtensions.Path, "profiles");
                                if (this.FileSystem.Directory.Exists(profileExtensionsPath))
                                {
                                    profileDirectories.Add(profileExtensionsPath);
                                }
                            }
                        }
                    }

                    // 4) Combined extensions binaries (default + user-defined).
                    if (binaryDirectories.Any())
                    {
                        foreach (string directory in binaryDirectories)
                        {
                            IEnumerable<string> extensionsAssemblies = this.FileSystem.Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
                            if (extensionsAssemblies?.Any() == true)
                            {
                                foreach (string filePath in extensionsAssemblies)
                                {
                                    binaryExtensions.Add(this.FileSystem.FileInfo.New(filePath));
                                }
                            }
                        }
                    }

                    // 5) Combined extensions profiles (default + user-defined).
                    if (profileDirectories.Any())
                    {
                        foreach (string directory in profileDirectories)
                        {
                            IEnumerable<string> extensionsProfiles = this.FileSystem.Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(file => PackageManager.profileExtensionExpression.IsMatch(file));

                            if (extensionsProfiles?.Any() == true)
                            {
                                foreach (string filePath in extensionsProfiles)
                                {
                                    profileExtensions.Add(this.FileSystem.FileInfo.New(filePath));
                                }
                            }
                        }
                    }

                    telemetryContext.AddContext("binaryExtensionsFound", binaryExtensions.Any() ? binaryExtensions.Select(ext => ext.FullName) : null);
                    telemetryContext.AddContext("profileExtensionsFound", profileExtensions.Any() ? profileExtensions.Select(ext => ext.FullName) : null);

                    // Validate we do not have duplicate binaries defined.
                    var duplicateBinaries = binaryExtensions.GroupBy(file => file.Name).Where(group => group.Count() > 1);
                    if (duplicateBinaries?.Any() == true)
                    {
                        throw new DependencyException(
                            $"Duplicate extensions binaries discovered. Extensions binaries must have unique names in relation to other binaries. The following binaries are " +
                            $"duplicated: {string.Join(", ", duplicateBinaries.Select(g => g.Key))}",
                            ErrorReason.DuplicateExtensionsFound);
                    }

                    // Validate we do not have duplicate profiles defined.
                    var duplicateProfiles = profileExtensions.GroupBy(file => file.Name).Where(group => group.Count() > 1);
                    if (duplicateProfiles?.Any() == true)
                    {
                        throw new DependencyException(
                            $"Duplicate extensions profiles discovered. Extensions profiles must have unique names in relation to other profiles. The following profiles are " +
                            $"duplicated: {string.Join(", ", duplicateProfiles.Select(g => g.Key))}",
                            ErrorReason.DuplicateExtensionsFound);
                    }

                    return new PlatformExtensions(binaryExtensions, profileExtensions);
                }
                finally
                {
                    this.semaphore.Release();
                }
            });
        }

        /// <summary>
        /// Performs package discovery on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public Task<IEnumerable<DependencyPath>> DiscoverPackagesAsync(CancellationToken cancellationToken)
        {
            EventContext telemetryContext = EventContext.Persisted();

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.DiscoverPackages", LogLevel.Trace, telemetryContext, async () =>
            {
                this.semaphore.WaitOne();

                try
                {
                    // The VC package directory can be either the default 'packages' directory or one defined by the
                    // the user using the 'VC_PACKAGES_DIR' environment variable. We capture the fact that the environment
                    // variable was used here for reference in the telemetry output.
                    string userDefinedPackagesDir = this.PlatformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR);

                    if (!string.IsNullOrWhiteSpace(userDefinedPackagesDir))
                    {
                        telemetryContext.AddContext("userDefinedPackagesDirectory", userDefinedPackagesDir);
                    }

                    // 1) Packages defined in the packages directory.
                    List<DependencyPath> discoveredPackages = new List<DependencyPath>();
                    IEnumerable<DependencyPath> existingPackages = await this.DiscoverPackagesAsync(this.PlatformSpecifics.GetPackagePath(), cancellationToken);

                    if (existingPackages?.Any() == true)
                    {
                        discoveredPackages.AddRange(existingPackages);
                    }

                    // 2) Packages defined in the built-in tools directory.
                    IEnumerable<DependencyPath> toolsPackages = await this.DiscoverPackagesAsync(this.PlatformSpecifics.GetToolsPath(), cancellationToken);

                    if (toolsPackages?.Any() == true)
                    {
                        PackageManager.SetAsBuiltInToolset(toolsPackages?.ToArray());
                        discoveredPackages.AddRange(toolsPackages);
                    }

                    telemetryContext.AddContext(
                        "packagesDirectory",
                        this.PlatformSpecifics.GetPackagePath());

                    telemetryContext.AddContext(
                        "toolsDirectory",
                        this.PlatformSpecifics.GetToolsPath());

                    telemetryContext.AddContext(
                        "packagesFound",
                        discoveredPackages?.Any() == true ? discoveredPackages.Select(pkg => pkg.Name).OrderBy(p => p) : null);

                    // Validate we do not have duplicate binaries defined.
                    var duplicatePackages = discoveredPackages.GroupBy(pkg => pkg.Name).Where(group => group.Count() > 1);
                    if (duplicatePackages?.Any() == true)
                    {
                        throw new DependencyException(
                            $"Duplicate packages discovered. Packages must have unique names in relation to other packages. The following packages have " +
                            $"duplicates: {string.Join(", ", duplicatePackages.Select(g => g.Key))}",
                            ErrorReason.DuplicatePackagesFound);
                    }

                    return discoveredPackages as IEnumerable<DependencyPath>;
                }
                finally
                {
                    this.semaphore.Release();
                }
            });
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Extracts/unzips the package at the file path provided. This supports standard .zip and .nupkg
        /// file formats.
        /// </summary>
        /// <param name="archiveFilePath">The path to the package zip file.</param>
        /// <param name="destinationPath">The path to the directory where the files should be extracted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the extract operation.</param>
        /// <param name="archiveType">The type of archive format the file is in.</param>
        public Task ExtractPackageAsync(string archiveFilePath, string destinationPath, CancellationToken cancellationToken, ArchiveType archiveType = ArchiveType.Zip)
        {
            archiveFilePath.ThrowIfNullOrWhiteSpace(nameof(archiveFilePath));
            destinationPath.ThrowIfNullOrWhiteSpace(nameof(destinationPath));

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("archiveFilePath", archiveFilePath)
                .AddContext("destinationPath", destinationPath);

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.ExtractPackage", LogLevel.Trace, telemetryContext, async () =>
            {
                if (!this.FileSystem.File.Exists(archiveFilePath))
                {
                    throw new FileNotFoundException($"The archive file at path '{archiveFilePath}' does not exist.");
                }

                if (!this.FileSystem.Directory.Exists(destinationPath))
                {
                    this.FileSystem.Directory.CreateDirectory(destinationPath).Create();
                }

                await this.ExtractArchiveAsync(archiveFilePath, destinationPath, archiveType, cancellationToken).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        /// <param name="packageName">The name of the package dependency.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public Task<DependencyPath> GetPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            packageName.ThrowIfNullOrWhiteSpace(nameof(packageName));

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("packageName", packageName)
                .AddContext("packageDirectories", this.packagePaths);

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.GetPackage", LogLevel.Trace, telemetryContext, async () =>
            {
                this.semaphore.WaitOne();

                try
                {
                    // Search 'packages' and 'tools' folders.
                    DependencyPath package = await this.GetPackageAsync(this.packagePaths, packageName, cancellationToken);

                    telemetryContext.AddContext("packageFound", package != null);
                    telemetryContext.AddContext("packagePath", package?.Path);
                    telemetryContext.AddContext("package", package);

                    return package;
                }
                finally
                {
                    this.semaphore.Release();
                }
            });
        }

        /// <summary>
        /// Performs package initialization on the system including extraction of package archives.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public Task InitializePackagesAsync(CancellationToken cancellationToken)
        {
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("packageDirectories", this.packagePaths);

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.InitializePackages", LogLevel.Trace, telemetryContext, async () =>
            {
                // Note on the tuple structure:
                // {filePath},{fileExtension},{archiveType}
                StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
                List<Tuple<string, string, ArchiveType>> archiveFilesFound = new List<Tuple<string, string, ArchiveType>>();

                // Ensure the packages path exists.
                string packagesLocation = this.PlatformSpecifics.GetPackagePath();
                if (!this.FileSystem.Directory.Exists(packagesLocation))
                {
                    this.FileSystem.Directory.CreateDirectory(packagesLocation);
                }

                foreach (string packageDirectory in this.packagePaths)
                {
                    if (this.FileSystem.Directory.Exists(packageDirectory))
                    {
                        IEnumerable<string> filesInDirectory = this.FileSystem.Directory.EnumerateFiles(packageDirectory, "*.*", SearchOption.TopDirectoryOnly);
                        if (filesInDirectory?.Any() == true)
                        {
                            foreach (string files in filesInDirectory)
                            {
                                string file = files.Trim().TrimEnd(PackageManager.pathTrimChars);
                                if (file.EndsWith(".zip", ignoreCase))
                                {
                                    archiveFilesFound.Add(new Tuple<string, string, ArchiveType>(files, ".zip", ArchiveType.Zip));
                                }
                                else if (file.EndsWith(".tar.gz", ignoreCase))
                                {
                                    archiveFilesFound.Add(new Tuple<string, string, ArchiveType>(files, ".tar.gz", ArchiveType.Tgz));
                                }
                                else if (file.EndsWith(".tgz", ignoreCase))
                                {
                                    archiveFilesFound.Add(new Tuple<string, string, ArchiveType>(files, ".tgz", ArchiveType.Tgz));
                                }
                                else if (file.EndsWith(".gz", ignoreCase))
                                {
                                    archiveFilesFound.Add(new Tuple<string, string, ArchiveType>(files, ".gz", ArchiveType.Tgz));
                                }
                            }
                        }
                    }
                }

                if (archiveFilesFound.Any())
                {
                    telemetryContext.AddContext("packageArchivesFound", archiveFilesFound.Select(entry => entry.Item1).OrderBy(p => p));

                    foreach (var entry in archiveFilesFound)
                    {
                        string archiveFile = entry.Item1;
                        string archiveFileExtension = entry.Item2;
                        ArchiveType archiveType = entry.Item3;

                        string extractToDirectory = archiveFile.Substring(0, archiveFile.Length - archiveFileExtension.Length);

                        if (!this.FileSystem.Directory.Exists(extractToDirectory))
                        {
                            try
                            {
                                await this.ExtractArchiveAsync(archiveFile, extractToDirectory, archiveType, cancellationToken);
                            }
                            catch (InvalidDataException)
                            {
                                // This can happen if there is an invalid .zip file in the directory...perhaps the download failed
                                // on the previous attempt. When this happens, the .zip file itself will have a corrupted file pointer.
                                // For these scenarios, we will simply delete the file and move forward.
                                await RetryPolicies.FileDelete.ExecuteAsync(async () => await this.FileSystem.File.DeleteAsync(archiveFile));
                            }
                        }

                        await RetryPolicies.FileDelete.ExecuteAsync(async () => await this.FileSystem.File.DeleteAsync(archiveFile));
                    }
                }
            });
        }

        /// <summary>
        /// Installs the package from the Azure storage account blob store.
        /// </summary>
        /// <param name="packageStoreManager">The blob manager to use for downloading the package to the file system.</param>
        /// <param name="description">Provides information about the target package.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="installationPath">Optional installation path to be used to override the default installation path.</param>
        /// <param name="retryPolicy">A retry policy to apply to the blob download and installation to allow for transient error handling.</param>
        /// <returns>The path where the Blob package was installed.</returns>
        public Task<string> InstallPackageAsync(IBlobManager packageStoreManager, DependencyDescriptor description, CancellationToken cancellationToken, string installationPath = null, IAsyncPolicy retryPolicy = null)
        {
            description.ThrowIfNull(nameof(description), $"A dependency description is required.");

            if (packageStoreManager == null)
            {
                throw new DependencyException(
                    $"Package store not defined. The package '{description.Name}' cannot be installed because the package store information " +
                    $"was not provided to the application on the command line (e.g. --packages).",
                    ErrorReason.PackageStoreNotDefined);
            }

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("description", description.ObscureSecrets());

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.InstallDependencyPackage", LogLevel.Trace, telemetryContext, async () =>
            {
                description.Validate(nameof(description.Name), nameof(description.PackageName));

                try
                {
                    installationPath ??= this.PlatformSpecifics.GetPackagePath();
                    string installationDirectoryName = PackageManager.GetArchiveFileNameWithoutExtension(description.Name);

                    // e.g.
                    // /VirtualClient/packages/dependency
                    string dependencyPackagePath = this.PlatformSpecifics.Combine(installationPath, installationDirectoryName.ToLowerInvariant());

                    // Account for blob dependencies that might have a path structure (e.g. /virtual/path/to/dependency.zip
                    string dependencyName = PackageManager.GetPackageDirectoryName(description);

                    if (description.Extract)
                    {
                        if (description.ArchiveType == ArchiveType.Undefined)
                        {
                            throw new DependencyException(
                                $"The type of archive was not defined for the dependency. The dependency must be one " +
                                $"of the following supported archive types: {string.Join(", ", Enum.GetNames(typeof(ArchiveType)))}. " +
                                $"Additionally, the archive type must be defined in the description.",
                                ErrorReason.DependencyDescriptionInvalid);
                        }
                    }
                    else
                    {
                        // If the package does not need to be extracted, then we want to contain it within
                        // a folder matching the package name (e.g. the package path).
                        installationPath = this.PlatformSpecifics.Combine(installationPath, description.PackageName.ToLowerInvariant());
                        dependencyPackagePath = installationPath;
                    }

                    // e.g.
                    // /VirtualClient/packages/dependency.zip
                    string dependencyInstallationPath = this.PlatformSpecifics.Combine(installationPath, dependencyName);

                    if (this.FileSystem.Directory.Exists(dependencyPackagePath))
                    {
                        // Package already downloaded. Skipping.
                        telemetryContext.AddContext("packageExists", true);
                    }
                    else
                    {
                        telemetryContext.AddContext("installationPath", installationPath);
                        this.CreateDirectoryIfNotExists(installationPath);

                        await this.DownloadDependencyPackageAsync(packageStoreManager, description, dependencyInstallationPath, cancellationToken)
                            .ConfigureAwait(false);

                        if (description.Extract)
                        {
                            await this.ExtractArchiveAsync(dependencyInstallationPath, dependencyPackagePath, description.ArchiveType, cancellationToken).ConfigureAwait(false);
                            await this.FileSystem.File.DeleteAsync(dependencyInstallationPath).ConfigureAwait(false);
                        }
                    }

                    IEnumerable<DependencyPath> packageDescriptions = await this.DiscoverPackagesAsync(dependencyPackagePath, cancellationToken)
                        .ConfigureAwait(false);

                    bool registerPackageSpecified = true;
                    if (packageDescriptions?.Any() == true)
                    {
                        foreach (DependencyPath metadataDescription in packageDescriptions)
                        {
                            await this.RegisterPackageAsync(metadataDescription, cancellationToken).ConfigureAwait(false);
                        }

                        registerPackageSpecified = !packageDescriptions.Any(pkg => string.Equals(pkg.Name, description.PackageName, StringComparison.OrdinalIgnoreCase));
                    }

                    // It is possible that the package does not contain a *.vcpkg definition. It is also possible for the name that is defined in a profile to differ from
                    // the name defined in the package *.vcpkg file definition(s). In either case we want to register the package with the name defined in the
                    // description.
                    if (registerPackageSpecified)
                    {
                        await this.RegisterPackageAsync(new DependencyPath(description.PackageName, dependencyPackagePath), cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return dependencyPackagePath;
                }
                catch (DependencyException exc) when (exc.Reason == ErrorReason.Http403ForbiddenResponse)
                {
                    throw new DependencyException(
                        $"Package store access denied. The access token provided does not have required permissions to access the package store " +
                        $"and cannot be used to download dependency packages. Verify the access token is valid and that it has not expired.",
                        ErrorReason.Http403ForbiddenResponse);
                }
                catch (DependencyException)
                {
                    throw;
                }
                catch (Exception exc)
                {
                    throw new DependencyException(
                        $"Dependency package installation/download failed for package '{description.Name}'.",
                        exc,
                        ErrorReason.DependencyInstallationFailed);
                }
            });
        }

        /// <summary>
        /// Registers/saves the path so that it can be used by dependencies, workloads and monitors. Paths registered
        /// follow a strict format
        /// </summary>
        /// <param name="package">Describes a package dependency to register with the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public Task RegisterPackageAsync(DependencyPath package, CancellationToken cancellationToken)
        {
            package.ThrowIfNull(nameof(package));
            EventContext telemetryContext = EventContext.Persisted()
               .AddContext("package", package);

            return this.Logger.LogMessageAsync($"{nameof(PackageManager)}.RegisterPackage", LogLevel.Trace, telemetryContext, async () =>
            {
                // We register packages in the packages directory by default. Built-in toolsets are registered
                // in the tools directory.
                string registrationDirectory = this.PlatformSpecifics.GetPackagePath();
                if (PackageManager.IsBuiltInToolset(package))
                {
                    registrationDirectory = this.PlatformSpecifics.GetToolsPath();
                }

                string registrationFileName = $"{package.Name.ToLowerInvariant()}{PackageManager.VCPkgRegExtension}";
                string registrationFilePath = this.PlatformSpecifics.Combine(registrationDirectory, registrationFileName);
                string registrationFileContent = package.ToJson();

                await RetryPolicies.FileOperations.ExecuteAsync(() =>
                {
                    return this.FileSystem.File.WriteAllTextAsync(registrationFilePath, registrationFileContent, cancellationToken);
                });
            });
        }

        /// <summary>
        /// Searches the directory for package definition (*.vcpkg) files
        /// </summary>
        /// <param name="directoryPath">The path to the directory in which to perform package discovery.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="extensionsOnly">True to return packages that contain extensions only, false to return all packages.</param>
        protected virtual async Task<IEnumerable<DependencyPath>> DiscoverPackagesAsync(string directoryPath, CancellationToken cancellationToken, bool extensionsOnly = false)
        {
            List<DependencyPath> packages = new List<DependencyPath>();
            if (this.FileSystem.Directory.Exists(directoryPath))
            {
                if (this.TryGetPackageDescriptions(directoryPath, out IEnumerable<string> vcPkgFiles))
                {
                    foreach (string file in vcPkgFiles)
                    {
                        string fileContents = await this.FileSystem.File.ReadAllTextAsync(file, cancellationToken)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(fileContents))
                        {
                            try
                            {
                                DependencyMetadata packageDescription = fileContents.FromJson<DependencyMetadata>();
                                if (extensionsOnly && !packageDescription.IsExtensions)
                                {
                                    continue;
                                }

                                DependencyPath packageLocation = new DependencyPath(
                                    packageDescription.Name,
                                    Path.GetDirectoryName(file),
                                    packageDescription.Description,
                                    packageDescription.Version,
                                    packageDescription.Metadata);

                                packages.Add(packageLocation);
                            }
                            catch (JsonException exc)
                            {
                                throw new DependencyException(
                                    $"Invalid Virtual Client package definition. The contents of the package definition at the path '{file}' is not formatted correctly.",
                                    exc,
                                    ErrorReason.DependencyDescriptionInvalid);
                            }
                        }
                    }
                }
            }

            return packages;
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.semaphore.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Downloads the dependency from the container specified to the path defined.
        /// </summary>
        /// <param name="blobManager">The blob manager to use for downloading the package to the file system.</param>
        /// <param name="description">The dependency description.</param>
        /// <param name="downloadPath">Provides the location where the package should be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task DownloadDependencyPackageAsync(IBlobManager blobManager, DependencyDescriptor description, string downloadPath, CancellationToken cancellationToken)
        {
            blobManager.ThrowIfNull(nameof(blobManager));
            description.ThrowIfNull(nameof(description));
            downloadPath.ThrowIfNullOrWhiteSpace(nameof(downloadPath));

            using (FileStream stream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                await blobManager.DownloadBlobAsync(description, stream, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Extracts the archive/zip file to the destination path defined.
        /// </summary>
        /// <param name="archiveFilePath">The path to the archive/zip/nupkg file.</param>
        /// <param name="destinationPath">The directory path to which the file should be extracted.</param>
        /// <param name="archiveType">The type of archive the file is.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the extract operation.</param>
        protected virtual async Task ExtractArchiveAsync(string archiveFilePath, string destinationPath, ArchiveType archiveType, CancellationToken cancellationToken)
        {
            switch (archiveType)
            {
                case ArchiveType.Zip:
                    await this.ExtractZipAsync(archiveFilePath, destinationPath, cancellationToken)
                        .ConfigureAwait(false);

                    break;

                case ArchiveType.Tgz:
                case ArchiveType.Tar:
                    await this.ExtractTarballAsync(archiveFilePath, destinationPath, cancellationToken)
                        .ConfigureAwait(false);

                    break;

                default:
                    throw new WorkloadException($"Unsupported archive file format. Can not extract the contents of: '{archiveFilePath}'");
            }
        }

        /// <summary>
        /// Extracts the archive/zip file to the destination path defined.
        /// </summary>
        /// <param name="archiveFilePath">The path to the archive/zip/nupkg file.</param>
        /// <param name="destinationPath">The directory path to which the file should be extracted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the extract operation.</param>
        protected virtual Task ExtractZipAsync(string archiveFilePath, string destinationPath, CancellationToken cancellationToken)
        {
            return Task.Run(() => ZipFile.ExtractToDirectory(archiveFilePath, destinationPath), cancellationToken);
        }

        /// <summary>
        /// Extracts the archive/tar.gz file to the destination path defined.
        /// </summary>
        /// <param name="archiveFilepath">The path to the archive/tar.gz file.</param>
        /// <param name="destinationPath">The directory path to which the file should be extracted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the extract operation.</param>
        protected virtual async Task ExtractTarballAsync(string archiveFilepath, string destinationPath, CancellationToken cancellationToken)
        {
            if (!this.FileSystem.Directory.Exists(destinationPath))
            {
                this.FileSystem.Directory.CreateDirectory(destinationPath).Create();
            }

            ProcessManager manager = ProcessManager.Create(Environment.OSVersion.Platform);

            // To extract a .tar file, we use -xzf
            // To extract a .tar.gz, .tgz or .tar.gzip file, we use -xzvf
            string arguments = archiveFilepath.EndsWith(".tar") ? "-xzf" : "-xzvf";

            // -x: extract -z: pass through gzip before un-tarring -f archive file -C destination
            using (IProcessProxy process = manager.CreateProcess("tar", $"{arguments} {archiveFilepath} -C {destinationPath}"))
            {
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails(process, nameof(PackageManager), EventContext.Persisted(), "Tar");
                    process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.SystemOperationFailed);
                }
            }
        }

        private static bool IsBuiltInToolset(DependencyPath package)
        {
            if (package.Metadata.TryGetValue(PackageManager.BuiltIn, out IConvertible builtIn))
            {
                return true;
            }

            return false;
        }

        private static void SetAsBuiltInToolset(params DependencyPath[] packages)
        {
            if (packages?.Any() == true)
            {
                foreach (DependencyPath package in packages)
                {
                    package.Metadata[PackageManager.BuiltIn] = true;
                }
            }
        }

        private void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!this.FileSystem.Directory.Exists(directoryPath))
            {
                this.FileSystem.Directory.CreateDirectory(directoryPath).Create();
            }
        }

        private void DeleteDirectoryIfExists(string directoryPath)
        {
            if (this.FileSystem.Directory.Exists(directoryPath))
            {
                this.FileSystem.Directory.Delete(directoryPath, true);
            }
        }

        private bool TryGetPackageDescriptions(string packagePath, out IEnumerable<string> vcPkgFiles)
        {
            vcPkgFiles = null;
            IEnumerable<string> descriptions = this.FileSystem.Directory.EnumerateFiles(
                packagePath,
                $"*{PackageManager.VCPkgExtension}",
                SearchOption.AllDirectories);

            if (descriptions?.Any() == true)
            {
                vcPkgFiles = descriptions;
            }

            return vcPkgFiles != null;
        }

        private async Task<DependencyPath> GetPackageAsync(IEnumerable<string> paths, string packageName, CancellationToken cancellationToken)
        {
            DependencyPath package = null;
            foreach (string packagesPath in paths)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Example:
                // C:\any\directory\VirtualClient.1.2.3.4\content\win-x64\VirtualClient.exe
                // C:\any\directory\VirtualClient.1.2.3.4\content\win-x64\packages\examplepackage.vcpkgreg

                string registrationFilePath = this.PlatformSpecifics.Combine(packagesPath, $"{packageName.ToLowerInvariant()}{PackageManager.VCPkgRegExtension}");

                await RetryPolicies.FileOperations.ExecuteAsync(async () =>
                {
                    if (this.FileSystem.File.Exists(registrationFilePath))
                    {
                        string registrationFileContent = await this.FileSystem.File.ReadAllTextAsync(registrationFilePath, cancellationToken);
                        package = registrationFileContent?.FromJson<DependencyPath>();
                    }
                });

                if (package != null)
                {
                    break;
                }
            }

            return package;
        }
    }
}
