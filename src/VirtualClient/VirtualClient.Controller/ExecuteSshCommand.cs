// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Component executes an SSH command on a target system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ExecuteSshCommand : VirtualClientControllerComponent
    {
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

        /// <inheritdoc />
        protected override async Task ExecuteAsync(ISshClientProxy sshTarget, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("command", SensitiveData.ObscureSecrets(this.Command));

            try
            {
                await sshTarget.ConnectAsync();
                ProcessDetails result = await sshTarget.ExecuteCommandAsync(this.Command, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    sshTarget.StandardOutput?.WriteLine();
                    sshTarget.StandardOutput?.WriteLine($"[Execute Command on Target]");
                    sshTarget.StandardOutput?.WriteLine($"Command: {SensitiveData.ObscureSecrets(this.Command)}");

                    await this.LogProcessDetailsAsync(result, telemetryContext);
                    this.ThrowIfCommandFailed(result);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when a cancellation is requested.
            }
            catch (Exception exc)
            {
                sshTarget.StandardError?.WriteLine(exc.Message);
                throw;
            }
            finally
            {
                sshTarget.Disconnect();
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
