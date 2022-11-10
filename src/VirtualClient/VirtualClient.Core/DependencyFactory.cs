// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
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
        public static EventHubTelemetryChannel CreateEventHubChannel(string eventHubConnectionString, string eventHubName)
        {
            var client = new EventHubProducerClient(eventHubConnectionString, eventHubName);
            EventHubTelemetryChannel channel = new EventHubTelemetryChannel(client, enableDiagnostics: true);

            DependencyFactory.telemetryChannels.Add(channel);
            SystemManagement.CleanupTasks.Add(() => channel.Dispose());
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
                EventHubTelemetryChannel tracesChannel = DependencyFactory.CreateEventHubChannel(
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
                EventHubTelemetryChannel metricsChannel = DependencyFactory.CreateEventHubChannel(
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
                EventHubTelemetryChannel systemEventsChannel = DependencyFactory.CreateEventHubChannel(
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
        /// <param name="logFileDirectory">The path to the directory where log files are written.</param>
        /// <param name="settings">Defines the settings for each log file that will be written.</param>
        public static IEnumerable<ILoggerProvider> CreateFileLoggerProviders(string logFileDirectory, FileLogSettings settings)
        {
            // 100MB
            const long maxFileSizeBytes = 100000000;

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
                LoggerConfiguration tracesLogConfiguration = new LoggerConfiguration().WriteTo.RollingFile(
                    new JsonTextFormatter(excludes),
                    Path.Combine(logFileDirectory, settings.TracesFileName),
                    fileSizeLimitBytes: maxFileSizeBytes,
                    retainedFileCountLimit: 10,
                    flushToDiskInterval: TimeSpan.FromSeconds(10));

                ILoggerProvider tracesLoggerProvider = new SerilogFileLoggerProvider(tracesLogConfiguration)
                    .HandleTraceEvents();

                SystemManagement.CleanupTasks.Add(() => tracesLoggerProvider.Dispose());
                loggerProviders.Add(tracesLoggerProvider);

                // Metrics/Results
                LoggerConfiguration metricsLogConfiguration = new LoggerConfiguration().WriteTo.RollingFile(
                    new JsonTextFormatter(metricsExcludes),
                    Path.Combine(logFileDirectory, settings.MetricsFileName),
                    fileSizeLimitBytes: maxFileSizeBytes,
                    retainedFileCountLimit: 10,
                    flushToDiskInterval: TimeSpan.FromSeconds(5));

                ILoggerProvider metricsLoggerProvider = new SerilogFileLoggerProvider(metricsLogConfiguration)
                    .HandleMetricsEvents();

                SystemManagement.CleanupTasks.Add(() => metricsLoggerProvider.Dispose());
                loggerProviders.Add(metricsLoggerProvider);

                // Performance Counters
                LoggerConfiguration countersLogConfiguration = new LoggerConfiguration().WriteTo.RollingFile(
                    new JsonTextFormatter(metricsExcludes),
                    Path.Combine(logFileDirectory, settings.CountersFileName),
                    fileSizeLimitBytes: maxFileSizeBytes,
                    retainedFileCountLimit: 10,
                    flushToDiskInterval: TimeSpan.FromSeconds(5));

                ILoggerProvider countersLoggerProvider = new SerilogFileLoggerProvider(countersLogConfiguration)
                    .HandlePerformanceCounterEvents();

                SystemManagement.CleanupTasks.Add(() => countersLoggerProvider.Dispose());
                loggerProviders.Add(countersLoggerProvider);

                // System Events
                LoggerConfiguration eventsLogConfiguration = new LoggerConfiguration().WriteTo.RollingFile(
                    new JsonTextFormatter(excludes),
                    Path.Combine(logFileDirectory, settings.EventsFileName),
                    fileSizeLimitBytes: maxFileSizeBytes,
                    retainedFileCountLimit: 10,
                    flushToDiskInterval: TimeSpan.FromSeconds(5));

                ILoggerProvider eventsLoggerProvider = new SerilogFileLoggerProvider(eventsLogConfiguration)
                    .HandleSystemEvents();

                SystemManagement.CleanupTasks.Add(() => eventsLoggerProvider.Dispose());
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
        public static IBlobManager CreateProxyBlobManager(DependencyProxyStore storeDescription)
        {
            storeDescription.ThrowIfNull(nameof(storeDescription));

            VirtualClientProxyApiClient proxyApiClient = DependencyFactory.CreateVirtualClientProxyApiClient(storeDescription.ProxyApiUri, TimeSpan.FromHours(6));
            return new ProxyBlobManager(storeDescription, proxyApiClient);
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