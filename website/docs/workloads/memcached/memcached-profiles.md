# Memcached Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Memcached workload.  

* [Workload Details](./memcached.md)  
* [Workload Profile Metrics](./memcached-metrics.md)  


-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-MEMCACHED.json
#### Memtier Benchmarking Tool :
This tool can be used to generate various traffic patterns against Memcached instances.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime on Linux Systems
    * (2-core/vCPU VM) = 2 hours.( Depends on number of cores of the machine.)

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ``` csharp
  ./VirtualClient --profile=PERF-MEMCACHED.json --system=Azure --timeout=250 --packageStore="{BlobConnectionString|SAS Uri}"
  ```


-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)