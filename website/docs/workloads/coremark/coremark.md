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
