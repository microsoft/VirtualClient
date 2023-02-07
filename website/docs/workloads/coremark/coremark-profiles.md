# CoreMark Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the CoreMark workload.  

* [Workload Details](./coremark.md)  

## Preliminaries
CoreMark workload profiles have no dependencies on a package store and so this information is not required on the command line for the profiles 
in the sections below.

## PERF-CPU-COREMARK.json
Runs a CPU-intensive workload using the CoreMark toolset to test the performance of the CPU. This profile is designed to identify general/broad regressions when 
compared against a baseline. CoreMark is an industry standard benchmarking toolset.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-COREMARK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

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
  | CompilerVersion           | Optional. The version of the compiler to use.                                   | 10 
  | ThreadCount               | Optional. Overwrites the default -DMULTITHREAD                                  | System Core Count  

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-CPU-COREMARK.json --system=Demo --timeout=1440
  
   # Override the profile default parameters to use a different GCC compiler version
  ./VirtualClient --profile=PERF-CPU-COREMARK.json --system=Demo --timeout=1440 --parameters="CompilerVersion=9"
  ```