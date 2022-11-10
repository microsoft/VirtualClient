---
id: superbenchmark
sidebar_position: 1
---

# SuperBenchmark Workload Suite
SuperBench is a validation and profiling tool for AI infrastructure. It highly specializes in GPU performance benchmarking.
* [SuperBenchmark Documentation](https://microsoft.github.io/superbenchmark/docs/introduction)  
* [SuperBenchmark Benchmarks](https://microsoft.github.io/superbenchmark/docs/user-tutorial/benchmarks/micro-benchmarks)

### What is Being Tested?
GPU performance

---
### System Requirements
This is a GPU specific workload and requires high-performance graphic cards to run. The system under test has to have some high-performing Nvidia(M60 or higher)/AMD(MI25 or higher) graphics card.

### Supported Platforms
* Linux x64 - Nvidia GPU
* Linux x64 - AMD GPU (Work in progress)

---
### Dependencies
The following dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the MLPerf workload. Note that the Virtual Client will handle the installation of any required dependencies.
1. GPU driver (Nvidia: nvidia-smi, AMD: rocm-smi)
2. Docker CE
3. CUDA and Nvidia container toolkit
4. Actual GPU and turned on


