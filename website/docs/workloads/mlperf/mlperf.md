# MLPerf

MLPerf is a consortium of AI leaders from academia, research labs, and industry whose mission is to “build fair and useful benchmarks” that provide unbiased evaluations of training and inference performance for hardware, software, and services—all conducted under prescribed conditions. To stay on the cutting edge of industry trends, MLPerf continues to evolve, holding new tests at regular intervals and adding new workloads that represent the state of the art in AI.

* [MLPerf Training Documentation](https://github.com/mlcommons/training_results_v2.1/blob/main/MLPerf%E2%84%A2%20Training%20v2.0%20Results%20Discussion.pdf)  
* [MLPerf Inference Documentation](https://github.com/mlcommons/inference_results_v4.1)  
* [MLPerf Training Benchmarks](https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks)
* [MLPerf Inference Benchmarks](https://github.com/mlcommons/inference_results_v4.1/tree/master/closed/NVIDIA)
* [MLPerf Training Bert Preprocessing Data](./mlperf-trainining-bert-preprocessing-data.md)

## System Requirements

This is a GPU-specific workload and requires high-performance graphic cards to run. It is recommended that the system-under-test have a high-performing Nvidia (e.g. M60 or higher) or AMD (e.g. MI25 or higher)
graphics card.

## Supported Hardware Systems

The following section defines the hardware systems/SKUs on which the MLPerf workload will run effectively in cloud environments. These hardware systems contain
GPU components for which the MLPerf workload is designed to test.

* **Datacenter systems MLPerf Inference**  
  * A100-SXM4-40GBx8
  * A100-SXM-80GBx8 (NVIDIA DGX A100, 80GB variant)
  * A100-SXM-80GBx4 (NVIDIA DGX Station A100, "Red SEPTober", 80GB variant)
  * A100-PCIex8 (80GB variant)
  * A2x2
  * A30x8

* **Edge Systems MLPerf Inference**
  * A100-SXM-80GBx1
  * A100-PCIex1 (80 GB variant)
  * A30x1
  * A2x1
  * Orin
  * Xavier NX

* **Supported Config Files for MlPerf Bert Training (config_\{nodes}x\{gpus per node}x\{local batch size}x\{gradien accumulation}.sh)**
  * config_A30_1x2x224x14.sh
  * config_DGXA100_1x4x56x2.sh
  * config_DGXA100_1x8x56x1.sh
  * config_DGXA100_4gpu_common.sh
  * config_DGXA100_512x8x2x1_pack.sh
  * config_DGXA100_8x8x48x1.sh
  * config_DGXA100_common.sh

Source: [link](https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks/bert/implementations/pytorch-22.09)

Additional details on whether a system is supported or not can be found in the documetation here, 
for each benchmark check it's respective implementation folder :
https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks
https://github.com/mlcommons/inference_results_v4.1/tree/master/closed/NVIDIA

For systems which are not already included by MLPerf, add the config information in the appropriate \_\_init\_\_.py file under [GPUConfigFiles](https://github.com/microsoft/VirtualClient/tree/main/src/VirtualClient/VirtualClient.Actions/MLPerf/GPUConfigFiles).  
For example with A100-SXM4-40GBx8, we have the following section in the 3d-unet, SingleStream file which is copied during initialization: 
```
@ConfigRegistry.register(HarnessType.Custom, AccuracyTarget.k_99, PowerSetting.MaxP)
class A100_SXM4_40GBx8(A100_SXM4_40GBx1):
    system = KnownSystem.A100_SXM4_40GBx8
    gpu_batch_size = {'3d-unet': 8}
    start_from_device = True
    end_on_device = True
    single_stream_expected_latency_ns = 520000000
```

## What is Being Measured?

GPU performance across a wide range of inference models. Work is planned for integrating support for training models as well.

* **Training Benchmarks**
  * bert
  * ~~dlrm (not supported yet)~~
  * ~~maskrcnn (not supported yet)~~
  * ~~minigo (not supported yet)~~
  * ~~resnet (not supported yet)~~
  * ~~rnnt (not supported yet)~~
  * ~~ssd (not supported yet)~~
  * ~~unet3 (not supported yet)~~


* **Inference Benchmarks**  
  * bert
  * 3d-unet
  * ~~dlrm-v2 (not supported yet)~~
  * ~~gptj (not supported yet)~~
  * ~~llama2-70b (not supported yet)~~
  * ~~mixtral-8x7b (not supported yet)~~
  * ~~resnet50 (not supported yet)~~
  * ~~retinanet (not supported yet)~~
  * ~~stable-diffusion-xl (not supported yet)~~

## Workload Metrics MLPerf Inference

The following metrics are examples of those captured by the Virtual Client when running the MLPerf Inference workload.

|Scenario | Metric Name  | Example Value | Unit |
|---------|--------------|---------------|------|
| bert | PerformanceMode_p99 | 1.0 | VALID/INVALID |
| bert | latency_ns_p99 | 525066834 |   |
| bert | samples_per_second_p99 | 25.2768 | |
| bert | AccuracyMode_p99 | 1.0 | PASS/FAIL |
| bert | AccuracyValue_p99 | 0.86112 |  |
| bert | ThresholdValue_p99 | 0.853083 |  |
| bert | AccuracyThresholdRatio_p99 | 1.00831923740128 | PASS/FAIL |


## Workload Metrics MLPerf Training
| Scenario                                | Metric Name                    | Example Value (min)  | Example Value (max) | Example Value (avg) | Unit |
|-----------------------------------------|--------------------------------|----------------------|---------------------|---------------------|------|
| training-mlperf-bert-batchsize-45-gpu-8 | eval_mlm_accuracy                       | 0.650552854          | 0.672552854         | 0.662552854         | %    |
| training-mlperf-bert-batchsize-45-gpu-8 | e2e_time	                     | 1071.040571          | 1078.040571         | 1074.040571         | s    |
| training-mlperf-bert-batchsize-45-gpu-8 | training_sequences_per_second	 | 2288.463615          | 2300.463615         | 2295.463615         |      |
| training-mlperf-bert-batchsize-45-gpu-8 | final_loss	                   | 0	                  | 0                   | 0                   |      |
| training-mlperf-bert-batchsize-45-gpu-8 | raw_train_time	               | 1053.982237          | 1070.982237	        | 1063.982237         | s    |
