// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Contracts.Validation;
    using VirtualClient.Logging;
    using VirtualClient.Metadata;

    /// <summary>
    /// Command executes the operations of the Virtual Client workload profile. This is the
    /// default/root command for the Virtual Client application.
    /// </summary>
    internal class ExecuteProfileCommand : CommandBase
    {
        private const string ExecuteCommandProfile = "EXECUTE-COMMAND.json";
        private const string ExecuteScriptProfile = "EXECUTE-SCRIPT.json";
        private const string ExecuteSshCommandProfile = "EXECUTE-SSH-COMMAND.json";
        private const string FileUploadMonitorProfile = "MONITORS-FILE-UPLOAD.json";

        private static readonly Regex PowerShellExpression = new Regex(
            "pwsh.exe|pwsh|powershell.exe|powershell",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The full command line.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// True if VC should exit/crash on first/any error(s) regardless of their severity. Default = false.
        /// </summary>
        public bool? FailFast { get; set; }

        /// <summary>
        /// Returns true if the user command line arguments provided indicates a PowerShell (pwsh) 
        /// command (vs. a profile execution).
        /// </summary>
        protected bool IsPowerShell
        {
            get
            {
                return ExecuteProfileCommand.PowerShellExpression.IsMatch(this.Command);
            }
        }

        /// <summary>
        /// True if the profile dependencies should be installed as the only operations. False if
        /// the profile actions and monitors should also be considered.
        /// </summary>
        public bool InstallDependencies { get; set; }

        /// <summary>
        /// The environment layout definition.
        /// </summary>
        public EnvironmentLayout Layout { get; set; }

        /// <summary>
        /// A seed that can be used to guarantee identical randomization bases for workloads that
        /// require it.
        /// </summary>
        public int? RandomizationSeed { get; set; }

        /// <summary>
        /// Defines a set of scenarios (as defined in a workload profile) to execute
        /// (vs. the entire profile).
        /// </summary>
        public IEnumerable<string> Scenarios { get; set; }

        /// <summary>
        /// Defines the application timeout constraints. The timing logic supports explicit timeouts as
        /// well as explicit rounds/iterations of profile actions. The 2 cannot be used together. The command
        /// line parsing logic will not allow 2 parameters to have the same name, so the duplication of the
        /// <see cref="ProfileTiming"/> parameter here is a workaround.
        /// </summary>
        public ProfileTiming Iterations { get; set; }

        /// <summary>
        /// Defines the application timeout constraints.
        /// </summary>
        public ProfileTiming Timeout { get; set; }

        /// <summary>
        /// Platform extensions discovered at runtime (e.g. binaries/.dlls, profiles).
        /// </summary>
        protected PlatformExtensions Extensions { get; set; }

        /// <summary>
        /// Normalizes the PowerShell command for execution in a non-interactive
        /// environment.
        /// </summary>
        /// <param name="commandLine">The command line arguments provided by the user.</param>
        /// <returns>A PowerShell command ready for non-interactive execution.</returns>
        protected static string NormalizeForPowerShell(string commandLine)
        {
            string normalizedCommand = commandLine;
            if (!Regex.IsMatch(commandLine, "-NonInteractive", RegexOptions.IgnoreCase))
            {
                int indexToInsert = -1;
                List<string> commandArgs = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                for (int i = 0; i < commandArgs.Count; i++)
                {
                    if (ExecuteProfileCommand.PowerShellExpression.IsMatch(commandArgs[i]))
                    {
                        indexToInsert = i + 1;
                        break;
                    }
                }

                if (indexToInsert >= 0)
                {
                    commandArgs.Insert(indexToInsert, "-NonInteractive");
                }

                normalizedCommand = string.Join(' ', commandArgs);
            }

            return normalizedCommand;
        }

        /// <summary>
        /// Executes the profile operations.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="dependencies">Dependencies/services created for the application.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected override async Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;
            ILogger logger = null;
            IPackageManager packageManager = null;
            ISystemManagement systemManagement = null;
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                logger = dependencies.GetService<ILogger>();
                packageManager = dependencies.GetService<IPackageManager>();
                systemManagement = dependencies.GetService<ISystemManagement>();

                EventContext telemetryContext = EventContext.Persisted();

                logger.LogMessage($"Platform.Initialize", telemetryContext);

                // Defend long-running workloads against unattended-upgrades triggered SIGKILL on
                // Ubuntu 24.04+ (needrestart auto-restart-on-upgrade is enabled by default).
                // Best-effort; never fails VC startup.
                await this.DisableLinuxAutoUpdatesAsync(logger, systemManagement, telemetryContext, cancellationToken);

                if (this.Profiles?.Any() == true)
                {
                    this.LogContextToConsole(dependencies);

                    // Extracts and registers any packages that are pre-existing on the system (e.g. they exist in
                    // the 'packages' directory already).
                    await this.InitializePackagesAsync(packageManager, cancellationToken);

                    // Ensure all Virtual Client types are loaded from .dlls in the execution directory.
                    ComponentTypeCache.Instance.LoadComponentTypes(AppDomain.CurrentDomain.BaseDirectory);

                    // Installs any extensions that are pre-existing on the system (e.g. they exist in
                    // the 'packages' directory already).
                    this.Extensions = await this.DiscoverExtensionsAsync(packageManager, cancellationToken);
                    if (this.Extensions?.Binaries?.Any() == true)
                    {
                        await this.LoadExtensionsBinariesAsync(this.Extensions, cancellationToken);
                    }

                    IEnumerable<string> profileNames = await this.EvaluateProfilesAsync(dependencies);
                    this.SetGlobalTelemetryProperties(profileNames, dependencies);
                    this.SetHostMetadataTelemetryProperties(profileNames, dependencies);

                    IEnumerable<string> effectiveProfiles = await this.EvaluateProfilesAsync(dependencies, true, cancellationToken);

                    if (this.InstallDependencies)
                    {
                        await this.ExecuteProfileDependenciesInstallationAsync(effectiveProfiles, dependencies, cancellationTokenSource);
                    }
                    else
                    {
                        await this.ExecuteProfileAsync(effectiveProfiles, dependencies, cancellationTokenSource);
                    }
                }
            }
            finally
            {
                // In order to include all of the experiment + agent etc... context, we need to
                // get the current/persisted context.
                EventContext exitingContext = EventContext.Persisted();

                // Allow components to handle any final exit operations.
                VirtualClientRuntime.ExecuteExitActions();

                if (VirtualClientRuntime.IsRebootRequested)
                {
                    Program.LogMessage(logger, $"{nameof(ExecuteProfileCommand)}.RebootingSystem", exitingContext);
                }

                Program.LogMessage(logger, $"Exit Code: {exitCode}", exitingContext);

                TimeSpan remainingWait = TimeSpan.FromMinutes(2);
                if (this.ExitWaitTimeout != DateTime.MinValue)
                {
                    remainingWait = this.ExitWaitTimeout.SafeSubtract(DateTime.UtcNow);
                }

                if (remainingWait <= TimeSpan.Zero && this.ExitWait > TimeSpan.Zero)
                {
                    remainingWait = TimeSpan.FromMinutes(2);
                }

                Program.LogMessage(logger, $"Flush Telemetry", exitingContext);
                Program.LogMessage(logger, $"Flushed", exitingContext);

                // Allow components to handle any final cleanup operations.
                VirtualClientRuntime.ExecuteCleanupActions();

                // Reboots must happen after telemetry is flushed and just before the application is exiting. This ensures
                // we capture all important telemetry and allow the profile execution operations to exit gracefully before
                // we suddenly reboot the system.
                if (VirtualClientRuntime.IsRebootRequested)
                {
                    await CommandBase.RebootSystemAsync(dependencies);
                }
            }

            return exitCode;
        }

        /// <summary>
        /// On Ubuntu 24.04 and later, the default installation of the
        /// <see href="https://manpages.ubuntu.com/manpages/noble/man1/needrestart.1.html">needrestart</see>
        /// package (combined with the <c>apt-daily-upgrade.timer</c> systemd timer that fires every day
        /// between 06:00 and 07:00 UTC) will automatically restart services whose shared libraries are
        /// updated by unattended upgrades. For long-running workloads this manifests as the Virtual Client
        /// process being killed mid-run, desynchronizing multi-VM experiments and invalidating results.
        /// <para>
        /// This method is a best-effort mitigation that masks (and stops) the apt-daily timers and
        /// services when running on Ubuntu 24.04 or newer. Failures are logged but never propagated
        /// so that Virtual Client startup is unaffected.
        /// </para>
        /// </summary>
        protected virtual async Task DisableLinuxAutoUpdatesAsync(
            ILogger logger,
            ISystemManagement systemManagement,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (systemManagement == null || systemManagement.Platform != PlatformID.Unix)
            {
                return;
            }

            try
            {
                LinuxDistributionInfo distroInfo = await systemManagement.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                // The auto-restart-on-unattended-upgrade behavior has only been confirmed on Ubuntu 24.04+.
                // Earlier Ubuntu releases and Debian do not auto-restart services in non-interactive mode
                // and are not known to cause this issue in the field.
                if (distroInfo?.LinuxDistribution != LinuxDistribution.Ubuntu
                    || !ExecuteProfileCommand.TryGetUbuntuMajorVersion(distroInfo.OperationSystemFullName, out int majorVersion)
                    || majorVersion < 24)
                {
                    return;
                }

                EventContext maskContext = telemetryContext.Clone()
                    .AddContext("linuxDistribution", distroInfo.LinuxDistribution.ToString())
                    .AddContext("linuxDistributionFullName", distroInfo.OperationSystemFullName)
                    .AddContext("ubuntuMajorVersion", majorVersion);

                // Double-quoted -c argument is required: .NET Process argument tokenization follows
                // Windows CommandLineToArgvW-style rules (double-quote grouping only), so single quotes
                // would be passed literally to bash.
                string bashArgs = "-c \"systemctl mask apt-daily.timer apt-daily-upgrade.timer apt-daily.service apt-daily-upgrade.service;"
                    + " systemctl stop apt-daily.timer apt-daily-upgrade.timer apt-daily.service apt-daily-upgrade.service;"
                    + " exit 0\"";

                using (IProcessProxy process = systemManagement.ProcessManager.CreateProcess("bash", bashArgs))
                {
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                    maskContext.AddContext("exitCode", process.ExitCode);
                    maskContext.AddContext("standardError", process.StandardError?.ToString());
                }

                logger.LogMessage($"{nameof(ExecuteProfileCommand)}.DisabledLinuxAutoUpdates", LogLevel.Information, maskContext);
            }
            catch (Exception exc)
            {
                // Never fail VC startup because of this mitigation.
                logger.LogMessage(
                    $"{nameof(ExecuteProfileCommand)}.DisableLinuxAutoUpdatesFailed",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(exc));
            }
        }

        /// <summary>
        /// Parses the Ubuntu major version (e.g. 22, 24) from the PRETTY_NAME string returned
        /// by <see cref="LinuxDistributionInfo.OperationSystemFullName"/> (e.g. "Ubuntu 24.04.1 LTS").
        /// Returns false when the name does not contain a parseable Ubuntu version.
        /// </summary>
        internal static bool TryGetUbuntuMajorVersion(string osFullName, out int majorVersion)
        {
            majorVersion = 0;

            if (string.IsNullOrWhiteSpace(osFullName))
            {
                return false;
            }

            Match match = Regex.Match(osFullName, @"Ubuntu\s+(\d+)\.", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out majorVersion);
        }

        /// <summary>
        /// Downloads the profile from a remote location to the local profile downloads folder.
        /// </summary>
        /// <param name="dependencies">Provides components used to access external dependencies.</param>
        /// <param name="profile">Describes the endpoint target profile (and authentication requirements) to download to the local system.</param>
        /// <param name="profilePath">The full file path to which the profile should be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        protected virtual async Task DownloadProfileAsync(IServiceCollection dependencies, DependencyProfileReference profile, string profilePath, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            IProfileManager profileManager = dependencies.GetService<IProfileManager>();

            string downloadDirectory = Path.GetDirectoryName(profilePath);
            if (!fileSystem.Directory.Exists(downloadDirectory))
            {
                fileSystem.Directory.CreateDirectory(downloadDirectory);
            }

            using (var fs = new FileStream(profilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, cancellationToken, profile.Credentials);
            }
        }

        /// <summary>
        /// Initializes the profiles specified on the command line and returns the full path location for
        /// each of the files.
        /// </summary>
        /// <param name="dependencies">Provides components for accessing external system resources.</param>
        /// <param name="initialize">True to perform any initialization steps required to make the profiles available.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        protected async Task<IEnumerable<string>> EvaluateProfilesAsync(IServiceCollection dependencies, bool initialize = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

            List<string> effectiveProfiles = new List<string>();
            foreach (DependencyProfileReference profileReference in this.Profiles)
            {
                string profileFullPath = null;
                if (profileReference.ProfileUri != null)
                {
                    // The profile downloaded from internet will live in /profiles/downloads directory and not
                    // interfere with the out-of-box or extensions profiles.
                    profileFullPath = systemManagement.PlatformSpecifics.GetProfileDownloadsPath(profileReference.ProfileName);

                    if (initialize && !cancellationToken.IsCancellationRequested)
                    {
                        await this.DownloadProfileAsync(dependencies, profileReference, profileFullPath, cancellationToken);
                    }
                }
                else if (profileReference.IsFullPath)
                {
                    profileFullPath = systemManagement.PlatformSpecifics.StandardizePath(profileReference.ProfileName);
                }
                else
                {
                    string profileName = profileReference.ProfileName;
                    profileFullPath = systemManagement.PlatformSpecifics.GetProfilePath(profileName);

                    // If the profile defined is not a full path to a profile located on the system, then we
                    // fallback to looking for the profile in the 'profiles' directory within the Virtual Client
                    // parent directory itself or in any platform extensions locations.
                    string pathFound;
                    if (this.TryGetProfileFromDefaultLocation(systemManagement, profileName, out pathFound)
                        || this.TryGetProfileFromDownloadsLocation(systemManagement, profileName, out pathFound)
                        || this.TryGetProfileFromExtensionsLocation(profileName, out pathFound))
                    {
                        profileFullPath = pathFound;
                    }
                }

                if (initialize && !fileSystem.File.Exists(profileFullPath))
                {
                    // If the profile defined is not a full path to a profile located on the system, then we
                    // fallback to looking for the profile in the 'profiles' directory within the Virtual Client
                    // parent directory itself or in any platform extensions locations.
                    throw new StartupException(
                        $"Profile not found. Profile does not exist at the path '{profileFullPath}' nor in any extensions location.",
                        ErrorReason.ProfileNotFound);
                }

                effectiveProfiles.Add(profileFullPath);
            }

            return effectiveProfiles;
        }

        /// <summary>
        /// Initializes the command runtime before dependency initialization and execution.
        /// </summary>
        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            if (this.Targets?.Any() == true && string.IsNullOrWhiteSpace(this.Command))
            {
                throw new NotSupportedException(
                    "Command line usage is not supported. A command must be defined to execute on the target systems (via SSH). " +
                    "Use the --command option.");
            }

            if (this.Profiles?.Any() != true 
                && string.IsNullOrWhiteSpace(this.Command) 
                && !this.IsArchiveLogsRequested 
                && !this.IsCleanRequested)
            {
                throw new NotSupportedException(
                    "Command line usage is not supported. The intended command or profile execution intentions are unclear. " +
                    "Use the --profile or --command options.");
            }

            // When timing constraints/hints are not provided on the command line, we run the
            // application until it is explicitly stopped by the user or automation.
            if (this.Timeout == null && this.Iterations == null)
            {
                this.Timeout = ProfileTiming.OneIteration();
            }

            if (this.Profiles?.Any() == true || !string.IsNullOrWhiteSpace(this.Command))
            {
                // 1) Local command execution.
                if (!string.IsNullOrWhiteSpace(this.Command))
                {
                    List<DependencyProfileReference> profiles = new List<DependencyProfileReference>();
                    IConvertible package = null;
                    this.Parameters?.TryGetValue("Package", out package);

                    if (this.Targets?.Any() == true)
                    {
                        profiles.Add(new DependencyProfileReference(ExecuteProfileCommand.ExecuteSshCommandProfile));
                    }
                    else if (package != null)
                    {
                        profiles.Add(new DependencyProfileReference(ExecuteProfileCommand.ExecuteScriptProfile));
                    }

                    if (this.Profiles?.Any() == true)
                    {
                        profiles.AddRange(this.Profiles);
                    }

                    this.Profiles = profiles;
                    if (this.Parameters == null)
                    {
                        this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
                    }

                    string[] commandArguments = this.Command?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    PlatformSpecifics.TryGetCommandName(commandArguments, out string commandName);

                    string fullCommand = this.Command;
                    if (this.IsPowerShell)
                    {
                        fullCommand = ExecuteProfileCommand.NormalizeForPowerShell(this.Command);
                    }

                    this.Parameters["Command"] = fullCommand;
                    this.Parameters["Scenario"] = $"Execute_{Regex.Replace(commandName, "[^a-zA-Z0-9]", "_")}";
                }
            }
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args, PlatformSpecifics platformSpecifics)
        {
            IServiceCollection dependencies = base.InitializeDependencies(args, platformSpecifics);
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            dependencies.AddSingleton<ProfileTiming>(this.Timeout ?? this.Iterations);

            // Note that a bug was found in the version of "lshw" (B.02.18) that is installed on some Ubuntu images. The bug causes the
            // lshw application to return a "Segmentation Fault" error. We built the "lshw" command from the
            // GitHub site where it is maintained that has the bug fix for this. This custom built version is included
            // in the built-in packages for VC.
            if (systemManagement.DiskManager is UnixDiskManager)
            {
                DependencyPath lshwPackage = systemManagement.PackageManager.GetPackageAsync(PackageManager.BuiltInLshwPackageName, CancellationToken.None)
                    .GetAwaiter().GetResult();

                if (lshwPackage != null)
                {
                    lshwPackage = systemManagement.PlatformSpecifics.ToPlatformSpecificPath(
                        lshwPackage,
                        platformSpecifics.Platform,
                        platformSpecifics.CpuArchitecture);

                    if (systemManagement.FileSystem.Directory.Exists(lshwPackage.Path))
                    {
                        string lshwPath = Path.Combine(lshwPackage.Path, PackageManager.BuiltInLshwPackageName);
                        systemManagement.MakeFileExecutableAsync(lshwPath, platformSpecifics.Platform, CancellationToken.None)
                            .GetAwaiter().GetResult();

                        (systemManagement.DiskManager as UnixDiskManager).LshwExecutable = lshwPath;
                    }
                }
            }

            // Ensure profiles can be validated as correct.
            ExecutionProfileValidation.Instance.AddRange(new List<IValidationRule<ExecutionProfile>>()
            {
                SchemaRules.Instance
            });

            return dependencies;
        }

        /// <summary>
        /// Initializes the profile that will be executed.
        /// </summary>
        protected async Task<ExecutionProfile> InitializeProfilesAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            List<string> allProfiles = new List<string>();
            ExecutionProfile profile = await this.ReadExecutionProfileAsync(profiles.First(), dependencies, cancellationToken)
                .ConfigureAwait(false);

            await this.InitializeProfileAsync(profile, dependencies);

            if (profiles.Count() > 1)
            {
                foreach (string additionalProfile in profiles.Skip(1))
                {
                    ExecutionProfile otherProfile = await this.ReadExecutionProfileAsync(additionalProfile, dependencies, cancellationToken)
                        .ConfigureAwait(false);

                    await this.InitializeProfileAsync(otherProfile, dependencies);
                    profile = profile.MergeWith(otherProfile);
                }
            }

            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();

            // Adding file upload monitoring if the user has supplied a content store or Proxy Api Uri.
            if (this.ContentStore != null || this.ProxyApiUri != null)
            {
                string fileUploadMonitorProfilePath = systemManagement.PlatformSpecifics.GetProfilePath(ExecuteProfileCommand.FileUploadMonitorProfile);
                ExecutionProfile fileUploadMonitorProfile = await this.ReadExecutionProfileAsync(fileUploadMonitorProfilePath, dependencies, cancellationToken)
                    .ConfigureAwait(false);

                await this.InitializeProfileAsync(fileUploadMonitorProfile, dependencies);
                profile = profile.MergeWith(fileUploadMonitorProfile);
            }

            MetadataContract.Persist(
                profile.Metadata.Keys.ToDictionary(key => key, entry => profile.Metadata[entry] as object).ObscureSecrets(),
                MetadataContract.DefaultCategory);

            return profile;
        }

        /// <summary>
        /// Loads/reads the execution profile file provided to the Virtual Client on the command line.
        /// </summary>
        protected async Task<ExecutionProfile> ReadExecutionProfileAsync(string path, IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            // string profilePath = path;
            ExecutionProfile profile = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                ConsoleLogger.Default.LogMessage($"Execution Profile: {Path.GetFileNameWithoutExtension(path)}", EventContext.Persisted());

                IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
                string profileContent = (await fileSystem.File.ReadAllTextAsync(path)).Trim();

                // JSON profile content will always start with a '{' character
                if (profileContent.StartsWith("{", StringComparison.OrdinalIgnoreCase))
                {
                    profile = JsonConvert.DeserializeObject<ExecutionProfile>(profileContent);
                    profile.ProfileFormat = "JSON";
                }
                else
                {
                    var yamlSerializer = new YamlDotNet.Serialization.DeserializerBuilder()
                        .WithTypeConverter(new YamlParameterDictionaryTypeConverter())
                        .Build();

                    ExecutionProfileYamlShim profileShim = yamlSerializer.Deserialize<ExecutionProfileYamlShim>(profileContent);
                    profile = new ExecutionProfile(profileShim);
                    profile.ProfileFormat = "YAML";
                }
            }

            return profile;
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        protected override void SetGlobalTelemetryProperties(string[] args)
        {
            // Additional persistent/global telemetry properties in addition to the ones
            // added on application startup.
            base.SetGlobalTelemetryProperties(args);

            if (this.Profiles?.Any() == true)
            {
                DependencyProfileReference profile = this.Profiles.First();
                string profilePath = profile.ProfileName;
                string profileName = Path.GetFileName(profilePath);
                string platformSpecificProfileName = PlatformSpecifics.GetProfileName(profileName, Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

                // Additional persistent/global telemetry properties in addition to the ones
                // added on application startup.
                EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
                {
                    // Ex: PERF-CPU-OPENSSL (win-x64)
                    [MetadataContract.ExecutionProfile] = platformSpecificProfileName,

                    // Ex: PERF-CPU-OPENSSL.json
                    [MetadataContract.ExecutionProfileName] = profileName
                });
            }
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        protected void SetGlobalTelemetryProperties(ExecutionProfile profile)
        {
            // Additional persistent/global telemetry properties in addition to the ones
            // added on application startup.
            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                [MetadataContract.ExecutionProfileDescription] = profile.Description
            });
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        protected void SetGlobalTelemetryProperties(IEnumerable<string> profiles, IServiceCollection dependencies)
        {
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();

            string profile = profiles.First();
            string profileFullPath = systemManagement.PlatformSpecifics.StandardizePath(Path.GetFullPath(profile));

            // Additional persistent/global telemetry properties in addition to the ones
            // added on application startup.
            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                [MetadataContract.ExecutionProfilePath] = profileFullPath
            });

            try
            {
                EventContext.PersistentProperties[MetadataContract.LinuxDistribution] = systemManagement.GetLinuxDistributionAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch
            {
                // Best Effort only
            }
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        protected void SetHostMetadataTelemetryProperties(IEnumerable<string> profiles, IServiceCollection dependencies)
        {
            ILogger logger = dependencies.GetService<ILogger>();
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();

            IDictionary<string, object> hostMetadata = systemManagement.GetHostMetadataAsync(logger)
                .GetAwaiter().GetResult();

            // Hardware Parts metadata contains information on the physical hardware
            // parts on the system (e.g. CPU, memory chips, network cards).
            hostMetadata.AddRange(systemManagement.GetHardwarePartsMetadataAsync(logger)
                .GetAwaiter().GetResult());

            List<IDictionary<string, object>> partsMetadata = new List<IDictionary<string, object>>();

            MetadataContract.Persist(
                hostMetadata,
                MetadataContract.HostCategory);

            MetadataContract.Persist(
                new Dictionary<string, object>
                {
                    { "exitWait", this.ExitWait },
                    { "layout", this.Layout?.ToString() },
                    { "logToFile", this.LogToFile },
                    { "iterations", this.Iterations?.ProfileIterations },
                    { "profiles", string.Join(",", profiles.Select(p => Path.GetFileName(p))) },
                    { "timeout", this.Timeout?.Duration },
                    { "timeoutScope", this.Timeout?.LevelOfDeterminism.ToString() },
                    { "scenarios", this.Scenarios != null ? string.Join(",", this.Scenarios) : null },
                },
                MetadataContract.RuntimeCategory);
        }

        private async Task<PlatformExtensions> DiscoverExtensionsAsync(IPackageManager packageManager, CancellationToken cancellationToken)
        {
            return await packageManager.DiscoverExtensionsAsync(cancellationToken);
        }

        private async Task ExecuteProfileDependenciesInstallationAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            EventContext telemetryContext = EventContext.Persisted();

            // The user can supply more than 1 profile on the command line. The individual profiles will be merged
            // into a single profile for execution.
            ExecutionProfile profile = await this.InitializeProfilesAsync(profiles, dependencies, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext("executionProfileActions", profile.Actions?.Select(d => new
            {
                type = d.Type,
                parameters = d.Parameters?.ObscureSecrets()
            }));

            telemetryContext.AddContext("executionProfileDependencies", profile.Dependencies?.Select(d => new
            {
                type = d.Type,
                parameters = d.Parameters?.ObscureSecrets()
            }));

            telemetryContext.AddContext("executionProfileMonitors", profile.Monitors?.Select(d => new
            {
                type = d.Type,
                parameters = d.Parameters?.ObscureSecrets()
            }));

            this.SetGlobalTelemetryProperties(profile);

            if (this.Layout != null)
            {
                dependencies.AddSingleton<EnvironmentLayout>(this.Layout);
                telemetryContext.AddContext("layout", this.Layout);
            }

            logger.LogMessage($"ProfileExecution.Begin", telemetryContext);

            ComponentSettings componentSettings = new ComponentSettings
            {
                ContentPathTemplate = this.ContentPathTemplate,
                ExitWait = this.ExitWait,
                FailFast = this.FailFast,
                LogToFile = this.LogToFile,
                Seed = this.RandomizationSeed
            };

            // Only dependencies defined in the profile will be considered.
            dependencies.AddSingleton(profile);
            using (ProfileExecutor profileExecutor = new ProfileExecutor(profile, dependencies, componentSettings, this.Scenarios, logger))
            {
                profileExecutor.ExecuteActions = false;
                profileExecutor.ExecuteMonitors = false;

                profileExecutor.BeforeExiting += (source, args) =>
                {
                    this.ExitWaitTimeout = DateTime.UtcNow.SafeAdd(this.ExitWait);
                };

                await profileExecutor.ExecuteAsync(ProfileTiming.OneIteration(), cancellationToken)
                    .ConfigureAwait(false);
            }

            // If the dependencies installed include any packages that contain extensions, the extensions will
            // be installed/integrated into the VC runtime. This might include additional profiles or binaries
            // that contain actions, monitors or dependency component definitions.
            await this.DiscoverExtensionsAsync(systemManagement.PackageManager, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private async Task ExecuteProfileAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            EventContext telemetryContext = EventContext.Persisted();

            // The user can supply more than 1 profile on the command line. The individual profiles will be merged
            // into a single profile for execution.
            ExecutionProfile profile = await this.InitializeProfilesAsync(profiles, dependencies, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext("executionProfileActions", profile.Actions?.Select(d => new
            {
                type = d.Type,
                parameters = d.Parameters?.ObscureSecrets()
            }));

            telemetryContext.AddContext("executionProfileDependencies", profile.Dependencies?.Select(d => new
            {
                type = d.Type,
                parameters = d.Parameters?.ObscureSecrets()
            }));

            telemetryContext.AddContext("executionProfileMonitors", profile.Monitors?.Select(d => new
            {
                type = d.Type,
                parameters = d.Parameters?.ObscureSecrets()
            }));

            if (this.Timeout?.Duration != null && profile.Metadata?.TryGetValue("MinimumRequiredExecutionTime", out IConvertible minimumExecutionTime) == true)
            {
                if (TimeSpan.TryParse(minimumExecutionTime.ToString(), out TimeSpan minimumTime) && minimumTime > this.Timeout.Duration)
                {
                    throw new StartupException(
                        $"The profile(s) supplied has actions/workloads or monitors that require a minimum required execution time of '{minimumTime}' " +
                        $"which is longer than the duration/timeout supplied on the command line '{this.Timeout.Duration}'. Increase the duration/timeout of the command line to " +
                        $"a length of time that is longer than the minimum required execution time.");
                }
            }

            this.SetGlobalTelemetryProperties(profile);

            if (this.Layout != null)
            {
                dependencies.AddSingleton<EnvironmentLayout>(this.Layout);
                telemetryContext.AddContext("layout", this.Layout);
            }

            logger.LogMessage($"ProfileExecution.Begin", telemetryContext);

            if (this.Timeout?.ProfileIterations != null)
            {
                logger.LogMessage($"Iterations: {this.Timeout.ProfileIterations}", telemetryContext);
            }
            else if (this.Timeout?.Duration != null)
            {
                logger.LogMessage($"Duration: {this.Timeout.Duration}", telemetryContext);
            }
            
            this.Validate(dependencies, profile);

            ComponentSettings componentSettings = new ComponentSettings
            {
                ContentPathTemplate = this.ContentPathTemplate,
                ExitWait = this.ExitWait,
                FailFast = this.FailFast,
                LogToFile = this.LogToFile,
                Seed = this.RandomizationSeed
            };

            dependencies.AddSingleton(profile);
            using (ProfileExecutor profileExecutor = new ProfileExecutor(profile, dependencies, componentSettings, this.Scenarios, logger))
            {
                profileExecutor.BeforeExiting += (source, args) =>
                {
                    this.ExitWaitTimeout = DateTime.UtcNow.SafeAdd(this.ExitWait);
                };

                // Profile timeout and iterations options are mutually-exclusive on the command line. They cannot be used
                // at the same time.
                await profileExecutor.ExecuteAsync(this.Timeout ?? this.Iterations, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task InitializeProfileAsync(ExecutionProfile profile, IServiceCollection dependencies)
        {
            if (this.Metadata?.Any() == true)
            {
                // Command-line metadata overrides metadata in the profile itself.
                profile.Metadata.AddRange(this.Metadata, true);
            }

            if (this.Parameters?.Any() == true)
            {
                // Command-line parameters override parameters defined in the profile
                // itself.
                profile.Parameters.AddRange(this.Parameters, true);
            }

            // Process conditional parameters (ParametersOn feature)
            await profile.EvaluateConditionalParametersAsync(dependencies);

            ValidationResult result = ExecutionProfileValidation.Instance.Validate(profile);
            result.ThrowIfInvalid();

            profile.Inline();
        }

        private Task LoadExtensionsBinariesAsync(PlatformExtensions extensions, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (extensions?.Binaries?.Any() == true)
                {
                    IEnumerable<string> binaryDirectories = extensions.Binaries.Select(bin => bin.DirectoryName).Distinct();
                    if (binaryDirectories?.Any() == true)
                    {
                        foreach (string directory in binaryDirectories)
                        {
                            ComponentTypeCache.Instance.LoadComponentTypes(directory);
                        }
                    }

                    // Load supporting assemblies
                    foreach (IFileInfo binary in extensions.Binaries)
                    {
                        ComponentTypeCache.Instance.LoadAssembly(binary.FullName);
                    }
                }
            });
        }

        private void LogContextToConsole(IServiceCollection dependencies)
        {
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();

            EventContext telemetryContext = EventContext.Persisted();
            ConsoleLogger.Default.LogMessage($"Experiment ID: {this.ExperimentId}", telemetryContext);
            ConsoleLogger.Default.LogMessage($"Client ID: {this.ClientId}", telemetryContext);
            ConsoleLogger.Default.LogMessage($"Log To File: {this.LogToFile}", telemetryContext);
            ConsoleLogger.Default.LogMessage($"Log Directory: {platformSpecifics.LogsDirectory}", telemetryContext);
            ConsoleLogger.Default.LogMessage($"Package Directory: {platformSpecifics.PackagesDirectory}", telemetryContext);
            ConsoleLogger.Default.LogMessage($"State Directory: {platformSpecifics.StateDirectory}", telemetryContext);
            ConsoleLogger.Default.LogMessage($"Temp Directory: {platformSpecifics.TempDirectory}", telemetryContext);

            if (this.Layout != null)
            {
                ConsoleLogger.Default.LogMessage($"Environment Layout: {this.Layout}", telemetryContext);
            }

            if (this.Timeout?.Duration != null)
            {
                switch (this.Timeout.LevelOfDeterminism)
                {
                    case DeterminismScope.AllActions:
                        ConsoleLogger.Default.LogMessage($"Duration: {this.Timeout.Duration},deterministic*", telemetryContext);
                        break;

                    case DeterminismScope.IndividualAction:
                        ConsoleLogger.Default.LogMessage($"Duration: {this.Timeout.Duration},deterministic", telemetryContext);
                        break;

                    default:
                        ConsoleLogger.Default.LogMessage($"Duration: {this.Timeout.Duration}", telemetryContext);
                        break;
                }
            }

            if (this.Iterations?.ProfileIterations != null)
            {
                ConsoleLogger.Default.LogMessage($"Iterations: {this.Iterations.ProfileIterations}", telemetryContext);
            }
        }

        private bool TryGetProfileFromDefaultLocation(ISystemManagement systemManagement, string profileName, out string profilePath)
        {
            profilePath = null;
            string filePath = systemManagement.PlatformSpecifics.GetProfilePath(profileName);

            if (systemManagement.FileSystem.File.Exists(filePath))
            {
                profilePath = filePath;
            }

            return profilePath != null;
        }

        private bool TryGetProfileFromDownloadsLocation(ISystemManagement systemManagement, string profileName, out string profilePath)
        {
            profilePath = null;
            string filePath = systemManagement.PlatformSpecifics.GetProfileDownloadsPath(profileName);

            if (systemManagement.FileSystem.File.Exists(filePath))
            {
                profilePath = filePath;
            }

            return profilePath != null;
        }

        private bool TryGetProfileFromExtensionsLocation(string profileName, out string profilePath)
        {
            profilePath = null;
            if (this.Extensions?.Profiles?.Any() == true)
            {
                IFileInfo file = this.Extensions.Profiles.FirstOrDefault(p => string.Equals(p.Name, profileName));

                if (file != null && file.Exists)
                {
                    profilePath = file.FullName;
                }
            }

            return profilePath != null;
        }

        private void Validate(IServiceCollection dependencies, ExecutionProfile profile)
        {
            ProfileTiming timing = dependencies.GetService<ProfileTiming>();

            if (profile.Metadata?.Any() == true
                && profile.Metadata.TryGetValue(ProfileMetadata.SupportsIterations, out IConvertible supportsIterations)
                && timing.ProfileIterations != null
                && supportsIterations.ToBoolean(CultureInfo.InvariantCulture) == false)
            {
                throw new NotSupportedException(
                    $"Iterations not supported. One or more of the profiles supplied on the command line have metadata indicating that " +
                    "iterations (e.g. --iterations) is not supported.");
            }
        }
    }
}
