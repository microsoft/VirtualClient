// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Readability", "AZCA1006:PrefixStaticCallsWithClassName Rule", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.QueuedLogger.Log(System.Collections.IDictionary)")]
[assembly: SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.EventWritten(System.Object,System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs)")]
[assembly: SuppressMessage("Readability", "AZCA1006:PrefixStaticCallsWithClassName Rule", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.EventWritten(System.Object,System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs)")]
[assembly: SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.Dispose")]
[assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.Dispose")]
[assembly: SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.Dispose")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.#ctor(VirtualClient.Monitors.Root)")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.#ctor(VirtualClient.Monitors.Root)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.EventLogTraceLogger(System.Diagnostics.Eventing.Reader.EventRecord)~System.Collections.Generic.Dictionary{System.String,System.String}")]
[assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Ignore the error", Scope = "type", Target = "~T:VirtualClient.Monitors.EventLogWatcher")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Ignore the error", Scope = "member", Target = "~F:VirtualClient.Monitors.RunExeProcess.toolDirectory")]
[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Ignore the error", Scope = "type", Target = "~T:VirtualClient.Monitors.ETWRecorder")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.#ctor(VirtualClient.Monitors.Root)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.ImageWatcher.#ctor(VirtualClient.Monitors.Root)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.ImageWatcher.ProcessImageLoadEtwTrace(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ImageLoadTraceData)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.ProcessWatcher.#ctor(VirtualClient.Monitors.Root)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.RegistryWatcher.#ctor(VirtualClient.Monitors.Root)")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.WindowsRealTimeDataCollector.OnSetup(VirtualClient.Monitors.RealTimeDataMonitor)")]
[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Ignore the error", Scope = "type", Target = "~T:VirtualClient.Monitors.EventLogWatcher")]
[assembly: SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Ignore the error", Scope = "type", Target = "~T:VirtualClient.Monitors.WindowsRealTimeDataCollector")]
[assembly: SuppressMessage("Readability", "AZCA1006:PrefixStaticCallsWithClassName Rule", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.QueuedLogger.InitLogger(VirtualClient.Monitors.RealTimeDataCollector)")]
[assembly: SuppressMessage("Readability", "AZCA1006:PrefixStaticCallsWithClassName Rule", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.QueuedLogger.SendTelemetry(System.Collections.IDictionary)")]
[assembly: SuppressMessage("Readability", "AZCA1006:PrefixStaticCallsWithClassName Rule", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.QueuedLogger.InitLogger(VirtualClient.Monitors.WindowsRealTimeDataCollector)")]
[assembly: SuppressMessage("Readability", "AZCA1006:PrefixStaticCallsWithClassName Rule", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.QueuedLogger.OnTelemetryEvent(System.EventArgs)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Ignore the error", Scope = "member", Target = "~M:VirtualClient.Monitors.EventLogWatcher.ConvertRecord(System.Diagnostics.Eventing.Reader.EventRecord)~System.Collections.Generic.Dictionary{System.String,System.Object}")]
