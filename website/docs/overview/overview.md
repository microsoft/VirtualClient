---
id: overview
sidebar_position: 1
---

# Platform Overview
The Virtual Client is a unified workload and system monitoring platform for running customer-representative scenarios on virtual machines or physical hosts/blades in the Azure Cloud.
The platform supports a wide range of different industry standard/benchmark workloads used to measuring various aspects of the system under test (e.g. CPU, I/O, network performance, power consumption).
The platform additionally provides the ability to capture important performance and reliability measurements from the underlying system. The platform supports all business-critical
Azure environments including guest/VM systems, host/blade systems and data center/DC lab systems. The platform additionally supports both x64 and ARM64 compute architectures.

* [Platform Features](./0010-features.md)
* [Platform Design](./0020-design.md)
* [Usage](../guides/0010-command-line.md)
* [Usage Examples](../guides/0200-usage-examples.md)
* [Developer Guide](../developing/0010-develop-guide.md)

## Team Contacts
* [virtualclient@microsoft.com](mailto:virtualclient@microsoft.com)

## Downloads
Release versions of the Virtual Client are available on public NuGet.org. Note that versions that are not tagged as 'Release'
are considered beta quality. Production quality releases will be tagged appropriately.

* NuGet Releases
    * https://www.nuget.org/packages/VirtualClient
    * Please note we will be renaming package to Microsoft.VirtualClient in the future pending MSFT internal processes.

## Platform/Architectures
In the workload and profile sections below, the following designations are used to indicate the OS platform and CPU architecture on which the workloads are supported:
* **linux-x64**
  The workload is supported on the Linux operating system for Intel and AMD x64 architectures.<br/><br/>
* **linux-arm64**
  The workload is supported on the Linux operating system for ARM64/AARCH64 architectures.<br/><br/>
* **win-x64**
  The workload is supported on the Windows operating system for Intel and AMD x64 architectures.<br/><br/>
* **win-arm64**
  The workload is supported on the Windows operating system for ARM64/AARCH64 architectures.


## Supported Workloads/Benchmarks
The following list of workloads are used by Virtual Client profiles to exercise the system components in a consistent way required to measure performance baselines and differences.

:::caution Comply to licenses you are using
VirtualClient handles the installation and execution of various tools. Individual license files are not prompted for each workload. By using 
VirtualClient, users accept the license of each of the benchmarks individually, comply to the terms for the tool you are using, and take responsibility 
for using them.
:::

| **Workload/Benchmark** | **Specialization** | **Supported Platforms/Architectures** | **License(s)** | 
|------------------------|--------------------|---------------------------------------|----------------|
| [7zip](https://microsoft.github.io/VirtualClient/docs/workloads/compression/7zip) | Compression | linux-x64, linux-arm64 | [GNU LGPL](https://www.7-zip.org/faq.html) |
| [AspNetBench](https://microsoft.github.io/VirtualClient/docs/workloads/aspnetbench/aspnetbench) | ASP.NET Web Server.  | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT (ASP.NET)](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)<br/>[MIT (Bombardier)](https://github.com/codesenberg/bombardier/blob/master/LICENSE) |
| [CoreMark](https://microsoft.github.io/VirtualClient/docs/workloads/coremark/coremark) | CPU Performance | linux-x64, linux-arm64 | [Apache+Custom](https://github.com/eembc/coremark/blob/main/LICENSE.md)  |
| [CoreMark Pro](https://microsoft.github.io/VirtualClient/docs/workloads/coremark) | Precision CPU | linux-x64, linux-arm64, win-x64, win-arm64 | [Apache+Custom](https://github.com/eembc/coremark-pro/blob/main/LICENSE.md) |
| [CPS](https://microsoft.github.io/VirtualClient/docs/workloads/network-suite/network-suite.md) | Network Connection Reliability | linux-x64, linux-arm64, win-x64, win-arm64 | Microsoft-Developed  |
| [DCGMI](https://microsoft.github.io/VirtualClient/docs/workloads/dcgmi/dcgmi.md)| GPU Qualification| linux-x64 | [Apache-2.0](https://github.com/NVIDIA/DCGM/blob/master/LICENSE)
| [DeathStarBench](https://microsoft.github.io/VirtualClient/docs/workloads/deathstarbench/deathstarbench.md) | Docker Swarm/Container Microservices | linux-x64, linux-arm64, win-x64, win-arm64 | [Apache-2.0](https://github.com/delimitrou/DeathStarBench/blob/master/LICENSE)  |
| [DiskSpd](https://microsoft.github.io/VirtualClient/docs/workloads/diskspd/diskspd.md) | Disk I/O Performance | win-x64, win-arm64 | [MIT](https://github.com/microsoft/diskspd/blob/master/LICENSE)  |
| [Flexible IO Tester (FIO)](https://microsoft.github.io/VirtualClient/docs/workloads/fio/fio.md) | Disk I/O Performance | linux-x64, linux-arm64, win-x64 | [GPL-2.0](https://github.com/axboe/fio/blob/master/COPYING)  |
| [GeekBench5](https://microsoft.github.io/VirtualClient/docs/workloads/geekbench/) | CPU Performance | linux-x64, win-x64, win-arm64 | [End User License Required](https://www.primatelabs.com/legal/eula-v5.html) |
| [Graph500](https://microsoft.github.io/VirtualClient/docs/workloads/graph500/graph500.md) | 3D Simulation | linux-x64, linux-arm64 | [Custom](https://github.com/graph500/graph500/blob/newreference/license.txt)  |
| [Gzip](https://microsoft.github.io/VirtualClient/docs/workloads/compression/gzip.md) | Compression | linux-x64, linux-arm64 | [GPL](https://www.gnu.org/software/gzip/)  |
| [HPCG](https://microsoft.github.io/VirtualClient/docs/workloads/hpcg/hpcg.md) | High Performance Compute (HPC) | linux-x64, linux-arm64 | [Custom](https://github.com/hpcg-benchmark/hpcg/blob/master/COPYING)  |
| [HPLinpack](https://microsoft.github.io/VirtualClient/docs/workloads/hplinpack/hplinpack.md) | Linear Equations | linux-x64, linux-arm64| [IBM](https://netlib.org/benchmark/hpl/IBM_LICENSE.TXT)  |
| [LAPACK](https://microsoft.github.io/VirtualClient/docs/workloads/lapack/lapack.md) | Linear Equations | linux-x64, linux-arm64, win-x64, win-arm64 | [Custom](https://github.com/Reference-LAPACK/lapack/blob/master/LICENSE)  |
| [Latte](https://microsoft.github.io/VirtualClient/docs/workloads/network-suite/network-suite.md) | Network Latencies | win-x64, win-arm64 | [MIT](https://github.com/microsoft/latte/blob/main/LICENSE)  |
| [LMbench](https://microsoft.github.io/VirtualClient/docs/workloads/lmbench/lmbench.md) | Memory Performance | linux-x64, linux-arm64 | [GPL-2.0](https://github.com/intel/lmbench/blob/master/COPYING)  |
| [LZBench](https://microsoft.github.io/VirtualClient/docs/workloads/compression/lzbench) | Compression/Streaming | linux-x64, linux-arm64, win-x64, win-arm64 | [None](https://github.com/inikep/lzbench)  |
| [Memcached](https://microsoft.github.io/VirtualClient/docs/workloads/memcached/memcached.md) | In-Memory Data Cache | linux-x64, linux-arm64 | [BSD-3 (Memcached)](https://github.com/memcached/memcached/blob/master/LICENSE)<br/>[GPL-2.0 (Memtier)](https://github.com/RedisLabs/memtier_benchmark/blob/master/COPYING)  |
| [MLPerf](https://microsoft.github.io/VirtualClient/docs/workloads/mlperf/mlperf.md) | Machine Learning | linux-x64 | [Custom](https://github.com/mlcommons/training/blob/master/LICENSE.md)  |
| [NAS Parallel](https://microsoft.github.io/VirtualClient/docs/workloads/nasparallel/nasparallel.md) | High Performance Compute (HPC) | linux-x64, linux-arm64 | [NASA-1.3](https://opensource.org/licenses/nasa1.3.php)  |
| [Network ICMP Ping](https://microsoft.github.io/VirtualClient/docs/workloads/network-ping/network-ping.md) | Network Latencies | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT](https://github.com/microsoft/VirtualClient/blob/main/LICENSE)  |
| [NTttcp](https://microsoft.github.io/VirtualClient/docs/workloads/network-suite/network-suite.md) | Network Bandwidth | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT](https://github.com/microsoft/ntttcp/blob/main/LICENSE)  |
| [OpenFOAM](https://microsoft.github.io/VirtualClient/docs/workloads/openfoam/openfoam.md) | Computational Fluid Dynamics | linux-x64, linux-arm64 | [Custom](https://github.com/OpenFOAM/OpenFOAM-10/blob/master/COPYING)  |
| [OpenSSL](https://microsoft.github.io/VirtualClient/docs/workloads/openssl/openssl.md) | Cryptography/Encryption | linux-x64, linux-arm64, win-x64 | [Apache-2.0](https://github.com/openssl/openssl/blob/master/LICENSE.txt)  |
| [Pbzip2](https://microsoft.github.io/VirtualClient/docs/workloads/compression/pbzip2.md) | Compression | linux-x64, linux-arm64 | [BSD](http://compression.great-site.net/pbzip2/)  |
| [PostgreSQL](https://microsoft.github.io/VirtualClient/docs/workloads/postgresql//postgresql.md) | Relational Database Performance | linux-x64, linux-arm64, win-x64 | [PostgreSQL](https://www.postgresql.org/about/licence/)
| [Prime95](https://microsoft.github.io/VirtualClient/docs/workloads/prime95/prime95.md) | CPU Stress | linux-x64 | [Custom](https://www.mersenne.org/legal/)  |
| [Redis](https://microsoft.github.io/VirtualClient/docs/workloads/redis/redis.md) | In-Memory Data Cache | linux-x64, linux-arm64 | [BSD-3 (Redis)](https://github.com/redis/redis/blob/unstable/COPYING)<br/>[GPL-2.0 (Memtier)](https://github.com/RedisLabs/memtier_benchmark/blob/master/COPYING)  |
| [SockPerf](https://microsoft.github.io/VirtualClient/docs/workloads/network-suite/network-suite.md) | Network Latencies | linux-x64, linux-arm64 | [Custom](https://github.com/Mellanox/sockperf/blob/sockperf_v2/copying)  |
| [SPEC CPU 2017, SPECrate Integer](https://microsoft.github.io/VirtualClient/docs/workloads/speccpu/) | Precision CPU, Integer Calculations | linux-x64, linux-arm64 | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html) |
| [SPEC CPU 2017, SPECrate Floating Point](https://microsoft.github.io/VirtualClient/docs/workloads/speccpu/) | Precision CPU, Floating-point Calculations | linux-x64, linux-arm64 | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html) |
| [SPEC CPU 2017, SPECspeed Integer](https://microsoft.github.io/VirtualClient/docs/workloads/speccpu/) | Precision CPU, Integer Calculations | linux-x64, linux-arm64 | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html) |
| [SPEC CPU 2017, SPECspeed Floating Point](https://microsoft.github.io/VirtualClient/docs/workloads/speccpu/) | Precision CPU, Floating-point Calculations | linux-x64, linux-arm64 | [End User License Required](https://www.spec.org/cpu2017/Docs/licenses.html) |
| [SPEC JBB 2015, SPECjbb](https://microsoft.github.io/VirtualClient/docs/workloads/specjbb/) | Java Server | linux-x64, linux-arm64, win-x64, win-arm64 | [End User License Required](https://www.spec.org/jbb2015/) |
| [SPEC JVM 2008, SPECjvm](https://microsoft.github.io/VirtualClient/docs/workloads/specjvm/specjvm.md) | Java Runtime Performance | linux-x64, linux-arm64, win-x64, win-arm64 | [SPEC](https://www.spec.org/spec/docs/SPEC_General_License.pdf)  |
| [SPEC Power 2008, SPECpower](https://microsoft.github.io/VirtualClient/docs/workloads/specpower/) | High precision, steady-state CPU usage | linux-x64, linux-arm64, win-x64, win-arm64 | [End User License Required](https://www.spec.org/power_ssj2008/) |
| [Stressapptest](https://microsoft.github.io/VirtualClient/docs/workloads/stressapptest/stressapptest.md) | Fault Tolerance | linux-x64, linux-arm64 | [Apache-2.0](https://github.com/stressapptest/stressapptest/blob/master/NOTICE)  |
| [Stress-ng](https://microsoft.github.io/VirtualClient/docs/workloads/stress-ng/stress-ng.md) | Fault Tolerance | linux-x64, linux-arm64 | [GPL-2.0](https://github.com/ColinIanKing/stress-ng/blob/master/COPYING)  |
| [SuperBench](https://microsoft.github.io/VirtualClient/docs/workloads/superbenchmark/superbenchmark.md) | Machine Learning | linux-x64 | [MIT](https://github.com/microsoft/superbenchmark/blob/main/LICENSE)  |
| [Sysbench OLTP w/MySQL](https://microsoft.github.io/VirtualClient/docs/workloads/sysbench-oltp/sysbench-oltp.md) | Relational Database Performance | linux-x64, linux-arm64 | [GPL-2.0 (Sysbench)](https://github.com/akopytov/sysbench/blob/master/COPYING)<br/>[GPL-2.0 (MySQL)](https://www.mysql.com/about/legal/licensing/oem/) |

## Supported System Monitoring Facilities
The platform supports capturing information from the system in the background while workloads are running. The following list of monitoring facilities are available in the Virtual Client.

:::info
Certain monitoring facilities are only available on specific hardware because they expect specific tools/hardware on the system (e.g. ipmiutil, nvidia monitors).
:::

| **Monitor** | **Specialization** | **Supported Platforms/Architectures** | **License(s)**  | 
|-------------|--------------------|---------------------------------------|-----------------|
| [Nvidia SMI](https://microsoft.github.io/VirtualClient/docs/monitors/0300-nvidia-smi/)                     | Nvidia GPUs          | linux-x64, linux-arm64 | |
| [Performance Counters](https://microsoft.github.io/VirtualClient/docs/monitors/0100-perf-counter-metrics/) | Performance Counters | linux-x64, linux-arm64, win-x64, win-arm64 | |

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