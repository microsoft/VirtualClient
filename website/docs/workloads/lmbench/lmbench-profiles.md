# LMbench Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the LMbench workload.  

* [Workload Details](./lmbench.md)  

## PERF-MEM-LMBENCH.json
Runs a memory-intensive workload using the LMbench toolset to test the performance of the system RAM/memory. This profile is designed to identify general/broad 
regressions when compared against a baseline.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-LMBENCH.json)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 
  | CompilerParameters        | Optional. The parameters that will be passed to the compiler. | -O3 $ARRAY_FLAG -fopenmp -march=native -DNTIMES=5000

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (2-cores/vCPUs) = < 1 hour

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-MEM-LMBENCH.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```