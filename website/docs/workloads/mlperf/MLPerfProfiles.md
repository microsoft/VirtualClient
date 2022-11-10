# MLPerf Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the MLPerf workload.

* [Workload Details](./MLPerf.md)  
* [Workload Profile Metrics](./MLPerfMetrics.md)
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-GPU-MLPERF.json
Runs the MLPerf benchmark workload to test GPU performance. 

:::warn
This workload is supported ONLY for systems that contain Nvidia GPU hardware components
:::

* **Supported Platform/Architectures**
  * linux-x64



* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).
  * The VM must run on hardware containing Nvidia GPU cards/components.

* **Profile Parameters**  
  The following parameters can be supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | Username     | Optional. User which needs to be created in container to run MLPerf benchmarks. | testuser  |
  | DiskFilter     | Optional. Filter to decide the disk that will be used to download benchmarks. Since benchmarks data is around 800gb, default disk filter is greater than 1000gb. | SizeGreaterThan:1000gb  |
  | CudaVersion     | Optional. Version of CUDA that needs to be installed. | 11.6  |
  | DriverVersion     | Optional. Version of GPU driver that needs to be installed. | 510  |
  | LocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run  |


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime = 8 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ```bash
  ./VirtualClient --profile=PERF-GPU-MLPERF.json --system=Azure --timeout=1440
  ```

-----------------------------------------------------------------------


### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)