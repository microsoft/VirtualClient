# Command Line Options
The following sections describe the command line options available on the Virtual Client application.

## Default Command Options
The following command line options are available on the default Virtual Client command. The default command allows the user to execute one or more profiles
on the system.

| Option                                                         | Required | Data Type                    | Description |
|----------------------------------------------------------------|----------|------------------------------|-------------|
| --p, --profile=\<profile\>                                     | Yes      | string/text                  | The execution profile which indicates the set of workloads to run. |
| --ps, --packages, --packageStore=\<authtoken\>                 | Yes/No   | string/connection string/SAS | A full connection string or SAS URI to an Azure storage account blob store from which workload and dependency packages can be downloaded. This is required for most workloads because the workload binary/script packages are not typically packaged with the Virtual Client application itself. Contact the VC Team to get a SAS URI for your team. See [Azure Storage Account Integration](./0600-integration-blob-storage.md). |
| --a, --agentId, --clientId=\<id\>                              | No       | string/text                  | An identifier that can be used to uniquely identify the instance of the Virtual Client in telemetry separate from other instances. The default value is the name of the system if this option is not explicitly defined (i.e. the name as defined by the operating system). |
| --port, --api-port=\<port\>                                    | No       | integer                      | The port to use for hosting the Virtual Client REST API service for profiles that allow multi-system, client/server operations (e.g. networking). Additionally, a port may be defined for each role associated with the profile operations using the format {Port}/{Role} with each port/role combination delimited by a comma (e.g. 4501/Client,4502/Server). |
| --cs, --content, --contentStore=\<authtoken\>                  | No       | string/connection string/SAS | A full connection string or SAS URI to an Azure storage account blob store where files/content can be uploaded as part of background monitoring processes. Contact the VC Team to get a SAS URI for your team. See [Azure Storage Account Integration](./0600-integration-blob-storage.md). |
| --eh, --eventHub, --eventHubConnectionString=\<accesspolicy\>  | No       | string/connection string     | A full connection string/access policy for the Azure Event Hub namespace where telemetry should be written. Contact the VC Team to get an access policy for your team. See [Azure Event Hub Integration](./0610-integration-event-hub.md). |
| --e, --experimentId=\<guid\>                                   | No       | guid                         | A unique identifier that defines the ID of the experiment for which the Virtual Client workload is associated. |
| --lp, --layoutPath=\<path\>                                    | No       | string/path                  | A path to a environment layout file that provides additional metadata about the system/hardware on which the Virtual Client will run and information required to support client/server advanced topologies. See [Client/Server Support](./0020-client-server.md). |
| --ltf, --log-to-file, --logtofile                              | No       |                              | Flag indicates that the output of processes executed by the Virtual Client should be written to log files in the logs directory. |
| --mt, --metadata=\<key=value,,,key=value...\>                  | No       | string/text                  | Metadata to include with all logs/telemetry output from the Virtual Client. <br/><br/>Each metadata entry should be akey value pair separated by ",,," delimiters (e.g. property1=value1,,,property2=value2). |
| --pm, --parameters=\<key=value,,,key=value...\>                | No       | string/text                  | Parameters or overrides to pass to the execution profiles that can modify aspects of their operation. <br/><br/>Each instruction should be a key value pair separated by ",,," delimiters (e.g. instruction1=value1,,,instruction2=value2). |
| --sc, --scenarios=\<scenario,scenario...\>                     | No       | string/text                  | A comma-delimited list/subset of scenarios defined in the execution profile to run (e.g. scenario1,scenario2,scenario3). |
| --sd, --seed=\<seed\>                                          | No       | integer                      | The seed used to guarantee identical randomization between executions.  |
| --s, --system=\<executionSystem\>                              | No       | string/text                  | The execution system/platform in which Virtual Client is running (e.g. Azure). |
| --t, --timeout=\<mins_or_timespan\>,deterministic<br/>--timeout=\<mins_or_timespan\>,deterministic\*  | No | timespan or integer | Specifies a timespan or the length of time (in minutes) that the Virtual Client should run before timing out and exiting (e.g. 1440, 01.00:00:00). The user can additionally provide an extra instruction to indicate the application should wait for deterministic completions.<br/><br/>Use the '**deterministic**' instruction to indicate the application should wait for the current action/workload to complete before timing out (e.g. --timeout=1440,deterministic).<br/><br/>Use the '**deterministic***' instruction to indicate the application should wait for all actions/workloads in the profile to complete before timing out (e.g. --timeout=1440,deterministic*).<br/><br/> Note that this option cannot be used with the --iterations option.<br/><br/>If neither the --timeout nor --iterations option are supplied, the Virtual Client will run non-stop until manually terminated.   |
| --i, --iterations=\<count\>                                    | No       | integer                      | Defines the number of iterations/rounds of all actions in the profile to execute before exiting.<br/><br/> Note that this option cannot be used with the --timeout option.  |
| --fw, --flush-wait=<mins_or_timespan>                          | No       | timespan or integer          | Specifies a timespan or the length of time (in minutes) that the Virtual Client should wait for telemetry to be fully flushed before exiting (e.g. 60, 01:00:00). This is useful for scenarios where Event Hub resources are used to ensure that all telemetry is uploaded successfully before exit. Default = 30 mins. |
| --dependencies                                                 | No       |                              | Flag indicates that only the dependencies defined in the profile should be executed/installed. |
| --debug                                                        | No       |                              | If this flag is set, verbose logging will be output to the console.  |
| -?, -h, --help                                                 | No       |                              | Show help information. |
| --ver                                                          | No       |                              | Show application version information. |

```bash
 # Run a workload profile
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}"

 # Include specific metadata in the telemetry output by the application.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"

 # Include experiment/run IDs and agent IDs as correlation identifiers in addition to metadata output by the application.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --experimentId=b9fd4dce-eb3b-455f-bc81-2a394d1ff849 --clientId=cluster01,eb3fc2d9-157b-4efc-b39c-a454a0779a5b,VCTest4-01 --packages="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"

 # Upload telemetry output to a target Event Hub namespace.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --eventHub="{AccessPolicy}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"

 # Use the 'deterministic' instruction to ensure that an action/workload running is allowed
 # to complete before timing out.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180,deterministic --packages="{BlobStoreConnectionString|SAS URI}"

 # Use the 'deterministic*' instruction to ensure that all profile actions/workloads are allowed
 # to complete before timing out.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180,deterministic* --packages="{BlobStoreConnectionString|SAS URI}"

 # Run the actions in a profile a certain number of iterations/rounds before exiting.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --iterations=3 --packages="{BlobStoreConnectionString|SAS URI}"

 # Install just the dependencies defined in the profile (but do not run the actions or monitors).
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --dependencies --packages="{BlobStoreConnectionString|SAS URI}"

 # Log the output of workload, monitor and dependency processes to the logs directory on the file system.
 VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --log-to-file
```

## Subcommands
The following tables describe the various subcommands that are supported by the Virtual Client application.

* ### bootstrap
  Command is used to bootstrap/install dependency packages on the system. This is used for example to install "extensions" packages to the Virtual Client before they
  can be used (see the Developer Guide at the top for information on developing extensions). Note that many of the options below are similar to the default
  command documented above. Most are not required but allow the user/automation to use the same correlation identifiers for the bootstrapping operations as will
  be used for the profile execution operations that may follow.


| Option                                                        | Required | Data Type                    | Description |
|---------------------------------------------------------------|----------|------------------------------|-------------|
| --pkg, --package =\<blobName\>                                | Yes      | string/blob name             | Defines the name/ID of a package to bootstrap/install (e.g. anypackage.1.0.0.zip). |
| --n, --name=\<name\>                                          | Yes      | string/name                  | Defines the logical name of a package as it should be registered on the system (e.g. anypackage.1.0.0.zip -> anypackage). |
| --ps, --packages, --packageStore=\<authtoken\>                | Yes/No   | string/connection string/SAS | A full connection string or SAS URI to an Azure storage account blob store from which dependency packages can be downloaded. This is required if the dependency package DOES NOT already exist on the system and vice versa. Contact the VC Team to get a SAS URI for your team. See [Azure Storage Account Integration](./0600-integration-blob-storage.md). |
| --a, --agentId, --clientId=\<id\>                             | No       | string/text                  | An identifier that can be used to uniquely identify the instance of the Virtual Client in telemetry separate from other instances. The default value is the name of the system if this option is not explicitly defined (i.e. the name as defined by the operating system). |
| --eh, --eventHub, --eventHubConnectionString=\<accesspolicy\> | No       | string/connection string     | A full connection string/access policy for the Azure Event Hub namespace where telemetry should be written. Contact the VC Team to get an access policy for your team. See [Azure Event Hub Integration](./0610-integration-event-hub.md). |
| --e, --experimentId=\<guid\>                                  | No       | guid                         | A unique identifier that defines the ID of the experiment for which the Virtual Client workload is associated. |
| --ltf, --log-to-file, --logtofile                             | No       |                              | Flag indicates that the output of processes executed by the Virtual Client should be written to log files in the logs directory. |
| --mt, --metadata=\<key=value,,,key=value...\>                 | No       | string/text                  | Metadata to include with all logs/telemetry output from the Virtual Client. <br/><br/>Each metadata entry should be akey value pair separated by ",,," delimiters (e.g. property1=value1,,,property2=value2). |
| --s, --system=\<executionSystem\>                             | No       | string/text                  | The execution system/platform in which Virtual Client is running (e.g. Azure). |
| --debug                                                       | No       |                              | If this flag is set, verbose logging will be output to the console.  |
| -?, -h, --help                                                | No       |                              | Show help information. |
| --ver                                                         | No       |                              | Show application version information. |

``` bash
# Run a basic bootstrap operation.
VirtualClient.exe bootstrap --package=anyworkload.1.0.0.zip --name=anyworkload --packages="{BlobStoreConnectionString|SAS URI}"

# Run a bootstrap operation supplying a range of additional correlation identifiers and metadata
# that can then be associated with subsequent profile execution operations.
VirtualClient.exe bootstrap --package=anyworkload.1.0.0.zip --name=anyworkload --system=Demo --experimentId=b9fd4dce-eb3b-455f-bc81-2a394d1ff849 --agentId=Agent01 --packages="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"
```

* ### runapi
  Runs the Virtual Client API service and optionally monitors the API (local or a remote instance) for heartbeats.


| Option              | Required | Data Type         | Description |
|---------------------|----------|-------------------|-------------|
| --port, --api-port=\<port\> | No  | integer | The port to use for hosting the Virtual Client REST API service. Additionally, a port may be defined for the Client system and Server system independently using the format '\{Port\}/\{Role\}' with each port/role combination delimited by a comma (e.g. 4501/Client,4502/Server). |
| --ip, --ipAddress   | No       | string/IP address | An IPv4 or IPv6 address of a target/remote system on which a Virtual Client instance is running to monitor. The API service must also be running on the target instance.  |
| --mon, --monitor    | No       |                   | If supplied as a flag (i.e. no argument), the Virtual Client will run a background thread that tests the local API. If an IP address is provided, the target Virtual Client API will be monitored/tested. This is typically used for debugging scenarios to make sure 2 different instances of the Virtual Client can communicate with each other through the API. |
| --debug             | No       |                   | If this flag is set, verbose logging will be output to the console.  |
| -?, -h, --help      | No       |                   | Show help information. |
| --ver               | No       |                   | Show application version information. |



``` bash
# Run the API service locally.
VirtualClient.exe runapi

# Run the API service locally and monitor another remote instance of the Virtual Client.
VirtualClient.exe runapi --monitor --ipAddress=1.2.3.4
```

## Exit Codes
The Virtual Client application is instrumented to provide fine-grained return/exit codes that describe the outcome or result of the application operations. An exit code of
0 means that the application was successful. Any non-zero exit code indicates a failure somewhere in the set of operations. See the definitions for a list of exit
codes and their meaning in the source code here: [ErrorReason](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/Enumerations.cs)

## Response File Support
The Virtual Client application supports response files out of the box. A response file is a file that contains the command line arguments within. This is useful for certain scenarios where
passing in secrets (e.g. connection strings, SAS URIs) on the command line may not be supported by the automation process executing the Virtual Client application. The following examples shows
how to use response files with the Virtual Client on the command line.

```
# The extension does not really matter, but is is common to use one such as '*.rsp' or '*.response' to indicate the file is a response file.
VirtualClient.exe @.\CommandLineOptions.rsp
VirtualClient.exe @C:\VirtualClient\win-x64\CommandLineOptions.rsp

# On Linux systems.
./VirtualClient @./CommandLineOptions.rsp
./VirtualClient @/home/anyuser/VirtualClient/linux-x64/CommandLineOptions.rsp
```

```
# Example Response File Contents
# Each command line option and argument should be defined on a separate line within the file.
#
# Inside the CommandLineOptions.rsp File:

--profile=PERF-CPU-OPENSSL.json
--system=Demo
--timeout=1440
--experimentId=b9fd4dce-eb3b-455f-bc81-2a394d1ff849
--clientId=cluster01,eb3fc2d9-157b-4efc-b39c-a454a0779a5b,VCTest4-01
--packages="{BlobStoreConnectionString|SAS URI}"
--eventHub="{AccessPolicy}"
--metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01" 
```
