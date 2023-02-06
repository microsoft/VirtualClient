# DCGMI Workload Profiles
The following profiles run DCGMI for qualifying GPUs.  

* [Workload Details](./dcgmi.md)  

## QUAL-GPU-DCGMI-DIAG.json
DCGM is part of the Nvidia GPU Deployment Kit and is designed to work with Nvidia's Tesla GPU accelerators, which are commonly used in data centers for high-performance computing and other GPU-accelerated workloads.
This profile is designed to identify general/broad regressions when compared against a baseline by validating few tests as part of Active health checks.

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * This monitor has dependency on Nvidia Driver Installation and nvidia-dcgm installation [[DCGMI installation](https://docs.nvidia.com/datacenter/dcgm/latest/user-guide/getting-started.html) - Version 3.1].

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**

  The following parameters can be optionally supplied on the command line to change this default behavior.

  | Parameter  |Purpose                | Default value |
  |------------|--------------------------|---------------|
  |  Username  | Optional. User which needs to be created in container to run MLPerf benchmarks. | testuser  |
  | CudaVersion     | Optional. Version of CUDA that needs to be installed. | 11.6  |
  | DriverVersion     | Optional. Version of GPU driver that needs to be installed. | 510  |
  | LocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run  |
  | Level | Optional. Which level of tests to run | 4 |


* **Profile Runtimes**  
  The runtime is dependent on the value of "Level" parameter. 

   | Level value | Runtime |
   |-------------|---------|
   | 4           | 1-2 hour|
   | 3           | 30 min  |
   | 2           | 2 min   |
   | 1           | few seconds |
 
    [Timings Documentation for DCGMI diag](https://docs.nvidia.com/datacenter/dcgm/latest/user-guide/dcgm-diagnostics.html#run-levels-and-tests)

* **Usage Examples**

  The following section provides a few basic examples of how to use the monitor profile.

  ```bash
  # Execute the monitor profile
  VirtualClient.exe --profile=QUAL-GPU-DCGMI-DIAG.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"


  VirtualClient.exe --profile=QUAL-GPU-DCGMI-DIAG.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=Level=1
  ```