# MLPerf Workload Profiles
The following profiles run customer-representative or benchmarking scenarios using the MLPerf workload.

* [Workload Details](./mlperf.md)  
* [MLPerf Training Bert Preprocessing Data](./mlperf-trainining-bert-preprocessing-data.md)

## PERF-GPU-MLPERF.json
Runs the MLPerf benchmark workload to test GPU performance.

:::warning
This workload is supported ONLY for systems that contain Nvidia GPU hardware components. See the documentation above for more specifics.
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-MLPERF.json) 

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
  | Username     | Optional. User which needs to be created in container to run MLPerf benchmarks. | testuser  |
  | DiskFilter     | Optional. Filter to decide the disk that will be used to download benchmarks. Since benchmarks data is around 800gb, default disk filter is greater than 1000gb. | SizeGreaterThan:1000gb  |
  | LinuxCudaVersion     | Optional. Version of CUDA that needs to be installed. | 12.4  |
  | LinuxDriverVersion     | Optional. Version of GPU driver that needs to be installed. | 550  |
  | LinuxLocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/12.0.0/local_installers/cuda_12.4.0_550.54.14_linux.run  |
  | RequireCustomSystemSupport | Optional. This enables additional A100_PCIe_40GBx8 system support that was not supported by github repo of MLPerf. Ones that are supported by github repo of MLPerf are still supported. | true |

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores. 

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  VirtualClient.exe --profile=PERF-GPU-MLPERF.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```

## PERF-GPU-MLPERF-TRAINING-NVIDIA.json
Runs the MLPerf benchmark workload to test GPU performance. 

:::warning
*This workload is supported ONLY for systems that contain Nvidia GPU hardware components. See the documentation above for more specifics.*
:::

* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-MLPERF-TRAINING-NVIDIA.json) 

* **Supported Platform/Architectures**
  * linux-x64

* **Supports Disconnected Scenarios**  
  * No. Internet connection required.

* **Storage Requirements**
  * Min OS Disk Size : 256GB
  * Min Data Disk Size : 8TB (1 Disk)

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
  | Username     | Optional. User which needs to be created in container to run MLPerf benchmarks. |   |
  | DiskFilter     | Optional. Filter to decide the disk that will be used to download training data. | BiggestSize  |
  | LinuxCudaVersion     | Optional. Version of CUDA that needs to be installed. | 12.0  |
  | LinuxDriverVersion     | Optional. Version of GPU driver that needs to be installed. | 525  |
  | LinuxLocalRunFile     | Optional. Link to download specified CUDA and GPU driver versions. | https://developer.download.nvidia.com/compute/cuda/12.0.0/local_installers/cuda_12.0.0_525.60.13_linux.run  |

* **MLPerfTrainingExecutor Parameters**

  | Parameter             | Purpose                                                                                           | Default Value |
  |-----------------------|---------------------------------------------------------------------------------------------------|---------------|
  | Model                 | Optional. The type of model to run for training supported models mentioned in mlperf.md           | bert          |
  | Username              | Optional. Username for running the docker. On null it selects logged in user.                     | null          |
  | BatchSize             | Optional. BatchSize for the datachunks in training model.                                         | 40            |
  | Implementation        | Optional. Implementation present for a given model/benchmark. Example for bert [link](https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks)                     | pytorch-22.09    |
  | ContainerName         | Optional. Name for docker model.             |language_model |
  | DataPath              | Optional. Folder name for training data. /mlperftraining0/\{DataPath}             |mlperf-training-data-bert.1.0.0|
  | GPUNum                | Optional. Number of GPUs to stress.               |8              |
  | ConfigFile            | Optional. Configuration for running workload. Visit the implementation for a model for all supported config files. [link](https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks).             |config_DGXA100_1x8x56x1.sh|
  | PackageName           | Required. Packname for mlperf training.               |               |

* **MLPERF Trainging Changes**
  * Changed "nvidia-docker" to "docker" inside run_with_docker.sh file.

* **Profile Runtimes**  
  See the 'Metadata' section of the profile for estimated runtimes. These timings represent the length of time required to run a single round of profile 
  actions. These timings can be used to determine minimum required runtimes for the Virtual Client in order to get results. These are often estimates based on the
  number of system cores.

* **Usage Examples**  
  The following section provides a few basic examples of how to use the workload profile.

  ```bash
  # Execute the workload profile
  sudo ./VirtualClient --profile=PERF-GPU-MLPERF-TRAINING.json --system=Demo --timeout=1440 --packageStore="{BlobConnectionString|SAS Uri}"
  ```
