# MLPerf
MLPerf 2.0 is a consortium of AI leaders from academia, research labs, and industry whose mission is to “build fair and useful benchmarks” that provide unbiased evaluations of training and inference performance for hardware, software, and services—all conducted under prescribed conditions. To stay on the cutting edge of industry trends, MLPerf continues to evolve, holding new tests at regular intervals and adding new workloads that represent the state of the art in AI.
* [MLPerf Training Documentation](https://github.com/mlcommons/training_results_v2.0/blob/main/MLPerf%E2%84%A2%20Training%20v2.0%20Results%20Discussion.pdf)  
* [MLPerf Inference Documentation](https://github.com/mlcommons/inference_results_v2.0)  
* [MLPerf Training Benchmarks](https://github.com/mlcommons/training_results_v2.0/tree/main/NVIDIA/benchmarks)
* [MLPerf Inference Benchmarks](https://github.com/mlcommons/inference_results_v2.0/tree/master/closed/NVIDIA)

## System Requirements
This is a GPU-specific workload and requires high-performance graphic cards to run. It is recommended that the system-under-test have a high-performing Nvidia (e.g. M60 or higher) or AMD (e.g. MI25 or higher)
graphics card.

## Supported Hardware Systems
The following section defines the hardware systems/SKUs on which the MLPerf 2.0 workload will run effectively in cloud environments. These hardware systems contain
GPU components for which the MLPerf workload is designed to test.

* **Datacenter systems**  
  * A100-SXM-80GBx8 (NVIDIA DGX A100, 80GB variant)
  * A100-SXM-80GBx4 (NVIDIA DGX Station A100, "Red October", 80GB variant)
  * A100-PCIex8 (80GB variant)
  * A2x2
  * A30x8

* **Edge Systems**
  * A100-SXM-80GBx1
  * A100-PCIex1 (80 GB variant)
  * A30x1
  * A2x1
  * Orin
  * Xavier NX

Additional details on whether a system is supported or not can be found in the documetation here: 
https://github.com/mlcommons/inference_results_v2.0/tree/master/closed/NVIDIA

## What is Being Measured?
GPU performance across a wide range of inference models. Work is planned for integrating support for training models as well.

* ~~**Training Benchmarks** (not currently supported)~~

* **Inference Benchmarks**  
  * bert
  * rnnt
  * ssd-mobilenet
  * ssd-resnet34
  * ~~resnet50 (not supported yet)~~
  * ~~DLRM (not supported yet)~~
  * ~~3D UNET (not supported yet)~~

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the MLPerf workload

|Scenario | Metric Name  | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------|--------------|---------------------|---------------------|---------------------|------|
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Server-PerformanceMode | 0.0 | 1.0 | 0.5333333333333333 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-PerformanceMode | 0.0 | 1.0 | 0.8333333333333334 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Server-PerformanceMode | 0.0 | 1.0 | 0.7954545454545454 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Server-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| bert | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Server-PerformanceMode | 0.0 | 1.0 | 0.9680851063829787 | VALID/INVALID |
| rnnt | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| rnnt | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| rnnt | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| rnnt | DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| ssd-mobilenet | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| ssd-mobilenet | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| ssd-mobilenet | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| ssd-mobilenet | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Server-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Server-AccuracyMode | 1.0 | 1.0 | 1.0 | PASS/FAIL |
| ssd-resnet34 | DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Server-PerformanceMode | 1.0 | 1.0 | 1.0 | VALID/INVALID |
