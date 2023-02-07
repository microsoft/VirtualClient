// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// The Performance Counter Monitor for Virtual Client
    /// </summary>
    public class LspciMonitor : VirtualClientIntervalBasedMonitor
    {
        private static readonly string Lspci = "lspci";

        /// <summary>
        /// Initializes a new instance of the <see cref="LspciMonitor"/> class.
        /// </summary>
        public LspciMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    if (this.CpuArchitecture == System.Runtime.InteropServices.Architecture.X64)
                    {
                        await this.ListPciAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }

                    // skipping if running ARM64
                    break;

                case PlatformID.Unix:
                    await this.ListPciAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void ValidateParameters()
        {
            base.ValidateParameters();
            if (this.MonitorFrequency <= TimeSpan.Zero)
            {
                throw new MonitorException(
                    $"The monitor frequency defined/provided for the '{this.TypeName}' component '{this.MonitorFrequency}' is not valid. " +
                    $"The frequency must be greater than zero.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private async Task ListPciAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

            string command = "lspci";
            string commandArguments = "-vvv";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string workingDir = Environment.CurrentDirectory;
                    if (this.Platform == PlatformID.Win32NT)
                    {
                        DependencyPath lspciPackage = await systemManagement.PackageManager.GetPackageAsync(LspciMonitor.Lspci, CancellationToken.None).ConfigureAwait(false);
                        workingDir = this.PlatformSpecifics.ToPlatformSpecificPath(lspciPackage, this.Platform, this.CpuArchitecture).Path;
                    }

                    using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, $"{commandArguments}", workingDir))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        DateTime startTime = DateTime.UtcNow;
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        DateTime endTime = DateTime.UtcNow;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                // We cannot log the process details here. The output is too large.
                                // this.Logger.LogProcessDetails<LspciMonitor>(process, EventContext.Persisted());
                                process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.MonitorFailed);

                                if (process.StandardOutput.Length > 0)
                                {
                                    LspciParser parser = new LspciParser(process.StandardOutput.ToString());
                                    IList<PciDevice> pciDevices = parser.Parse();

                                    foreach (PciDevice pciDevice in pciDevices)
                                    {
                                        string message = $"PCI Device: '{pciDevice.Name}'. Address: '{pciDevice.Address}'";
                                        this.Logger.LogSystemEvents(message, pciDevice.Properties, telemetryContext);
                                        foreach (PciDevice.PciDeviceCapability capability in pciDevice.Capabilities)
                                        {
                                            message = $"{message}. Capability: '{capability.Name}'";
                                            this.Logger.LogSystemEvents(message, pciDevice.Properties, telemetryContext);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // We cannot log the process details here. The output is too large. We will log on errors
                                // though.
                                this.Logger.LogProcessDetails<LspciMonitor>(process, EventContext.Persisted());
                                throw;
                            }
                        }

                        await Task.Delay(this.MonitorFrequency).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever ctrl-C is used.
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }
    }
}