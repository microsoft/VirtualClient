// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes a command on the system with the working directory set to a 
    /// package installed.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ExecuteCommandMonitor : VirtualClientIntervalBasedMonitor
    {
        private const int MaxOutputLength = 125000;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteCommandMonitor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public ExecuteCommandMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                this.MaxRetries,
                (retries) => TimeSpan.FromSeconds(retries + 1));
        }

        /// <summary>
        /// Parameter defines the command(s) to execute. Multiple commands should be delimited using
        /// the '&amp;&amp;' characters which works on both Windows and Unix systems (e.g. ./configure&amp;&amp;make).
        /// </summary>
        public string Command
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Command));
            }
        }

        /// <summary>
        /// Parameter defines the maximum number of retries on failures of the
        /// command execution. Default = 0;
        /// </summary>
        public int MaxRetries
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MaxRetries), 0);
            }
        }

        /// <summary>
        /// Parameter defines the event ID (e.g. eventlog.journalctl)
        /// </summary>
        public string MonitorEventId
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.MonitorEventId), out IConvertible eventId);
                return eventId?.ToString();
            }
        }

        /// <summary>
        /// Parameter defines the event source (e.g. ipconfig). 
        /// </summary>
        public string MonitorEventSource
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.MonitorEventSource));
            }
        }

        /// <summary>
        /// Parameter defines the event type (e.g. system_info).
        /// </summary>
        public string MonitorEventType
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.MonitorEventType));
            }
        }

        /// <summary>
        /// Parameter defines the working directory from which the command should be executed. When the
        /// 'PackageName' parameter is defined, this parameter will take precedence. Otherwise, the directory
        /// where the package is installed for the 'PackageName' parameter will be used as the working directory.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.WorkingDirectory), out IConvertible workingDir);
                return workingDir?.ToString();
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Execute the monitor to run the command on intervals.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            return Task.Run(async () =>
            {
                try
                {
                    telemetryContext.AddContext("command", SensitiveData.ObscureSecrets(this.Command));
                    telemetryContext.AddContext("workingDirectory", this.WorkingDirectory);
                    telemetryContext.AddContext("platforms", string.Join(VirtualClientComponent.CommonDelimiters.First(), this.SupportedPlatforms));

                    await this.WaitAsync(this.MonitorWarmupPeriod, cancellationToken);

                    int iterations = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (this.MonitorStrategy != null)
                            {
                                switch (this.MonitorStrategy)
                                {
                                    case VirtualClient.MonitorStrategy.Once:
                                    case VirtualClient.MonitorStrategy.OnceAtBeginAndEnd:
                                        await this.ExecuteCommandAsync(telemetryContext, cancellationToken);
                                        break;
                                }
                            }
                            else
                            {
                                iterations++;
                                if (this.IsIterationComplete(iterations))
                                {
                                    break;
                                }

                                await this.ExecuteCommandAsync(telemetryContext, cancellationToken);
                            }
                        }
                        catch (Exception exc)
                        {
                            // Do not let the monitor operations crash the application.
                            this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                        }
                        finally
                        {
                            await this.WaitAsync(this.MonitorFrequency, cancellationToken);
                        }
                    }

                    if (this.MonitorStrategy == VirtualClient.MonitorStrategy.OnceAtBeginAndEnd)
                    {
                        await this.ExecuteCommandAsync(telemetryContext, CancellationToken.None);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected on ctrl-c or a cancellation is requested.
                }
                catch (Exception exc)
                {
                    // Do not let the monitor operations crash the application.
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            });
        }

        /// <summary>
        /// Execute the command(s) logic on the system.
        /// </summary>
        protected async Task ExecuteCommandAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<string> commandsToExecute = this.GetCommandsToExecute();

            if (commandsToExecute?.Any() == true)
            {
                foreach (string originalCommand in commandsToExecute)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        string command = originalCommand;
                        bool commandHasRelativePaths = PlatformSpecifics.RelativePathExpression.IsMatch(command);
                        string effectiveWorkingDirectory = this.WorkingDirectory;

                        if (!string.IsNullOrWhiteSpace(effectiveWorkingDirectory))
                        {
                            effectiveWorkingDirectory = PlatformSpecifics.ResolveRelativePaths(effectiveWorkingDirectory)
                                ?.TrimEnd(new char[] { '\\', '/' });
                        }

                        if (commandHasRelativePaths)
                        {
                            command = PlatformSpecifics.ResolveRelativePaths(command);
                        }

                        if (PlatformSpecifics.TryGetCommandParts(command, out string effectiveCommand, out string effectiveCommandArguments))
                        {
                            if (!string.IsNullOrWhiteSpace(effectiveWorkingDirectory))
                            {
                                // There appears to be some kind of bug or unfortunate implementation choice
                                // in .NET causing a Win32Exception like the following despite a correct command
                                // name and working directory being defined for the process object. This is a workaround
                                // to the issue.
                                //
                                // System.ComponentModel.Win32Exception:
                                // 'An error occurred trying to start process 'execute_ipconfig.cmd'
                                // with working directory 'S:\microsoft\virtualclient\out\bin\Debug\AnyCPU'.
                                // The system cannot find the file specified.
                                Match commandMatch = Regex.Match(effectiveCommand, @"\x22*([\x20\x21\x23-\x7E]+)\x22*", RegexOptions.IgnoreCase);
                                if (commandMatch.Success && !string.Equals(commandMatch.Value, "sudo", StringComparison.OrdinalIgnoreCase))
                                {
                                    string commandText = commandMatch.Groups[1].Value;
                                    if (!Path.IsPathRooted(commandText))
                                    {
                                        effectiveCommand = command.Replace(commandText, this.Combine(effectiveWorkingDirectory, commandText));
                                    }
                                }
                            }

                            await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                            {
                                using (IProcessProxy process = await this.ExecuteCommandAsync(effectiveCommand, effectiveCommandArguments, effectiveWorkingDirectory, telemetryContext, cancellationToken))
                                {
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        await this.LogProcessDetailsAsync(process, telemetryContext, this.LogFolderName);
                                        process.ThrowIfMonitorFailed();
                                        this.CaptureEventInformation(process, telemetryContext);
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the component for execution.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.EvaluateParametersAsync(cancellationToken);
        }

        /// <summary>
        /// Validates the parameters supplied to the monitor.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (this.MonitorStrategy != null)
            {
                switch (this.MonitorStrategy)
                {
                    case VirtualClient.MonitorStrategy.Once:
                    case VirtualClient.MonitorStrategy.OnceAtBeginAndEnd:
                        break;

                    default:
                        throw new NotSupportedException($"The monitoring strategy '{this.MonitorStrategy}' is not supported.");
                }
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the system.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = false;
            if (base.IsSupported())
            {
                // We execute only if the current platform/architecture matches those
                // defined in the parameters.
                if (!this.SupportedPlatforms.Any() || this.SupportedPlatforms.Contains(this.PlatformArchitectureName))
                {
                    isSupported = true;
                }
            }

            return isSupported;
        }

        private void CaptureEventInformation(IProcessProxy process, EventContext telemetryContext)
        {
            string standardOutput = process.StandardOutput?.ToString();
            if (!string.IsNullOrWhiteSpace(standardOutput) && standardOutput.Length > MaxOutputLength)
            {
                standardOutput = $"{standardOutput.Substring(0, MaxOutputLength)}...";
            }

            string standardError = process.StandardError?.ToString();
            if (!string.IsNullOrWhiteSpace(standardError) && standardError.Length > MaxOutputLength)
            {
                standardError = $"{standardError.Substring(0, MaxOutputLength)}...";
            }

            string eventType = !string.IsNullOrWhiteSpace(this.MonitorEventType) 
                ? this.MonitorEventType
                : "system.monitor";

            string eventSource = !string.IsNullOrWhiteSpace(this.MonitorEventSource)
                ? this.MonitorEventSource
                : "system";

            string eventId = !string.IsNullOrWhiteSpace(this.MonitorEventId)
                ? this.MonitorEventId

                // Give the same command, the hashcode will be the same every time and is sufficient
                // to use for an identifier (e.g. system.monitor.173564897).
                : $"{eventType}.{this.Command.ToLowerInvariant().Replace("-", string.Empty).RemoveWhitespace().GetHashCode()}";

            this.Logger.LogSystemEvent(
                eventType,
                eventSource,
                eventId,
                LogLevel.Information,
                telemetryContext,
                eventCode: process.ExitCode,
                eventInfo: new Dictionary<string, object>
                {
                    { "command", SensitiveData.ObscureSecrets(process.FullCommand()) },
                    { "exitCode", process.ExitCode },
                    { "workingDirectory", process.StartInfo?.WorkingDirectory },
                    { "standardOutput", standardOutput },
                    { "standardError", standardError }
                });
        }

        private IEnumerable<string> GetCommandsToExecute()
        {
            List<string> commandsToExecute = new List<string>();
            bool sudo = this.Command.StartsWith("sudo", StringComparison.OrdinalIgnoreCase);

            IEnumerable<string> commands = this.Command.Split("&&", StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries);

            foreach (string fullCommand in commands)
            {
                if (sudo && !fullCommand.Contains("sudo", StringComparison.OrdinalIgnoreCase))
                {
                    commandsToExecute.Add($"sudo {fullCommand}");
                }
                else
                {
                    commandsToExecute.Add(fullCommand);
                }
            }

            return commandsToExecute;
        }
    }
}
