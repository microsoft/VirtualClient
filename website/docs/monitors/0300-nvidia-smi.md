# Nvidia SMI
The NVIDIA System Management Interface (nvidia-smi) is a command line utility, based on top of the NVIDIA Management Library (NVML), intended to aid in the management and monitoring of NVIDIA GPU devices. 

This utility allows administrators to query GPU device state and with the appropriate privileges, permits administrators to modify GPU device state.  It is targeted at the TeslaTM, GRIDTM, QuadroTM and Titan X product, though limited support is also available on other NVIDIA GPUs.

NVIDIA-smi ships with NVIDIA GPU display drivers on Linux, and with 64bit Windows Server 2008 R2 and Windows 7. Nvidia-smi can report query information as XML or human readable plain text to either standard output or a file. For more details, please refer to the nvidia-smi documentation.

* [NVIDIA System Management Interface](https://developer.nvidia.com/nvidia-system-management-interface)
* [nvidia-smi manual](https://developer.download.nvidia.com/compute/DCGM/docs/nvidia-smi-367.38.pdf)

## Dependency
This monitor has dependency on nvidia-smi. Please use [Nvidia Driver Installation] dependency first to make sure nvidia-smi is present on the system.

## Supported Platforms
* linux-x64
* linux-arm64

## Supported Query
Right now the only query supported is --query-gpu. Please create a feature request if you need other queries.

## nvidia-smi Output Description
The following section describes the various counters/metrics that are available with the nvidia-smi toolset.

| Metric Name | Description |
|-------------|-------------|
| temperature.gpu | GPU temperature in celsuis. |
| utilization.gpu [%] | GPU Utilization percentage. |
| utilization.memory [%] | GPU Memory Utilization percentage. |
| memory.total [MiB] | Total GPU Memory in MiB. |
| memory.free [MiB] | Free GPU Memory in MiB. |
| memory.used [MiB] | Used GPU Memory in MiB. |

### Example
This is an example of the minimum profile to run NvidiaSmiMonitor.

```json
{
    "Description": "Default Monitors",
    "Parameters": {
      "MonitorFrequency": "00:01:00",
      "MonitorWarmupPeriod": "00:01:00"
    },
    "Actions": [
    ],
    "Dependencies": [
    ],
    "Monitors": [
      {
        "Type": "NvidiaSmiMonitor",
        "Parameters": {
          "Scenario": "CaptureNvidiaGpuCounters",
          "MonitorFrequency": "$.Parameters.MonitorFrequency",
          "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
      }
    ]
  }
```