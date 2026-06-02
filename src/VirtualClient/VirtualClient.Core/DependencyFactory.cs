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
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Messaging.EventHubs.Producer;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using Serilog.Formatting;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Configuration;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;
    using VirtualClient.Identity;
    using VirtualClient.Logging;
    using VirtualClient.Proxy;

    /// <summary>
    /// Factory for creating workload runtime dependencies.
    /// </summary>
    public static partial class DependencyFactory
    {
        /// <summary>
        /// Creates an <see cref="IBlobManager"/> instance that can be used to download blobs/files from a store or
        /// upload blobs/files to a store.
        /// </summary>
        /// <param name="storeName">The name of the store (e.g. Packages, Content).</param>
        /// <param name="endpoint">The endpoint URL or connection string for the blob store.</param>
        /// <param name="certificateManager">The certificate manager used for authentication.</param>
        /// <param name="platformSpecifics">Provides platform-specific information.</param>
        /// <exception cref="DependencyException">Thrown when the provided information is not sufficient to create a blob manager.</exception>
        public static IBlobManager CreateBlobManager(string storeName, string endpoint, ICertificateManager certificateManager, PlatformSpecifics platformSpecifics)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));
            certificateManager.ThrowIfNull(nameof(certificateManager));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));

            IBlobManager blobManager = null;
            endpoint = ValidateAndFormatPackageUri(endpoint);
            string argumentValue = endpoint.Trim(new char[] { '\'', '"', ' ' });

            if (EndpointUtility.IsStorageAccountConnectionString(argumentValue))
            {
                // Storage account-level or container-level connection string
                // e.g.
                // DefaultEndpointsProtocol=https;AccountName=anystorage01;AccountKey=...;EndpointSuffix=core.windows.net
                string connectionString = argumentValue;
                blobManager = DependencyFactory.CreateBlobManager(storeName, connectionString);
            }
            else if (EndpointUtility.IsCustomConnectionString(argumentValue))
            {
                // e.g.
                // EndpointUrl=anystorage01.blob.core.windows.net;ClientId=307591a4-abb2...;TenantId=985bbc17...
                IDictionary<string, string> connectionProperties = TextParsingExtensions.ParseDelimitedValues(argumentValue)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                blobManager = DependencyFactory.CreateBlobManager(storeName, connectionProperties, certificateManager);
            }
            else if (Uri.TryCreate(argumentValue, UriKind.Absolute, out Uri endpointUri))
            {
                // e.g.
                // SAS URI
                // https://any.service.azure.com?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https
                // or
                // Custom URI
                // https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=1753429a8bc4f91d
                // or
                // Package URI
                // https://packages.virtualclient.microsoft.com

                blobManager = DependencyFactory.CreateBlobManager(storeName, endpointUri, certificateManager, platformSpecifics);
            }

            if (blobManager == null)
            {
                throw new SchemaException(
                    $"The value provided for the Storage Account endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI or proxy URI{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A URI to a CDN or web proxy for the storage account{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return blobManager;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to local CSV files.
        /// </summary>
        /// <param name="logFileDirectory">The path to the directory where log files are written.</param>
        public static IEnumerable<ILoggerProvider> CreateCsvFileLoggerProviders(string logFileDirectory)
        {
            logFileDirectory.ThrowIfNullOrWhiteSpace(nameof(logFileDirectory));

            // 20MB
            // General Sizing:
            // Around 34,400 metric records will fit inside of a single CSV file at 20MB.
            const long maxFileSizeBytes = 20000000;

            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();

            string metricsCsvFilePath = Path.Combine(logFileDirectory, "metrics.csv");
            ILoggerProvider metricsCsvProvider = new MetricsCsvFileLoggerProvider(metricsCsvFilePath, maxFileSizeBytes);
            loggerProviders.Add(metricsCsvProvider);

            return loggerProviders;
        }

        /// <summary>
        /// Creates a disk manager for the OS/system platform (e.g. Windows, Linux).
        /// </summary>
        /// <param name="platform">The OS/system platform.</param>
        public static DiskManager CreateDiskManager(PlatformID platform)
        {
            DiskManager manager = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    manager = new WindowsDiskManager(new WindowsProcessManager());
                    break;

                case PlatformID.Unix:
                    manager = new UnixDiskManager(new UnixProcessManager());
                    break;

                default:
                    throw new NotSupportedException($"Disk management features are not yet supported for '{platform}' platforms.");
            }

            return manager;
        }

        /// <summary>
        /// Creates a telemetry channel for writing messages to an Event Hub target.
        /// </summary>
        /// <param name="endpoint">Describes the Event Hub namespace endpoint connection string or URI.</param>
        /// <param name="eventHubName">Defines the name of the Event Hub targeted.</param>
        /// <param name="certificateManager">The certificate manager used for authentication.</param>
        /// <param name="platformSpecifics">Provides platform-specific information.</param>
        public static EventHubTelemetryChannel CreateEventHubTelemetryChannel(string endpoint, string eventHubName, ICertificateManager certificateManager, PlatformSpecifics platformSpecifics)
        {
            EventHubTelemetryChannel telemetryChannel = null;
            EventHubProducerClientOptions clientOptions = null;

            string argumentValue = endpoint.Trim(new char[] { '\'', '\"', ' ' });
            string proxyUri = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_EVENT_HUB_PROXY);

            if (!string.IsNullOrWhiteSpace(proxyUri))
            {
                // Supports the use of a Proxy for routing Event Hub traffic. This allows clients to use secure endpoints inside of their network 
                // to route traffic to the Event Hub namespace (vs. a direct connection).
                // (e.g. http://proxy-dmz.contoso.com:135).
                string proxyUsername = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_EVENT_HUB_PROXY_USERNAME);
                string proxyPassword = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_EVENT_HUB_PROXY_PASSWORD);

                clientOptions = new EventHubProducerClientOptions();
                clientOptions.ConnectionOptions.TransportType = Azure.Messaging.EventHubs.EventHubsTransportType.AmqpWebSockets;
                clientOptions.ConnectionOptions.Proxy = new System.Net.WebProxy(proxyUri, true);

                if (!string.IsNullOrWhiteSpace(proxyUsername) && !string.IsNullOrWhiteSpace(proxyPassword))
                {
                    clientOptions.ConnectionOptions.Proxy.Credentials = new NetworkCredential(proxyUsername, proxyPassword);
                }
            }

            if (EndpointUtility.IsCustomConnectionString(argumentValue))
            {
                // e.g.
                // Endpoint=sb://any.servicebus.windows.net;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4
                // EventHubNamespace=any.servicebus.windows.net;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4
                IDictionary<string, string> connectionParameters = TextParsingExtensions.ParseDelimitedValues(argumentValue)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                // We support an 'EventHubNamespace' property in custom connection strings. To ensure consistency downstream,
                // we define the endpoint to be a proper Event Hub namespace URI.
                if (connectionParameters.TryGetValue(ConnectionParameter.EventHubNamespace, out string eventHubNamespace))
                {
                    connectionParameters[ConnectionParameter.EndpointUrl] = eventHubNamespace;
                    if (!eventHubNamespace.Trim().StartsWith("sb://"))
                    {
                        connectionParameters[ConnectionParameter.EndpointUrl] = $"sb://{eventHubNamespace}";
                    }
                }

                telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(eventHubName, connectionParameters, certificateManager, clientOptions);
            }
            else if (EndpointUtility.IsEventHubConnectionString(argumentValue))
            {
                // e.g.
                // Endpoint=sb://xxx.servicebus.windows.net/;SharedAccessKeyName=xxx
                string connectionString = argumentValue;
                telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(eventHubName, connectionString, clientOptions);

            }
            else if (Uri.TryCreate(argumentValue, UriKind.Absolute, out Uri endpointUri))
            {
                // e.g.
                // sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789
                telemetryChannel = DependencyFactory.CreateEventHubTelemetryChannel(eventHubName, endpointUri, certificateManager, platformSpecifics, clientOptions);
            }

            if (telemetryChannel == null)
            {
                throw new SchemaException(
                    $"The value provided for the Event Hub endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Event Hub namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App identity information(e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A conection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"4) A URI to a web proxy or reverse gateway for the Event Hub{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0610-integration-event-hub/{Environment.NewLine}");
            }

            return telemetryChannel;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to Event Hub targets.
        /// </summary>
        /// <param name="endpoint">Describes the Event Hub namespace endpoint connection string or URI.</param>
        /// <param name="settings">Defines the settings for each individual Event Hub targeted.</param>
        /// <param name="level">The logging severity level.</param>
        /// <param name="certificateManager">The certificate manager used for authentication.</param>
        /// <param name="platformSpecifics">Provides platform-specific information.</param>
        /// <param name="flushTimeout">A timeout to apply to flush operations.</param>
        public static IEnumerable<ILoggerProvider> CreateEventHubLoggerProviders(string endpoint, EventHubLogSettings settings, LogLevel level, ICertificateManager certificateManager, PlatformSpecifics platformSpecifics, TimeSpan? flushTimeout = null)
        {
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));
            settings.ThrowIfNull(nameof(settings));
            certificateManager.ThrowIfNull(nameof(certificateManager));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));

            EventHubTelemetryChannel eventsChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                endpoint,
                settings.EventsHubName,
                certificateManager,
                platformSpecifics);

            EventHubTelemetryChannel metricsChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                endpoint,
                settings.MetricsHubName,
                certificateManager,
                platformSpecifics);

            EventHubTelemetryChannel tracesChannel = DependencyFactory.CreateEventHubTelemetryChannel(
                endpoint,
                settings.TracesHubName,
                certificateManager,
                platformSpecifics);

            eventsChannel.EventTransmissionError += (sender, args) =>
            {
                ConsoleLogger.Default.LogWarning($"Event Hub Transmission Error (events): {args.Error.Message}");
            };

            metricsChannel.EventTransmissionError += (sender, args) =>
            {
                ConsoleLogger.Default.LogWarning($"Event Hub Transmission Error (metrics): {args.Error.Message}");
            };

            tracesChannel.EventTransmissionError += (sender, args) =>
            {
                ConsoleLogger.Default.LogWarning($"Event Hub Transmission Error (traces): {args.Error.Message}");
            };

            // System Events are NOT affected by --log-level values defined on the command line. Events are
            // always written.
            ILoggerProvider eventsLoggerProvider = new EventHubTelemetryLoggerProvider(eventsChannel, LogLevel.Trace, flushTimeout)
                .HandleSystemEvents();

            // Metrics are NOT affected by --log-level values defined on the command line. Metrics are
            // always written.
            ILoggerProvider metricsLoggerProvider = new EventHubTelemetryLoggerProvider(metricsChannel, LogLevel.Trace, flushTimeout)
                .HandleMetrics();

            // Traces logging is affected by --log-level values defined on the command line.
            ILoggerProvider tracesLoggerProvider = new EventHubTelemetryLoggerProvider(tracesChannel, level, flushTimeout)
                .HandleTraces();

            return new List<ILoggerProvider>
            {
                eventsLoggerProvider,
                metricsLoggerProvider,
                tracesLoggerProvider
            };
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to local log files.
        /// </summary>
        /// <param name="logFilePath">The full path for the log file (e.g. C:\users\any\VirtualClient\logs\traces.log).</param>
        /// <param name="flushInterval">The interval at which the information should be flushed to disk.</param>
        /// <param name="level">The logging severity level.</param>
        /// <param name="formatter">Provides a formatter to use for structuring the output.</param>
        public static ILoggerProvider CreateFileLoggerProvider(string logFilePath, TimeSpan flushInterval, LogLevel level, ITextFormatter formatter = null)
        {
            logFilePath.ThrowIfNullOrWhiteSpace(nameof(logFilePath));

            // 20MB
            const long maxFileSizeBytes = 20000000;

            ILoggerProvider loggerProvider = null;

            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                LoggerConfiguration logConfiguration = null;
                if (formatter != null)
                {
                    logConfiguration = new LoggerConfiguration().WriteTo.File(
                        formatter,
                        logFilePath,
                        fileSizeLimitBytes: maxFileSizeBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 50,
                        flushToDiskInterval: flushInterval);
                }
                else
                {
                    logConfiguration = new LoggerConfiguration().WriteTo.File(
                        logFilePath,
                        fileSizeLimitBytes: maxFileSizeBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 50,
                        flushToDiskInterval: flushInterval);
                }

                loggerProvider = new SerilogFileLoggerProvider(logConfiguration, level);

                VirtualClientRuntime.CleanupTasks.Add(new Action_(() => loggerProvider.Dispose()));
            }

            return loggerProvider;
        }

        /// <summary>
        /// Creates logger providers for writing telemetry to local log files.
        /// </summary>
        /// <param name="logFileDirectory">The path to the directory where log files are written.</param>
        /// <param name="settings">Defines the settings for each log file that will be written.</param>
        /// <param name="level">The minimum logging severity level.</param>
        public static IEnumerable<ILoggerProvider> CreateFileLoggerProviders(string logFileDirectory, FileLogSettings settings, LogLevel level)
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();

            if (!string.IsNullOrWhiteSpace(logFileDirectory) && settings != null)
            {
                IEnumerable<string> propertiesToExcludeForMetrics = new List<string>
                {
                    "durationMs",
                    "message",
                    "profileFriendlyName"
                };

                IEnumerable<string> propertiesToExcludeForEvents = new List<string>
                {
                    "durationMs",
                    "message",
                    "profileFriendlyName"
                };

                // Logs/Traces
                ILoggerProvider tracesLoggerProvider = DependencyFactory.CreateFileLoggerProvider(
                    Path.Combine(logFileDirectory, settings.TracesFileName),
                    TimeSpan.FromSeconds(5),
                    level,
                    new SerilogJsonTextFormatter()).HandleTraces();

                loggerProviders.Add(tracesLoggerProvider);

                // Metrics/Results
                ILoggerProvider metricsLoggerProvider = DependencyFactory.CreateFileLoggerProvider(
                    Path.Combine(logFileDirectory, settings.MetricsFileName),
                    TimeSpan.FromSeconds(3),
                    LogLevel.Trace,
                    new SerilogJsonTextFormatter(propertiesToExcludeForMetrics)).HandleMetrics();

                loggerProviders.Add(metricsLoggerProvider);

                // System Events
                ILoggerProvider eventsLoggerProvider = DependencyFactory.CreateFileLoggerProvider(
                    Path.Combine(logFileDirectory, settings.EventsFileName),
                    TimeSpan.FromSeconds(5),
                    LogLevel.Trace,
                    new SerilogJsonTextFormatter(propertiesToExcludeForEvents)).HandleSystemEvents();

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
        /// Creates an <see cref="IKeyVaultManager"/> instance that can be used to access secrets and certificates from Key vault
        /// </summary>
        /// <param name="endpoint">A connection string or URI describing the target Key Vault endpoint and any identity/authentication information.</param>
        /// <param name="certificateManager"></param>
        public static IKeyVaultManager CreateKeyVaultManager(string endpoint, ICertificateManager certificateManager)
        {
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            IKeyVaultManager keyVaultManager = null;
            string argumentValue = endpoint.Trim(new char[] { '\'', '\"', ' ' });

            if (EndpointUtility.IsCustomConnectionString(argumentValue))
            {
                // e.g.
                // Endpoint=https://my-keyvault.vault.azure.net/;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4
                IDictionary<string, string> connectionParameters = TextParsingExtensions.ParseDelimitedValues(argumentValue)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                keyVaultManager = DependencyFactory.CreateKeyVaultManager(DependencyStore.KeyVault, connectionParameters, certificateManager);
            }
            else if (Uri.TryCreate(argumentValue, UriKind.Absolute, out Uri endpointUri))
            {
                // e.g.
                // https://my-keyvault.vault.azure.net
                // https://my-keyvault.vault.azure.net/?cid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&tid=307591a4-abb2-4559-af59-b47177d140cf&crtt=123456789
                keyVaultManager = DependencyFactory.CreateKeyVaultManager(DependencyStore.KeyVault, endpointUri, certificateManager);
            }

            if (keyVaultManager == null)
            {
                throw new SchemaException(
                    $"The value provided for the Key Vault endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Key Vault namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}");
            }

            return keyVaultManager;
        }

        /// <summary>
        /// Creates a <see cref="DependencyProfileReference"/> definition from the endpoint URI provided. 
        /// <list>
        /// <item>The following type of URIs are supported:</item>
        /// <list type="bullet">
        /// <item>Storage account or blob container SAS URI<br/>(e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&amp;ss=b&amp;srt=co&amp;sp=rtf&amp;se=2024-07-02T05:15:29Z&amp;st=2024-07-01T21:15:29Z&amp;spr=https).</item>
        /// <item>Microsoft Entra or Managed Identity referencing URI<br/>(e.g. https://any.service.azure.com/profiles/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&amp;tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&amp;crti=ABC&amp;crts=any.service.com).</item>
        /// </list>
        /// </list>
        /// </summary>
        /// <param name="endpoint">A connection string or URI describing the target profile storage endpoint and any identity/authentication information.</param>
        /// <param name="certificateManager">Provides features for reading certificates from the local system certificate stores.</param>
        public static DependencyProfileReference CreateProfileReference(string endpoint, ICertificateManager certificateManager)
        {
            DependencyProfileReference profile = null;

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri profileUri))
            {
                profile = DependencyFactory.CreateProfileReference(profileUri, certificateManager);
            }
            else if (EndpointUtility.IsCustomConnectionString(endpoint))
            {
                // e.g.
                // EndpointUrl=anystorage01.blob.core.windows.net/profile/ANY-PROFILE.json;ClientId=307591a4-abb2...;TenantId=985bbc17...
                IDictionary<string, string> connectionParameters = TextParsingExtensions.ParseDelimitedValues(endpoint)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                profile = DependencyFactory.CreateProfileReference(connectionParameters, certificateManager);
            }
            else
            {
                profile = new DependencyProfileReference(endpoint);
            }

            return profile;
        }

        /// <summary>
        /// Creates an <see cref="IBlobManager"/> that uploads or downloads files from a proxy API
        /// endpoint.
        /// </summary>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="proxyUri">The URI to the proxy API.</param>
        /// <param name="source">An explicit source to use for blob uploads/downloads through the proxy API.</param>
        /// <param name="logger">A logger to use for capturing information related to blob upload/download operations.</param>
        /// <param name="certificate">The certificate to authenticate to the proxy API</param>
        public static IBlobManager CreateProxyBlobManager(string storeName, Uri proxyUri, string source = null, Microsoft.Extensions.Logging.ILogger logger = null, X509Certificate2 certificate = null)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            proxyUri.ThrowIfNull(nameof(proxyUri));

            DependencyProxyStore proxyStore = new DependencyProxyStore(storeName, proxyUri);
            VirtualClientProxyApiClient proxyApiClient = DependencyFactory.CreateVirtualClientProxyApiClient(proxyUri, TimeSpan.FromHours(6), certificate);
            ProxyBlobManager blobManager = new ProxyBlobManager(proxyStore, proxyApiClient, source);

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

            return channel;
        }

        /// <summary>
        /// Creates a new <see cref="SystemManagement" /> instance.
        /// </summary>
        /// <param name="agentId">The ID of the agent as part of the larger experiment in operation.</param>
        /// <param name="experimentId">The ID of the larger experiment in operation.</param>
        /// <param name="platformSpecifics">Provides features for platform-specific operations (e.g. Windows, Unix).</param>
        /// <param name="executionSystem">The name of the execution system launching the application.</param>
        /// <param name="isolated">Instructs the factory to construct dependencies for cross-process/isolated runs.</param>
        public static ISystemManagement CreateSystemManager(string agentId, string experimentId, PlatformSpecifics platformSpecifics, string executionSystem = null, bool isolated = false)
        {
            agentId.ThrowIfNullOrWhiteSpace(nameof(agentId));
            experimentId.ThrowIfNullOrWhiteSpace(nameof(experimentId));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));

            PlatformID platform = platformSpecifics.Platform;
            ProcessManager processManager = ProcessManager.Create(platform);
            IDiskManager diskManager = DependencyFactory.CreateDiskManager(platform);
            IFileSystem fileSystem = new FileSystem();
            IFirewallManager firewallManager = DependencyFactory.CreateFirewallManager(platform, processManager);
            IPackageManager packageManager = new PackageManager(platformSpecifics, fileSystem);

            if (isolated)
            {
                packageManager = new IsolatedPackageManager(packageManager);
            }

            ISshClientFactory sshClientManager = new SshClientFactory();
            IStateManager stateManager = new StateManager(fileSystem, platformSpecifics);

            return new SystemManagement
            {
                AgentId = agentId,
                ExecutionSystem = executionSystem,
                ExperimentId = experimentId.ToLowerInvariant(),
                DiskManager = diskManager,
                FileSystem = fileSystem,
                FirewallManager = firewallManager,
                PackageManager = packageManager,
                PlatformSpecifics = platformSpecifics,
                ProcessManager = processManager,
                SshClientFactory = sshClientManager,
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
        /// <param name="certificate">The certificate to authenticate to the proxy API</param>
        public static VirtualClientProxyApiClient CreateVirtualClientProxyApiClient(Uri proxyApiUri, TimeSpan? timeout = null, X509Certificate2 certificate = null)
        {
            proxyApiUri.ThrowIfNull(nameof(proxyApiUri));

            if (!string.IsNullOrWhiteSpace(proxyApiUri.Query))
            {
                // e.g.
                // https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://any.service.azure.com/

                proxyApiUri = new Uri(proxyApiUri.OriginalString.Substring(0, proxyApiUri.OriginalString.IndexOf("?")));
            }

            IRestClientBuilder builder = new RestClientBuilder(timeout)
                .AlwaysTrustServerCertificate()
                .AddAcceptedMediaType(MediaType.Json);

            if (certificate != null)
            {
                builder.AddCertificate(certificate);
            }

            return new VirtualClientProxyApiClient(builder.Build(), proxyApiUri);
        }

        /// <summary>
        /// Applies a filter to the logger generated by the provider that will handle the logging
        /// of test metrics/results events.
        /// </summary>
        internal static ILoggerProvider HandleMetrics(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id == LogType.Metric);
        }

        /// <summary>
        /// Applies a filter to the logger generated by the provider that will handle the logging
        /// of test metrics/results events.
        /// </summary>
        internal static ILoggerProvider HandlePerformanceCounters(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id == LogType.Metric && eventId.Name == "PerformanceCounter");
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
        internal static ILoggerProvider HandleTraces(this ILoggerProvider loggerProvider)
        {
            return loggerProvider.WithFilter((eventId, logLevel, state) => (LogType)eventId.Id <= LogType.Error);
        }

        /// <summary>
        /// Returns the endpoint by verifying package uri checks.
        /// if the endpoint is a package uri without http or https protocols then append the protocol else return the endpoint value.
        /// </summary>
        /// <param name="endpoint">endpoint to verify and format</param>
        /// <returns></returns>
        internal static string ValidateAndFormatPackageUri(string endpoint)
        {
            string packageUri = new Uri(EndpointUtility.DefaultPackageStoreUri).Host;
            return packageUri == endpoint ? $"https://{endpoint}" : endpoint;
        }

        private static IBlobManager CreateBlobManager(string storeName, string connectionString)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            DependencyBlobStore store = new DependencyBlobStore(storeName, connectionString);
            return new BlobManager(store);
        }

        private static IBlobManager CreateBlobManager(string storeName, Uri endpointUri, ICertificateManager certificateManager, PlatformSpecifics platformSpecifics)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            IBlobManager blobManager = null;

            if (EndpointUtility.IsDefaultPackageStore(endpointUri))
            {
                if (string.Equals(DependencyStore.Content, storeName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"Invalid content store. The value provided for the content store cannot be the default package store '{EndpointUtility.DefaultPackageStoreUri}'. " +
                        $"This package store does not support content uploads.");
                }

                // The default packages store
                // e.g.
                // https://packages.virtualclient.microsoft.com
                var store = new DependencyBlobStore(storeName, endpointUri, DependencyStore.StoreTypeAzureCDN);
                blobManager = new BlobManager(store);
            }
            else if (EndpointUtility.IsCustomUri(endpointUri))
            {
                // 3) URI for Microsoft Entra or Managed Identity
                //    e.g. https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com)

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(endpointUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetUriManagedIdentityReference(queryParameters, out string managedIdentityId))
                {
                    credential = CredentialFactory.CreateManagedIdentityTokenCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetUriMicrosoftEntraReference(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateThumbprint))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                    }
                    else if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://any.service.azure.com/
                    Uri baseUri = new Uri(endpointUri.OriginalString.Substring(0, endpointUri.OriginalString.IndexOf("?")));
                    var store = new DependencyBlobStore(storeName, baseUri, credential);
                    blobManager = new BlobManager(store);
                }
            }
            else if (EndpointUtility.IsStorageAccountUri(endpointUri) || EndpointUtility.IsStorageAccountSasUri(endpointUri))
            {
                // Storage account or blob container anonymous URI or SAS URI.
                // https://any.blob.core.windows.net/
                // https://any.blob.core.windows.net/?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https
                var store = new DependencyBlobStore(storeName, endpointUri);
                blobManager = new BlobManager(store);
            }
            else
            {
                // Basic URI
                // e.g.
                // https://any.blob.core.windows.net
                //
                // API management
                // e.g.
                // https://any.azure-api.net
                var store = new DependencyBlobStore(storeName, endpointUri, DependencyStore.StoreTypeProxy);

                // Check for API management support.
                string subscriptionKey = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_APIM_SUBSCRIPTION_KEY);
                if (!string.IsNullOrWhiteSpace(subscriptionKey))
                {
                    HttpClient restClient = new HttpClient();
                    restClient.Timeout = Timeout.InfiniteTimeSpan;
                    restClient.DefaultRequestHeaders.Add(RequestHeader.OcpApimSubscriptionKey, subscriptionKey);
                    blobManager = new BlobManager(store, restClient);
                }
                else
                {
                    blobManager = new BlobManager(store);
                }
            }

            return blobManager;
        }

        private static IBlobManager CreateBlobManager(string storeName, IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            IBlobManager blobManager = null;
            if (EndpointUtility.TryGetConnectionStringEndpoint(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetConnectionStringManagedIdentityReference(connectionParameters, out string managedIdentityId))
                    {
                        credential = CredentialFactory.CreateManagedIdentityTokenCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateThumbprint))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                        }
                        else if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                        }
                    }

                    if (credential != null)
                    {
                        var store = new DependencyBlobStore(storeName, endpointUri, credential);
                        blobManager = new BlobManager(store);
                    }
                }
            }

            return blobManager;
        }

        private static EventHubTelemetryChannel CreateEventHubTelemetryChannel(string eventHubName, string eventHubConnectionString, EventHubProducerClientOptions clientOptions = null)
        {
            EventHubProducerClient client = new EventHubProducerClient(eventHubConnectionString, eventHubName, clientOptions);
            return new EventHubTelemetryChannel(client, enableDiagnostics: true);
        }

        private static EventHubTelemetryChannel CreateEventHubTelemetryChannel(string eventHubName, IDictionary<string, string> connectionParameters, ICertificateManager certificateManager, EventHubProducerClientOptions clientOptions = null)
        {
            eventHubName.ThrowIfNullOrWhiteSpace(nameof(eventHubName));
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            EventHubTelemetryChannel channel = null;

            if (EndpointUtility.TryGetConnectionStringEndpoint(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetConnectionStringManagedIdentityReference(connectionParameters, out string managedIdentityId))
                    {
                        credential = CredentialFactory.CreateManagedIdentityTokenCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateThumbprint))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                        }
                        else if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                        }
                    }

                    if (credential != null)
                    {
                        EventHubProducerClient client = new EventHubProducerClient(endpointUri.Host, eventHubName, credential, clientOptions);
                        channel = new EventHubTelemetryChannel(client);
                    }
                }
            }

            return channel;
        }

        private static EventHubTelemetryChannel CreateEventHubTelemetryChannel(string eventHubName, Uri endpointUri, ICertificateManager certificateManager, PlatformSpecifics platformSpecifics, EventHubProducerClientOptions clientOptions = null)
        {
            eventHubName.ThrowIfNullOrWhiteSpace(nameof(eventHubName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            EventHubTelemetryChannel channel = null;

            if (EndpointUtility.IsCustomUri(endpointUri))
            {
                // URI for Microsoft Entra or Managed Identity
                // e.g. sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com)

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(endpointUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetUriManagedIdentityReference(queryParameters, out string managedIdentityId))
                {
                    credential = CredentialFactory.CreateManagedIdentityTokenCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetUriMicrosoftEntraReference(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateThumbprint))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                    }
                    else if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // sb://any.servicebus.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> sb://any.servicebus.azure.com/

                    Uri baseUri = new Uri(endpointUri.OriginalString.Substring(0, endpointUri.OriginalString.IndexOf("?")));
                    EventHubProducerClient client = new EventHubProducerClient(baseUri.Host, eventHubName, credential, clientOptions);
                    channel = new EventHubTelemetryChannel(client, enableDiagnostics: true);
                }
            }
            else
            {
                // Check for API management targeting.
                string subscriptionKey = platformSpecifics.GetEnvironmentVariable(EnvironmentVariable.VC_APIM_SUBSCRIPTION_KEY);

                if (!string.IsNullOrWhiteSpace(subscriptionKey))
                {
                    // Supports the use of Azure API Management as a gateway for Event Hub traffic. This allows clients to use a secure API endpoint to route traffic to the
                    // Event Hub namespace (vs. a direct connection).
                    HttpClient restClient = new HttpClient();
                    Uri baseUri = new Uri(endpointUri, eventHubName);

                    if (!string.IsNullOrWhiteSpace(endpointUri.Query))
                    {
                        baseUri = new Uri($"{baseUri.AbsoluteUri}{endpointUri.Query}");
                    }

                    restClient.BaseAddress = baseUri;
                    restClient.Timeout = Timeout.InfiniteTimeSpan;
                    restClient.DefaultRequestHeaders.Add(RequestHeader.OcpApimSubscriptionKey, subscriptionKey);
                    channel = new EventHubTelemetryChannel(restClient, enableDiagnostics: true);
                }
            }

            return channel;
        }

        private static IKeyVaultManager CreateKeyVaultManager(string storeName, Uri endpointUri, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            IKeyVaultManager keyVaultManager = null;

            if (EndpointUtility.IsKeyVaultUri(endpointUri) && string.IsNullOrWhiteSpace(endpointUri.Query))
            {
                // Basic URI without any query parameters
                // (e.g. https://anyvault.vault.azure.net)
                var store = new DependencyKeyVaultStore(DependencyStore.KeyVault, endpointUri);
                keyVaultManager = new KeyVaultManager(store);
            }
            else if (EndpointUtility.IsKeyVaultUri(endpointUri) && EndpointUtility.IsCustomUri(endpointUri))
            {
                // URI for Microsoft Entra or Managed Identity
                // e.g. https://my-keyvault.vault.azure.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com)

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(endpointUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetUriManagedIdentityReference(queryParameters, out string managedIdentityId))
                {
                    credential = CredentialFactory.CreateManagedIdentityTokenCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetUriMicrosoftEntraReference(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateThumbprint))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                    }
                    else if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // https://my-keyvault.vault.azure.net/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://my-keyvault.vault.azure.net/
                    Uri baseUri = new Uri(endpointUri.OriginalString.Substring(0, endpointUri.OriginalString.IndexOf("?")));
                    var store = new DependencyKeyVaultStore(storeName, baseUri, credential);
                    keyVaultManager = new KeyVaultManager(store);
                }
            }

            return keyVaultManager;
        }

        private static IKeyVaultManager CreateKeyVaultManager(string storeName, IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            IKeyVaultManager keyVaultManager = null;

            if (EndpointUtility.TryGetConnectionStringEndpoint(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri) && EndpointUtility.IsKeyVaultUri(endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetConnectionStringManagedIdentityReference(connectionParameters, out string managedIdentityId))
                    {
                        credential = CredentialFactory.CreateManagedIdentityTokenCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateThumbprint))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                        }
                        else if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                        }
                    }

                    if (credential != null)
                    {
                        var store = new DependencyKeyVaultStore(storeName, endpointUri, credential);
                        keyVaultManager = new KeyVaultManager(store);
                    }
                }
            }

            return keyVaultManager;
        }

        private static DependencyProfileReference CreateProfileReference(Uri profileUri, ICertificateManager certificateManager)
        {
            profileUri.ThrowIfNull(nameof(profileUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyProfileReference reference = null;

            // Remote profile for download.
            string profileName = profileUri.Segments.Last();

            if (string.IsNullOrWhiteSpace(profileUri.Query))
            {
                // A URI to a profile that does not require any specific authentication (e.g. anonymous auth).
                // e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json
                //
                // or a SAS URI
                // e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https

                reference = new DependencyProfileReference(profileUri);
            }
            else if (EndpointUtility.IsStorageAccountSasUri(profileUri))
            {
                // or a SAS URI
                // e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https

                reference = new DependencyProfileReference(profileUri);
            }
            else if (EndpointUtility.IsCustomUri(profileUri))
            {
                // Custom URI
                // e.g. https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=1753429a8bc4f91d

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(profileUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetUriManagedIdentityReference(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetUriMicrosoftEntraReference(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateThumbprint))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                    }
                    else if (EndpointUtility.TryGetUriCertificateReference(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://any.service.azure.com/

                    Uri baseUri = new Uri(profileUri.OriginalString.Substring(0, profileUri.OriginalString.IndexOf("?")));
                    reference = new DependencyProfileReference(baseUri, credential);
                }
            }

            if (reference == null)
            {
                throw new SchemaException(
                    $"The value provided for the profile reference is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return reference;
        }

        private static DependencyProfileReference CreateProfileReference(IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyProfileReference profile = null;

            if (EndpointUtility.TryGetConnectionStringEndpoint(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetConnectionStringManagedIdentityReference(connectionParameters, out string managedIdentityId))
                    {
                        credential = new ManagedIdentityCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetConnectionStringMicrosoftEntraReference(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateThumbprint))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateThumbprint);
                        }
                        else if (EndpointUtility.TryGetConnectionStringCertificateReference(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = CredentialFactory.CreateCertificateTokenCredential(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject);
                        }
                    }

                    if (credential != null)
                    {
                        profile = new DependencyProfileReference(endpointUri, credential);
                    }
                }
            }

            if (profile == null)
            {
                throw new SchemaException(
                    $"The value provided for the profile reference is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) The name of an out-of-box profile {Environment.NewLine}" +
                    $"2) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"4) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"5) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return profile;
        }
    }
}