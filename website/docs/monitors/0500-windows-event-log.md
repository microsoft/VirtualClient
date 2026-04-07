# Windows Event Log
The Windows Event Log is a kernel layer facility based on Windows Event Tracing (ETW). This facility allows applications a central store
on the operating system to log important events and can be a valuable resource for debugging.

* [Windows Event Viewer](https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2008-R2-and-2008/cc766042(v=ws.11))
* [Windows Event Tracing](https://learn.microsoft.com/en-us/windows/win32/etw/event-tracing-portal)  


## Supported Platforms 
  * win-arm64
  * win-x64

## Windows Event Log Output Description
The following section describes the type of information emitted by the Windows Event Log monitoring facility in Virtual Client. Virtual Client utilizes
the system event logging facility to capture events from the Windows Event Log. The section below shows examples of the output.

``` json
{
	"eventType": "EventLog",
	"eventInfo": {
		"eventCode": -1,
		"eventDescription": "Events captured from the Windows 'System' channel/log.",
		"eventId": "System",
		"eventSource": "Windows Event Log",
		"events": [
			{
				"channel": "System",
				"computer": "demo_system",
				"description": "The driver Driver WUDFRd failed to load. Device: ACPI /AMDI0080/1 Status: 0xC0000365",
				"eventID": "219",
				"eventID_Qualifiers": "16384",
				"eventRecordID": "63852",
				"execution_ProcessID": "56868",
				"execution_ThreadID": "0",
				"keywords": "0x8000000000002000",
				"level": "3",
				"opcode": "0",
				"provider_Name": "Microsoft-Windows-Kernel-PnP",
				"task": "0",
				"timeCreated_SystemTime": "2025-04-08T21:46:06.1652638Z",
				"version": "0",
				"provider_EventSourceName": "Kernel-PnP",
				"provider_Guid": "9c205a39-1250-487d-abd7-e831c6290539"
			}
		]
	}
}

{
	"eventType": "EventLog",
	"eventInfo": {
		"eventCode": -1,
		"eventDescription": "Events captured from the Windows 'Application' channel/log.",
		"eventId": "Application",
		"eventSource": "Windows Event Log",
		"events": [
			{
				"channel": "Application",
				"computer": "demo_system",
				"description": "CoId={0F5671E9-BAD1-49DE-BAA1-7BD6BB4B6755}: The user SYSTEM has started dialing a Connection Manager connection using a per-user connection profile named vctest-vnet. The connection settings are:...",
				"eventID": "20221",
				"eventID_Qualifiers": "0",
				"eventRecordID": "35360",
				"execution_ProcessID": "9456",
				"execution_ThreadID": "0",
				"keywords": "0x80000000000000",
				"level": "4",
				"opcode": "0",
				"provider_Name": "RasClient",
				"task": "0",
				"timeCreated_SystemTime": "2025-04-08T21:47:08.6267674Z",
				"version": "0"
			},
			{
				"channel": "Application",
				"computer": "demo_system",
				"description": "Offline downlevel migration succeeded.",
				"eventID": "16384",
				"eventID_Qualifiers": "16384",
				"eventRecordID": "35359",
				"execution_ProcessID": "56868",
				"execution_ThreadID": "0",
				"keywords": "0x80000000000000",
				"level": "4",
				"opcode": "0",
				"provider_Name": "Microsoft-Windows-Security-SPP",
				"task": "0",
				"timeCreated_SystemTime": "2025-04-08T21:46:06.1652638Z",
				"version": "0",
				"provider_EventSourceName": "Software Protection Platform Service",
				"provider_Guid": "E23B33B0-C8C9-472C-A5F9-F2BDFEA0F156"
			}
		]
	}
}
```

## Profile Parameters
The following parameters are available for the WindowsEventLogMonitor component.

| Parameter           | Required | Description | Default Value |
|---------------------|----------|-------------|---------------|
| LogNames            | Yes      | 1 or more names of event logs/channels to watch for events delimited by a comma (e.g. Application,System). | |
| LogLevel            | No       | The minimum logging level for events to capture. Valid values include: Trace, Debug, Information, Warning, Error, Critical. | Warning |
| Query               | No       | An event log filter/query that defines the context of the events to capture (e.g. *[System[Level \<= 5]]). This can be used as an alternative to the 'LogLevel' parameter for more granular event watcher definitions. | |
| MonitorFrequency    | No       | The interval/frequency to capture events from the system. | 00:05:00 (5 mins) |
| MonitorWarmupPeriod | No       | The period of time to wait before beginning to capture events from the system. | 00:05:00 (5 mins) |
| Scenario            | No       | A name defining the purpose of the component. | |

### Usage Examples
The following section illustrates how to include Windows Event Log monitoring in your profiles.

```json
"Monitors": [
	{
		"Type": "WindowsEventLogMonitor",
		"Parameters": {
			"Scenario": "CaptureEventLogs",
			"LogNames": "Application,Security,System",
			"LogLevel": "Warning",
			"MonitorFrequency": "00:10:00",
			"MonitorWarmupPeriod": "00:00:30"
		}
	}
]

"Monitors": [
	{
		"Type": "WindowsEventLogMonitor",
		"Parameters": {
			"Scenario": "CaptureEventLogs",
			"LogNames": "Application,Security,System",
			"LogLevel": "Error",
			"MonitorFrequency": "00:10:00",
			"MonitorWarmupPeriod": "00:00:30"
		}
	}
]

"Monitors": [
	{
		"Type": "WindowsEventLogMonitor",
		"Parameters": {
			"Scenario": "CaptureEventLogs",
			"LogNames": "Application,Security,System",
			"Query": "*[System[Level <= 5]]",
			"MonitorFrequency": "00:10:00",
			"MonitorWarmupPeriod": "00:00:30"
		}
	}
]

"Monitors": [
	{
		"Type": "WindowsEventLogMonitor",
		"Parameters": {
			"Scenario": "CaptureEventLogs",
			"LogNames": "Application",
			"Query": "*[System[Provider[@Name='Application Error']] and System[Level <= 5]]",
			"MonitorFrequency": "00:10:00",
			"MonitorWarmupPeriod": "00:00:30"
		}
	}
]
```