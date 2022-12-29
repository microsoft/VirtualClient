
# Virtual Client Platform Design
The following sections cover important design aspects of the Virtual Client platform application. This document is written for engineers and technical roles who
are interested in how the Virtual Client platform and application is designed. The application itself is a .NET 6.0 command line application written in C# that 
is has both cross-platform and multi-architecture support. It is compiled to support both Windows and Linux operating system platforms as well as x64 and arm64 
architectures. The choice to implement the application using managed code was made to enable a foundation for ease-of-rapid feature development at the same time 
as meeting all of the requirements for cross-platform + architecture support. Additionally, the .NET 6.0 framework integrates all of the performance and runtime 
efficiency work done by the .NET Core team over the past 5 years into a unified platform.

## Application Concepts
The following sections describes some of the high-level concepts and features sets of the Virtual Client platform.

### Workload/Test Profiles
Workload profiles define a set of one or more different ways to run a given workload or test on a system. The primary goal of these workload profiles is to evaluate the system
in a wide range of different ways. This in turn ensures that each round of execution of a workload on the Virtual Client platform can produce a breadth and depth of results
that are useful in comparing the performance of the system. Each of the workload profiles are tailored based on feedback from subject matter expert teams in the
Azure organization as well as from empirical evidence derived from running them in large-scale experiments. The Virtual Client has ran on more than
a million VM systems (Windows and Linux) in the Azure cloud.

* [Workloads and Profiles Supported](../overview/overview.md)  
* [Example Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-OPENSSL.json&version=GBmaster)
* [Profiles Supported](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles)
* [Usage Examples](../guides/0200-usage-examples.md)

Using the example below as a reference, there are a 3 different fundamental sections inside a workload profile:

* **Parameters**  
  At the top of most workload profiles are a set of one or more parameters. These parameters are referenced by each of the other
  components throughout the rest of the profile. Before a workload runs, all parameter references in a workload profile will be 
  replaced with the concrete values defined in the profile parameters at the top. Additionally, any parameters defined at the top
  of a workload profile can be overridden on the command line. This feature allows for flexibility in running workloads for different
  scenarios while not reducing the cohesion and validity of the goals that originally informed the design of the workload profile (this
  latter point is why not all parameters are allowed be overridden).

  ``` bash
  # An example of overriding the profile parameters so to use a smaller DiskSpd test file footprint. This might
  # for example be a scenario where the local/temp disk on an Azure virtual machine should be tested. These disks
  # are often very small and so the default 496G file size would be way to big.
  #
  # In the example JSON below, you will also see placeholders in the text of some of the step-specific parameters. These
  # placeholders will match the names of the parameters at the top and will be replaced with a value before executing
  # the step (e.g. [diskfillsize]).
  VirtualClient.exe --profile=PERF-IO-DISKSPD.json --timeout=1440 --parameters:DiskFillSize=30G,,,FileSize=30G
  ```

* **Actions**  
This section contains the workload execution workflow. Each of the steps in the 'Actions' section will be executed in sequential order. This section
allows a given workload (or more than 1) to be executed on the system in a wide range of ways to cover system performance and reliability
requirements thoroughly and with depth. Workloads and tests are often part of the actions as they are intended to utilize the system
and its resources in a measurable way. The term 'Executor' will be used often with components in the 'Actions' section.

* **Dependencies**  
This section contains steps for downloading and installing dependencies and for configuring the system to meet preliminary requirements for running
a specific workload. In the example below, there are 2 dependencies that must be accounted for before the workload is executed. All unformatted disks
on the system should be initialized and formatted. Additionally the package containing the workload binaries/executables itself should be downloaded
to the system from an Azure storage account blob store. Other examples of dependencies that are required include different Linux packages
required, libraries/frameworks (e.g. PowerShell 7.0), scripts and configuring the system itself.

``` json
{
    "Description": "DiskSpd I/O Stress Performance Workload",
    "Parameters": {
        "DiskFillSize": "500GB",
        "FileSize": "496G",
        "Tests": null
    },
    "Actions": [
      {
            "Type": "DiskSpdExecutor",
            "Parameters": {
                "Scenario": "DiskFill",
                "PackageName": "diskspd",
                "CommandLine": "-c[diskfillsize] -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L",
                "TestName": "disk_fill",
                "FileName": "diskspd-test.dat",
                "DiskFill": true,
                "DiskFillSize": "$.Parameters.DiskFillSize",
                "Tags": "IO,DiskSpd,randwrite",
                "ProcessModel": "SingleProcessPerDisk",
                "DeleteTestFilesOnFinish": false,
                "Tests": "$.Parameters.Tests"
            }
        }
    ],
    "Dependencies": [
        {
            "Type": "FormatDisks"
        },
        {
            "Type": "DependencyPackageInstallation",
            "Parameters": {
                "BlobContainer": "packages",
                "BlobName": "diskspd.1.2.0.zip",
                "PackageName": "diskspd",
                "Extract": true
            }
        }
    ]
}
```

### Monitoring Profiles
Monitoring profiles define a set of one or more different background system monitors to run during the execution of the application. For example, a common monitoring scenario 
is the need to capture performance counters from the system. Monitoring profiles are often used in conjunction with workload profiles to capture performance
and reliability information while workloads are executing in-parallel. The Virtual Client runs a default monitoring profile (MONITORS-DEFAULT.json) when a specific monitoring
profile is not provided. However, a different monitoring profile can be supplied on the command line if desired (extensibility).

* [Monitors and Profiles Supported](../overview/overview.md)  
* [Example Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/MONITORS-DEFAULT.json&version=GBmaster)
* [Profiles Supported](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles)

Using the example below as a reference, there are a 3 different fundamental sections inside a workload profile:

* **Parameters**  
  Parameters in monitoring profiles serve the same purpose as they do in workload profiles. See the 'Workload Profiles' section above
  for details.



  ``` bash
  # An example of overriding the profile parameters to capture
  #
  # In the example JSON below, you will also see placeholders in the text of some of the step-specific parameters. These
  # placeholders will match the names of the parameters at the top and will be replaced with a value before executing
  # the step (e.g. [diskfillsize]).
  VirtualClient.exe --profile=MONITORS-DEFAULT.json --timeout=1440 --parameters:CounterMonitorFrequency=00:05:00,,,CounterMonitorWarmupPeriod=00:00:30
  ```


* **Monitors**  
This section contains one or more background monitors to run on the system. These are often intended to be ran in the background while the
workloads defined in a workload profile are running. Each of the steps in the 'Monitors' section will be executed on a background thread to run
concurrently and independently of each other. The monitors will typically run independently from workloads that are running on the system as well.

* **Dependencies**  
Dependencies in monitoring profiles serve the same purpose as they do in workload profiles. See the 'Workload Profiles' section above
for details.

``` json
{
    "Description": "Default Monitors",
    "Parameters": {
        "CounterMonitorFrequency": "00:10:00",
        "CounterMonitorWarmupPeriod": "00:05:00"
    }
    "Dependencies": [
        {
          "Type": "AptPackageInstallation",
          "Parameters": {
            "Packages": "atop",
            "AllowUpgrades": true
          }
        }
    ],
    "Monitors": [
        {
            "Type": "PerfCounterMonitor",
            "Parameters": {
                "Scenario": "PerformanceCounterMonitoring",
                "MonitorFrequency": "$.Parameters.CounterMonitorFrequency",
                "MonitorWarmupPeriod": "$.Parameters.CounterMonitorWarmupPeriod"
            }
        }
    ]
}
```

### Workload Dependency Packages
The section above talked a bit about defining dependencies in Virtual Client workload and monitoring profiles. The Virtual Client platform has a model defined for how dependencies should
be packaged. Virtual Client workload and dependency packages follow the strict schema for the folder structure of the packages that allows for putting binaries, scripts and
other files in the package separated by their target runtime OS and architecture platforms (e.g. win-x64, win-arm64, linux-x64, linux-arm64). Workload and dependency
packages are typically stored in an Azure storage account blob store. However, Virtual Client also supports the ability to include the packages alongside the runtime platform
and they will be incorporated at runtime without need of download. 

In addition to workload and dependency packages stored in a Storage Account location, there are some packages that are packaged directly with the Virtual Client application itself.
The are called "built-in packages". There is no specific rhyme or reason to what is determined to qualify as a built-in package; however, they are as a general rule dependencies
that are needed by more than 1 workload and often operating-system specific libraries/toolsets/binaries. Technically, any of the workload packages and dependencies could be
bundled with the Virtual Client itself removing the requirement at runtime of downloading any packages. This is not done by default because it would cause the size of
the Virtual Client package to be very large. This is an issue for deployment simplicity and reliability in cloud environments. With that said, the Virtual Client
Official build pipeline can support producing different packages/bundles for the Virtual Client that contain more workloads built-in.

### Multi-Instance API Support
Certain workload scenarios require multiple systems to operate (e.g. client/server networking workloads and high-performance compute workloads). These workloads have a requirement to communicate
with each other to be able to synchronize client-side executions with server-side expectations. The Virtual Client has a self-hosted REST API that is used
for this purpose. This REST API enables simple HTTP communications between 2 or more different instances of the Virtual Client. The REST API provides the following
support:

* [Client/Server Support](../guides/0020-client-server.md)

* **State Management**  
  The API enables both the client and the server instances of the application to preserve state on the local system. Additionally, state objects/requests can be
  passed from the client to the server (or vice-versa) to be saved on the remote endpoint system. State is used primarily by workload executors to synchronize handshakes and
  requirements between the client and the server. For example, the server workload must be confirmed online and running in the NTttcp network throughput workload
  before the client can start. The client will perform a set of synchronization steps to ensure this is the case.

* **Heartbeats**  
  The API enables one instance of the Virtual Client to confirm another instance of the Virtual Client running on a different system is up and running. This is called
  a heartbeat.

* **Instructions/Eventing**  
  The API also enables one instance of the Virtual Client to send an event request to another instance of the application. An event is typically a request for the
  target endpoint instance to take an actions. For example in the NTttcp network throughput workload, the client will request that the server-side workload startup by sending it an
  event request. It will then poll for a particular server-side state to determine when the server-side workload is definitively up and running.

### Telemetry Support
One of the most important features of the Virtual Client platform is that it provides a lot of very useful telemetry. Telemetry emitted by the application follows a consistent schema 
(based on Application Insights) that is designed to enable strong correlation between data related to the process/system executing the application and data related to workloads and 
monitors.

Telemetry/data emitted by the Virtual Client application is divided into 4 different categories of data:

* **Traces/Logs/Errors**  
  The Virtual Client is heavily instrumented with structured logging/tracing logic. This ensures that the inner workings of the application can
  are easily visible to the user. This is particularly important for debugging scenarios.  Errors are a specific type of log/trace that indicate 
  issues that happen during the execution of Virtual Client commands. These represent situations that may be causing the Virtual Client to fail or 
  to work in unexpected ways.

  **Workload Metrics**  
  Workload metrics are measurements and information captured from the output of a particular workload (e.g. DiskSpd, FIO, Coremark) that represent
  performance data from the system under test. Metrics also include measurements of the system performance and reliability such as performance counters.

  **System Events**  
  System events describe certain types of important information on the system beyond simple performance measurements. This might for example
  include Windows registry changes or special event logs.

### Data Correlation
To enable correlation between data from an execution system and the Virtual Client, metadata is supplied to the Virtual Client on the command line. This is a simple way to "connect-the-dots"
when creating reports based on the data. This metadata will be included with every telemetry event/message that is emitted by the Virtual Client. The following shows an example of the schema 
and how metadata is supplied on the command line as well as what the contents of a single telemetry event emitted would look like.

* [Data/Telemetry Support](../guides/0040-telemetry.md)

``` bash
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=1440 --experimentId=2451d02e-b22b-4e8a-9a1f-5436512dbc01 --agentId=virtualmachine01 --metadata:"anyCorrelationId=identifier,,,property2=123,,,property3=true"
```

``` json
{
    "timestamp": "2021-04-19T16:01:31.9148275Z",
    "message": "OpenSSLExecutor.Execute",
    "severityLevel": "1",
    "itemType": "trace",
    "operation_Id": "6978d01d-b28d-4f81-8a0e-6296592dad07",
    "operation_ParentId": "00000000-0000-0000-0000-000000000000",
    "appName": "VirtualClient",
    "appHost": "virtualmachine01",
    "sdkVersion": "1.9.0.0",
    "customDimensions": {
        "agentId": "virtualmachine01",
        "experimentId": "2451d02e-b22b-4e8a-9a1f-5436512dbc01",
        "metadata": {
            "anyCorrelationId": "identifier",
            "property2": 123,
            "property3": true
        }
    }
}
```

### File Upload Support
In addition to having support for structured telemetry, the Virtual Client platform supports the ability to upload files/content to an Azure storage account
blob store. This is a need often enough with certain types of background monitors that produce very large results files too large to send through traditional
telemetry pipelines. Any component in the Virtual Client can be developed to upload files/content to a target blob store. Then the user of the application simply
passes in a connection string or SAS URI to the target "content" store on the command line.

* [Blob Store Support](../guides/0600-integration-blob-storage.md)

``` bash
VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --timeout=1440 --contentStore={ConnectionString or SASTokenUri}
```

## Application Development Concepts
The following sections cover some of the important application development concepts for contributing to the Virtual Client platform. Given the above concepts covered,
the next sections dive a bit more into the depths of the application (coding concepts).

### Implementation Concepts
The following concepts and terminology is used to describe the various coded components that exist in Virtual Client codebase.

* **Workload Executors**  
  A workload executor is a class/implementation that is responsible for managing initialization requirements and the execution of a given workload. Workloads can
  be vastly different than each other with regards to execution requirements (e.g. which OS they can operate on, command line parameters, length of run etc...). 
  The workload executor foundation in Virtual Client ensures that these details are all encapsulated into one or more executors specific to
  that workload. Furthermore, workload executors can be implemented to support parameterization for a wide range of different ways to run a
  given workload. Workload profiles in the Virtual Client platform will have one or more workload executors defined in the 'Actions' section.

* **Background Monitors**  
  A background monitor is a class/implementation that is responsible for gathering information from the system on a background thread during the lifetime of the
  Virtual Client application runtime (e.g. performance counters). Monitors are implemented to start a long-running background task but will return immediately so as
  to avoid blocking the main thread.

* **Dependency Installers/Handlers**  
  A dependency handler is a class/implementation that is responsible for downloading, installing and setting up dependencies on the system as preliminary requirements
  before any workloads or monitors are executed. If any one of the dependencies fail to perform their task, the Virtual Client application will exit returning a code that
  will indicate what actually failed.

* **Components**  
  All of the above noted types of classes/implementations in the Virtual Client codebase are collectively called "components". This is a generic
  term directly related to the base/fundamental class in the Virtual Client, the  [VirtualClientComponent](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/VirtualClientComponent.cs).
  All workload executors, background monitors and dependency handlers in the Virtual Client codebase derive from this class.

### Telemetry/Logging Concepts
To ensure consistency, a set of common extension methods are used to capture all telemetry from the operations of the application or the execution of workloads and monitors.

  * [Logging Extensions](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/VirtualClientLoggingExtensions.cs)