# 3DMark Workload Profiles
The following profile runs the 3DMark TimeSpy Workloads.

* [Workload Details](./3DMark.md)  

## PERF-GPU-3DMARK.json
Runs the stock 3DMark TimeSpy Workloads.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-3DMARK.json) 

* **Supported Platform/Architectures**
  * win-x64

* **Supports Disconnected Scenarios**  
  * Yes. Internet connection only required for lisence key validation.

* **Dependencies**  
  An NVIDIA or AMD GPU driver installation is required for this workload.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | LisenceKey           | Required. The [3DMark](https://benchmarks.ul.com/3dmark?_ga=2.106445760.293481338.1681124251-1769566625.1681124251#windows) lisence key| None|

* **Profile Runtimes**  
  The Timespy workload takes about 5-10 minutes to run depending on the performance of the target system.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-3DMARK.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the profile default parameters to use a different .NET SDK version
  VirtualClient.exe --profile=PERF-GPU-3DMARK.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"
  ```