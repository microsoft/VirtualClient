# LMbench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the LMbench workload.  

* [Workload Details](./LMbench.md)  
* [Workload Profile Metrics](./LMbenchMetrics.md)
* [Workload Packages](./DependencyPackages.md)
* [Usage Scenarios/Examples](./UsageScenarios.md)

-----------------------------------------------------------------------

### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

-----------------------------------------------------------------------

### PERF-MEM-LMBENCH.json
Runs a memory-intensive workload using the LMbench toolset to test the performance of the system RAM/memory. This profile is designed to identify general/broad 
regressions when compared against a baseline.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64


* **Dependencies**  
  The following dependencies must be met to run this workload profile.

  * Workload package must exist in the 'packages' directory or connection information for the package store supplied on the command line (see 'Workload Packages' link above).

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 
  | CompilerParameters        | Optional. The parameters that will be passed to the compiler. | -O3 $ARRAY_FLAG -fopenmp -march=native -DNTIMES=5000

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for 1 to 2 hours extra runtime to ensure the tests can complete full test runs.

  * Expected Runtime on Linux Systems
    * (2-core/vCPU VM) = < 1 hour

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ```bash
  VirtualClient.exe --profile=PERF-MEM-LMBENCH.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

  **Notes**
  For some systems, workload is giving segmentation fault while calculating cache line size.