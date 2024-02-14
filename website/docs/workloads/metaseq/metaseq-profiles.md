# Metaseq Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the Metaseq workload.

* [Workload Details](./metaseq.md)  
* [Metaseq Apptainer Image Curation Steps](./metaseq-apptainer-image-curation-steps.md)

## PERF-GPU-METASEQ.json
Runs the Metaseq benchmark workload to test GPU performance.

:::warning
*This workload is supported ONLY for systems that contain Nvidia GPU hardware components. See the documentation above for more specifics.*
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-METASEQ-NVIDIA.json) 

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
  | Hostnames  | Required. Comma separated list of hostnames on which metaseq needs to be run | Empty string |
  | ApptainerImageVersion     | Optional. Apptainer image version that needs to be used to run the benchmark. | 1.1.6  |
  | TrainingScript     | Optional. Script that needs to be used to train. | train_7.5.sh  |
  
* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-METASEQ-NVIDIA.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```