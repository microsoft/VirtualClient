// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Component executes an SSH command on a target system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ExecuteSshCommand : VirtualClientComponent
    {
        private static readonly SemaphoreSlim StandardOutputLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteSshCommand"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        public ExecuteSshCommand(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
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
        /// Executes the command on the target systems through SSH connections.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.TryGetSshClients(out IEnumerable<ISshClientProxy> sshClients))
            {
                throw new WorkloadException(
                    "Target agents not defined. One or more target agent SSH connections must be defined on the command line.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            List<Task> agentExecutionTasks = new List<Task>();
            foreach (ISshClientProxy sshClient in sshClients)
            {
                agentExecutionTasks.Add(this.ExecuteAsync(sshClient, telemetryContext, cancellationToken));
            }

            return Task.WhenAll(agentExecutionTasks);
        }

        /// <summary>
        /// Executes the command on the target system through SSH connections.
        /// </summary>
        protected async Task ExecuteAsync(ISshClientProxy sshClient, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                EventContext relatedContext = telemetryContext.Clone();
                relatedContext.AddContext("target", sshClient.ToString());
                relatedContext.AddContext("command", SensitiveData.ObscureSecrets(this.Command));

                await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteOnTarget", LogLevel.Trace, relatedContext, async () =>
                {
                    await sshClient.ConnectAsync();

                    using (TextWriter standardOutput = new StringWriter())
                    {
                        using (TextWriter standardError = new StringWriter())
                        {
                            sshClient.StandardError = standardOutput;
                            sshClient.StandardOutput = standardError;
                            ProcessDetails result = null;

                            try
                            {
                                result = await sshClient.ExecuteCommandAsync(this.Command, cancellationToken);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await this.LogProcessDetailsAsync(result, relatedContext);
                                    this.ThrowIfCommandFailed(result);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected when a cancellation is requested.
                            }
                            finally
                            {
                                relatedContext.AddContext("standardOutput", standardOutput);
                                relatedContext.AddContext("standardError", standardError);
                                this.LogToStandardOutput(sshClient, result);
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Expected when a cancellation is requested.
            }
            finally
            {
                sshClient.Disconnect();
            }
        }

        /// <summary>
        /// Validates the parameters and initial state of the component.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (this.ComponentType == ComponentType.Monitor)
            {
                throw new NotSupportedException($"Invalid component usage. The '{nameof(ExecuteSshCommand)}' component cannot be used as a monitor.");
            }
        }

        private async Task LogToStandardOutput(ISshClientProxy sshClient, ProcessDetails results)
        {
            if (results != null)
            {
                await ExecuteSshCommand.StandardOutputLock.WaitAsync(CancellationToken.None);

                try
                {
                    Console.WriteLine();
                    Console.WriteLine($"{sshClient.ToString()}:");
                    Console.WriteLine($"---------------------------------------");
                    Console.WriteLine($"{sshClient.StandardOutput}{Environment.NewLine}{Environment.NewLine}{sshClient.StandardError}".Trim());
                    Console.WriteLine();
                }
                finally
                {
                    ExecuteSshCommand.StandardOutputLock.Release();
                }
            }
        }

        private void ThrowIfCommandFailed(ProcessDetails result)
        {
            if (result.ExitCode != 0)
            {
                if (this.ComponentType == ComponentType.Action)
                {
                    throw new WorkloadException(
                        $"SSH command execution failed (exit code = {result.ExitCode}, command = {SensitiveData.ObscureSecrets(result.CommandLine)}).",
                        ErrorReason.WorkloadFailed);
                }
                else if (this.ComponentType == ComponentType.Dependency)
                {
                    throw new DependencyException(
                        $"SSH command execution failed (exit code = {result.ExitCode}, command = {SensitiveData.ObscureSecrets(result.CommandLine)}).",
                        ErrorReason.DependencyInstallationFailed);
                }
            }
        }
    }
}
