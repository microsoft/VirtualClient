# MLPerf Workload Metrics
The following document illustrates the type of results that are emitted by the MLPerf workload and captured by the
Virtual Client for net impact analysis.

* [MLPerf Training Benchmarks](https://github.com/mlcommons/training_results_v2.0/tree/main/NVIDIA/benchmarks)
* [MLPerf Inference Benchmarks](https://github.com/mlcommons/inference_results_v2.0/tree/master/closed/NVIDIA/code)    

### System Metrics
* [Performance Counters](./PerformanceCounterMetrics.md)
* [Power/Temperature Measurements](./PowerMetrics.md)  

### Workload-Specific Metrics
The following metrics are captured during the operations of the MLPerf workload.

# Training Benchmarks
Currently training benchmarks are not supported.

# Inference Benchmarks

* bert
* rnnt
* ssd-mobilenet
* ssd-resnet34
* resnet50 (TODO)
* DLRM (TODO)
* 3D UNET (TODO)

## bert

| Name                                                                   | Unit                   | Description                                               |
|------------------------------------------------------------------------|------------------------|-----------------------------------------------------------|
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-AccuracyMode     | PASS/FAIL |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version default and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-AccuracyMode | PASS/FAIL |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version default and mode accuracy passed or failed.    |
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Offline-AccuracyMode | PASS/FAIL |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version high_accuracy and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Server-AccuracyMode | PASS/FAIL |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version high_accuracy and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Offline-AccuracyMode | PASS/FAIL | Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version triton and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Server-AccuracyMode | PASS/FAIL | Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version triton and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-AccuracyMode | PASS/FAIL |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version high_accuracy_triton and mode accuracy passed or failed.|
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Server-AccuracyMode | PASS/FAIL |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version high_accuracy_triton and mode accuracy passed or failed.|
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Offline-PerformanceMode | VALID/INVALID |Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version default and mode performance valid or invalid.|
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_9_MaxP-Server-PerformanceMode | VALID/INVALID | Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version default and mode performance valid or invalid.|
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-PerformanceMode | VALID/INVALID |Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version high_accuracy and mode performance valid or invalid.. |
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Server-PerformanceMode | VALID/INVALID | Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version high_accuracy and mode performance valid or invalid. |
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-PerformanceMode | VALID/INVALID |  Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version triton and mode performance valid or invalid.|
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-PerformanceMode | VALID/INVALID |Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version triton and mode performance valid or invalid. |
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Offline-PerformanceMode | VALID/INVALID |Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario offline, config version high_accuracy_triton and mode performance valid or invalid. |
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_9_MaxP-Server-PerformanceMode | VALID/INVALID | Determines whether benchmark run on GPU DGX-A100_A100-SXM4-40GBx8 with scenario server, config version high_accuracy_triton and mode performance valid or invalid.| 
| 

## rnnt

| Name                                                                   | Unit                   | Description                                               |
|------------------------------------------------------------------------|------------------------|-----------------------------------------------------------|
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-AccuracyMode     | PASS/FAIL |  Determines whether benchmark run with scenario offline, config version default and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-AccuracyMode | PASS/FAIL | Determines whether benchmark run with scenario server, config version default and mode accuracy passed or failed. |
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Offline-PerformanceMode | VALID/INVALID | Determines whether benchmark run with scenario offline, config version default and mode performance valid or invalid. | 
| DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-PerformanceMode | VALID/INVALIID | Determines whether benchmark run with scenario server, config version default and mode performance valid or invalid. |

## ssd-mobilenet

| Name                                                                   | Unit                   | Description                                               |
|------------------------------------------------------------------------|------------------------|-----------------------------------------------------------|
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-AccuracyMode     | PASS/FAIL |  Determines whether benchmark run with scenario offline, config version triton and mode accuracy passed or failed.     |
| DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-AccuracyMode | PASS/FAIL |  Determines whether benchmark run with scenario offline, config version default and mode accuracy passed or failed.|
| DGX-A100_A100-SXM4-40GBx8_TRT_Triton-triton_k_99_MaxP-Offline-PerformanceMode | VALID/INVALID | Determines whether benchmark run with scenario offline, config version triton and mode performance valid or invalid.| 
| DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-PerformanceMode | VALID/INVALID | Determines whether benchmark run with scenario offline, config version default and mode performance valid or invalid.|

## ssd-resnet34

| Name                                                                   | Unit                   | Description                                               |
|------------------------------------------------------------------------|------------------------|-----------------------------------------------------------|
| DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Offline-PerformanceMode    | VALID/INVALID |  Determines whether benchmark run with scenario offline, config version default and mode performance valid or invalid.     |
| DGX-A100_A100-SXM4-40GBx8_TRT-lwis_k_99_MaxP-Server-PerformanceMode | VALID/INVALID | Determines whether benchmark run with scenario offline, config version server and mode performance valid or invalid.|
