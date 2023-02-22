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


## Supported Commands
DCGM Diagnostics are designed to: 

Provide multiple test timeframes to facilitate different preparedness or failure conditions:

* Level 1 tests to use as a readiness metric
* Level 2 tests to use as an epilogue on failure
* Level 3 and Level 4 tests to be run by an administrator as post-mortem.

It is a single tool to discover deployment, system software and hardware configuration issues, basic diagnostics, integration issues, and relative system performance.

Commands supported are "dcgmi diag", "dcgmi discovery","dcgmi fieldgroup", " dcgmi group", "dcgmi modules", "dcgmi health" and CUDA Generator scenario.
```
dcgmi diag -r {level} -j
dcgmi discovery -l
dcgmi fieldgroup -l
dcgmi group -l
dcgmi modules -l
dcgmi health -c -j
CUDA generator scenario:
/usr/bin/dcgmproftester11 --no-dcgm-validation -t {FieldID} -d 10
dcgmi dmon -e {ListOfFieldIDs} -c 15
```

Please create a feature request if you need support for any other commands.


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
