// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
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

        // e.g.
        // .\relative\path
        // ..\..\relative\path
        // ./relative/path
        // ../../relative/path
        private static readonly Regex RelativePathExpression = new Regex(@"\.{1,}[\\\/]{1,2}", RegexOptions.Compiled);

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
        /// Returns true if the command parts can be determined and outputs the parts.
        /// </summary>
        /// <param name="fullCommand">The full comamnd and arguments (e.g. sudo lshw -c disk).</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="commandArguments">The arguments to pass to the command.</param>
        protected static bool TryGetCommandParts(string fullCommand, out string command, out string commandArguments)
        {
            fullCommand.ThrowIfNullOrWhiteSpace(nameof(fullCommand));

            command = null;
            commandArguments = null;

            string[] commandParts = fullCommand.Split(' ');
            command = commandParts[0].Trim();

            if (commandParts.Length > 1)
            {
                commandArguments = string.Join(' ', commandParts.Skip(1)).Trim();
            }

            return command != null;
        }

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
                            iterations++;
                            if (this.IsIterationComplete(iterations))
                            {
                                break;
                            }

                            await this.ExecuteCommandAsync(telemetryContext, cancellationToken);
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
            if (!cancellationToken.IsCancellationRequested)
            {
                if (ExecuteCommandMonitor.TryGetCommandParts(this.Command, out string command, out string commandArguments))
                {
                    await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                    {
                        string effectiveWorkingDirectory = this.WorkingDirectory;
                        if (string.IsNullOrWhiteSpace(this.WorkingDirectory) && ExecuteCommandMonitor.RelativePathExpression.IsMatch(this.Command))
                        {
                            // If relative path references are used in the command, set the working directory
                            // to the current application/.exe base directory.
                            effectiveWorkingDirectory = this.PlatformSpecifics.CurrentDirectory;
                        }

                        if (!string.IsNullOrWhiteSpace(effectiveWorkingDirectory))
                        {
                            effectiveWorkingDirectory = effectiveWorkingDirectory.TrimEnd(new char[] { '\\', '/' });
                            if (ExecuteCommandMonitor.RelativePathExpression.IsMatch(effectiveWorkingDirectory))
                            {
                                // Ensure that relative working directory paths are fully expanded.
                                effectiveWorkingDirectory = Path.GetFullPath(effectiveWorkingDirectory);
                            }

                            // There appears to be some kind of bug or unfortunate implementation choice
                            // in .NET causing a Win32Exception like the following despite a correct command
                            // name and working directory being defined for the process object. This is a workaround
                            // to the issue.
                            //
                            // System.ComponentModel.Win32Exception:
                            // 'An error occurred trying to start process 'execute_ipconfig.cmd'
                            // with working directory 'S:\microsoft\virtualclient\out\bin\Debug\AnyCPU'.
                            // The system cannot find the file specified.
                            if (string.Equals(command, "sudo", StringComparison.OrdinalIgnoreCase) && ExecuteCommandMonitor.RelativePathExpression.IsMatch(commandArguments))
                            {
                                commandArguments = Path.Combine(effectiveWorkingDirectory, commandArguments);
                            }
                            else if (!Path.IsPathRooted(command))
                            {
                                command = Path.Combine(effectiveWorkingDirectory, command);
                            }
                        }

                        using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, effectiveWorkingDirectory, telemetryContext, cancellationToken))
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

        /// <summary>
        /// Initializes the component for execution.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.EvaluateParametersAsync(cancellationToken);
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

            this.Logger.LogSystemEvent(
                this.MonitorEventType,
                this.MonitorEventSource,
                $"{this.MonitorEventType}_{this.MonitorEventSource}".ToLowerInvariant(),
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
    }
}
