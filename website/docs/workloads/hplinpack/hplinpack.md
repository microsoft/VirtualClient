# HPLINPACK
HPL stands for High Performance Linpack, is a software package that solves a (random) dense linear system in double precision (64 bits) arithmetic on distributed-memory computers. It can thus be regarded as a portable as well as freely available implementation of the High Performance Computing Linpack Benchmark. 

* [HPLINPACK Offical Website](https://netlib.org/benchmark/hpl/)
* [HPLINPACK Installation Guide](https://netlib.org/benchmark/hpl/software.html)

## What is Being Measured?
HPLinpack is designed to be a very simple benchmarking tool. It produces the amount of time it takes to solve linear system and rate of execution for solving the linear system.

* Time for solving linear system 
* Rate of execution for solving linear system

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the HPLinpack workload.

| Metric Name | Value  | Unit   | Description                                   |
|-------------|--------|--------|-----------------------------------------------|
| Time	      | 152.22 | secs   | Time in seconds to solve the linear system.   |
| GFlops      |	35.041 | Gflops | Rate of execution for solving the linear system.|
