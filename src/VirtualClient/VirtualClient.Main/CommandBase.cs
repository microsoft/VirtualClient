// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Configuration;
    using VirtualClient.Contracts;
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
        /// Blob store that is behind (backed by) a proxy API service endpoint. Blobs are uploaded
        /// or downloaded from the proxy endpoint.
        /// </summary>
        public Uri ProxyApiUri { get; set; }

        /// <summary>
        /// Blob store to use for uploading Virtual Client content/monitoring files.
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
        /// A connection string to the Event Hub into which telemetry will be uploaded.
        /// </summary>
        public string EventHubConnectionString { get; set; }

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
        /// Blob store to use for downloading dependencies/workload packages required
        /// by Virtual Client profiles.
        /// </summary>
        public DependencyStore PackageStore { get; set; }

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

            this.InitializeGlobalTelemetryProperties(args);

            IConfiguration configuration = Program.LoadAppSettings();

            IConvertible telemetrySource = null;
            this.Parameters?.TryGetValue(GlobalParameter.TelemetrySource, out telemetrySource);

            ILogger logger = CommandBase.CreateLogger(
                configuration,
                platformSpecifics,
                this.EventHubConnectionString,
                this.ProxyApiUri,
                this.Debug,
                telemetrySource?.ToString());

            List<IBlobManager> blobStores = new List<IBlobManager>();

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

                ILogger debugLogger = DependencyFactory.CreateFileLoggerProvider(platformSpecifics.GetLogsPath("proxy-traces-blobs.log"), TimeSpan.FromSeconds(5))
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
            dependencies.AddSingleton<ILogger>(logger);
            dependencies.AddSingleton<IConfiguration>(configuration);
            dependencies.AddSingleton<IApiClientManager>(new ApiClientManager(this.ApiPorts));
            dependencies.AddSingleton<IExpressionEvaluator>(ProfileExpressionEvaluator.Instance);
            dependencies.AddSingleton<PlatformSpecifics>(platformSpecifics);
            dependencies.AddSingleton<IEnumerable<IBlobManager>>(blobStores);

            return dependencies;
        }

        private static void AddConsoleLogging(List<ILoggerProvider> loggerProviders, bool debugMode)
        {
            loggerProviders.Add(new VirtualClient.ConsoleLoggerProvider(LogLevel.Trace)
                .WithFilter((eventId, logLevel, state) =>
                {
                    return logLevel >= LogLevel.Warning || eventId.Id == (int)LogType.Trace && debugMode == true;
                }));
        }

        private static void AddEventHubLogging(List<ILoggerProvider> loggingProviders, IConfiguration configuration, string eventHubConnectionString)
        {
            if (!string.IsNullOrWhiteSpace(eventHubConnectionString))
            {
                EventHubLogSettings settings = new EventHubLogSettings();
                configuration.Bind(nameof(EventHubLogSettings), settings);

                if (settings.IsEnabled)
                {
                    IEnumerable<ILoggerProvider> eventHubProviders = DependencyFactory.CreateEventHubLoggerProviders(eventHubConnectionString, settings);
                    if (eventHubProviders?.Any() == true)
                    {
                        loggingProviders.AddRange(eventHubProviders);
                    }
                }
            }
        }

        private static void AddFileLogging(List<ILoggerProvider> loggingProviders, IConfiguration configuration)
        {
            FileLogSettings settings = new FileLogSettings();
            configuration.Bind(nameof(FileLogSettings), settings);

            if (settings.IsEnabled)
            {
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);
                IEnumerable<ILoggerProvider> logProviders = DependencyFactory.CreateFileLoggerProviders(platformSpecifics.LogsDirectory, settings);

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
                ILogger debugLogger = DependencyFactory.CreateFileLoggerProvider(specifics.GetLogsPath("proxy-traces.log"), TimeSpan.FromSeconds(5))
                    .CreateLogger("Proxy");

                CommandBase.proxyApiDebugLoggers.Add(debugLogger);

                VirtualClientProxyApiClient proxyApiClient = DependencyFactory.CreateVirtualClientProxyApiClient(proxyApiUri);
                ProxyTelemetryChannel telemetryChannel = DependencyFactory.CreateProxyTelemetryChannel(proxyApiClient, debugLogger);

                loggingProviders.Add(new ProxyLoggerProvider(telemetryChannel, source));
            }
        }

        private static ILogger CreateLogger(IConfiguration configuration, PlatformSpecifics specifics, string eventHubConnectionString, Uri proxyApiUri, bool debugMode, string source = null)
        {
            // Application loggers. Events are routed to different loggers based upon
            // the EventId defined when the message is logged (e.g. Trace, Error, SystemEvent, TestMetrics).
            List<ILoggerProvider> loggingProviders = new List<ILoggerProvider>();

            CommandBase.AddConsoleLogging(loggingProviders, debugMode);
            CommandBase.AddFileLogging(loggingProviders, configuration);

            if (proxyApiUri != null)
            {
                CommandBase.AddProxyApiLogging(loggingProviders, configuration, specifics, proxyApiUri, source);
            }
            else
            {
                CommandBase.AddEventHubLogging(loggingProviders, configuration, eventHubConnectionString);
            }

            return loggingProviders.Any() ? new LoggerFactory(loggingProviders).CreateLogger("VirtualClient") : NullLogger.Instance;
        }

        private void InitializeGlobalTelemetryProperties(string[] args)
        {
            this.GetVersionInfo(out string platformVersion, out string extensionsVersion);
            
            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                ["clientId"] = this.AgentId.ToLowerInvariant(),
                ["appVersion"] = extensionsVersion ?? platformVersion,
                ["appPlatformVersion"] = platformVersion,
                ["executionArguments"] = SensitiveData.ObscureSecrets(string.Join(" ", args)),
                ["executionSystem"] = this.ExecutionSystem,
                ["operatingSystemPlatform"] = Environment.OSVersion.Platform.ToString(),
                ["platformArchitecture"] = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
            });
        }
    }
}
