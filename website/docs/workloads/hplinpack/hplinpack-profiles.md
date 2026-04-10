# HPLinpack Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the HPLinpack workload.  

* [Workload Details](./hplinpack.md)  

## PERF-CPU-HPLINPACK.json
This profile runs HPLinpack workload on the system without any specific performance libraries used.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-HPLINPACK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Linux Distrbutions**
    * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters are specific to this workload and decides behavior of the workload.

  | Parameter                 | Purpose                                                                            | Default Value |
  |---------------------------|------------------------------------------------------------------------------------|---------------|
  | BindToCores               | True/false to bind each process to a single core.                                  | false |
  | BlockSizeNB               | The partitioning blocking factor                                                   | 256 |
  | CompilerVersion           | Version of the GCC compiler to use.                                                | Default version of GCC for the Linux distro. |
  | CCFLAGS                   | GCC compiler flags                                                                 | <ul><li>Intel/AMD Systems: -O3 -march=native</li><li>ARM Systems: -O3 -march=armv8-a</li></ul>  |
  | NumberOfProcesses         | The number of processes to launch in parallel.                                     |  # logical processors |
  | ProblemSizeN              | The order of coefficient matrix of set of linear equations that we want to solve   | Calculated to use approximately 80% of available memory on the system. |

  There are two other input values for HPLinpack. They are 
  * P (The number of process rows)
  * Q (The number of process columns)

  These values are machine dependent and are calculated by 3 rules
      
      * P * Q = No. of processors
      * P {'<='} Q 
      * Q-P to be the minimum possible value 

  * [Resources for above input parameters(ProblemSizeN,BlockSizeNB) configuration setting](https://netlib.org/utk/people/JackDongarra/faq-linpack.html#_For_HPL_What_problem%20size%20N%20should)

  * [Inputs Tuning](https://community.arm.com/arm-community-blogs/b/high-performance-computing-blog/posts/profiling-and-tuning-linpack-step-step-guide)
  
* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-CPU-HPLINPACK.json --system=Demo"
  
  # Execute the workload profile with specific compiler version and flags
  ./VirtualClient --profile=PERF-CPU-HPLINPACK.json --system=Demo" --parameters="CompilerVersion=11,,,CCFLAGS=-O2 -flto -march=x86_64_v3"
  ```

## PERF-CPU-HPLINPACK-AMD.json
This profile runs HPLinpack workload with AMD performance libraries.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-HPLINPACK-AMD.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Linux Distrbutions**
    * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters are specific to this workload and decides behavior of the workload.

  | Parameter                 | Purpose                                                                            | Default Value |
  |---------------------------|------------------------------------------------------------------------------------|---------------|
  | BindToCores               | True/false to bind each process to a single core.                                  | false |
  | BlockSizeNB               | The partitioning blocking factor                                                   | 256 |
  | CompilerVersion           | Version of the GCC compiler to use.                                                | Default version of GCC for the Linux distro. |
  | CCFLAGS                   | GCC compiler flags                                                                 | -O3 -march=native  |
  | NumberOfProcesses         | The number of processes to launch in parallel.                                     |  # logical processors |
  | PerformanceLibraryVersion | The version of the performance libraries you would like to use. Supported versions for AMD include: 4.2.0, 5.0.0, 5.1.0 | 5.1.0  |
  | ProblemSizeN              | The order of coefficient matrix of set of linear equations that we want to solve   | Calculated to use approximately 80% of available memory on the system. |
  
* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile with AMD performance libraries
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-AMD.json --system=Demo"
  
  # Execute the workload profile with specific compiler version and flags
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-AMD.json --system=Demo" --parameters="CompilerVersion=11,,,CCFLAGS=-O2 -flto -march=x86_64_v3"

  # Execute the workload profile with a specific version of the AMD performance libraries
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-AMD.json --system=Demo --parameters="PerformanceLibraryVersion=4.2.0"
  ```

## PERF-CPU-HPLINPACK-ARM.json
This profile runs HPLinpack workload with ARM performance libraries.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-HPLINPACK-ARM.json) 

* **Supported Platform/Architectures**
  * linux-arm64

* **Supported Linux Distrbutions**
    * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters are specific to this workload and decides behavior of the workload.

  | Parameter                 | Purpose                                                                            | Default Value |
  |---------------------------|------------------------------------------------------------------------------------|---------------|
  | BindToCores               | True/false to bind each process to a single core.                                  | false |
  | BlockSizeNB               | The partitioning blocking factor                                                   | 256 |
  | CompilerVersion           | Version of the GCC compiler to use.                                                | Default version of GCC for the Linux distro. |
  | CCFLAGS                   | GCC compiler flags                                                                 | -O3 -march=armv8-a  |
  | NumberOfProcesses         | The number of processes to launch in parallel.                                     |  # logical processors |
  | PerformanceLibraryVersion | The version of the performance libraries you would like to use. Supported versions for ARM include: 23.04.1, 24.10, 25.04.1 | 25.04.1  |
  | ProblemSizeN              | The order of coefficient matrix of set of linear equations that we want to solve   | Calculated to use approximately 80% of available memory on the system. |
  
* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile with AMD performance libraries
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-ARM.json --system=Demo"
  
  # Execute the workload profile with specific compiler version and flags
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-ARM.json --system=Demo" --parameters="CompilerVersion=11,,,CCFLAGS=-O2 -flto -march=armv8.2-a"

  # Execute the workload profile with a specific version of the ARM performance libraries
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-ARM.json --system=Demo --parameters="PerformanceLibraryVersion=25.04.1"
  ```

## PERF-CPU-HPLINPACK-INTEL.json
This profile runs HPLinpack workload with Intel performance libraries.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-HPLINPACK-INTEL.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supported Linux Distrbutions**
    * Ubuntu

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters are specific to this workload and decides behavior of the workload.

  | Parameter                 | Purpose                                                                            | Default Value |
  |---------------------------|------------------------------------------------------------------------------------|---------------|
  | BindToCores               | True/false to bind each process to a single core.                                  | false |
  | BlockSizeNB               | The partitioning blocking factor                                                   | 256 |
  | CompilerVersion           | Version of the GCC compiler to use.                                                | Default version of GCC for the Linux distro. |
  | CCFLAGS                   | GCC compiler flags                                                                 | -O3 -march=native  |
  | NumberOfProcesses         | The number of processes to launch in parallel.                                     |  # logical processors |
  | PerformanceLibraryVersion | The version of the performance libraries you would like to use. Supported versions for Intel include: 2024.2.2.17, 2025.1.0.803 | 2024.2.2.17  |
  | ProblemSizeN              | The order of coefficient matrix of set of linear equations that we want to solve   | Calculated to use approximately 80% of available memory on the system. |
  
* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile with Intel performance libraries
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-INTEL.json --system=Demo"
  
  # Execute the workload profile with specific compiler version and flags
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-INTEL.json --system=Demo" --parameters="CompilerVersion=11,,,CCFLAGS=-O2 -flto -march=x86_64_v3"

  # Execute the workload profile with a specific version of the Intel performance libraries
  ./VirtualClient --profile=PERF-CPU-HPLINPACK-INTEL.json --system=Demo --parameters="PerformanceLibraryVersion=2024.2.2.17"
  ```

* **Resources**

* [Performance Libraries for AMD] (https://www.amd.com/en/developer/zen-software-studio/applications/spack/hpl-benchmark.html)
* [Performance Libraries for ARM] (https://developer.arm.com/downloads/-/arm-performance-libraries)
* [Performance Libraries for Intel](https://www.intel.com/content/www/us/en/developer/articles/technical/onemkl-benchmarks-suite.html)
* [HPLinpack related links] (https://netlib.org/benchmark/hpl/links.html)
