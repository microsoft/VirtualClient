# SuperBenchmark
SuperBench is a validation and profiling tool for AI infrastructure. It highly specializes in GPU performance benchmarking.

* [Micro Benchmarks](https://microsoft.github.io/superbenchmark/docs/user-tutorial/benchmarks/micro-benchmarks)
* [Model Benchmarks](https://microsoft.github.io/superbenchmark/docs/user-tutorial/benchmarks/model-benchmarks)  
* [Docker Benchmarks](https://microsoft.github.io/superbenchmark/docs/user-tutorial/benchmarks/docker-benchmarks)  

## System Requirements
This is a GPU-specific workload and requires high-performance graphic cards to run. It is recommended that the system-under-test have a high-performing Nvidia (e.g. M60 or higher) or AMD (e.g. MI25 or higher)
graphics card.

## Dependencies
The following dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the Superbench workload. Note that the Virtual Client will handle the installation of any required dependencies.
1. GPU driver (Nvidia: nvidia-smi, AMD: rocm-smi)
2. Docker CE
3. CUDA and Nvidia container toolkit
4. Actual GPU and turned on

:warning:  
*Note that at the moment, the Virtual Client ONLY has support for Nvidia GPU systems. Work is underway to finalize support for the installation of drivers required in order to support AMD GPU systems.*

## What is Being Measured?
Measures the GEMM FLOPS for the GPU on the system using different float and int data types, with or without Tensor Core (XDLOPS), using by NVIDIA [cutlass](https://github.com/NVIDIA/cutlass/tree/ccb697bac77fcc898e9c897b2c90aa5b60ac72fb)
or AMD [rocblas-bench](https://github.com/ROCmSoftwarePlatform/rocBLAS/tree/develop/clients/benchmarks). The following benchmarks are supported:

### Computational Benchmarks
* **kernel-launch**  
  Measure GPU kernel launch latency which is defined as the time range from the beginning of the launch API call to the beginning of the kernel execution. The following examples illustrate
  the metrics that are emitted:

  | Name                     | Unit      | Description                          |
  |--------------------------|-----------|--------------------------------------|
  | kernel-launch/event_time | time (ms) | Launch latency measured in GPU time. |
  | kernel-launch/wall_time  | time (ms) | Launch latency measured in CPU time. |

* **gemm-flops**  
  Measure the GPU GEMM FLOPS for different float and int data types with or without Tensor Core (XDLOPS) using NVIDIA [cutlass](https://github.com/NVIDIA/cutlass/tree/ccb697bac77fcc898e9c897b2c90aa5b60ac72fb)
  or AMD [rocblas-bench](https://github.com/ROCmSoftwarePlatform/rocBLAS/tree/develop/clients/benchmarks). The following examples illustrate the metrics that are emitted:

  | Name                         | Unit           | Description                                             |
  |------------------------------|----------------|---------------------------------------------------------|
  | gemm-flops/fp64_flops        | FLOPS (GFLOPS) | GEMM float64 peak FLOPS.                                |
  | gemm-flops/fp32_flops        | FLOPS (GFLOPS) | GEMM float32 peak FLOPS.                                |
  | gemm-flops/fp16_flops        | FLOPS (GFLOPS) | GEMM float16 peak FLOPS.                                |
  | gemm-flops/fp64_tc_flops     | FLOPS (GFLOPS) | GEMM float64 peak FLOPS with NVIDIA Tensor Core.        |
  | gemm-flops/tf32_tc_flops     | FLOPS (GFLOPS) | GEMM tensor-float32 peak FLOPS with NVIDIA Tensor Core. |
  | gemm-flops/fp16_tc_flops     | FLOPS (GFLOPS) | GEMM float16 peak FLOPS with NVIDIA Tensor Core.        |
  | gemm-flops/bf16_tc_flops     | FLOPS (GFLOPS) | GEMM bfloat16 peak FLOPS with NVIDIA Tensor Core.       |
  | gemm-flops/int8_tc_iops      | IOPS (GIOPS)   | GEMM int8 peak IOPS with NVIDIA Tensor Core.            |
  | gemm-flops/int4_tc_iops      | IOPS (GIOPS)   | GEMM int4 peak IOPS with NVIDIA Tensor Core.            |
  | gemm-flops/fp32_xdlops_flops | FLOPS (GFLOPS) | GEMM tensor-float32 peak FLOPS with AMD XDLOPS.         |
  | gemm-flops/fp16_xdlops_flops | FLOPS (GFLOPS) | GEMM float16 peak FLOPS with AMD XDLOPS.                |
  | gemm-flops/bf16_xdlops_flops | FLOPS (GFLOPS) | GEMM bfloat16 peak FLOPS with AMD XDLOPS.               |

* **matmul**  
  Large scale matmul operation using `torch.matmul` with one GPU. The following examples illustrate the metrics that are emitted:

  | Name                           | Unit      | Description                    |
  |--------------------------------|-----------|--------------------------------|
  | pytorch-matmul/nosharding_time | time (ms) | Time of pure matmul operation. |

* **tensorrt-inference**  
  Inference PyTorch/ONNX models on NVIDIA GPUs with [TensorRT](https://developer.nvidia.com/tensorrt). The following models are currently supported:
  * alexnet
  * densenet121
  * densenet169
  * densenet201
  * densenet161
  * googlenet
  * inception_v3
  * mnasnet0_5
  * mnasnet1_0
  * mobilenet_v2
  * resnet18
  * resnet34
  * resnet50
  * resnet101
  * resnet152
  * resnext50_32x4d
  * resnext101_32x8d
  * wide_resnet50_2
  * wide_resnet101_2
  * shufflenet_v2_x0_5
  * shufflenet_v2_x1_0
  * squeezenet1_0
  * squeezenet1_1
  * vgg11
  * vgg11_bn
  * vgg13
  * vgg13_bn
  * vgg16
  * vgg16_bn
  * vgg19_bn
  * vgg19

  The following examples illustrate the metrics that are emitted:

  | Name                                             | Unit      | Description                                                                                              |
  |--------------------------------------------------|-----------|----------------------------------------------------------------------------------------------------------|
  | tensorrt-inference/$\{model}_gpu_time_mean        | time (ms) | The mean GPU latency to execute the kernels for a query.                                                 |
  | tensorrt-inference/$\{model}_gpu_time_99          | time (ms) | The 99th percentile GPU latency to execute the kernels for a query.                                      |
  | tensorrt-inference/$\{model}_host_time_mean       | time (ms) | The mean H2D, GPU, and D2H latency to execute the kernels for a query.                                   |
  | tensorrt-inference/$\{model}_host_time_99         | time (ms) | The 99th percentile H2D, GPU, and D2H latency to execute the kernels for a query.                        |
  | tensorrt-inference/$\{model}_end_to_end_time_mean | time (ms) | The mean duration from when the H2D of a query is called to when the D2H of the same query is completed. |
  | tensorrt-inference/$\{model}_end_to_end_time_99   | time (ms) | The P99 duration from when the H2D of a query is called to when the D2H of the same query is completed.  |

* **ort-inference**  
  Inference performance of the torchvision models using ONNXRuntime. The following models are currently supported:
  * alexnet
  * densenet121
  * densenet169
  * densenet201
  * densenet161
  * googlenet
  * inception_v3
  * mnasnet0_5
  * mnasnet1_0
  * mobilenet_v2
  * resnet18
  * resnet34
  * resnet50
  * resnet101
  * resnet152
  * resnext50_32x4d
  * resnext101_32x8d
  * wide_resnet50_2
  * wide_resnet101_2
  * shufflenet_v2_x0_5
  * shufflenet_v2_x1_0
  * squeezenet1_0
  * squeezenet1_1
  * vgg11
  * vgg11_bn
  * vgg13
  * vgg13_bn
  * vgg16
  * vgg16_bn
  * vgg19_bn
  * vgg19

  The following examples illustrate the metrics that are emitted:

  | Name                                          | Unit      | Description                                               |
  |-----------------------------------------------|-----------|-----------------------------------------------------------|
  | ort-inference/\{precision}_\{model}_time        | time (ms) | The mean latency to execute one batch of inference.       |

### Communication Benchmarks
* **membw**  
  Measure the memory copy bandwidth across PCI-e and memory copy bandwidth between GPUs, using [NVIDIA](https://github.com/NVIDIA/cuda-samples/tree/master/Samples/bandwidthTest)
  or [AMD](https://github.com/ROCm-Developer-Tools/HIP/tree/master/samples/1_Utils/hipBusBandwidth) bandwidth test tools. The following examples illustrate the metrics that are emitted:

  | Name          | Unit             | Description                      |
  |---------------|------------------|----------------------------------|
  | mem-bw/h2d_bw | bandwidth (GB/s) | Host to device copy bandwidth.   |
  | mem-bw/d2h_bw | bandwidth (GB/s) | Device to host copy bandwidth.   |
  | mem-bw/d2d_bw | bandwidth (GB/s) | Device to device copy bandwidth. |

* **gpu-copy-bw**  
  Measure the memory copy bandwidth performed by GPU SM/DMA engine including device-to-host, host-to-device and device-to-device. The following examples illustrate the metrics 
  that are emitted:

  | Name                                                                          | Unit             | Description                                                                                                                |
  |-------------------------------------------------------------------------------|------------------|----------------------------------------------------------------------------------------------------------------------------|
  | cpu\_to\_gpu[0-9]+\_by\_gpu[0-9]+\_using\_(sm\|dma)\_under_numa[0-9]+_bw      | bandwidth (GB/s) | The bandwidth reading from all NUMA nodes' host memory using DMA engine or GPU SM by all GPUs.                             |
  | gpu[0-9]+\_to\_cpu\_by\_gpu[0-9]+\_using\_(sm\|dma)\_under_numa[0-9]+_bw      | bandwidth (GB/s) | The bandwidth writing to all NUMA nodes' host memory using DMA engine or GPU SM by all GPUs.                               |
  | gpu[0-9]+\_to_gpu[0-9]+\_by\_gpu[0-9]+\_using\_(sm\|dma)\_under_numa[0-9]+_bw | bandwidth (GB/s) | The bandwidth reading from  or writing to all GPUs using DMA engine or GPU SM by all GPUs with peer communication enabled. |

* **ib-loopback**  
  Measure the InfiniBand loopback verbs bandwidth using [OFED performance tests](https://github.com/linux-rdma/perftest/tree/7504ce48ac396a02f4d00de359257b2cb8458f06). The following examples illustrate 
  the metrics that are emitted:

  | Name                                        | Unit             | Description                                                  |
  |---------------------------------------------|------------------|--------------------------------------------------------------|
  | ib-loopback/ib_write_$\{msg_size}_ib[0-9]_bw | bandwidth (GB/s) | InfiniBand loopback write bandwidth with given message size. |
  | ib-loopback/ib_read_$\{msg_size}_ib[0-9]_bw  | bandwidth (GB/s) | InfiniBand loopback read bandwidth with given message size.  |
  | ib-loopback/ib_send_$\{msg_size}_ib[0-9]_bw  | bandwidth (GB/s) | InfiniBand loopback send bandwidth with given message size.  |

* **ib-traffic**  
  Measure the InfiniBand performance under multi nodes' traffic pattern. The traffic pattern is defined in a config file, which is pre-defined for one-to-many, many-to-one and all-to-all patterns.
  Each row in the config is one round, and all pairs of nodes in a row run ib command simultaneous. The following examples illustrate the metrics that are emitted:

  | Metrics                                                       | Unit             | Description                                                                                                                                                                                                                         |
  |---------------------------------------------------------------|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
  | ib-traffic/$\{command}_$\{line}_$\{pair}_$\{server}_$\{client}_bw  | bandwidth (GB/s) | The max bandwidth of ib command (ib_write_bw, ib_send_bw, ib_read_bw) run between the $\{pair}<sup>th</sup> node pair in the $\{line}<sup>th</sup> line of the config, $\{server} and $\{client} are the hostname of server and client  |
  | ib-traffic/$\{command}_$\{line}_$\{pair}_$\{server}_$\{client}_lat | time (us)        | The max latency of ib command (ib_write_lat, ib_send_lat, ib_read_lat) run between the $\{pair}<sup>th</sup> node pair in the $\{line}<sup>th</sup> line of the config, $\{server} and $\{client} are the hostname of server and client |

* **nccl-bw/rccl-bw**  
  Measure the performance of NCCL/RCCL operations using [nccl-tests](https://github.com/NVIDIA/nccl-tests/tree/44df0bf010dcc95e840ca0fb7466c67cff3f1f0f)
  or [rccl-tests](https://github.com/ROCmSoftwarePlatform/rccl-tests/tree/dc1ad4853d7ec738387d42a75a58a98d7af00c7b). The following operations are currently supported:
  * allreduce
  * allgather
  * broadcast
  * reduce
  * reducescatter
  * alltoall

  The following examples illustrate the metrics that are emitted:

  | Name                                   | Unit             | Description                                                 |
  |----------------------------------------|------------------|-------------------------------------------------------------|
  | nccl-bw/$\{operation}_$\{msg_size}_time  | time (us)        | NCCL operation latency with given message size.            |
  | nccl-bw/$\{operation}_$\{msg_size}_algbw | bandwidth (GB/s) | NCCL operation algorithm bandwidth with given message size. |
  | nccl-bw/$\{operation}_$\{msg_size}_busbw | bandwidth (GB/s) | NCCL operation bus bandwidth with given message size.       |
  | rccl-bw/$\{operation}_$\{msg_size}_time  | time (us)        | RCCL operation latency with given message size.            |
  | rccl-bw/$\{operation}_$\{msg_size}_algbw | bandwidth (GB/s) | RCCL operation algorithm bandwidth with given message size. |
  | rccl-bw/$\{operation}_$\{msg_size}_busbw | bandwidth (GB/s) | RCCL operation bus bandwidth with given message size.       |

* **tcp-connectivity**  
  Test the TCP connectivity between current node and nodes in the hostfile using [tcping](https://github.com/zhengxiaowai/tcping) tools. The following examples illustrate the metrics that are emitted:

  | Metrics                                         | Unit      | Description                                                                           |
  |-------------------------------------------------|-----------|---------------------------------------------------------------------------------------|
  | tcp-connectivity/$\{hostname/ip}_succeeded_count | count     | succeeded times of tcp connections between current node and other nodes               |
  | tcp-connectivity/$\{hostname/ip}_failed_count    | count     | failed times of tcp connections between current node and other nodes                  |
  | tcp-connectivity/$\{hostname/ip}_success_rate    |           | success rate (successed/total) of tcp connection between current node and other nodes |
  | tcp-connectivity/$\{hostname/ip}_time_min        | time (ms) | minimum latency of tcp connections between current node and other nodes               |
  | tcp-connectivity/$\{hostname/ip}_time_max        | time (ms) | maximum latency of tcp connections between current node and other nodes               |
  | tcp-connectivity/$\{hostname/ip}_time_avg        | time (ms) | average latency of tcp connections between current node and other nodes               |

* **gpcnet-network-test/gpcnet-network-load-test**  
  Distributed test of the global network performance and congestion using [GPCNET](https://github.com/netbench/GPCNET) tools. The following variations are supported:
  * gpcnet-network-test: Full system network tests in random and natural ring, alltoall and allreduce, at least 2 nodes
  * gpcnet-network-load-test: Select full system network tests run with four congestors to measure network congestion or contention, at least 10 nodes
    - supporting network tests: RR Two-sided Lat (8 B), RR Get Lat (8 B), RR Two-sided BW (131072 B), RR Put BW (131072 B), RR Two-sided BW+Sync (131072 B), Nat Two-sided BW (131072 B), Multiple Allreduce (8 B), Multiple Alltoall (4096 B)
    - supporting congestors: Alltoall (4096 B), Two-sided Incast (4096 B), Put Incast (4096 B), Get Bcast (4096 B)

  The following examples illustrate the metrics that are emitted:

  | Metrics                                                 | Unit                   | Description                                                                                                                                                                |
  |---------------------------------------------------------|------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
  | gpcnet-network-test/rr_two-sided_lat_$\{stat}            | time (us)              | statistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'random ring communication pattern two-side latency' for network testing                 |
  | gpcnet-network-test/rr_two-sided+sync_bw_$\{stat}        | bandwidth (MiB/s/rank) | fstatistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'random ring communication pattern two-side bandwidth with barrier' for network testing |
  | gpcnet-network-test/multiple_allreduce_time_$\{stat}     | time (us)              | statistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'multiple allreduce bandwidth' for network testing                                       |
  | gpcnet-network-test/rr_get_lat_$\{stat}                  | bandwidth (MiB/s/rank) | statistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'RR GetLat (8 B)' for network testing                                                    |
  | gpcnet-network-test/rr_two-sided_bw_$\{stat}             | bandwidth (MiB/s/rank) | statistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'RR Two-sidedBW (131072 B)' for network testing                                          |
  | gpcnet-network-test/nat_two-sided_bw_$\{stat}            | bandwidth (MiB/s/rank) | statistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'Nat Two-sidedBW (131072 B)' for network testing                                         |
  | gpcnet-network-test/multiple_alltoall_bw_$\{stat}        | bandwidth (MiB/s/rank) | statistical values(min, max, avg, 99%, 99.9%) obtained by all nodes use algorithm 'Multiple Alltoall (4096 B)' for network testing                                         |
  | gpcnet-network-load-test/rr_two-sided_lat_x_$\{stat}     | factor (x)             | summary about congestion impact factor of the network test algorithm                                                                                                       |
  | gpcnet-network-load-test/rr_two-sided+sync_bw_x_$\{stat} | factor (x)             | summary about congestion impact factor of the network test algorithm                                                                                                       |
  | gpcnet-network-load-test/multiple_allreduce_x_$\{stat}   | factor (x)             | summary about congestion impact factor of the network test algorithm                                                                                                       |

### Computation-Communication Benchmarks
* **computation-communication-overlap**  
  Test the performance of single node when communication and computation overlap. The following examples illustrate the metrics that are emitted:

  | Name                                                  | Unit      | Description                                                  |
  |-------------------------------------------------------|-----------|--------------------------------------------------------------|
  | pytorch-computation-communication-overlap/mul_time    | time (ms) | Time of communication and mul kernel computation overlap.    |
  | pytorch-computation-communication-overlap/matmul_time | time (ms) | Time of communication and matmul kernel computation overlap. |

* **sharding-matmul**  
  Test the performance of large scale matmul operation with multiple GPUs:
  * allreduce: Each GPU will calculate part of the MM calculation, and use AllReduce to merge all data into one tensor.
  * allgather: Each GPU will calculate part of the MM calculation, and use AllGather + Concat to merge all data into one tensor.

  The following examples illustrate the metrics that are emitted:

  | Name                                   | Unit      | Description                              |
  |----------------------------------------|-----------|------------------------------------------|
  | pytorch-sharding-matmul/allreduce_time | time (ms) | Time of sharding matmul using allreduce. |
  | pytorch-sharding-matmul/allgather_time | time (ms) | Time of sharding matmul using allgather. |

### Storage Benchmarks
* **disk-benchmark**  
  Measure the disk performance through [FIO](https://github.com/axboe/fio/tree/0313e938c9c8bb37d71dade239f1f5326677b079). The following examples illustrate the metrics that are emitted:

  | Name                                                          | Unit         | Description                                              |
  |---------------------------------------------------------------|--------------|----------------------------------------------------------|
  | disk-benchmark/$\{disk_name}_rand_read_write_bs                | size (bytes) | Disk random read write block size.                       |
  | disk-benchmark/$\{disk_name}_rand_read_write_read_iops         | IOPS         | Disk random read write read IOPS.                        |
  | disk-benchmark/$\{disk_name}_rand_read_write_read_lat_ns_95.0  | time (ns)    | Disk random read write read latency in 95.0 percentile.  |
  | disk-benchmark/$\{disk_name}_rand_read_write_read_lat_ns_99.0  | time (ns)    | Disk random read write read latency in 99.0 percentile.  |
  | disk-benchmark/$\{disk_name}_rand_read_write_read_lat_ns_99.9  | time (ns)    | Disk random read write read latency in 99.9 percentile.  |
  | disk-benchmark/$\{disk_name}_rand_read_write_write_iops        | IOPS         | Disk random read write write IOPS.                       |
  | disk-benchmark/$\{disk_name}_rand_read_write_write_lat_ns_95.0 | time (ns)    | Disk random read write write latency in 95.0 percentile. |
  | disk-benchmark/$\{disk_name}_rand_read_write_write_lat_ns_99.0 | time (ns)    | Disk random read write write latency in 99.0 percentile. |
  | disk-benchmark/$\{disk_name}_rand_read_write_write_lat_ns_99.9 | time (ns)    | Disk random read write write latency in 99.9 percentile. |

### Model Benchmarks
* **gpt_models**  
  PyTorch model running training or inference tasks with single or half precision for GPT models including gpt2-small, gpt2-medium, gpt2-large and gpt2-xl.
  The following examples illustrate the metrics that are emitted:

  | Name                                                       | Unit                   | Description                                 |
  |------------------------------------------------------------|------------------------|---------------------------------------------|
  | gpt_models/pytorch-$\{model_name}/fp32_train_step_time      | time (ms)              | Train step time with single precision.      |
  | gpt_models/pytorch-$\{model_name}/fp32_train_throughput     | throughput (samples/s) | Train throughput with single precision.     |
  | gpt_models/pytorch-$\{model_name}/fp32_inference_step_time  | time (ms)              | Inference step time with single precision.  |
  | gpt_models/pytorch-$\{model_name}/fp32_inference_throughput | throughput (samples/s) | Inference throughput with single precision. |
  | gpt_models/pytorch-$\{model_name}/fp16_train_step_time      | time (ms)              | Train step time with half precision.        |
  | gpt_models/pytorch-$\{model_name}/fp16_train_throughput     | throughput (samples/s) | Train throughput with half precision.       |
  | gpt_models/pytorch-$\{model_name}/fp16_inference_step_time  | time (ms)              | Inference step time with half precision.    |
  | gpt_models/pytorch-$\{model_name}/fp16_inference_throughput | throughput (samples/s) | Inference throughput with half precision.   |

* **bert_models**  
  PyTorch model running training or inference tasks with single or half precision for BERT models including bert-base and bert-large.
  The following examples illustrate the metrics that are emitted:

  | Name                                                        | Unit                   | Description                                 |
  |-------------------------------------------------------------|------------------------|---------------------------------------------|
  | bert_models/pytorch-$\{model_name}/fp32_train_step_time      | time (ms)              | Train step time with single precision.      |
  | bert_models/pytorch-$\{model_name}/fp32_train_throughput     | throughput (samples/s) | Train throughput with single precision.     |
  | bert_models/pytorch-$\{model_name}/fp32_inference_step_time  | time (ms)              | Inference step time with single precision.  |
  | bert_models/pytorch-$\{model_name}/fp32_inference_throughput | throughput (samples/s) | Inference throughput with single precision. |
  | bert_models/pytorch-$\{model_name}/fp16_train_step_time      | time (ms)              | Train step time with half precision.        |
  | bert_models/pytorch-$\{model_name}/fp16_train_throughput     | throughput (samples/s) | Train throughput with half precision.       |
  | bert_models/pytorch-$\{model_name}/fp16_inference_step_time  | time (ms)              | Inference step time with half precision.    |
  | bert_models/pytorch-$\{model_name}/fp16_inference_throughput | throughput (samples/s) | Inference throughput with half precision.   |

* **lstm_models**  
  PyTorch model running training or inference tasks with single or half precision for one bidirectional LSTM model. The following examples illustrate the metrics that are emitted:

  | Name                                               | Unit                   | Description                                 |
  |----------------------------------------------------|------------------------|---------------------------------------------|
  | lstm_models/pytorch-lstm/fp32_train_step_time      | time (ms)              | Train step time with single precision.      |
  | lstm_models/pytorch-lstm/fp32_train_throughput     | throughput (samples/s) | Train throughput with single precision.     |
  | lstm_models/pytorch-lstm/fp32_inference_step_time  | time (ms)              | Inference step time with single precision.  |
  | lstm_models/pytorch-lstm/fp32_inference_throughput | throughput (samples/s) | Inference throughput with single precision. |
  | lstm_models/pytorch-lstm/fp16_train_step_time      | time (ms)              | Train step time with half precision.        |
  | lstm_models/pytorch-lstm/fp16_train_throughput     | throughput (samples/s) | Train throughput with half precision.       |
  | lstm_models/pytorch-lstm/fp16_inference_step_time  | time (ms)              | Inference step time with half precision.    |
  | lstm_models/pytorch-lstm/fp16_inference_throughput | throughput (samples/s) | Inference throughput with half precision.   |

* **cnn_models**  
  PyTorch model running training or inference tasks with single or half precision for CNN models listed in [`torchvision.models`](https://pytorch.org/vision/0.8/models.html), including:
  * resnet: resnet18, resnet34, resnet50, resnet101, resnet152
  * resnext: resnext50_32x4d, resnext101_32x8d
  * wide_resnet: wide_resnet50_2, wide_resnet101_2
  * densenet: densenet121, densenet169, densenet201, densenet161
  * vgg: vgg11, vgg11_bn, vgg13, vgg13_bn, vgg16, vgg16_bn, vgg19_bn, vgg19
  * mnasnet: mnasnet0_5, mnasnet0_75, mnasnet1_0, mnasnet1_3
  * mobilenet: mobilenet_v2
  * shufflenet: shufflenet_v2_x0_5, shufflenet_v2_x1_0, shufflenet_v2_x1_5, shufflenet_v2_x2_0
  * squeezenet: squeezenet1_0, squeezenet1_1
  * others: alexnet, googlenet, inception_v3
  
  The following examples illustrate the metrics that are emitted:

  | Name                                                       | Unit                   | Description                                 |
  |------------------------------------------------------------|------------------------|---------------------------------------------|
  | cnn_models/pytorch-$\{model_name}/fp32_train_step_time      | time (ms)              | Train step time with single precision.      |
  | cnn_models/pytorch-$\{model_name}/fp32_train_throughput     | throughput (samples/s) | Train throughput with single precision.     |
  | cnn_models/pytorch-$\{model_name}/fp32_inference_step_time  | time (ms)              | Inference step time with single precision.  |
  | cnn_models/pytorch-$\{model_name}/fp32_inference_throughput | throughput (samples/s) | Inference throughput with single precision. |
  | cnn_models/pytorch-$\{model_name}/fp16_train_step_time      | time (ms)              | Train step time with half precision.        |
  | cnn_models/pytorch-$\{model_name}/fp16_train_throughput     | throughput (samples/s) | Train throughput with half precision.       |
  | cnn_models/pytorch-$\{model_name}/fp16_inference_step_time  | time (ms)              | Inference step time with half precision.    |
  | cnn_models/pytorch-$\{model_name}/fp16_inference_throughput | throughput (samples/s) | Inference throughput with half precision.   |

### Docker-based Benchmarks
* **ort-models**  
  Run the rocm onnxruntime model training benchmarks packaged in docker `superbench/benchmark:rocm4.3.1-onnxruntime1.9.0` which includes Bert-large, Distilbert-base, GPT-2, 
  facebook/Bart-large and Roberta-large. The following examples illustrate the metrics that are emitted:

  | Name                                                                   | Unit                   | Description                                               |
  |------------------------------------------------------------------------|------------------------|-----------------------------------------------------------|
  | onnxruntime-ort-models/bert_large_uncased_ngpu_1_train_throughput      | throughput (samples/s) | The throughput of bert large uncased model on 1 GPU.      |
  | onnxruntime-ort-models/bert_large_uncased_ngpu_8_train_throughput      | throughput (samples/s) | The throughput of bert large uncased model on 8 GPU.      |
  | onnxruntime-ort-models/distilbert_base_uncased_ngpu_1_train_throughput | throughput (samples/s) | The throughput of distilbert base uncased model on 1 GPU. |
  | onnxruntime-ort-models/distilbert_base_uncased_ngpu_8_train_throughput | throughput (samples/s) | The throughput of distilbert base uncased model on 8 GPU. |
  | onnxruntime-ort-models/gpt2_ngpu_1_train_throughput                    | throughput (samples/s) | The throughput of gpt2 model on 1 GPU.                    |
  | onnxruntime-ort-models/gpt2_ngpu_8_train_throughput                    | throughput (samples/s) | The throughput of gpt2 model on 8 GPU.                    |
  | onnxruntime-ort-models/facebook_bart_large_ngpu_1_train_throughput     | throughput (samples/s) | The throughput of facebook bart large model on 1 GPU.     |
  | onnxruntime-ort-models/facebook_bart_large_ngpu_8_train_throughput     | throughput (samples/s) | The throughput of facebook bart large model on 8 GPU.     |
  | onnxruntime-ort-models/roberta_large_ngpu_1_train_throughput           | throughput (samples/s) | The throughput of roberta large model on 1 GPU.           |
  | onnxruntime-ort-models/roberta_large_ngpu_8_train_throughput           | throughput (samples/s) | The throughput of roberta large model on 8 GPU.           |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the SuperBenchmark workload.

| Metric Name  | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|--------------|---------------------|---------------------|---------------------|------|
| bert_models/pytorch-bert-base/fp16_train_step_time | 44.9216029047966 | 286.86057925224307 | 201.96287847311863 |  |
| bert_models/pytorch-bert-base/fp16_train_throughput | 6.972049207997031 | 373.9878279479464 | 27.849846571717746 |  |
| bert_models/pytorch-bert-base/fp32_train_step_time | 47.20543313026428 | 361.39744567871096 | 251.49612356031853 |  |
| bert_models/pytorch-bert-base/fp32_train_throughput | 5.534098838203878 | 279.62370507399876 | 24.097880131546327 |  |
| bert_models/pytorch-bert-base/return_code | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-base/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/fp16_train_step_time | 203.466139562428 | 206.25331059098245 | 204.63056197638313 |  |
| bert_models/pytorch-bert-large/fp16_train_throughput | 155.310178377294 | 157.3466631563798 | 156.47806609792213 |  |
| bert_models/pytorch-bert-large/fp32_train_step_time | 304.83736431598666 | 314.97883063554766 | 308.7339255611102 |  |
| bert_models/pytorch-bert-large/fp32_train_throughput | 101.65049135797402 | 104.99603397183249 | 103.69163683795266 |  |
| bert_models/pytorch-bert-large/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| bert_models/pytorch-bert-large/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/matmul_time:0 | 124.91917255175777 | 134.1622295126954 | 128.05827917765303 |  |
| computation-communication-overlap/matmul_time:1 | 124.91912217480473 | 133.51491016210938 | 127.95046343929033 |  |
| computation-communication-overlap/matmul_time:2 | 124.91913861621096 | 134.16198100634768 | 128.05830186197918 |  |
| computation-communication-overlap/matmul_time:3 | 124.91915493115214 | 133.51495779492195 | 127.950445782959 |  |
| computation-communication-overlap/matmul_time:4 | 124.91920877197282 | 134.1613300390625 | 128.05821213769537 |  |
| computation-communication-overlap/matmul_time:5 | 124.91912800097671 | 133.51496286474606 | 127.84491153068034 |  |
| computation-communication-overlap/matmul_time:6 | 124.91904205419924 | 134.16193414843748 | 128.05822409733069 |  |
| computation-communication-overlap/matmul_time:7 | 124.91913599414062 | 133.51493672265625 | 127.95023592700191 |  |
| computation-communication-overlap/mul_time:0 | 45.07996017968763 | 46.05545878808592 | 45.406273460205138 |  |
| computation-communication-overlap/mul_time:1 | 45.07994472656263 | 46.055451313476549 | 45.40627131144208 |  |
| computation-communication-overlap/mul_time:2 | 45.07993932763665 | 46.05544633300784 | 45.40626473120116 |  |
| computation-communication-overlap/mul_time:3 | 45.0799464775391 | 46.05546883642586 | 45.40627796337889 |  |
| computation-communication-overlap/mul_time:4 | 45.079920461914117 | 46.05548028613282 | 45.40627255712889 |  |
| computation-communication-overlap/mul_time:5 | 45.07995083642569 | 46.05550472363262 | 45.40627432942705 |  |
| computation-communication-overlap/mul_time:6 | 45.079942099121257 | 46.05546691406248 | 45.40626869962565 |  |
| computation-communication-overlap/mul_time:7 | 45.07994110107427 | 46.05546507958969 | 45.406253526123048 |  |
| computation-communication-overlap/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| computation-communication-overlap/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:0 | 113.1502 | 113.5731 | 113.28080166666666 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:1 | 113.15234 | 113.52696 | 113.36456833333334 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:2 | 113.19463999999999 | 113.63356999999999 | 113.36563666666666 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:3 | 113.2748 | 113.56794000000001 | 113.43090833333334 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:4 | 113.25257 | 113.6575 | 113.40207833333334 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:5 | 113.29926 | 113.63622 | 113.42657833333333 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:6 | 113.18889 | 113.74152 | 113.37978333333332 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_128_n_32_transa_0_transb_1_time:7 | 113.2762 | 113.42837 | 113.31880833333334 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:0 | 72.421446 | 73.626541 | 73.12794016666668 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:1 | 72.78713 | 73.334909 | 73.016355 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:2 | 72.215593 | 73.082976 | 72.80132233333335 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:3 | 72.430615 | 73.780104 | 72.92495666666666 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:4 | 71.942314 | 73.086683 | 72.63142816666667 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:5 | 71.904016 | 73.242266 | 72.38534 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:6 | 72.038079 | 73.390659 | 72.85798766666666 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_128_m_64_n_32_transa_0_transb_1_time:7 | 72.400272 | 73.289402 | 72.968667 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:0 | 12.063581 | 12.550694 | 12.301183166666667 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:1 | 12.120797 | 12.529367 | 12.323395499999999 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:2 | 12.150376 | 12.590127 | 12.391484333333333 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:3 | 12.176294 | 12.461474 | 12.288187166666665 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:4 | 12.126611 | 12.496182 | 12.327343166666666 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:5 | 12.180138 | 12.64185 | 12.350372 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:6 | 12.292151 | 12.453361 | 12.376233499999998 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_3_m_64_n_32_transa_0_transb_1_time:7 | 12.050917 | 12.430579999999999 | 12.311228999999999 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:0 | 64.769788 | 64.968454 | 64.82417916666667 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:1 | 64.858662 | 65.191664 | 65.03132583333333 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:2 | 64.874521 | 65.148178 | 64.99576350000001 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:3 | 64.936419 | 65.150449 | 65.07999566666668 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:4 | 64.882711 | 65.328656 | 65.03074199999999 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:5 | 64.939196 | 65.148223 | 65.074586 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:6 | 64.876778 | 65.069566 | 64.98883466666666 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_128_n_32_transa_0_transb_1_time:7 | 64.93994000000001 | 65.154808 | 65.05331050000001 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:0 | 37.555338 | 37.886365 | 37.68242733333333 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:1 | 38.229974 | 38.477798 | 38.324844166666668 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:2 | 38.149625 | 38.467377 | 38.267830499999998 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:3 | 38.257528 | 38.442294 | 38.338148 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:4 | 38.040112 | 38.368485 | 38.23796066666667 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:5 | 37.957108 | 38.355858 | 38.18870033333334 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:6 | 38.077443 | 38.440592 | 38.27283933333333 |  |
| cublas-function/name_cublascgemm3mstridedbatched_batchcount_544_k_64_m_64_n_32_transa_1_transb_0_time:7 | 38.059838 | 38.336361 | 38.22762683333333 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:0 | 21.669418 | 21.864881 | 21.7927535 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:1 | 21.73701 | 21.965488 | 21.80511233333333 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:2 | 21.659621 | 21.819876 | 21.737663833333334 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:3 | 21.758799 | 21.986409 | 21.857205000000005 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:4 | 21.624662 | 22.099623 | 21.794980499999999 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:5 | 21.682958 | 21.805218 | 21.74106 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:6 | 21.679599 | 21.851159 | 21.794716166666665 |  |
| cublas-function/name_cublascgemm_k_32_m_2048_n_512_transa_1_transb_0_time:7 | 21.750638 | 21.984616 | 21.820670333333337 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:0 | 21.69218 | 21.802833 | 21.75483433333333 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:1 | 21.678527 | 21.751181 | 21.731092500000004 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:2 | 21.681291 | 21.740634 | 21.706030166666669 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:3 | 21.66799 | 21.918496 | 21.76981033333333 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:4 | 21.605535 | 21.812716 | 21.699059166666669 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:5 | 21.669266 | 21.834082 | 21.7293715 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:6 | 21.601166 | 21.908860999999999 | 21.708056999999998 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_2048_transa_1_transb_0_time:7 | 21.665758 | 21.870656 | 21.762912333333334 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:0 | 11.31349 | 11.584056 | 11.408157833333334 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:1 | 11.415366 | 11.706813 | 11.534488833333335 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:2 | 11.263833 | 11.73098 | 11.597118833333335 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:3 | 11.261912 | 11.829353 | 11.525427333333335 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:4 | 11.2311 | 11.554175 | 11.387495000000002 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:5 | 11.454752 | 11.59912 | 11.541473000000002 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:6 | 11.277812 | 11.791364 | 11.534079499999999 |  |
| cublas-function/name_cublascgemm_k_32_m_512_n_512_transa_1_transb_0_time:7 | 11.213217 | 11.583194 | 11.423028333333335 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:0 | 18.404925 | 18.552880000000003 | 18.476811333333335 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:1 | 18.415159 | 18.686775 | 18.556088 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:2 | 18.361397 | 18.629381 | 18.504256333333335 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:3 | 18.458244 | 18.599942 | 18.541639 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:4 | 18.440004000000003 | 18.622024 | 18.519766 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:5 | 18.366279 | 18.574178 | 18.481699166666667 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:6 | 18.404922 | 18.622268 | 18.494112333333335 |  |
| cublas-function/name_cublascgemm_k_32_m_640_n_1280_transa_1_transb_0_time:7 | 18.319056 | 18.630706 | 18.495031833333333 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:0 | 32.287564 | 32.37721 | 32.346927666666669 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:1 | 32.286483 | 32.55363 | 32.384691 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:2 | 32.294615 | 32.438735 | 32.354474333333339 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:3 | 32.287752 | 32.431186 | 32.35603666666666 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:4 | 32.253436 | 32.474807 | 32.342323666666668 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:5 | 32.390008 | 32.465861000000007 | 32.42295483333333 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:6 | 32.291243 | 32.496082 | 32.381914333333337 |  |
| cublas-function/name_cublascgemm_k_32_m_896_n_1792_transa_1_transb_0_time:7 | 32.310112000000007 | 32.446632 | 32.37400183333333 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:0 | 18.637913 | 18.910739 | 18.805563166666667 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:1 | 18.619695 | 18.935698 | 18.793263166666667 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:2 | 18.613434 | 18.956438 | 18.810571166666667 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:3 | 18.683558 | 18.951383 | 18.8162095 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:4 | 18.65713 | 18.865424 | 18.759739 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:5 | 18.72006 | 18.856329 | 18.789843833333334 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:6 | 18.653402 | 18.8414 | 18.763620166666667 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_1000_m_4000_n_224_transa_0_transb_0_use_tensor_core_false_time:7 | 18.690218 | 18.976937 | 18.8289865 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:0 | 22.791665000000003 | 23.218913 | 22.994049333333334 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:1 | 22.954568 | 23.356861 | 23.139998333333336 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:2 | 22.904727 | 23.307107 | 23.101424166666665 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:3 | 22.902531 | 23.217849 | 23.076562166666663 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:4 | 23.050038 | 23.371218 | 23.21418266666667 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:5 | 23.125747 | 23.353984 | 23.207172666666666 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:6 | 22.693879 | 23.138559 | 23.021961333333335 |  |
| cublas-function/name_cublasgemmex_datatype_float_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:7 | 22.908893 | 23.164812 | 23.0713145 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:0 | 124.99698 | 132.95016 | 127.94383 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:1 | 124.73411 | 126.40263 | 125.30798333333333 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:2 | 125.03524 | 128.84956 | 126.149285 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:3 | 124.80798 | 125.60825 | 125.26699166666667 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:4 | 124.71254 | 127.7872 | 125.82094333333333 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:5 | 124.83216 | 125.98274 | 125.23543166666666 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:6 | 125.12422 | 133.45931 | 127.68385666666666 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_1000_m_4000_n_224_transa_1_transb_0_use_tensor_core_true_time:7 | 124.96816 | 126.26232 | 125.33655499999999 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:0 | 126.73955 | 138.14419 | 133.77800166666669 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:1 | 126.2757 | 137.97529 | 129.996325 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:2 | 126.74078 | 137.5417 | 130.47226666666669 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:3 | 125.81039 | 135.2722 | 127.76204999999999 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:4 | 126.69252 | 137.93747 | 132.11799833333334 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:5 | 126.71193 | 138.18089 | 135.10967833333334 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:6 | 126.19409 | 137.16478 | 129.35203666666667 |  |
| cublas-function/name_cublasgemmex_datatype_half_k_4000_m_1000_n_224_transa_0_transb_0_use_tensor_core_false_time:7 | 126.08915999999999 | 135.27791 | 127.80049333333334 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:0 | 141.48361 | 141.71545 | 141.60326 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:1 | 141.30208 | 141.91832 | 141.59414833333333 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:2 | 141.09747 | 141.8533 | 141.49698999999999 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:3 | 141.49727 | 142.00213 | 141.78461666666667 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:4 | 141.31918 | 141.72713 | 141.53211166666669 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:5 | 141.26917 | 141.66486 | 141.45829833333336 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:6 | 141.25491 | 141.85207 | 141.646635 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_224_m_64_n_224_transa_0_transb_0_use_tensor_core_true_time:7 | 141.53578 | 141.84118 | 141.64705833333336 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:0 | 99.36504 | 99.699793 | 99.51787233333333 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:1 | 99.548855 | 99.838974 | 99.69164083333334 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:2 | 99.616558 | 99.843837 | 99.69539466666667 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:3 | 99.643637 | 99.854169 | 99.74750583333334 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:4 | 99.526721 | 99.838342 | 99.716492 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:5 | 99.587967 | 99.81206 | 99.72897166666667 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:6 | 99.566474 | 99.873476 | 99.69902733333334 |  |
| cublas-function/name_cublasgemmstridedbatchedex_batchcount_160_datatype_half_k_64_m_224_n_224_transa_0_transb_0_use_tensor_core_true_time:7 | 99.596752 | 99.817928 | 99.71682016666667 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:0 | 888.09485 | 963.13637 | 916.1654416666667 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:1 | 880.5754 | 902.21527 | 888.61487 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:2 | 880.49301 | 935.46902 | 900.415465 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:3 | 881.87991 | 900.87385 | 890.3336283333333 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:4 | 889.4206800000001 | 910.28188 | 898.9274599999999 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:5 | 879.49043 | 896.70566 | 886.7843616666665 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:6 | 893.81145 | 960.55522 | 913.502915 |  |
| cublas-function/name_cublassgemm_k_1024_m_1024_n_7168_transa_1_transb_0_time:7 | 883.81064 | 905.73327 | 890.1238649999999 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:0 | 141.50558 | 141.80538 | 141.60270666666669 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:1 | 141.23597 | 141.83432 | 141.5282983333333 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:2 | 141.23346 | 141.77578 | 141.54340833333334 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:3 | 141.50979999999999 | 142.04881 | 141.793665 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:4 | 141.25271 | 141.78397999999999 | 141.53754333333334 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:5 | 141.17042 | 141.86512 | 141.49822666666666 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:6 | 141.53064 | 141.97602 | 141.72193833333334 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_160_k_224_m_64_n_224_transa_0_transb_0_time:7 | 141.47393 | 142.00744 | 141.67315 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:0 | 436.7874 | 437.87871 | 437.1637533333333 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:1 | 436.7638 | 437.76444 | 437.0509016666667 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:2 | 436.74471 | 437.85488 | 437.1689666666666 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:3 | 437.24612 | 437.94029 | 437.53083666666665 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:4 | 436.83899 | 437.70129 | 437.3024833333334 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:5 | 436.61882 | 437.85636999999999 | 437.2927966666667 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:6 | 437.0036 | 439.0005 | 437.50746333333339 |  |
| cublas-function/name_cublassgemmstridedbatched_batchcount_512_k_224_m_64_n_224_transa_0_transb_0_time:7 | 436.88984 | 437.42201 | 437.0986249999999 |  |
| cublas-function/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| cublas-function/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:0 | 98.897146 | 100.114456 | 99.27759283333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:1 | 98.838041 | 100.3459 | 99.36282933333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:2 | 98.956891 | 99.945243 | 99.301897 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:3 | 98.803667 | 100.2547 | 99.29956416666666 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:4 | 98.72370599999999 | 100.096721 | 99.32875133333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:5 | 99.046737 | 100.39707 | 99.62480083333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:6 | 98.833352 | 99.57642799999999 | 99.11962533333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_0_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_false_time:7 | 98.77168 | 99.484374 | 99.17583116666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:0 | 99.003094 | 100.38895000000001 | 99.342777 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:1 | 98.69342999999999 | 100.032753 | 99.24435166666668 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:2 | 98.953785 | 99.924848 | 99.36696866666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:3 | 98.846621 | 99.902382 | 99.27426083333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:4 | 98.72703200000001 | 100.133529 | 99.27913083333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:5 | 98.956783 | 100.22386 | 99.61754283333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:6 | 98.94099200000001 | 99.548024 | 99.11557933333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_1024_14_14]_inputstride_[200704_196_14_1]_inputtype_2_mode_1_outputdims_[32_256_14_14]_outputstride_[50176_196_14_1]_pada_[0_0]_tensorop_true_time:7 | 98.90080800000001 | 99.114966 | 98.99175983333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:0 | 23.974922 | 25.35406 | 24.903619833333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:1 | 23.929631 | 25.546467 | 25.027396166666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:2 | 24.027950999999999 | 25.582433 | 24.7997855 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:3 | 23.928234 | 25.244329999999999 | 24.64035366666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:4 | 23.990976 | 25.304676 | 24.932436666666665 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:5 | 24.117783 | 25.132258 | 24.784836666666668 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:6 | 23.987695 | 25.293297 | 24.59107933333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_2_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_true_time:7 | 24.409718 | 25.973883 | 25.065641833333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:0 | 286.69199000000006 | 312.06901 | 295.61654666666666 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:1 | 283.41238000000006 | 291.6127 | 286.96156666666669 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:2 | 285.3074 | 298.35569 | 289.795405 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:3 | 283.04053 | 289.5436 | 286.576165 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:4 | 286.68294 | 291.40693 | 289.04923333333337 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:5 | 283.35194 | 289.2161 | 285.81301333333337 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:6 | 288.24766 | 301.41312 | 292.877235 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:7 | 284.81372999999999 | 291.63749 | 287.01415000000005 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:0 | 286.11574 | 304.05358 | 294.013795 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:1 | 283.39573 | 290.84387 | 286.46210666666669 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:2 | 285.13589 | 298.68152999999998 | 289.62677833333336 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:3 | 283.60374 | 289.26202 | 286.7411866666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:4 | 286.99417 | 291.92935 | 289.31070833333339 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:5 | 283.11174 | 289.13318 | 285.70940833333335 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:6 | 288.61318 | 303.50888999999997 | 292.95292666666668 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:7 | 285.4895 | 290.40579 | 286.893795 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:0 | 32.45738 | 32.819863999999999 | 32.649939999999997 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:1 | 32.40842 | 32.669168 | 32.57155383333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:2 | 32.337013999999999 | 32.866405 | 32.58065200000001 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:3 | 32.371778000000009 | 32.734802 | 32.60792766666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:4 | 32.460246999999998 | 32.55516 | 32.49840183333333 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:5 | 32.432454 | 32.908826000000008 | 32.639814666666669 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:6 | 32.451783 | 32.737081 | 32.62559866666667 |  |
| cudnn-function/name_cudnnconvolutionbackwarddata_algo_4_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_32_14_14]_inputstride_[6272_196_14_1]_inputtype_0_mode_1_outputdims_[32_128_14_14]_outputstride_[25088_196_14_1]_pada_[1_1]_tensorop_false_time:7 | 32.469427 | 32.869460000000007 | 32.603407 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:0 | 52.452543999999999 | 53.839962 | 52.94054366666666 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:1 | 52.353142999999999 | 53.881571 | 52.99805116666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:2 | 52.711371 | 53.728517 | 53.06997116666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:3 | 52.718267 | 53.619864 | 53.08352183333333 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:4 | 52.389635 | 54.238952 | 53.11242166666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:5 | 52.561091999999998 | 53.623248 | 53.20796516666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:6 | 52.657416999999998 | 53.322569 | 52.985032 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_0_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:7 | 52.578261 | 53.192139 | 52.88673400000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:0 | 103.28532999999999 | 104.19723 | 103.72608500000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:1 | 103.14918 | 103.81811 | 103.58045333333333 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:2 | 103.10739000000001 | 104.08642 | 103.44405166666668 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:3 | 103.27082 | 104.27927 | 103.87550833333335 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:4 | 102.82226 | 104.38625 | 103.53797666666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:5 | 102.87612999999999 | 104.41082999999999 | 103.69927499999999 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:6 | 103.17764 | 104.84138 | 103.79870166666668 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:7 | 103.48083 | 104.09469 | 103.80397 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:0 | 102.95828999999999 | 104.76194 | 103.76726000000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:1 | 103.04781 | 103.88047999999999 | 103.43968500000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:2 | 103.20629000000001 | 104.15634999999999 | 103.59112999999998 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:3 | 103.16923 | 104.07918 | 103.59432500000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:4 | 102.9342 | 104.27656 | 103.53621333333335 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:5 | 102.98955 | 104.2204 | 103.73826000000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:6 | 103.08042 | 104.40653 | 103.76822499999999 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:7 | 103.37392 | 104.49996999999999 | 103.95866000000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:0 | 84.871094 | 85.778736 | 85.32086149999999 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:1 | 84.984388 | 85.98766400000001 | 85.4923085 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:2 | 85.181583 | 85.709222 | 85.53070050000001 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:3 | 84.931145 | 85.564625 | 85.2146445 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:4 | 84.987531 | 86.121865 | 85.41115483333334 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:5 | 84.964197 | 85.38174099999999 | 85.173823 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:6 | 85.04326800000001 | 85.552533 | 85.29111 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:7 | 84.878218 | 85.51087 | 85.24390783333333 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:0 | 374.00893 | 398.4525 | 384.599355 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:1 | 372.56185 | 381.89329 | 375.9916616666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:2 | 372.36305000000007 | 389.38354 | 377.647195 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:3 | 372.07078 | 380.38815 | 375.6002633333333 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:4 | 374.78021 | 381.29504 | 378.03609166666669 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:5 | 369.60239 | 379.40087 | 374.42107166666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:6 | 376.28265999999999 | 397.48848999999998 | 382.381235 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:7 | 373.19208999999997 | 382.53282 | 375.82836 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:0 | 374.60325 | 395.07293 | 384.35002333333338 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:1 | 371.87940000000006 | 381.26268000000007 | 375.766645 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:2 | 372.16082 | 391.08086 | 378.288295 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:3 | 372.20415 | 379.63619 | 375.5568083333333 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:4 | 376.23749 | 381.93471 | 378.45376333333328 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:5 | 370.20596 | 379.10451 | 374.51100333333337 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:6 | 376.7349 | 394.53245999999998 | 382.3466716666667 |  |
| cudnn-function/name_cudnnconvolutionbackwardfilter_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:7 | 373.10197 | 382.1483 | 375.52106499999999 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:0 | 79.29505 | 79.84352899999999 | 79.55798583333334 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:1 | 79.370076 | 80.221751 | 79.662027 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:2 | 79.3102 | 80.218946 | 79.58912149999999 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:3 | 79.47088400000001 | 79.671751 | 79.59436166666666 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:4 | 79.37647100000001 | 80.096457 | 79.70541733333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:5 | 79.523392 | 80.04405 | 79.73800916666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:6 | 79.280242 | 79.9134 | 79.59506833333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_0_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_false_time:7 | 79.404592 | 79.735818 | 79.5726745 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:0 | 79.28429 | 79.840398 | 79.54307983333334 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:1 | 79.249229 | 80.16886600000001 | 79.58339983333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:2 | 79.469235 | 79.841756 | 79.67081383333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:3 | 79.438266 | 79.845512 | 79.64765566666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:4 | 79.460453 | 80.210367 | 79.80648833333334 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:5 | 79.515314 | 80.030203 | 79.70997566666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:6 | 79.49830700000001 | 79.781351 | 79.62396299999999 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[1024_256_1_1]_filterstridea_[1_1]_inputdims_[32_256_14_14]_inputstride_[50176_196_14_1]_inputtype_2_mode_1_outputdims_[32_1024_14_14]_outputstride_[200704_196_14_1]_pada_[0_0]_tensorop_true_time:7 | 79.343103 | 79.811059 | 79.57136183333334 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:0 | 55.626352 | 56.3731 | 55.959444166666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:1 | 55.854752 | 56.628259 | 56.132445499999999 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:2 | 55.647239 | 56.11678 | 55.9364175 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:3 | 55.938923 | 56.644502 | 56.3239235 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:4 | 55.628509 | 56.617417 | 56.253504166666669 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:5 | 55.832194 | 56.821176 | 56.350377 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:6 | 55.857155 | 56.317863 | 56.064628666666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_2_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_true_time:7 | 55.897338999999998 | 56.271935 | 56.082216499999997 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:0 | 280.36133 | 298.96247 | 287.9489483333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:1 | 278.51468 | 286.41778 | 281.32612166666669 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:2 | 279.12944 | 290.73091 | 282.86751 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:3 | 277.47723 | 283.09668 | 280.46655000000006 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:4 | 281.96747 | 286.52072 | 283.48278999999999 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:5 | 277.71227 | 284.26514 | 280.1258583333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:6 | 282.28105 | 293.95574000000007 | 286.138955 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_0_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_false_time:7 | 279.21643 | 284.71714 | 280.78393 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:0 | 280.48402 | 297.73650000000006 | 287.61147666666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:1 | 278.51189999999999 | 285.75304 | 281.149745 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:2 | 279.38165999999998 | 292.71825 | 283.43276499999998 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:3 | 277.61681 | 284.11545 | 280.7962883333334 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:4 | 281.51338 | 286.18853 | 283.3503216666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:5 | 277.76589 | 283.66995000000005 | 280.09838333333337 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:6 | 282.28303999999999 | 297.7684 | 286.6985966666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_1_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[512_512_3_3]_filterstridea_[1_1]_inputdims_[32_512_14_14]_inputstride_[100352_196_14_1]_inputtype_2_mode_1_outputdims_[32_512_14_14]_outputstride_[100352_196_14_1]_pada_[1_1]_tensorop_true_time:7 | 279.12564000000006 | 284.71834 | 280.7541166666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:0 | 36.66272 | 37.212277 | 36.934237833333337 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:1 | 36.6804 | 37.449494 | 37.016225666666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:2 | 36.712478 | 37.12077 | 36.92553683333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:3 | 36.780747000000008 | 37.202518 | 36.9789695 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:4 | 36.684283 | 37.415130999999998 | 37.03319833333334 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:5 | 36.89835 | 37.344460000000008 | 37.13333266666667 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:6 | 36.89328 | 37.212461000000008 | 37.09213483333333 |  |
| cudnn-function/name_cudnnconvolutionforward_algo_6_arraylength_2_convtype_0_dilationa_[1_1]_filterdims_[32_128_3_3]_filterstridea_[1_1]_inputdims_[32_128_14_14]_inputstride_[25088_196_14_1]_inputtype_0_mode_1_outputdims_[32_32_14_14]_outputstride_[6272_196_14_1]_pada_[1_1]_tensorop_false_time:7 | 36.676832 | 37.200521 | 36.95109333333334 |  |
| cudnn-function/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| cudnn-function/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/fp16_train_step_time | 133.83819329738618 | 555.9085130691528 | 401.83629462855506 |  |
| densenet_models/pytorch-densenet169/fp16_train_throughput | 57.56541168063091 | 239.2000081906667 | 119.74691628345579 |  |
| densenet_models/pytorch-densenet169/fp32_train_step_time | 142.33385932445527 | 616.386833190918 | 445.73356583751896 |  |
| densenet_models/pytorch-densenet169/fp32_train_throughput | 51.91698154662019 | 224.91214886651253 | 111.00711266121495 |  |
| densenet_models/pytorch-densenet169/return_code | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet169/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/fp16_train_step_time | 163.62678456306458 | 846.3730449676514 | 523.7491532048234 |  |
| densenet_models/pytorch-densenet201/fp16_train_throughput | 42.43202766185742 | 195.6595295187772 | 95.54036577037239 |  |
| densenet_models/pytorch-densenet201/fp32_train_step_time | 173.68286645412446 | 781.9243693351746 | 562.8373178437228 |  |
| densenet_models/pytorch-densenet201/fp32_train_throughput | 40.925710830055518 | 184.3174891999258 | 89.89816653034312 |  |
| densenet_models/pytorch-densenet201/return_code | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| densenet_models/pytorch-densenet201/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/bf16_tc_flops:0 | 242090.0 | 265192.0 | 256069.16666666667 |  |
| gemm-flops/bf16_tc_flops:1 | 260007.0 | 267449.0 | 264553.8333333333 |  |
| gemm-flops/bf16_tc_flops:2 | 255183.0 | 267143.0 | 262783.5 |  |
| gemm-flops/bf16_tc_flops:3 | 261419.0 | 267710.0 | 264319.8333333333 |  |
| gemm-flops/bf16_tc_flops:4 | 259160.0 | 264452.0 | 261865.16666666667 |  |
| gemm-flops/bf16_tc_flops:5 | 261768.0 | 269533.0 | 265895.1666666667 |  |
| gemm-flops/bf16_tc_flops:6 | 245322.0 | 263075.0 | 257522.83333333335 |  |
| gemm-flops/bf16_tc_flops:7 | 259195.0 | 266390.0 | 264186.8333333333 |  |
| gemm-flops/fp16_flops:0 | 33780.4 | 33914.0 | 33877.166666666664 |  |
| gemm-flops/fp16_flops:1 | 33850.9 | 33911.6 | 33888.46666666667 |  |
| gemm-flops/fp16_flops:2 | 33700.9 | 33908.8 | 33837.78333333333 |  |
| gemm-flops/fp16_flops:3 | 33754.2 | 33910.0 | 33812.566666666666 |  |
| gemm-flops/fp16_flops:4 | 33765.5 | 33914.1 | 33843.066666666666 |  |
| gemm-flops/fp16_flops:5 | 33822.1 | 33899.4 | 33866.0 |  |
| gemm-flops/fp16_flops:6 | 33637.4 | 33910.7 | 33855.51666666667 |  |
| gemm-flops/fp16_flops:7 | 33791.5 | 33912.6 | 33874.99999999999 |  |
| gemm-flops/fp16_tc_flops:0 | 258756.0 | 280001.0 | 270489.5 |  |
| gemm-flops/fp16_tc_flops:1 | 274408.0 | 281567.0 | 278977.3333333333 |  |
| gemm-flops/fp16_tc_flops:2 | 271888.0 | 282078.0 | 277691.6666666667 |  |
| gemm-flops/fp16_tc_flops:3 | 274942.0 | 282733.0 | 278512.6666666667 |  |
| gemm-flops/fp16_tc_flops:4 | 272963.0 | 278453.0 | 276253.5 |  |
| gemm-flops/fp16_tc_flops:5 | 275741.0 | 283778.0 | 280191.6666666667 |  |
| gemm-flops/fp16_tc_flops:6 | 262348.0 | 277369.0 | 272209.6666666667 |  |
| gemm-flops/fp16_tc_flops:7 | 273979.0 | 281014.0 | 278726.0 |  |
| gemm-flops/fp32_flops:0 | 18271.7 | 18375.9 | 18347.100000000002 |  |
| gemm-flops/fp32_flops:1 | 18363.6 | 18376.0 | 18370.516666666666 |  |
| gemm-flops/fp32_flops:2 | 18281.8 | 18374.8 | 18344.483333333334 |  |
| gemm-flops/fp32_flops:3 | 18305.8 | 18375.0 | 18348.733333333334 |  |
| gemm-flops/fp32_flops:4 | 18311.1 | 18376.3 | 18355.433333333334 |  |
| gemm-flops/fp32_flops:5 | 18284.9 | 18373.0 | 18338.46666666667 |  |
| gemm-flops/fp32_flops:6 | 18294.7 | 18375.8 | 18356.783333333333 |  |
| gemm-flops/fp32_flops:7 | 18325.6 | 18376.1 | 18361.383333333335 |  |
| gemm-flops/fp64_flops:0 | 9000.74 | 9041.29 | 9031.103333333334 |  |
| gemm-flops/fp64_flops:1 | 9035.28 | 9041.53 | 9038.538333333334 |  |
| gemm-flops/fp64_flops:2 | 8999.79 | 9040.93 | 9029.358333333334 |  |
| gemm-flops/fp64_flops:3 | 9021.47 | 9041.44 | 9031.388333333334 |  |
| gemm-flops/fp64_flops:4 | 9030.24 | 9042.03 | 9035.491666666667 |  |
| gemm-flops/fp64_flops:5 | 9022.6 | 9038.52 | 9030.898333333334 |  |
| gemm-flops/fp64_flops:6 | 9031.83 | 9041.49 | 9037.368333333334 |  |
| gemm-flops/fp64_flops:7 | 9017.3 | 9041.77 | 9033.703333333333 |  |
| gemm-flops/fp64_tc_flops:0 | 18830.8 | 18971.6 | 18937.75 |  |
| gemm-flops/fp64_tc_flops:1 | 18949.4 | 18971.8 | 18963.649999999998 |  |
| gemm-flops/fp64_tc_flops:2 | 18856.3 | 18971.0 | 18925.899999999998 |  |
| gemm-flops/fp64_tc_flops:3 | 18904.1 | 18968.6 | 18947.95 |  |
| gemm-flops/fp64_tc_flops:4 | 18919.5 | 18970.8 | 18955.88333333333 |  |
| gemm-flops/fp64_tc_flops:5 | 18880.0 | 18967.8 | 18937.350000000002 |  |
| gemm-flops/fp64_tc_flops:6 | 18900.3 | 18971.7 | 18955.683333333334 |  |
| gemm-flops/fp64_tc_flops:7 | 18913.2 | 18972.0 | 18953.100000000002 |  |
| gemm-flops/int4_tc_iops:0 | 917478.0 | 975296.0 | 948284.1666666666 |  |
| gemm-flops/int4_tc_iops:1 | 952562.0 | 982519.0 | 970722.5 |  |
| gemm-flops/int4_tc_iops:2 | 951884.0 | 982171.0 | 968316.1666666666 |  |
| gemm-flops/int4_tc_iops:3 | 958130.0 | 982210.0 | 968647.8333333334 |  |
| gemm-flops/int4_tc_iops:4 | 948529.0 | 970650.0 | 960465.3333333334 |  |
| gemm-flops/int4_tc_iops:5 | 957267.0 | 986978.0 | 975829.6666666666 |  |
| gemm-flops/int4_tc_iops:6 | 937663.0 | 967214.0 | 955392.6666666666 |  |
| gemm-flops/int4_tc_iops:7 | 953370.0 | 976948.0 | 969971.1666666666 |  |
| gemm-flops/int8_tc_iops:0 | 440347.0 | 476690.0 | 461061.1666666667 |  |
| gemm-flops/int8_tc_iops:1 | 464604.0 | 477222.0 | 473194.0 |  |
| gemm-flops/int8_tc_iops:2 | 462628.0 | 479096.0 | 472533.5 |  |
| gemm-flops/int8_tc_iops:3 | 468554.0 | 481374.0 | 473731.3333333333 |  |
| gemm-flops/int8_tc_iops:4 | 463702.0 | 474460.0 | 469655.8333333333 |  |
| gemm-flops/int8_tc_iops:5 | 470203.0 | 485159.0 | 477810.1666666667 |  |
| gemm-flops/int8_tc_iops:6 | 448039.0 | 470634.0 | 463783.0 |  |
| gemm-flops/int8_tc_iops:7 | 464763.0 | 477026.0 | 473455.0 |  |
| gemm-flops/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| gemm-flops/tf32_tc_flops:0 | 117875.0 | 129009.0 | 124596.0 |  |
| gemm-flops/tf32_tc_flops:1 | 126540.0 | 129292.0 | 128384.33333333333 |  |
| gemm-flops/tf32_tc_flops:2 | 123638.0 | 129271.0 | 127028.83333333333 |  |
| gemm-flops/tf32_tc_flops:3 | 126473.0 | 129312.0 | 127978.83333333333 |  |
| gemm-flops/tf32_tc_flops:4 | 125978.0 | 127695.0 | 127139.66666666667 |  |
| gemm-flops/tf32_tc_flops:5 | 127370.0 | 129016.0 | 128318.0 |  |
| gemm-flops/tf32_tc_flops:6 | 118876.0 | 127906.0 | 125119.5 |  |
| gemm-flops/tf32_tc_flops:7 | 126069.0 | 129244.0 | 128412.83333333333 |  |
| gpt_models/pytorch-gpt2-large/fp16_train_step_time | 186.64892454445363 | 189.82946683466435 | 188.38848959406219 |  |
| gpt_models/pytorch-gpt2-large/fp16_train_throughput | 42.1501212372239 | 42.87368758667614 | 42.47518986495928 |  |
| gpt_models/pytorch-gpt2-large/fp32_train_step_time | 291.80782610177996 | 294.7833593636751 | 292.9316874866684 |  |
| gpt_models/pytorch-gpt2-large/fp32_train_throughput | 27.142057173553395 | 27.416544650424727 | 27.31380672199087 |  |
| gpt_models/pytorch-gpt2-large/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-large/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/fp16_train_step_time | 37.98911726474762 | 326.8325899839401 | 145.26514018069813 |  |
| gpt_models/pytorch-gpt2-small/fp16_train_throughput | 4.231499995142115 | 162.6779415010631 | 15.460237609909465 |  |
| gpt_models/pytorch-gpt2-small/fp32_train_step_time | 39.54342374205589 | 360.50366258621218 | 190.8125524758108 |  |
| gpt_models/pytorch-gpt2-small/fp32_train_throughput | 3.134325568246455 | 137.4913566297608 | 13.828107705251595 |  |
| gpt_models/pytorch-gpt2-small/return_code | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| gpt_models/pytorch-gpt2-small/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/ib_write_1024_ib0_bw | 3.2496 | 3.36757 | 3.32694 |  |
| ib-loopback/ib_write_1024_ib1_bw | 3.24177 | 3.3243 | 3.2820966666666666 |  |
| ib-loopback/ib_write_1024_ib2_bw | 3.2365 | 3.46469 | 3.35385 |  |
| ib-loopback/ib_write_1024_ib3_bw | 3.1809600000000004 | 3.55202 | 3.40521 |  |
| ib-loopback/ib_write_1024_ib4_bw | 3.24991 | 3.39896 | 3.317201666666667 |  |
| ib-loopback/ib_write_1024_ib5_bw | 3.22818 | 3.38261 | 3.302766666666667 |  |
| ib-loopback/ib_write_1024_ib6_bw | 3.2063200000000005 | 3.3286700000000004 | 3.2691033333333339 |  |
| ib-loopback/ib_write_1024_ib7_bw | 3.28306 | 3.38469 | 3.345 |  |
| ib-loopback/ib_write_1048576_ib0_bw | 22.02687 | 22.66894 | 22.36603 |  |
| ib-loopback/ib_write_1048576_ib1_bw | 21.7862 | 22.69314 | 22.378761666666667 |  |
| ib-loopback/ib_write_1048576_ib2_bw | 22.46357 | 22.89966 | 22.633371666666667 |  |
| ib-loopback/ib_write_1048576_ib3_bw | 22.16245 | 22.93122 | 22.578599999999999 |  |
| ib-loopback/ib_write_1048576_ib4_bw | 21.68359 | 22.76298 | 22.4438 |  |
| ib-loopback/ib_write_1048576_ib5_bw | 11.434280000000001 | 22.647779999999999 | 20.670315 |  |
| ib-loopback/ib_write_1048576_ib6_bw | 21.82555 | 22.5485 | 22.29513 |  |
| ib-loopback/ib_write_1048576_ib7_bw | 21.7003 | 22.78455 | 22.228386666666667 |  |
| ib-loopback/ib_write_131072_ib0_bw | 21.94048 | 22.67718 | 22.244371666666667 |  |
| ib-loopback/ib_write_131072_ib1_bw | 21.76041 | 22.470470000000004 | 22.21943166666667 |  |
| ib-loopback/ib_write_131072_ib2_bw | 22.15183 | 22.86443 | 22.451203333333337 |  |
| ib-loopback/ib_write_131072_ib3_bw | 21.46566 | 22.56848 | 22.182236666666669 |  |
| ib-loopback/ib_write_131072_ib4_bw | 21.30293 | 22.43324 | 21.94987 |  |
| ib-loopback/ib_write_131072_ib5_bw | 10.92907 | 22.35794 | 20.266730000000004 |  |
| ib-loopback/ib_write_131072_ib6_bw | 22.01257 | 23.41291 | 22.674783333333335 |  |
| ib-loopback/ib_write_131072_ib7_bw | 21.65534 | 22.911759999999999 | 22.47101833333333 |  |
| ib-loopback/ib_write_16384_ib0_bw | 20.181720000000003 | 20.92718 | 20.558628333333336 |  |
| ib-loopback/ib_write_16384_ib1_bw | 19.85934 | 20.94494 | 20.48742 |  |
| ib-loopback/ib_write_16384_ib2_bw | 19.893349999999999 | 21.51061 | 20.632126666666669 |  |
| ib-loopback/ib_write_16384_ib3_bw | 19.81447 | 20.7282 | 20.298818333333334 |  |
| ib-loopback/ib_write_16384_ib4_bw | 19.355580000000005 | 20.40191 | 20.00963 |  |
| ib-loopback/ib_write_16384_ib5_bw | 9.97869 | 20.90518 | 18.235578333333334 |  |
| ib-loopback/ib_write_16384_ib6_bw | 20.01135 | 21.2152 | 20.724634999999997 |  |
| ib-loopback/ib_write_16384_ib7_bw | 20.12157 | 21.22544 | 20.627118333333333 |  |
| ib-loopback/ib_write_2048_ib0_bw | 6.55954 | 7.024100000000001 | 6.716795 |  |
| ib-loopback/ib_write_2048_ib1_bw | 6.46408 | 6.99755 | 6.7765249999999999 |  |
| ib-loopback/ib_write_2048_ib2_bw | 6.62838 | 7.29432 | 7.067228333333333 |  |
| ib-loopback/ib_write_2048_ib3_bw | 6.66409 | 7.3988000000000009 | 6.974786666666667 |  |
| ib-loopback/ib_write_2048_ib4_bw | 6.54743 | 7.2020100000000009 | 6.906501666666666 |  |
| ib-loopback/ib_write_2048_ib5_bw | 6.69362 | 7.405279999999999 | 7.111085 |  |
| ib-loopback/ib_write_2048_ib6_bw | 6.6898800000000009 | 7.0095600000000009 | 6.789285 |  |
| ib-loopback/ib_write_2048_ib7_bw | 6.6944799999999999 | 7.04008 | 6.7875499999999999 |  |
| ib-loopback/ib_write_2097152_ib0_bw | 22.04063 | 22.5773 | 22.315158333333334 |  |
| ib-loopback/ib_write_2097152_ib1_bw | 22.015900000000003 | 22.62534 | 22.372568333333335 |  |
| ib-loopback/ib_write_2097152_ib2_bw | 22.30644 | 22.99858 | 22.698603333333336 |  |
| ib-loopback/ib_write_2097152_ib3_bw | 22.446759999999999 | 23.05331 | 22.742793333333336 |  |
| ib-loopback/ib_write_2097152_ib4_bw | 21.919169999999999 | 22.90221 | 22.565573333333334 |  |
| ib-loopback/ib_write_2097152_ib5_bw | 14.444139999999999 | 22.67472 | 21.17702166666667 |  |
| ib-loopback/ib_write_2097152_ib6_bw | 22.21171 | 22.86732 | 22.556905 |  |
| ib-loopback/ib_write_2097152_ib7_bw | 21.98594 | 22.91925 | 22.615098333333333 |  |
| ib-loopback/ib_write_262144_ib0_bw | 22.033990000000004 | 22.63996 | 22.26207666666667 |  |
| ib-loopback/ib_write_262144_ib1_bw | 22.15261 | 22.60949 | 22.366215 |  |
| ib-loopback/ib_write_262144_ib2_bw | 22.16255 | 22.9695 | 22.579108333333335 |  |
| ib-loopback/ib_write_262144_ib3_bw | 22.254849999999999 | 22.94538 | 22.48115833333333 |  |
| ib-loopback/ib_write_262144_ib4_bw | 21.372419999999999 | 22.36609 | 21.98141 |  |
| ib-loopback/ib_write_262144_ib5_bw | 11.02666 | 22.47476 | 20.19280666666667 |  |
| ib-loopback/ib_write_262144_ib6_bw | 22.34868 | 23.01494 | 22.655771666666668 |  |
| ib-loopback/ib_write_262144_ib7_bw | 22.33691 | 22.94313 | 22.63854166666667 |  |
| ib-loopback/ib_write_32768_ib0_bw | 21.6895 | 22.069740000000004 | 21.838328333333334 |  |
| ib-loopback/ib_write_32768_ib1_bw | 21.489900000000003 | 22.336740000000004 | 21.993068333333338 |  |
| ib-loopback/ib_write_32768_ib2_bw | 21.56048 | 22.39191 | 21.93259333333333 |  |
| ib-loopback/ib_write_32768_ib3_bw | 20.26293 | 22.40778 | 21.693888333333335 |  |
| ib-loopback/ib_write_32768_ib4_bw | 20.90112 | 22.24521 | 21.502766666666664 |  |
| ib-loopback/ib_write_32768_ib5_bw | 9.94131 | 22.05309 | 19.26934 |  |
| ib-loopback/ib_write_32768_ib6_bw | 21.4207 | 22.65634 | 22.123700000000004 |  |
| ib-loopback/ib_write_32768_ib7_bw | 21.77601 | 22.43813 | 22.05366666666667 |  |
| ib-loopback/ib_write_4096_ib0_bw | 12.37614 | 14.051459999999999 | 13.418116666666668 |  |
| ib-loopback/ib_write_4096_ib1_bw | 13.134540000000002 | 14.05245 | 13.606868333333333 |  |
| ib-loopback/ib_write_4096_ib2_bw | 13.0462 | 14.21158 | 13.642919999999999 |  |
| ib-loopback/ib_write_4096_ib3_bw | 12.002469999999999 | 13.944540000000002 | 13.327836666666665 |  |
| ib-loopback/ib_write_4096_ib4_bw | 13.022450000000001 | 14.80086 | 13.829583333333332 |  |
| ib-loopback/ib_write_4096_ib5_bw | 8.05746 | 14.79875 | 13.146763333333333 |  |
| ib-loopback/ib_write_4096_ib6_bw | 13.12732 | 13.89508 | 13.60103 |  |
| ib-loopback/ib_write_4096_ib7_bw | 13.11149 | 14.10579 | 13.785596666666665 |  |
| ib-loopback/ib_write_4194304_ib0_bw | 21.99154 | 22.41034 | 22.197133333333338 |  |
| ib-loopback/ib_write_4194304_ib1_bw | 21.8405 | 22.52132 | 22.26180666666666 |  |
| ib-loopback/ib_write_4194304_ib2_bw | 21.97705 | 23.057479999999999 | 22.57337666666667 |  |
| ib-loopback/ib_write_4194304_ib3_bw | 22.26257 | 22.95 | 22.553028333333335 |  |
| ib-loopback/ib_write_4194304_ib4_bw | 22.24691 | 22.89706 | 22.590473333333333 |  |
| ib-loopback/ib_write_4194304_ib5_bw | 17.36779 | 22.64334 | 21.662255 |  |
| ib-loopback/ib_write_4194304_ib6_bw | 22.33194 | 23.093220000000004 | 22.755625 |  |
| ib-loopback/ib_write_4194304_ib7_bw | 22.24111 | 22.8661 | 22.630764999999998 |  |
| ib-loopback/ib_write_512_ib0_bw | 1.4906400000000002 | 1.5559 | 1.5189833333333336 |  |
| ib-loopback/ib_write_512_ib1_bw | 1.49558 | 1.53567 | 1.5134983333333335 |  |
| ib-loopback/ib_write_512_ib2_bw | 1.49572 | 1.64534 | 1.5921899999999998 |  |
| ib-loopback/ib_write_512_ib3_bw | 1.49786 | 1.62333 | 1.5493199999999999 |  |
| ib-loopback/ib_write_512_ib4_bw | 1.4900799999999999 | 1.56127 | 1.5145600000000002 |  |
| ib-loopback/ib_write_512_ib5_bw | 1.4764000000000002 | 1.56782 | 1.5150949999999999 |  |
| ib-loopback/ib_write_512_ib6_bw | 1.48122 | 1.54199 | 1.5145383333333334 |  |
| ib-loopback/ib_write_512_ib7_bw | 1.47797 | 1.53959 | 1.5133133333333333 |  |
| ib-loopback/ib_write_524288_ib0_bw | 22.172369999999999 | 22.558919999999998 | 22.328994999999997 |  |
| ib-loopback/ib_write_524288_ib1_bw | 22.01747 | 22.747349999999999 | 22.36332833333333 |  |
| ib-loopback/ib_write_524288_ib2_bw | 22.312540000000003 | 22.94389 | 22.603493333333334 |  |
| ib-loopback/ib_write_524288_ib3_bw | 22.32899 | 22.84158 | 22.558444999999997 |  |
| ib-loopback/ib_write_524288_ib4_bw | 21.163259999999999 | 22.47475 | 22.05937 |  |
| ib-loopback/ib_write_524288_ib5_bw | 10.49666 | 22.907799999999999 | 20.52802833333333 |  |
| ib-loopback/ib_write_524288_ib6_bw | 22.16985 | 22.68121 | 22.434208333333335 |  |
| ib-loopback/ib_write_524288_ib7_bw | 22.04099 | 22.883689999999999 | 22.453210000000003 |  |
| ib-loopback/ib_write_65536_ib0_bw | 22.00973 | 22.389770000000003 | 22.156201666666666 |  |
| ib-loopback/ib_write_65536_ib1_bw | 21.11184 | 22.443939999999999 | 22.043256666666669 |  |
| ib-loopback/ib_write_65536_ib2_bw | 21.05311 | 22.929740000000004 | 22.223546666666669 |  |
| ib-loopback/ib_write_65536_ib3_bw | 20.872709999999999 | 22.499209999999999 | 21.83387833333333 |  |
| ib-loopback/ib_write_65536_ib4_bw | 21.16403 | 22.207549999999999 | 21.677466666666665 |  |
| ib-loopback/ib_write_65536_ib5_bw | 10.45167 | 21.83536 | 19.679668333333333 |  |
| ib-loopback/ib_write_65536_ib6_bw | 22.18627 | 22.99207 | 22.58490333333333 |  |
| ib-loopback/ib_write_65536_ib7_bw | 21.91065 | 22.87806 | 22.421733333333333 |  |
| ib-loopback/ib_write_8192_ib0_bw | 17.584139999999999 | 19.4277 | 18.576328333333337 |  |
| ib-loopback/ib_write_8192_ib1_bw | 18.40617 | 19.40073 | 18.89776833333333 |  |
| ib-loopback/ib_write_8192_ib2_bw | 17.115209999999999 | 19.12852 | 18.401526666666667 |  |
| ib-loopback/ib_write_8192_ib3_bw | 18.5514 | 19.46734 | 18.95923 |  |
| ib-loopback/ib_write_8192_ib4_bw | 17.33 | 19.00201 | 18.241576666666668 |  |
| ib-loopback/ib_write_8192_ib5_bw | 9.83299 | 19.32186 | 17.04467 |  |
| ib-loopback/ib_write_8192_ib6_bw | 18.63909 | 19.416330000000003 | 19.044073333333335 |  |
| ib-loopback/ib_write_8192_ib7_bw | 18.71551 | 19.92865 | 19.15118 |  |
| ib-loopback/ib_write_8388608_ib0_bw | 22.0981 | 22.47284 | 22.298518333333335 |  |
| ib-loopback/ib_write_8388608_ib1_bw | 22.08354 | 22.53182 | 22.39095 |  |
| ib-loopback/ib_write_8388608_ib2_bw | 22.204169999999999 | 23.142259999999998 | 22.704415 |  |
| ib-loopback/ib_write_8388608_ib3_bw | 22.24648 | 22.987479999999999 | 22.60818 |  |
| ib-loopback/ib_write_8388608_ib4_bw | 20.87492 | 22.78991 | 22.030941666666668 |  |
| ib-loopback/ib_write_8388608_ib5_bw | 19.60497 | 22.582669999999998 | 21.671266666666669 |  |
| ib-loopback/ib_write_8388608_ib6_bw | 21.46823 | 23.107310000000003 | 22.52020166666667 |  |
| ib-loopback/ib_write_8388608_ib7_bw | 22.15186 | 22.78093 | 22.5986 |  |
| ib-loopback/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| ib-loopback/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/event_time:0 | 0.00558 | 0.00579 | 0.005661666666666666 |  |
| kernel-launch/event_time:1 | 0.00559 | 0.00587 | 0.0057350000000000009 |  |
| kernel-launch/event_time:2 | 0.00567 | 0.00583 | 0.005743333333333333 |  |
| kernel-launch/event_time:3 | 0.00541 | 0.0058 | 0.005583333333333333 |  |
| kernel-launch/event_time:4 | 0.00547 | 0.00582 | 0.0056700000000000009 |  |
| kernel-launch/event_time:5 | 0.00539 | 0.0059 | 0.005645 |  |
| kernel-launch/event_time:6 | 0.00541 | 0.00568 | 0.005555 |  |
| kernel-launch/event_time:7 | 0.00537 | 0.00562 | 0.00551 |  |
| kernel-launch/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| kernel-launch/wall_time:0 | 0.0096 | 0.01037 | 0.010065 |  |
| kernel-launch/wall_time:1 | 0.00962 | 0.01037 | 0.010141666666666667 |  |
| kernel-launch/wall_time:2 | 0.01021 | 0.0104 | 0.010316666666666667 |  |
| kernel-launch/wall_time:3 | 0.00962 | 0.01034 | 0.010096666666666666 |  |
| kernel-launch/wall_time:4 | 0.00976 | 0.01041 | 0.010003333333333333 |  |
| kernel-launch/wall_time:5 | 0.00968 | 0.01048 | 0.010041666666666666 |  |
| kernel-launch/wall_time:6 | 0.00947 | 0.01006 | 0.00966 |  |
| kernel-launch/wall_time:7 | 0.0095 | 0.01008 | 0.009675 |  |
| lstm_models/pytorch-lstm/fp16_train_step_time | 29.822806946001948 | 2465.177515029907 | 1601.5951260646269 |  |
| lstm_models/pytorch-lstm/fp16_train_throughput | 13.066083299261578 | 7951.362709547692 | 271.023543237469 |  |
| lstm_models/pytorch-lstm/fp32_train_step_time | 52.06409422494471 | 2554.69087600708 | 1702.7455349960774 |  |
| lstm_models/pytorch-lstm/fp32_train_throughput | 12.557779013876054 | 4407.142705595614 | 165.27284343271939 |  |
| lstm_models/pytorch-lstm/return_code | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| lstm_models/pytorch-lstm/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| matmul/nosharding_time | 34.608806133270267 | 35.756775856018069 | 34.993525981903079 |  |
| matmul/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| matmul/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/d2d_bw:0 | 1152.3 | 1157.4 | 1155.1000000000002 |  |
| mem-bw/d2d_bw:1 | 1148.1 | 1160.0 | 1153.2 |  |
| mem-bw/d2d_bw:2 | 1144.8 | 1155.3 | 1150.3166666666667 |  |
| mem-bw/d2d_bw:3 | 1149.2 | 1157.9 | 1154.6666666666668 |  |
| mem-bw/d2d_bw:4 | 1139.3 | 1155.5 | 1150.2166666666665 |  |
| mem-bw/d2d_bw:5 | 1148.8 | 1159.4 | 1153.3999999999999 |  |
| mem-bw/d2d_bw:6 | 1148.4 | 1159.1 | 1154.5 |  |
| mem-bw/d2d_bw:7 | 1150.4 | 1160.6 | 1155.9833333333334 |  |
| mem-bw/d2h_bw:0 | 25.2 | 26.2 | 25.76666666666667 |  |
| mem-bw/d2h_bw:1 | 26.2 | 26.3 | 26.28333333333333 |  |
| mem-bw/d2h_bw:2 | 23.7 | 26.3 | 25.566666666666668 |  |
| mem-bw/d2h_bw:3 | 23.0 | 26.3 | 25.733333333333336 |  |
| mem-bw/d2h_bw:4 | 23.0 | 26.3 | 25.73333333333333 |  |
| mem-bw/d2h_bw:5 | 26.2 | 26.3 | 26.28333333333333 |  |
| mem-bw/d2h_bw:6 | 23.0 | 26.3 | 25.71666666666667 |  |
| mem-bw/d2h_bw:7 | 26.2 | 26.3 | 26.25 |  |
| mem-bw/h2d_bw:0 | 26.1 | 26.1 | 26.100000000000006 |  |
| mem-bw/h2d_bw:1 | 26.1 | 26.1 | 26.100000000000006 |  |
| mem-bw/h2d_bw:2 | 26.1 | 26.1 | 26.100000000000006 |  |
| mem-bw/h2d_bw:3 | 26.1 | 26.2 | 26.133333333333338 |  |
| mem-bw/h2d_bw:4 | 26.0 | 26.2 | 26.100000000000006 |  |
| mem-bw/h2d_bw:5 | 26.1 | 26.1 | 26.100000000000006 |  |
| mem-bw/h2d_bw:6 | 26.1 | 26.1 | 26.100000000000006 |  |
| mem-bw/h2d_bw:7 | 26.0 | 26.1 | 26.083333333333333 |  |
| mem-bw/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| mem-bw/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:4 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:5 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:6 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_corrected_ecc:7 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_power_limit:0 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:1 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:2 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:3 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:4 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:5 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:6 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_power_limit:7 | 400.0 | 400.0 | 400.0 |  |
| monitor/gpu_remap_correctable_error:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_correctable_error:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_correctable_error:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_correctable_error:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_correctable_error:4 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_correctable_error:5 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_correctable_error:6 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_correctable_error:7 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_high:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_high:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_high:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_high:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_high:4 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_high:5 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_high:6 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_high:7 | 0.0 | 1.0 | 0.16666666666666667 |  |
| monitor/gpu_remap_low:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:4 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:5 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:6 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_low:7 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_max:0 | 640.0 | 640.0 | 640.0 |  |
| monitor/gpu_remap_max:1 | 640.0 | 640.0 | 640.0 |  |
| monitor/gpu_remap_max:2 | 640.0 | 640.0 | 640.0 |  |
| monitor/gpu_remap_max:3 | 640.0 | 640.0 | 640.0 |  |
| monitor/gpu_remap_max:4 | 639.0 | 640.0 | 639.8333333333334 |  |
| monitor/gpu_remap_max:5 | 639.0 | 640.0 | 639.8333333333334 |  |
| monitor/gpu_remap_max:6 | 639.0 | 640.0 | 639.8333333333334 |  |
| monitor/gpu_remap_max:7 | 639.0 | 640.0 | 639.8333333333334 |  |
| monitor/gpu_remap_none:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:4 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:5 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:6 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_none:7 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:4 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:5 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:6 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_partial:7 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:4 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:5 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:6 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_remap_uncorrectable_error:7 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_temperature:0 | 79.0 | 84.0 | 81.5 |  |
| monitor/gpu_temperature:1 | 69.0 | 81.0 | 74.16666666666667 |  |
| monitor/gpu_temperature:2 | 75.0 | 85.0 | 79.33333333333333 |  |
| monitor/gpu_temperature:3 | 68.0 | 81.0 | 74.16666666666667 |  |
| monitor/gpu_temperature:4 | 76.0 | 84.0 | 80.33333333333333 |  |
| monitor/gpu_temperature:5 | 64.0 | 77.0 | 71.16666666666667 |  |
| monitor/gpu_temperature:6 | 76.0 | 84.0 | 80.33333333333333 |  |
| monitor/gpu_temperature:7 | 66.0 | 79.0 | 74.16666666666667 |  |
| monitor/gpu_uncorrected_ecc:0 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:1 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:2 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:3 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:4 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:5 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:6 | 0.0 | 0.0 | 0.0 |  |
| monitor/gpu_uncorrected_ecc:7 | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_1024_algbw | 0.03 | 0.03 | 0.03 |  |
| nccl-bw/allreduce_1024_busbw | 0.06 | 0.06 | 0.06 |  |
| nccl-bw/allreduce_1024_time | 30.86 | 31.86 | 31.461666666666664 |  |
| nccl-bw/allreduce_1048576_algbw | 14.61 | 15.19 | 14.971666666666666 |  |
| nccl-bw/allreduce_1048576_busbw | 25.56 | 26.58 | 26.2 |  |
| nccl-bw/allreduce_1048576_time | 69.03 | 71.78 | 70.05333333333333 |  |
| nccl-bw/allreduce_1073741824_algbw | 131.65 | 131.76 | 131.7 |  |
| nccl-bw/allreduce_1073741824_busbw | 230.39 | 230.59 | 230.47833333333333 |  |
| nccl-bw/allreduce_1073741824_time | 8148.9 | 8155.8 | 8152.8499999999999 |  |
| nccl-bw/allreduce_128_algbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_128_busbw | 0.01 | 0.01 | 0.01 |  |
| nccl-bw/allreduce_128_time | 31.26 | 32.67 | 31.805000000000005 |  |
| nccl-bw/allreduce_131072_algbw | 3.44 | 3.54 | 3.498333333333333 |  |
| nccl-bw/allreduce_131072_busbw | 6.03 | 6.19 | 6.12 |  |
| nccl-bw/allreduce_131072_time | 37.07 | 38.06 | 37.48166666666666 |  |
| nccl-bw/allreduce_134217728_algbw | 120.51 | 121.95 | 121.26833333333336 |  |
| nccl-bw/allreduce_134217728_busbw | 210.89 | 213.41 | 212.22 |  |
| nccl-bw/allreduce_134217728_time | 1100.6 | 1113.7 | 1106.7666666666667 |  |
| nccl-bw/allreduce_16384_algbw | 0.49 | 0.51 | 0.5016666666666666 |  |
| nccl-bw/allreduce_16384_busbw | 0.85 | 0.9 | 0.8766666666666666 |  |
| nccl-bw/allreduce_16384_time | 31.91 | 33.55 | 32.65333333333333 |  |
| nccl-bw/allreduce_16777216_algbw | 69.71 | 72.57 | 71.54666666666667 |  |
| nccl-bw/allreduce_16777216_busbw | 121.99 | 126.99 | 125.205 |  |
| nccl-bw/allreduce_16777216_time | 231.2 | 240.7 | 234.54999999999999 |  |
| nccl-bw/allreduce_16_algbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_16_busbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_16_time | 31.26 | 32.33 | 31.506666666666665 |  |
| nccl-bw/allreduce_2048_algbw | 0.06 | 0.06 | 0.06 |  |
| nccl-bw/allreduce_2048_busbw | 0.11 | 0.11 | 0.11 |  |
| nccl-bw/allreduce_2048_time | 31.93 | 32.53 | 32.25 |  |
| nccl-bw/allreduce_2097152_algbw | 23.3 | 24.07 | 23.73166666666667 |  |
| nccl-bw/allreduce_2097152_busbw | 40.78 | 42.13 | 41.53 |  |
| nccl-bw/allreduce_2097152_time | 87.11 | 90.01 | 88.38 |  |
| nccl-bw/allreduce_2147483648_algbw | 132.58 | 132.73 | 132.65666666666668 |  |
| nccl-bw/allreduce_2147483648_busbw | 232.02 | 232.28 | 232.15 |  |
| nccl-bw/allreduce_2147483648_time | 16179.0 | 16197.0 | 16188.0 |  |
| nccl-bw/allreduce_256_algbw | 0.01 | 0.01 | 0.01 |  |
| nccl-bw/allreduce_256_busbw | 0.01 | 0.01 | 0.01 |  |
| nccl-bw/allreduce_256_time | 31.15 | 32.24 | 31.621666666666667 |  |
| nccl-bw/allreduce_262144_algbw | 5.78 | 6.03 | 5.933333333333334 |  |
| nccl-bw/allreduce_262144_busbw | 10.11 | 10.55 | 10.383333333333333 |  |
| nccl-bw/allreduce_262144_time | 43.49 | 45.36 | 44.19166666666666 |  |
| nccl-bw/allreduce_268435456_algbw | 127.93 | 128.57 | 128.19333333333337 |  |
| nccl-bw/allreduce_268435456_busbw | 223.87 | 225.0 | 224.33666666666668 |  |
| nccl-bw/allreduce_268435456_time | 2087.9 | 2098.4 | 2094.0499999999999 |  |
| nccl-bw/allreduce_32768_algbw | 1.01 | 1.06 | 1.0366666666666669 |  |
| nccl-bw/allreduce_32768_busbw | 1.78 | 1.85 | 1.8183333333333334 |  |
| nccl-bw/allreduce_32768_time | 31.04 | 32.29 | 31.586666666666664 |  |
| nccl-bw/allreduce_32_algbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_32_busbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_32_time | 31.15 | 32.24 | 31.60833333333333 |  |
| nccl-bw/allreduce_33554432_algbw | 93.88 | 94.85 | 94.49166666666668 |  |
| nccl-bw/allreduce_33554432_busbw | 164.29 | 165.98 | 165.35999999999999 |  |
| nccl-bw/allreduce_33554432_time | 353.8 | 357.4 | 355.11666666666675 |  |
| nccl-bw/allreduce_4096_algbw | 0.13 | 0.13 | 0.13 |  |
| nccl-bw/allreduce_4096_busbw | 0.22 | 0.22 | 0.22 |  |
| nccl-bw/allreduce_4096_time | 31.93 | 32.69 | 32.265 |  |
| nccl-bw/allreduce_4194304_algbw | 41.33 | 41.86 | 41.595 |  |
| nccl-bw/allreduce_4194304_busbw | 72.32 | 73.26 | 72.79333333333334 |  |
| nccl-bw/allreduce_4194304_time | 100.2 | 101.5 | 100.83333333333333 |  |
| nccl-bw/allreduce_4294967296_algbw | 133.74 | 134.06 | 133.98333333333333 |  |
| nccl-bw/allreduce_4294967296_busbw | 234.05 | 234.6 | 234.475 |  |
| nccl-bw/allreduce_4294967296_time | 32038.0 | 32114.0 | 32055.5 |  |
| nccl-bw/allreduce_512_algbw | 0.02 | 0.02 | 0.02 |  |
| nccl-bw/allreduce_512_busbw | 0.03 | 0.03 | 0.03 |  |
| nccl-bw/allreduce_512_time | 31.19 | 32.01 | 31.525000000000003 |  |
| nccl-bw/allreduce_524288_algbw | 9.1 | 9.26 | 9.19 |  |
| nccl-bw/allreduce_524288_busbw | 15.93 | 16.21 | 16.086666666666667 |  |
| nccl-bw/allreduce_524288_time | 56.59 | 57.6 | 57.031666666666669 |  |
| nccl-bw/allreduce_536870912_algbw | 129.48 | 129.75 | 129.61166666666666 |  |
| nccl-bw/allreduce_536870912_busbw | 226.6 | 227.06 | 226.8166666666667 |  |
| nccl-bw/allreduce_536870912_time | 4137.8 | 4146.3 | 4142.216666666667 |  |
| nccl-bw/allreduce_64_algbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_64_busbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_64_time | 31.23 | 31.63 | 31.355 |  |
| nccl-bw/allreduce_65536_algbw | 1.93 | 1.96 | 1.9416666666666665 |  |
| nccl-bw/allreduce_65536_busbw | 3.37 | 3.43 | 3.3983333333333336 |  |
| nccl-bw/allreduce_65536_time | 33.47 | 34.03 | 33.76333333333333 |  |
| nccl-bw/allreduce_67108864_algbw | 118.17 | 119.25 | 118.85000000000001 |  |
| nccl-bw/allreduce_67108864_busbw | 206.81 | 208.69 | 207.98833333333332 |  |
| nccl-bw/allreduce_67108864_time | 562.8 | 567.9 | 564.6666666666666 |  |
| nccl-bw/allreduce_8192_algbw | 0.25 | 0.26 | 0.25666666666666668 |  |
| nccl-bw/allreduce_8192_busbw | 0.44 | 0.45 | 0.4466666666666667 |  |
| nccl-bw/allreduce_8192_time | 31.63 | 32.57 | 32.093333333333337 |  |
| nccl-bw/allreduce_8388608_algbw | 57.04 | 59.18 | 58.12833333333333 |  |
| nccl-bw/allreduce_8388608_busbw | 99.81 | 103.57 | 101.72166666666668 |  |
| nccl-bw/allreduce_8388608_time | 141.7 | 147.1 | 144.35 |  |
| nccl-bw/allreduce_8589934592_algbw | 134.49 | 134.62 | 134.58333333333335 |  |
| nccl-bw/allreduce_8589934592_busbw | 235.36 | 235.59 | 235.5233333333333 |  |
| nccl-bw/allreduce_8589934592_time | 63807.0 | 63871.0 | 63825.666666666664 |  |
| nccl-bw/allreduce_8_algbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_8_busbw | 0.0 | 0.0 | 0.0 |  |
| nccl-bw/allreduce_8_time | 31.32 | 32.19 | 31.618333333333337 |  |
| nccl-bw/return_code | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/fp16_train_step_time | 75.2089096903801 | 594.7049889564514 | 410.4021521926425 |  |
| resnet_models/pytorch-resnet101/fp16_train_throughput | 53.82110385760114 | 758.410774850355 | 190.275701565551 |  |
| resnet_models/pytorch-resnet101/fp32_train_step_time | 92.29285472631455 | 726.5537548065186 | 500.8662575792568 |  |
| resnet_models/pytorch-resnet101/fp32_train_throughput | 44.05092328534653 | 499.5588153825924 | 153.46013380837239 |  |
| resnet_models/pytorch-resnet101/return_code | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet101/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/fp16_train_step_time | 110.18137443065644 | 836.2550797462463 | 581.6630462729781 |  |
| resnet_models/pytorch-resnet152/fp16_train_throughput | 38.26984987644678 | 545.0770878036914 | 131.0200561380481 |  |
| resnet_models/pytorch-resnet152/fp32_train_step_time | 135.50994110107423 | 1013.7442817687988 | 702.3912634779719 |  |
| resnet_models/pytorch-resnet152/fp32_train_throughput | 31.570805629582649 | 355.7244976219539 | 105.4446247444141 |  |
| resnet_models/pytorch-resnet152/return_code | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet152/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/fp16_train_step_time | 40.51648998260498 | 411.7493374347687 | 249.08652069541203 |  |
| resnet_models/pytorch-resnet50/fp16_train_throughput | 83.2208437289737 | 1100.1343042177978 | 338.7381451716442 |  |
| resnet_models/pytorch-resnet50/fp32_train_step_time | 55.160889238119128 | 461.3578190803528 | 318.3858556666384 |  |
| resnet_models/pytorch-resnet50/fp32_train_throughput | 69.38277547344055 | 758.7263290038582 | 252.38379023818295 |  |
| resnet_models/pytorch-resnet50/return_code | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| resnet_models/pytorch-resnet50/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/allgather_time | 10.049663543701172 | 10.064876556396485 | 10.057035764058432 |  |
| sharding-matmul/allreduce_time | 10.535126209259034 | 10.57395315170288 | 10.551154772440592 |  |
| sharding-matmul/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| sharding-matmul/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/fp16_train_step_time | 25.390705093741418 | 333.33779859542849 | 219.33479604352983 |  |
| vgg_models/pytorch-vgg11/fp16_train_throughput | 96.02416054550727 | 1260.344817373537 | 497.0967924194184 |  |
| vgg_models/pytorch-vgg11/fp32_train_step_time | 40.444567799568179 | 408.00737047195437 | 273.79270820471626 |  |
| vgg_models/pytorch-vgg11/fp32_train_throughput | 78.44570939667838 | 791.2721035661484 | 324.9363093970622 |  |
| vgg_models/pytorch-vgg11/return_code | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg11/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/fp16_train_step_time | 34.70470201969147 | 566.627167224884 | 359.92531138534607 |  |
| vgg_models/pytorch-vgg13/fp16_train_throughput | 57.26963262564189 | 922.08435842867 | 358.1590016309353 |  |
| vgg_models/pytorch-vgg13/fp32_train_step_time | 56.147372394800189 | 647.7513403892517 | 419.95702596235568 |  |
| vgg_models/pytorch-vgg13/fp32_train_throughput | 49.97278092624873 | 569.9507707944061 | 231.68832214979595 |  |
| vgg_models/pytorch-vgg13/return_code | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg13/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/fp16_train_step_time | 39.8976745903492 | 646.1778335571289 | 419.52491444365656 |  |
| vgg_models/pytorch-vgg16/fp16_train_throughput | 49.53667935138827 | 802.063626989122 | 311.40216388428578 |  |
| vgg_models/pytorch-vgg16/fp32_train_step_time | 65.62238788604737 | 748.2917523384094 | 489.9213562081081 |  |
| vgg_models/pytorch-vgg16/fp32_train_throughput | 43.13893718095357 | 487.6615145076602 | 198.62286471467895 |  |
| vgg_models/pytorch-vgg16/return_code | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg16/return_code:7 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/fp16_train_step_time | 45.13699382543564 | 744.2489204406738 | 480.06073132130049 |  |
| vgg_models/pytorch-vgg19/fp16_train_throughput | 43.00179752145532 | 708.9611079567222 | 275.3163537463149 |  |
| vgg_models/pytorch-vgg19/fp32_train_step_time | 74.86466133594513 | 864.0479183197022 | 560.2605650500086 |  |
| vgg_models/pytorch-vgg19/fp32_train_throughput | 37.4898894091596 | 427.452003309124 | 174.03624176807086 |  |
| vgg_models/pytorch-vgg19/return_code | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:0 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:1 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:2 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:3 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:4 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:5 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:6 | 0.0 | 0.0 | 0.0 |  |
| vgg_models/pytorch-vgg19/return_code:7 | 0.0 | 0.0 | 0.0 |  |