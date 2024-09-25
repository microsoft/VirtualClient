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
Right now the query supported are --query-gpu and --query-c2c. Please create a feature request if you need other queries.

## nvidia-smi Output Description
The following section describes the various counters/metrics that are available with the nvidia-smi toolset.

| Metric Name | Description |
|-------------|-------------|
| utilization.gpu | GPU Utilization percentage. |
| utilization.memory | GPU Memory Utilization percentage. |
| temperature.gpu | GPU temperature in celsuis. |
| temperature.memory | GPU memory temperature in celsuis. |
| power.draw.average | Average GPU Power Draw in Watts. |
| clocks.gr | GPU Graphics Clock in MHz. |
| clocks.sm | GPU SM Clock in MHz. |
| clocks.video | GPU Video Clock in MHz. |
| clocks.mem | GPU Memory Clock in MHz. |
| memory.total | Total GPU Memory in MiB. |
| memory.free | Free GPU Memory in MiB. |
| memory.used | Used GPU Memory in MiB. |
| power.draw.instant | Instantaneous GPU Power Draw in Watts. |
| pcie.link.gen.gpucurrent | Current PCIe Link Generation. |
| pcie.link.width.current | Current PCIe Link Width. |
| ecc.errors.corrected.volatile.device_memory | Volatile Device Memory Corrected ECC Errors. |
| ecc.errors.corrected.volatile.dram | Volatile DRAM Corrected ECC Errors. |
| ecc.errors.corrected.volatile.sram | Volatile SRAM Corrected ECC Errors. |
| ecc.errors.corrected.volatile.total | Volatile Total Corrected ECC Errors. |
| ecc.errors.corrected.aggregate.device_memory | Aggregate Device Memory Corrected ECC Errors. |
| ecc.errors.corrected.aggregate.dram | Aggregate DRAM Corrected ECC Errors. |
| ecc.errors.corrected.aggregate.sram | Aggregate SRAM Corrected ECC Errors. |
| ecc.errors.corrected.aggregate.total | Aggregate Total Corrected ECC Errors. |
| ecc.errors.uncorrected.volatile.device_memory | Volatile Device Memory Uncorrected ECC Errors. |
| ecc.errors.uncorrected.volatile.dram | Volatile DRAM Uncorrected ECC Errors. |
| ecc.errors.uncorrected.volatile.sram | Volatile SRAM Uncorrected ECC Errors. |
| ecc.errors.uncorrected.volatile.total | Volatile Total Uncorrected ECC Errors. |
| ecc.errors.uncorrected.aggregate.device_memory | Aggregate Device Memory Uncorrected ECC Errors. |
| ecc.errors.uncorrected.aggregate.dram | Aggregate DRAM Uncorrected ECC Errors. |
| ecc.errors.uncorrected.aggregate.sram | Aggregate SRAM Uncorrected ECC Errors. |
| ecc.errors.uncorrected.aggregate.total | Aggregate Total Uncorrected ECC Errors. |
| GPU 0: C2C Link 0 Speed | C2C link speed in GB/s. |

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
          "Scenario": "CaptureNvidiaSmiCounters",
          "MonitorFrequency": "$.Parameters.MonitorFrequency",
          "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
      }
    ]
  }
```