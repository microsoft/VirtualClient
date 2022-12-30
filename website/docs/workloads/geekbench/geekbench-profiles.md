# GeekBench5 Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the GeekBench5 workload.

* [Workload Details](./GeekBench.md)  
* [Workload Profile Metrics](./GeekBenchMetrics.md)
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-CPU-GEEKBENCH.json
Runs a CPU-intensive workload using the GeekBench5 toolset to test the performance of the CPU across various types of common application algorithms 
(e.g. Gaussian Blur, AES-XTS, Text Compression, Image Compression). This profile is designed to identify general/broad regressions when compared 
against a baseline. GeekBench is an industry standard benchmarking toolset.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime on Linux Systems
    * (2-core/vCPU VM) = 2 - 3 minutes
  * Expected Runtime on Windows Systems
    * (2-core/vCPU VM) = 2 - 4 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


``` csharp
VirtualClient.exe --profile=PERF-CPU-GEEKBENCH.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
```
