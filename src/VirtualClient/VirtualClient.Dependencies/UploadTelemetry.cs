// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Extensibility;
    using VirtualClient.Logging;
    using MetadataContract = VirtualClient.Contracts.Metadata.MetadataContract;

    /// <summary>
    /// Reads metrics values from a set of files and uploads the metrics to
    /// a target telemetry store/endpoint.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class UploadTelemetry : VirtualClientComponent
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadTelemetry"/> class.
        /// </summary>
        public UploadTelemetry(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();

            if (string.IsNullOrWhiteSpace(this.MatchExpression))
            {
                this.SetDefaultMatchExpression();
            }
        }

        /// <summary>
        /// The type of telemetry data points to process. Supported values = Events, Metrics.
        /// </summary>
        public DataFormat Format
        {
            get
            {
                return this.Parameters.GetEnumValue<DataFormat>(nameof(this.Format));
            }
        }

        /// <summary>
        /// True to indicate that the information parsed is "intrinsic" to the system on which we are running (e.g. host metadata).
        /// This instructs VC to include the "intrinsic" information in the metrics data points. Default = false (no assumptions made).
        /// </summary>
        public bool Intrinsic
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.Intrinsic), false);
            }
        }

        /// <summary>
        /// A regular expression to apply for matching files in the target
        /// directory.  Note that this instruction does not apply when targeting 
        /// specific files.
        /// </summary>
        public string MatchExpression
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.MatchExpression), out IConvertible matchExpression);
                return matchExpression?.ToString().Trim();
            }

            private set
            {
                this.Parameters[nameof(this.MatchExpression)] = value;
            }
        }

        /// <summary>
        /// True to apply a recursive search to the target directory. Note that
        /// this instruction does not apply when targeting specific files.
        /// </summary>
        public bool Recursive
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.Recursive), false);
            }
        }

        /// <summary>
        /// The type of telemetry data points to process. Supported values = Events, Metrics.
        /// </summary>
        public DataSchema Schema
        {
            get
            {
                return this.Parameters.GetEnumValue<DataSchema>(nameof(this.Schema));
            }
        }

        /// <summary>
        /// The directory to search for data point files.
        /// </summary>
        public string TargetDirectory
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.TargetDirectory), out IConvertible directory);
                return directory?.ToString().Trim();
            }
        }

        /// <summary>
        /// A set of data point files to parse.
        /// </summary>
        public IEnumerable<string> TargetFiles
        {
            get
            {
                this.Parameters.TryGetCollection<string>(nameof(this.TargetFiles), out IEnumerable<string> files);
                return files?.Select(f => f.Trim());
            }
        }

        /// <summary>
        /// Creates a telemetry context object for the upload that includes certain information from the
        /// parent context as well as any intrinsic information when requested.
        /// </summary>
        /// <param name="dataPoint">The data point being uploaded.</param>
        /// <param name="ingestionTimestamp">A current timestamp defining the actual time the data is ingested (vs. when the data was captured).</param>
        /// <remarks>
        /// The timestamp for the data points represents exactly when that particular information was captured and
        /// will almost always be some point in the past. This is the timestamp that should be reflected in the data stores. 
        /// However, it is also useful to know when the data was ingested (emitted into the data store).
        /// </remarks>
        protected EventContext CreateContext(TelemetryDataPoint dataPoint, DateTime ingestionTimestamp)
        {
            // Notes:
            // When publishing telemetry from a file base, we cannot assume the system on which the files
            // exist are the system from which they were originally captured. The contents of the data points
            // within the file will be the source of truth. The following describes the aspects under consideration:
            //
            // Metadata passed
            EventContext baseContext = new EventContext(dataPoint.OperationId, dataPoint.OperationParentId);

            // DO NOT apply persistent telemetry properties. These apply to the context of
            // the Virtual Client application but MAY NOT apply to the context of the application
            // that produced the data points.
            baseContext.Properties.Clear();

            // These properties MUST match with those set in the VC application startup
            // (e.g. VirtualClient.Main -> CommandBase.cs, ExecuteProfileCommand.cs -> SetGlobalTelemetryProperties).
            string platformArchitecture = dataPoint.PlatformArchitecture;
            string profile = dataPoint.ProfileName.Trim();
            string profileName = null;

            Match profileNameMatch = Regex.Match(profile, $@"([^\(\)]+)\s*(\({platformArchitecture}\))$");
            if (profileNameMatch.Success)
            {
                // e.g. METIS-CPU-PERFORMANCE (linux-x64)
                profileName = profileNameMatch.Groups[1].Value;
            }
            else
            {
                profileName = profile;
                profile = $"{profile} ({platformArchitecture})";
            }

            baseContext.Properties[MetadataContract.AppHost] = dataPoint.AppHost;
            baseContext.Properties[MetadataContract.AppName] = dataPoint.AppName;
            baseContext.Properties[MetadataContract.AppVersion] = dataPoint.AppVersion;
            baseContext.Properties[MetadataContract.AppPlatformVersion] = dataPoint.AppVersion;
            baseContext.Properties[MetadataContract.ClientId] = dataPoint.ClientId;
            baseContext.Properties[MetadataContract.ExperimentId] = dataPoint.ExperimentId;
            baseContext.Properties[MetadataContract.ExecutionProfile] = profile;
            baseContext.Properties[MetadataContract.ExecutionProfileName] = profileName;
            baseContext.Properties[MetadataContract.ExecutionSystem] = dataPoint.ExecutionSystem;
            baseContext.Properties[MetadataContract.OperatingSystemPlatform] = dataPoint.OperatingSystemPlatform.ToString();
            baseContext.Properties[MetadataContract.PlatformArchitecture] = dataPoint.PlatformArchitecture;
            baseContext.Properties[MetadataContract.Timestamp] = dataPoint.Timestamp;
            baseContext.Properties[MetadataContract.IngestionTimestamp] = ingestionTimestamp.ToString("o");

            MetadataContract dataContract = new MetadataContract();

            // e.g.
            // metadata_host key
            IDictionary<string, object> metadata = this.MetadataContract.Get(MetadataContract.DefaultCategory);
            if (metadata?.Any() == true)
            {
                dataContract.Add(metadata, MetadataContract.DefaultCategory);
            }

            if (this.Intrinsic)
            {
                // e.g.
                // metadata_host key
                IDictionary<string, object> persistedHostMetadata = MetadataContract.GetPersisted(MetadataContract.HostCategory);
                IDictionary<string, object> hostMetadata = this.MetadataContract.Get(MetadataContract.HostCategory);

                if (persistedHostMetadata?.Any() == true)
                {
                    dataContract.Add(persistedHostMetadata, MetadataContract.HostCategory);
                }

                if (hostMetadata?.Any() == true)
                {
                    dataContract.Add(hostMetadata, MetadataContract.HostCategory);
                }
            }

            // Data point-specific metadata information. Overrides intrinsic metadata.
            // e.g.
            // metadata key
            if (dataPoint.Metadata?.Any() == true)
            {
                dataContract.Add(dataPoint.Metadata, MetadataContract.DefaultCategory, replace: true);
            }

            // Data point-specific host metadata information. Overrides intrinsic metadata
            // e.g.
            // metadata_host key
            if (dataPoint.HostMetadata?.Any() == true)
            {
                dataContract.Add(dataPoint.HostMetadata, MetadataContract.HostCategory, replace: true);
            }

            dataContract.Apply(baseContext, persisted: false);

            return baseContext;
        }

        /// <summary>
        /// Executes the logic to process the files in the target directory.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "Better readability in this case")]
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<string> targetFiles = this.GetTargetFiles();
            telemetryContext.AddContext("targetFiles", targetFiles);

            if (targetFiles?.Any() == true)
            {
                DateTime ingestionTimestamp = DateTime.UtcNow;
                foreach (string file in targetFiles)
                {
                    EventContext relatedContext = telemetryContext.Clone();
                    relatedContext.AddContext("file", file);

                    try
                    {
                        this.Logger.LogMessage($"{this.TypeName}.ProcessFile", LogLevel.Information, relatedContext);

                        switch (this.Schema)
                        {
                            case DataSchema.Events:
                                await this.ProcessEventDataAsync(file, ingestionTimestamp, relatedContext);
                                break;

                            case DataSchema.Metrics:
                                await this.ProcessMetricsDataAsync(file, ingestionTimestamp, relatedContext);
                                break;
                        }
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogMessage($"{this.TypeName}.ProcessFileError", LogLevel.Error, relatedContext.AddError(exc));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the set of target files to process.
        /// </summary>
        protected IEnumerable<string> GetTargetFiles()
        {
            // Note:
            // Hashsets help prevent against duplicate entries. This ensures we do not
            // upload duplicate telemetry during the operations.
            HashSet<string> targetFiles = new HashSet<string>();

            if (this.TargetFiles?.Any() == true)
            {
                targetFiles.AddRange(this.TargetFiles.Select(file => file.Trim()));
            }
            else
            {
                SearchOption searchOption = this.Recursive == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                IEnumerable<string> allFiles = this.fileSystem.Directory.EnumerateFiles(this.TargetDirectory, "*.*", searchOption);

                if (allFiles?.Any() == true)
                {
                    Regex fileMatch = new Regex(this.MatchExpression, RegexOptions.IgnoreCase);

                    foreach (string file in allFiles)
                    {
                        if (fileMatch.IsMatch(file))
                        {
                            targetFiles.Add(file);
                        }
                    }
                }
                else
                {
                    ConsoleLogger.Default.LogMessage(
                        $"No telemetry data point files exist in the target directory '{this.TargetDirectory}' matching the expression '{this.MatchExpression}'.",
                        LogLevel.Warning,
                        EventContext.None);
                }
            }

            return targetFiles;
        }

        /// <summary>
        /// Processes the delimited events from the file at the path provided.
        /// </summary>
        /// <param name="filePath">The path to the file containing the delimited events telemetry.</param>
        /// <param name="ingestionTimestamp">A current timestamp defining the actual time the data is ingested (vs. when the data was captured).</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry information.</param>
        protected async Task ProcessEventDataAsync(string filePath, DateTime ingestionTimestamp, EventContext telemetryContext)
        {
            string content = await this.fileSystem.File.ReadAllTextAsync(filePath);
            if (!string.IsNullOrWhiteSpace(content))
            {
                using (var enumerator = new DelimitedEventDataEnumerator(content, this.Format))
                {
                    enumerator.ParsingError += (sender, args) =>
                    {
                        EventContext errorContext = telemetryContext.Clone();
                        errorContext.AddContext("itemIndex", args.ItemIndex);
                        errorContext.AddContext("itemSchema", "Events");
                        errorContext.AddContext("item", args.Item);
                        errorContext.AddError(args.Error);

                        this.Logger.LogMessage($"{this.TypeName}.EventParsingError", LogLevel.Error, errorContext);
                    };

                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != null)
                        {
                            EventDataPoint dataPoint = enumerator.Current;
                            dataPoint.Validate();

                            EventContext eventContext = this.CreateContext(dataPoint, ingestionTimestamp);
                            Enum.TryParse<LogLevel>(dataPoint.SeverityLevel.ToString(), out LogLevel eventSeverity);

                            this.Logger.LogSystemEvent(
                                dataPoint.EventType,
                                dataPoint.EventSource,
                                dataPoint.EventId,
                                eventSeverity,
                                eventContext,
                                dataPoint.EventCode,
                                dataPoint.EventDescription,
                                dataPoint.EventInfo,
                                dataPoint.Tags?.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes the delimited metrics from the file at the path provided.
        /// </summary>
        /// <param name="filePath">The path to the file containing the delimited metrics telemetry.</param>
        /// <param name="ingestionTimestamp">A current timestamp defining the actual time the data is ingested (vs. when the data was captured).</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry information.</param>
        protected async Task ProcessMetricsDataAsync(string filePath, DateTime ingestionTimestamp, EventContext telemetryContext)
        {
            string content = await this.fileSystem.File.ReadAllTextAsync(filePath);
            if (!string.IsNullOrWhiteSpace(content))
            {
                using (var enumerator = new DelimitedMetricDataEnumerator(content, this.Format))
                {
                    enumerator.ParsingError += (sender, args) =>
                    {
                        EventContext errorContext = telemetryContext.Clone();
                        errorContext.AddContext("itemIndex", args.ItemIndex);
                        errorContext.AddContext("itemSchema", "Metrics");
                        errorContext.AddContext("item", args.Item);
                        errorContext.AddError(args.Error);

                        this.Logger.LogMessage($"{this.TypeName}.MetricParsingError", LogLevel.Error, errorContext);
                    };

                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != null)
                        {
                            MetricDataPoint dataPoint = enumerator.Current;
                            dataPoint.Validate();

                            EventContext eventContext = this.CreateContext(dataPoint, ingestionTimestamp);
                            Enum.TryParse<LogLevel>(dataPoint.SeverityLevel.ToString(), out LogLevel metricSeverity);

                            this.Logger.LogMetric(
                                dataPoint.ToolName,
                                dataPoint.ScenarioName,
                                dataPoint.ScenarioStartTime.Value,
                                dataPoint.ScenarioEndTime.Value,
                                dataPoint.MetricName,
                                dataPoint.MetricValue.Value,
                                dataPoint.MetricUnit,
                                dataPoint.MetricCategorization,
                                null,
                                dataPoint.Tags?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                                eventContext,
                                dataPoint.MetricRelativity.Value,
                                dataPoint.MetricVerbosity,
                                dataPoint.MetricDescription,
                                dataPoint.ToolResults,
                                dataPoint.ToolVersion,
                                null,
                                metricSeverity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates the component parameters.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (this.TargetFiles?.Any() != true && string.IsNullOrWhiteSpace(this.TargetDirectory))
            {
                throw new DependencyException(
                    $"Invalid usage. Either a set of target files or a target directory must be supplied on the command line.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private void SetDefaultMatchExpression()
        {
            switch (this.Schema)
            {
                case DataSchema.Events:
                    this.MatchExpression = @"\.events\.csv$";
                    break;

                case DataSchema.Metrics:
                    this.MatchExpression = @"\.metrics\.csv$";
                    break;

                default:
                    throw new NotSupportedException($"Data schema '{this.Schema}' is not supported.");
            }
        }
    }
}
