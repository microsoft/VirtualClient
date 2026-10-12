// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes the ipmiutil toolset to monitor SEL logs on the system.
    /// Enhanced to detect suspected hardware errors.
    /// </summary>
    /// <remarks>
    /// https://ipmiutil.sourceforge.net/
    /// https://ipmiutil.sourceforge.net/docs/UserGuide
    /// </remarks>
    [SupportedPlatforms("linux-x64,linux-arm64,win-x64,win-arm64")]
    public class IpmiUtilSelMonitor : VirtualClientIntervalBasedMonitor
    {
        internal static readonly IDictionary<string, PlatformID> ToolsetAlternateLocations = new Dictionary<string, PlatformID>
        {
            // The set of possible locations of the ipmiutil toolset for the case that
            // the host monitoring package is not installed.
            { "C:\\BladeFX_latest\\BladeFX\\Tools\\IpmiUtil\\ipmiutil.exe", PlatformID.Win32NT }
        };

        // Specific hardware error patterns
        internal static readonly string[] HardwareErrorPatterns = new[]
        {
                "failure detected", "fault", "error", "throttled", "power supply",
                "temperature", "voltage", "fan", "memory", "processor"
        };

        private const string CommandToGetDecodedSel = "sel -uc";
        private const string CommandToGetRawHexCodes = "sel -r";
        private const string CommandToClearSelLog = "sel -d";

        private IFileSystem fileSystem;
        private ProcessManager processManager;
        private ISystemManagement systemManager;
        private string lastRecordProcessed;

        /// <summary>
        /// Initializes a new instance of the <see cref="IpmiUtilSelMonitor"/> class.
        /// </summary>
        public IpmiUtilSelMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.processManager = dependencies.GetService<ProcessManager>();
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Option to clear the SEL data.
        /// </summary>
        public bool ClearSel
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.ClearSel), false);
            }

            set
            {
                this.Parameters[nameof(this.ClearSel)] = value;
            }
        }

        /// <summary>
        /// Option to enable suspected event detection and create separate files for suspected hardware errors.
        /// When enabled, creates additional output files containing only suspected events.
        /// </summary>
        public bool CaptureSuspectedEvents
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.CaptureSuspectedEvents), false);
            }

            set
            {
                this.Parameters[nameof(this.CaptureSuspectedEvents)] = value;
            }
        }

        /// <summary>
        /// Option to include warning level events in suspected event detection.
        /// When disabled, only hardware errors and critical events are considered suspected.
        /// </summary>
        public bool IncludeWarningsInSuspectedEvents
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.IncludeWarningsInSuspectedEvents), true);
            }

            set
            {
                this.Parameters[nameof(this.IncludeWarningsInSuspectedEvents)] = value;
            }
        }

        /// <summary>
        /// Option to enable SEL decoding. When enabled, the raw SEL hex data
        /// is passed through the configured decoder tool for enhanced, human-readable decoding.
        /// </summary>
        public bool EnableSelDecoding
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.EnableSelDecoding), false);
            }

            set
            {
                this.Parameters[nameof(this.EnableSelDecoding)] = value;
            }
        }

        /// <summary>
        /// Selects the decoder engine to use. Supported values: "Sherlock", "Generic".
        /// Required when <see cref="EnableSelDecoding"/> is true.
        /// </summary>
        public string DecoderType
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DecoderType));
            }

            set
            {
                this.Parameters[nameof(this.DecoderType)] = value;
            }
        }

        /// <summary>
        /// Additional command-line arguments to pass to the SEL decoder.
        /// For Sherlock, these are appended before the input file (-f) argument.
        /// For Generic, these are appended after the input file argument.
        /// e.g. "-nc -t Pacific -v --sel_parser" or "--sku c41a8".
        /// </summary>
        public string DecoderArguments
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.DecoderArguments), out IConvertible decoderArguments);
                return decoderArguments?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.DecoderArguments)] = value;
            }
        }

        /// <summary>
        /// The name of the package that contains the SEL decoder toolset.
        /// </summary>
        public string DecoderPackageName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.DecoderPackageName), out IConvertible packageName);
                return packageName?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.DecoderPackageName)] = value;
            }
        }

        /// <summary>
        /// The executable name of the SEL decoder tool inside the decoder package.
        /// Optional. If not provided, defaults to "crcsdkseldecoder.exe" for Generic
        /// and "sel_decode.exe" for Sherlock. Profiles may override this when the
        /// package ships a different executable name.
        /// </summary>
        public string DecoderExecutableName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.DecoderExecutableName), out IConvertible executableName);
                return executableName?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.DecoderExecutableName)] = value;
            }
        }

        /// <summary>
        /// A regular expression that will be used to match records on each line of the
        /// output. Default is to match any events with severity level major/MAJ or critical/CRT.
        /// </summary>
        public string EventsFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.EventsFilter), "MAJ|CRT");
            }

            set
            {
                this.Parameters[nameof(this.EventsFilter)] = value;
            }
        }

        /// <summary>
        /// The count of Decoded SEL to be emitted as a metric. This is useful to track how many SEL entries were decoded for each monitor iteration.
        /// </summary>
        public int DecodedSelCount { get; set; }

        /// <summary>
        /// The path to the impiutil
        /// </summary>
        protected string IpmiUtilExePath { get; set; }

        /// <summary>
        /// The path to the SEL decoder executable (Sherlock or Generic, based on <see cref="DecoderType"/>).
        /// </summary>
        protected string DecoderExePath { get; set; }

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
                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken);

                            // ipmiutil returns various exit codes on success when using the help
                            // switch. We check for the presence of standard output to confirm the toolset exists.
                            if (!cancellationToken.IsCancellationRequested && !string.IsNullOrWhiteSpace(process.StandardOutput?.ToString()))
                            {
                                this.IpmiUtilExePath = executableName;
                            }
                        }
                        catch
                        {
                            // Win32Exception will be thrown if the executable does not exist on the
                            // system at all.
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

                if (this.EnableSelDecoding)
                {
                    await this.InitializeDecoderAsync(telemetryContext, cancellationToken);
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
                try
                {
                    if (string.IsNullOrWhiteSpace(this.IpmiUtilExePath))
                    {
                        this.Logger.LogMessage(
                            $"The 'ipmiutil' toolset was not found on the system. This monitor will exit.",
                            LogLevel.Warning,
                            telemetryContext);
                    }
                    else
                    {
                        // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
                        // logic will block without returning. Monitors are typically expected to be fire-and-forget.
                        var commandArguments = string.Join(",",
                            IpmiUtilSelMonitor.CommandToGetDecodedSel,
                            IpmiUtilSelMonitor.CommandToGetRawHexCodes,
                            this.ClearSel ? IpmiUtilSelMonitor.CommandToClearSelLog : string.Empty).TrimEnd(',');

                        long currentIteration = 1;
                        DateTime nextIteration = DateTime.UtcNow.Add(this.MonitorWarmupPeriod);
                        while (!cancellationToken.IsCancellationRequested && !this.IsIterationComplete(currentIteration))
                        {
                            try
                            {
                                string combinedSelRecords = null;
                                string rawSelRecords = null;
                                await this.WaitAsync(nextIteration, cancellationToken);
                                nextIteration = DateTime.UtcNow.Add(this.MonitorFrequency);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }

                                // 1) Decoded (human-readable) SEL data
                                //
                                // e.g.
                                // 07cc | 01/24/25 21:35:33 | MAJ | BMC  | Temperature | GPU0_DRAM0_TMP0  | Hi Crit thresh actual=127.00 C, threshold=107.00 C
                                string decodedSelRecords = null;
                                DateTime startTime = DateTime.UtcNow;

                                using (IProcessProxy process1 = await this.ExecuteCommandAsync(this.IpmiUtilExePath, "sel -uc", null, telemetryContext, CancellationToken.None, runElevated: true))
                                {
                                    if (process1.ExitCode != 0)
                                    {
                                        await this.LogProcessDetailsAsync(process1, telemetryContext, "ipmiutil_sel_error");

                                        this.Logger.LogMessage(
                                            $"{this.TypeName}.ReadDecodedSelError",
                                            LogLevel.Warning,
                                            telemetryContext.Clone().AddProcessDetails(process1.ToProcessDetails("ipmiutil")));

                                        continue;
                                    }

                                    decodedSelRecords = process1.StandardOutput?.ToString();
                                    if (!string.IsNullOrWhiteSpace(decodedSelRecords))
                                    {
                                        await this.WaitAsync(TimeSpan.FromSeconds(2), CancellationToken.None);

                                        // 2) Raw ASCII SEL data
                                        //
                                        // e.g.
                                        // cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b
                                        //
                                        // Note that the decoded record and ascii record will be paired together when matched
                                        // by record ID (e.g. record ID = 07cc -> cc 07). The hash at the beginning is used to identify
                                        // unique records so that the logic is prepared to handle cases where SEL logs are or are not
                                        // cleared over periods of time.
                                        //
                                        // RecId | Date/Time         | SEV | Src  | Evt_Type    | Sensor           | Evt_detail                                         | Supplementary/Added -> Evt_detail_raw
                                        // 07cc  | 01/24/25 21:35:33 | MAJ | BMC  | Temperature | GPU0_DRAM0_TMP0  | Hi Crit thresh actual=127.00 C, threshold=107.00 C | cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b

                                        using (IProcessProxy process2 = await this.ExecuteCommandAsync(this.IpmiUtilExePath, "sel -r", null, telemetryContext, CancellationToken.None, runElevated: true))
                                        {
                                            if (process2.ExitCode != 0)
                                            {
                                                await this.LogProcessDetailsAsync(process2, telemetryContext, "ipmiutil_sel_error");

                                                this.Logger.LogMessage(
                                                    $"{this.TypeName}.ReadRawSelError",
                                                    LogLevel.Warning,
                                                    telemetryContext.Clone().AddProcessDetails(process2.ToProcessDetails("ipmiutil")));
                                            }
                                            else
                                            {
                                                rawSelRecords = process2.StandardOutput?.ToString();
                                                combinedSelRecords = IpmiUtilSelMetricsParser.CombineRecords(rawSelRecords, decodedSelRecords);
                                            }
                                        }
                                    }

                                    DateTime exitTime = DateTime.UtcNow;
                                    await this.LogProcessDetailsAsync(
                                        new ProcessDetails
                                        {
                                            Id = process1.Id,
                                            ExitCode = process1.ExitCode,
                                            CommandLine = process1.FullCommand(),
                                            StandardOutput = combinedSelRecords,
                                            StandardError = process1.StandardError?.ToString(),
                                            ToolName = "ipmiutil_sel",
                                            WorkingDirectory = Environment.CurrentDirectory,
                                            StartTime = startTime,
                                            ExitTime = exitTime
                                        },
                                        telemetryContext,
                                        logFileName: "ipmiutil_sel");
                                }

                                this.CaptureMetrics(combinedSelRecords ?? decodedSelRecords, commandArguments, startTime, DateTime.UtcNow, telemetryContext);

                                // Capture suspected events if enabled
                                if (this.CaptureSuspectedEvents)
                                {
                                    await this.CaptureSuspectedEventsAsync(combinedSelRecords ?? decodedSelRecords, telemetryContext, cancellationToken);
                                }

                                // Run SEL decoding if enabled. The decoder engine (Sherlock or Generic) is selected via DecoderType.
                                if (this.EnableSelDecoding && !string.IsNullOrWhiteSpace(this.DecoderExePath))
                                {
                                    if (string.Equals(this.DecoderType, "Generic", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // The generic decoder natively handles pure hex lines (the format produced by `sel -r`),
                                        // so the raw hex records are passed directly without the combined/decoded format.
                                        await this.ExecuteGenericDecodingAsync(rawSelRecords, telemetryContext, cancellationToken);
                                    }
                                    else if (string.Equals(this.DecoderType, "Sherlock", StringComparison.OrdinalIgnoreCase))
                                    {
                                        await this.ExecuteSherlockDecodingAsync(combinedSelRecords ?? decodedSelRecords, telemetryContext, cancellationToken);
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"Unsupported DecoderType: '{this.DecoderType}'. Supported values are 'Sherlock' and 'Generic'.");
                                    }
                                }

                                if (this.ClearSel)
                                {
                                    using (IProcessProxy process3 = await this.ExecuteCommandAsync(this.IpmiUtilExePath, "sel -d", null, telemetryContext, CancellationToken.None, runElevated: true))
                                    {
                                        await this.LogProcessDetailsAsync(process3, telemetryContext, "ipmiutil_sel_clear");

                                        if (process3.ExitCode != 0)
                                        {
                                            this.Logger.LogMessage(
                                                $"{this.TypeName}.ClearSelError",
                                                LogLevel.Warning,
                                                telemetryContext.Clone().AddProcessDetails(process3.ToProcessDetails("ipmiutil")));
                                        }
                                    }
                                }
                            }
                            catch (Exception exc)
                            {
                                // Do not let the monitor crash the application. Continue to attempt to run the
                                // ipmiutil monitoring commands.
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
                }
                catch (Exception exc)
                {
                    // Do not let the monitor crash the application.
                    this.Logger.LogMessage(
                        $"{this.TypeName}.ExecuteError",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddError(exc));
                }
            });
        }

        /// <summary>
        /// Capture metrics from the results of the 'ipmiutil sel' command.
        /// </summary>
        protected void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (!string.IsNullOrWhiteSpace(results))
            {
                Regex eventsFilter = null;
                if (!string.IsNullOrWhiteSpace(this.EventsFilter))
                {
                    eventsFilter = new Regex(this.EventsFilter, RegexOptions.IgnoreCase);
                }

                IpmiUtilSelMetricsParser.ParsingResult parsingResult = IpmiUtilSelMetricsParser.Parse(
                    results,
                    this.lastRecordProcessed,
                    eventsFilter);

                if (parsingResult?.Metrics?.Any() == true)
                {
                    this.Logger.LogMetrics(
                        toolName: "ipmiutil",
                        scenarioName: "SEL",
                        startTime,
                        endTime,
                        parsingResult.Metrics,
                        metricCategorization: "IPMI",
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);
                }

                this.lastRecordProcessed = parsingResult.LastRecordProcessed;
            }
        }

        /// <summary>
        /// Initializes the SEL decoder toolset based on the configured <see cref="DecoderType"/>.
        /// </summary>
        protected async Task InitializeDecoderAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                string executableName;
                if (!string.IsNullOrWhiteSpace(this.DecoderExecutableName))
                {
                    executableName = this.DecoderExecutableName;
                }
                else if (string.Equals(this.DecoderType, "Generic", StringComparison.OrdinalIgnoreCase))
                {
                    executableName = this.Platform == PlatformID.Unix ? "crcsdkseldecoder" : "crcsdkseldecoder.exe";
                }
                else if (string.Equals(this.DecoderType, "Sherlock", StringComparison.OrdinalIgnoreCase))
                {
                    executableName = "sel_decode.exe";
                }
                else
                {
                    throw new ArgumentException($"Unsupported DecoderType: '{this.DecoderType}'. Supported values are 'Sherlock' and 'Generic'.");
                }

                if (!string.IsNullOrWhiteSpace(this.DecoderPackageName))
                {
                    try
                    {
                        DependencyPath decoderPackage = await this.GetPlatformSpecificPackageAsync(this.DecoderPackageName, cancellationToken, throwIfNotfound: false);

                        if (decoderPackage != null && this.fileSystem.File.Exists(this.Combine(decoderPackage.Path, executableName)))
                        {
                            this.DecoderExePath = this.Combine(decoderPackage.Path, executableName);

                            if (this.Platform == PlatformID.Unix)
                            {
                                await this.systemManager.MakeFileExecutableAsync(this.DecoderExePath, this.Platform, cancellationToken);
                            }
                        }
                    }
                    catch
                    {
                        // Account for bug in GetPlatformSpecificPackageAsync method when throwIfNotFound = false
                        // until the bug is addressed.
                    }
                }

                if (string.IsNullOrWhiteSpace(this.DecoderExePath))
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.DecoderToolsetNotFound",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddContext("message", $"The {this.DecoderType} SEL decoder toolset was not found. Decoding will be skipped.")
                            .AddContext("decoderType", this.DecoderType)
                            .AddContext("decoderPackageName", this.DecoderPackageName));
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.DecoderInitializationError",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddContext("decoderType", this.DecoderType).AddError(exc));
            }
        }

        /// <summary>
        /// Executes the Sherlock SEL decoder and uploads the decoded output.
        /// Create a file even if no data found and a comment in the file stating no sel records available for decoding.
        /// </summary>
        protected async Task ExecuteSherlockDecodingAsync(string selRecords, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selRecords))
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.SherlockDecodingSkipped",
                        LogLevel.Information,
                        telemetryContext.Clone().AddContext("message", "No sel data available for Sherlock decoding."));
                    return;
                }

                this.DecodedSelCount = 0;
                string logsDirectory = this.PlatformSpecifics.GetLogsPath();
                string scenarioDirectory = this.PlatformSpecifics.Combine(logsDirectory, this.Scenario ?? "IpmiUtilSel");
                this.fileSystem.Directory.CreateDirectory(scenarioDirectory);

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
                string inputFilePath = this.PlatformSpecifics.Combine(scenarioDirectory, $"{timestamp}_selRecords.txt");
                await this.fileSystem.File.WriteAllTextAsync(inputFilePath, selRecords, cancellationToken);

                // Execute the Sherlock decoder.
                string extraArgs = !string.IsNullOrWhiteSpace(this.DecoderArguments) ? $"{this.DecoderArguments.Trim()} " : string.Empty;
                string decoderArguments = $"{extraArgs} -f \"{inputFilePath}\"";

                using (IProcessProxy sherlockProcess = await this.ExecuteCommandAsync(
                    this.DecoderExePath, decoderArguments, null, telemetryContext, CancellationToken.None, runElevated: false))
                {
                    await this.LogProcessDetailsAsync(
                        sherlockProcess,
                        telemetryContext,
                        toolName: "ipmiutil_sel",
                        logFileName: "decodedseloutput");

                    if (sherlockProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(sherlockProcess.StandardOutput.ToString()))
                    {
                        this.DecodedSelCount = this.ParseAndLogDecodedSelEntries(sherlockProcess.StandardOutput.ToString(), telemetryContext);
                    }
                    else
                    {
                        this.Logger.LogMessage(
                            $"{this.TypeName}.SherlockDecodingError",
                            LogLevel.Warning,
                            telemetryContext.Clone().AddProcessDetails(sherlockProcess.ToProcessDetails("sel_decode")));
                    }

                    this.Logger.LogMetric(
                        new Metric(nameof(this.DecodedSelCount), this.DecodedSelCount, "count"),
                        "SherlockIpmiUtilSelDecoder",
                        this.Scenario,
                        sherlockProcess.StartTime,
                        sherlockProcess.ExitTime,
                        decoderArguments,
                        telemetryContext);
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.SherlockDecodingError",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(exc));
            }
        }

        /// <summary>
        /// Saves the raw SEL hex records to a file and executes the generic SEL decoder against it.
        /// The generic decoder writes its output to files in a timestamped output directory; the
        /// "All-BMC-SEL-Decoded.log" file is read back, parsed, and uploaded to the content store.
        /// </summary>
        protected async Task ExecuteGenericDecodingAsync(string selRecords, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selRecords))
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.GenericDecodingSkipped",
                        LogLevel.Information,
                        telemetryContext.Clone().AddContext("message", "No SEL data available for generic decoding."));
                    return;
                }

                this.DecodedSelCount = 0;
                string logsDirectory = this.PlatformSpecifics.GetLogsPath();
                string scenarioDirectory = this.PlatformSpecifics.Combine(logsDirectory, this.Scenario ?? "IpmiUtilSel");
                this.fileSystem.Directory.CreateDirectory(scenarioDirectory);

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-fff");
                string outputDirectory = this.PlatformSpecifics.Combine(scenarioDirectory, $"{timestamp}_output");
                this.fileSystem.Directory.CreateDirectory(outputDirectory);

                string inputFilePath = this.PlatformSpecifics.Combine(outputDirectory, $"{timestamp}_input_selRecords.txt");
                await this.fileSystem.File.WriteAllTextAsync(inputFilePath, selRecords, cancellationToken);

                string extraArgs = !string.IsNullOrWhiteSpace(this.DecoderArguments) ? $" {this.DecoderArguments.Trim()}" : string.Empty;
                string genericArguments = $"\"{inputFilePath}\" -o \"{outputDirectory}\"{extraArgs}";

                using (IProcessProxy genericProcess = await this.ExecuteCommandAsync(
                    this.DecoderExePath, genericArguments, null, telemetryContext, CancellationToken.None, runElevated: false))
                {
                    await this.LogProcessDetailsAsync(
                        genericProcess,
                        telemetryContext,
                        toolName: "ipmiutil_sel",
                        logFileName: "decodedseloutput");

                    string decodedOutputFile = this.PlatformSpecifics.Combine(outputDirectory, "All-BMC-SEL-Decoded.log");

                    if (genericProcess.ExitCode == 0 && this.fileSystem.File.Exists(decodedOutputFile))
                    {
                        string decodedOutput = await this.fileSystem.File.ReadAllTextAsync(decodedOutputFile, cancellationToken);

                        if (!string.IsNullOrWhiteSpace(decodedOutput))
                        {
                            this.DecodedSelCount = this.ParseAndLogGenericDecodedSelEntries(decodedOutput, telemetryContext);

                            // Copy the decoded file to a timestamped name and request upload to the content store.
                            string decodedTimestampedName = $"{timestamp}_decoded_selRecords.log";
                            string decodedTimestampedPath = this.PlatformSpecifics.Combine(outputDirectory, decodedTimestampedName);
                            this.fileSystem.File.Copy(decodedOutputFile, decodedTimestampedPath, overwrite: true);

                            if (this.fileSystem.File.Exists(decodedTimestampedPath) && this.TryGetContentStoreManager(out IBlobManager blobManager))
                            {
                                FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                                    new FileContext(
                                        this.fileSystem.FileInfo.New(decodedTimestampedPath),
                                        HttpContentType.PlainText,
                                        Encoding.UTF8.WebName,
                                        this.ExperimentId,
                                        this.AgentId,
                                        "ipmiutil_sel",
                                        this.Scenario,
                                        null,
                                        this.Roles?.FirstOrDefault()),
                                    this.Parameters,
                                    timestamped: false);

                                await this.RequestFileUploadAsync(descriptor);
                            }
                        }
                        else
                        {
                            this.Logger.LogTraceMessage($"Generic SEL decoder produced an empty output file: {decodedOutputFile}");

                            this.Logger.LogMessage(
                                $"{this.TypeName}.GenericDecodingError",
                                LogLevel.Warning,
                                telemetryContext.Clone()
                                    .AddContext("message", "Decoded output file is empty.")
                                    .AddContext("inputFile", inputFilePath)
                                    .AddContext("outputFile", decodedOutputFile)
                                    .AddProcessDetails(genericProcess.ToProcessDetails("crcsdkseldecoder")));
                        }
                    }
                    else
                    {
                        this.Logger.LogTraceMessage($"Generic SEL decoder failed (exitCode={genericProcess.ExitCode}): {genericProcess.StandardError?.ToString()}");

                        this.Logger.LogMessage(
                            $"{this.TypeName}.GenericDecodingError",
                            LogLevel.Warning,
                            telemetryContext.Clone()
                                .AddContext("message", genericProcess.StandardError?.ToString())
                                .AddContext("exitCode", genericProcess.ExitCode)
                                .AddContext("inputFile", inputFilePath)
                                .AddProcessDetails(genericProcess.ToProcessDetails("crcsdkseldecoder")));
                    }

                    this.Logger.LogMetric(
                        new Metric(nameof(this.DecodedSelCount), this.DecodedSelCount, "count"),
                        "GenericIpmiUtilSelDecoder",
                        this.Scenario,
                        genericProcess.StartTime,
                        genericProcess.ExitTime,
                        genericArguments,
                        telemetryContext);
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.GenericDecodingError",
                    LogLevel.Warning,
                    telemetryContext.Clone()
                        .AddContext("message", exc.Message)
                        .AddError(exc));
            }
        }

        /// <summary>
        /// Captures suspected events from SEL records.
        /// Creates separate files for suspected and warning SEL entries.
        /// </summary>
        protected async Task CaptureSuspectedEventsAsync(string results, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(results))
                {
                    // Create empty suspected events file to indicate monitoring ran but found no events
                    await this.WriteSuspectedEventsFile(
                        "# No SEL events found during monitoring period\n" +
                        $"# Monitoring time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                        $"# Suspected event detection: Enabled\n" +
                        $"# Include warnings: {this.IncludeWarningsInSuspectedEvents}\n",
                        "SuspectSelEntries",
                        cancellationToken);
                    return;
                }

                var (suspectedEvents, warningEvents) = this.AnalyzeSuspectedSelEvents(results);

                // Create suspected events file (always create, even if empty)
                string suspectedContent = suspectedEvents.Any() 
                    ? string.Join("\n", suspectedEvents) 
                    : "# No suspected SEL events found during monitoring period\n" +
                      $"# Monitoring time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                      $"# Total SEL events scanned: {this.CountSelRecords(results)}\n" +
                      $"# Suspected event detection: Enabled\n" +
                      $"# Include warnings: {this.IncludeWarningsInSuspectedEvents}\n";

                await this.WriteSuspectedEventsFile(suspectedContent, "SuspectSelEntries", cancellationToken);

                // Create warning events file if there are any warning events
                if (warningEvents.Any())
                {
                    string warningContent = $"# Warning SEL events found during monitoring period\n" +
                                          $"# Monitoring time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                                          $"# Warning events count: {warningEvents.Count}\n\n" +
                                          string.Join("\n", warningEvents);

                    await this.WriteSuspectedEventsFile(warningContent, "WarningSelEntries", cancellationToken);
                }

                // Log telemetry about suspected events
                telemetryContext.AddContext("SuspectedSelEventsCount", suspectedEvents.Count);
                telemetryContext.AddContext("WarningSelEventsCount", warningEvents.Count);

                if (suspectedEvents.Any())
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.SuspectedSelEventsFound",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddContext("message", $"Found {suspectedEvents.Count} suspected SEL events"));
                }
                else
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.NoSuspectedSelEventsFound",
                        LogLevel.Information,
                        telemetryContext.Clone().AddContext("message", "No suspected SEL events found"));
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.SuspectedEventsError",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(exc));
            }
        }

        /// <summary>
        /// Analyzes SEL events to identify suspected hardware errors and warnings.
        /// </summary>
        protected (IList<string> suspectedEvents, IList<string> warningEvents) AnalyzeSuspectedSelEvents(string results)
        {
            var suspectedEvents = new List<string>();
            var warningEvents = new List<string>();
            var assertedEvents = new Dictionary<string, List<string>>();

            if (string.IsNullOrWhiteSpace(results))
            {
                return (suspectedEvents, warningEvents);
            }

            string[] lines = results.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Skip header lines
                if (line.Contains("RecId") && line.Contains("Date/Time"))
                {
                    continue;
                }

                var selRecord = this.ParseSelRecord(line);
                if (selRecord == null)
                {
                    continue;
                }

                bool isSuspected = this.IsSelEventSuspected(selRecord);
                bool isWarning = this.IsSelEventWarning(selRecord);

                if (isSuspected)
                {
                    suspectedEvents.Add(line);
                }
                else if (isWarning)
                {
                    warningEvents.Add(line);
                }
            }

            return (suspectedEvents, warningEvents);
        }

        /// <summary>
        /// Determines if a SEL event should be considered suspected.
        /// </summary>
        protected bool IsSelEventSuspected(SelRecord selRecord)
        {
            if (selRecord == null)
            {
                return false;
            }

            // Hardware errors are always suspected
            if (selRecord.IsHardwareError)
            {
                return true;
            }

            // Critical and major severity events are always suspected
            if (selRecord.Severity?.Equals("CRT", StringComparison.OrdinalIgnoreCase) == true ||
                selRecord.Severity?.Equals("MAJ", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // Specific hardware-related events that are always suspected
            var suspectedKeywords = new[]
            {
                "failure detected", "throttled", "power supply ac lost", "fault", "error",
                "correctable ecc", "uncorrectable ecc", "ierr", "frb2", "frb3", "predictive failure",
                "bus error", "fatal nmi"
            };

            string eventDetail = selRecord.EventDetail?.ToLowerInvariant() ?? string.Empty;
            string eventType = selRecord.EventType?.ToLowerInvariant() ?? string.Empty;

            return suspectedKeywords.Any(keyword => 
                eventDetail.Contains(keyword) || eventType.Contains(keyword));
        }

        /// <summary>
        /// Determines if a SEL event should be considered a warning that needs pairing analysis.
        /// </summary>
        protected bool IsSelEventWarning(SelRecord selRecord)
        {
            if (selRecord == null || !this.IncludeWarningsInSuspectedEvents)
            {
                return false;
            }

            // Warning level events that might have asserted/deasserted pairs
            string eventDetail = selRecord.EventDetail?.ToLowerInvariant() ?? string.Empty;

            return eventDetail.Contains("asserted") || 
                   eventDetail.Contains("deasserted") ||
                   eventDetail.Contains("exceeded") ||
                   eventDetail.Contains("within") ||
                   eventDetail.Contains("above") ||
                   eventDetail.Contains("below");
        }

        /// <summary>
        /// Parses a SEL record line into a structured object.
        /// </summary>
        protected SelRecord ParseSelRecord(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var match = IpmiUtilSelMetricsParser.SelRecordPattern.Match(line);
            if (!match.Success)
            {
                return null;
            }

            return new SelRecord
            {
                RecordId = match.Groups["RecordId"]?.Value?.Trim(),
                Timestamp = match.Groups["Timestamp"]?.Value?.Trim(),
                Severity = match.Groups["Severity"]?.Value?.Trim(),
                Source = match.Groups["Source"]?.Value?.Trim(),
                EventType = match.Groups["EventType"]?.Value?.Trim(),
                Sensor = match.Groups["Sensor"]?.Value?.Trim(),
                EventDetail = match.Groups["EventDetail"]?.Value?.Trim(),
                IsHardwareError = this.DetermineIfHardwareError(match.Groups["Severity"]?.Value?.Trim(), 
                                                              match.Groups["EventDetail"]?.Value?.Trim())
            };
        }

        /// <summary>
        /// Determines if an event represents a hardware error.
        /// </summary>
        protected bool DetermineIfHardwareError(string severity, string eventDetail)
        {
            if (string.IsNullOrWhiteSpace(severity))
            {
                return false;
            }

            // Critical and Major events are considered hardware errors
            if (severity.Equals("CRT", StringComparison.OrdinalIgnoreCase) ||
                severity.Equals("MAJ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // For other severity levels, check if eventDetail contains hardware error patterns
            if (string.IsNullOrWhiteSpace(eventDetail))
            {
                return false;
            }

            string detail = eventDetail.ToLowerInvariant();
            return HardwareErrorPatterns.Any(pattern => detail.Contains(pattern));
        }

        /// <summary>
        /// Counts the number of SEL records in the results.
        /// </summary>
        protected int CountSelRecords(string results)
        {
            if (string.IsNullOrWhiteSpace(results))
            {
                return 0;
            }

            return results.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .Count(line => IpmiUtilSelMetricsParser.SelRecordPattern.IsMatch(line));
        }

        /// <summary>
        /// Writes suspected events to a file in the logs directory.
        /// </summary>
        protected async Task WriteSuspectedEventsFile(string content, string fileName, CancellationToken cancellationToken)
        {
            try
            {
                string logsDirectory = this.PlatformSpecifics.GetLogsPath();
                string scenarioDirectory = this.PlatformSpecifics.Combine(logsDirectory, this.Scenario ?? "IpmiUtilSel");
                this.fileSystem.Directory.CreateDirectory(scenarioDirectory);

                string filePath = this.PlatformSpecifics.Combine(scenarioDirectory, $"{fileName}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.log");
                await this.fileSystem.File.WriteAllTextAsync(filePath, content, cancellationToken);

                await this.UploadEventLogs(filePath);

                this.Logger.LogMessage(
                    $"{this.TypeName}.SuspectedEventsFileCreated",
                    LogLevel.Information,
                    EventContext.Persisted().AddContext("filePath", filePath));
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.SuspectedEventsFileError",
                    LogLevel.Warning,
                    EventContext.Persisted().AddError(exc));
            }
        }

        /// <summary>
        /// Writes the events to a local file and uploads to content store
        /// </summary>
        protected async Task UploadEventLogs(string outputFilePath)
        {
            // Upload log file to view on result explorer and portal
            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(new FileContext(this.fileSystem.FileInfo.New(outputFilePath), HttpContentType.PlainText, Encoding.UTF8.WebName, this.ExperimentId, this.AgentId, "ipmiutil_sel", this.Scenario, null, this.Roles?.FirstOrDefault()), this.Parameters, timestamped: false);

                await this.RequestFileUploadAsync(descriptor);
            }
        }

        /// <summary>
        /// Parses the decoded SEL output from Sherlock and logs each entry as a system event.
        /// </summary>
        private int ParseAndLogDecodedSelEntries(string decodedOutput, EventContext telemetryContext)
        {
            int decodedSelCount = 0;

            if (string.IsNullOrWhiteSpace(decodedOutput))
            {
                return decodedSelCount;
            }

            string[] lines = decodedOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int headerIndex = -1;

            // Find the header line
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("ID") && lines[i].Contains("Timestamp") && lines[i].Contains("Raw Hex"))
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == -1 || headerIndex + 2 >= lines.Length)
            {
                return decodedSelCount;
            }

            // Skip header and separator line
            int dataStartIndex = headerIndex + 2;

            for (int i = dataStartIndex; i < lines.Length; i++)
            {
                string line = lines[i];

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Check if this is a new entry (starts with 0x)
                if (line.TrimStart().StartsWith("0x"))
                {
                    try
                    {
                        // Parse the main entry line
                        string[] parts = Regex.Split(line, @"\s{2,}");

                        if (parts.Length >= 9)
                        {
                            string id = parts[0].Trim();
                            string timestampUtc = parts[1].Trim();
                            string sensorOwner = parts[2].Trim();
                            string sensorType = parts[3].Trim();
                            string selEventType = parts[4].Trim();
                            string message = parts[5].Trim();
                            string selDirection = parts[6].Trim();
                            string selRecordType = parts[7].Trim();
                            string rawHex = parts[8].Trim();

                            // Collect continuation lines (multi-line messages)
                            int j = i + 1;
                            while (j < lines.Length && !lines[j].TrimStart().StartsWith("0x") && !lines[j].Contains("Decode Errors:") && !lines[j].Contains("Parse Errors:"))
                            {
                                string continuationLine = lines[j].Trim();
                                if (!string.IsNullOrWhiteSpace(continuationLine))
                                {
                                    message = $"{message} {continuationLine}";
                                }

                                j++;
                            }

                            // Update loop counter to skip processed continuation lines
                            i = j - 1;

                            decodedSelCount++;

                            // Log the parsed entry
                            this.Logger.LogSystemEvent(
                                "SelDecoded",
                                "SherlockIpmiUtilSelDecoder",
                                id,
                                LogLevel.Information,
                                telemetryContext,
                                null,
                                message,
                                new Dictionary<string, object>
                                {
                                    ["eventProvider"] = sensorOwner,
                                    ["eventMessage"] = rawHex,
                                    ["eventError"] = null,
                                    ["eventResult"] = message,
                                    ["severityLevel"] = null,
                                    ["eventTimeStamp"] = timestampUtc,
                                    ["sensorType"] = sensorType,
                                    ["selEventType"] = selEventType,
                                    ["selDirection"] = selDirection,
                                    ["selRecordType"] = selRecordType
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogMessage(
                            $"{this.TypeName}.SelEntryParsingError",
                            LogLevel.Warning,
                            telemetryContext.Clone()
                                .AddContext("line", line)
                                .AddError(ex));
                    }
                }
            }

            return decodedSelCount;
        }

        /// <summary>
        /// Parses the decoded SEL output from the generic decoder and logs each entry as a system event.
        /// The output has variable pipe-delimited fields: 7 for SER records, 5 for OEM timestamped, 4 for OEM non-timestamped.
        /// </summary>
        private int ParseAndLogGenericDecodedSelEntries(string decodedOutput, EventContext telemetryContext)
        {
            int decodedSelCount = 0;

            if (string.IsNullOrWhiteSpace(decodedOutput))
            {
                return decodedSelCount;
            }

            string[] lines = decodedOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                try
                {
                    string[] parts = trimmedLine.Split('|');

                    if (parts.Length >= 7)
                    {
                        // SER Record: RecordID | Timestamp | Source | SensorType | SensorID | Message | Direction
                        string recordId = parts[0].Trim();
                        string timestampUtc = parts[1].Trim();
                        string eventSource = parts[2].Trim();
                        string sensorType = parts[3].Trim();
                        string sensorId = parts[4].Trim();
                        string message = string.Join("|", parts, 5, parts.Length - 6).Trim();
                        string direction = parts[parts.Length - 1].Trim();

                        decodedSelCount++;

                        this.Logger.LogSystemEvent(
                            "SelDecoded",
                            "GenericIpmiUtilSelDecoder",
                            recordId,
                            LogLevel.Information,
                            telemetryContext,
                            null,
                            message,
                            new Dictionary<string, object>
                            {
                                ["eventProvider"] = eventSource,
                                ["eventMessage"] = message,
                                ["eventError"] = null,
                                ["eventResult"] = message,
                                ["severityLevel"] = null,
                                ["eventTimeStamp"] = timestampUtc,
                                ["sensorType"] = sensorType,
                                ["selEventType"] = sensorId,
                                ["selDirection"] = direction,
                                ["selRecordType"] = "SER"
                            });
                    }
                    else if (parts.Length >= 5)
                    {
                        // OEM Timestamped: RecordID | Timestamp | Manufacturer | RecordName | Message
                        string recordId = parts[0].Trim();
                        string timestampUtc = parts[1].Trim();
                        string manufacturer = parts[2].Trim();
                        string recordName = parts[3].Trim();
                        string message = string.Join("|", parts, 4, parts.Length - 4).Trim();

                        decodedSelCount++;

                        this.Logger.LogSystemEvent(
                            "SelDecoded",
                            "GenericIpmiUtilSelDecoder",
                            recordId,
                            LogLevel.Information,
                            telemetryContext,
                            null,
                            message,
                            new Dictionary<string, object>
                            {
                                ["eventProvider"] = manufacturer,
                                ["eventMessage"] = message,
                                ["eventError"] = null,
                                ["eventResult"] = message,
                                ["severityLevel"] = null,
                                ["eventTimeStamp"] = timestampUtc,
                                ["sensorType"] = recordName,
                                ["selEventType"] = null,
                                ["selDirection"] = null,
                                ["selRecordType"] = "OEM_Timestamped"
                            });
                    }
                    else if (parts.Length == 4)
                    {
                        // OEM Non-timestamped: RecordID | Manufacturer | RecordName | Message
                        string recordId = parts[0].Trim();
                        string manufacturer = parts[1].Trim();
                        string recordName = parts[2].Trim();
                        string message = parts[3].Trim();

                        decodedSelCount++;

                        this.Logger.LogSystemEvent(
                            "SelDecoded",
                            "GenericIpmiUtilSelDecoder",
                            recordId,
                            LogLevel.Information,
                            telemetryContext,
                            null,
                            message,
                            new Dictionary<string, object>
                            {
                                ["eventProvider"] = manufacturer,
                                ["eventMessage"] = message,
                                ["eventError"] = null,
                                ["eventResult"] = message,
                                ["severityLevel"] = null,
                                ["eventTimeStamp"] = null,
                                ["sensorType"] = recordName,
                                ["selEventType"] = null,
                                ["selDirection"] = null,
                                ["selRecordType"] = "OEM_NonTimestamped"
                            });
                    }
                    else
                    {
                        // Fallback: fewer than 4 pipe-delimited fields. Surface the whole line
                        // in eventMessage and eventResult so nothing is lost.
                        string recordId = parts.Length > 0 ? parts[0].Trim() : string.Empty;

                        decodedSelCount++;

                        this.Logger.LogSystemEvent(
                            "SelDecoded",
                            "GenericIpmiUtilSelDecoder",
                            recordId,
                            LogLevel.Information,
                            telemetryContext,
                            null,
                            trimmedLine,
                            new Dictionary<string, object>
                            {
                                ["eventProvider"] = null,
                                ["eventMessage"] = trimmedLine,
                                ["eventError"] = null,
                                ["eventResult"] = trimmedLine,
                                ["severityLevel"] = null,
                                ["eventTimeStamp"] = null,
                                ["sensorType"] = null,
                                ["selEventType"] = null,
                                ["selDirection"] = null,
                                ["selRecordType"] = "Unknown"
                            });
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.GenericSelEntryParsingError",
                        LogLevel.Warning,
                        telemetryContext.Clone()
                            .AddContext("line", line)
                            .AddError(ex));
                }
            }

            return decodedSelCount;
        }

        /// <summary>
        /// Represents a parsed SEL record for suspected event analysis.
        /// </summary>
        protected class SelRecord
        {
            /// <summary>
            /// Gets or sets the SEL record ID.
            /// </summary>
            public string RecordId { get; set; }

            /// <summary>
            /// Gets or sets the timestamp of the SEL record.
            /// </summary>
            public string Timestamp { get; set; }

            /// <summary>
            /// Gets or sets the severity level of the SEL record.
            /// </summary>
            public string Severity { get; set; }

            /// <summary>
            /// Gets or sets the source of the SEL record.
            /// </summary>
            public string Source { get; set; }

            /// <summary>
            /// Gets or sets the event type of the SEL record.
            /// </summary>
            public string EventType { get; set; }

            /// <summary>
            /// Gets or sets the sensor that generated the SEL record.
            /// </summary>
            public string Sensor { get; set; }

            /// <summary>
            /// Gets or sets the event detail description.
            /// </summary>
            public string EventDetail { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this is a hardware error.
            /// </summary>
            public bool IsHardwareError { get; set; }
        }
    }
}