---
slug: /monitors/monitor-profiles
sidebar_position: 1
---

# Monitor Profiles
The following sections describe the various monitor profiles that are available with the Virtual Client application. Monitor profiles are used to 
define the background monitors that will run on the system. Monitors are often ran in conjunction with workloads (defined in workload profiles) in
order to capture performance and reliability information from the system while workloads are running.

## MONITORS-AZURE-HOST.json
This compatibility profile captures IPMIUtil sensors, BMC SEL records, and Windows performance counters on an Azure host or blade. It preserves the monitor names, intervals, event filter, and counter definitions from the profile previously distributed by the internal Virtual Client extension.

The profile supports `win-x64` and `win-arm64` and requires direct access to an IPMI-capable physical host. IPMIUtil must already be available on `PATH`, in the legacy BladeFX tool location, or in an installed `host.monitors` package.

See the [IPMIUtil monitor documentation](/docs/monitors/ipmiutil) for hardware requirements and emitted telemetry.

## MONITORS-DEFAULT.json
The default monitor profile for the Virtual Client. This profile captures performance counters on the system using one or more different specialized
toolsets. This monitor profile will be used when no other monitor profiles are specified on the command line.

* **Supported Platform-Architectures**  
  Counters captured on Linux systems using Atop application. Counters captured on Windows systems using the .NET SDK.

  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Dependencies**  
  * Linux systems must have an internet connection in order to install the Atop application if not already installed on the system.

* **Scenarios**  
  * [Performance Counters](/docs/monitors/performance-counters)
  * Captures the following information on Linux systems: 
    * Performance counters using the [Atop](/docs/monitors/atop) application.
    * Standard output of various toolsets including: hostnamectl, lscpu, lshw, lspci.
    * System logs at 'Error' severity (journalctl).
  * Captures the following information on Windows systems:
    * Performance counters using the .NET SDK.
    * Event logs at 'Error' severity in the following system logs: Application, Security, System
    * Standard output of various toolsets including: ipconfig, systeminfo, pnputil

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to change this default behavior.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Scenario                  | Optional. A description of the purpose of the monitor within the overall profile workflow. |    |
  | MonitorFrequency          | Optional. Defines the frequency (timespan) at which performance counters will be captured/emitted (e.g. 00:01:00). | 00:05:00 |
  | MonitorWarmupPeriod       | Optional. Defines a period of time (timespan) to wait before starting to track/capture performance counters (e.g. 00:03:00). This allows the system to get to a more typical operational state and generally results better representation for the counters captured. | 00:05:00 |

* **Profile Runtimes**  
  1 iteration of the profile = ~5 mins. The profile will begin capturing and emitting information within 5 minutes.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # Run the monitoring facilities only.
  VirtualClient.exe --profile=MONITORS-DEFAULT.json --logger=csv --log-to-file

  # Runs the default monitor profile.
  VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --system=Demo --timeout=1440 --logger=csv --log-to-file

  # Monitor profile explicitly defined.
  VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --profile=MONITORS-DEFAULT.json --system=Demo --timeout=1440 --logger=csv --log-to-file
  ```

## MONITORS-GPU-AMD.json
The monitor profile designed for AMD GPU systems.

<mark>
Note that this profile requires the AMD GPU driver to be already installed on the system. The profile does not attempt to install the GPU driver or 
any of the dependencies required by the driver. If the driver is not already installed, then this profile will fail to capture monitoring information.
</mark>

* **Supported Platform-Architectures**  
  * linux-x64
  * linux-arm64

* **Dependencies**  
  * The system must have AMD GPU driver installed.

* **Scenarios**  
  * Captures AMD GPU performance counters on Linux systems using the AMD-SMI toolset.

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to change this default behavior.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Scenario                  | Optional. A description of the purpose of the monitor within the overall profile workflow. |    |
  | MonitorFrequency          | Optional. Defines the frequency (timespan) at which performance counters will be captured/emitted (e.g. 00:01:00). | 00:05:00 |
  | MonitorWarmupPeriod       | Optional. Defines a period of time (timespan) to wait before starting to track/capture performance counters (e.g. 00:03:00). This allows the system to get to a more typical operational state and generally results better representation for the counters captured. | 00:05:00 |
  | MetricFilter              | Optional. A comma-delimited list of performance counter names to capture. The default behavior is to capture/emit all performance counters (e.g. \Processor Information(_Total)\% System Time,\Processor Information(_Total)\% User Time). This allows the profile author to focus on a smaller/specific subset of the counters. This is typically used when a lower monitor frequency is required for higher sample precision to keep the size of the data sets emitted by the Virtual Client to a minimum. | |

* **Profile Runtimes**  
  1 iteration of the profile = ~5 mins. The profile will begin capturing and emitting information within 5 minutes.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top. Note that the AMD GPU driver must be already installed on the system.

  ``` bash
  # Run the monitoring facilities only.
  ./VirtualClient --profile=MONITORS-GPU-AMD.json --logger=csv --log-to-file
  ```

## MONITORS-GPU-NVIDIA.json
The monitor profile designed for Nvidia GPU systems. The profile captures counters on Linux systems of Nvidia GPUs with nvidia-smi, and lspci utilities.

<mark>
Note that this profile requires the Nvidia GPU driver and CUDA toolsets to be already installed on the system. The profile does not attempt to install the GPU driver or 
any of the dependencies required by the driver. If the driver is not already installed, then this profile will fail to capture monitoring information.
</mark>

* **Supported Platform-Architectures**  
  * linux-x64
  * linux-arm64

* **Dependencies**  
  * The system must have Nvidia GPU driver with CUDA installed.

* **Scenarios**  
  * Captures Nvidia GPU performance counters on Linux systems using [nvidia-smi](/docs/monitors/nvidia-smi)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to change this default behavior.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | Scenario                  | Optional. A description of the purpose of the monitor within the overall profile workflow. |    |
  | MonitorFrequency          | Optional. Defines the frequency (timespan) at which performance counters will be captured/emitted (e.g. 00:01:00). | 00:05:00 |
  | MonitorWarmupPeriod       | Optional. Defines a period of time (timespan) to wait before starting to track/capture performance counters (e.g. 00:03:00). This allows the system to get to a more typical operational state and generally results better representation for the counters captured. | 00:05:00 |
  | MetricFilter              | Optional. A comma-delimited list of performance counter names to capture. The default behavior is to capture/emit all performance counters (e.g. \Processor Information(_Total)\% System Time,\Processor Information(_Total)\% User Time). This allows the profile author to focus on a smaller/specific subset of the counters. This is typically used when a lower monitor frequency is required for higher sample precision to keep the size of the data sets emitted by the Virtual Client to a minimum. | |

* **Profile Runtimes**  
  1 iteration of the profile = ~5 mins. The profile will begin capturing and emitting information within 5 minutes.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top. Note that the Nvidia GPU driver and CUDA toolsets must be already installed on the system.

  ``` bash
  # Run the monitoring facilities only.
  ./VirtualClient --profile=MONITORS-GPU-NVIDIA.json --logger=csv --log-to-file
  ```

## MONITORS-ETW.json
This profile runs both the kernel and non-kernel [Windows ETW monitor](/docs/monitors/windows-etw) configurations.

* **Supported Platform-Architectures**
  * win-x64
  * win-arm64

* **Scenarios**
  * `CaptureKernelETWTraces` captures process start, process stop, and image-load events from the Windows kernel provider.
  * `CaptureNonKernelETWTraces` captures selected garbage-collection events from the .NET runtime provider.

* **Profile Parameters**

  | Parameter | Purpose | Default value |
  |-----------|---------|---------------|
  | ProfilingEnabled | Enables or disables ETW collection. | `true` |
  | ProfilingMode | Collection mode (`Interval` or `OnDemand`). | `Interval` |
  | ProfilingPeriod | Length of each interval collection period. | `00:05:00` |
  | ProfilingInterval | Delay between interval collection periods. | `00:00:30` |
  | ProfilingWarmUpPeriod | Delay before interval collection starts. | `00:00:10` |

* **Usage Examples**

  ```powershell
  # Run both ETW monitor configurations.
  VirtualClient.exe --profile=MONITORS-ETW.json --timeout=00:30:00 --log-to-file

  # Run ETW monitoring with a workload profile.
  VirtualClient.exe --profile=PERF-NETWORK.json --profile=MONITORS-ETW.json --timeout=00:30:00 --log-to-file
  ```

## MONITORS-ETW-KERNEL.json
This profile runs the [Windows ETW monitor](/docs/monitors/windows-etw) for the Windows kernel provider. It captures `ImageLoad`,
`ProcessStart`, and `ProcessStop` events enabled by the `ImageLoad` and `Process` keywords.

The profile supports win-x64 and win-arm64 and exposes the same profile parameters documented for
[MONITORS-ETW.json](#monitors-etwjson).

```powershell
VirtualClient.exe --profile=MONITORS-ETW-KERNEL.json --timeout=00:30:00 --log-to-file
```

## MONITORS-ETW-NONKERNEL.json
This profile runs the [Windows ETW monitor](/docs/monitors/windows-etw) for the `Microsoft-Windows-DotNETRuntime` provider. It captures
`GCSetGCHandle` and `GCTriggered` events at the configured levels.

The profile supports win-x64 and win-arm64 and exposes the same profile parameters documented for
[MONITORS-ETW.json](#monitors-etwjson).

```powershell
VirtualClient.exe --profile=MONITORS-ETW-NONKERNEL.json --timeout=00:30:00 --log-to-file
```

## MONITORS-IPMIUTIL.json
The IPMIUtil monitor profile captures BMC sensor measurements and System Event Log records throughout a Virtual Client run.

* **Supported Platform-Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Dependencies**
  * A supported IPMI/BMC interface and sufficient privileges to access it.
  * Linux access to an operating-system package repository that provides `ipmiutil`.
  * Windows access to the Virtual Client package store containing `host.monitors.1.0.1.zip`.

* **Scenarios**
  * Capture numeric BMC sensor readings.
  * Capture and combine decoded and raw BMC SEL records.
  * Optionally preserve suspected hardware events and clear the SEL.

See the [IPMIUtil monitor documentation](/docs/monitors/ipmiutil) for metrics, parameters, setup requirements, and examples.
