// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for logging facilities in the Virtual Client.
    /// </summary>
    public static class VirtualClientLoggingExtensions
    {
        internal static readonly Regex PathReservedCharacterExpression = new Regex(@"[""<>:|?*\\/]+", RegexOptions.Compiled);

        private static readonly IAsyncPolicy FileSystemAccessRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries));

        private static readonly Semaphore FileAccessLock = new Semaphore(1, 1);

        /// <summary>
        /// Captures the details of the process including standard output, standard error and exit codes to 
        /// telemetry and log files on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="toolName">The name of the toolset running in the process.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="results">Results from the process execution (i.e. outside of standard output).</param>
        /// <param name="logToTelemetry">True to log the results to telemetry. Default = true.</param>
        /// <param name="logToFile">True to log the results to a log file on the file system. Default = true.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        /// <param name="logFileName">The name to use for the log file when writing to the file system. Default = component 'Scenario' parameter value.</param>
        /// <param name="timestamped">True if any log files generated should be prefixed with timestamps. Default = true.</param>
        /// <param name="upload">True to request the file be uploaded when a content store is defined.</param>
        /// <param name="enableOutputSplit">
        /// When true, splits the standard output and error into multiple telemetry events if they exceed maxChars.
        /// When false, truncates the standard output/error at maxChars (existing behavior).
        /// </param>
        public static Task LogProcessDetailsAsync(
            this VirtualClientComponent component, IProcessProxy process, EventContext telemetryContext, string toolName = null, IEnumerable<string> results = null, bool logToTelemetry = true, bool logToFile = true, int logToTelemetryMaxChars = 125000, string logFileName = null, bool timestamped = true, bool upload = true, bool enableOutputSplit = false)
        {
            component.ThrowIfNull(nameof(component));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            return LogProcessDetailsAsync(component, process.ToProcessDetails(toolName, results), telemetryContext, logToTelemetry, logToFile, logToTelemetryMaxChars, logFileName, timestamped, upload, enableOutputSplit);
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error and exit codes to 
        /// telemetry and log files on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="processDetails">The process details that will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetry">True to log the results to telemetry. Default = true.</param>
        /// <param name="logToFile">
        /// True to log the results to a log file on the file system. This enables an override at a component-level to the user's request to log results 
        /// to the file system for components whose output is not sufficient or useful as a log file. Logging to file is enabled by default when the user
        /// has requested logging to file on the file system (e.g. --log-to-file). Default = true.
        /// </param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        /// <param name="logFileName">The name to use for the log file when writing to the file system. Default = component 'Scenario' parameter value.</param>
        /// <param name="timestamped">True if any log files generated should be prefixed with timestamps. Default = true.</param>
        /// <param name="upload">True to request the file be uploaded when a content store is defined.</param>
        /// <param name="enableOutputSplit">
        /// When true, splits the standard output and error into multiple telemetry events if they exceed maxChars.
        /// When false, truncates the standard output/error at maxChars (existing behavior).
        /// </param>
        public static async Task LogProcessDetailsAsync(this VirtualClientComponent component, ProcessDetails processDetails, EventContext telemetryContext, bool logToTelemetry = true, bool logToFile = true, int logToTelemetryMaxChars = 125000, string logFileName = null, bool timestamped = true, bool upload = true, bool enableOutputSplit = false)
        {
            component.ThrowIfNull(nameof(component));
            processDetails.ThrowIfNull(nameof(processDetails));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            if (VirtualClientLoggingExtensions.FileAccessLock.WaitOne())
            {
                try
                {
                    if (logToTelemetry)
                    {
                        try
                        {
                            component.Logger?.LogProcessDetails(processDetails, component.TypeName, telemetryContext, logToTelemetryMaxChars, enableOutputSplit: enableOutputSplit);
                        }
                        catch (Exception exc)
                        {
                            // Best effort but we should never crash VC if the logging fails. Metric capture
                            // is more important to the operations of VC. We do want to log the failure.
                            component.Logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                        }
                    }

                    // The VirtualClientComponent itself has a global setting (defined on the command line)
                    // for logging to file. The secondary extension method level boolean parameter here enables
                    // individual usages of this method to override if needed at the use case level.
                    // 
                    // e.g.
                    // The user may request logging to file on the command line. However, a specific component
                    // implementation may want to avoid logging its contents to file because it is not useful information etc...
                    if (component.LogToFile && logToFile)
                    {
                        try
                        {
                            await component.LogProcessDetailsToFileAsync(processDetails, telemetryContext, logFileName, timestamped, upload);
                        }
                        catch (Exception exc)
                        {
                            // Best effort but we should never crash VC if the logging fails. Metric capture
                            // is more important to the operations of VC. We do want to log the failure.
                            component.Logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                        }
                    }
                }
                finally
                {
                    VirtualClientLoggingExtensions.FileAccessLock.Release();
                }
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error
        /// and exit code.
        /// </summary>
        /// <param name="logger">The telemetry logger.</param>
        /// <param name="componentType">The type of component (e.g. GeekbenchExecutor).</param>
        /// <param name="processDetails">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        /// <param name="enableOutputSplit">
        /// When true, splits the standard output and error into multiple telemetry events if they exceed maxChars.
        /// When false, truncates the standard output/error at maxChars (existing behavior).
        /// </param>
        internal static void LogProcessDetails(this ILogger logger, ProcessDetails processDetails, string componentType, EventContext telemetryContext, int logToTelemetryMaxChars = 125000, bool enableOutputSplit = false)
        {
            logger.ThrowIfNull(nameof(logger));
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            processDetails.ThrowIfNull(nameof(processDetails));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            telemetryContext.AddContext(nameof(enableOutputSplit), enableOutputSplit);

            try
            {
                // Obscure sensitive data in the command line
                processDetails.CommandLine = SensitiveData.ObscureSecrets(processDetails.CommandLine);
                processDetails.StandardOutput = SensitiveData.ObscureSecrets(processDetails.StandardOutput);
                processDetails.StandardError = SensitiveData.ObscureSecrets(processDetails.StandardError);

                // Examples:
                // --------------
                // GeekbenchExecutor.ProcessDetails
                // GeekbenchExecutor.Geekbench.ProcessDetails
                // GeekbenchExecutor.ProcessResults
                // GeekbenchExecutor.Geekbench.ProcessResults
                string eventNamePrefix = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                    !string.IsNullOrWhiteSpace(processDetails.ToolName) ? $"{componentType}.{processDetails.ToolName}" : componentType,
                    string.Empty);

                if (enableOutputSplit)
                {
                    // Handle splitting standard output and error if enabled and necessary
                    List<string> standardOutputChunks = VirtualClientLoggingExtensions.SplitString(processDetails.StandardOutput, logToTelemetryMaxChars);
                    List<string> standardErrorChunks = VirtualClientLoggingExtensions.SplitString(processDetails.StandardError, logToTelemetryMaxChars);

                    for (int i = 0; i < standardOutputChunks.Count; i++)
                    {
                        ProcessDetails chunkedProcess = processDetails.Clone();
                        chunkedProcess.StandardOutput = standardOutputChunks[i];
                        chunkedProcess.StandardError = null; // Only include standard error in one of the events (to avoid duplication).
                        EventContext context = telemetryContext.Clone()
                            .AddContext("standardOutputChunkPart", i + 1)
                            .AddProcessDetails(chunkedProcess, maxChars: logToTelemetryMaxChars);

                        logger.LogMessage($"{eventNamePrefix}.ProcessDetails", LogLevel.Information, context);

                    }

                    for (int j = 0; j < standardErrorChunks.Count; j++)
                    {
                        ProcessDetails chunkedProcess = processDetails.Clone();
                        chunkedProcess.StandardOutput = null; // Only include standard output in one of the events (to avoid duplication).
                        chunkedProcess.StandardError = standardErrorChunks[j];

                        EventContext context = telemetryContext.Clone()
                            .AddContext("standardErrorChunkPart", j + 1)
                            .AddProcessDetails(chunkedProcess, maxChars: logToTelemetryMaxChars);

                        logger.LogMessage($"{eventNamePrefix}.ProcessDetails", LogLevel.Information, context);
                    }
                }
                else
                {
                    logger.LogMessage(
                        $"{eventNamePrefix}.ProcessDetails",
                        LogLevel.Information,
                        telemetryContext.Clone().AddProcessDetails(processDetails, maxChars: logToTelemetryMaxChars));
                }

                if (processDetails.Results?.Any() == true)
                {
                    logger.LogMessage(
                        $"{eventNamePrefix}.ProcessResults",
                        LogLevel.Information,
                        telemetryContext.Clone().AddProcessResults(processDetails, maxChars: logToTelemetryMaxChars));
                }

            }
            catch
            {
                // Best effort.
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error, exit code and results in log files
        /// on the system.
        /// </summary>
        /// <param name="component">The component that ran the process.</param>
        /// <param name="processDetails">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logFileName">The name to use for the log file when writing to the file system. Default = component 'Scenario' parameter value.</param>
        /// <param name="timestamped">True if any log files generated should be prefixed with timestamps. Default = true.</param>
        /// <param name="upload">True to request the file be uploaded when a content store is defined.</param>
        internal static async Task LogProcessDetailsToFileAsync(this VirtualClientComponent component, ProcessDetails processDetails, EventContext telemetryContext, string logFileName = null, bool timestamped = true, bool upload = true)
        {
            component.ThrowIfNull(nameof(component));
            processDetails.ThrowIfNull(nameof(processDetails));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                if (component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem)
                    && component.Dependencies.TryGetService<PlatformSpecifics>(out PlatformSpecifics specifics))
                {
                    string[] possibleLogFolderNames = new string[]
                    {
                        component.LogFolderName,
                        processDetails.ToolName,
                        component.TypeName
                    };

                    string logFolderName = VirtualClientLoggingExtensions.GetSafeFileName(possibleLogFolderNames.First(name => !string.IsNullOrWhiteSpace(name)), false);

                    string[] possibleLogFileNames = new string[]
                    {
                        logFileName,
                        component.Scenario,
                        processDetails.ToolName,
                        component.TypeName
                    };

                    string logDirectory = specifics.GetLogsPath(logFolderName.ToLowerInvariant().RemoveWhitespace());
                    string standardizedLogFileName = VirtualClientLoggingExtensions.GetSafeFileName(possibleLogFileNames.First(name => !string.IsNullOrWhiteSpace(name)), timestamped);

                    if (string.IsNullOrWhiteSpace(Path.GetExtension(standardizedLogFileName)))
                    {
                        standardizedLogFileName = $"{standardizedLogFileName}.log";
                    }

                    string effectiveCommand = $"{SensitiveData.ObscureSecrets(processDetails?.CommandLine)}".Trim();

                    if (!fileSystem.Directory.Exists(logDirectory))
                    {
                        fileSystem.Directory.CreateDirectory(logDirectory).Create();
                    }

                    string logFilePath = specifics.Combine(logDirectory, standardizedLogFileName);

                    // Examples:
                    // --------------
                    // Command           : /home/user/nuget/virtualclient/packages/fio/linux-x64/fio --name=randwrite_4k --size=128G --numjobs=8 --rw=randwrite --bs=4k
                    // Working Directory : /home/user/nuget/virtualclient/packages/fio/linux-x64
                    // Exit Code         : 0
                    //
                    // ##StandardOutput##
                    // {
                    //    "fio version" : "fio-3.26-19-ge7e53",
                    //      "timestamp" : 1646873555,
                    //      "timestamp_ms" : 1646873555274,
                    //      "time" : "Thu Mar 10 00:52:35 2022",
                    //      "jobs" : [
                    //        {
                    //           ...
                    //        }
                    //      ]
                    // }
                    // 
                    // ##GeneratedResults##
                    // Any results from the output of the process

                    // We are using a list of Tuples here to ensure the items are written in the exact
                    // order they are defined. We have to support multiple .NET frameworks and this is a lowest
                    // common denominator choice.
                    IList<KeyValuePair<string, IConvertible>> metadata = new List<KeyValuePair<string, IConvertible>>
                    {
                        new KeyValuePair<string, IConvertible>("Command", effectiveCommand),
                        new KeyValuePair<string, IConvertible>("WorkingDirectory", processDetails?.WorkingDirectory),
                        new KeyValuePair<string, IConvertible>("ElapsedTime", processDetails?.ElapsedTime?.ToString()),
                        new KeyValuePair<string, IConvertible>("StartTime", processDetails?.StartTime?.ToString("yyyy-MM-ddThh:mm:ss.fffZ")),
                        new KeyValuePair<string, IConvertible>("ExitTime", processDetails?.ExitTime?.ToString("yyyy-MM-ddThh:mm:ss.fffZ")),
                        new KeyValuePair<string, IConvertible>("ExitCode", processDetails?.ExitCode),
                    };

                    IDictionary<string, IConvertible> additionalMetadata = new SortedDictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "ExperimentId", component.ExperimentId },
                        { "ClientId", component.AgentId },
                        { "ComponentType", component.ComponentType },
                        { "MachineName", Environment.MachineName },
                        { "PlatformArchitecture", component.PlatformSpecifics.PlatformArchitectureName },
                        { "OperatingSystemVersion", Environment.OSVersion.ToString() },
                        { "OperatingSystemDescription", RuntimeInformation.OSDescription },
                        { "Role", component.Roles?.FirstOrDefault() },
                        { "Scenario", component.Scenario },
                        { "TimeZone", TimeZoneInfo.Local.StandardName },
                        { "ToolName", processDetails.ToolName }
                    };

                    foreach (var entry in component.Metadata)
                    {
                        if (!additionalMetadata.ContainsKey(entry.Key))
                        {
                            additionalMetadata[entry.Key] = entry.Value;
                        }
                    }

                    int maxKeyLength1 = metadata.Max(item => item.Key.Length);
                    int maxKeyLength2 = additionalMetadata.Keys.Max(key => key.Length);
                    int keyColumnWidth = maxKeyLength1 >= maxKeyLength2 ? maxKeyLength1 : maxKeyLength2;

                    StringBuilder outputBuilder = new StringBuilder();
                    foreach (var entry in metadata)
                    {
                        outputBuilder.AppendLine(string.Format($"{{0,-{keyColumnWidth}}} : {{1}}", entry.Key, entry.Value));
                    }

                    foreach (var entry in additionalMetadata)
                    {
                        outputBuilder.AppendLine(string.Format($"{{0,-{keyColumnWidth}}} : {{1}}", entry.Key, entry.Value));
                    }

                    if (!string.IsNullOrWhiteSpace(processDetails.StandardOutput))
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##StandardOutput##"); // This is a simple delimiter that will NOT conflict with regular expressions possibly used in custom parsing.
                        outputBuilder.AppendLine(processDetails.StandardOutput);
                    }

                    if (!string.IsNullOrWhiteSpace(processDetails.StandardError))
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##StandardError##");
                        outputBuilder.AppendLine(processDetails.StandardError);
                    }

                    if (processDetails?.Results?.Any() == true)
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##GeneratedResults##");
                        outputBuilder.AppendLine(string.Join('\n', processDetails.Results));
                    }

                    await VirtualClientLoggingExtensions.FileSystemAccessRetryPolicy.ExecuteAsync(async () =>
                    {
                        await fileSystem.File.WriteAllTextAsync(logFilePath, outputBuilder.ToString());
                    });

                    if (upload && component.TryGetContentStoreManager(out IBlobManager blobManager))
                    {
                        string effectiveToolName = logFolderName;

                        FileContext fileContext = new FileContext(
                            fileSystem.FileInfo.New(logFilePath),
                            HttpContentType.PlainText,
                            Encoding.UTF8.WebName,
                            component.ExperimentId,
                            component.AgentId,
                            effectiveToolName,
                            component.Scenario,
                            effectiveCommand,
                            component.Roles?.Any() == true ? string.Join(',', component.Roles) : null);

                        // Upload the file as-is. If the file was timestamped, then it will be uploaded with that timestamp.
                        FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext, timestamped: false);

                        await component.RequestFileUploadAsync(descriptor);
                    }
                }
            }
            catch (Exception exc)
            {
                // Best effort but we should never crash VC if the logging fails. Metric capture
                // is more important to the operations of VC. We do want to log the failure.
                component.Logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
            }
        }

        /// <summary>
        /// Returns a file name that does not have any reserved characters for 
        /// operating system files and directories.
        /// </summary>
        /// <param name="fileName">The name of the file (e.g. fio.log).</param>
        /// <param name="timestamped">True if any log files generated should be prefixed with timestamps. Default = true.</param>
        private static string GetSafeFileName(string fileName, bool timestamped = true)
        {
            // Examples:
            // --------------
            // /logs/fio/2023-02-01-1223300-fio.log
            // /logs/fio/2023-02-01-1227450-fio.log
            //
            // /logs/fio/2023-02-01-1223300-randomwrite_4k_blocksize.log
            // /logs/fio/2023-02-01-1227450-randomwrite_8k_blocksize.log

            string effectiveLogFileName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(fileName, string.Empty)
                .RemoveWhitespace();

            if (timestamped)
            {
                // Note:
                // In order to best ensure we handle concurrent writes happening at near the same instant
                // in time, we include parts of the timestamp down to the millionths of a second.
                effectiveLogFileName = $"{DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmssffffff")}-{effectiveLogFileName}";
            }

            return effectiveLogFileName.ToLowerInvariant();
        }

        /// <summary>
        /// Splits a given string into a list of substrings, each with a maximum specified length.
        /// Useful for processing or transmitting large strings in manageable chunks.
        /// </summary>
        /// <param name="inputString">The original string to be split. If null, it will be treated as an empty string.</param>
        /// <param name="chunkSize">The maximum length of each chunk. Defaults to 125,000 characters.</param>
        /// <returns>A list of substrings, each with a length up to the specified chunk size.</returns>
        private static List<string> SplitString(string inputString, int chunkSize = 125000)
        {
            string finalString = inputString ?? string.Empty;

            var result = new List<string>();
            for (int i = 0; i < finalString.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, finalString.Length - i);
                result.Add(finalString.Substring(i, length));
            }

            return result;
        }
    }
}
