// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
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
    public class ExecuteCommand : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteCommand"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public ExecuteCommand(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
        /// Provides a set of environment variables to add to the monitor process executions.
        /// </summary>
        public IDictionary<string, IConvertible> EnvironmentVariables
        {
            get
            {
                IDictionary<string, IConvertible> environmentVariables = null;
                this.Parameters.TryGetValue(nameof(this.EnvironmentVariables), out IConvertible variables);

                if (variables != null)
                {
                    environmentVariables = TextParsingExtensions.ParseDelimitedValues(variables?.ToString());
                }

                return environmentVariables;
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
        /// A policy that defines how the component will retry when it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

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
        /// Execute the command(s) logic on the system.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("command", SensitiveData.ObscureSecrets(this.Command));
            telemetryContext.AddContext("workingDirectory", SensitiveData.ObscureSecrets(this.WorkingDirectory));
            telemetryContext.AddContext("platforms", string.Join(VirtualClientComponent.CommonDelimiters.First(), this.SupportedPlatforms));

            if (!cancellationToken.IsCancellationRequested)
            {
                IEnumerable<string> commandsToExecute = this.GetCommandsToExecute();

                if (commandsToExecute?.Any() == true)
                {
                    IDictionary<string, IConvertible> environmentVariables = this.EnvironmentVariables;
                    foreach (string originalCommand in commandsToExecute)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (PlatformSpecifics.TryGetCommandParts(originalCommand, out string effectiveCommand, out string effectiveCommandArguments))
                            {
                                await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                                {
                                    // There appears to be an unfortunate implementation choice in .NET causing a Win32Exception similar to the following when
                                    // referencing a binary and setting the working directory.
                                    //
                                    // System.ComponentModel.Win32Exception:
                                    // 'An error occurred trying to start process 'Coreinfo64.exe' with working directory 'S:\microsoft\virtualclient\out\bin\Debug\AnyCPU\VirtualClient.Main\net9.0\packages\system_tools\win-x64'.
                                    // The system cannot find the file specified.
                                    //
                                    // The .NET Process class does not reference the 'WorkingDirectory' when looking for the 'FileName' when UseShellExecute = false. The workaround
                                    // for this is to add the working directory to the PATH environment variable.
                                    string effectiveWorkingDirectory = this.WorkingDirectory;
                                    if (!string.IsNullOrWhiteSpace(effectiveWorkingDirectory))
                                    {
                                        this.PlatformSpecifics.SetEnvironmentVariable(
                                            EnvironmentVariable.PATH, 
                                            effectiveWorkingDirectory, 
                                            EnvironmentVariableTarget.Process, 
                                            append: true);
                                    }

                                    using (IProcessProxy process = await this.ExecuteCommandAsync(effectiveCommand, effectiveCommandArguments, effectiveWorkingDirectory, telemetryContext, cancellationToken))
                                    {
                                        this.AddEnvironmentVariables(process, environmentVariables);

                                        if (!cancellationToken.IsCancellationRequested)
                                        {
                                            await this.LogProcessDetailsAsync(process, telemetryContext, toolName: this.LogFolderName, logFileName: this.LogFileName);
                                            process.ThrowIfComponentOperationFailed(this.ComponentType);
                                        }
                                    }
                                });
                            }
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

        private void AddEnvironmentVariables(IProcessProxy process, IDictionary<string, IConvertible> environmentVariables)
        {
            if (environmentVariables?.Any() == true)
            {
                foreach (var entry in environmentVariables)
                {
                    string variableName = entry.Key.Trim();
                    string variableValue = entry.Value?.ToString().Trim();

                    if (string.IsNullOrWhiteSpace(variableValue) && process.EnvironmentVariables.ContainsKey(variableName))
                    {
                        process.EnvironmentVariables.Remove(variableName);
                    }
                    else if (variableValue.StartsWith("+"))
                    {
                        this.PlatformSpecifics.SetEnvironmentVariable(process, variableName, variableValue.Substring(1).Trim(), true);
                    }
                    else
                    {
                        this.PlatformSpecifics.SetEnvironmentVariable(process, variableName, variableValue);
                    }
                }
            }
        }

        private IEnumerable<string> GetCommandsToExecute()
        {
            List<string> commandsToExecute = new List<string>();
            bool sudo = this.Command.StartsWith("sudo", StringComparison.OrdinalIgnoreCase);

            IEnumerable<string> commands = this.Command.Split("&&", StringSplitOptions.RemoveEmptyEntries)?.Select(cmd => cmd.Trim());

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
