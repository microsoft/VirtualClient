---
slug: /monitors/monitor-profiles
---

# Monitor Profiles
The following sections describe the various monitor profiles that are available with the Virtual Client application. Monitor profiles are used to 
define the background monitors that will run on the system. Monitors are often ran in conjunction with workloads (defined in workload profiles) in
order to capture performance and reliability information from the system while workloads are running.

## MONITORS-DEFAULT.json
The default monitor profile for the Virtual Client. This profile captures performance counters on the system using one or more different specialized
toolsets. This monitor profile will be used when no other monitor profiles are specified on the command line.

* **Supported Platform/Architectures**  
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

* **Supported Platform/Architectures**  
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

* **Supported Platform/Architectures**  
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
