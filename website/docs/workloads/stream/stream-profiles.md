# STREAM Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the STREAM workload.

* [Workload Details](./stream.md)  

## PERF-MEM-STREAM.json
Runs a memory-intensive workload using the STREAM benchmark to test the sustainable memory bandwidth of the system. STREAM measures memory bandwidth 
using four simple vector kernels (Copy, Scale, Add, and Triad) designed to stress the memory subsystem with minimal dependency on cache.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-STREAM.json)

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Blob storage account from which the required dependencies package can be downloaded.
	* https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-STREAM.json

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerVersion           | Optional. The version of the compiler to use.  | The default version for the OS/distro. |
  | CompilerParameters        | Optional. Compiler flags used to compile the STREAM binary. | -fopenmp -mcmodel=large -D_OPENMP -DNTIMES=5000 -DSTREAM_ARRAY_SIZE=100000000 |
  | ThreadCount               | Optional. The number of threads to use for running the benchmark. | # of physical cores |
  | CommandArgumentsWindows   | Optional. Command-line arguments for the Windows version of STREAM. | -n 50 -s 320000000 |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

  * Recommended Minimum Execution Time = 10 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-MEM-STREAM.json --system=Azure --timeout=60 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the compiler version
  ./VirtualClient --profile=PERF-MEM-STREAM.json --system=Azure --timeout=60 --parameters="CompilerVersion=11" --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the thread count
  ./VirtualClient --profile=PERF-MEM-STREAM.json --system=Azure --timeout=60 --parameters="ThreadCount=8" --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## PERF-MEM-STREAMTRIAD.json
Runs a memory-intensive workload using the Intel-optimized STREAM Triad benchmark to test memory bandwidth. This profile is specifically designed 
by the Intel team to maximize the utilization of Intel processors. The STREAMTriad toolset focuses on the Triad kernel which is often considered 
the most representative of real-world memory access patterns.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-STREAMTRIAD.json)

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Blob storage account from which the required dependencies package can be downloaded.
	* https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-STREAMTRIAD.json

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | ThreadCount               | Optional. The number of threads to use for running the benchmark. | # of physical cores |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

  * Recommended Minimum Execution Time = 10 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-MEM-STREAMTRIAD.json --system=Azure --timeout=60 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the thread count
  ./VirtualClient --profile=PERF-MEM-STREAMTRIAD.json --system=Azure --timeout=60 --parameters="ThreadCount=16" --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## PERF-MEM-STREAMMSFT.json
Runs a memory-intensive workload using Microsoft's optimized STREAM implementation to test memory bandwidth. This profile is specifically designed 
by the Microsoft team to provide additional metrics including detailed latency measurements and support for ARM64 architectures. The implementation 
includes additional memory operations (Read and Write) beyond the standard STREAM kernels.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-STREAMMSFT.json)

* **Supported Platform/Architectures**
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * Blob storage account from which the required dependencies package can be downloaded.
	* https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-MEM-STREAMMSFT.json

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                                         | Default value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerVersion           | Optional. The version of the compiler to use.  | The default version for the OS/distro. |
  | CommandLineParameters     | Optional. Command-line arguments for the STREAMMSFT benchmark. | --internal-iter 1000 --internal-iter-lat 1000 |
  | ThreadCount               | Optional. The number of threads to use for running the benchmark. | # of physical cores |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

  * Recommended Minimum Execution Time = 10 minutes

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  ./VirtualClient --profile=PERF-MEM-STREAMMSFT.json --system=Azure --timeout=60 --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the compiler version
  ./VirtualClient --profile=PERF-MEM-STREAMMSFT.json --system=Azure --timeout=60 --parameters="CompilerVersion=11" --packageStore="{BlobConnectionString|SAS Uri}"

  # Override command-line parameters
  ./VirtualClient --profile=PERF-MEM-STREAMMSFT.json --system=Azure --timeout=60 --parameters="CommandLineParameters='--internal-iter 2000 --internal-iter-lat 2000'" --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the thread count
  ./VirtualClient --profile=PERF-MEM-STREAMMSFT.json --system=Azure --timeout=60 --parameters="ThreadCount=32" --packageStore="{BlobConnectionString|SAS Uri}"
  ```