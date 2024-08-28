# SPEC CPU Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPEC CPU workload.

* [Workload Details](./speccpu.md)  

:::info
*SPEC CPU 2017 workloads are long running workloads. When running in Peak performance scenarios, some take more than 2 days to complete. Check the 'Profile Runtimes' section below for more details on what
to expect.*
:::

## PERF-SPECCPU-FPRATE.json
Runs the SPEC CPU Floating Point Rate (fprate) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECCPU-FPRATE.json)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

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
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc |
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 |
  | RunPeak                   | Optional. True to run the workload 'Peak' scenario, False to run the workload 'Base' scenario. | false (Base) |
  | BaseOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Base' scenario | -g -O3 -march=native -frecord-gcc-switches|
  | PeakOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Peak' scenario | -g -Ofast -march=native -flto -frecord-gcc-switches |

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * Base Scenario (8-cores/vCPUs) =~ 14 hours
  * Base scenario (48-cores/vCPUs) =~ 10 hours
  * Peak scenario (8-cores/vCPUs) =~ 28 hours
  * Peak scenario (48-cores/vCPUs) =~ 18 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` csharp
  # Execute the workload profile
  ./VirtualClient --profile=PERF-SPECCPU-FPRATE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## PERF-SPECCPU-FPSPEED.json
Runs the SPEC CPU Floating Point Speed (fpspeed) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECCPU-FPSPEED.json)

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

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
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc |
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 |
  | RunPeak                   | Optional. True to run the workload 'Peak' scenario, False to run the workload 'Base' scenario. | false (Base) |
  | BaseOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Base' scenario | -g -O3 -march=native |
  | PeakOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Peak' scenario | -g -Ofast -march=native -flto |


* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 
  
* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-SPECCPU-FPSPEED.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## PERF-SPECCPU-INTRATE.json
Runs the SPEC CPU Integer Rate (intrate) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECCPU-INTRATE.json)

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

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
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc |
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 |
  | RunPeak                   | Optional. True to run the workload 'Peak' scenario, False to run the workload 'Base' scenario. | false (Base) |
  | BaseOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Base' scenario | -g -O3 -march=native |
  | PeakOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Peak' scenario | -g -Ofast -march=native -flto |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-SPECCPU-INTRATE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## PERF-SPECCPU-INTSPEED.json
Runs the SPEC CPU Integer Speed (intspeed) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECCPU-INTSPEED.json)

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

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
  | CompilerName              | Optional. The name of the compiler to use for compiling CoreMark on the system. | gcc |
  | CompilerVersion           | Optional. The version of the compiler to use.  | 10 |
  | RunPeak                   | Optional. True to run the workload 'Peak' scenario, False to run the workload 'Base' scenario. | false (Base) |
  | BaseOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Base' scenario | -g -O3 -march=native |
  | PeakOptimizationFlags     | Optional. Optimization flags to pass to the GCC compiler when running the 'Peak' scenario | -g -Ofast -march=native -flto | 

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-SPECCPU-INTSPEED.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
