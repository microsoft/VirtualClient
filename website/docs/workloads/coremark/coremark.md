# CoreMark
CoreMark is a third party tool that runs a set of benchmark tests in order to measure CPU performance. CoreMark is a preferred CPU benchmarking
toolset because it is compiled to be specialized for the exact physical characteristics of the CPU on the system.

* [CoreMark Documentation](https://www.eembc.org/coremark/)

-----------------------------------------------------------------------

### What is Being Tested?
CoreMark is designed to be a very simple benchmarking tool. It produces a single-number score that allows users to make quick comparisons
between different processors. CoreMark is compiled on the system for which it will run in order to establish a precise test to evaluate the performance of the CPU.

CoreMark runs the following CPU-intensive algorithms in order to produce the single-number score. 

| Name                  | Description                                               |
|-----------------------|-----------------------------------------------------------|
| List Processing       | Find and sort algorithm(s)                                |
| Matrix Manipulation   | Common matrix operations                                  |
| State Machine         | Determine if an input stream contains valid numbers       |
| CRC                   | Cyclic redundancy check                                   |

-----------------------------------------------------------------------

### Supported Platforms
* Linux x64
* Linux arm64

### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the LMbench workload. Note that the Virtual Client will handle the installation of any required dependencies.

* gcc
* make
