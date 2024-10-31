// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
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
                    // Do nothing on Windows for now. The binary we are using have error:
                    // lspci: Cannot find any working access method.
                    break;

                case PlatformID.Unix:
                    await this.ListPciAsync(telemetryContext, cancellationToken);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();
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

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken);

            bool firstRun = true;
            long iteration = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                string command = (this.Platform == PlatformID.Unix) ? "lspci" : "lspci.exe";
                string commandArguments = "-vvv";

                if (this.MonitorIterations != -1 && iteration >= this.MonitorIterations) 
                {
                    // Default is set to -1 for infinite loop.
                    break;
                }

                try
                {
                    if (!firstRun)
                    {
                        // Wait on the monitor frequency.
                        await Task.Delay(this.MonitorFrequency, cancellationToken);
                    }

                    firstRun = false;
                    string workingDir = Environment.CurrentDirectory;

                    if (this.Platform == PlatformID.Win32NT)
                    {
                        DependencyPath lspciPackage = await this.GetPlatformSpecificPackageAsync(LspciMonitor.Lspci, CancellationToken.None);
                        workingDir = lspciPackage.Path;
                        command = this.PlatformSpecifics.Combine(workingDir, command);
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
                            // The output is rather large, but we account for it by applying a maximum number of characters
                            // to the output.
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Lspci", logToFile: true);
                            process.ThrowIfMonitorFailed();

                            if (process.StandardOutput.Length > 0)
                            {
                                LspciParser parser = new LspciParser(process.StandardOutput.ToString());
                                IList<PciDevice> pciDevices = parser.Parse();

                                foreach (PciDevice pciDevice in pciDevices)
                                {
                                    string message = $"PCI Device: '{pciDevice.Name}'. Address: '{pciDevice.Address}'";
                                    IEnumerable<string> capabilities = pciDevice.Capabilities.Select(c => c.Name);
                                    IDictionary<string, object> eventInfo = new Dictionary<string, object>(pciDevice.Properties);
                                    eventInfo.Add("capabilities", capabilities);

                                    this.Logger.LogSystemEvent(
                                        "SystemInfo", 
                                        "lspci",
                                        $"{pciDevice.Name}_{pciDevice.Address}".ToLowerInvariant(),
                                        LogLevel.Information, 
                                        telemetryContext,
                                        eventDescription: message,
                                        eventInfo: eventInfo);
                                }
                            }
                        }
                    }

                    iteration++;
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