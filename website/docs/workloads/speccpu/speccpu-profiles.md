# SPEC CPU Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPEC CPU workload.

* [Workload Details](./speccpu.md)  
* [Workload Profile Metrics](./speccpu-metrics.md)


### Preliminaries
The profiles below require the ability to download workload packages and dependencies from a package store. In order to download the workload packages, connection information 
must be supplied on the command line. See the 'Workload Packages' documentation above for details on how that works.

:::info
SPECcpu 2017 workloads are long running workloads. Some take more than 2 days to complete. Check the 'Workload Runtimes' section for more details on what
to expect.
:::

-----------------------------------------------------------------------

### PERF-SPECCPU-FPRATE.json
Runs the SPEC CPU Floating Point Rate (fprate) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

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


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 28 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-FPRATE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```


-----------------------------------------------------------------------

### PERF-SPECCPU-FPSPEED.json
Runs the SPEC CPU Floating Point Speed (fpspeed) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

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


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 6 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-FPSPEED.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```



-----------------------------------------------------------------------

### PERF-SPECCPU-INTRATE.json
Runs the SPEC CPU Integer Rate (intrate) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

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


* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs. This
  particular workload takes multiple days to complete the number of iterations required for valid results.

  * Expected Runtime (8-core/vCPU VM) = 16 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-INTRATE.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

### PERF-SPECCPU-INTSPEED.json
Runs the SPEC CPU Integer Speed (intspeed) benchmark workload on the system focusing on baseline + peak measurements. This workload is an industry standard 
for evaluating the performance of the CPU for processing calculations.

* **OS/Architecture Platforms**
  * linux-x64
  * linux-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22

* **Supported Compilers**  
  The following compilers are supported with the workload for this profile. See profile parameters and usage examples below.

  * GCC Compiler Versions = 8, 9, 10

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

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of tests ran. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the use of prescribed VM SKUs.

  * Expected Runtime (8-core/vCPU VM) = 9 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.


  ``` csharp
  ./VirtualClient --profile=PERF-SPECCPU-INTSPEED.json --system=Azure --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
