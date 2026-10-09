---
slug: /monitors/windows-etw
---

# Windows Event Tracing
Event Tracing for Windows (ETW) is a Windows facility for collecting events from operating-system and application providers. The
`WindowsETWMonitor` starts an ETW session, enables the configured provider, filters the events when requested, and emits each matching event
through the Virtual Client system-event telemetry channel.

* [Event Tracing for Windows](https://learn.microsoft.com/windows/win32/etw/event-tracing-portal)
* [About Event Tracing](https://learn.microsoft.com/windows/win32/etw/about-event-tracing)

## Supported Platforms
* win-x64
* win-arm64

The monitor requires Windows 8/Windows Server 2012 or later and Virtual Client must run with administrator privileges.

## Provider Types
The monitor supports the following provider types:

* **Kernel** - Enables Windows kernel providers. `ProviderKeywords` is required and `ProviderEvents` can limit collection to specific kernel
  events.
* **NonKernel** - Enables an application or system provider by name or GUID. The provider's dynamic events are collected.

Only one provider is configured per monitor instance. Add multiple monitor entries to a profile to collect from multiple providers.

## Output
Each matching ETW event is emitted as a Virtual Client system event with:

| Field | Value |
|-------|-------|
| Event type | `ETW` |
| Event source | `Windows ETW` |
| Event ID | The ETW event ID, when available |
| Event description | The ETW event name, when available |
| Severity | `Information` |
| Event information | ETW header fields and provider-specific payload fields |

The event information includes standard ETW fields such as `ActivityID`, `ProviderGuid`, `ProviderName`, `EventID`, `Version`, `Level`,
`Task`, `TaskName`, `OpCode`, `OpCodeName`, `Keywords`, `TimeStamp`, `ProcessID`, `ProcessName`, `ProcessorNumber`, `ThreadID`, and
`EventName`. Provider-specific payload fields are added using their ETW payload names.

When `--log-to-file` is specified, the configured Virtual Client logger writes these events to the local logs directory. Other configured
telemetry loggers can consume the same system-event records.

## Parameters
The following parameters are available for the `WindowsETWMonitor` component.

| Parameter | Required | Description | Default value |
|-----------|----------|-------------|---------------|
| ProviderType | Yes | Provider category. Supported values are `Kernel` and `NonKernel`. | |
| Provider | Yes | Provider name or GUID. Use `Windows Kernel` for the kernel provider. | |
| ProviderKeywords | Kernel only | Comma-delimited `KernelTraceEventParser.Keywords` values used to enable kernel events (for example, `ImageLoad,Process`). | |
| ProviderEvents | No | Comma-delimited kernel event names to capture (for example, `ImageLoad,ProcessStart,ProcessStop`). When omitted for a kernel provider, all events enabled by the keywords are captured. | |
| JsonFilters | No | JSON object whose values are string arrays. An event must match every filter key. Key and value comparisons are case-insensitive (for example, `{'EventName':['ProcessStart'],'Level':['4']}`). | |
| ProfilingEnabled | No | Enables or disables collection. | `true` |
| ProfilingMode | No | Collection mode. Supported values are `Interval` and `OnDemand`. | `None` |
| ProfilingPeriod | Interval mode | Length of each collection period. | `00:05:00` |
| ProfilingInterval | Interval mode | Delay between collection periods. | `00:00:30` |
| ProfilingWarmUpPeriod | No | Delay before interval collection begins. On-demand requests supply their own warm-up period. | `00:00:10` in the provided profiles |
| Scenario | No | Name describing the purpose of the monitor in the profile. | |

`JsonFilters` values must be strings, including values that represent numbers. For example:

```json
"JsonFilters": "{'ProcessName':['cmd'],'EventName':['ProcessStart'],'ExitStatus':['259']}"
```

## Usage Examples
The following example captures process and image-load kernel events on an interval:

```json
"Monitors": [
  {
    "Type": "WindowsETWMonitor",
    "Parameters": {
      "Scenario": "CaptureKernelETWTraces",
      "ProfilingEnabled": true,
      "ProfilingMode": "Interval",
      "ProfilingPeriod": "00:05:00",
      "ProfilingInterval": "00:00:30",
      "ProfilingWarmUpPeriod": "00:00:10",
      "ProviderType": "Kernel",
      "Provider": "Windows Kernel",
      "ProviderKeywords": "ImageLoad,Process",
      "ProviderEvents": "ImageLoad,ProcessStart,ProcessStop",
      "JsonFilters": ""
    }
  }
]
```

The following example captures selected .NET runtime events:

```json
"Monitors": [
  {
    "Type": "WindowsETWMonitor",
    "Parameters": {
      "Scenario": "CaptureNonKernelETWTraces",
      "ProfilingEnabled": true,
      "ProfilingMode": "Interval",
      "ProfilingPeriod": "00:05:00",
      "ProfilingInterval": "00:00:30",
      "ProfilingWarmUpPeriod": "00:00:10",
      "ProviderType": "NonKernel",
      "Provider": "Microsoft-Windows-DotNETRuntime",
      "JsonFilters": "{'EventName':['GCSetGCHandle','GCTriggered'],'Level':['4','2']}"
    }
  }
]
```

Virtual Client includes ready-to-use profiles for kernel, non-kernel, and combined collection. See
[Monitor Profiles](/docs/monitors/monitor-profiles#monitors-etwjson).

## Troubleshooting
* **Unauthorized error** - Start Virtual Client from an elevated command prompt or PowerShell session.
* **No events captured** - Confirm the provider name or GUID, keywords, and event names. The provider must emit events while the session is
  active.
* **No events after adding filters** - Confirm each filter key matches an emitted header or payload field and each value is represented as a
  string in an array.
* **Platform-not-supported error** - Run the monitor on Windows 8/Windows Server 2012 or later.
