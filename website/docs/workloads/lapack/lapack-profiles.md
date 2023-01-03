# LAPACK Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the LAPACK workload.  

* [Workload Details](./lapack.md)  

## PERF-CPU-LAPACK.json
Runs a CPU-intensive workload using the LAPACK toolset to test the performance of the CPU in processing different tests for Fortran subroutines.
This profile is designed to identify general/broad regressions when compared against a baseline by testing routines that provide complete 
solutions for the most common problems of numerical linear algebra.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-LAPACK.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supported Operating Systems**
  * Ubuntu 18
  * Ubuntu 20
  * Ubuntu 22
  * Windows 10
  * Windows 11
  * Windows Server 2016
  * Windows Server 2019

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (2-cores/vCPUs) = 1.5 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  VirtualClient.exe --profile=PERF-CPU-LAPACK.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```