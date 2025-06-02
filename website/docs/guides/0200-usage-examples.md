# Usage: Command Line Examples
The following documentation covers a range of usage scenarios that apply to the Virtual Client. The sections that follow are meant
to illustrate how to use Virtual Client within these various scenarios as well as what to expect.

## Scenario: Running a Simple Workload
One of the first things that users of the Virtual Client want to do is to run the application to see how it works. This is a very
basic example where we are supplying the minimum number of parameters to the Virtual Client. The '--debug' option instructs the
Virtual Client to output verbose tracing information to the console.

``` bash
# Run a COREMARK workload profile. The workload package itself containing the COREMARK executables will
# be downloaded from the VC workload package store.
#
# On Windows
VirtualClient.exe --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"

# On Linux
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}"

# Run the workload profile for a single iteration.
./VirtualClient --profile=PERF-CPU-COREMARK.json --packages="{BlobStoreConnectionString|SAS URI}"
```

## Scenario: Running a Client Server Workload
Some workload profiles require multiple systems to operate. For example the CPS, NTttcp and SockPerf workloads requires a client system and a server
system to be valid. Multi-system workload profiles require and environment layout to be supplied to the Virtual Client. An environment
layout describes the topology...where the other Virtual Client instances are and what roles they will play. To get familiar
with defining an environment layouts, see the documentation below. Note that each of the workload profiles has documentation that
provides examples of a valid environment layout for that particular workload profile/workload.

* [Environment Layouts](./0020-client-server.md)

``` bash
# Run the workload using the default port for hosting the REST API
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --layout-path=C:\any\path\to\layout.json

# Run the workload using a specific port for hosting the REST API
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --api-port=4501 --packages="{BlobStoreConnectionString|SAS URI}" --layout-path=C:\any\path\to\layout.json

# Run the workload using a specific port for hosting the REST API different for the Client and Server roles
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --api-port=4501/Client,4502/Server --packages="{BlobStoreConnectionString|SAS URI}" --layout-path=C:\any\path\to\layout.json
```

## Scenario: Pass in Metadata for Correlation
The Virtual Client is designed to be generally agnostic to the nomenclature of the automation/execution system that runs it. However, to ensure that the data emitted by
the application can be correlated with the data captured by the automation system executing it, metadata can be supplied on the command line. Every metadata property emitted
will be included in ALL metrics, counters, logs etc... telemetry that is emitted by the application.

``` bash
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,nodeId=eB3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=73e8ae54-e0a0-48b6-9bda-4a269672b9b1,,,cluster=cluster01,,,region=East US 2"
```

## Scenario: Instruct the Application to Fail Fast
Virtual Client typically will continue to retry the execution of actions within a profile in the event that one of the actions fails
for a non-terminal reason. Users may want to instruct the application to promptly exit on any errors regardless of the severity (terminal or not).
Note that this generally refers to 'Actions' in the profile. The application always fails fast on the failure of 'Dependencies'. 'Monitors' are
typically implemented to handle exceptions due to the requirement they continue to operate in the background even on failure.

``` bash
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --fail-fast
```

## Scenario: Write the Output of Processes to the File System
Virtual Client runs a wide range of workloads, monitors and dependency handlers when executing a given profile. The following examples show
how to instruct the application to log the output of processes to files in the logs directory on the file system.

``` bash
# Log the workload process output to the file system.
#
# On Windows
VirtualClient.exe --profile=PERF-IO-FIO.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --log-to-file

# On Linux
./VirtualClient --profile=PERF-IO-FIO.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --log-to-file
```

## Scenario: Instruct the Appliction to Perform an Initial Cleanup
Virtual Client writes various types of content to the file system. Some common types of content include log files, package downloads and
files used to represent state for managing repeat operations/idempotency. Over time the count and size of the file content on the file
system can grow to where it becomes desirable to cleanup some of the files. For example, a user might want to cleanup up the log files
in order to minimize the overall size of the log content on the file system (...no one wants to run a drive out of space). Additionally,
a user might want to perform a "reset" to force Virtual Client to treat a given profile as a "first run" again. In this scenario, the
user would want to cleanup the local state files. The following examples show how to perform an initial cleanup on the system.

``` bash
# Perform a full clean. This will remove ALL log files/directories, any packages previously downloaded minus
# those that are "built-in" or part of the Virtual Client package itself and any state files previously written.
# This essentially resets Virtual Client back to the state it was in before the first run on the system.
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean=all

# Clean specific target resources.
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean=logs
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean=packages
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean=state

# Clean multiple specific target resources all together.
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean=logs,state
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --clean=logs,packages,state

# Apply a log retention period to the log files. This will cause log files older than the period to
# be removed but will preserve any remaining. Note that this is the same as --clean=logs --log-retention=02.00:00:00.
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --log-retention=02.00:00:00

# Log retentions can be in 'minutes' as well (e.g. 2800 minutes = 2 days). Note that this is the same as --clean=logs --log-retention=2880.
./VirtualClient --profile=PERF-CPU-COREMARK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --log-retention=2880
```

## Scenario: Upload Metrics and Logs to an Event Hub
The Virtual Client supports the ability to upload metrics, counters, logs etc... to an [Azure Event Hub](https://azure.microsoft.com/en-us/services/event-hubs/?OCID=AID2200277_SEM_092bba0f3fec11eb8ce6dbef46f6464a:G:s&ef_id=092bba0f3fec11eb8ce6dbef46f6464a:G:s&msclkid=092bba0f3fec11eb8ce6dbef46f6464a).
Event Hubs are a highly-scalable messaging platform in the Azure Cloud that can be integrated out-of-the-box with other big-data platforms such as Azure Data Explorer (ADX/Kusto).
Note that the Virtual Client does have a set of explicit expectations for how the Event Hubs are setup. The following documentation covers what is required:

* [Event Hub Integration](./0610-integration-event-hub.md) 

``` bash
# To send data to an Event Hub, supply a connection string to the Event Hub namespace on the command line.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --event-hub="{EventHubConnectionString}"
```

## Scenario: Upload Log Files to a Content Store
Most components in the Virtual Client allow the user to upload information or files produced by the execution of workloads and monitors to
a cloud Blob store. In order to enable this, the connection string or SAS URI to the Blob store should be supplied on the command line. See the documentation
on monitor profiles below for additional details on which profiles support this.

* [Blob Store Support](./0600-integration-blob-storage.md)
* [Monitor Profiles](../monitors/0200-monitor-profiles.md)

``` bash
VirtualClient.exe --profile=PERF-NETWORK.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --content="{BlobStoreConnectionString|SAS URI}" --parameters=ProfilingEnabled=true,,,ProfilingMode=Interval
```

## Scenario: Change the Amount of Operational Trace Telemetry Emitted
Virtual Client emits quite a bit of operational traces while running in order to provide good information to the user. There are times when this
amount of information is not desirable. The logging level (or severity) can be changed on the command line. The default logging level is 'Information'.

``` bash
# Emit traces for 'Warning' level and above only.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Warning

# Emit traces for 'Error' level and above only.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Error

# Emit traces for 'Critical' level and above only.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Critical
```

Correspondingly, there are times when more operational traces are desirable (e.g. for debugging scenarios). The default logging level is 'Information'.

``` bash
# Emit all traces (...the most verbose option)
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Trace

# Emit traces for 'Debug' level and above.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Debug

# Emit traces for 'Information' level and above only.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Information
```

## Scenario: Change the Default Location for Log Files
Virtual Client writes log files to the application `/logs` folder by default. However, the user can change this by defining an alternate
path on the command line or via an environment variable. See the section below on 'Supported Environment Variables' for more information on options
available.

``` bash
# Define an alternate location for log files on the command line.
/home/user/virtualclient$ VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --log-dir="/home/user/logs"

# Define an alternate location for log files using an environment variable.
/home/user/virtualclient$ export VC_LOGS_DIR="/home/user/logs"
/home/user/virtualclient$ VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}"
```

## Scenario: Change the Default Location for Package Downloads
Virtual Client downloads packages to the application `/packages` folder by default. However, the user can change this by defining an alternate
path on the command line or via an environment variable. See the section below on 'Supported Environment Variables' for more information on options
available.

``` bash
# Define an alternate location for package downloads on the command line.
/home/user/virtualclient$ VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}" --package-dir="/home/user/packages"

# Define an alternate location for package downloads using an environment variable.
/home/user/virtualclient$ export VC_PACKAGES_DIR="/home/user/packages"
/home/user/virtualclient$ VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=03:00:00 --packages="{BlobStoreConnectionString|SAS URI}"
```

## Supported Environment Variables
The Virtual Client application supports a small set of environment variables that allow users to provide information to the application. Each of 
these environment variables has a corresponding command line option to enable support for a range of use cases. The following environment variables
can be used to define alternate locations for dependencies:

* **VC_LIBRARY_PATH**  
  Defines 1 or more path locations where extensions assemblies/.dlls exist and that should be loaded at runtime. Multiple directory paths can be defined separated
  by a semi-colon ';' character (similar to the Windows and Linux `PATH` environment variable). Note that Virtual Client will search the immediate directory only
  for extension assemblies/.dlls. Recursive subdirectory searches are not supported.

  ``` bash
  # Example Folder Contents:
  # /VirtualClient.Extensions.Actions
  #      /VirtualClient.Extensions.Actions.dll
  #      /VirtualClient.Extensions.Actions.pdb
  #
  # /VirtualClient.Extensions.Monitors
  #      /VirtualClient.Extensions.Monitors.dll
  #      /VirtualClient.Extensions.Monitors.pdb
  #
  # On Windows systems
  C:\VirtualClient> set VC_LIBRARY_PATH=C:\Extensions\VirtualClient.Extensions.Actions
  C:\VirtualClient> set VC_LIBRARY_PATH=C:\Extensions\VirtualClient.Extensions.Actions;C:\Extensions\VirtualClient.Extensions.Monitors

  # On Linux systems.
  /home/user/virtualclient$ export VC_LIBRARY_PATH=/home/user/Extensions/VirtualClient.Extensions.Actions
  /home/user/virtualclient$ export VC_LIBRARY_PATH=/home/user/Extensions/VirtualClient.Extensions.Actions;/home/user/Extensions/VirtualClient.Extensions.Monitors
  ```

* **VC_LOGS_DIR**  
  Defines an alternate directory path to which Virtual Client should write log files. This overrides the default logs location 
  (e.g. \<application-dir\>/logs).

  ``` bash
  # On Windows systems
  C:\VirtualClient> set VC_LOGS_DIR=C:\Users\User1\Logs

  # On Linux systems.
  /home/user/virtualclient$ export VC_LOGS_DIR="/home/user/logs"

  # Note that the logs directory can be defined on the command line as well. Log directories
  # defined on the command line take priority over those defined by the environment variable.
  /home/user/virtualclient$ ./VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=120 --log-dir="/home/user/logs"
  ```

* **VC_PACKAGES_DIR**  
  Defines an alternate directory path where Virtual Client packages (including extensions packages) exist and to where packages should be downloaded. This overrides the
  default packages location (e.g. \<application_dir\>/packages) such that it will not be used for package searches or downloads. The must be a single directory. Recursive subdirectory searches 
  are not supported.

  ``` bash
  # Example Folder Contents:
  # /custom_packages
  #      /workload_a.1.0.0.zip
  #      /workload_b.1.0.0.zip
  #      /workload_c.1.0.0.zip
  #      /workload_d.1.0.0.zip
  
  # On Windows systems
  C:\VirtualClient> set VC_PACKAGES_DIR=C:\custom_packages

  # On Linux systems.
  /home/user/virtualclient$ export VC_PACKAGES_DIR="/home/user/custom_packages"

  # Note that the packages directory can be defined on the command line as well. Package directories
  # defined on the command line take priority over those defined by the environment variable.
  /home/user/virtualclient$ ./VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=120 --package-dir="/home/user/packages"
  ```

* **VC_STATE_DIR**  
  Defines an alternate directory path to which Virtual Client should write state files/documents. This overrides the default state location 
  (e.g. \<application-dir\>/state).

  ``` bash
  # On Windows systems
  C:\VirtualClient> set VC_STATE_DIR=C:\Users\User1\State

  # On Linux systems.
  /home/user/virtualclient$ export VC_STATE_DIR="/home/user/state"

  # Note that the state directory can be defined on the command line as well. State directories
  # defined on the command line take priority over those defined by the environment variable.
  /home/user/virtualclient$ ./VirtualClient --profile=PERF-CPU-OPENSSL.json --timeout=120 --state-dir="/home/user/state"
  ```