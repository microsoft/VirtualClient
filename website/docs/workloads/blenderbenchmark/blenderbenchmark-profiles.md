# Blender Workload Profiles
The following profile runs the Blender benchmark Workloads.

* [Workload Details](./blender.md)  

## PERF-BLENDER-AMD.json
Runs the Blender Workloads.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-BLENDER-AMD.json) 

* **Supported Platform/Architectures**
  * win-x64
  * AMD v620 GPU or AMD Mi25 GPU

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  | Parameter                 | Purpose                                                                                           | Default Value |
  |---------------------------|---------------------------------------------------------------------------------------------------|---------------|
  | GpuModel                  | Required. Specify which AMD GPU is used to run the workload. Supported GPU: [v620, mi25]          | None          |
  | BlenderVersion            | Optional. Specify which blender version the benchmark should use. Currently supported versions: [3.6.0, 3.5.0, 3.4.0, 3.3.0, 3.2.1]  | 3.6.0         |
  | Scenes                    | Optional. Specify which scene(s) the benchmark should run. Default action is to run all scenes    | monster junkshop classroom |
  | DeviceTypes               | Optional. Specify which device (CPU, HIP) the benchmark should run on. HIP means AMD GPU.         | CPU, HIP |



* **Profile Runtimes**  
  * Blenderbenchmark takes about five minutes to run all three scenes on a CPU/GPU.
  * The exact numbers may vary depending on the system and the internet performance. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-BLENDER-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}" --pm="GpuModel=v620"

  # Override the profile default parameters to run different blender version
  VirtualClient.exe --profile=PERF-BLENDER-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}" --pm="GpuModel=v620,,,BlenderVersion=3.5.0"
  ```