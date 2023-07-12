// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Producer;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Configuration;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;
    using VirtualClient.Logging;
    using VirtualClient.Proxy;

    /// <summary>
    /// Factory for creating workload runtime dependencies.
    /// </summary>
    public static class DependencyFactory
    {
        private static List<IFlushableChannel> telemetryChannels = new List<IFlushableChannel>();

        /// <summary>
        /// Creates an <see cref="IBlobManager"/> instance that can be used to download blobs/files from a store or
        /// upload blobs/files to a store.
        /// </summary>
        /// <param name="dependencyStore">Describes the type of dependency store.</param>
        public static IBlobManager CreateBlobManager(DependencyStore dependencyStore)
        {
            IBlobManager blobManager = null;
            switch (dependencyStore.StoreType)
            {
                case DependencyStore.StoreTypeAzureStorageBlob:
                    DependencyBlobStore blobStore = dependencyStore as DependencyBlobStore;
                    if (blobStore != null)
                    {
                        blobManager = new BlobManager(blobStore);
                    }

                    break;

                case DependencyStore.StoreTypeFileSystem:
                    DependencyFileStore fileStore = dependencyStore as DependencyFileStore;
                    if (fileStore != null)
                    {
                        // We will be adding this support in soon.
                        throw new NotSupportedException($"The dependency store type provided '{DependencyStore.StoreTypeFileSystem}' is not supported.");
                    }

                    break;

                default:
                    throw new NotSupportedException($"The dependency store type provided '{dependencyStore.StoreType}' is not supported.");
            }

            if (blobManager == null)
            {
                throw new DependencyException(
                    $"Required dependency store information not provided. A dependency store of type '{dependencyStore.StoreType}' is missing " +
                    $"required information or was provided in an unsupported format.",
                    ErrorReason.DependencyDescriptionInvalid);
            }

            return blobManager;
        }

        /// <summary>
        /// Creates a disk manager for the OS/system platform (e.g. Windows, Linux).
        /// </summary>
        /// <param name="platform">The OS/system platform.</param>
        /// <param name="logger">A logger for capturing disk management telemetry.</param>
        public static DiskManager CreateDiskManager(PlatformID platform, Microsoft.Extensions.Logging.ILogger logger = null)
        {
            DiskManager manager = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    manager = new WindowsDiskManager(new WindowsProcessManager(), logger);
                    break;

                case PlatformID.Unix:
                    manager = new UnixDiskManager(new UnixProcessManager(), logger);
                    break;

                default:
                    throw new NotSupportedException($"Disk management features are not yet supported for '{platform}' platforms.");
            }

            return manager;
        }

        /// <summary>
        /// Creates an Event Hub channel targeting the hub provided.
        /// </summary>
        /// <param name="eventHubConnectionString">The connection string to the Event Hub namespace.</param>
        /// <param name="eventHubName">The name of the Event Hub within the namespace (e.g. telemetry-logs, telemetry-metrics).</param>
        public static EventHubTelemetryChannel CreateEventHubTelemetryChannel(string eventHubConnectionString, string eventHubName)
        {
            var client = new EventHubProducerClient(eventHubConnectionString, eventHubName);
            EventHubTelemetryChannel channel = new EventHubTelemetryChannel(client, enableDiagnostics: true);

            DependencyFactory.telemetryChannels.Add(channel);
            VirtualClientRuntime.CleanupTasks.Add(new Action_(() => channel.Dispose()));
            return channel;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to Event Hub targets.
        /// </summary>
        /// <param name="eventHubConnectionString">The connection string to the Event Hub namespace.</param>
        /// <param name="settings">Defines the settings for each individual Event Hub targeted.</param>
        public static IEnumerable<ILoggerProvider> CreateEventHubLoggerProviders(string eventHubConnectionString, EventHubLogSettings settings)
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();

            if (!string.IsNullOrWhiteSpace(eventHubConnectionString) && settings != null)
            {
                // Logs/Traces
                EventHubTelemetryChannel tracesChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                eventHubConnectionString,
                settings.TracesHubName);

                tracesChannel.EventTransmissionError += (sender, args) =>
                {
                    ConsoleLogger.Default.LogWarning($"Event Hub Transmission Error (traces): {args.Error.Message}");
                };

                ILoggerProvider tracesLoggerProvider = new EventHubTelemetryLoggerProvider(tracesChannel)
                    .HandleTraceEvents();

                loggerProviders.Add(tracesLoggerProvider);

                // Test Metrics/Results
                EventHubTelemetryChannel metricsChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                    eventHubConnectionString,
                    settings.MetricsHubName);

                metricsChannel.EventTransmissionError += (sender, args) =>
                {
                    ConsoleLogger.Default.LogWarning($"Event Hub Transmission Error (metrics): {args.Error.Message}");
                };

                ILoggerProvider metricsLoggerProvider = new EventHubTelemetryLoggerProvider(metricsChannel)
                    .HandleMetricsEvents();

                loggerProviders.Add(metricsLoggerProvider);

                // System Events
                EventHubTelemetryChannel systemEventsChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                    eventHubConnectionString,
                    settings.EventsHubName);

                systemEventsChannel.EventTransmissionError += (sender, args) =>
                {
                    ConsoleLogger.Default.LogWarning($"Event Hub Transmission Error (events): {args.Error.Message}");
                };

                ILoggerProvider eventsLoggerProvider = new EventHubTelemetryLoggerProvider(systemEventsChannel)
                    .HandleSystemEvents();

                loggerProviders.Add(eventsLoggerProvider);
            }

            return loggerProviders;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to local log files.
        /// </summary>
        /// <param name="logFilePath">The full path for the log file (e.g. C:\users\any\VirtualClient\logs\traces.log).</param>
        /// <param name="flushInterval">The interval at which the information should be flushed to disk.</param>
        /// <param name="excludes">Properties to exclude from JSON structured output.</param>
        public static ILoggerProvider CreateFileLoggerProvider(string logFilePath, TimeSpan flushInterval, IEnumerable<string> excludes = null)
        {
            logFilePath.ThrowIfNullOrWhiteSpace(nameof(logFilePath));

            // 100MB
            const long maxFileSizeBytes = 100000000;

            ILoggerProvider loggerProvider = null;

            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                LoggerConfiguration logConfiguration = new LoggerConfiguration().WriteTo.RollingFile(
                    new JsonTextFormatter(excludes),
                    logFilePath,
                    fileSizeLimitBytes: maxFileSizeBytes,
                    retainedFileCountLimit: 10,
                    flushToDiskInterval: flushInterval);

                loggerProvider = new SerilogFileLoggerProvider(logConfiguration);

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => loggerProvider.Dispose()));
            }

            return loggerProvider;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to local CSV files.
        /// </summary>
        /// <param name="csvFilePath">The full path for the log file (e.g. C:\users\any\VirtualClient\logs\metrics.csv).</param>
        public static ILoggerProvider CreateCsvFileLoggerProvider(string csvFilePath)
        {
            csvFilePath.ThrowIfNullOrWhiteSpace(nameof(csvFilePath));

            // 50MB
            // General Sizing:
            // Around 86,000 metrics will fit inside of a single CSV file at 50MB.
            const long maxFileSizeBytes = 50000000;

            ILoggerProvider loggerProvider = null;

            if (!string.IsNullOrWhiteSpace(csvFilePath))
            {
                loggerProvider = new MetricsCsvFileLoggerProvider(csvFilePath, maxFileSizeBytes);
                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => loggerProvider.Dispose()));
            }

            return loggerProvider;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to local log files.
        /// </summary>
        /// <param name="logFileDirectory">The path to the directory where log files are written.</param>
        /// <param name="settings">Defines the settings for each log file that will be written.</param>
        public static IEnumerable<ILoggerProvider> CreateFileLoggerProviders(string logFileDirectory, FileLogSettings settings)
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            List<string> excludes = new List<string>
            {
                "executionPlatform",
                "executionProfile",
                "executionProfileDescription",
                "executionProfileParameters",
                "profileFriendlyName"
            };

            List<string> metricsExcludes = new List<string>(excludes)
            {
                "binaryVersion",
                "transactionId",
                "durationMs",
                "executionArguments",
                "operatingSystemPlatform"
            };

            if (!string.IsNullOrWhiteSpace(logFileDirectory) && settings != null)
            {
                // Logs/Traces
                ILoggerProvider tracesLoggerProvider = DependencyFactory.CreateFileLoggerProvider(Path.Combine(logFileDirectory, settings.TracesFileName), TimeSpan.FromSeconds(5), excludes)
                    .HandleTraceEvents();

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => tracesLoggerProvider.Dispose()));
                loggerProviders.Add(tracesLoggerProvider);

                // Metrics/Results
                ILoggerProvider metricsLoggerProvider = DependencyFactory.CreateFileLoggerProvider(Path.Combine(logFileDirectory, settings.MetricsFileName), TimeSpan.FromSeconds(3), metricsExcludes)
                    .HandleMetricsEvents();

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => metricsLoggerProvider.Dispose()));
                loggerProviders.Add(metricsLoggerProvider);

                // Metrics/Results in CSV Format
                ILoggerProvider metricsCsvLoggerProvider = DependencyFactory.CreateCsvFileLoggerProvider(Path.Combine(logFileDirectory, settings.MetricsCsvFileName))
                    .HandleMetricsEvents();

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => metricsCsvLoggerProvider.Dispose()));
                loggerProviders.Add(metricsCsvLoggerProvider);

                // Performance Counters
                ILoggerProvider countersLoggerProvider = DependencyFactory.CreateFileLoggerProvider(Path.Combine(logFileDirectory, settings.CountersFileName), TimeSpan.FromSeconds(5), metricsExcludes)
                    .HandlePerformanceCounterEvents();

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => countersLoggerProvider.Dispose()));
                loggerProviders.Add(countersLoggerProvider);

                // System Events
                ILoggerProvider eventsLoggerProvider = DependencyFactory.CreateFileLoggerProvider(Path.Combine(logFileDirectory, settings.EventsFileName), TimeSpan.FromSeconds(5), excludes)
                    .HandleSystemEvents();

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => eventsLoggerProvider.Dispose()));
                loggerProviders.Add(eventsLoggerProvider);
            }

            return loggerProviders;
        }

        /// <summary>
        /// Factory method creates a firewall manager specific to the system/platform.
        /// </summary>
        public static IFirewallManager CreateFirewallManager(PlatformID platform, ProcessManager processManager)
        {
            FirewallManager manager = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    manager = new WindowsFirewallManager(processManager);
                    break;

                case PlatformID.Unix:
                    manager = new UnixFirewallManager(processManager);
                    break;

                default:
                    throw new NotSupportedException($"Firewall management features are not yet supported for '{platform}' platforms.");
            }

            return manager;
        }

        /// <summary>
        /// Creates an <see cref="IBlobManager"/> that uploads or downloads files from a proxy API
        /// endpoint.
        /// </summary>
        /// <param name="storeDescription">Describes the type of blob store (e.g. Content, Packages).</param>
        /// <param name="source">An explicit source to use for blob uploads/downloads through the proxy API.</param>
        /// <param name="logger">A logger to use for capturing information related to blob upload/download operations.</param>
        public static IBlobManager CreateProxyBlobManager(DependencyProxyStore storeDescription, string source = null, Microsoft.Extensions.Logging.ILogger logger = null)
        {
            storeDescription.ThrowIfNull(nameof(storeDescription));

            VirtualClientProxyApiClient proxyApiClient = DependencyFactory.CreateVirtualClientProxyApiClient(storeDescription.ProxyApiUri, TimeSpan.FromHours(6));
            ProxyBlobManager blobManager = new ProxyBlobManager(storeDescription, proxyApiClient, source);

            if (logger != null)
            {
                blobManager.BlobDownload += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("blob", args.Descriptor)
                            .AddContext("context", args.Context);

                        logger?.LogMessage($"{nameof(ProxyBlobManager)}.BlobDownload", LogLevel.Information, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };

                blobManager.BlobDownloadError += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("blob", args.Descriptor)
                            .AddContext("context", args.Context)
                            .AddError(args.Error);

                        logger?.LogMessage($"{nameof(ProxyBlobManager)}.BlobDownloadError", LogLevel.Error, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };

                blobManager.BlobUpload += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("blob", args.Descriptor)
                            .AddContext("context", args.Context);

                        logger?.LogMessage($"{nameof(ProxyBlobManager)}.BlobUpload", LogLevel.Information, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };

                blobManager.BlobUploadError += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("blob", args.Descriptor)
                            .AddContext("context", args.Context)
                            .AddError(args.Error);

                        logger?.LogMessage($"{nameof(ProxyBlobManager)}.BlobUploadError", LogLevel.Error, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };
            }

            return blobManager;
        }

        /// <summary>
        /// Creates a <see cref="ProxyTelemetryChannel"/> for uploading telemetry to a Proxy API endpoint.
        /// </summary>
        /// <param name="proxyApiClient">Proxy API client for uploading telemetry.</param>
        /// <param name="logger">A logger to use for capturing information related to channel/message operations.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Not relevant here.")]
        public static ProxyTelemetryChannel CreateProxyTelemetryChannel(IProxyApiClient proxyApiClient, Microsoft.Extensions.Logging.ILogger logger = null)
        {
            ProxyTelemetryChannel channel = new ProxyTelemetryChannel(proxyApiClient);

            if (logger != null)
            {
                channel.MessagesDropped += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("messages", args.Messages)
                            .AddContext("context", args.Context)
                            .AddError(args.Error);

                        logger?.LogMessage($"{nameof(ProxyTelemetryChannel)}.MessagesDropped", LogLevel.Error, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };

                channel.MessageTransmissionError += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("messages", args.Messages)
                            .AddContext("context", args.Context)
                            .AddError(args.Error);

                        logger?.LogMessage($"{nameof(ProxyTelemetryChannel)}.MessageTransmissionError", LogLevel.Error, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };

                channel.MessagesTransmitted += (sender, args) =>
                {
                    try
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("messages", args.Messages?.Select(msg => new
                            {
                                message = msg.Message,
                                severityLevel = msg.SeverityLevel.ToString(),
                                operationId = msg.OperationId
                            }))
                            .AddContext("context", args.Context);

                        logger?.LogMessage($"{nameof(ProxyTelemetryChannel)}.MessagesTransmitted", LogLevel.Information, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };

                channel.FlushMessages += (sender, args) =>
                {
                    try
                    {
                        // While flushing, let's lower the wait time in between failed Proxy API calls.
                        (sender as ProxyTelemetryChannel).TransmissionFailureWaitTime = TimeSpan.FromMilliseconds(500);

                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("messages", args.Messages?.Select(msg => new
                            {
                                message = msg.Message,
                                severityLevel = msg.SeverityLevel.ToString(),
                                operationId = msg.OperationId
                            }))
                            .AddContext("context", args.Context);

                        logger?.LogMessage($"{nameof(ProxyTelemetryChannel)}.FlushingMessages", LogLevel.Information, telemetryContext);
                    }
                    catch
                    {
                        // Best effort. Should not cause errors for the callers/invokers of the
                        // event handler.
                    }
                };
            }

            DependencyFactory.telemetryChannels.Add(channel);

            return channel;
        }

        /// <summary>
        /// Creates a new <see cref="SystemManagement" /> instance.
        /// </summary>
        /// <param name="agentId">The ID of the agent as part of the larger experiment in operation.</param>
        /// <param name="experimentId">The ID of the larger experiment in operation.</param>
        /// <param name="platform">The OS/system platform hosting the application (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU/processor architecture (e.g. amd64, arm, x86).</param>
        /// <param name="logger">A logger used to capture telemetry.</param>
        public static ISystemManagement CreateSystemManager(
            string agentId,
            string experimentId,
            PlatformID platform,
            Architecture architecture,
            Microsoft.Extensions.Logging.ILogger logger = null)
        {
            agentId.ThrowIfNullOrWhiteSpace(nameof(agentId));
            experimentId.ThrowIfNullOrWhiteSpace(nameof(experimentId));

            PlatformSpecifics platformSpecifics = new PlatformSpecifics(platform, architecture);
            IFileSystem fileSystem = new FileSystem();
            ProcessManager processManager = ProcessManager.Create(platform);
            IStateManager stateManager = new StateManager(fileSystem, platformSpecifics);
            IStateManager packageStateManager = new PackageStateManager(fileSystem, platformSpecifics);

            return new SystemManagement
            {
                AgentId = agentId,
                ExperimentId = experimentId.ToLowerInvariant(),
                DiskManager = DependencyFactory.CreateDiskManager(platform, logger),
                FileSystem = fileSystem,
                FirewallManager = DependencyFactory.CreateFirewallManager(platform, processManager),
                PackageManager = new PackageManager(packageStateManager, fileSystem, platformSpecifics, logger),
                PlatformSpecifics = platformSpecifics,
                ProcessManager = processManager,
                StateManager = stateManager
            };
        }

        /// <summary>
        /// Creates an <see cref="VirtualClientApiClient"/> that can be used to communicate with the Virtual Client API
        /// service.
        /// </summary>
        /// <param name="apiUri">The IP address of the system hosting the Virtual Client API/service.</param>
        public static VirtualClientApiClient CreateVirtualClientApiClient(Uri apiUri)
        {
            apiUri.ThrowIfNull(nameof(apiUri));

            IRestClient restClient = new RestClientBuilder()
                .AlwaysTrustServerCertificate()
                .AddAcceptedMediaType(MediaType.Json)
                .Build();

            return new VirtualClientApiClient(restClient, apiUri);
        }

        /// <summary>
        /// Creates an <see cref="VirtualClientApiClient"/> that can be used to communicate with the Virtual Client API
        /// service.
        /// </summary>
        /// <param name="ipAddress">The IP address of the system hosting the Virtual Client API/service.</param>
        /// <param name="port">The port for communications with the API/service.</param>
        public static VirtualClientApiClient CreateVirtualClientApiClient(IPAddress ipAddress, int port)
        {
            ipAddress.ThrowIfNull(nameof(ipAddress));

            string address = ipAddress.ToString();
            if (ipAddress == IPAddress.Loopback)
            {
                address = "localhost";
            }

            return CreateVirtualClientApiClient(new Uri($"http://{address}:{port}"));
        }

        /// <summary>
        /// Creates an <see cref="VirtualClientProxyApiClient"/> that can be used to communicate with the Virtual Client proxy API
        /// service.
        /// </summary>
        /// <param name="proxyApiUri">The URI for the proxy API/service including its port (e.g. http://any.uri:5000).</param>
        /// <param name="timeout">A timeout to use for the underlying HTTP client.</param>
        public static VirtualClientProxyApiClient CreateVirtualClientProxyApiClient(Uri proxyApiUri, TimeSpan? timeout = null)
        {
            proxyApiUri.ThrowIfNull(nameof(proxyApiUri));

            IRestClient restClient = new RestClientBuilder(timeout)
                .AlwaysTrustServerCertificate()
                .AddAcceptedMediaType(MediaType.Json)
                .Build();

            return new VirtualClientProxyApiClient(restClient, proxyApiUri);
        }

        /// <summary>
        /// Flushes buffered telemetry from all channels.
        /// </summary>
        /// <param name="timeout">The absolute timeout to flush the telemetry from each individual channel.</param>
        /// <returns>
        /// </returns>
        public static void FlushTelemetry(TimeSpan? timeout = null)
        {
            Parallel.ForEach(DependencyFactory.telemetryChannels, channel => channel.Flush(timeout));
         }

        /// <summary>
        /// Applies a filter to the logger generated by the provider that will handle the logging
        /// of test metrics/results events.
        /// </summary>
        internal static ILoggerProvider HandleMetricsEvents(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id == LogType.Metrics);
        }

        /// <summary>
        /// Applies a filter to the logger generated by the provider that will handle the logging
        /// of test metrics/results events.
        /// </summary>
        internal static ILoggerProvider HandlePerformanceCounterEvents(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id == LogType.Metrics && eventId.Name == "PerformanceCounter");
        }

        /// <summary>
        /// Applies a filter to the logger generated by the provider that will handle the logging
        /// of system event events.
        /// </summary>
        internal static ILoggerProvider HandleSystemEvents(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id == LogType.SystemEvent);
        }

        /// <summary>
        /// Applies a filter to the logger generated by the provider that will handle the logging
        /// of trace events.
        /// </summary>
        internal static ILoggerProvider HandleTraceEvents(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id <= LogType.Error);
        }
    }
}