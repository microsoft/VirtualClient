# AMD SMI
The AMD System Management Interface (SMI) Library, or AMD SMI library, is a C library for Linux that provides a user space interface for applications to monitor and control AMD devices.

AMD SMI library supports Linux bare metal and Linux virtual machine guest for AMD GPUs. 
AMD SMI library can run on AMD ROCm supported platforms, refer to [System requirements (Linux)](https://rocm.docs.amd.com/projects/install-on-linux/en/latest/reference/system-requirements.html) for more information.

To run the AMD SMI library, the amdgpu driver and the hsmp driver needs to be installed. 

In command line, it can report information as CSV, JSON or human readable plain text to either standard output or a file. For more details, please refer to the amd-smi documentation.

* [AMD SMI Documentation](https://rocmdocs.amd.com/projects/amdsmi/en/latest/index.html)
* [AMD SMI CLI](https://rocmdocs.amd.com/projects/amdsmi/en/latest/how-to/using-AMD-SMI-CLI-tool.html)
* [AMD SMI Github Repo](https://github.com/ROCm/amdsmi)

## Dependency
This monitor has dependency on amd-smi. Please use [AMD Driver Installation] dependency first to make sure amd-smi is present on the system.

## Supported Platforms
* linux-x64
* win-x64

## Supported Query
The 2 subcommands supported are metric and xgmi. Please create a feature request if you need more subcommands or metrics parsed.

## amd-smi Output Description
The following section describes the various metrics that are available with amd-smi.

| Metric Name | Description |
|-------------|-------------|
| utilization.gpu | GPU Utilization in Percentage |
| utilization.memory | GPU Memory Utilization in Percentage |
| temperature.gpu | GPU temperature in Celsius |
| temperature.memory | GPU memory temperature in Celsius |
| power.draw.average | GPU Power Drawn in Watts |
| gfx_clk_avg | Averaged GPU Graphics Clock in MHz |
| mem_clk | GPU Memory Clock in MHz |
| video_vclk_avg | Averaged GPU Video VCLK Clock in MHz |
| video_dclk_avg | Averaged GPU Video DCLK Clock in MHz |
| pcie_bw | Current Bidirectional Bandwidth of PCIe Link of CPU to OAM  in MB/s |
| xgmi.bw | Current Total Bidirectional Bandwidth of 7 XGMI Links of OAM in MB/s |
| framebuffer.total | Total Frame Buffer in MB |
| framebuffer.used | Used Frame Buffer in MB |

### Example
This is an example of the minimum profile to run AmdSmiMonitor.

Remove the monitor with xgmi subsystem if GPU topology does not include xgmi links.

```json
{
  "Description": "AMD SMI Monitor for AMD GPU systems.",
  "Metadata": {
    "SupportedPlatforms": "linux-x64,win-x64",
    "SupportedOperatingSystems": "CBL-Mariner,CentOS,Debian,RedHat,Suse,Ubuntu,Windows"
  },
  "Parameters": {
    "MonitorFrequency": "00:00:01",
    "MonitorWarmupPeriod": "00:00:01"
  },
  "Monitors": [
    {
        "Type": "AmdSmiMonitor",
        "Parameters": {
            "Scenario": "AmdGpuCounters",
            "Subsystem": "metric",
            "MonitorFrequency": "$.Parameters.MonitorFrequency",
            "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
    },
    {
        "Type": "AmdSmiMonitor",
        "Parameters": {
            "Scenario": "AmdGpuCounters",
            "Subsystem": "xgmi",
            "MonitorFrequency": "$.Parameters.MonitorFrequency",
            "MonitorWarmupPeriod": "$.Parameters.MonitorWarmupPeriod"
        }
    }
  ]
}
```