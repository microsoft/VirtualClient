# Usage: Command Line Examples
The following documentation covers a range of usage scenarios that apply to the Virtual Client. The sections that follow are meant
to illustrate how to use Virtual Client within these various scenarios as well as what to expect.

## Scenario: Running a Simple Workload
One of the first things that users of the Virtual Client want to do is to run the application to see how it works. This is a very
basic example where we are supplying the minimum number of parameters to the Virtual Client. The '--debug' option instructs the
Virtual Client to output verbose tracing information to the console.

```
# Run a COREMARK workload profile. The workload package itself containing the COREMARK executables will
# be downloaded from the VC workload package store.
#
# On Windows
VirtualClient.exe --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"

# On Linux
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}"
```

## Scenario: Running a Simple Monitor
Virtual Client offers certain profiles which are designed to run monitors in the background. Monitors often run in the background to capture
information and measurements from the system on which the Virtual Client is running. This is useful when running a workload on the system at the
same time. However, monitors can be run alone and without any workloads if desirable.

```
# Run a default monitor profile. The default monitor profile captures performance counters
# from the system.
#
# On Windows
VirtualClient.exe --profile=MONITORS-DEFAULT.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"

# On Linux
./VirtualClient --profile=MONITORS-DEFAULT.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"
```

## Scenario: Running a Client Server Workload
Some workload profiles require multiple systems to operate. For example the CPS, NTttcp and SockPerf workloads requires a client system and a server
system to be valid. Multi-system workload profiles require and environment layout to be supplied to the Virtual Client. An environment
layout describes the topology...where the other Virtual Client instances are and what roles they will play. To get familiar
with defining an environment layouts, see the documentation below. Note that each of the workload profiles has documentation that
provides examples of a valid environment layout for that particular workload profile/workload.

* [Environment Layouts](./0020-client-server.md)

```
# Run the workload using the default port for hosting the REST API
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --layoutPath=C:\any\path\to\layout.json

# Run the workload using a specific port for hosting the REST API
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --api-port=4501 --packages="{BlobStoreConnectionString|SAS URI}" --layoutPath=C:\any\path\to\layout.json

# Run the workload using a specific port for hosting the REST API different for the Client and Server roles
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --api-port=4501/Client,4502/Server --packages="{BlobStoreConnectionString|SAS URI}" --layoutPath=C:\any\path\to\layout.json
```

## Scenario: Pass in Metadata for Correlation
The Virtual Client is designed to be generally agnostic to the nomenclature of the automation/execution system that runs it. However, to ensure that the data emitted by
the application can be correlated with the data captured by the automation system executing it, metadata can be supplied on the command line. Every metadata property emitted
will be included in ALL metrics, counters, logs etc... telemetry that is emitted by the application.

```
VirtualClient.exe --profile=PERF-CPU-GEEKBENCH.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,nodeId=eB3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=73e8ae54-e0a0-48b6-9bda-4a269672b9b1,,,cluster=cluster01,,,region=East US 2"
```

## Scenario: Instruct the Application to Fail Fast
Virtual Client typically will continue to retry the execution of actions within a profile in the event that one of the actions fails
for a non-terminal reason. Users may want to instruct the application to promptly exit on any errors regardless of the severity (terminal or not).
Note that this generally refers to 'Actions' in the profile. The application always fails fast on the failure of 'Dependencies'. 'Monitors' are
typically implemented to handle exceptions due to the requirement they continue to operate in the background even on failure.

```
VirtualClient.exe --profile=PERF-CPU-GEEKBENCH.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --fail-fast
```

## Scenario: Write the Output of Processes to the File System
Virtual Client runs a wide range of workloads, monitors and dependency handlers when executing a given profile. The following examples show
how to instruct the application to log the output of processes to files in the logs directory on the file system.

```
# Log the workload process output to the file system.
#
# On Windows
VirtualClient.exe --profile=PERF-IO-FIO.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --log-to-file

# On Linux
./VirtualClient --profile=PERF-IO-FIO.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --log-to-file
```

## Scenario: Upload Metrics and Logs to an Event Hub
The Virtual Client supports the ability to upload metrics, counters, logs etc... to an [Azure Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/?OCID=AID2200277_SEM_092bba0f3fec11eb8ce6dbef46f6464a:G:s&ef_id=092bba0f3fec11eb8ce6dbef46f6464a:G:s&msclkid=092bba0f3fec11eb8ce6dbef46f6464a).
Event Hubs are a highly-scalable messaging platform in the Azure Cloud that can be integrated out-of-the-box with other big-data platforms such as Azure Data Explorer (ADX/Kusto).
Note that the Virtual Client does have a set of explicit expectations for how the Event Hubs are setup. The following documentation covers what is required:

* [Event Hub Integration](./0610-integration-event-hub.md) 

```
# To send data to an Event Hub, supply a connection string to the Event Hub namespace on the command line.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --eventHub="{EventHubConnectionString}"
```

## Scenario: Uploading Monitoring Information to a Content Store
Certain monitors that exist in the Virtual Client allow the user to upload information or files produced by the monitor (e.g. Azure Profiler .bin files) to
a cloud Blob store. In order to enable this, the connection string or SAS URI to the Blob store should be supplied on the command line. See the documentation
on monitor profiles below for additional details on which profiles support this.

* [Blob Store Support](./0600-integration-blob-storage.md)
* [Monitor Profiles](../monitors/0200-monitor-profiles.md)

```
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --content="{BlobStoreConnectionString|SAS URI}" --parameters=ProfilingEnabled=true,,,ProfilingMode=Interval
```

## Scenario: Running the Azure Profiler in the Background and Uploading .bin Files to a Content Store
The Virtual Client supports the use of the Azure Profiler for capturing profiles on the system. The profiler can be ran in 2 modes: interval-based and on-demand.
The Azure Profiler monitor is a part of the default monitoring profile and can be easily enabled by supplying a few parameters on the command line.
In order to enable file uploads, the connection string or SAS URI to the Blob store should be supplied on the command line. See the documentation
on monitor profiles below for additional details on which profiles support this.

* [Blob Store Support](./0600-integration-blob-storage.md)
* [Monitor Profiles](../monitors/0200-monitor-profiles.md)

```
# Profiling on an interval in the background.
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --content="{BlobStoreConnectionString|SAS URI}" --parameters=ProfilingEnabled=true,,,ProfilingMode=Interval

# Profiling on-demand when signalled by the workload. Note that NOT all profiles support on-demand profiling. 
# Look for a ProfilingMode global parameter in the profile to determine if it is supported.
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --content="{BlobStoreConnectionString|SAS URI}" --parameters=ProfilingEnabled=true,,,ProfilingMode=OnDemand
```
