# Profiler Integration
Profilers are applications/programs that are used to capture very detailed information for operations that are happening on the
system. For example, the [Azure Profiler](https://eng.ms/docs/products/azure-profiler/azure-profiler) is a toolet that captures
detailed information for various types of low level operations on the system such as CPU callstacks, memory usage, and context
switches. The following sections cover support that exists in the Virtual Client for profilers as well as how that is enabled in
workload and monitoring profiles.

## Preliminaries
The following pieces of documentation are helpful to understand before implementing any new monitor or profiler support in the
Virtual Client.

* [Platform Overview](https://github.com/microsoft/VirtualClient/blob/main/website/docs/overview/features.md)
* [Platform Design Aspects](https://github.com/microsoft/VirtualClient/blob/main/website/docs/overview/design.md)

Note that there is a project in the Virtual Client solution that showcases both interval-based as well as on-demand profiling
scenarios. From Visual Studio, you can set the project [VirtualClient.Examples](https://msazure.visualstudio.com/One/_git/CRC-AIR-Workloads?path=/src/VirtualClient/VirtualClient.Examples)
as the startup project and run it. Select option #2 on the initial menu to see an example of interval-based profiling. Select option #3
on the initial menu to see an example of interval-based monitoring. These are just examples so no actual system profiling is happening,
but this illustrates the code flow and can be debugged in the Visual Studio IDE.

## Supported Profilers
The following profilers are currently supported by the Virtual Client:

* [Azure Profiler](https://eng.ms/docs/products/azure-profiler/azure-profiler)

## Profiling Scenarios
The Virtual Client supports a few different profiling scenarios designed to enable flexibility with the capture of information from the
system:

* **Interval-Based Profiling**  
  Interval-based profiling involves running a profiler on consistent intervals constantly throughout the runtime execution of the Virtual
  Client.

* **On-Demand Profiling**  
  On-demand profiling involves running a profiler only when signaled and for a length of time defined by the component that initiated the
  signal. For example, a workload executor might want to have profiling run in the background for the duration that the workload itself runs
  and no longer.

It is recommended that you consider implementing both scenarios in monitors that run profilers. Both scenarios are useful for various needs
when running on Azure guests/VMs vs. Azure hosts/blades. For example, it is desirable to run profiling during the execution of a specific workload
on a guest/VM while running a steady/constant profiling on the host/blade underneath.

## Interval-Based Profiling
The exact nature of how a given profiler works is dependent upon how it is implemented. The following section intends to provide a recommended
approach to how a background monitor that runs a profiler should work when supporting interval-based profiling.

### Recommendations:
Note that the recommendations below are largely implementation details. 

* Use the following examples for reference:
  * [Example Monitor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Monitors/ExampleProfilingMonitor.cs)

* The profiler should be implemented as a background monitor so that it can be defined in a monitoring profile in the "Monitors" section.
* The specifics of the interval are defined in a monitoring profile. Monitoring profiles define a set of one or more monitors to run and any
  parameters that control the behaviors of the monitors. The following parameters are commonly used to define behaviors for interval-based profilers:

  * **ProfilingEnabled**  
    Defines true/false whether background profiling is enabled. This determines if the profiling toolsets associated with the monitors will be run.

  * **ProfilingMode**  
    Defines how/when profiling operations will occur (Interval vs. OnDemand). For interval-based profiling, this will be set to 'Interval'. If the
    monitor supports both types of profiling, a profile-level parameter should be defined allowing this value to be overridden/defined on the command
    line (see the 'ExampleProfilerMode' parameter in the example below). Default = None (i.e. no specific profiling mode).

  * **ProfilingPeriod**  
    Defines how long the profiler will run/execute to capture information from the system. For interval-based profiling, this will be a consistent
    period throughout the runtime execution of the Virtual Client. Default = TimeSpan.Zero (i.e. no profiling period).

  * **ProfilingWarmUpPeriod**  
    Defines a period of time to wait before executing the profiler operations. After this period of time, the profiler will run for the amount of
    time specified by the 'ProfilingPeriod'. Default = TimeSpan.Zero (i.e. no warm-up period).

  * **ProfilingInterval**  
    Defines the interval to wait between each execution of the profiler. The profiler will execute at the beginning of each interval and will run
    for a period of time as defined by the 'ProfilingPeriod'. For example given a 1 minute interval and 30 second profiling period, a profiler starting
    at 12:00:00 AM would run until 12:00:30 AM and would then wait until 12:01:00 AM before running again (for another 30 seconds). 
    Default = TimeSpan.Zero (i.e. profiler runs on non-stop intervals).

  * **A Parameter to Enable Disable the Monitor**  
    Each individual monitor that runs a profiler should have a unique parameter that defines whether the profiler is enabled or not. Additionally, a
    profile-level parameter should be defined so that this can be overridden/defined on the command line (see the 'ExampleProfilerEnabled' parameter in
    the example below). Default = true (profiler is enabled by default).

  <div class="code-section">

  ``` json
  {
    "Description": "Example Monitoring Profile That Supports Interval + On-Demand Profiling.",
    "Parameters": {
        "ProfilingEnabled": true,
        "ProfilingMode": "Interval"
    },
    "Monitors": [
        {
            "Type": "ExampleWorkloadProfilingScenarioMonitor",
            "Parameters": {
                "Scenario": "ProfileSystem",
                "ExampleProfilerEnabled": "$.Parameters.ProfilingEnabled",
                "ProfilingMode": "$.Parameters.ProfilingMode",
                "ProfilingPeriod": "00:00:30",
                "ProfilingInterval": "00:00:45",
                "Tags": "Test,VC"
            }
        }
    ]
  }
  ```
  </div>

## On-Demand Profiling
The exact nature of how a given profiler works is dependent upon how it is implemented. The following section intends to provide a recommended
approach to how a background monitor that runs a profiler should work when supporting on-demand profiling. The Virtual Client utilizes standard
.NET events to support on-demand behaviors. This enables real-time support for in-process notifications between workload executors and background
monitors.

* [VirtualClientRuntime Class](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/VirtualClientRuntime.cs)
* [.NET Events](https://docs.microsoft.com/en-us/dotnet/standard/events/)

### Recommendations:
Note that the recommendations below are largely implementation details. In this implementation requirement there are one or more monitors that run profilers
defined in a monitoring profile who simply listen until signaled. The workload executors are defined in a workload profile. The workload executors
send signals to the monitors/profilers to run using simple .NET events where "instructions" are passed to the monitors/profilers.

* Use the following examples for reference:
  * [Example Workload Executor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Examples/ExampleWorkloadProfilingScenarioExecutor.cs)
  * [Example Monitor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Examples/ExampleWorkloadProfilingScenarioMonitor.cs)
  * [Example Profiles](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Examples/profiles)

* The profiler should be implemented as a background monitor so that it can be defined in a monitoring profile in the "Monitors" section.
* The monitor should be defined in a monitoring profile (separate from any workload profiles).
* Parameters that define the behaviors of the on-demand monitor/profiler should be defined in a workload profile within the 'Parameters' section
  of each individual workload 'Action'. The following parameters are commonly used to define behaviors for on-demand profilers:

  * **ProfilingEnabled**  
    Defines true/false whether on-demand profiling is enabled for a particular workload 'Action'. A profile-level parameter should be defined that
    allows this value to be overridden/defined on the command line (see the 'OnDemandProfilingEnabled' parameter below).

  * **ProfilingMode**  
    Defines how/when profiling operations will occur (Interval vs. OnDemand). For interval-based profiling, this will be set to 'Interval'. If the
    monitor supports both types of profiling, a profile-level parameter should be defined allowing this value to be overridden/defined on the command
    line (see the 'ExampleProfilerMode' parameter in the example below). Default = None (i.e. no specific profiling mode).

  * **ProfilingPeriod**  
    Defines how long the profiler will run/execute to capture information from the system. For interval-based profiling, this will be a consistent
    period throughout the runtime execution of the Virtual Client.

  * **ProfilingWarmUpPeriod**  
    Defines a period of time to wait before executing the profiler operations. After this period of time, the profiler will run for the amount of
    time specified by the 'ProfilingPeriod'.

  * **Cancellation on Workload Completion**  
    The monitor/profiler should honor the CancellationToken provided to it in the instructions sent by the workload executor when the 'ProfilingPeriod'
    is not defined (i.e. period == TimeSpan.Zero). This enables support for running the profiler for the entire length of time that the workload runs
    and stopping it promptly when the workload itself finishes.

  <div class="code-section">

  ``` json
  {
    "Description": "Example Workload Profile That Supports On-Demand Profiling While Workload Run.",
    "MinimumExecutionInterval": "00:00:05",
    "Parameters": {
        "ProfilingEnabled": false
    },
    "Actions": [
        {
            "Type": "ExampleWorkloadProfilingScenarioExecutor",
            "Parameters": {
                "Scenario": "Scenario1",
                "WorkloadRuntime": "00:01:00",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "ProfilingPeriod": "00:00:30",
                "ProfilingWarmUpPeriod": "00:00:10",
                "Tags": "Test,VC"
            }
        },
        {
            "Type": "ExampleWorkloadProfilingScenarioExecutor",
            "Parameters": {
                "Scenario": "Scenario2",
                "WorkloadRuntime": "00:00:30",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "ProfilingPeriod": "00:00:30",
                "ProfilingWarmUpPeriod": "00:00:00",
                "Tags": "Test,VC"
            }
        },
        {
            "Type": "ExampleWorkloadProfilingScenarioExecutor",
            "Parameters": {
                "Scenario": "Scenario3",
                "WorkloadRuntime": "00:00:45",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "ProfilingWarmUpPeriod": "00:00:00",
                "Tags": "Test,VC"
            }
        }
    ]
  }
  ```
  </div>

## Bringing it All Together
The documentation above covers the essentials for how profilers are integrated into the Virtual Client application. One final thing to cover is the
extent to which this integration applies. There is logic in the base [VirtualClientComponent](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Contracts/VirtualClientComponent.cs) class
that ensures this behavior is available for all workload executors that exist in the application. There is no need to make any code changes to individual
workload executors to enable profiling. Thus the parameters noted above in the section for on-demand profiling can be added to the parameters for and of 
the workload 'Actions' to enable support for monitors/profilers supported by the application. Once these parameters have been added to an existing profile,
the user/automation can run workloads with profiling by simply defining the related profiles on the command line...a workload profile and a monitoring profile.
The two profiles will be merged into one.

<div class="code-section">

```
VirtualClient.exe --profile=PERF-NETWORK.json --profile=MONITORS-PROFILING.json --timeout=180 --packages="{BlobStoreConnectionString|SAS URI}" --parameters="OnDemandProfilingEnabled=true,,,AzureProfilerMode=OnDemand"
```

``` json
# PERF-NETWORK.json

{
    "Description": "Azure Networking Workload",
    "Parameters": {
        "ProfilingEnabled": false,
        "ProfilingMode": "None"
    },
    "Actions": [
        {
            "Type": "NetworkingWorkloadSetupExecutor"
        },
        {
            "Type": "NetworkingWorkloadExecutor",
            "Parameters": {
                "Scenario": "NTttcp_TCP_4K_Buffer_T1",
                "ToolName": "NTttcp",
                "PackageName": "Networking",
                "Protocol": "TCP",
                "ThreadCount": 1,
                "BufferSize": "4K",
                "TestDuration": "60",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "PowerShell7PackageName": "PowerShell7"
            }
        },
        {
            "Type": "NetworkingWorkloadExecutor",
            "Parameters": {
                "Scenario": "NTttcp_TCP_64K_Buffer_T1",
                "ToolName": "NTttcp",
                "PackageName": "Networking",
                "Protocol": "TCP",
                "ThreadCount": 1,
                "BufferSize": "64K",
                "TestDuration": "60",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "PowerShell7PackageName": "PowerShell7"
            }
        },
        {
            "Type": "NetworkingWorkloadExecutor",
            "Parameters": {
                "Scenario": "Latte_TCP",
                "ToolName": "Latte",
                "PackageName": "Networking",
                "Protocol": "TCP",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "PowerShell7PackageName": "PowerShell7"
            }
        },
        {
            "Type": "NetworkingWorkloadExecutor",
            "Parameters": {
                "Scenario": "CPS",
                "ToolName": "CPS",
                "PackageName": "Networking",
                "Connections": "16",
                "TestDuration": "300",
                "WarmupTime" : "60",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "PowerShell7PackageName": "PowerShell7"
            }
        }
        ...
    ]
}
```

``` json
# MONITORS-PROFILING.json

{
    "Description": "Monitors for Profiling the System",
    "Parameters": {
        "ProfilingEnabled": true,
        "ProfilingMode": "Interval"
    },
    "Monitors": [
        {
            "Type": "AzureProfilerMonitor",
            "Parameters": {
                "Scenario": "CaptureProfiles",
                "ProfilingEnabled": "$.Parameters.ProfilingEnabled",
                "ProfilingMode": "$.Parameters.ProfilingMode",
                "ProfilingPeriod": "00:00:30",
                "ProfilingInterval": "00:01:00",
                "ProfilingWarmupPeriod": "00:00:00",
                "ProfileDataUploadInterval": "00:00:30"
            }
        }
        ...
    ]
}
```
</div>

## Profiling Integration Code Requirements
It is necessary to make a small set of code changes to any workload executor that should support on-demand profiling. Note that this does not apply nor is
it required to support interval-based profiling. Interval-based profiling takes no dependency on the workload executors for signals. The following section
covers the code changes that must be made to a workload executor.

* Use the following examples for reference:
  * [Example Workload Executor](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Examples/ExampleWorkloadProfilingScenarioExecutor.cs)
  * [BackgroundProfiling Class](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Core/BackgroundProfiling.cs)

### Add a BackgroundProfiling Block
Each workload executor might perform a number of different operations before executing an actual workload. In order to allow each workload executor to be 
instrumented such that on-demand profiling happens at exactly the right/desired moment, a BackgroundProfiling block will be added. The following example
shows how to do this:

<div class="code-section">

``` csharp
 protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
{
    try
    {
        using (BackgroundProfiling profiling = BackgroundProfiling.Begin(this, cancellationToken))
        {
            using (process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, "C:\any\path\to\workload.exe", arguments))
            {
                // Workload will be executed here. This ensures that profiling will run during period precisely when
                // the workload itself is running and can be stopped as soon as the workload exits. The exact behavior
                // depends upon how the background monitor running the profiler is implemented. However, these type of
                // background monitors are expected to be implemented following the recommendations above.
                 await process.StartAndWaitAsync(cancellationToken, timeout)
                     .ConfigureAwait(false);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when a Task.Delay is cancelled.
    }
}

```
</div>