// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Net.Http;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extensions to help with telemetry requirements.
    /// </summary>
    public static class WorkloadTelemetryExtensions
    {
        /// <summary>
        /// Adds the details of the process including standard output, standard error
        /// and exit code to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="name">The property name to use for the process telemetry.</param>
        public static EventContext AddProcessContext(this EventContext telemetryContext, IProcessProxy process, string name = null)
        {
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                int? finalExitCode = null;
                string standardOutput = null;
                string standardError = null;

                try
                {
                    finalExitCode = process.ExitCode;
                }
                catch
                {
                }

                try
                {
                    standardOutput = process.StandardOutput?.ToString();
                }
                catch
                {
                }

                try
                {
                    standardError = process.StandardError?.ToString();
                }
                catch
                {
                }

                string fullCommand = $"{process.StartInfo?.FileName} {process.StartInfo?.Arguments}";
                if (!string.IsNullOrWhiteSpace(fullCommand))
                {
                    fullCommand = SensitiveData.ObscureSecrets(fullCommand);
                }

                telemetryContext.AddContext(name ?? "process", new
                {
                    id = process.Id,
                    command = fullCommand,
                    workingDir = process.StartInfo?.WorkingDirectory,
                    exitCode = finalExitCode,
                    standardOutput = standardOutput,
                    standardError = standardError
                });
            }
            catch
            {
                // Best effort.
            }

            return telemetryContext;
        }

        /// <summary>
        /// Extension adds HTTP action response information to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context object.</param>
        /// <param name="response">The HTTP action response.</param>
        /// <param name="propertyName">Optional property allows the caller to define the name of the telemetry context property.</param>
        public static void AddResponseContext(this EventContext telemetryContext, HttpResponseMessage response, string propertyName = "response")
        {
            response.ThrowIfNull(nameof(response));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            string responseContent = null;
            if (response.Content != null)
            {
                try
                {
                    responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // Best effort only.
                }
            }

            telemetryContext.AddContext(propertyName, new
            {
                statusCode = (int)response.StatusCode,
                method = $"{response?.RequestMessage?.Method}",
                requestUri = $"{response?.RequestMessage?.RequestUri}",
                content = responseContent
            });
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error
        /// and exit code.
        /// </summary>
        /// <param name="logger">The telemetry logger.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        public static void LogProcessDetails<T>(this ILogger logger, IProcessProxy process, EventContext telemetryContext)
            where T : class
        {
            logger.ThrowIfNull(nameof(logger));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                logger.LogMessage(
                    $"{typeof(T).Name}.ProcessDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddProcessContext(process));
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
        /// <param name="toolset">The name of the command/toolset that produced the results. The suffix 'ProcessDetails' will be appended.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        public static void LogProcessDetails(this ILogger logger, IProcessProxy process, string toolset, EventContext telemetryContext)
        {
            logger.ThrowIfNull(nameof(logger));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                logger.LogMessage(
                    $"{toolset}.ProcessDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddProcessContext(process));
            }
            catch
            {
                // Best effort.
            }
        }
    }
}
