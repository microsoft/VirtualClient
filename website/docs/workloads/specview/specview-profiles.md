# SPECviewperf Workload Profiles
The following profile runs the SPECviewperf Workloads.

* [Workload Details](./specview.md)  

## PERF-GPU-SPECVIEW-AMD.json
Runs the stock SPECviewperf Workloads.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SPECVIEW-AMD.json) 

* **Supported Platform/Architectures**
  * win-x64
  * AMD v620 GPU

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
NA

* **Profile Runtimes**  
  * The SPECviewperf package zip file is around 30GB. Downloading and extracting this file take about 30 minutes to complete. 
  * Each SPECviewperf viewset takes about 5 min to complete on a machine with a single AMD v620 GPU. Running all the viewsets takes about 40 minutes to complete.
  * The exact numbers may vary depending on the system and the internet performance. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-SPECVIEW-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"

  # Override the profile default parameters to use a different .NET SDK version
  VirtualClient.exe --profile=PERF-GPU-SPECVIEW-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}"
  ```