# Linux System Log
The Linux System Log is a kernel layer facility that can be accessed on Linux using the journalctl command. This facility allows applications a central store
on the operating system to log important events and can be a valuable resource for debugging.

* [Guide to journalctl](https://linuxhandbook.com/journalctl-command/)
* [journalctl documentation](https://www.man7.org/linux/man-pages/man1/journalctl.1.html)

## Dependency
Most recent Linux operating system distros have the System log facility out-of-box.

## Supported Platforms
* linux-arm64
* linux-x64

## Linux Event/System Log Output Description
The following section describes the type of information emitted by the Linux Event/System Log monitoring facility in Virtual Client. Virtual Client utilizes
the **journalctl** command to get this information. The section below shows examples of the output.

``` json
{
	"eventType": EventLog,
	"eventInfo": {
		"eventId": "journalctl",
		"eventSource": "Linux Event Log",
		"lastCheckPoint": "2025-04-09T19:57:34.7508937Z",
		"level": 0,
		"events": [
			{
				"_AUDIT_LOGINUID": 1000,
				"_AUDIT_SESSION": 1,
				"_BOOT_ID": "1dcc4995b7b349dc90454fd461970aeb",
				"_CAP_EFFECTIVE": "1ffffffffff",
				"_CMDLINE": "sudo dmidecode --type memory",
				"_COMM": "sudo",
				"_EXE": "/usr/bin/sudo",
				"_GID": 1000,
				"_HOSTNAME": "demo-vm01",
				"_MACHINE_ID": "743691b0fc204defb0bcf6f1e9824f3a",
				"_MESSAGE": "junovmadmin : TTY=pts/0 ; PWD=/home/junovmadmin/virtualclient/linux-arm64 ; USER=root ; COMMAND=/usr/sbin/dmidecode --type memory",
				"_PID": 2232,
				"_PRIORITY": 5,
				"_SELINUX_CONTEXT": "unconfined",
				"_SOURCE_REALTIME_TIMESTAMP": "1744228654591576",
				"_SYSLOG_FACILITY": 10,
				"_SYSLOG_IDENTIFIER": "sudo",
				"_SYSLOG_TIMESTAMP": "Apr  9 19:57:34"
				"_SYSTEMD_CGROUP": "/user.slice/user-1000.slice/session-1.scope",
				"_SYSTEMD_INVOCATION_ID": "59d8cae2570f4a44845e7317318bada9",
				"_SYSTEMD_OWNER_UID": 1000,
				"_SYSTEMD_SESSION": 1,
				"_SYSTEMD_SLICE": "user-1000.slice",
				"_SYSTEMD_UNIT": "session-1.scope",
				"_SYSTEMD_USER_SLICE": "-.slice",
				"_TIMESTAMP": "2025-04-09T19:57:34.5916020Z",
				"_TRANSPORT": "syslog",
				"_UID": 1000,
				"__CURSOR": "s=a353fda1ecc549628c93d8d4f2a59e91;i=c550;b=1dcc4995b7b349dc90454fd461970aeb;m=112557b82;t=6325de15d2e72;x=4f74d28ecf2309b1",
				"__MONOTONIC_TIMESTAMP": 4602559362,
				"__REALTIME_TIMESTAMP": "1744228654591602"
			},
			{
				"_AUDIT_LOGINUID": 1000,
				"_AUDIT_SESSION": 1,
				"_BOOT_ID": "1dcc4995b7b349dc90454fd461970aeb",
				"_CAP_EFFECTIVE": "1ffffffffff",
				"_CMDLINE": "sudo dmidecode --type memory",
				"_COMM": "sudo",
				"_EXE": "/usr/bin/sudo",
				"_GID": 0,
				"_HOSTNAME": "demo-vm01",
				"_MACHINE_ID": "743691b0fc204defb0bcf6f1e9824f3a",
				"_MESSAGE": "pam_unix(sudo:session): session opened for user root(uid=0) by junovmadmin(uid=1000)",
				"_PID": 2232,
				"_PRIORITY": 6,
				"_SELINUX_CONTEXT": "unconfined",
				"_SOURCE_REALTIME_TIMESTAMP": "1744228654592034",
				"_SYSLOG_FACILITY": 10,
				"_SYSLOG_IDENTIFIER": "sudo",
				"_SYSLOG_TIMESTAMP": "Apr  9 19:57:34",
				"_SYSTEMD_CGROUP": "/user.slice/user-1000.slice/session-1.scope",
				"_SYSTEMD_INVOCATION_ID": "59d8cae2570f4a44845e7317318bada9",
				"_SYSTEMD_OWNER_UID": 1000,
				"_SYSTEMD_SESSION": 1,
				"_SYSTEMD_SLICE": "user-1000.slice",
				"_SYSTEMD_UNIT": "session-1.scope",
				"_SYSTEMD_USER_SLICE": "-.slice",
				"_TIMESTAMP": "2025-04-09T19:57:34.5920530Z",
				"_TRANSPORT": "syslog",
				"_UID": 1000,
				"__CURSOR": "s=a353fda1ecc549628c93d8d4f2a59e91;i=c551;b=1dcc4995b7b349dc90454fd461970aeb;m=112557d45;t=6325de15d3035;x=7c1acb486f6eb6f8",
				"__MONOTONIC_TIMESTAMP": 4602559813,
				"__REALTIME_TIMESTAMP": "1744228654592053"
			},
			{
				"_AUDIT_LOGINUID": 1000,
				"_AUDIT_SESSION": 1,
				"_BOOT_ID": "1dcc4995b7b349dc90454fd461970aeb",
				"_CAP_EFFECTIVE": "1ffffffffff",
				"_CMDLINE": "sudo dmidecode --type memory",
				"_COMM": "sudo",
				"_EXE": "/usr/bin/sudo",
				"_GID": 0,
				"_HOSTNAME": "demo-vm01",
				"_MACHINE_ID": "743691b0fc204defb0bcf6f1e9824f3a",
				"_MESSAGE": "pam_unix(sudo:session): session closed for user root",
				"_PID": 2232,
				"_PRIORITY": 6,
				"_SELINUX_CONTEXT": "unconfined",
				"_SOURCE_REALTIME_TIMESTAMP": "1744228654593809",
				"_SYSLOG_FACILITY": 10,
				"_SYSLOG_IDENTIFIER": "sudo",
				"_SYSLOG_TIMESTAMP": "Apr  9 19:57:34",
				"_SYSTEMD_CGROUP": "/user.slice/user-1000.slice/session-1.scope",
				"_SYSTEMD_INVOCATION_ID": "59d8cae2570f4a44845e7317318bada9",
				"_SYSTEMD_OWNER_UID": 1000,
				"_SYSTEMD_SESSION": 1,
				"_SYSTEMD_SLICE": "user-1000.slice",
				"_SYSTEMD_UNIT": "session-1.scope",
				"_SYSTEMD_USER_SLICE": "-.slice",
				"_TIMESTAMP": "2025-04-09T19:57:34.5938200Z",
				"_TRANSPORT": "syslog",
				"_UID": 1000,
				"__CURSOR": "s=a353fda1ecc549628c93d8d4f2a59e91;i=c552;b=1dcc4995b7b349dc90454fd461970aeb;m=11255842c;t=6325de15d371c;x=788d80d7f120e56c",
				"__MONOTONIC_TIMESTAMP": 4602561580,
				"__REALTIME_TIMESTAMP": "1744228654593820"
			}
		]
	}
}
```

## Profile Parameters
The following parameters are available for the LinuxEventLogMonitor component.

| Parameter           | Required | Description | Default Value |
|---------------------|----------|-------------|---------------|
| LogLevel            | No       | The minimum logging level for events to capture. Valid values include: Trace, Debug, Information, Warning, Error, Critical. | Warning |
| MonitorFrequency    | No       | The interval/frequency to capture events from the system. | 00:05:00 (5 mins) |
| MonitorWarmupPeriod | No       | The period of time to wait before beginning to capture events from the system. | 00:05:00 (5 mins) |
| Scenario            | No       | A name defining the purpose of the component. | |


### Usage Examples
The following section illustrates how to include Windows Event Log monitoring in your profiles.

```json
"Monitors": [
	{
		"Type": "LinuxEventLogMonitor",
		"Parameters": {
			"Scenario": "CaptureEventLogs",
			"LogLevel": "Warning",
			"MonitorFrequency": "00:10:00",
			"MonitorWarmupPeriod": "00:00:30"
		}
	}
]

"Monitors": [
	{
		"Type": "LinuxEventLogMonitor",
		"Parameters": {
			"Scenario": "CaptureEventLogs",
			"LogLevel": "Error",
			"MonitorFrequency": "00:10:00",
			"MonitorWarmupPeriod": "00:00:30"
		}
	}
]
```