# Data/Telemetry Support
The Virtual Client emits a range of different types of data/telemetry as part of the execution of workload and monitoring
profiles. This data/telemetry might for example include measurements/metrics emitted by a particular workload, performance counters
or just common tracing/logging output. This data is useful for using the Virtual Client as a platform for evaluating performance
of a system while under test.

## Categories of Data
Telemetry data emitted is divided into 3 different categories:

* **Logs/Traces**  
  The Virtual Client is heavily instrumented with structured logging/tracing logic. This ensures that the inner workings of the application can
  are easily visible to the user. This is particularly important for debugging scenarios. Errors experienced by the application are captured here
  as well and will contain detailed error + callstack information.

  **Workload and System Metrics**  
  Workload metrics are measurements and information captured from the output of a particular workload (e.g. DiskSpd, FIO, GeekBench) that represent
  performance data from the system under test. Performance counters for example provide measurements from the system as-a-whole and are useful for determining exactly how the resources (e.g. CPU, memory, I/O, network)
  were used during the execution of a workload.

  **System Events**  
  System events describe certain types of important information on the system beyond simple performance measurements. This might for example
  include Windows registry changes or special event logs.

## Log Files
The Virtual Client emits ALL data/telemetry captured from workloads, monitors and from the system to standard log files. Log files can be found 
in the **logs** directory within the Virtual Client application's parent directory itself. Logs are separated into the following categories:

- **Traces**  
  operational traces about everything the Virtual Client is doing while running useful for debugging/triage purposes.

- **Metrics**  
  Important measurements captured from the workload and the system that can be used to analyze the performance and reliability of the workload and correspondingly
  the system on which it is running.

- **Counters**  
  Similar to metrics capturing important measurements from the system itself that can be used to analyze the performance, reliability and resource usage on the system
  while the workload is running.

## Larger-Scale Scenarios
For larger-scale scenarios where the Virtual Client may be ran on many systems for long periods of time each producing a lot of data/telemetry. In these
scenarios, it is not that easy to gather log files from systems and to parse out the meaningful information from them required to analyze the performance
and reliability of the systems. Virtual Client supports options to integrate with "big data" cloud resources for these type of scenarios.

### Azure Event Hubs Support
Azure Event Hubs is a large-scale messaging platform available in the Azure cloud. The platform integrates with other data storage and analytics resources
such as Azure Data Explorer/Kusto and Azure Storage Accounts. This makes Event Hubs a very nice option for emitting data/telemetry from the operations
of the Virtual Client. Event Hubs can support both the scale and the need to aggregate/consolidate the data so that it can be readily analyzed. The Virtual Client
allows users to request data/telemetry be sent to a set of Event Hubs by supplying the connection string to the Event Hub Namespace on the command line.

See the following documentation for more information:
* [Event Hubs Integration](./0610-integration-event-hub.md)

### Azure Storage Account Support
An Azure Storage Account is a large-scale file/blob storage platform available in the Azure cloud. The platform is one of the most fundamental resources available
in the Azure cloud and it integrates with many other resources such as Azure Data Factory. The Virtual Client allows users to request files and logs content to 
be uploaded to a Storage Account by passing in a connection string or a SAS URI to the Storage Account on the command line.

See the following documentation for more information:
* [Storage Account Integration](./0600-integration-blob-storage.md)