# Redis Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Redis workload.  

* [Workload Details](./Redis.md)  
* [Workload Profile Metrics](./RedisMetrics.md)  
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-REDIS.json
#### 1. Memtier Benchmarking Tool :
This tool can be used to generate various traffic patterns against Redis instances.
#### 2.Redis Benchmarking Tool:
The redis-benchmark program is a quick and useful way to get some figures and evaluate the performance of a Redis instance on a given hardware.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime on Linux Systems
    * (2-core/vCPU VM) = 30 minutes.( Depends on number of cores of the machine.)

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` csharp
  ./VirtualClient --profile=PERF-REDIS.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)