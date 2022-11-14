# LAPACK Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the LAPACK workload.  

* [Workload Details](./lapack.md)  
* [Workload Profile Metrics](./lapack-metrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-CPU-LAPACK.json
Runs a CPU-intensive workload using the LAPACK toolset to test the performance of the CPU in processing different tests for fortran subroutines.
This profile is designed to identify general/broad regressions when compared against a baseline by testing routines that provide complete 
solutions for the most common problems of numerical linear algebra.

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
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for 1 to 2 hours extra runtime to ensure the tests can complete full test runs.

  * Expected Runtime on Linux Systems
    * (2-core/vCPU VM) = 1.5 hours
  * Expected Runtime on Windows Systems
    * (2-core/vCPU VM) = 1.5 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ``` bash
  VirtualClient.exe --profile=PERF-CPU-LAPACK.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```


### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)