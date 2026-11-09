// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.ProcessAffinity;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    internal sealed class AspNetServerProcess
    {
        private readonly VirtualClientComponent component;
        private readonly ISystemManagement systemManagement;
        private readonly string processLogName;
        private CancellationTokenSource cancellation;

        public AspNetServerProcess(VirtualClientComponent component, ISystemManagement systemManagement, string processLogName)
        {
            this.component = component;
            this.systemManagement = systemManagement;
            this.processLogName = processLogName;
        }

        public Task Completion { get; private set; }

        public void Start(
            string command,
            string arguments,
            string directory,
            ProcessAffinityConfiguration affinity,
            IAsyncPolicy retryPolicy,
            EventContext context,
            CancellationToken cancellationToken)
        {
            if (this.Completion != null)
            {
                throw new InvalidOperationException("The previous ASP.NET server must be stopped before starting another instance.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            this.cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (affinity != null)
            {
                context.AddContext("affinityMask", affinity.ToString());
            }

            this.Completion = this.RunAsync(command, arguments, directory, affinity, retryPolicy, context, this.cancellation.Token);
        }

        public async Task EnsureRunningAsync()
        {
            if (this.Completion.IsCompleted)
            {
                await this.Completion.ConfigureAwait(false);
            }
        }

        public async Task StopAsync(EventContext context)
        {
            this.component.Logger.LogTraceMessage($"{this.component.TypeName}.KillServerInstances", context);
            if (this.cancellation != null)
            {
                try
                {
                    await this.cancellation.CancelAsync().ConfigureAwait(false);
                    await this.Completion.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (this.cancellation.IsCancellationRequested)
                {
                }
                finally
                {
                    this.cancellation.Dispose();
                    this.cancellation = null;
                    this.Completion = null;
                }
            }
        }

        private async Task RunAsync(
            string command,
            string arguments,
            string directory,
            ProcessAffinityConfiguration affinity,
            IAsyncPolicy retryPolicy,
            EventContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                await (retryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(
                    token => this.RunInstanceAsync(command, arguments, directory, affinity, context, token),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                this.component.SetServerOnline(false);
                this.component.Logger.LogMessage(
                    $"{this.component.TypeName}.ServerFailed",
                    LogLevel.Error,
                    context.Clone().AddError(exception));

                throw;
            }
        }

        private async Task RunInstanceAsync(
            string command,
            string arguments,
            string directory,
            ProcessAffinityConfiguration affinity,
            EventContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                IProcessProxy process;
                if (affinity is LinuxProcessAffinityConfiguration linuxAffinity)
                {
                    string fullCommand = linuxAffinity.GetCommandWithAffinity(null, $"{command} {arguments}");
                    process = this.systemManagement.ProcessManager.CreateProcess("/bin/bash", $"-c {fullCommand}", directory);
                }
                else
                {
                    process = this.systemManagement.ProcessManager.CreateProcess(command, arguments, directory);
                }

                using (process)
                {
                    this.component.Logger.LogTraceMessage($"Executing: {command} {SensitiveData.ObscureSecrets(arguments)}", context);
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (!process.Start())
                        {
                            throw new WorkloadException("The ASP.NET server process did not start.", ErrorReason.WorkloadFailed);
                        }
                    }
                    catch (System.ComponentModel.Win32Exception exception)
                    {
                        throw new WorkloadException(
                            $"The ASP.NET server process could not start (command={SensitiveData.ObscureSecrets(process.FullCommand())}).",
                            exception,
                            ErrorReason.WorkloadFailed);
                    }

                    try
                    {
                        if (affinity is WindowsProcessAffinityConfiguration windowsAffinity)
                        {
                            process.ApplyAffinity(windowsAffinity);
                        }

                        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        process.SafeKill(this.component.Logger, TimeSpan.FromSeconds(30));
                        if (!process.HasExited)
                        {
                            throw new WorkloadException("The ASP.NET server process could not be stopped.", ErrorReason.WorkloadFailed);
                        }

                        await this.component.LogProcessDetailsAsync(process, context, this.processLogName).ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    process.ThrowIfWorkloadFailed();
                    throw new WorkloadException("The ASP.NET server exited unexpectedly.", ErrorReason.WorkloadFailed);
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                this.component.Logger.LogMessage(
                    $"{this.component.TypeName}.StartServerInstanceError",
                    LogLevel.Error,
                    context.Clone().AddError(exception));

                if (exception is OperationCanceledException)
                {
                    throw new WorkloadException("The ASP.NET server was cancelled unexpectedly.", exception, ErrorReason.WorkloadFailed);
                }

                throw;
            }
        }
    }
}
