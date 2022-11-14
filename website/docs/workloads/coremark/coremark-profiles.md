# CoreMark Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the CoreMark workload.  

* [Workload Details](./coremark.md)  
* [Workload Profile Metrics](./coremark-metrics.md)

-----------------------------------------------------------------------

### Preliminaries
CoreMark workload profiles have no dependencies on a package store and so this information is not required on the command line for the profiles 
in the sections below.

-----------------------------------------------------------------------

### PERF-CPU-COREMARK.json
Runs a CPU-intensive workload using the CoreMark toolset to test the performance of the CPU. This profile is designed to identify general/broad regressions when 
compared against a baseline. CoreMark is an industry standard benchmarking toolset.

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload. See the 'Usage Scenarios/Examples' above for examples on how to supply parameters to 
  Virtual Client profiles.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.
  It is practical to allow for 1 to 2 hours extra runtime to ensure the tests can complete full test runs.

  * Expected Runtime on Linux systems
    * (2-core/vCPU VM) = 1 - 2 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.



  ```
  ./VirtualClient --profile=PERF-CPU-COREMARK.json --system=Azure --timeout=1440
  
   # Use a different GCC compiler version
  ./VirtualClient --profile=PERF-CPU-COREMARK.json --system=Azure --timeout=1440 --parameters=CompilerVersion=9
  ```


-----------------------------------------------------------------------

### Resources
* [Azure VM Sizes](https://docs.microsoft.com/en-us/azure/virtual-machines/sizes)
* [Azure Managed Disks](https://azure.microsoft.com/en-us/pricing/details/managed-disks/)