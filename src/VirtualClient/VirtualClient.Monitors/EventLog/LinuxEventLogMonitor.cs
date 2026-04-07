// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Monitors for different types of system logs on the system and emits the
    /// data as event telemetry.
    /// </summary>
    /// <remarks>
    /// https://linuxhandbook.com/journalctl-command/
    /// </remarks>
    [SupportedPlatforms("linux-arm64,linux-x64", throwError: false)]
    public class LinuxEventLogMonitor : VirtualClientIntervalBasedMonitor
    {
        // We have to ensure that we do not exceed maximum telemetry message
        // size. We cap each telemetry event towards this purpose.
        private const int MaxEventsPerTelemetryCapture = 50;

        // Unix epoch starts at 1970-01-01T00:00:00Z
        private static readonly DateTimeOffset EpochStart = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxEventLogMonitor"/> class.
        /// </summary>
        public LinuxEventLogMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            // MUST be in local time.
            this.LastCheckPoint = DateTime.Now;
        }

        /// <summary>
        /// The minimum severity log level for which the events are logged.
        /// </summary>
        public LogLevel LogLevel
        {
            get
            {
                return this.Parameters.GetEnumValue<LogLevel>(nameof(this.LogLevel), LogLevel.Warning);
            }
        }

        /// <summary>
        /// Tracks the last time the system event log was checked
        /// </summary>
        protected DateTime LastCheckPoint { get; set; }

        /// <summary>
        /// Converts the JSON record into a dictionary form.
        /// </summary>
        protected static Dictionary<string, object> ConvertRecord(JObject eventRecord)
        {
            var eventData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (JToken element in eventRecord.Children())
            {
                JProperty property = element as JProperty;
                if (property != null)
                {
                    string name = property.Name;
                    if (!name.StartsWith("_"))
                    {
                        // The JSON serializers will modify the casing of the properties
                        // if they start with letters. The underscore ensures the property
                        // remains cased exactly as-is.
                        name = $"_{name}";
                    }

                    string value = property.Value?.ToString().Trim();
                    if (long.TryParse(value, out long numericValue))
                    {
                        eventData[name] = numericValue;
                    }
                    else
                    {
                        eventData[name] = value;
                    }
                }
            }

            if (eventData.TryGetValue("__REALTIME_TIMESTAMP", out object microsecondsSinceEpoch))
            {
                try
                {
                    // microseconds to ticks
                    DateTime timestamp = LinuxEventLogMonitor.EpochStart.AddTicks((long)microsecondsSinceEpoch * 10).DateTime.ToUniversalTime();
                    eventData["_TIMESTAMP"] = timestamp.ToString("o");
                }
                catch
                {
                    // Best effort
                }
            }

            return eventData;
        }

        /// <summary>
        /// Executes the monitoring operations.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            return Task.Run(async () =>
            {
                try
                {
                    await this.WaitAsync(this.MonitorWarmupPeriod, cancellationToken);

                    long iterations = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        EventContext relatedContext = telemetryContext.Clone();

                        try
                        {
                            if (this.IsIterationComplete(iterations))
                            {
                                break;
                            }

                            relatedContext
                                .AddContext("logLevel", this.LogLevel)
                                .AddContext("lastCheckPointTime", this.LastCheckPoint)
                                .AddContext("iteration", iterations);

                            await this.ProcessEventsAsync(this.LastCheckPoint, relatedContext, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected with Task.Delay on cancellations.
                        }
                        catch (Exception exc)
                        {
                            this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                        }
                        finally
                        {
                            iterations++;

                            // MUST be in local time.
                            this.LastCheckPoint = DateTime.Now;
                            await this.WaitAsync(this.MonitorFrequency, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Do nothing
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            });
        }

        /// <summary>
        /// Processes events in the system event log.
        /// </summary>
        protected virtual async Task ProcessEventsAsync(DateTime lastCheckPoint, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int minLogLevel = this.GetMinimumLogLevel(this.LogLevel);
            string command = "journalctl";
            string commandArguments = 
                $"--since=\"{lastCheckPoint.ToString("yyyy-MM-dd HH:mm:ss")}\" " +
                $"--priority={minLogLevel} " +
                $"--output=json-pretty " +
                $"--utc";

            using (IProcessProxy journalCtl = await this.ExecuteCommandAsync(command, commandArguments, null, telemetryContext, cancellationToken, runElevated: true))
            {
                await this.LogProcessDetailsAsync(journalCtl, telemetryContext, "EventLog");

                if (journalCtl.StandardOutput.Length > 0)
                {
                    MatchCollection matches = Regex.Matches(journalCtl.StandardOutput.ToString(), @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}");

                    if (matches?.Any() == true)
                    {
                        List<IDictionary<string, object>> currentEventSet = new List<IDictionary<string, object>>();
                        int currentEventNumber = 0;

                        foreach (Match match in matches)
                        {
                            try
                            {
                                currentEventNumber++;
                                JObject sysEvent = JObject.Parse(match.Value);
                                IDictionary<string, object> systemEvent = LinuxEventLogMonitor.ConvertRecord(sysEvent);

                                if (systemEvent?.Any() == true)
                                {
                                    currentEventSet.Add(systemEvent);
                                }

                                if (currentEventSet.Count > LinuxEventLogMonitor.MaxEventsPerTelemetryCapture || currentEventNumber >= matches.Count)
                                {
                                    this.Logger.LogSystemEvent(
                                        "EventLog",
                                        "Linux Event Log",
                                        "journalctl",
                                        this.LogLevel,
                                        telemetryContext,
                                        null,
                                        null,
                                        new Dictionary<string, object>
                                        {
                                            ["lastCheckPoint"] = lastCheckPoint,
                                            ["level"] = this.LogLevel,
                                            ["events"] = currentEventSet
                                        });
                                }
                            }
                            catch
                            {
                                // Best effort.
                            }
                        }
                    }
                }
            }
        }

        private int GetMinimumLogLevel(LogLevel level)
        {
            // Reference:
            // https://linuxhandbook.com/journalctl-command/
            //
            // 0 = emergency
            // 1 = alert
            // 2 = critical
            // 3 = error
            // 4 = warning
            // 5 = notice
            // 6 = information
            // 7 = debug

            int minLogLevel = 4;
            switch (this.LogLevel)
            {
                case LogLevel.Critical:
                    minLogLevel = 2;
                    break;

                case LogLevel.Error:
                    minLogLevel = 3;
                    break;

                case LogLevel.Information:
                case LogLevel.Debug:
                    minLogLevel = 6;
                    break;

                case LogLevel.Trace:
                    minLogLevel = 7;
                    break;
            }

            return minLogLevel;
        }
    }
}