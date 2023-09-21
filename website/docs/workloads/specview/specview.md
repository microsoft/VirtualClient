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

### Composites
| Viewset       | fps |
|----------------|-------|
| 3dsmax-07      | 35.46 |
| catia-06       | 43.3  |
| creo-03        | 56.98 |
| energy-03      | 27.46 |
| maya-06        | 227.8 |
| medical-03     | 35.16 |
| snx-04         | 139.11|
| solidworks-07  | 97.8  |

### Individual benchmark
| Viewset        | Index | Name                           | Weight | FPS    |
|----------------|-------|--------------------------------|--------|--------|
| 3dsmax-07      | 1     | 3dsmax_Arch_Shaded             | 9.52   | 50.87  |
| 3dsmax-07      | 2     | 3dsmax_Arch_Graphite           | 9.52   | 42.35  |
| 3dsmax-07      | 3     | 3dsmax_Space_Wireframe         | 9.52   | 147.76 |
| 3dsmax-07      | ....  | ....                           | ....   | ....   |
| catia-06       | 1     | catiaV5test1                   | 14.28  | 34.01  |
| catia-06       | 2     | catiaV5test2                   | 14.28  | 44.3   |
| catia-06       | 3     | catiaV5test3                   | 0      | 424.59 |
| catia-06       | ....  | ....                           | ....   | ....   |
| creo-03        | 1     | Creo_03_test_01                | 8.33   | 60.44  |
| creo-03        | 2     | Creo_03_test_02                | 8.33   | 25.21  |
| creo-03        | 3     | Creo_03_test_03                | 8.34   | 33.31  |
| creo-03        | ....  | ....                           | ....   | ....   |
| energy-03      | 1     | Test1                          | 16.67  | 38.02  |
| energy-03      | 2     | Test2                          | 16.67  | 20.46  |
| energy-03      | 3     | Test3                          | 16.67  | 18.04  |
| energy-03      | ....  | ....                           | ....   | ....   |
| maya-06        | 1     | Maya_01                        | 8.33   | 120.22 |
| maya-06        | 2     | Maya_02                        | 8.33   | 664.73 |
| maya-06        | 3     | Maya_03                        | 12.5   | 106.89 |
| maya-06        | ....  | ....                           | ....   | ....   |
| medical-03     | 1     | Test1                          | 10     | 258.34 |
| medical-03     | 2     | Test2                          | 10     | 310.64 |
| medical-03     | 3     | Test3                          | 10     | 63.19  |
| medical-03     | ....  | ....                           | ....   | ....   |
| snx-04         | 1     | NX8_AdvancedStudioAA           | 7.5    | 123.54 |
| snx-04         | 2     | NX8_ShadedAA                   | 10     | 164.72 |
| snx-04         | 3     | NX8_ShadedWithEdgeAA           | 20     | 85.33  |
| snx-04         | ....  | ....                           | ....   | ....   |
| solidworks-07  | 1     | SW2020_RallyCar_Realview       | 10     | 139.26 |
| solidworks-07  | 2     | SW2020_RallyCar_ShadedEdges    | 10     | 191.75 |
| solidworks-07  | 3     | SW2020_RallyCar_Shaded         | 15     | 256.76 |
| solidworks-07  | ....  | ....                           | ....   | ....   |