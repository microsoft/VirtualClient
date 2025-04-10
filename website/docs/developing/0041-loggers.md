# Loggers
The following documentation covers the different package store options available in Virtual Client used for downloading and installing
dependencies on the system. Virtual Client supports NuGet feeds as well as Azure Blob stores for hosting dependency packages that need
to be downloaded to the system during the execution of a workload profile. The following sections describes how this works in the Virtual
Client.


## Console Logger
This logger outputs human-readable information on the console output. This is a default VC logger.

Console out example for a VC execution with downloading a package.
```text
[2/25/2025 12:58:02 PM] Platform.Initialize
[2/25/2025 12:58:02 PM] Experiment ID: c4f27285-8fbc-46de-af4d-b5ca9da627c3
[2/25/2025 12:58:02 PM] Client ID: USER-ABC
[2/25/2025 12:58:02 PM] Log To File: False
[2/25/2025 12:58:02 PM] Log Directory: E:\Source\github\VirtualClient\out\bin\Release\x64\VirtualClient.Main\net8.0\win-x64\logs
[2/25/2025 12:58:02 PM] Package Directory: E:\Source\github\VirtualClient\out\bin\Release\x64\VirtualClient.Main\net8.0\win-x64\packages
[2/25/2025 12:58:02 PM] State Directory: E:\Source\github\VirtualClient\out\bin\Release\x64\VirtualClient.Main\net8.0\win-x64\state
[2/25/2025 12:58:04 PM] Execution Profile: PERF-CPU-GEEKBENCH5
[2/25/2025 12:58:04 PM] Execution Profile: MONITORS-NONE
[2/25/2025 12:58:07 PM] ProfileExecution.Begin
[2/25/2025 12:58:07 PM] Profile: Initialize
[2/25/2025 12:58:07 PM] Profile: Install Dependencies
[2/25/2025 12:58:07 PM] Profile: Dependency = DependencyPackageInstallation (scenario=InstallGeekBench5Package)
[2/25/2025 12:58:07 PM] DependencyPackageInstallation.ExecuteStart
[2/25/2025 12:58:07 PM] DependencyPackageInstallation.ExecuteStop
[2/25/2025 12:58:07 PM] RunProfileCommand.End
[2/25/2025 12:58:07 PM] Exit Code: 0
[2/25/2025 12:58:07 PM] Flush Telemetry
[2/25/2025 12:58:09 PM] Flushed
```

## File Logger
This logger writes machine-readable information on local disk. The log directory typically defaults to `virtualclient\logs`

File logger writes one json file for each type (vc.events, vc.metrics, vc.logs), and one csv file for metrics (metrics.csv).

## EventHub Logger
This logger sends telemetry data to EventHub. EventHub logger is documented in detail at [event-hub.md](../guides/0610-integration-event-hub.md).

An example to add EventHub logger is to specify '--logger=eventhub=eventhubconnectionstring' on command line.

## Summary File Logger
--logger=VirtualClient.Logging.SummaryFileLogger

## Proxy EventHub Logger


## Proxy Storage Debug File Logger