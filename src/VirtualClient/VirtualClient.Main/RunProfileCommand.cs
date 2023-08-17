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
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Contracts.Validation;
    using VirtualClient.Metadata;

    /// <summary>
    /// Command executes the operations of the Virtual Client workload profile. This is the
    /// default/root command for the Virtual Client application.
    /// </summary>
    internal class RunProfileCommand : CommandBase
    {
        private static readonly Uri DefaultBlobStoreUri = new Uri("https://virtualclient.blob.core.windows.net/");
        private const string DefaultMonitorsProfile = "MONITORS-DEFAULT.json";
        private const string NoMonitorsProfile = "MONITORS-NONE.json";
        private const string FileUploadMonitorProfile = "MONITORS-FILE-UPLOAD.json";

        /// <summary>
        /// The ID to use for the experiment and to include in telemetry output.
        /// </summary>
        public string ExperimentId { get; set; }

        /// <summary>
        /// True if the profile dependencies should be installed as the only operations. False if
        /// the profile actions and monitors should also be considered.
        /// </summary>
        public bool InstallDependencies { get; set; }

        /// <summary>
        /// The path to the environment layout .json file.
        /// </summary>
        public string LayoutPath { get; set; }

        /// <summary>
        /// The workload/monitoring profiles to execute (e.g. PERF-CPU-OPENSSL.json).
        /// </summary>
        public List<string> Profiles { get; set; }

        /// <summary>
        /// A seed that can be used to guarantee identical randomization bases for workloads that
        /// require it.
        /// </summary>
        public int RandomizationSeed { get; set; }

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
        /// Executes the profile operations.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;
            ILogger logger = null;
            IPackageManager packageManager = null;
            IServiceCollection dependencies = null;
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                this.SetGlobalTelemetryProperties();

                // When timing constraints/hints are not provided on the command line, we run the
                // application until it is explicitly stopped by the user or automation.
                if (this.Timeout == null && this.Iterations == null)
                {
                    this.Timeout = ProfileTiming.Forever();
                }

                // 1) Setup any dependencies required to execute the workload profile.
                this.ApplyBackwardsCompatibilityRequirements();
                dependencies = this.InitializeDependencies(args);
                logger = dependencies.GetService<ILogger>();
                packageManager = dependencies.GetService<IPackageManager>();
                VirtualClientComponent.ContentStorePathTemplate = this.ContentStorePathTemplate;

                IEnumerable<string> profileNames = this.GetProfilePaths(dependencies);
                this.SetGlobalTelemetryProperties(profileNames, dependencies);

                // Extracts and registers any packages that are pre-existing on the system (e.g. they exist in
                // the 'packages' directory already).
                await this.InitializePackagesAsync(packageManager, cancellationToken)
                    .ConfigureAwait(false);

                // Installs any extensions that are pre-existing on the system (e.g. they exist in
                // the 'packages' directory already).
                await this.InstallExtensionsAsync(packageManager, cancellationToken)
                    .ConfigureAwait(false);

                // Ensure all Virtual Client types are loaded from .dlls in the execution directory.
                ComponentTypeCache.Instance.LoadComponentTypes(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).Location));

                IEnumerable<string> effectiveProfiles = await this.InitializeProfilesAsync(dependencies, cancellationToken)
                    .ConfigureAwait(false);

                if (this.InstallDependencies)
                {
                    await this.ExecuteProfileDependenciesInstallationAsync(effectiveProfiles, dependencies, cancellationTokenSource)
                        .ConfigureAwait(false);
                }
                else
                {
                    await this.ExecuteProfileAsync(effectiveProfiles, dependencies, cancellationTokenSource)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the Ctrl-C is pressed to cancel operation.
            }
            catch (NotSupportedException exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = (int)ErrorReason.NotSupported;
            }
            catch (VirtualClientException exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = (int)exc.Reason;
            }
            catch (Exception exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = 1;
            }
            finally
            {
                // In order to include all of the experiment + agent etc... context, we need to
                // get the current/persisted context.
                EventContext exitingContext = EventContext.Persisted();

                // Allow components to handle any final exit operations.
                VirtualClientRuntime.OnExiting();

                if (VirtualClientRuntime.IsRebootRequested)
                {
                    Program.LogMessage(logger, $"{nameof(RunProfileCommand)}.RebootingSystem", exitingContext);
                }

                Program.LogMessage(logger, $"{nameof(RunProfileCommand)}.End", exitingContext);
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
                DependencyFactory.FlushTelemetry(remainingWait);

                Program.LogMessage(logger, $"Flushed", exitingContext);
                DependencyFactory.FlushTelemetry(TimeSpan.FromMinutes(1));

                // Allow components to handle any final cleanup operations.
                VirtualClientRuntime.OnCleanup();

                // Reboots must happen after telemetry is flushed and just before the application is exiting. This ensures
                // we capture all important telemetry and allow the profile execution operations to exit gracefully before
                // we suddenly reboot the system.
                if (VirtualClientRuntime.IsRebootRequested)
                {
                    await CommandBase.RebootSystemAsync(dependencies)
                        .ConfigureAwait(false);
                }
            }

            return exitCode;
        }

        /// <summary>
        /// Performs any changes that need to be made to support backwards compatibility
        /// requirements for the application.
        /// </summary>
        protected void ApplyBackwardsCompatibilityRequirements()
        {
            this.AgentId = BackwardsCompatibility.GetAgentId(this.AgentId, this.Metadata);
            this.ExperimentId = BackwardsCompatibility.GetExperimentId(this.ExperimentId, this.Metadata);

            // Preserve backwards compatibility with the previous way of referencing a blob store using
            // an AccountKey supplied on the command line. In the future, we will be removing the need to 
            // supply the AccountKey as a parameter on the command line (e.g. --parameters:AccountKey={key})
            // in favor of supplying the package store connection string/SAS URI (e.g. --packagesStore={connectionstring}).
            if (this.PackageStore == null && this.Parameters?.Any() == true)
            {
                if (this.Parameters.TryGetValue("AccountKey", out IConvertible accountKey))
                {
                    string connectionToken =
                        $"DefaultEndpointsProtocol=https;" +
                        $"AccountName={RunProfileCommand.DefaultBlobStoreUri.Host.Split('.').First()};" +
                        $"AccountKey={accountKey};" +
                        $"EndpointSuffix=core.windows.net";

                    this.PackageStore = new DependencyBlobStore(DependencyStore.Packages, connectionToken);
                }
            }
        }

        /// <summary>
        /// Returns the full paths to the profiles specified on the command line.
        /// </summary>
        protected IEnumerable<string> GetProfilePaths(IServiceCollection dependencies)
        {
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

            List<string> effectiveProfiles = new List<string>();
            foreach (string path in this.Profiles)
            {
                string profileFullPath = null;
                if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var profileUri = new Uri(path);
                    string profileName = Path.GetFileName(profileUri.AbsolutePath);
                    profileFullPath = systemManagement.PlatformSpecifics.GetProfilePath(profileName);
                }
                else
                {
                    profileFullPath = systemManagement.PlatformSpecifics.StandardizePath(path);

                    if (BackwardsCompatibility.TryMapProfile(profileFullPath, out string remappedProfile))
                    {
                        profileFullPath = remappedProfile;
                    }

                    if (!fileSystem.File.Exists(profileFullPath))
                    {
                        // If the profile defined is not a full path to a profile located on the system, then we
                        // fallback to looking for the profile in the 'profiles' directory within the Virtual Client
                        // parent directory itself.
                        profileFullPath = systemManagement.PlatformSpecifics.GetProfilePath(path);
                    }
                }

                effectiveProfiles.Add(profileFullPath);
            }

            return effectiveProfiles;
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args)
        {
            IServiceCollection dependencies = base.InitializeDependencies(args);
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();
            ILogger logger = dependencies.GetService<ILogger>();
            Program.Logger = logger;

            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                this.AgentId,
                this.ExperimentId,
                platformSpecifics.Platform,
                platformSpecifics.CpuArchitecture,
                logger);

            IApiManager apiManager = new ApiManager(systemManagement.FirewallManager);

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

            dependencies.AddSingleton<ISystemInfo>(systemManagement);
            dependencies.AddSingleton<ISystemManagement>(systemManagement);
            dependencies.AddSingleton<IApiManager>(apiManager);
            dependencies.AddSingleton<ProcessManager>(systemManagement.ProcessManager);
            dependencies.AddSingleton<IDiskManager>(systemManagement.DiskManager);
            dependencies.AddSingleton<IFileSystem>(systemManagement.FileSystem);
            dependencies.AddSingleton<IFirewallManager>(systemManagement.FirewallManager);
            dependencies.AddSingleton<IPackageManager>(systemManagement.PackageManager);
            dependencies.AddSingleton<IStateManager>(systemManagement.StateManager);
            dependencies.AddSingleton<ProfileTiming>(this.Timeout ?? this.Iterations);

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
        protected async Task<ExecutionProfile> InitializeProfileAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            ExecutionProfile profile = await this.ReadExecutionProfileAsync(profiles.First(), dependencies, cancellationToken)
                .ConfigureAwait(false);

            this.InitializeProfile(profile);

            if (profiles.Count() > 1)
            {
                foreach (string additionalProfile in profiles.Skip(1))
                {
                    ExecutionProfile otherProfile = await this.ReadExecutionProfileAsync(additionalProfile, dependencies, cancellationToken)
                        .ConfigureAwait(false);

                    this.InitializeProfile(otherProfile);
                    profile = profile.MergeWith(otherProfile);
                }
            }

            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();

            // If we are not just installing dependencies, then we may include a default monitor
            // profile.
            if (!this.InstallDependencies)
            {
                if (profile.Actions.Any()
                   && !profiles.Any(p => p.Contains(RunProfileCommand.NoMonitorsProfile, StringComparison.OrdinalIgnoreCase))
                   && !profile.Monitors.Any())
                {
                    // We always run the default monitoring profile if a specific monitor profile is not provided.
                    
                    string defaultMonitorProfilePath = systemManagement.PlatformSpecifics.GetProfilePath(RunProfileCommand.DefaultMonitorsProfile);
                    ExecutionProfile defaultMonitorProfile = await this.ReadExecutionProfileAsync(defaultMonitorProfilePath, dependencies, cancellationToken)
                        .ConfigureAwait(false);

                    this.InitializeProfile(defaultMonitorProfile);
                    profile = profile.MergeWith(defaultMonitorProfile);
                }
            }

            // Adding file upload monitoring if the user has supplied a content store or Proxy Api Uri.
            if (this.ContentStore != null || this.ProxyApiUri != null)
            {
                string fileUploadMonitorProfilePath = systemManagement.PlatformSpecifics.GetProfilePath(RunProfileCommand.FileUploadMonitorProfile);
                ExecutionProfile fileUploadMonitorProfile = await this.ReadExecutionProfileAsync(fileUploadMonitorProfilePath, dependencies, cancellationToken)
                    .ConfigureAwait(false);

                this.InitializeProfile(fileUploadMonitorProfile);
                profile = profile.MergeWith(fileUploadMonitorProfile);
            }

            return profile;
        }

        /// <summary>
        /// Validates the existence of the profiles specified downloading them as needed.
        /// </summary>
        protected async Task<IEnumerable<string>> InitializeProfilesAsync(IServiceCollection dependencies, CancellationToken cancellationToken, bool pathsOnly = false)
        {
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

            List<string> effectiveProfiles = new List<string>();
            foreach (string path in this.Profiles)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    // Virtual Client profiles can be downloaded from a cloud storage location using a simple URI reference. This reference
                    // can additionally include a SAS URI for authentication where desired.
                    // 
                    // e.g.
                    // Anonymous Read: https://any.blob.core.windows.net/profiles/ANY-PROFILE.json
                    // Authenticated:  https://any.blob.core.windows.net/profiles/ANY-PROFILE.json?sp=r&st=2022-09-11T19:28:36Z&se=2022-09-12T03:28:36Z&spr=https&sv=2021-06-08&sr=c&...

                    string profileFullPath = null;
                    if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        var profileUri = new Uri(path);
                        string profileName = Path.GetFileName(profileUri.AbsolutePath);
                        profileFullPath = systemManagement.PlatformSpecifics.GetProfilePath(profileName);

                        if (!pathsOnly)
                        {
                            using (var client = new HttpClient())
                            {
                                await Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries * 2)).ExecuteAsync(async () =>
                                {
                                    var response = await client.GetAsync(profileUri);
                                    using (var fs = new FileStream(profileFullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                                    {
                                        await response.Content.CopyToAsync(fs);
                                    }
                                }).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        profileFullPath = systemManagement.PlatformSpecifics.StandardizePath(path);

                        if (BackwardsCompatibility.TryMapProfile(profileFullPath, out string remappedProfile))
                        {
                            profileFullPath = remappedProfile;
                        }

                        if (!fileSystem.File.Exists(profileFullPath))
                        {
                            // If the profile defined is not a full path to a profile located on the system, then we
                            // fallback to looking for the profile in the 'profiles' directory within the Virtual Client
                            // parent directory itself.
                            profileFullPath = systemManagement.PlatformSpecifics.GetProfilePath(path);
                            if (!pathsOnly && !fileSystem.File.Exists(profileFullPath))
                            {
                                throw new DependencyException($"Profile does not exist at the path '{path}'.", ErrorReason.ProfileNotFound);
                            }
                        }
                    }

                    effectiveProfiles.Add(profileFullPath);
                }
            }

            return effectiveProfiles;
        }

        /// <summary>
        /// Loads/Reads the environment layout file provided to the Virtual Client on the command line.
        /// </summary>
        protected async Task<EnvironmentLayout> ReadEnvironmentLayoutAsync(IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            EnvironmentLayout layout = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                if (!string.IsNullOrWhiteSpace(this.LayoutPath))
                {
                    ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                    ILogger logger = dependencies.GetService<ILogger>();

                    string layoutFullPath = systemManagement.PlatformSpecifics.StandardizePath(Path.GetFullPath(this.LayoutPath));

                    if (!systemManagement.FileSystem.File.Exists(layoutFullPath))
                    {
                        throw new FileNotFoundException(
                            $"Invalid path specified. An environment layout file does not exist at path '{layoutFullPath}'.");
                    }

                    logger.LogTraceMessage($"Environment Layout: {layoutFullPath}");

                    string layoutContent = await systemManagement.FileSystem.File.ReadAllTextAsync(layoutFullPath)
                        .ConfigureAwait(false);

                    layout = layoutContent.FromJson<EnvironmentLayout>();
                }
            }

            return layout;
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
                ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                IFileSystem fileSystem = systemManagement.FileSystem;
                ILogger logger = dependencies.GetService<ILogger>();
                
                logger.LogTraceMessage($"Execution Profile: {Path.GetFileNameWithoutExtension(path)}");

                string profileContent = await fileSystem.File.ReadAllTextAsync(path);
                profile = JsonConvert.DeserializeObject<ExecutionProfile>(profileContent);
            }

            return profile;
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        protected void SetGlobalTelemetryProperties()
        {
            // Additional persistent/global telemetry properties in addition to the ones
            // added on application startup.
            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                ["experimentId"] = this.ExperimentId.ToLowerInvariant(),
                ["executionProfileParameters"] = this.Parameters?.ObscureSecrets()
            });
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
                ["executionProfileDescription"] = profile.Description,
                ["profileFriendlyName"] = profile.Description,
            });
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        protected void SetGlobalTelemetryProperties(IEnumerable<string> profiles, IServiceCollection dependencies)
        {
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            string profile = profiles.First();
            string profileName = Path.GetFileName(profile);
            string profileFullPath = systemManagement.PlatformSpecifics.StandardizePath(Path.GetFullPath(profile));
            string platformSpecificProfileName = PlatformSpecifics.GetProfileName(profileName, systemManagement.Platform, systemManagement.CpuArchitecture);

            // Additional persistent/global telemetry properties in addition to the ones
            // added on application startup.
            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                // Ex: PERF-CPU-OPENSSL (win-x64)
                ["executionProfile"] = platformSpecificProfileName,

                // Ex: PERF-CPU-OPENSSL.json
                ["executionProfileName"] = profileName,
                ["executionProfilePath"] = profileFullPath
            });


            IDictionary<string, object> metadata = new Dictionary<string, object>();

            if (this.Metadata?.Any() == true)
            {
                this.Metadata.ToList().ForEach(entry =>
                {
                    metadata[entry.Key] = entry.Value;
                });
            }

            // For backwards compatibility, ensure that the experiment ID and agent ID
            // values are a part of the metadata. This is required for the original VC table
            // JSON mappings that expect these properties to exist in the metadata supplied to
            // VC on the command line.
            metadata["experimentId"] = this.ExperimentId.ToLowerInvariant();
            metadata["agentId"] = this.AgentId;

            MetadataContract.Persist(metadata, MetadataContractCategory.Default);

            IDictionary<string, object> hostMetadata = systemManagement.GetHostMetadataAsync(logger)
                .GetAwaiter().GetResult();

            // Hardware Parts metadata contains information on the physical hardware
            // parts on the system (e.g. CPU, memory chips, network cards).
            hostMetadata.AddRange(systemManagement.GetHardwarePartsMetadataAsync(logger)
                .GetAwaiter().GetResult());

            List<IDictionary<string, object>> partsMetadata = new List<IDictionary<string, object>>();

            MetadataContract.Persist(
                hostMetadata,
                MetadataContractCategory.Host);

            MetadataContract.Persist(
                new Dictionary<string, object>
                {
                    { "exitWait", this.ExitWait },
                    { "layout", this.LayoutPath },
                    { "logToFile", this.LogToFile },
                    { "iterations", this.Iterations?.ProfileIterations },
                    { "profiles", string.Join(",", profiles.Select(p => Path.GetFileName(p))) },
                    { "timeout", this.Timeout?.Duration },
                    { "timeoutScope", this.Timeout?.LevelOfDeterminism.ToString() },
                    { "scenarios", this.Scenarios != null ? string.Join(",", this.Scenarios) : null },
                },
                MetadataContractCategory.Runtime);
        }

        private async Task CaptureSystemInfoAsync(IServiceCollection dependencies, CancellationToken cancellationToken)
        {
            try
            {
                ILogger logger = dependencies.GetService<ILogger>();
                ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
                IEnumerable<IDictionary<string, IConvertible>> systemDetails = await systemManagement.GetSystemDetailedInfoAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (systemDetails?.Any() == true)
                {
                    List<KeyValuePair<string, object>> systemInfo = new List<KeyValuePair<string, object>>();
                    foreach (var entry in systemDetails)
                    {
                        systemInfo.Add(new KeyValuePair<string, object>("SystemInfo", entry));
                    }

                    logger.LogSystemEvents("SystemInfo", systemInfo, EventContext.Persisted());
                }
            }
            catch
            {
                // Best Effort only
            }
        }

        private async Task ExecuteProfileDependenciesInstallationAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            logger.LogMessage($"{nameof(RunProfileCommand)}.Begin", EventContext.Persisted());
            logger.LogTraceMessage($"Experiment ID: {this.ExperimentId}");
            logger.LogTraceMessage($"Agent ID: {this.AgentId}");
            logger.LogTraceMessage($"Log To File: {VirtualClientComponent.LogToFile}");

            // The user can supply more than 1 profile on the command line. The individual profiles will be merged
            // into a single profile for execution.
            ExecutionProfile profile = await this.InitializeProfileAsync(profiles, dependencies, cancellationToken)
                .ConfigureAwait(false);

            this.SetGlobalTelemetryProperties(profile);

            await this.CaptureSystemInfoAsync(dependencies, cancellationToken)
                .ConfigureAwait(false);

            // The environment layout provides information for other Virtual Client instances
            // that may be a part of the workload execution. This enables support for client/server
            // workload requirements.
            EnvironmentLayout environmentLayout = await this.ReadEnvironmentLayoutAsync(dependencies, cancellationToken)
                .ConfigureAwait(false);

            if (environmentLayout != null)
            {
                dependencies.AddSingleton<EnvironmentLayout>(environmentLayout);
            }

            // Only dependencies defined in the profile will be considered.
            using (ProfileExecutor profileExecutor = new ProfileExecutor(profile, dependencies, this.Scenarios, this.Metadata, logger))
            {
                profileExecutor.ExecuteActions = false;
                profileExecutor.ExecuteMonitors = false;
                profileExecutor.ExitWait = this.ExitWait;

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
            await this.InstallExtensionsAsync(systemManagement.PackageManager, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private async Task ExecuteProfileAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IFileSystem fileSystem = dependencies.GetService<IFileSystem>();
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            logger.LogMessage($"{nameof(RunProfileCommand)}.Begin", EventContext.Persisted());
            logger.LogTraceMessage($"Experiment ID: {this.ExperimentId}");
            logger.LogTraceMessage($"Agent ID: {this.AgentId}");
            logger.LogTraceMessage($"Log To File: {VirtualClientComponent.LogToFile}");

            // The user can supply more than 1 profile on the command line. The individual profiles will be merged
            // into a single profile for execution.
            ExecutionProfile profile = await this.InitializeProfileAsync(profiles, dependencies, cancellationToken)
                .ConfigureAwait(false);

            if (this.Timeout?.Duration != null)
            {
                if (profile.MinimumRequiredExecutionTime != null && profile.MinimumRequiredExecutionTime > this.Timeout.Duration)
                {
                    throw new StartupException(
                        $"The profile(s) supplied has actions/workloads or monitors that require a minimum required execution time of '{profile.MinimumRequiredExecutionTime}' " +
                        $"which is longer than the duration/timeout supplied on the command line '{this.Timeout.Duration}'. Increase the duration/timeout of the command line to " +
                        $"a length of time that is longer than the minimum required execution time.");
                }

                logger.LogTraceMessage($"Duration: {this.Timeout.Duration}");
            }

            this.SetGlobalTelemetryProperties(profile);

            await this.CaptureSystemInfoAsync(dependencies, cancellationToken)
                .ConfigureAwait(false);

            // The environment layout provides information for other Virtual Client instances
            // that may be a part of the workload execution. This enables support for client/server
            // workload requirements.
            EnvironmentLayout environmentLayout = await this.ReadEnvironmentLayoutAsync(dependencies, cancellationToken)
                .ConfigureAwait(false);

            if (environmentLayout != null)
            {
                dependencies.AddSingleton<EnvironmentLayout>(environmentLayout);
            }

            this.Validate(dependencies, profile);

            using (ProfileExecutor profileExecutor = new ProfileExecutor(profile, dependencies, this.Scenarios, this.Metadata, logger))
            {
                profileExecutor.RandomizationSeed = this.RandomizationSeed;
                profileExecutor.ExitWait = this.ExitWait;

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

        private void InitializeProfile(ExecutionProfile profile)
        {
            if (this.Parameters?.Any() == true)
            {
                profile.Parameters.AddRange(this.Parameters, true);
            }

            ValidationResult result = ExecutionProfileValidation.Instance.Validate(profile);
            result.ThrowIfInvalid();
            profile.Inline();
        }

        private async Task InitializePackagesAsync(IPackageManager packageManager, CancellationToken cancellationToken)
        {
            // 3) Initialize, discover and register any pre-existing packages on the system.
            await packageManager.InitializePackagesAsync(cancellationToken)
                .ConfigureAwait(false);

            IEnumerable<DependencyPath> packages = await packageManager.DiscoverPackagesAsync(cancellationToken)
                .ConfigureAwait(false);

            if (packages?.Any() == true)
            {
                await packageManager.RegisterPackagesAsync(packages, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task InstallExtensionsAsync(IPackageManager packageManager, CancellationToken cancellationToken)
        {
            IEnumerable<DependencyPath> extensions = await packageManager.DiscoverExtensionsAsync(cancellationToken)
                .ConfigureAwait(false);

            if (extensions?.Any() == true)
            {
                await packageManager.InstallExtensionsAsync(extensions, cancellationToken)
                    .ConfigureAwait(false);
            }
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
