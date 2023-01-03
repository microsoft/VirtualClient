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

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

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
  | LocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run  |

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores. Although results
  are produced in a relatively short period of time, this particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime = 8 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-MLPERF.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```