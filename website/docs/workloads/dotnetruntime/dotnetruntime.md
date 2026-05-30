# DotNetRuntime
The DotNetRuntime workload is a .NET program which mimics a 3-tier system with emphasis on the middle tier.The first tier is a set
of random input selections. This workload is a compute intensive workload and drives up the CPU utilization of the system that it is 
running on. It is representative of a middle tier system and is simplified for easy benchmarking.

* [DotNetRuntime Documentation](./dotnetruntime-workload-overview.docx)  

## What is Being Measured?
DotNetRuntime workload is designed to be a very simple benchmarking tool. It produces a measurement of **throughput** for each run of the workload 
on the system.

* Throughput in bops (billions of operations per second)

## Workload Metrics
The following metrics are produced by the DotNetRuntime workload itself.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|------|
| throughput  | 15937.97 | 16141.61 | 16047.287142857143 | bops |

## System Metrics
Different metrics are captured from the system depending upon which monitor profiles are used. If a monitor profile is not
defined, the default MONITORS-DEFAULT.json profile is used. See the following documentation to determine monitor profiles
that are available.

* [Monitor Profiles](https://github.com/microsoft/VirtualClient/blob/main/website/docs/monitors/monitor-profiles.md)
* [Monitor Profiles (internal only)](../../monitors/monitor-profiles.md)