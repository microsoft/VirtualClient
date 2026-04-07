# SuperBenchmark Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the SuperBenchmark workload.

* [Workload Details](./superbenchmark.md)  

:::warning
*Note that at the moment, the Virtual Client ONLY has support for Nvidia GPU systems. Work is underway to finalize support for the installation of drivers required in order to support AMD GPU systems.*
:::

## PERF-GPU-SUPERBENCH.json
Runs the SuperBenchmark benchmark workload to test GPU performance. <mark>This workload is <b>supported ONLY for systems that contain nVidia GPU
hardware components</b>. Work is underway with partner teams in Azure to support additional GPU manufacturers.</mark>

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-SUPERBENCH.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Dependencies**  
  The dependencies defined in the 'Dependencies' section of the profile itself are required in order to run the workload operations effectively.
  * Internet connection.
  * The VM must run on hardware containing Nvidia GPU cards/components.

  Additional information on components that exist within the 'Dependencies' section of the profile can be found in the following locations:
  * [Installing Dependencies](https://microsoft.github.io/VirtualClient/docs/category/dependencies/)

* **Profile Parameters**  
  The following parameters can be optionally supplied on the command line to modify the behaviors of the workload.

  | Parameter             | Purpose | Default Value |
  |-----------------------|---------|---------------|
  | ConfigurationFile     | Optional. The configuration file to use on the system providing all of the specifics required by Superbench for the particular GPU SKU. | default.yaml  |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile. Additional usage examples can be found in the
  'Usage Scenarios/Examples' link at the top.

  ``` bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-SUPERBENCH.json
  ```