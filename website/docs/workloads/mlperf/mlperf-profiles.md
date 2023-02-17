# MLPerf Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the MLPerf workload.

* [Workload Details](./mlperf.md)  

## PERF-GPU-MLPERF.json
Runs the MLPerf benchmark workload to test GPU performance. 

:::warn
*This workload is supported ONLY for systems that contain Nvidia GPU hardware components. See the documentation above for more specifics.*
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-MLPERF.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The VM must run on hardware containing Nvidia GPU cards/components.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | Username     | Optional. User which needs to be created in container to run MLPerf benchmarks. | testuser  |
  | DiskFilter     | Optional. Filter to decide the disk that will be used to download benchmarks. Since benchmarks data is around 800gb, default disk filter is greater than 1000gb. | SizeGreaterThan:1000gb  |
  | CudaVersion     | Optional. Version of CUDA that needs to be installed. | 11.6  |
  | DriverVersion     | Optional. Version of GPU driver that needs to be installed. | 510  |
  | LocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/12.0.0/local_installers/cuda_12.0.0_525.60.13_linux.run  |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-MLPERF.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```