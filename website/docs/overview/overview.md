---
id: overview
slug: /overview
sidebar_position: 1
---

# Platform Overview
The Virtual Client is a unified workload and system monitoring platform for running customer-representative scenarios on virtual machines or physical hosts/blades in the Azure Cloud.
The platform supports a wide range of different industry standard/benchmark workloads used to measuring various aspects of the system under test (e.g. CPU, I/O, network performance, power consumption).
The platform additionally provides the ability to capture important performance and reliability measurements from the underlying system. The platform supports all business-critical
Azure environments including guest/VM systems, host/blade systems and data center/DC lab systems. The platform additionally supports both x64 and ARM64 compute architectures.

* [Getting Started/Downloads](/docs/guides/getting-started)
* [Platform Features](/docs/overview/features)
* [Platform Design](/docs/overview/design)
* [Usage](/docs/guides/command-line)
* [Usage Examples](/docs/guides/usage-examples)
* [Developer Guide](/docs/developing/develop-guide)

## Team Contacts
* [virtualclient@microsoft.com](mailto:virtualclient@microsoft.com)

## Supported Workloads/Benchmarks
The following list of workloads are used by Virtual Client profiles to exercise the system components in a consistent way required to measure performance baselines and differences.

:::caution Comply to licenses you are using
Virtual Client will handle the installation and execution of various tools. Individual license files are not prompted for each workload. By using 
Virtual Client, users accept the license of each of the benchmarks individually, comply to the terms for the tool you are using, and take responsibility 
for using them.
:::

| **Workload/Benchmark** | **Specialization** | **Supported Platforms/Architectures** | **License(s)** |
|------------------------|--------------------|---------------------------------------|----------------|
| [7zip](/docs/workloads/compression/7zip/) | Compression | linux-x64, linux-arm64 | [GNU LGPL](https://www.7-zip.org/faq.html) |
| [ASP.NET Bench](/docs/workloads/aspnetbench) | ASP.NET Kestrel web server throughput and latency.  | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT (ASP.NET)](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)<br/>[MIT (Bombardier)](https://github.com/codesenberg/bombardier/blob/master/LICENSE)<br/>[Apache 2.0 (Wrk)](https://github.com/wg/wrk/blob/master/LICENSE) |
| [BlenderBenchmark](/docs/workloads/blenderbenchmark) | GPU/Graphics Rendering Performance | win-x64 | [GNU LGPL](https://projects.blender.org/infrastructure/blender-open-data/src/branch/main/LICENSE) |
| [CoreMark](/docs/workloads/coremark/) | CPU Performance | linux-x64, linux-arm64 | [Apache+Custom](https://github.com/eembc/coremark/blob/main/LICENSE.md)  |
| [CoreMark Pro](/docs/workloads/coremark) | Precision CPU | linux-x64, linux-arm64, win-x64, win-arm64 | [Apache+Custom](https://github.com/eembc/coremark-pro/blob/main/LICENSE.md) |
| [NCPS](/docs/workloads/network-suite) | Network Connection Reliability | linux-x64, linux-arm64, win-x64, win-arm64 | Microsoft-Developed  |
| [DCGMI](/docs/workloads/dcgmi) | GPU Qualification| linux-x64 | [Apache-2.0](https://github.com/NVIDIA/DCGM/blob/master/LICENSE) |
| [DeathStarBench](/docs/workloads/deathstarbench) | Docker Swarm/Container Microservices | linux-x64, linux-arm64, win-x64, win-arm64 | [Apache-2.0](https://github.com/delimitrou/DeathStarBench/blob/master/LICENSE)  |
| [DiskSpd](/docs/workloads/diskspd) | Disk I/O Performance | win-x64, win-arm64 | [MIT](https://github.com/microsoft/diskspd/blob/master/LICENSE)  |
| [Dotnet (.NET) Runtime](/docs/workloads/dotnetruntime/) | .NET Application Performance | win-x64, win-arm64 | [MIT License](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) |
| [ElasticSearch (Rally)](/docs/workloads/elasticsearch) | Search/Query Engine | linux-x64, linux-arm64, win-x64, win-arm64 | [Apache-2.0](https://github.com/elastic/rally/blob/master/LICENSE) |
| [Flexible IO Tester (FIO)](/docs/workloads/fio) | Disk I/O Performance | linux-x64, linux-arm64, win-x64 | [GPL-2.0](https://github.com/axboe/fio/blob/master/COPYING)  |
| [GeekBench5](/docs/workloads/geekbench/) | CPU Performance | linux-x64, win-x64, win-arm64 | [End User License Required](https://www.primatelabs.com/legal/eula-v5.html). Software/license purchase required for use. |
| [GeekBench6](/docs/workloads/geekbench/) | CPU Performance | linux-x64, win-x64, win-arm64 | [End User License Required](https://www.primatelabs.com/legal/eula-v6.html). Software/license purchase required for use. |
| [Graph500](/docs/workloads/graph500) | 3D Simulation | linux-x64, linux-arm64 | [Custom](https://github.com/graph500/graph500/blob/newreference/license.txt)  |
| [Gzip](/docs/workloads/compression/gzip) | Compression | linux-x64, linux-arm64 | [GPL](https://www.gnu.org/software/gzip/)  |
| [HPCG](/docs/workloads/hpcg) | High Performance Compute (HPC) | linux-x64, linux-arm64 | [Custom](https://github.com/hpcg-benchmark/hpcg/blob/master/COPYING)  |
| [HPLinpack](/docs/workloads/hplinpack) | Linear Equations | linux-x64, linux-arm64| [IBM](https://netlib.org/benchmark/hpl/IBM_LICENSE.TXT)  |
| [LAPACK](/docs/workloads/lapack) | Linear Equations | linux-x64, linux-arm64, win-x64, win-arm64 | [Custom](https://github.com/Reference-LAPACK/lapack/blob/master/LICENSE)  |
| [Latte](/docs/workloads/network-suite) | Network Latencies | win-x64, win-arm64 | [MIT](https://github.com/microsoft/latte/blob/main/LICENSE)  |
| [LMbench](/docs/workloads/lmbench) | Memory Performance | linux-x64, linux-arm64 | [GPL-2.0](https://github.com/intel/lmbench/blob/master/COPYING)  |
| [LZBench](/docs/workloads/compression/lzbench) | Compression/Streaming | linux-x64, linux-arm64, win-x64, win-arm64 | [None](https://github.com/inikep/lzbench)  |
| [Memcached](/docs/workloads/memcached) | In-Memory Data Cache | linux-x64, linux-arm64 | [BSD-3 (Memcached)](https://github.com/memcached/memcached/blob/master/LICENSE)<br/>[GPL-2.0 (Memtier)](https://github.com/RedisLabs/memtier_benchmark/blob/master/COPYING)  |
| [NAS Parallel](/docs/workloads/nasparallel) | High Performance Compute (HPC) | linux-x64, linux-arm64 | [NASA-1.3](https://opensource.org/licenses/nasa1.3.php)  |
| [Network ICMP Ping](/docs/workloads/network-ping) | Network Latencies | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT](https://github.com/microsoft/VirtualClient/blob/main/LICENSE)  |
| [NGINX](/docs/workloads/nginx) | Web Server | linux-x64, linux-arm64, win-x64, win-arm64 | [BSD-2-Clause (NGINX)](https://github.com/nginx/nginx/blob/master/LICENSE)<br/>[Apache 2.0 (Wrk)](https://github.com/wg/wrk/blob/master/LICENSE)<br/>[Apache 2.0 (Wrk2)](https://github.com/giltene/wrk2/blob/master/LICENSE)  |
| [NTttcp](/docs/workloads/network-suite) | Network Bandwidth | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT](https://github.com/microsoft/ntttcp/blob/main/LICENSE)  |
| [OpenFOAM](/docs/workloads/openfoam) | Computational Fluid Dynamics | linux-x64, linux-arm64 | [Custom](https://github.com/OpenFOAM/OpenFOAM-10/blob/master/COPYING)  |
| [OpenSSL](/docs/workloads/openssl) | Cryptography/Encryption | linux-x64, linux-arm64, win-x64 | [Apache-2.0](https://github.com/openssl/openssl/blob/master/LICENSE.txt)  |
| [Pbzip2](/docs/workloads/compression/pbzip2) | Compression | linux-x64, linux-arm64 | [BSD](http://compression.great-site.net/pbzip2/)  |
| [PostgreSQL](/docs/workloads/postgresql) | Relational Database Performance | linux-x64, linux-arm64, win-x64 | [PostgreSQL](https://www.postgresql.org/about/licence/) |
| [Prime95](/docs/workloads/prime95) | CPU Stress | linux-x64 | [Custom](https://www.mersenne.org/legal/)  |
| [Redis](/docs/workloads/redis) | In-Memory Data Cache | linux-x64, linux-arm64 | [BSD-3 (Redis)](https://github.com/redis/redis/blob/unstable/COPYING)<br/>[GPL-2.0 (Memtier)](https://github.com/RedisLabs/memtier_benchmark/blob/master/COPYING)  |
| [SockPerf](/docs/workloads/network-suite) | Network Latencies | linux-x64, linux-arm64 | [Custom](https://github.com/Mellanox/sockperf/blob/sockperf_v2/copying)  |
| [SPEC CPU 2017, SPECrate Integer](/docs/workloads/speccpu/) | Precision CPU, Integer Calculations | linux-x64, linux-arm64, win-x64, win-arm64 | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2017, SPECrate Floating Point](/docs/workloads/speccpu/) | Precision CPU, Floating-point Calculations | linux-x64, linux-arm64, win-x64, win-arm64  | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2017, SPECspeed Integer](/docs/workloads/speccpu/) | Precision CPU, Integer Calculations | linux-x64, linux-arm64, win-x64, win-arm64  | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2017, SPECspeed Floating Point](/docs/workloads/speccpu/) | Precision CPU, Floating-point Calculations | linux-x64, linux-arm64, win-x64, win-arm64  | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2026, SPECrate Integer](/docs/workloads/speccpu/) | Precision CPU, Integer Calculations | linux-x64, linux-arm64 | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2026, SPECrate Floating Point](/docs/workloads/speccpu/) | Precision CPU, Floating-point Calculations | linux-x64, linux-arm64  | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2026, SPECspeed Integer](/docs/workloads/speccpu/) | Precision CPU, Integer Calculations | linux-x64, linux-arm64  | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC CPU 2026, SPECspeed Floating Point](/docs/workloads/speccpu/) | Precision CPU, Floating-point Calculations | linux-x64, linux-arm64  | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html). Software/license purchase required for use. |
| [SPEC JBB 2015, SPECjbb](/docs/workloads/specjbb/) | Java Server | linux-x64, linux-arm64, win-x64, win-arm64 | [End User License Required](https://www.spec.org/jbb2015/). Software/license purchase required for use. |
| [SPEC JVM 2008, SPECjvm](/docs/workloads/specjvm) | Java Runtime Performance | linux-x64, linux-arm64, win-x64, win-arm64 | [SPEC](https://www.spec.org/spec/docs/SPEC_General_License.pdf)  |
| [SPEC Power 2008, SPECpower](/docs/workloads/specpower/) | High precision, steady-state CPU usage | linux-x64, linux-arm64, win-x64, win-arm64 | [End User License Required](https://www.spec.org/power_ssj2008/). Software/license purchase required for use. |
| [SPECviewperf 2020, SPECview](/docs/workloads/specview) | 3D graphics performance | win-x64 | [SPEC](https://gwpg.spec.org/benchmarks/benchmark/specviewperf-2020-v3-0/)  |
| [Stressapptest](/docs/workloads/stressapptest) | Fault Tolerance | linux-x64, linux-arm64 | [Apache-2.0](https://github.com/stressapptest/stressapptest/blob/master/NOTICE)  |
| [Stress-ng](/docs/workloads/stress-ng) | Fault Tolerance | linux-x64, linux-arm64 | [GPL-2.0](https://github.com/ColinIanKing/stress-ng/blob/master/COPYING)  |
| [Sysbench](/docs/workloads/sysbench) | Relational Database Performance | linux-x64, linux-arm64 | [GPL-2.0 (Sysbench)](https://github.com/akopytov/sysbench/blob/master/COPYING)<br/>[GPL-2.0 (MySQL)](https://www.mysql.com/about/legal/licensing/oem/) |

## Data Collection Notice
The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services
and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may
enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing
appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located
at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement.
Your use of the software operates as your consent to these practices.

## Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
