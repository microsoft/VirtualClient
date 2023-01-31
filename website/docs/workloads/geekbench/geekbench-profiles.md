# GeekBench5 Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the GeekBench5 workload.

* [Workload Details](./geekbench.md)  

## PERF-CPU-GEEKBENCH.json
Runs a CPU-intensive workload using the GeekBench5 toolset to test the performance of the CPU across various types of common application algorithms 
(e.g. Gaussian Blur, AES-XTS, Text Compression, Image Compression). This profile is designed to identify general/broad regressions when compared 
against a baseline. GeekBench is an industry standard benchmarking toolset.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-CPU-GEEKBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64
  * linux-arm64
  * win-x64
  * win-arm64

* **Supports Disconnected Scenarios**  
  * Yes. When the GeekBench5 package is included in 'packages' directory of the Virtual Client.
    * [Installing VC Packages](../../dependencies/0001-install-vc-packages.md). 

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-CPU-GEEKBENCH.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
