// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Cleanup;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Configuration;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Contracts.Proxy;
    using VirtualClient.Proxy;

    /// <summary>
    /// Base class for Virtual Client commands.
    /// </summary>
    public abstract class CommandBase
    {
        private static List<ILogger> proxyApiDebugLoggers = new List<ILogger>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBase"/> class.
        /// </summary>
        protected CommandBase()
        {
        }

        /// <summary>
        /// The ID to use as the identifier for the agent (i.e. the instance of Virtual Client)
        /// and to include in telemetry output.
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        /// The port(s) to use to listen to HTTP traffic for the Virtual Client REST API.
        /// </summary>
        public IDictionary<string, int> ApiPorts { get; set; }

        /// <summary>
        /// A set of target resources to clean (e.g. logs, packages, state, all).
        /// </summary>
        public IList<string> CleanTargets { get; set; }

        /// <summary>
        /// Describes the target store to which content files/logs should be uploaded.
        /// </summary>
        public DependencyStore ContentStore { get; set; }

        /// <summary>
        /// Parameter defines the content path format/structure using a template to use when uploading content
        /// to target storage resources. When not defined the 'Default' structure is used.
        /// </summary>
        public string ContentPathTemplate { get; set; }

        /// <summary>s
        /// True to have debug/verbose output emitted to standard output on
        /// the console/terminal.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Describes the target Event Hub namespace to which telemetry should be sent.
        /// </summary>
        public DependencyEventHubStore EventHubStore { get; set; }

        /// <summary>
        /// The execution system/environment platform (e.g. Azure).
        /// </summary>
        public string ExecutionSystem { get; set; }

        /// <summary>
        /// Defines the time at which the application will wait for the application to wait for processes
        /// to exit or for telemetry to be flushed before exiting regardless.
        /// </summary>
        public TimeSpan ExitWait { get; set; }

        /// <summary>
        /// Defines an explicit time for which the application will wait before exiting. This is correlated with
        /// the exit/flush wait supplied by the user on the command line.
        /// </summary>
        public DateTime ExitWaitTimeout { get; set; }

        /// <summary>
        /// The ID to use for the experiment and to include in telemetry output.
        /// </summary>
        public string ExperimentId { get; set; }

        /// <summary>
        /// True if a request to perform clean operations was requested on the
        /// command line.
        /// </summary>
        public bool IsCleanRequested
        {
            get
            {
                return this.CleanTargets != null || this.LogRetention != null;
            }
        }

        /// <summary>
        /// The logging level for the application (0 = Trace, 1 = Debug, 2 = Information, 3 = Warning, 4 = Error, 5 = Critical).
        /// </summary>
        public LogLevel LoggingLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// The retention period to keep log files. If not defined, log files will be left on
        /// the system indefinitely.
        /// </summary>
        public TimeSpan? LogRetention { get; set; }

        /// <summary>
        /// True if the output of processes executed should be logged to files in
        /// the logs directory.
        /// </summary>
        public bool LogToFile { get; set; }

        /// <summary>
        /// Metadata properties (key/value pairs) supplied to the application.
        /// </summary>
        public IDictionary<string, IConvertible> Metadata { get; set; }

        /// <summary>
        /// Additional or override parameters (key/value pairs) supplied to the application.
        /// </summary>
        public IDictionary<string, IConvertible> Parameters { get; set; }

        /// <summary>
        /// Describes the target store from which dependency packages should be downloaded.
        /// </summary>
        public DependencyStore PackageStore { get; set; }

        /// <summary>
        /// Blob store that is behind (backed by) a proxy API service endpoint. Blobs are uploaded
        /// or downloaded from the proxy endpoint.
        /// </summary>
        public Uri ProxyApiUri { get; set; }

        /// <summary>
        /// Issues a request to the OS to reboot.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies for requesting a reboot.</param>
        public static Task RebootSystemAsync(IServiceCollection dependencies)
        {
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            return systemManagement.RebootSystemAsync(CancellationToken.None);
        }

        /// <summary>
        /// Executes the default workload execution command.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public abstract Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource);

        /// <summary>
        /// Executes clean/reset operations within the Virtual Client application folder. This is used to 
        /// cleanup resources such as the "logs", "packages" and "state" directories.
        /// </summary>
        /// <param name="systemManagement">Provides system management functions required to execute the clean operations.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <param name="logger"></param>
        protected async Task CleanAsync(ISystemManagement systemManagement, CancellationToken cancellationToken, ILogger logger = null)
        {
            VirtualClient.Contracts.CleanTargets targets = null;
            DateTime? logRetentionDate = null;

            if (this.LogRetention != null)
            {
                logRetentionDate = DateTime.UtcNow.Subtract(this.LogRetention.Value);
            }

            if (this.CleanTargets != null)
            {
                if (this.CleanTargets?.Any() != true)
                {
                    // --clean used as a flag
                    targets = VirtualClient.Contracts.CleanTargets.Create();
                }
                else
                {
                    targets = VirtualClient.Contracts.CleanTargets.Create(this.CleanTargets);
                }

                Type commandType = this.GetType();
                EventContext telemetryContext = EventContext.Persisted();

                telemetryContext.AddContext("cleanTargets", this.CleanTargets);

                if (this.LogRetention != null)
                {
                    telemetryContext.AddContext("logRetention", this.LogRetention);
                }

                if (targets.CleanLogs)
                {
                    try
                    {
                        await (logger ?? NullLogger.Instance).LogMessageAsync($"{commandType.Name}.CleanLogs", LogLevel.Trace, telemetryContext, async () =>
                        {
                            await systemManagement.CleanLogsDirectoryAsync(cancellationToken, logRetentionDate);
                        });
                    }
                    catch
                    {
                        // Best effort.
                    }
                }

                if (targets.CleanPackages)
                {
                    await (logger ?? NullLogger.Instance).LogMessageAsync($"{commandType.Name}.CleanPackages", LogLevel.Trace, telemetryContext, async () =>
                    {
                        await systemManagement.CleanPackagesDirectoryAsync(cancellationToken);
                    });
                }

                if (targets.CleanState)
                {
                    await (logger ?? NullLogger.Instance).LogMessageAsync($"{commandType.Name}.CleanState", LogLevel.Trace, telemetryContext, async () =>
                    {
                        await systemManagement.CleanStateDirectoryAsync(cancellationToken);
                    });
                }
            }
            else if (logRetentionDate != null)
            {
                Type commandType = this.GetType();
                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("logRetention", this.LogRetention);

                try
                {
                    await (logger ?? NullLogger.Instance).LogMessageAsync($"{commandType.Name}.CleanLogs", LogLevel.Trace, telemetryContext, async () =>
                    {
                        await systemManagement.CleanLogsDirectoryAsync(cancellationToken, logRetentionDate);
                    });
                }
                catch
                {
                    // Best effort.
                }
            }
        }

        /// <summary>
        /// Returns assembly version information for the application.
        /// </summary>
        /// <param name="platformVersion">The version of the open source platform core..</param>
        /// <param name="extensionsVersion">The version of any extensions to the platform.</param>
        protected void GetVersionInfo(out string platformVersion, out string extensionsVersion)
        {
            platformVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            extensionsVersion = null;

            // If a VC extension dll exist, then the appVersion in telemetry will be replaced by the extension version.
            string[] versionDllFiles = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*VirtualClient.Version.dll");
            if (versionDllFiles.Length > 0)
            {
                extensionsVersion = versionDllFiles.ToList().Select(f => FileVersionInfo.GetVersionInfo(f).FileVersion).FirstOrDefault();
            }
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected virtual IServiceCollection InitializeDependencies(string[] args)
        {
            PlatformID osPlatform = Environment.OSVersion.Platform;
            Architecture cpuArchitecture = RuntimeInformation.ProcessArchitecture;

            PlatformSpecifics.ThrowIfNotSupported(osPlatform);
            PlatformSpecifics.ThrowIfNotSupported(cpuArchitecture);
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(osPlatform, cpuArchitecture);

            // Users can override the default packages directory using the 'VC_PACKAGES_DIR'
            // environment variable. When defined, VC will download packages to this directory
            // instead of the default location.
            string userDefinedPackageDirectory = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR);
            if (!string.IsNullOrWhiteSpace(userDefinedPackageDirectory))
            {
                platformSpecifics.PackagesDirectory = userDefinedPackageDirectory;
            }

            if (this.Debug)
            {
                this.LoggingLevel = LogLevel.Trace;
            }

            if (this.Metadata?.Any() == true)
            {
                VirtualClientRuntime.Metadata.AddRange(this.Metadata, true);
            }

            if (this.Parameters?.Any() == true)
            {
                VirtualClientRuntime.Parameters.AddRange(this.Parameters, true);
            }

            IConfiguration configuration = Program.LoadAppSettings();
            IConvertible telemetrySource = null;
            this.Parameters?.TryGetValue(GlobalParameter.TelemetrySource, out telemetrySource);

            ILogger logger = CommandBase.CreateLogger(
                configuration,
                platformSpecifics,
                this.EventHubStore,
                this.ProxyApiUri,
                this.LoggingLevel,
                telemetrySource?.ToString());

            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                this.AgentId,
                this.ExperimentId,
                platformSpecifics,
                logger);

            IApiManager apiManager = new ApiManager(systemManagement.FirewallManager);
            IProfileManager profileManager = new ProfileManager();
            List <IBlobManager> blobStores = new List<IBlobManager>();

            // The Virtual Client supports a proxy API interface. When a proxy API is used, all dependencies/blobs will be download
            // through the proxy endpoint. All content/files will be uploaded through the proxy endpoint. All telemetry will be uploaded
            // the proxy endpoint (with the exception of file logging which remains as-is). This enables Virtual Client to support disconnected
            // scenarios where the system on which the Virtual Client is running does not have public internet access but can access a system
            // on the same local network that has that access (e.g. hardware manufacturing facility scenarios).
            if (this.ProxyApiUri != null)
            {
                IConvertible contentSource = null;
                IConvertible packageSource = null;
                this.Parameters?.TryGetValue(GlobalParameter.ContestStoreSource, out contentSource);
                this.Parameters?.TryGetValue(GlobalParameter.PackageStoreSource, out packageSource);

                ILogger debugLogger = DependencyFactory.CreateFileLoggerProvider(platformSpecifics.GetLogsPath("proxy-traces-blobs.log"), TimeSpan.FromSeconds(5), LogLevel.Trace)
                    .CreateLogger("Proxy");

                CommandBase.proxyApiDebugLoggers.Add(debugLogger);

                blobStores.Add(DependencyFactory.CreateProxyBlobManager(new DependencyProxyStore(DependencyBlobStore.Content, this.ProxyApiUri), contentSource?.ToString(), debugLogger));
                blobStores.Add(DependencyFactory.CreateProxyBlobManager(new DependencyProxyStore(DependencyBlobStore.Packages, this.ProxyApiUri), packageSource?.ToString(), debugLogger));
            }
            else
            {
                if (this.ContentStore != null)
                {
                    blobStores.Add(DependencyFactory.CreateBlobManager(this.ContentStore));
                }

                if (this.PackageStore != null)
                {
                    blobStores.Add(DependencyFactory.CreateBlobManager(this.PackageStore));
                }
            }

            IServiceCollection dependencies = new ServiceCollection();
            dependencies.AddSingleton<PlatformSpecifics>(platformSpecifics);
            dependencies.AddSingleton<IApiManager>(apiManager);
            dependencies.AddSingleton<IApiClientManager>(new ApiClientManager(this.ApiPorts));
            dependencies.AddSingleton<IConfiguration>(configuration);
            dependencies.AddSingleton<IDiskManager>(systemManagement.DiskManager);
            dependencies.AddSingleton<IExpressionEvaluator>(ProfileExpressionEvaluator.Instance);
            dependencies.AddSingleton<IEnumerable<IBlobManager>>(blobStores);
            dependencies.AddSingleton<IFileSystem>(systemManagement.FileSystem);
            dependencies.AddSingleton<IFirewallManager>(systemManagement.FirewallManager);
            dependencies.AddSingleton<ILogger>(logger);
            dependencies.AddSingleton<IPackageManager>(systemManagement.PackageManager);
            dependencies.AddSingleton<IProfileManager>(profileManager);
            dependencies.AddSingleton<IStateManager>(systemManagement.StateManager);
            dependencies.AddSingleton<ISystemInfo>(systemManagement);
            dependencies.AddSingleton<ISystemManagement>(systemManagement);
            dependencies.AddSingleton<ProcessManager>(systemManagement.ProcessManager);

            return dependencies;
        }

        /// <summary>
        /// Initializes and registers existing dependency packages and toolset packages on the system.
        /// </summary>
        /// <param name="packageManager">Provides package management facilities required to discover and register packages.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        protected virtual async Task InitializePackagesAsync(IPackageManager packageManager, CancellationToken cancellationToken)
        {
            // 3) Initialize, discover and register any pre-existing packages on the system.
            await packageManager.InitializePackagesAsync(cancellationToken);

            IEnumerable<DependencyPath> packages = await packageManager.DiscoverPackagesAsync(cancellationToken);

            if (packages?.Any() == true)
            {
                await packageManager.RegisterPackagesAsync(packages, cancellationToken);
            }
        }

        /// <summary>
        /// Initializes the global/persistent telemetry properties that will be included
        /// with all telemetry emitted from the Virtual Client.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        protected virtual void SetGlobalTelemetryProperties(string[] args)
        {
            this.GetVersionInfo(out string platformVersion, out string extensionsVersion);

            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                ["clientId"] = this.AgentId.ToLowerInvariant(),
                ["clientInstance"] = Guid.NewGuid().ToString(),
                ["appVersion"] = extensionsVersion ?? platformVersion,
                ["appPlatformVersion"] = platformVersion,
                ["executionArguments"] = SensitiveData.ObscureSecrets(string.Join(" ", args)),
                ["executionSystem"] = this.ExecutionSystem,
                ["operatingSystemPlatform"] = Environment.OSVersion.Platform.ToString(),
                ["platformArchitecture"] = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
            });

            IDictionary<string, IConvertible> parameters = this.Parameters?.ObscureSecrets();
            EventContext.PersistentProperties["executionProfileParameters"] = parameters;
            EventContext.PersistentProperties["parameters"] = parameters;

            IDictionary<string, object> metadata = new Dictionary<string, object>();

            if (this.Metadata?.Any() == true)
            {
                this.Metadata.ToList().ForEach(entry =>
                {
                    metadata[entry.Key] = entry.Value;
                });
            }

            EventContext.PersistentProperties["metadata"] = metadata;

            MetadataContract.Persist(
                metadata?.ToDictionary(entry => entry.Key, entry => entry.Value as object),
                MetadataContractCategory.Default);
        }

        private static void AddConsoleLogging(List<ILoggerProvider> loggerProviders, LogLevel level)
        {
            loggerProviders.Add(new VirtualClient.ConsoleLoggerProvider(level)
                .HandleTraceEvents());
        }

        private static void AddEventHubLogging(List<ILoggerProvider> loggingProviders, IConfiguration configuration, DependencyEventHubStore eventHubStore, LogLevel level)
        {
            if (eventHubStore != null)
            {
                EventHubLogSettings settings = configuration.GetSection(nameof(EventHubLogSettings)).Get<EventHubLogSettings>();

                if (settings.IsEnabled)
                {
                    IEnumerable<ILoggerProvider> eventHubProviders = DependencyFactory.CreateEventHubLoggerProviders(eventHubStore, settings, level);
                    if (eventHubProviders?.Any() == true)
                    {
                        loggingProviders.AddRange(eventHubProviders);
                    }
                }
            }
        }

        private static void AddFileLogging(List<ILoggerProvider> loggingProviders, IConfiguration configuration, LogLevel level)
        {
            FileLogSettings settings = configuration.GetSection(nameof(FileLogSettings)).Get<FileLogSettings>();

            if (settings.IsEnabled)
            {
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);
                IEnumerable<ILoggerProvider> logProviders = DependencyFactory.CreateFileLoggerProviders(platformSpecifics.LogsDirectory, settings, level);

                if (loggingProviders?.Any() == true)
                {
                    loggingProviders.AddRange(logProviders);
                }
            }
        }

        private static void AddProxyApiLogging(List<ILoggerProvider> loggingProviders, IConfiguration configuration, PlatformSpecifics specifics, Uri proxyApiUri, string source = null)
        {
            if (proxyApiUri != null)
            {
                ILogger debugLogger = DependencyFactory.CreateFileLoggerProvider(specifics.GetLogsPath("proxy-traces.log"), TimeSpan.FromSeconds(5), LogLevel.Trace)
                    .CreateLogger("Proxy");

                CommandBase.proxyApiDebugLoggers.Add(debugLogger);

                VirtualClientProxyApiClient proxyApiClient = DependencyFactory.CreateVirtualClientProxyApiClient(proxyApiUri);
                ProxyTelemetryChannel telemetryChannel = DependencyFactory.CreateProxyTelemetryChannel(proxyApiClient, debugLogger);

                loggingProviders.Add(new ProxyLoggerProvider(telemetryChannel, source));
            }
        }

        private static ILogger CreateLogger(IConfiguration configuration, PlatformSpecifics specifics, DependencyEventHubStore eventHubStore, Uri proxyApiUri, LogLevel level, string source = null)
        {
            // Application loggers. Events are routed to different loggers based upon
            // the EventId defined when the message is logged (e.g. Trace, Error, SystemEvent, TestMetrics).
            List<ILoggerProvider> loggingProviders = new List<ILoggerProvider>();

            CommandBase.AddConsoleLogging(loggingProviders, level);
            CommandBase.AddFileLogging(loggingProviders, configuration, level);

            if (proxyApiUri != null)
            {
                CommandBase.AddProxyApiLogging(loggingProviders, configuration, specifics, proxyApiUri, source);
            }
            else
            {
                CommandBase.AddEventHubLogging(loggingProviders, configuration, eventHubStore, level);
            }

            return loggingProviders.Any() ? new LoggerFactory(loggingProviders).CreateLogger("VirtualClient") : NullLogger.Instance;
        }
    }
}
