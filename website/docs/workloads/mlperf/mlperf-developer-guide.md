# MLPerf Developer Guide
The following guide details the developer process for the MLPerf workload. The focus of this guide is on MLPerf *Inference*.

* [Workload Details](./mlperf.md)
* [Workload Profile](https://github.com/microsoft/VirtualClient/blob/main/src/VirtualClient/VirtualClient.Main/profiles/PERF-GPU-MLPERF.json)
* [MLPerf Inference Results Repository](https://github.com/mlcommons/inference_results_v4.1)

In machine learning, inference involves using an already trained model to make predictions on unseen data.
The MLPerf workload will run multiple benchmarks on a GPU-based system, in which the performance of the model
to make those predictions is measured. Throughput and latency are used as metrics.

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
- **high accuracy**: Uses higher precision and prioritizes accuracy over performance. (not supported yet)
- **triton**: Uses the triton inference server to manage and serve models. (not supported yet)
- **triton high accuracy**: Uses the triton inference server and higher precision. (not supported yet)

## Hardware for MLPerf
- **A100_SXM4_40GBx8**: Azure VM sku Standard_ND96asr_v4. This represents a system with 8 A100 NVIDIA GPUs. The NVIDIA A100 GPU
is designed for high-performance computing.

## Adding a config
The config information given the benchmark, scenario, config version, and system to test is in the [\_\_init\_\_.py file under the benchmark folder](https://github.com/mlcommons/inference_results_v4.1/blob/main/closed/NVIDIA/configs/bert/SingleStream/__init__.py).
```
├───configs
│   │   configuration.py
│   │   error.py
│   │
│   ├───3d-unet
│   │   │   __init__.py
│   │   │
│   │   ├───Offline
│   │   │       __init__.py
│   │   │
│   │   └───SingleStream
│   │           __init__.py
│   │
│   ├───bert
│   │   │   __init__.py
│   │   │
│   │   ├───Offline
│   │   │       __init__.py
│   │   │
│   │   ├───Server
│   │   │       __init__.py
│   │   │
│   │   └───SingleStream
│   │           __init__.py
```
By default, the repository does not have support for all systems (i.e. A100_SXM4_40GBx8). To add support, the file is replaced with Virtual Client at runtime.
For example with bert in the SingleStream scenario, a file with the following section is used:
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
Nvidia container toolkit is installed with NvidiaContainerToolkitInstallation.
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
on the efficiency of the model in making predictions. The json output will include a valid/invalid output, and either the latency or throughput.
```
{
    "benchmark_full": "bert-99",
    "benchmark_short": "bert",
    "config_name": "DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-SingleStream",
    "detected_system": "SystemConfiguration(host_cpu_conf=CPUConfiguration(layout={CPU(name=\"AMD EPYC 7V12 64-Core Processor\", architecture=CPUArchitecture.x86_64, core_count=48, threads_per_core=1): 2}), host_mem_conf=MemoryConfiguration(host_memory_capacity=Memory(quantity=928.7656999999999, byte_suffix=ByteSuffix.GB), comparison_tolerance=0.05), accelerator_conf=AcceleratorConfiguration(layout={GPU(name=\"NVIDIA A100-SXM4-40GB\", accelerator_type=AcceleratorType.Discrete, vram=Memory(quantity=40.0, byte_suffix=ByteSuffix.GiB), max_power_limit=400.0, pci_id=\"0x20B010DE\", compute_sm=80): 8}), numa_conf=NUMAConfiguration(numa_nodes={}, num_numa_nodes=4), system_id=\"DGX-A100_A100-SXM4-40GBx8\")",
    "early_stopping_met": true,
    "effective_min_duration_ms": 600000,
    "effective_min_query_count": 100,
    "result_90.00_percentile_latency_ns": 1924537,
    "result_validity": "INVALID",
    "satisfies_query_constraint": false,
    "scenario": "SingleStream",
    "scenario_key": "result_90.00_percentile_latency_ns",
    "summary_string": "result_90.00_percentile_latency_ns: 1924537, Result is INVALID, 10-min runtime requirement met: True",
    "system_name": "DGX-A100_A100-SXM4-40GBx8_TRT",
    "tensorrt_version": "10.2.0",
    "test_mode": "PerformanceOnly"
}
```
- **make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=AccuracyOnly --fast**: Run accuracy mode which focuses on
the accuracy of the model's predictions. The json output will inculde a pass/fail output, and the accuracy score.
```
{
    "accuracy": [
        {
            "name": "F1",
            "pass": true,
            "threshold": 89.96526,
            "value": 90.2147015680108
        }
    ],
    "accuracy_pass": true,
    "benchmark_full": "bert-99",
    "benchmark_short": "bert",
    "config_name": "DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline",
    "detected_system": "SystemConfiguration(host_cpu_conf=CPUConfiguration(layout={CPU(name=\"AMD EPYC 7V12 64-Core Processor\", architecture=CPUArchitecture.x86_64, core_count=48, threads_per_core=1): 2}), host_mem_conf=MemoryConfiguration(host_memory_capacity=Memory(quantity=928.7656999999999, byte_suffix=ByteSuffix.GB), comparison_tolerance=0.05), accelerator_conf=AcceleratorConfiguration(layout={GPU(name=\"NVIDIA A100-SXM4-40GB\", accelerator_type=AcceleratorType.Discrete, vram=Memory(quantity=40.0, byte_suffix=ByteSuffix.GiB), max_power_limit=400.0, pci_id=\"0x20B010DE\", compute_sm=80): 8}), numa_conf=NUMAConfiguration(numa_nodes={}, num_numa_nodes=4), system_id=\"DGX-A100_A100-SXM4-40GBx8\")",
    "effective_min_duration_ms": 600000,
    "effective_samples_per_query": 19800000,
    "satisfies_query_constraint": true,
    "scenario": "Offline",
    "scenario_key": "result_samples_per_second",
    "summary_string": "[PASSED] F1: 90.215 (Threshold=89.965)",
    "system_name": "DGX-A100_A100-SXM4-40GBx8_TRT",
    "tensorrt_version": "10.2.0",
    "test_mode": "AccuracyOnly"
}
```