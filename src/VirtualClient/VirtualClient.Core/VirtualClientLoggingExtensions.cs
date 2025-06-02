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
        public static Task LogProcessDetailsAsync(
            this VirtualClientComponent component, IProcessProxy process, EventContext telemetryContext, string toolName = null, IEnumerable<string> results = null, bool logToTelemetry = true, bool logToFile = true, int logToTelemetryMaxChars = 125000)
        {
            component.ThrowIfNull(nameof(component));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            process.ProcessDetails.ToolName = toolName;
            process.ProcessDetails.GeneratedResults = results;

            return LogProcessDetailsAsync(component, process.ProcessDetails, telemetryContext, logToTelemetry, logToFile, logToTelemetryMaxChars);
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error and exit codes to 
        /// telemetry and log files on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="sshCommandProxy">The process whose details will be captured.</param>
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
        public static Task LogProcessDetailsAsync(
            this VirtualClientComponent component, ISshCommandProxy sshCommandProxy, EventContext telemetryContext, string toolName = null, List<string> results = null, bool logToTelemetry = true, bool logToFile = true, int logToTelemetryMaxChars = 125000)
        {
            component.ThrowIfNull(nameof(component));
            sshCommandProxy.ThrowIfNull(nameof(sshCommandProxy));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            sshCommandProxy.ProcessDetails.ToolName = toolName;
            sshCommandProxy.ProcessDetails.GeneratedResults = results;

            return LogProcessDetailsAsync(component, sshCommandProxy.ProcessDetails, telemetryContext, logToTelemetry, logToFile, logToTelemetryMaxChars);
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
        public static async Task LogProcessDetailsAsync(
            this VirtualClientComponent component, ProcessDetails processDetails, EventContext telemetryContext, bool logToTelemetry = true, bool logToFile = true, int logToTelemetryMaxChars = 125000)
        {
            component.ThrowIfNull(nameof(component));
            processDetails.ThrowIfNull(nameof(processDetails));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            ILogger logger = null;

            if (logToTelemetry)
            {
                try
                {
                    if (component.Dependencies.TryGetService<ILogger>(out logger))
                    {
                        logger.LogProcessDetails(processDetails, component.TypeName, telemetryContext, logToTelemetryMaxChars);
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
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
                    await component.LogProcessDetailsToFileAsync(processDetails, telemetryContext);
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
        /// <param name="processDetails">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        /// <param name="logToConsole">When set to true will output the process standard output and error to the VC console.</param>
        internal static void LogProcessDetails(this ILogger logger, ProcessDetails processDetails, string componentType, EventContext telemetryContext, int logToTelemetryMaxChars = 125000, bool logToConsole = false)
        {
            logger.ThrowIfNull(nameof(logger));
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            processDetails.ThrowIfNull(nameof(processDetails));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

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

                logger.LogMessage(
                    $"{eventNamePrefix}.ProcessDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddProcessContext(processDetails, maxChars: logToTelemetryMaxChars));

                if (processDetails.GeneratedResults != null && processDetails.GeneratedResults.Any())
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
                // Obscure sensitive data in the command line
                process.ProcessDetails.CommandLine = SensitiveData.ObscureSecrets(process.ProcessDetails.CommandLine);
                process.ProcessDetails.StandardOutput = SensitiveData.ObscureSecrets(process.ProcessDetails.StandardOutput);
                process.ProcessDetails.StandardError = SensitiveData.ObscureSecrets(process.ProcessDetails.StandardError);

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
        /// <param name="processDetails">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        internal static async Task LogProcessDetailsToFileAsync(this VirtualClientComponent component, ProcessDetails processDetails, EventContext telemetryContext)
        {
            component.ThrowIfNull(nameof(component));
            processDetails.ThrowIfNull(nameof(processDetails));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                if (component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem)
                    && component.Dependencies.TryGetService<PlatformSpecifics>(out PlatformSpecifics specifics))
                {
                    string effectiveToolName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                        (!string.IsNullOrWhiteSpace(processDetails.ToolName) ? processDetails.ToolName : component.TypeName).ToLowerInvariant().RemoveWhitespace(),
                        string.Empty);

                    string effectiveCommand = $"{SensitiveData.ObscureSecrets(processDetails?.CommandLine)}".Trim();
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
                    string effectiveLogFileName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                        $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssffffZ")}-{(!string.IsNullOrWhiteSpace(component.Scenario) ? component.Scenario : effectiveToolName)}.log",
                        string.Empty).ToLowerInvariant().RemoveWhitespace();

                    string logFilePath = specifics.Combine(logPath, effectiveLogFileName);

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

                    StringBuilder outputBuilder = new StringBuilder();
                    outputBuilder.AppendLine($"Command           : {effectiveCommand}");
                    outputBuilder.AppendLine($"Working Directory : {processDetails?.WorkingDirectory}");
                    outputBuilder.AppendLine($"Exit Code         : {processDetails?.ExitCode}");

                    if (!string.IsNullOrEmpty(processDetails.StandardOutput))
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##StandardOutput##"); // This is a simple delimiter that will NOT conflict with regular expressions possibly used in custom parsing.
                        outputBuilder.AppendLine(processDetails.StandardOutput);
                    }

                    if (!string.IsNullOrEmpty(processDetails.StandardError))
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##StandardError##");
                        outputBuilder.AppendLine(processDetails.StandardError);
                    }

                    if (processDetails != null && processDetails.GeneratedResults != null && processDetails.GeneratedResults.Any())
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##GeneratedResults##");
                        outputBuilder.AppendLine(string.Join('\n', processDetails.GeneratedResults));
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
                            effectiveCommand,
                            component.Roles?.Any() == true ? string.Join(',', component.Roles) : null);

                        // The file is already timestamped at this point, so there is no need to add any additional
                        // timestamping information.
                        FileUploadDescriptor descriptor = component.CreateFileUploadDescriptor(fileContext, component.Parameters, timestamped: false);

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
