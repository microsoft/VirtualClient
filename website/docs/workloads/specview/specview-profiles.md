# SPECviewperf Workload Profiles
The following profile runs the SPECviewperf Workloads.

* [Workload Details](./specview.md)  

## PERF-GPU-SPECVIEW-AMD.json
Runs the stock SPECviewperf Workloads.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SPECVIEW-AMD.json) 

* **Supported Platform/Architectures**
  * win-x64

* **Supports Disconnected Scenarios**  
  * Yes.

* **Dependencies**  
  An AMD GPU driver installation is required for this workload.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
NA

* **Profile Runtimes**  
  The SPECviewperf workload takes about 30 minutes to run depending on the performance of the target system.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-SPECVIEW-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the profile default parameters to use a different .NET SDK version
  VirtualClient.exe --profile=PERF-GPU-SPECVIEW-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"
  ```