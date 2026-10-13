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
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Executes the ipmiutil toolset to monitor BMC sensors on the system.
    /// </summary>
    [SupportedPlatforms("linux-x64,linux-arm64,win-x64,win-arm64")]
    public class IpmiUtilSensorMonitor : VirtualClientIntervalBasedMonitor
    {
        private IFileSystem fileSystem;
        private ProcessManager processManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="IpmiUtilSensorMonitor"/> class.
        /// </summary>
        public IpmiUtilSensorMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.processManager = dependencies.GetService<ProcessManager>();
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The path to the impiutil
        /// </summary>
        protected string IpmiUtilExePath { get; set; }

        /// <summary>
        /// Initializes the monitor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                string executableName = this.Platform == PlatformID.Unix ? "ipmiutil" : "ipmiutil.exe";
                string helpSwitch = this.Platform == PlatformID.Unix ? "--help" : "/?";

                DependencyPath monitorsPackage = null;

                if (this.Parameters.TryGetValue(nameof(this.PackageName), out IConvertible packageName) 
                    && !string.IsNullOrWhiteSpace(packageName?.ToString()))
                {
                    try
                    {
                        DependencyPath toolsetPackage = await this.GetPlatformSpecificPackageAsync(packageName?.ToString(), cancellationToken, throwIfNotfound: false);

                        // Account for shortcomings in the GetPlatformSpecificPackageAsync method
                        // implementation when throwIfNotFound = false until fixed in open source.
                        if (toolsetPackage != null && this.fileSystem.File.Exists(this.Combine(toolsetPackage.Path, executableName)))
                        {
                            monitorsPackage = toolsetPackage;
                        }
                    }
                    catch
                    {
                        // Account for bug in GetPlatformSpecificPackageAsync method when throwIfNotFound = false
                        // until the bug is addressed.
                    }
                }

                if (monitorsPackage != null)
                {
                    // e.g.
                    // C:\Users\AnyUser\VirtualClient\packages\host.monitors.1.0.0\win-x64\ipmiutil.exe
                    // /home/anyuser/VirtualClient/packages/host.monitors.1.0.0/linux-x64/ipmiutil
                    this.IpmiUtilExePath = this.Combine(monitorsPackage.Path, executableName);

                    if (this.Platform == PlatformID.Unix)
                    {
                        await this.systemManager.MakeFileExecutableAsync(this.IpmiUtilExePath, this.Platform, cancellationToken);
                    }
                }
                else
                {
                    // If we cannot find the ipmiutil toolset in the alternate locations, it might be available
                    // in a location defined in the PATH environment variable.
                    //
                    // e.g.
                    // ipmiutil --help
                    // ipmiutil /?
                    using (IProcessProxy process = this.processManager.CreateProcess(executableName, helpSwitch, null))
                    {
                        await process.StartAndWaitAsync(cancellationToken);

                        // ipmiutil returns various exit codes on success when using the help
                        // switch. We check for the presence of standard output to confirm the toolset exists.
                        if (!cancellationToken.IsCancellationRequested && !string.IsNullOrWhiteSpace(process.StandardOutput?.ToString()))
                        {
                            this.IpmiUtilExePath = executableName;
                        }
                    }

                    // If we cannot find the ipmiutil toolset in any other location, fallback to any defined
                    // alternate locations.
                    if (string.IsNullOrWhiteSpace(this.IpmiUtilExePath))
                    {
                        foreach (var alternateLocation in IpmiUtilSelMonitor.ToolsetAlternateLocations.Where(entry => entry.Value == this.Platform))
                        {
                            string executablePath = alternateLocation.Key;
                            if (this.fileSystem.File.Exists(executablePath))
                            {
                                this.IpmiUtilExePath = executablePath;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                // Do not let the monitor crash the application.
                this.Logger.LogMessage(
                    $"{this.TypeName}.InitializationError",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(exc));
            }
        }

        /// <summary>
        /// Monitors Ipmiutil on the target machine
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
                // logic will block without returning. Monitors are typically expected to be fire-and-forget.

                if (string.IsNullOrWhiteSpace(this.IpmiUtilExePath))
                {
                    this.Logger.LogMessage(
                        $"The 'ipmiutil' toolset was not found on the system. This monitor will exit.",
                        LogLevel.Warning,
                        telemetryContext);
                }
                else
                {
                    long currentIteration = 1;
                    DateTime nextIteration = DateTime.UtcNow.Add(this.MonitorWarmupPeriod);
                    while (!cancellationToken.IsCancellationRequested && !this.IsIterationComplete(currentIteration))
                    {
                        try
                        {
                            await this.WaitAsync(nextIteration, cancellationToken);
                            nextIteration = DateTime.UtcNow.Add(this.MonitorFrequency);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            string command = this.IpmiUtilExePath;
                            string commandArguments = "sensor -s";

                            telemetryContext.AddContext("command", command);
                            telemetryContext.AddContext("commandArguments", commandArguments);

                            using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, null, telemetryContext, CancellationToken.None, runElevated: true))
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, toolName: "ipmiutil_sensor", logFileName: "ipmiutil_sensor");
                                process.ThrowIfMonitorFailed();

                                if (process.ExitCode == 0)
                                {
                                    IpmiUtilSensorMetricsParser parser = new IpmiUtilSensorMetricsParser();
                                    IList<Metric> metrics = parser.Parse(process.StandardOutput.ToString());

                                    if (metrics?.Any() == true)
                                    {
                                        this.Logger.LogMetrics(
                                            toolName: "ipmiutil",
                                            scenarioName: "Sensors",
                                            process.StartTime,
                                            process.ExitTime,
                                            metrics,
                                            metricCategorization: "IPMI",
                                            scenarioArguments: process.FullCommand(),
                                            this.Tags,
                                            telemetryContext);
                                    }

                                    if (parser.InvalidIpmiSensorEntries?.Any() == true)
                                    {
                                        foreach (var sensorEvent in parser.InvalidIpmiSensorEntries)
                                        {
                                            if (sensorEvent.TryGetValue("name", out var name))
                                            {
                                                string sensorName = name?.ToString();

                                                EventContext relatedContext = telemetryContext.Clone();
                                                relatedContext.AddContext("sensorName", sensorName);

                                                this.Logger.LogSystemEvent(
                                                    "ipmi_sensor_event",
                                                    "ipmiutil",
                                                    sensorName,
                                                    LogLevel.Warning,
                                                    relatedContext,
                                                    eventDescription: $"{this.IpmiUtilExePath} {commandArguments}",
                                                    eventInfo: sensorEvent);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            this.Logger.LogMessage(
                                $"{this.TypeName}.{this.Scenario}Warning",
                                LogLevel.Warning,
                                telemetryContext.Clone().AddError(exc));
                        }
                        finally
                        {
                            currentIteration++;
                        }
                    }
                }
            });
        }
    }
}