namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides features for capturing the Windows Event Log for specific events
    /// of interest.
    /// </summary>
    [SupportedPlatforms("win-x64,win-arm64", throwError: false)]
    public class WindowsEventLogMonitor : VirtualClientIntervalBasedMonitor
    {
        internal static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Newtonsoft.Json.Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        // We have to ensure that we do not exceed maximum telemetry message
        // size. We cap each telemetry event towards this purpose.
        private const int MaxEventsPerTelemetryCapture = 50;

        private readonly object lockObject = new object();
        private List<EventLogWatcher> eventWatchers;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientComponent"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public WindowsEventLogMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.eventWatchers = new List<EventLogWatcher>();
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
        /// Get the Event Log log names (e.g. Application,System).
        /// </summary>
        public IEnumerable<string> LogNames
        {
            get
            {
                this.Parameters.TryGetCollection<string>(nameof(WindowsEventLogMonitor.LogNames), out IEnumerable<string> logNames);
                return logNames;
            }
        }

        /// <summary>
        /// Get the filter query to capture the specific logs 
        /// </summary>
        public string Query
        {
            get
            {
                this.Parameters.TryGetValue(nameof(WindowsEventLogMonitor.Query), out IConvertible query);
                return query?.ToString();
            }
        }

        /// <summary>
        /// A queue of events that have been captured to process.
        /// </summary>
        protected BlockingCollection<LogEvent> Queue { get; } = new BlockingCollection<LogEvent>();

        /// <summary>
        /// Converts the XML record into a dictionary form.
        /// </summary>
        protected static Dictionary<string, object> ConvertRecord(XmlDocument eventRecord)
        {
            var eventData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (eventRecord != null && eventRecord.FirstChild != null)
            {
                foreach (XmlNode element in eventRecord.FirstChild.ChildNodes)
                {
                    if (element != null)
                    {
                        if (element.Name == "System")
                        {
                            foreach (XmlNode childElement in element)
                            {
                                // e.g.
                                // <Provider Name="Microsoft-Windows-Security-SPP" Guid="{E23B33B0-C8C9-472C-A5F9-F2BDFEA0F156}" EventSourceName="Software Protection Platform Service" />
                                // <EventID Qualifiers="49152">16394</EventID>
                                string nodeName = childElement.Name;
                                string nodeText = childElement.InnerText;

                                if (!string.IsNullOrEmpty(nodeText))
                                {
                                    // Capture this: <EventID Qualifiers="49152">16394</EventID>
                                    // Not this: <Security />
                                    eventData[nodeName] = nodeText;
                                }

                                if (childElement.Attributes.Count > 0)
                                {
                                    for (int i = 0; i < childElement.Attributes.Count; i++)
                                    {
                                        // e.g.
                                        // <Provider Name="Microsoft-Windows-Security-SPP" Guid="{E23B33B0-C8C9-472C-A5F9-F2BDFEA0F156}" EventSourceName="Software Protection Platform Service" />
                                        //
                                        // Provider_Name, Provider_Guid, Provider_EventSourceName
                                        string attributeName = childElement.Attributes[i].Name;
                                        string attributeValue = childElement.Attributes[i].Value;

                                        eventData[$"{nodeName}_{attributeName}"] = attributeValue?.Replace("{", string.Empty).Replace("}", string.Empty);
                                    }
                                }
                            }
                        }
                        else if (element.Name == "EventData")
                        {
                            foreach (XmlNode childElement in element)
                            {
                                if (childElement.Name == "Data" && childElement.FirstChild != null)
                                {
                                    // e.g.
                                    // <Data Name="AppName">AIMTService.exe</Data>
                                    // <Data Name="ModuleName">AIMTService.exe</Data>
                                    // <Data Name="ModuleVersion">4.5.0.1060</Data>
                                    string nodeValue = childElement.FirstChild.Value;
                                    if (!string.IsNullOrWhiteSpace(nodeValue))
                                    {
                                        if (childElement.Attributes.Count > 0)
                                        {
                                            for (int i = 0; i < childElement.Attributes.Count; i++)
                                            {
                                                if (childElement.Attributes[i].Name == "Name")
                                                {
                                                    string attributeValue = childElement.Attributes[i].Value;
                                                    eventData[$"{childElement.Name}_{attributeValue}"] = nodeValue?.Replace("{", string.Empty).Replace("}", string.Empty);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return eventData;
        }

        /// <summary>
        /// Begins watching for events in the Event Log on the system.
        /// </summary>
        protected virtual void BeginWatchingEvents()
        {
            lock (this.lockObject)
            {
                string query = this.GetEventQuery();
                foreach (string logName in this.LogNames)
                {
                    var eventWatcher = new EventLogWatcher(new EventLogQuery(logName, PathType.LogName, query));
                    this.eventWatchers.Add(eventWatcher);
                    eventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(this.EventWritten);
                    eventWatcher.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (!this.disposed)
                {
                    this.Queue.Dispose();
                    this.disposed = true;

                    if (this.eventWatchers.Any())
                    {
                        foreach (EventLogWatcher watcher in this.eventWatchers)
                        {
                            watcher.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Executes the background monitoring process..
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitors should return a background Task immediately to avoid blocking
            // the thread.

            return Task.Run(async () =>
            {
                try
                {
                    await this.ExecuteMonitoringAsync(telemetryContext, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when a Task.Delay is cancelled.
                }
                catch (Exception exc)
                {
                    // Do not allow the monitor to crash the VC process.
                    this.Logger.LogErrorMessage(exc, EventContext.Persisted());
                }
            });
        }

        /// <summary>
        /// Helper function that executes background monitoring process (see ExecuteAsync).
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task ExecuteMonitoringAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                await this.WaitAsync(this.MonitorWarmupPeriod, cancellationToken);
                this.BeginWatchingEvents();

                long iterations = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (this.IsIterationComplete(iterations))
                        {
                            break;
                        }

                        await this.ProcessEventsAsync(telemetryContext.Clone(), cancellationToken);
                    }
                    catch (InvalidOperationException)
                    {
                        // An InvalidOperationException means that Take() was called on a completed collection
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogErrorMessage(exc, telemetryContext.Clone());
                    }
                    finally
                    {
                        iterations++;
                        await this.WaitAsync(this.MonitorFrequency, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when a Task.Delay is cancelled.
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, EventContext.Persisted());
            }
            finally
            {
                this.StopWatchingEvents();
            }

            // Process any remaining events on the tail-end
            await this.ProcessEventsAsync(telemetryContext.Clone(), cancellationToken);
        }

        /// <summary>
        /// Returns the effective query to use when monitoring Event Log events.
        /// </summary>
        protected virtual string GetEventQuery()
        {
            string query = null;
            if (!string.IsNullOrWhiteSpace(this.Query))
            {
                query = this.Query;
            }
            else
            {
                // Default = Warning level
                int logLevel = 3;
                switch (this.LogLevel)
                {
                    case LogLevel.Debug:
                    case LogLevel.Trace:
                        logLevel = 5;
                        break;

                    case LogLevel.Information:
                        logLevel = 4;
                        break;

                    case LogLevel.Error:
                        logLevel = 2;
                        break;

                    case LogLevel.Critical:
                        logLevel = 1;
                        break;
                }

                // Example Event.
                // The query is an XPath formatted text.
                //
                // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
                //   <System>
                //     <Provider Name="Microsoft-Windows-Search" Guid="{CA4E628D-8567-4896-AB6B-835B221F373F}" EventSourceName="Windows Search Service" />
                //     <EventID Qualifiers="32768">10024</EventID>
                //     <Version>0</Version>
                //     <Level>3</Level>
                //     <Task>3</Task>
                //     <Opcode>0</Opcode>
                //     <Keywords>0x80000000000000</Keywords>
                //     <TimeCreated SystemTime="2025-03-28T16:13:09.8269843Z" />
                //     <EventRecordID>32120</EventRecordID>
                //     <Correlation />
                //     <Execution ProcessID="30888" ThreadID="0" />
                //     <Channel>Application</Channel>
                //     <Computer>computer01</Computer>
                //     <Security />
                //   </System>
                //   <EventData>
                //     <Data Name="ExtraInfo">
                //     </Data>
                //     <Data Name="FilterHostProcessID">23576</Data>
                //   </EventData>
                // </Event>

                query = $"*[System/Level <= {logLevel}]";
            }

            return query;
        }

        /// <summary>
        /// Processes the events in the queue.
        /// </summary>
        protected virtual async Task ProcessEventsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Queue.Count > 0)
            {
                var eventsToLog = new List<LogEvent>();
                while (this.Queue.TryTake(out LogEvent eventRecord, 0, cancellationToken))
                {
                    eventsToLog.Add(eventRecord);
                }

                if (eventsToLog.Any())
                {
                    string query = this.GetEventQuery();

                    // We log JSON format to telemetry.
                    var eventsGroupedByChannel = eventsToLog.GroupBy(evt => evt.LogName);

                    if (this.LogToFile)
                    {
                        foreach (var channelGroup in eventsGroupedByChannel)
                        {
                            List<string> logOutputResults = new List<string>();
                            foreach (LogEvent channelEvent in channelGroup)
                            {
                                try
                                {
                                    logOutputResults.Add(channelEvent.ToDictionary().ToJson(WindowsEventLogMonitor.SerializationSettings));
                                }
                                catch (Exception exc)
                                {
                                    // Handle XML serialization issues.
                                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                                }
                            }

                            // We log the original XML to log file.
                            await this.LogProcessDetailsAsync(
                                new ProcessDetails
                                {
                                    CommandLine = $"LogName = {channelGroup.Key}, Query = {query}",
                                    Results = logOutputResults,
                                    ToolName = $"EventLog_{channelGroup.Key}",
                                },
                                telemetryContext,
                                logToTelemetry: false);
                        }
                    }

                    foreach (var channelGroup in eventsGroupedByChannel)
                    {
                        int currentEventNumber = 0;
                        int channelGroupCount = channelGroup.Count();

                        var currentEventSet = new List<IDictionary<string, object>>();
                        foreach (LogEvent channelEvent in channelGroup)
                        {
                            currentEventNumber++;
                            IDictionary<string, object> systemEvent = channelEvent.ToDictionary();

                            if (systemEvent?.Any() == true)
                            {
                                currentEventSet.Add(systemEvent);
                            }

                            if (currentEventSet.Count > WindowsEventLogMonitor.MaxEventsPerTelemetryCapture || currentEventNumber >= channelGroupCount)
                            {
                                this.Logger.LogSystemEvent(
                                    "EventLog",
                                    "Windows Event Log",
                                    channelGroup.Key,
                                    LogLevel.Information,
                                    telemetryContext,
                                    null,
                                    $"Events captured from the Windows '{channelGroup.Key}' channel/log.",
                                    new Dictionary<string, object>
                                    {
                                        ["events"] = currentEventSet
                                    });

                                currentEventSet.Clear();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops watching for events in the Event Log on the system.
        /// </summary>
        protected virtual void StopWatchingEvents()
        {
            lock (this.lockObject)
            {
                if (this.eventWatchers.Any())
                {
                    foreach (EventLogWatcher watcher in this.eventWatchers)
                    {
                        watcher.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// validate parameters
        /// </summary>
        protected override void Validate()
        {
            if (this.LogNames?.Any() != true)
            {
                throw new MonitorException(
                    $"Unexpected profile definition. One or more of the parameters in the profile does not contain the " +
                    $"required '{nameof(WindowsEventLogMonitor.LogNames)}' parameter definition. This parameter defines " +
                    $"1 or more Event Log names to monitor for events delimited with a comma (e.g. Application,System).",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        /// <summary>
        /// Event logs are captured each time an event is raised.
        /// </summary>
        private void EventWritten(object obj, EventRecordWrittenEventArgs args)
        {
            try
            {
                // Make sure there was no error reading the event.
                if (args?.EventRecord?.LogName != null)
                {
                    XmlDocument eventRecord = new XmlDocument();
                    eventRecord.LoadXml(args.EventRecord.ToXml());
                    this.Queue.TryAdd(new LogEvent(args.EventRecord.LogName, args.EventRecord.FormatDescription(), eventRecord));
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, EventContext.Persisted(), LogLevel.Warning);
            }
        }

        /// <summary>
        /// Represents a single event captured from the Windows Event Log.
        /// </summary>
        public class LogEvent
        {
            private IDictionary<string, object> convertedEvent;

            /// <summary>
            /// Initializes a new instance of the <see cref="LogEvent"/> class.
            /// </summary>
            public LogEvent(string logName, string description, XmlDocument eventInfo)
            {
                this.LogName = logName;
                this.Description = description;
                this.EventInfo = eventInfo;
            }

            /// <summary>
            /// A description of the event.
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// The full event information.
            /// </summary>
            public XmlDocument EventInfo { get; }

            /// <summary>
            /// The Event Log channel (e.g. Application, System).
            /// </summary>
            public string LogName { get; }

            /// <summary>
            /// Converts the event XML into dictionary form.
            /// </summary>
            /// <returns></returns>
            public IDictionary<string, object> ToDictionary()
            {
                if (this.convertedEvent == null)
                {
                    this.convertedEvent = WindowsEventLogMonitor.ConvertRecord(this.EventInfo);
                    if (this.convertedEvent?.Any() == true)
                    {
                        this.convertedEvent["Description"] = this.Description;
                    }
                }

                return this.convertedEvent;
            }
        }
    }
}