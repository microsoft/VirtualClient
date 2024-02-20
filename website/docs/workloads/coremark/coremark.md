# CoreMark
CoreMark is a third party tool that runs a set of benchmark tests in order to measure CPU performance. CoreMark is a preferred CPU benchmarking
toolset because it is compiled to be specialized for the exact physical characteristics of the CPU on the system.

* [CoreMark Documentation](https://www.eembc.org/coremark/)

## What is Being Measured?
CoreMark is designed to be a very simple benchmarking tool. It produces a single-number score that allows users to make quick comparisons
between different processors. CoreMark is compiled on the system for which it will run in order to establish a precise test to evaluate the performance
of the CPU.

CoreMark runs the following CPU-intensive algorithms in order to produce the single-number score. 

| Name                  | Description                                               |
|-----------------------|-----------------------------------------------------------|
| List Processing       | Find and sort algorithm(s)                                |
| Matrix Manipulation   | Common matrix operations                                  |
| State Machine         | Determine if an input stream contains valid numbers       |
| CRC                   | Cyclic redundancy check                                   |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the CoreMark workload.

| Tool Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| CoreMark | CoreMark Size | 666.0 | 666.0 | 666.0 | bytes |
| CoreMark | Iterations | 400000.0 | 800000.0 | 773160.1731601731 | iterations |
| CoreMark | Iterations/Sec | 19968.051118 | 33889.689062 | 33081.75554433839 | iterations/sec |
| CoreMark | Parallel PThreads | 2.0 | 2.0 | 2.0 | threads |
| CoreMark | Total ticks | 12022.0 | 36126.0 | 23365.67617325762 | ticks |
| CoreMark | Total time (secs) | 12.022 | 36.126 | 23.365676173257606 | secs |

# CoreMark-Pro
CoreMark-PRO is a comprehensive, advanced processor benchmark that works with and enhances the market-proven industry-standard EEMBC CoreMark® benchmark. While CoreMark stresses the CPU pipeline, CoreMark-Pro tests the entire processor, adding comprehensive support for multicore technology, a combination of integer and floating-point workloads, and data sets for utilizing larger memory subsystems. Together, EEMBC CoreMark and CoreMark-PRO provide a standard benchmark covering the spectrum from low-end microcontrollers to high-performance computing processors.

## Workload Metrics

| Tool Name | Metric Name | Example Value | Unit |
|-----------|-------------|---------------|------|
| CoreMark | MultiCore-cjpeg-rose7-preset | 555.56 | iterations/sec |
| CoreMark | MultiCore-cjpeg-rose7-preset | 156.25 | iterations/sec  |
| CoreMark | MultiCore-cjpeg-rose7-preset | 3.56 | scale |
| CoreMark | core | 4.87 | iterations/sec |
| CoreMark | core | 1.30 |  iterations/sec |
| CoreMark | core | 3.75 | scale |
| CoreMark | linear_alg-mid-100x100-sp | 1428.57 | iterations/sec |
| CoreMark | linear_alg-mid-100x100-sp | 409.84 | iterations/sec  |
| CoreMark | linear_alg-mid-100x100-sp  | 3.49 | scale |
| CoreMark | loops-all-mid-10k-sp | 22.56   | iterations/sec |          
| CoreMark | loops-all-mid-10k-sp | 6.25 | iterations/sec |
| CoreMark | loops-all-mid-10k-sp | 3.61  | scale |
| CoreMark | nnet_test | 33.22   | iterations/sec |       
| CoreMark | nnet_test  | 10.56 | iterations/sec |
| CoreMark | nnet_test | 3.15    | scale |        
| CoreMark | parser-125k | 70.18   | iterations/sec |
| CoreMark | parser-125k | 19.23 | iterations/sec |
| CoreMark | parser-125k | 3.65      | scale |                               
| CoreMark | radix2-big-64k | 1666.67  | iterations/sec |  
| CoreMark | radix2-big-64k  | 453.72 | iterations/sec |
| CoreMark | radix2-big-64k  | 3.67  | scale |
| CoreMark | sha-test |   588.24 | iterations/sec |
| CoreMark | sha-test | 172.41 | iterations/sec |
| CoreMark | sha-test | 3.41  | scale |
| CoreMark | zip-test | 500.00 | iterations/sec |
| CoreMark | zip-test | 142.86 | iterations/sec |
| CoreMark | zip-test | 3.50  | scale |
| CoreMark | CoreMark-PRO | 19183.84  | Score  
| CoreMark | CoreMark-PRO | 5439.59 | Score
| CoreMark | CoreMark-PRO | 3.53 | scale
