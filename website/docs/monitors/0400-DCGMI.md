# DCGMI
The NVIDIA Data Center GPU Manager (DCGM) has DCGMI (Data Center GPU Management Interface) as a command line utility, which is a software tool for managing and monitoring GPU resources in a data center environment. DCGMi provides administrators with access to a wide range of information about the state of GPUs in their data center, including utilization, temperature, power consumption, and more.

DCGM is part of the Nvidia GPU Deployment Kit and is designed to work with Nvidia's Tesla GPU accelerators, which are commonly used in data centers for high-performance computing and other GPU-accelerated workloads.

At its heart, DCGM is an intelligent, lightweight user space library/agent that performs a variety of functions on each host system:

* GPU behavior monitoring
* GPU configuration management
* GPU policy oversight
* GPU health and diagnostics
* GPU accounting and process statistics
* NVSwitch configuration and monitoring

[DCGMI user guide](https://docs.nvidia.com/datacenter/dcgm/latest/user-guide/index.html)

## Dependency
This monitor has dependency on Nvidia Driver Installation and nvidia-dcgm installation [DCGMI installation].

## Supported Platforms
* linux-x64

## Profile Parameters
  The following parameters can be optionally supplied on the command line to change this default behavior.

  | Parameter  |Purpose                | Default value |
  |------------|--------------------------|---------------|
  |  Username  | Optional. User which needs to be created in container to run MLPerf benchmarks. | testuser  |
  | CudaVersion     | Optional. Version of CUDA that needs to be installed. | 11.6  |
  | DriverVersion     | Optional. Version of GPU driver that needs to be installed. | 510  |
  | LocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run  |
  | Level | Optional. Which level of tests to run | 4 |


## Supported Command
DCGM Diagnostics are designed to: 

Provide multiple test timeframes to facilitate different preparedness or failure conditions:

* Level 1 tests to use as a readiness metric
* Level 2 tests to use as an epilogue on failure
* Level 3 and Level 4 tests to be run by an administrator as post-mortem.

It is a single tool to discover deployment, system software and hardware configuration issues, basic diagnostics, integration issues, and relative system performance.

Right now the only command supported is 
```
dcgmi diag -r {level} -j
```

Please create a feature request if you need support for other commands.

## Usage Examples
  The following section provides a few basic examples of how to use the monitor profile.

  ```bash
  # Execute the monitor profile
  VirtualClient.exe --profile=PERF-GPU-DCGMI.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"


  VirtualClient.exe --profile=PERF-GPU-DCGMI.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Level=1
  ```


## DCGMI Output Description
The following section describes the various counters/metrics that are available with the dcgmi toolset.

| Metric Name | Description | Value |
|-------------|-------------|-------|
| Deployment_Denylist | checks if machine has expected deny list of processes to run on GPU| 1 |
| Deployment_NVML Library | NVML library access and versioning check | 1 |
| Deployment_CUDA Main Library | CUDA library access and versioning | 1 |
| Deployment_Permissions and OS Blocks | checks permissions for a specific GPU and enforce OS level block to ensure GPU resources are being used in secure way | 1 | 
| Deployment_Persistence Mode | checks the behaviour of GPU persistence mode, which allows GPUs to maintain their state even when a process terminates or the GPU is reset | 1 |
| Deployment_Environment Variables | checks the behaviour of environment variables, which are used to control and configure the behaviour of DCGM. | 1 |
| Deployment_Page Retirement/Row Remap | checks the behaviour of Page Retirement and Row Remap, which are advanced memory management features that can help to improve the reliability and stability of GPU-based applications. | 1 |
| Deployment_Graphics Processes | checks the behaviour of graphics processes, which are processes that are run on GPUs to perform graphical or computational tasks.
| Integration_PCIe | Verify PCIe connection, Monitor PCIe performance, Verify results | 1 |
| Deployment_Inforom | checks the behavior of the Inforom, which is a chip located on the GPU that provides information about the GPU, its configuration, and its performance. | 1 |
| Hardware_GPU Memory | checks the GPU memory behaviour which is used to store data and perform computations. | 1|
| Hardware_Diagnostic | diagnose any issues with the GPU and its components | 1 |
| Hardware_Pulse Test | performance test that is used to check the performance of the GPU | 1 |
| Stress_Targeted Stress |check the performance and stability under heavy load. | 1|
| Stress_Targeted Power | check the power consumption under different load conditions | 1|
| Hardware_EUD Test | check the error detection and correction capabilities of the GPU memory. | 1 |
| Stress_Memory Bandwidth | check the memory bandwidth performance of a GPU| 1|
| Stress_Memtest | stresses the GPU memory in order to identify any issues or errors | 1 |


```
NOTE: Value 1,-1,0 indicates pass, skip, fail of tests respectively.
```
