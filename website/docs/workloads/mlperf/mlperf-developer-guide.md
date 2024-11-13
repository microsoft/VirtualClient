# MLPerf Developer Guide
The following guide details the developer process for the MLPerf workload. The focus of this guide is on MLPerf *Inference*.

* [Workload Details](./mlperf.md)
* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-MLPERF.json)
* [MLPerf Inference Results Repository](https://github.com/mlcommons/inference_results_v4.1)

In machine learning, inference involves using an already trained model to make predictions on unseen data.
The MLPerf workload will run multiple benchmarks on a GPU-based system, in which the performance of the model
to make those predictions is measured. The throughput (requests per second) and latency (response time for a 
single prediction) are used as metrics.

## Benchmarks
The benchmark suite for NVIDIA GPU-based systems in MLPerf Inference is detailed in the [Inference results
repository](https://github.com/mlcommons/inference_results_v4.1/tree/main/closed/NVIDIA/configs).  
The following are supported currently in Virtual Client:
- **bert**: Used for natural language processing tasks. This benchmark does not require any supplemental data to test.
- **3d-unet**: Used for 3D volumetric data for medical imaging applications. This benchmark does not require any supplemental data to test.

## Scenarios
MLPerf will evaluate the performance of a system in different scenarios. For a given benchmark, the configurations for each scenario
is available under the [directory for the benchmark](https://github.com/mlcommons/inference_results_v4.1/tree/main/closed/NVIDIA/configs/bert).
- **Offline**: All queries are aggregated into a batch and sent to the tested system. The maximum throughput without a latency constraint is measured.
- **Server**: Queries are aggregated into multiple batches and sent to the tested system. The maximum throughput with a latency constraint is measured.
- **SingleStream**: Queries are sent one-by-one to the tested system. The latency of processing individual queries is measured.

## Config Versions
- **default**: Uses lower precision to achieve faster inference times.  

## Hardware for MLPerf
- **A100_SXM4_40GBx8**: Azure VM sku Standard_ND96asr_v4. This represents a system with 8 A100 NVIDIA GPUs. The NVIDIA A100 GPU
is designed for high-performance computing.

The config information given the benchmark, scenario, config version, and system to test is in the [\_\_init\_\_.py file under the benchmark folder](https://github.com/mlcommons/inference_results_v4.1/blob/main/closed/NVIDIA/configs/bert/SingleStream/__init__.py).
By default, the repository does not have support for all systems (including A100_SXM4_40GBx8). To add support, the file is replaced with Virtual Client at runtime.
For bert in the SingleStream scenario, the following section is added:
```
@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GBx8(SingleStreamGPUBaseConfig):
    system = KnownSystem.A100_SXM4_40GBx8
    single_stream_expected_latency_ns = 1700000
```
These files are stored as script files for MLPerf, under [GPUConfigFiles](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/MLPerf/GPUConfigFiles).

## Dependencies
- **CUDA**: An API created by NVIDIA which enables general-purpose computing on GPUs. To install CUDA, a .run file is used.
- **NVIDIA Linux Driver**: Software component that enables communication between GPUs and the operating
system. The linux driver will handle the low-level interaction between the GPU and OS. This driver is 
required for CUDA to function.  
The values for CUDA and the Linux Driver are called out in profile parameters section.
```
"Parameters": {
    ...
    "LinuxCudaVersion": "12.4",
    "LinuxDriverVersion": "550",
    "LinuxLocalRunFile": "https://developer.download.nvidia.com/compute/cuda/12.4.0/local_installers/cuda_12.4.0_550.54.14_linux.run",
    ...
}
```
- **NVIDIA Fabric Manager**: A software stack which is used to connect multiple GPUs for high-performance computing tasks.  
CUDA, the NVIDIA linux driver, and the NVIDIA fabric manager are all installed using NvidiaCudaInstallation.
```
{
    "Type": "NvidiaCudaInstallation",
    "Parameters": {
        "Scenario": "InstallNvidiaCuda",
        "LinuxCudaVersion": "$.Parameters.LinuxCudaVersion",
        "LinuxDriverVersion": "$.Parameters.LinuxDriverVersion",
        "Username": "$.Parameters.Username",
        "LinuxLocalRunFile": "$.Parameters.LinuxLocalRunFile"
    }
}
```
The versions of the linux driver and the fabric manager **must match exactly otherwise the fabric manager will not start and the benchmark
cannot be run**.
```
commands.Add($"apt install nvidia-driver-{this.LinuxDriverVersion}-server nvidia-dkms-{this.LinuxDriverVersion}-server -y");
commands.Add($"apt install cuda-drivers-fabricmanager-{this.LinuxDriverVersion} -y");
```
- **Docker**: A platform to use containers. MLPerf inference will run benchmarks within a docker container.  
Docker is installed with DockerInstallation.
```
{
    "Type": "DockerInstallation",
    "Parameters": {
        "Scenario": "InstallDocker"
    }
}
```
- **NVIDIA Container Toolkit**: A set of tools which enable the use of NVIDIA GPUs within docker containers.
```
{
    "Type": "NvidiaContainerToolkitInstallation",
    "Parameters": {
        "Scenario": "InstallNvidiaContainerToolkit"
    }
}
```

## Running a Benchmark
There are a few setup steps before running the benchmark:
- **make prebuild**: Download the docker container image and launch the container.
The remaining commands are run within the container. In order to avoid launching the docker container shell,
the file is replaced with Virtual Client. The [replacing Makefile.docker file](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions/MLPerf/Makefile.docker)
does not launch the docker container shell.
- **make download_data BENCHMARKS="bert"**: Download datasets necessary to run the benchmark.
- **make download_model BENCHMARKS="bert"**: Download pre-trained model to be tested.
- **make preprocess_data BENCHMARKS="bert"**: Formats data to be used in the benchmark.
- **make build**: Compile and build the executable to run the benchmark.

To actually run the benchmark:
- **make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=PerformanceOnly --fast**: Run performance mode which focuses
on the efficiency of the model in making predictions. The [output metadata.json file](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Examples/MLPerf/Example_performance_summary2.json)
will contain a valid/invalid output, and either the latency or throughput.
- **make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=AccuracyOnly --fast**: Run accuracy mode which focuses on
the accuracy of the model's predictions. The [output metadata.json file](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Actions.UnitTests/Examples/MLPerf/Example_accuracy_summary1.json)
will contain a pass/fail output, and the accuracy score.