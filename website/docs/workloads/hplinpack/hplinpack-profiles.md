# HPLinpack Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the HPLinpack workload.  

* [Workload Details](./hplinpack.md)  

## PERF-CPU-HPLINPACK.json
HPL (High Performance Linpack) is a software package that solves a (random) dense linear system in double precision (64 bits) arithmetic on distributed-memory computers. It can thus be regarded as a portable as well as freely available implementation of the High Performance Computing Linpack Benchmark.

The HPL package provides a testing and timing program to quantify the accuracy of the obtained solution as well as the time it took to compute it. The best performance achievable by this software on your system depends on a large variety of factors.

This profile is designed to identify general/broad regressions when compared against a baseline by testing and timing programs that provide complete solutions for the most common problems for linear system of equations.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-HPLINPACK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supported Linux Distrbutions**
    * Ubuntu

  **Note**: Performance Libraries are enabled only for **Linux ARM-Ubuntu22.04 with gcc 11** as of now. Performance Libraries for AMD and Intel will be enabled soon.
  By default this profile runs without performance libraries. User can customize it from the VC command line parameters.

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters are specific to this workload and decides behavior of the workload.

  | Parameter                 | Purpose                                                                         | Default Value |
  |---------------------------|---------------------------------------------------------------------------------|---------------|
  | CompilerName | Name of compiler used | gcc |
  | CompilerVersion | Version of compiler | 11 |
  |   ProblemSizeN      |  The order of coefficient matrix of set of linear equations that we want to solve  | Convert.ToInt32(Math.Sqrt(totalAvailableMemoryKiloBytes * 1024 * 0.8 / 8)) (This value is dependent on memory of machine, uses 80% of available memory) |
  |   BlockSizeNB       |  The partitioning blocking factor  | 256 |
  |   PerformanceLibrary | Optional. This parameter allows you to specify machine-specific performance libraries. You can assign values such as ARM, AMD, and INTEL to utilize the corresponding performance libraries. | null |
  | PerformanceLibraryVersion | Optional, but required when using PerformanceLibrary parameter. Specify the version of the performance libraries you would like to use.<br/><br/>Curently, the supported configurations are :<br/>ARM - 23.04.1, 24.10 and 25.04.1 | null  |  | CCFLAGS | compiler flags| -O3 -march=native  |
  |  BindToCores | If you want to bind the process to single core | false|
  | NumberOfProcesses | Number of processes to be launched for the parallel program |  No. of logical cores|

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
  sudo ./VirtualClient --profile=PERF-CPU-HPLINPACK.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

  If you want to use performance libraries for the supported platforms and distribution run following command.

  ``` bash
  sudo ./VirtualClient --profile=PERF-CPU-HPLINPACK.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}" --parameters=PerformanceLibrary=ARM,,,PerformanceLibraryVersion=23.04.1

  ```

* **Resources**

* [Performance Libraries for ARM] (https://developer.arm.com/downloads/-/arm-performance-libraries)
* [HPLinpack related links] (https://netlib.org/benchmark/hpl/links.html)
