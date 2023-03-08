# HPLinpack Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the HPLinpack workload.  

* [Workload Details](./HPLinpack.md)  

## PERF-CPU-HPLINPACK.json
HPL (High Performance Linpack) is a software package that solves a (random) dense linear system in double precision (64 bits) arithmetic on distributed-memory computers. It can thus be regarded as a portable as well as freely available implementation of the High Performance Computing Linpack Benchmark.

The HPL package provides a testing and timing program to quantify the accuracy of the obtained solution as well as the time it took to compute it. The best performance achievable by this software on your system depends on a large variety of factors.

This profile is designed to identify general/broad regressions when compared against a baseline by testing and timing programs that provide complete solutions for the most common problems for linear system of equations.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-HPLINPACK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Arm Performance Libraries provides optimized standard core math libraries for high-performance computing applications on Arm processors. It has BLAS(Basic Linear Algebraic Subroutines) implementation and math libraries specific to ARM processors to run HPL that are .
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  VirtualClient.exe --profile=PERF-CPU-HPLINPACK.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```