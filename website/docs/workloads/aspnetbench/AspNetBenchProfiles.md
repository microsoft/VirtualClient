# AspNetBench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the AspNetBench workload.

* [Workload Details](./AspNetBench.md)  
* [Workload Profile Metrics](./AspNetBenchMetrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-ASPNETBENCH.json
Runs the AspNetBench benchmark workload to assess the performance of an ASP.NET Server.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 3 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ``` bash
  VirtualClient.exe --profile=PERF-ASPNETBENCH.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```


-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)