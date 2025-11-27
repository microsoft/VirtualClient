// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Extensibility;
    using VirtualClient.Logging;

    /// <summary>
    /// Command executes operations to upload metrics and events from files on the system
    /// to a telemetry endpoint.
    /// </summary>
    internal class ProcessTelemetryCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// The data point file format (e.g. Csv, Json, Yaml).
        /// </summary>
        public DataFormat DataFormat { get; set; }

        /// <summary>
        /// The data point file schema (e.g. Events, Metrics).
        /// </summary>
        public DataSchema DataSchema { get; set; }

        /// <summary>
        /// A regular expression to apply for matching files in the target
        /// directory.  Note that this instruction does not apply when targeting 
        /// specific files.
        /// </summary>
        public string MatchExpression { get; set; }

        /// <summary>
        /// True if the information in the data point files is intrinsic to
        /// the current system (i.e. captured from the system vs. a remote system).
        /// </summary>
        /// <remarks>
        /// When the data is intrinsic, system/host metadata will be automatically included
        /// with the data point information for upload.
        /// </remarks>
        public bool? Intrinsic { get; set; }

        /// <summary>
        /// True to apply a recursive search to the target directory. Note that
        /// this instruction does not apply when targeting specific files.
        /// </summary>
        public bool? Recursive { get; set; }

        /// <summary>
        /// The directory to search for data point files.
        /// </summary>
        public string TargetDirectory { get; set; }

        /// <summary>
        /// A set of exact data point files.
        /// </summary>
        public IEnumerable<string> TargetFiles { get; set; }

        /// <summary>
        /// Provides features for accessing the local file system.
        /// </summary>
        protected IFileSystem FileSystem { get; set; }

        /// <summary>
        /// Executes the telemetry upload operations.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="dependencies">Dependencies/services created for the application.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected override async Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            this.Validate();
            int exitCode = 0;
            
            if (this.FileSystem == null)
            {
                this.FileSystem = new FileSystem();
            }

            if (string.IsNullOrWhiteSpace(this.MatchExpression))
            {
                this.SetDefaultMatchExpression();
            }

            IEnumerable<string> targetFiles = this.GetTargetFiles(this.FileSystem);
            if (targetFiles?.Any() == true)
            {
                this.Timeout = ProfileTiming.OneIteration();
                this.Profiles = new List<DependencyProfileReference>
                {
                    new DependencyProfileReference("UPLOAD-TELEMETRY.json")
                };

                if (this.Parameters == null)
                {
                    this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
                }

                this.Parameters["Format"] = this.DataFormat;
                this.Parameters["Schema"] = this.DataSchema;
                this.Parameters["TargetFiles"] = string.Join(';', targetFiles);

                if (this.Intrinsic != null)
                {
                    this.Parameters["Intrinsic"] = this.Intrinsic;
                }

                exitCode = await base.ExecuteAsync(args, dependencies, cancellationTokenSource);
            }
            else
            {
                ConsoleLogger.Default.LogMessage(
                    $"No telemetry data point files found matching the expression '{this.MatchExpression}'.", 
                    LogLevel.Warning, 
                    EventContext.None);

                exitCode = 1;
            }

            return exitCode;
        }

        /// <inheritdoc/>
        protected override IEnumerable<string> GetLoggerDefinitions()
        {
            List<string> effectiveLoggerProviders = new List<string>();
            IEnumerable<string> loggerDefinitions = base.GetLoggerDefinitions();

            // To avoid file search conflicts, we remove out any of the default file loggers.
            foreach (string definition in loggerDefinitions)
            {
                if (!string.Equals("file", definition))
                {
                    effectiveLoggerProviders.Add(definition);
                }
            }

            return effectiveLoggerProviders;
        }

        private IEnumerable<string> GetTargetFiles(IFileSystem fileSystem)
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
                IEnumerable<string> allFiles = fileSystem.Directory.EnumerateFiles(this.TargetDirectory, "*.*", searchOption);

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

        private void SetDefaultMatchExpression()
        {
            switch (this.DataSchema)
            {
                case DataSchema.Events:
                    this.MatchExpression = @"\.events$";
                    break;

                case DataSchema.Metrics:
                    this.MatchExpression = @"\.metrics$";
                    break;

                default:
                    throw new NotSupportedException($"Data schema '{this.DataSchema}' is not supported.");
            }
        }

        private void Validate()
        {
            if (this.TargetFiles?.Any() != true && string.IsNullOrWhiteSpace(this.TargetDirectory))
            {
                throw new ArgumentException($"Invalid usage. Either a set of target files or a target directory must be supplied on the command line.");
            }
        }
    }
}
