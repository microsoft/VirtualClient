# Blender Workload Profiles
The following profile runs the Blender benchmark Workloads.

* [Workload Details](./blenderbenchmark.md)  

## PERF-BLENDER-AMD.json
Runs the Blender Workloads.

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-BLENDER-AMD.json) 

* **Supported Platform/Architectures**
  * win-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * One of the Following AMD GPUs:
    * Model = v620
    * Model = mi25

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  | Parameter                 | Purpose                                                                                           | Default Value |
  |---------------------------|---------------------------------------------------------------------------------------------------|---------------|
  | GpuModel                  | Required. Specify which GPU driver should be installed. Currently supports [AMD v620, AMD mi25]   | None          |
  | BlenderVersion            | Optional. Specify which version of the *blender engine* the *blender benchmark* should use. Currently supported versions: [3.6.0, 3.5.0, 3.4.0, 3.3.0, 3.2.1]  | 3.6.0         |
  | Scenes                    | Optional. Specify which scene(s) the *blender benchmark* should run. Default action is to run all scenes    | monster junkshop classroom |
  | DeviceTypes               | Optional. Specify what devices the *blender benchmark* should run on. When multiple devices are specified, *blender benchmark* will test each individual device on its own. Common devices: CPU, HIP (AMD GPU), CUDA (Nvidia GPU). See the full list of [rendering technologies](https://docs.blender.org/manual/en/latest/render/cycles/gpu_rendering.html)        | CPU |



* **Profile Runtimes**  
  * Blenderbenchmark takes about one minute to run each scene. 
  * The exact numbers may vary depending on the system and the internet performance. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-BLENDER-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}" --parameters="GpuModel=v620"

  # Override the profile default parameters to run different blender version
  VirtualClient.exe --profile=PERF-BLENDER-AMD.json --system=Demo --packageStore="{BlobConnectionString|SAS Uri}" --parameters="GpuModel=v620,,,BlenderVersion=3.5.0"
  ```