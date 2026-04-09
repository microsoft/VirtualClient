# 3DMark Workload Profiles
The following profile runs the 3DMark TimeSpy Workloads.

* [Workload Details](./3dmark.md)  

## PERF-GPU-3DMARK.json
Runs the stock 3DMark TimeSpy Workloads.

<mark>
Note that this profile requires the AMD or Nvidia GPU driver + CUDA toolsets to be already installed on the system. The profile does not attempt to install the GPU driver or 
any of the dependencies required by the driver. If the driver is not already installed, then this profile will fail to capture workload information.
</mark>

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-3DMARK.json) 

* **Supported Platform/Architectures**
  * win-x64

* **Supports Disconnected Scenarios**  
  * Yes. Internet connection only required for lisence key validation.

* **Dependencies**  
  * Internet connection.
  * The system must have the GPU driver (e.g. AMD, Nvidia) and related toolsets (e.g. CUDA) installed.ed.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter                 | Purpose                                                           | Default Value |
  |---------------------------|-------------------------------------------------------------------|---------------|
  | LicenseKey                | Required. The [3DMark](https://benchmarks.ul.com/3dmark?_ga=2.106445760.293481338.1681124251-1769566625.1681124251#windows) license key| None|

* **Profile Runtimes**  
  The TimeSpy workload takes about 5-10 minutes to run depending on the performance of the target system.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-3DMARK.json --system=Demo"
  ```