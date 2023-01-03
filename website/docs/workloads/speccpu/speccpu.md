# SPEC CPU Workload Suite
SPEC CPU is a workload created and licensed by the Standard Performance Evalution Corporation. The SPEC CPU® 2017 benchmark package contains SPEC's 
next-generation, industry-standardized, CPU intensive suites for measuring and comparing compute intensive performance, stressing a system's processor, 
memory subsystem and compiler.

* [SPEC CPU Documentation](https://www.spec.org/cpu2017/)  
* [SPEC CPU 2017 QuickStart](https://spec.org/cpu2017/Docs/quick-start.html)  
* [SPEC CPU Download](https://pro.spec.org/private/osg/cpu/cpu2017/src/)  
* [SPEC CPU Benchmarks](https://www.spec.org/cpu2017/Docs/overview.html#Q13)

### What is Being Tested?
SPEC teams designed these suites to provide a comparative measure of compute-intensive performance across the widest practical range of hardware 
using workloads developed from real user applications. The benchmarks are provided as source code and require the use of compiler commands 
as well as other commands via a shell or command prompt window. SPEC CPU 2017 also includes an optional metric for measuring energy consumption.

The SPEC CPU 2017 benchmark package contains 43 benchmarks, organized into four distinct workload suites:
* SPECspeed® 2017 Integer
  * Used for measure the time required for the computer to complete single integer calculations.
* SPECspeed® 2017 Floating Point 
  * Used to measure the time required for the computer to complete single floating-point calculations.
* SPECrate® 2017 Integer  
  * Measure the throughput or work per unit of time on the computer for integer calculations.
* SPECrate® 2017 Floating Point
  * Measure the throughput or work per unit of time on the computer for floating-point calculations.

---
### System Requirements
The following section provides special considerations required for the system on which the SPEC CPU workload will be run.

https://spec.org/cpu2017/Docs/system-requirements.html

* Physical Memory = 16 GB minimum  
* Disk Space = 250 GB minimum on the OS disk

### Supported Platforms

https://spec.org/cpu2017/Docs/system-requirements.html#SupportedTools

* Linux x64
* Linux arm64
* Windows x64
* Windows arm64

---
### Package Dependencies
The following package dependencies are required to be installed on the Unix/Linux system in order to support the requirements
of the SPEC CPU workload. Note that the Virtual Client will handle the installation of any required dependencies.

* gcc
* make
* gfortran
