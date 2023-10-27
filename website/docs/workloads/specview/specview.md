# SPECviewperf Benchmark
The SPECviewperf® 2020 v3.0 benchmark, released on December 9, 2021, is the worldwide standard for measuring graphics performance based on professional applications.
The benchmark’s workloads, called viewsets, represent graphics content and behavior extracted from professional applications, without the need to install the applications themselves. 

Applications represented by viewsets in SPECviewperf 2020 include Autodesk 3ds Max and Maya for media and entertainment; Dassault Systèmes Catia and Solidworks, PTC Creo, and Siemens NX for CAD/CAM; and two datasets representing professional energy and medical applications.

* [SPECviewperf 2020 v3.0](https://gwpg.spec.org/benchmarks/benchmark/specviewperf-2020-v3-0/)

## What is Being Measured 
The benchmark measures the 3D graphics performance of systems running under the OpenGL and DirectX application programming interfaces.

## System Requirements
* Microsoft Windows 10 Version 1709 (Fall Creators Update / RS3) or newer
* 16GB of system RAM or greater
* 120GB of available disk space
* A minimum screen resolution of 1920×1080 for submissions published on the SPEC website
OpenGL 4.5 (for catia-06, creo-03, energy-03, maya-06, medical-03, snx-04, and solidworks-07) and DirectX 12 API support (for 3dsmax-07)
* A GPU with 2GB or greater dedicated GPU memory

## Benchmark License
The SPECviewperf 2020 benchmark is available for free downloading to everyone except for any for-profit entity that sells computers or computer  related products in the commercial marketplace, with the exception of SPEC/GWPG member companies (AMD, Dell, Fujitsu, HP Inc, Intel, Lenovo, Nvidia, VeriSilicon) that receive benchmark licenses as a membership benefit. Examples of those requiring a paid license include:

* Computer hardware and software vendors
* Computer component manufacturers (hard drives, memory, device vendors)
* Computer-related service providers (for-profit resellers, distributors, consultants)
* Computer operating system companies

Read more about licensing from the "Download" section of [this page](https://gwpg.spec.org/benchmarks/benchmark/specviewperf-2020-v3-0/).

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the SPECview workload.

### Example Metrics
| MetricName      | MetricValue | MetricUnit |
|---------------- |-------------|------------|
| solidworks-07   |	10.23	    | fps        |
| solidworks-07       | ...         | ...        |
| 3dsmax-07       | 312.31         | fps       |
| 3dsmax-07       | ...         | ...        |
| 3dsmax-07       | ...         | ...        |
| catia-06       | 31.31         | fps        |
| catia-06       | ...         | ...        |
| catia-06       | ...         | ...        |
| creo-03       | 87.12        | fps        |
| creo-03       | ...         | ...        |
| creo-03       | ...         | ...        |
| energy-03       | 78.54         | fps        |
| energy-03       | ...         | ...        |
| energy-03       | ...         | ...        |
| maya-06       | 790.21        | fps        |
| maya-06       | ...         | ...        |
| maya-06       | ...         | ...        |
| medical-03       | 76.93         | fps        |
| medical-03       | ...         | ...        |
| medical-03       | ...         | ...        |
| snx-04       | 87.12        | fps        |
| snx-04       | ...         | ...        |
| snx-04       | ...         | ...        |
           
### Examples Metadata
More information can be found at the metric's metadata
```json 
{
  "weight": 10.0,
  "index": 10,
  "isCompositeScore": false
}
```