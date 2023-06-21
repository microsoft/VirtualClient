// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
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
        private static readonly IAsyncPolicy FileSystemAccessRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries));

        private static readonly Regex PathReservedCharacterExpression = new Regex(@"[""<>:|?*\\/]+", RegexOptions.Compiled);

        /// <summary>
        /// Captures the details of the process including standard output, standard error and exit codes to 
        /// telemetry and log files on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="toolName">The name of the toolset running in the process.</param>
        /// <param name="results">Results from the process execution (i.e. outside of standard output).</param>
        /// <param name="logToTelemetry">True to log the results to telemetry. Default = true.</param>
        /// <param name="logToFile">True to log the results to a log file on the file system. Default = false.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        public static async Task LogProcessDetailsAsync(
            this VirtualClientComponent component, IProcessProxy process, EventContext telemetryContext, string toolName = null, IEnumerable<string> results = null, bool logToTelemetry = true, bool logToFile = false, int logToTelemetryMaxChars = 125000)
        {
            component.ThrowIfNull(nameof(component));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            ILogger logger = null;

            if (logToTelemetry)
            {
                try
                {
                    if (component.Dependencies.TryGetService<ILogger>(out logger))
                    {
                        if (results?.Any() == true)
                        {
                            foreach (string result in results)
                            {
                                logger.LogProcessDetails(process, component.TypeName, telemetryContext, toolName, result, logToTelemetryMaxChars);
                            }
                        }
                        else
                        {
                            logger.LogProcessDetails(process, component.TypeName, telemetryContext, toolName, null, logToTelemetryMaxChars);
                        }
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }

            if (VirtualClientComponent.LogToFile && logToFile)
            {
                try
                {
                    if (results?.Any() == true)
                    {
                        foreach (string result in results)
                        {
                            await component.LogProcessDetailsToFileAsync(process, telemetryContext, toolName, result);
                        }
                    }
                    else
                    {
                        await component.LogProcessDetailsToFileAsync(process, telemetryContext, toolName, null);
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error
        /// and exit code.
        /// </summary>
        /// <param name="logger">The telemetry logger.</param>
        /// <param name="componentType">The type of component (e.g. GeekbenchExecutor).</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="toolName">The name of the toolset running in the process.</param>
        /// <param name="results">Results from the process execution (i.e. outside of standard output).</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        internal static void LogProcessDetails(this ILogger logger, IProcessProxy process, string componentType, EventContext telemetryContext, string toolName = null, string results = null, int logToTelemetryMaxChars = 125000)
        {
            logger.ThrowIfNull(nameof(logger));
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                // Examples:
                // --------------
                // GeekbenchExecutor.ProcessDetails
                // GeekbenchExecutor.Geekbench.ProcessDetails
                // GeekbenchExecutor.ProcessResults
                // GeekbenchExecutor.Geekbench.ProcessResults
                string eventNamePrefix = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                    !string.IsNullOrWhiteSpace(toolName) ? $"{componentType}.{toolName}" : componentType,
                    string.Empty);

                logger.LogMessage(
                    $"{eventNamePrefix}.ProcessDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddProcessContext(process, maxChars: logToTelemetryMaxChars));

                if (results != null)
                {
                    logger.LogMessage(
                        $"{eventNamePrefix}.ProcessResults",
                        LogLevel.Information,
                        telemetryContext.Clone().AddProcessContext(process, results: results, maxChars: logToTelemetryMaxChars));
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
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="toolName">The name of the toolset running in the process.</param>
        /// <param name="results">Results from the process execution (i.e. outside of standard output).</param>
        internal static async Task LogProcessDetailsToFileAsync(this VirtualClientComponent component, IProcessProxy process, EventContext telemetryContext, string toolName, string results)
        {
            component.ThrowIfNull(nameof(component));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                if (component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem)
                    && component.Dependencies.TryGetService<PlatformSpecifics>(out PlatformSpecifics specifics))
                {
                    string effectiveToolName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                        (!string.IsNullOrWhiteSpace(toolName) ? toolName : component.TypeName).ToLowerInvariant().RemoveWhitespace(),
                        string.Empty);

                    // Ensure that we obscure/remove any secrets (e.g. passwords) that may have been passed into
                    // the command on the command line as arguments.
                    string effectiveCommandArguments = process.StartInfo?.Arguments;
                    if (!string.IsNullOrWhiteSpace(effectiveCommandArguments))
                    {
                        effectiveCommandArguments = SensitiveData.ObscureSecrets(effectiveCommandArguments);
                    }

                    string effectiveCommand = $"{process.StartInfo?.FileName} {effectiveCommandArguments}".Trim();
                    string logPath = specifics.GetLogsPath(effectiveToolName.ToLowerInvariant().RemoveWhitespace());

                    if (!fileSystem.Directory.Exists(logPath))
                    {
                        fileSystem.Directory.CreateDirectory(logPath).Create();
                    }

                    // Examples:
                    // --------------
                    // /logs/fio/2023-02-01T122330Z-fio.log
                    // /logs/fio/2023-02-01T122745Z-fio.log
                    //
                    // /logs/fio/2023-02-01T122330Z-randomwrite_4k_blocksize.log
                    // /logs/fio/2023-02-01T122745Z-randomwrite_8k_blocksize.log
                    string effectiveLogFileName = FileUploadDescriptor.GetFileName(
                        $"{(!string.IsNullOrWhiteSpace(component.Scenario) ? component.Scenario : effectiveToolName)}.log",
                        DateTime.UtcNow);

                    string logFilePath = specifics.Combine(logPath, effectiveLogFileName.ToLowerInvariant());

                    // Examples:
                    // --------------
                    // Command           : /home/user/nuget/virtualclient/packages/fio/linux-x64/fio --name=randwrite_4k --size=128G --numjobs=8 --rw=randwrite --bs=4k
                    // Working Directory : /home/user/nuget/virtualclient/packages/fio/linux-x64
                    // Exit Code         : 0
                    //
                    // ##Output##
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
                    // ##Results##
                    // Any results from the output of the process

                    StringBuilder outputBuilder = new StringBuilder();
                    outputBuilder.AppendLine($"Command           : {effectiveCommand}");
                    outputBuilder.AppendLine($"Working Directory : {process.StartInfo?.WorkingDirectory}");
                    outputBuilder.AppendLine($"Exit Code         : {process.ExitCode}");
                    outputBuilder.AppendLine();
                    outputBuilder.AppendLine("##Output##"); // This is a simple delimiter that will NOT conflict with regular expressions possibly used in custom parsing.

                    if (process.StandardOutput?.Length > 0 == true)
                    {
                        outputBuilder.AppendLine(process.StandardOutput.ToString());
                    }

                    if (process.StandardError?.Length > 0 == true)
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine(process.StandardError.ToString());
                    }

                    if (results != null)
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##Results##");
                        outputBuilder.AppendLine(results);
                    }

                    await VirtualClientLoggingExtensions.FileSystemAccessRetryPolicy.ExecuteAsync(async () =>
                    {
                        await fileSystem.File.WriteAllTextAsync(logFilePath, outputBuilder.ToString());
                    });

                    if (component.TryGetContentStoreManager(out IBlobManager blobManager))
                    {
                        FileContext fileContext = new FileContext(
                            fileSystem.FileInfo.New(logFilePath),
                            HttpContentType.PlainText,
                            Encoding.UTF8.WebName,
                            component.ExperimentId,
                            component.AgentId,
                            effectiveToolName,
                            component.Scenario,
                            effectiveCommandArguments,
                            component.Roles?.Any() == true ? string.Join(',', component.Roles) : null);

                        FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext, component.Parameters, component.Metadata, true);

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
    }
}
