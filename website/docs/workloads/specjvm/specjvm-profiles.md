# SPECjvm Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SPECjvm workload.

* [Workload Details](./specjvm.md)  

## PERF-SPECJVM.json
Runs the SPECjvm benchmark workload to evaluate the performance of the core Java Runtime.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-SPECJVM.json) 

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

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Scenarios**  
  The following scenarios are covered by this workload profile.

  * Compression aspects
  * Cryptography aspects
  * Derby aspects
  * MPEG Audio streaming aspects
  * SciMark aspects
  * Serial aspects
  * Sunflow aspects

* **Workload Runtimes**  
  The following timings represent the length of time required to run a single round of profile actions. These timings can be used to determine
  minimum required runtimes for the Virtual Client in order to get results. These are estimates based on the number of system cores.

  * (8-cores/vCPUs) = 3 hours

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  VirtualClient.exe --profile=PERF-SPECJVM.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```