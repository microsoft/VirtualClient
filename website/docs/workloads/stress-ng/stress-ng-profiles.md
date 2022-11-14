# StressNg Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the StressNg workload.

* [Workload Details](./stress-ng.md)  
* [Workload Profile Metrics](./stress-ng-metrics.md)  

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-SRESSNG.json
Runs the StressNg benchmark workload to assess the performance of a Java Server.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 1 minute

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ``` bash
  VirtualClient.exe --profile=PERF-STRESSNG.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```


-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)