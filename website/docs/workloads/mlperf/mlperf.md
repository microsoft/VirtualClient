# MLPerf
MLPerf is a consortium of AI leaders from academia, research labs, and industry whose mission is to “build fair and useful benchmarks” that provide unbiased evaluations of training and inference performance for hardware, software, and services—all conducted under prescribed conditions. To stay on the cutting edge of industry trends, MLPerf continues to evolve, holding new tests at regular intervals and adding new workloads that represent the state of the art in AI.
* [MLPerf Training Documentation](https://github.com/mlcommons/training_results_v2.0/blob/main/MLPerf%E2%84%A2%20Training%20v2.0%20Results%20Discussion.pdf)  
* [MLPerf Inference Documentation](https://github.com/mlcommons/inference_results_v2.0)  
* [MLPerf Training Benchmarks](https://github.com/mlcommons/training_results_v2.0/tree/main/NVIDIA/benchmarks)
* [MLPerf Inference Benchmarks](https://github.com/mlcommons/inference_results_v2.0/tree/master/closed/NVIDIA)

### What is Being Tested?
GPU performance

### System Requirements
This is a GPU specific workload and requires high-performance graphic cards to run. 

MLPerf2.0 formally support and fully test the configuration files for the following systems:

### Datacenter systems:

* A100-SXM-80GBx8 (NVIDIA DGX A100, 80GB variant)
* A100-SXM-80GBx4 (NVIDIA DGX Station A100, "Red October", 80GB variant)
* A100-PCIex8 (80GB variant)
* A2x2
* A30x8


#### Edge Systems:

* A100-SXM-80GBx1
* A100-PCIex1 (80 GB variant)
* A30x1
* A2x1
* Orin
* Xavier NX


The details related to whether system is supported or not can be found here: [MLPerf](https://github.com/mlcommons/inference_results_v2.0/tree/master/closed/NVIDIA)

### Supported Platforms
* Linux x64 - Nvidia GPU

### Dependencies

#### For Nvidia GPUs:
The following dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the MLPerf workload. Note that the Virtual Client will handle the installation of any required dependencies.
1. CUDA and Nvidia GPU driver (Nvidia: nvidia-smi)
2. Docker CE
3. CUDA and Nvidia container toolkit
4. Actual GPU and turned on
