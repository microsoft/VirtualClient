# SPECviewperf Workload Profiles
The following profile runs the SPECviewperf Workloads.

* [Workload Details](./specview.md)  

## PERF-GPU-SPECVIEW.json
Runs the stock SPECviewperf Workloads.

<mark>
Note that this profile requires the AMD or Nvidia GPU driver + CUDA toolsets to be already installed on the system. The profile does not attempt to install the GPU driver or 
any of the dependencies required by the driver. If the driver is not already installed, then this profile will fail to capture workload information.
</mark>

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SPECVIEW.json) 

* **Supported Platform/Architectures**
  * win-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The system must have the GPU driver (e.g. AMD, Nvidia) and related toolsets (e.g. CUDA) installed.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  | Parameter                 | Purpose                                                                                           | Default Value |
  |---------------------------|---------------------------------------------------------------------------------------------------|---------------|
  | Viewsets                  | Optional. Specify which particular benchmarks should be run. See the list of viewsets in the [Workload Details](https://gwpg.spec.org/benchmarks/benchmark/specviewperf-2020-v3-1/) section.                                                                                                                        | "3dsmax,catia"|
  | PsExecSession             | Optional. If specified, specviewperf will be started by PsExec in the specified session.          | -1, specviewperf runs in the current session without psExec.|

* **Profile Runtimes**  
  * The SPECviewperf package zip file is around 30GB. Downloading and extracting this file take about 30 minutes to complete. 
  * The exact numbers may vary depending on the system and the internet performance. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-SPECVIEW.json --system=Demo

  # Override the profile default parameters to include all viewsets
  VirtualClient.exe --profile=PERF-GPU-SPECVIEW.json --parameters="Viewset=3dsmax,catia,creo,energy,maya,medical,snx,sw" --system=Demo
  ```
