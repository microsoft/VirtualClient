# SuperBenchmark Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SuperBenchmark workload.

* [Workload Details](./superbenchmark.md)  
* [Workload Profile Metrics](./superbenchmark-metrics.md)


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-GPU-SUPERBENCH.json
Runs the SuperBenchmark benchmark workload to test GPU performance. <mark>This workload is <b>supported ONLY for systems that contain nVidia GPU
hardware components</b>. Work is underway with partner teams in Azure to support additional GPU manufacturers.</mark>

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).
  * The VM must run on hardware containing Nvidia GPU cards/components.

* **Profile Parameters**  
  The following parameters can be supplied on the command line. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | ConfigurationFile     | Optional. The configuration file to use on the system providing all of the specifics required by Superbench for the particular GPU SKU. | default.yaml  |

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime = 4 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ``` bash
  VirtualClient.exe --profile=PERF-GPU-SUPERBENCH.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```


-----------------------------------------------------------------------


### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)