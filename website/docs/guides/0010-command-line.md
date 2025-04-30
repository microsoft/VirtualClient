# Command Line Options
The following sections describe the command line options available on the Virtual Client application.

## Default Command Options
The following command line options are available on the default Virtual Client command. The default command allows the user to execute one or more profiles
on the system.

| Option                                                         | Required | Data Type                    | Description |
|----------------------------------------------------------------|----------|------------------------------|-------------|
| --p, --profile=\<profile\>                                     | Yes      | string/text                  | The execution profile which indicates the set of workloads to run. |
| --ps, --packages, --package-store=\<authtoken\>                | No   | string/connection string/SAS | A full connection description for an [Azure Storage Account](./0600-integration-blob-storage.md) from which to download workload and dependency packages. This is required for most workloads because the workload binary/script packages are not typically packaged with the Virtual Client application itself. This option defaults to a public storage account VC team maintains.<br/><br/>The following are supported identifiers for this option:<br/><ul><li>Storage Account blob service SAS URIs</li><li>Storage Account blob container SAS URIs</li><li>Microsoft Entra ID/Apps using a certificate</li><li>Microsoft Azure managed identities</li></ul>See [Azure Storage Account Integration](./0600-integration-blob-storage.md) for additional details on supported identifiers.<br/><br/><mark>Always surround connection descriptions with quotation marks.</mark> |
| --c, --client, --client-id=\<id\>                              | No       | string/text                  | An identifier that can be used to uniquely identify the instance of the Virtual Client in telemetry separate from other instances. The default value is the name of the system if this option is not explicitly defined (i.e. the name as defined by the operating system). |
| --port, --api-port=\<port>                                     | No       | integer                      | The port to use for hosting the Virtual Client REST API service for profiles that allow multi-system, client/server operations (e.g. networking). Additionally, a port may be defined for each role associated with the profile operations using the format \{Port}/\{Role} with each port/role combination delimited by a comma (e.g. 4501/Client,4502/Server). |
| --clean=\<target,target...\>                                   | No       | string                       | Instructs the application to perform an initial clean before continuing to remove pre-existing files/content created by the application from the file system. This can include log files, packages previously downloaded and state management files. This option can be used as a flag (e.g. --clean) as well to clean all file content. Valid target resources include: logs, packages, state, all (e.g. --clean=logs, --clean=packages). Multiple resources can be comma-delimited (e.g. --clean=logs,packages). To perform a full reset of the application state, use the option as a flag (e.g. --clean). This effectively sets the application back to a "first run" state. |
| --cs, --content, --content-store=\<authtoken\>                 | No       | string/connection string/SAS | A full connection description for an [Azure Storage Account](./0600-integration-blob-storage.md) to use for uploading files/content (e.g. log files).<br/><br/>The following are supported identifiers for this option:<br/><ul><li>Storage Account blob service SAS URIs</li><li>Storage Account blob container SAS URIs</li><li>Microsoft Entra ID/Apps using a certificate</li><li>Microsoft Azure managed identities</li></ul>See [Azure Storage Account Integration](./0600-integration-blob-storage.md) for additional details on supported identifiers.<br/><br/><mark>Always surround connection descriptions with quotation marks.</mark> |
| --cp, --content-path, --content-path-template=\<folderPattern\>| No       | string/text                  | The content path format/structure to use when uploading content to target storage resources. When not defined the 'Default' structure is used. Default: "\{experimentId}/\{agentId}/\{toolName}/\{role}/\{scenario}" |
| [**Deprecating with --logger**]--eh, --eventhub, --event-hub=\<authtoken\>                    | No       | string/connection string     | A full connection description for an [Azure Event Hub namespace](./0610-integration-event-hub.md) to send/upload telemetry data from the operations of the Virtual Client.<br/><br/>The following are supported identifiers for this option:<br/><ul><li>Event Hub namespace shared access policies</li><li>Microsoft Entra ID/Apps using a certificate</li><li>Microsoft Azure managed identities</li></ul>See [Azure Event Hub Integration](./0610-integration-event-hub.md) for additional details on supported identifiers.<br/><br/><mark>Always surround connection descriptions with quotation marks.</mark> |
| --e, --experiment, --experiment-id=\<guid\>                    | No       | guid                         | A unique identifier that defines the ID of the experiment for which the Virtual Client workload is associated. |
| --ff, --fail-fast                                              | No       |                              | Flag indicates that the application should exit immediately on first/any errors regardless of their severity. This applies to 'Actions' in the profile only. 'Dependencies' are ALWAYS implemented to fail fast. 'Monitors' are generally implemented to handle transient issues and to keep running/trying in the background.  |
| --lp, --layout, --layout-path=\<path\>                         | No       | string/path                  | A path to a environment layout file that provides additional metadata about the system/hardware on which the Virtual Client will run and information required to support client/server advanced topologies. See [Client/Server Support](./0020-client-server.md). |
| --logger=\<logger definition\>                         | No       | string/path                  | One or more logger definition string. Allows defining multiple loggers and their definitions on command line. Defaults to console logger and file logger. |
| --ldir, --log-dir=\<path\>                                     | No       | string/path                  | Defines an alternate directory to which log files should be written. |
| --ll, --log-level                                              | No       | integer/string               | Defines the logging severity level for traces output. Values map to the [Microsoft.Extensions.Logging.LogLevel](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-8.0) enumeration. Valid values include: Trace (0), Debug (1), Information (2), Warning (3), Error (4), Critical (5). Note that this option affects ONLY trace logs and is designed to allow the user to control the amount of operational telemetry emitted by VC. It does not affect metrics or event logging nor any non-telemetry logging. Default = Information (2). |
| --lr, --log-retention=\<mins_or_timespan\>                     | No       | timespan or integer          | Defines the log retention period. This is a timespan or length of time (in minutes) to apply to cleaning up/deleting existing log files (e.g. 2880, 02.00:00:00). Log files with creation times older than the retention period will be deleted. |
| --ltf, --log-to-file                                           | No       |                              | Flag indicates that the output of processes executed by the Virtual Client should be written to log files in the logs directory. |
| --mt, --metadata=\<key=value,,,key=value...\>                  | No       | string/text                  | Metadata to include with all logs/telemetry output from the Virtual Client. Each metadata entry should be a key/value pair separated by ",,," delimiters or traditional delimiters such as a comma "," or a semi-colon ";".<br/><br/>e.g.<br/><ul><li>--metadata="property1=value1,,,property2=value2"</li><li>--metadata="property1=value1,property2=value2"</li><li>--metadata="property1=value1;property2=value2"</li></ul><mark>It is recommended that the user avoid mixing different delimiters together. Always surround metadata values with quotation marks.</mark> |
| --pdir, --package-dir=\<path\>                                 | No       | string/path                  | Defines an alternate directory to which packages will be downloaded. |
| --pm, --parameters=\<key=value,,,key=value...\>                | No       | string/text                  | Parameters or overrides to pass to the execution profiles that can modify aspects of their operation. Each property entry should be a key/value pair separated by ",,," delimiters or traditional delimiters such as a comma "," or a semi-colon ";".<br/><br/>e.g.<br/><ul><li>--parameters="property1=value1,,,parameters=value2"</li><li>--parameters="property1=value1,property2=value2"</li><li>--parameters="property1=value1;property2=value2"</li></ul><mark>It is recommended that the user avoid mixing different delimiters together. Always surround parameter values with quotation marks.</mark> |
| --sc, --scenarios=\<scenario,scenario...\>                     | No       | string/text                  | A comma-delimited list/subset of scenarios defined in the execution profile to include or exclude. Note that most components in a profile have a 'Scenario' parameter and this is the value to use. <br/><br/>To include/run a subset of actions within the profile, provide the scenario names delimited by a comma (e.g. scenario1,scenario2,scenario3). To exclude one or more actions from being ran simply place a minus sign in front of the delimited scenario names (e.g. -scenario1,-scenario2,-scenario3).<br/><br/>Monitors and dependencies within a profile can ONLY be excluded. This is specified in the same way that it is for actions with a minus sign in front of the scenario name(s). |
| --sd, --seed=\<seed\>                                          | No       | integer                      | The seed used to guarantee identical randomization between executions.  |
| --sdir, --state-dir=\<path\>                                   | No       | string/path                  | Defines an alternate directory to which state files/documents will be written. |
| --s, --system=\<executionSystem\>                              | No       | string/text                  | The execution system/platform in which Virtual Client is running (e.g. Azure). |
| --t, --timeout=\<mins_or_timespan\>,deterministic<br/>--timeout=\<mins_or_timespan\>,deterministic\*  | No | timespan or integer | Specifies a timespan or the length of time (in minutes) that the Virtual Client should run before timing out and exiting (e.g. 1440, 01.00:00:00). The user can additionally provide an extra instruction to indicate the application should wait for deterministic completions.<br/><br/> Use --timeout=-1 or --timeout=never to indicate run forever.<br/><br/>Use the '**deterministic**' instruction to indicate the application should wait for the current action/workload to complete before timing out (e.g. --timeout=1440,deterministic).<br/><br/>Use the '**deterministic***' instruction to indicate the application should wait for all actions/workloads in the profile to complete before timing out (e.g. --timeout=1440,deterministic*).<br/><br/> Note that this option cannot be used with the `--iterations` option.<br/><br/>If neither the `--timeout` nor `--iterations` option are supplied, the Virtual Client will run one iteration. |
| --i, --iterations=\<count\>                                    | No       | integer                      | Defines the number of iterations/rounds of all actions in the profile to execute before exiting.<br/><br/> Note that this option cannot be used with the `--timeout` option.<br/><br/>If neither the `--timeout` nor `--iterations` option are supplied, the Virtual Client will run one iteration.  |
| --wait, --exit-wait, --flush-wait=\<mins_or_timespan>          | No       | timespan or integer          | Specifies a timespan or the length of time (in minutes) that the Virtual Client should wait for workload and monitor processes to complete and for telemetry to be fully flushed before exiting (e.g. 60, 01:00:00). This is useful for scenarios where Event Hub resources are used to ensure that all telemetry is uploaded successfully before exit. Default = 30 mins. |
| --dependencies                                                 | No       |                              | Flag indicates that only the dependencies defined in the profile should be executed/installed. |
| --debug, --verbose                                             | No       |                              | Request verbose logging output to the console. This is equivalent to setting `--log-level=Trace`  |
| -?, -h, --help                                                 | No       |                              | Show help information. |
| --version                                                      | No       |                              | Show application version information. |

See the [Usage Examples](./0200-usage-examples.md) documentation for additional examples.

```bash
# Run a workload profile
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --package-store="{BlobStoreConnectionString|SAS URI}"

# Include experiment/run IDs and agent IDs as correlation identifiers in addition to metadata output by the application.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --experiment-id=b9fd4dce-eb3b-455f-bc81-2a394d1ff849 --client-id=cluster01,eb3fc2d9-157b-4efc-b39c-a454a0779a5b,VCTest4-01 --package-store="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"

# Include specific metadata in the telemetry output by the application.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"

# Override workload profile parameters on the command line. See the 'Parameters' section 
# at the top of the PERF-IO-DISKSPD.json profile under the 'Workloads' area of the site.
VirtualClient.exe --profile=PERF-IO-DISKSPD.json --system=Demo --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --parameters="DiskFillSize=128G,,,FileSize=128G,,,Duration=00:10:00"

# Upload telemetry output to a target Event Hub namespace.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --event-hub="{AccessPolicy}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"

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

# Use an alternate location to write log files instead of the default 'logs' folder within the application directory.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --log-dir="C:\Users\Any\Logs" --log-to-file

# Reduce the amount of traces/operational telemetry emitted by the application.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --log-level=Information

# Clean up existing log files before execution.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --clean=logs

# Clean up existing log files beyond a retention period (e.g. remove log files older than 2 days).
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --log-retention=02.00:00:00

# Use an alternate location to download packages instead of the default 'packages' folder within the application directory.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --package-dir="C:\Users\Any\Packages"

# Use an alternate location to write state files/documents instead of the default 'state' folder within the application directory.
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --packages="{BlobStoreConnectionString|SAS URI}" --state-dir="C:\Users\Any\State"
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
  | --ps, --packages, --package-store=\<authtoken\>               | Yes/No   | string/connection string/SAS | A full connection description for an [Azure Storage Account](./0600-integration-blob-storage.md) from which to download workload and dependency packages. This is required for most workloads because the workload binary/script packages are not typically packaged with the Virtual Client application itself.<br/><br/>The following are supported identifiers for this option:<br/><ul><li>Storage Account blob service SAS URIs</li><li>Storage Account blob container SAS URIs</li><li>Microsoft Entra ID/Apps using a certificate</li><li>Microsoft Azure managed identities</li></ul>See [Azure Storage Account Integration](./0600-integration-blob-storage.md) for additional details on supported identifiers.<br/><br/><mark>Always surround connection descriptions with quotation marks.</mark> |
  | --c, --client, --client-id=\<id\>                             | No       | string/text                  | An identifier that can be used to uniquely identify the instance of the Virtual Client in telemetry separate from other instances. The default value is the name of the system if this option is not explicitly defined (i.e. the name as defined by the operating system). |
  | --clean=\<target,target...\>                                  | No       | string                       | Instructs the application to perform an initial clean before continuing to remove pre-existing files/content created by the application from the file system. This can include log files, packages previously downloaded and state management files. This option can be used as a flag (e.g. --clean) as well to clean all file content. Valid target resources include: logs, packages, state, all (e.g. --clean=logs, --clean=packages). Multiple resources can be comma-delimited (e.g. --clean=logs,packages). To perform a full reset of the application state, use the option as a flag (e.g. --clean). This effectively sets the application back to a "first run" state. |
  | --eh, --eventhub, --event-hub=\<authtoken\>                   | No       | string/connection string     | A full connection description for an [Azure Event Hub namespace](./0610-integration-event-hub.md) to send/upload telemetry data from the operations of the Virtual Client.<br/><br/>The following are supported identifiers for this option:<br/><ul><li>Event Hub namespace shared access policies</li><li>Microsoft Entra ID/Apps using a certificate</li><li>Microsoft Azure managed identities</li></ul>See [Azure Event Hub Integration](./0610-integration-event-hub.md) for additional details on supported identifiers.<br/><br/><mark>Always surround connection descriptions with quotation marks.</mark> |
  | --e, --experiment, --experiment-id=\<guid\>                   | No       | guid                         | A unique identifier that defines the ID of the experiment for which the Virtual Client workload is associated. |
  | --ldir, --log-dir=\<path\>                                    | No       | string/path                  | Defines an alternate directory to which log files should be written. |
  | --ll, --log-level                                             | No       | integer/string               | Defines the logging severity level for traces output. Values map to the [Microsoft.Extensions.Logging.LogLevel](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-8.0) enumeration. Valid values include: Trace (0), Debug (1), Information (2), Warning (3), Error (4), Critical (5). Note that this option affects ONLY trace logs and is designed to allow the user to control the amount of operational telemetry emitted by VC. It does not affect metrics or event logging nor any non-telemetry logging. Default = Information (2). |
  | --lr, --log-retention=\<mins_or_timespan\>                    | No       | timespan or integer          | Defines the log retention period. This is a timespan or length of time (in minutes) to apply to cleaning up/deleting existing log files (e.g. 2880, 02.00:00:00). Log files with creation times older than the retention period will be deleted. |
  | --ltf, --log-to-file                                          | No       |                              | Flag indicates that the output of processes executed by the Virtual Client should be written to log files in the logs directory. |
  | --mt, --metadata=\<key=value,,,key=value...\>                 | No       | string/text                  | Metadata to include with all logs/telemetry output from the Virtual Client. Each metadata entry should be a key/value pair separated by ",,," delimiters or traditional delimiters such as a comma "," or a semi-colon ";".<br/><br/>e.g.<br/><ul><li>--metadata="property1=value1,,,property2=value2"</li><li>--metadata="property1=value1,property2=value2"</li><li>--metadata="property1=value1;property2=value2"</li></ul><mark>It is recommended that the user avoid mixing different delimiters together. Always surround metadata values with quotation marks.</mark> |
  | --pdir, --package-dir=\<path\>                                | No       | string/path                  | Defines an alternate directory to which packages will be downloaded. |
  | --sdir, --state-dir=\<path\>                                  | No       | string/path                  | Defines an alternate directory to which state files/documents will be written. |
  | --s, --system=\<executionSystem\>                             | No       | string/text                  | The execution system/platform in which Virtual Client is running (e.g. Azure). |
  | --wait, --exit-wait, --flush-wait=\<mins_or_timespan>         | No       | timespan or integer          | Specifies a timespan or the length of time (in minutes) that the Virtual Client should wait for workload and monitor processes to complete and for telemetry to be fully flushed before exiting (e.g. 60, 01:00:00). This is useful for scenarios where Event Hub resources are used to ensure that all telemetry is uploaded successfully before exit. Default = 30 mins. |
  | --debug, --verbose                                            | No       |                              | Request verbose logging output to the console. This is equivalent to setting `--log-level=Trace` |
  | -?, -h, --help                                                | No       |                              | Show help information. |
  | --version                                                     | No       |                              | Show application version information. |

  ``` bash
  # Run a basic bootstrap operation.
  VirtualClient.exe bootstrap --package=anyworkload.1.0.0.zip --name=anyworkload --package-store="{BlobStoreConnectionString|SAS URI}"

  # Run a bootstrap operation supplying a range of additional correlation identifiers and metadata
  # that can then be associated with subsequent profile execution operations.
  VirtualClient.exe bootstrap --package=anyworkload.1.0.0.zip --name=anyworkload --system=Demo --experimentId=b9fd4dce-eb3b-455f-bc81-2a394d1ff849 --client-id=Agent01 --package-store="{BlobStoreConnectionString|SAS URI}" --metadata="experimentGroup=Group A,,,cluster=cluster01,,,nodeId=eb3fc2d9-157b-4efc-b39c-a454a0779a5b,,,tipSessionId=5e66ecdf-575d-48b0-946f-5e6951545724,,,region=East US 2,,,vmName=VCTest4-01"
  ```

* ### clean/reset
  Command is used to perform a clean/reset on the system for Virtual Client 1) logs, 2) state or 3) packages. This is useful to force Virtual Client to process
  all dependency installations as if it is a first run on the system. Note that some workloads may not perform a full set of clean/reset operations. Note that most
  dependency handlers, workloads and monitors are designed to be idempotent but there may be outliers in more advanced workload scenarios.

  | Option                                         | Required | Data Type            | Description |
  |------------------------------------------------|----------|----------------------|-------------|
  | --clean=\<target,target...\>                   | No       | string                       | Instructs the application to perform an initial clean before continuing to remove pre-existing files/content created by the application from the file system. This can include log files, packages previously downloaded and state management files. This option can be used as a flag (e.g. --clean) as well to clean all file content. Valid target resources include: logs, packages, state, all (e.g. --clean=logs, --clean=packages). Multiple resources can be comma-delimited (e.g. --clean=logs,packages). To perform a full reset of the application state, use the option as a flag (e.g. --clean). This effectively sets the application back to a "first run" state. |
  | --ldir, --log-dir=\<path\>                     | No       | string/path                  | Defines an alternate directory to which log files should be written. |
  | --ll, --log-level                              | No       | integer/string               | Defines the logging severity level for traces output. Values map to the [Microsoft.Extensions.Logging.LogLevel](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-8.0) enumeration. Valid values include: Trace (0), Debug (1), Information (2), Warning (3), Error (4), Critical (5). Note that this option affects ONLY trace logs and is designed to allow the user to control the amount of operational telemetry emitted by VC. It does not affect metrics or event logging nor any non-telemetry logging. Default = Information (2). |
  | --lr, --log-retention=\<mins_or_timespan\>     | No       | timespan or integer          | Defines the log retention period. This is a timespan or length of time (in minutes) to apply to cleaning up/deleting existing log files (e.g. 2880, 02.00:00:00). Log files with creation times older than the retention period will be deleted. |
  | -?, -h, --help                                 | No       |                              | Show help information. |
  | --version                                      | No       |                              | Show application version information. |

  ``` bash
  # Clean everything (full reset)
  VirtualClient.exe clean
  VirtualClient.exe reset
  VirtualClient.exe reset --clean=all

  # Clean/reset specific targets (e.g. log files, state tracking, packages downloaded)
  VirtualClient.exe reset --clean=logs
  VirtualClient.exe reset --clean=state
  VirtualClient.exe reset --clean=packages

  # Clean/reset multiple targets (e.g. log files and packages downloaded)
  VirtualClient.exe reset --clean=logs,packages
  ```

* ### convert
  Virtual Client supports profiles in both JSON (default) and YAML format. This command is used to convert a given profile from one format to another (and vice-versa).

  | Option                                      | Required | Data Type            | Description |
  |---------------------------------------------|----------|----------------------|-------------|
  | --p, --profile=\<profile\>                  | Yes      | string/text          | The execution profile (in either JSON or YAML format) to convert to the other format (i.e. JSON to YAML or YAML to JSON). |
  | --path, --output, --output-path=\<path\>    | Yes      | string/path          | The full path to the directory to which the new/converted profile should be written. The file name of the original profile will be preserved (e.g. "PERF-CPU-OPENSSL.json" will be written to "PERF-CPU-OPENSSL.yml"). |
  | -?, -h, --help                              | No       |                      | Show help information. |
  | --version                                   | No       |                      | Show application version information. |

  ``` bash
  # Convert a JSON profile to YAML format
  # (e.g. PERF-CPU-OPENSSL.json to S:\Users\Any\Profiles\PERF-CPU-OPENSSL.yml)
  VirtualClient.exe convert --profile=PERF-CPU-OPENSSL.json --output-path=S:\Users\Any\Profiles

  # Convert a YAML profile to JSON format
  # (e.g. S:\Users\Any\Profiles\PERF-CPU-OPENSSL.yml to S:\Users\Any\Profiles\PERF-CPU-OPENSSL.json)
  VirtualClient.exe convert --profile=S:\Users\Any\Profiles\PERF-CPU-OPENSSL.yml --output-path=S:\Users\Any\Profiles
  ```

* ### runapi
  Runs the Virtual Client API service and optionally monitors the API (local or a remote instance) for heartbeats.

  | Option                     | Required | Data Type         | Description |
  |----------------------------|----------|-------------------|-------------|
  | --port, --api-port=\<port> | No       | integer           | The port to use for hosting the Virtual Client REST API service. Additionally, a port may be defined for the Client system and Server system independently using the format `\<Port> / \<Role>` with each port/role combination delimited by a comma (e.g. 4501/Client,4502/Server). |
  | --ip, --ip-address         | No       | string/IP address | An IPv4 or IPv6 address of a target/remote system on which a Virtual Client instance is running to monitor. The API service must also be running on the target instance.  |
  | --mon, --monitor           | No       |                   | If supplied as a flag (i.e. no argument), the Virtual Client will run a background thread that tests the local API. If an IP address is provided, the target Virtual Client API will be monitored/tested. This is typically used for debugging scenarios to make sure 2 different instances of the Virtual Client can communicate with each other through the API. |
  | --debug, --verbose         | No       |                   | Request verbose logging output to the console. This is equivalent to setting `--log-level=Trace` |
  | -?, -h, --help             | No       |                   | Show help information. |
  | --version                  | No       |                   | Show application version information. |

  ``` bash
  # Run the API service locally.
  VirtualClient.exe runapi

  # Run the API service locally and monitor another remote instance of the Virtual Client.
  VirtualClient.exe runapi --monitor --ip-address=1.2.3.4
  ```

## Exit Codes
The Virtual Client application is instrumented to provide fine-grained return/exit codes that describe the outcome or result of the application operations. An exit code of
0 means that the application was successful. Any non-zero exit code indicates a failure somewhere in the set of operations. See the definitions for a list of exit
codes and their meaning in the source code here: [ErrorReason](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/Enumerations.cs)

## Environment Variable Support
The Virtual Client application supports a small set of environment variables that allow users to provide information to the application. The full list of environment variables
supported are defined in the [Usage Examples](https://microsoft.github.io/VirtualClient/docs/guides/0200-usage-examples/) documentation.

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


