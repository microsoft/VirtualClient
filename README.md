# <img src="./website/static/img/vc-logo.svg" width="50"> Virtual Client


[![Pull Request Build](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pull-request.yml)
[![Document Build](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml/badge.svg?branch=main)](https://github.com/microsoft/VirtualClient/actions/workflows/deploy-doc.yml)
[![Document Deployment](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment/badge.svg)](https://github.com/microsoft/VirtualClient/actions/workflows/pages/pages-build-deployment)
[![NuGet Release Status](https://msazure.visualstudio.com/One/_apis/build/status/OneBranch/CRC-AIR-Workloads/microsoft.VirtualClient?branchName=main)](https://msazure.visualstudio.com/One/_build/latest?definitionId=297462&branchName=main)

---

Virtual Client is a unified workload and system monitoring platform for running customer-representative scenarios on virtual machines or physical hosts/blades in the Azure Cloud. 
The platform supports a wide range of different industry standard/benchmark workloads used to measuring various aspects of the system under test (e.g. CPU, I/O, network performance, power consumption). It has been an inner-source project in Microsoft for two years and now it is open sourced on GitHub.
The platform additionally provides the ability to capture important performance and reliability measurements from the underlying system. The platform supports different environments including guest/VM systems, host/blade systems and data center/DC lab systems. The platform additionally supports both x64 and ARM64 compute architectures.

* [Platform Features](https://microsoft.github.io/VirtualClient/docs/overview/features/)
* [Platform Design](https://microsoft.github.io/VirtualClient/docs/overview/design/)
* [Developer Guide](https://microsoft.github.io/VirtualClient/docs/developing/develop-guide/)
* [Additional Usage Examples](https://microsoft.github.io/VirtualClient/docs/category/usage-scenarios/)  

## Team Contacts
* [virtualclient@microsoft.com](mailto:virtualclient@microsoft.com)

## Installation

#### *NuGet package*

- VirtualClient NuGet Package is at https://www.nuget.org/packages/VirtualClient
```powershell
PM> NuGet\Install-Package VirtualClient -Version 0.0.4
```
- You could optionally download directly from NuGet https://www.nuget.org/api/v2/package/VirtualClient/0.0.4
- VC executable could be find in those paths
```treeview
VirtualClient/
├── content/
|   ├── linux-arm64
|   |   └── VirtualClient
|   ├── linux-x64
|   |   └── VirtualClient
|   ├── win-arm64
|   |   └── VirtualClient.exe
|   └── win-x64
|       └── VirtualClient.exe
└── etc.
```

#### *Build yourself*
- You need to [install .Net SDK 6.0.X](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- Use build script at the root of the repo build.cmd
```bash
build.cmd
```
- You will find VC binary in corresponding arch/runtimes folder. 
```bash
VirtualClient\out\bin\Debug\ARM64\VirtualClient.Main\net6.0\linux-arm64\publish\VirtualClient
VirtualClient\out\bin\Debug\ARM64\VirtualClient.Main\net6.0\win-arm64\publish\VirtualClient.exe
VirtualClient\out\bin\Debug\x64\VirtualClient.Main\net6.0\linux-x64\publish\VirtualClient
VirtualClient\out\bin\Debug\x64\VirtualClient.Main\net6.0\win-x64\publish\VirtualClient.exe
```
- VirtualClient is a self-contained .NET app. When you use VC, you need to copy over the entire `/publish/` folder

---

## [Getting Started](https://microsoft.github.io/VirtualClient/docs/guides/getting-started/)

You will follow the [**Tutorial**](https://microsoft.github.io/VirtualClient/docs/guides/getting-started/) to benchmark your system with a quick workload: OpenSSL Speed - SHA256.

---
## [Supported Workloads](https://microsoft.github.io/VirtualClient/docs/overview/#supported-benchmark-workloads)

The following list of workloads are used by Virtual Client profiles to exercise the system components in a consistent way required to measure performance baselines and differences. 

:::caution Comply to licenses you are using
VirtualClient handles the installation and execution of various tools. Individual license files are not prompted for each workload. By using VirtualClient, users accept the license of each of the benchmarks individually, comply to the terms for the tool you are using, and take responsibility for using them.
:::

| **Benchmark** | **Specialization** | **Supported Platforms/Architectures** | **License(s)**  |
|---|---|---|---|
| [7zip](https://microsoft.github.io/VirtualClient/docs/workloads/compression/7zip) | 7zip compression | linux-x64, linux-arm64 | [GNU LGPL](https://www.7-zip.org/faq.html)  |
| [AspNetBench](https://microsoft.github.io/VirtualClient/docs/workloads/aspnetbench) | ASP.NET server | linux-x64, linux-arm64, win-x64, win-arm64 | [**ASP.NET**:MIT](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt)<br/>[**Bombardier**:MIT](https://github.com/codesenberg/bombardier/blob/master/LICENSE)  |
| [CoreMark](https://microsoft.github.io/VirtualClient/docs/workloads/coremark) | Generic CPU | linux-x64,linux-arm64 | [Apache+Custom](https://github.com/eembc/coremark/blob/main/LICENSE.md)  |
| [CPS](https://microsoft.github.io/VirtualClient/docs/workloads/network) | Network RoundTripTime | linux-x64, linux-arm64, win-x64, win-arm64 | MSFT developed  |
| [DeathStarBench](https://microsoft.github.io/VirtualClient/docs/workloads/deathstarbench) | Swarm container microservices | linux-x64, linux-arm64, win-x64, win-arm64 | [Apache-2.0](https://github.com/delimitrou/DeathStarBench/blob/master/LICENSE)  |
| [DiskSpd](https://microsoft.github.io/VirtualClient/docs/workloads/diskspd) | Disk Stress | win-x64, win-arm64 | [MIT](https://github.com/microsoft/diskspd/blob/master/LICENSE)  |
| [Flexible IO Tester (FIO)](https://microsoft.github.io/VirtualClient/docs/workloads/fio) | Disk IO Performance | linux-x64, linux-arm64, win-x64 | [GPL-2.0](https://github.com/axboe/fio/blob/master/COPYING)  |
| [Graph500](https://microsoft.github.io/VirtualClient/docs/workloads/graph500) | 3D Simulation | linux-x64, linux-arm64 | [Custom](https://github.com/graph500/graph500/blob/newreference/license.txt)  |
| [gzip](https://microsoft.github.io/VirtualClient/docs/workloads/compression/gzip) | pbzip2 compression | linux-x64, linux-arm64 | [GPL](https://www.gnu.org/software/gzip/)  |
| [HPCG](https://microsoft.github.io/VirtualClient/docs/workloads/hpcg) | High Performance Compute (HPC) | linux-x64, linux-arm64 | [Custom](https://github.com/hpcg-benchmark/hpcg/blob/master/COPYING)  |
| [LAPACK](https://microsoft.github.io/VirtualClient/docs/workloads/lapack) | Linear Equations | linux-x64, linux-arm64, win-x64, win-arm64 | [Custom](https://github.com/Reference-LAPACK/lapack/blob/master/LICENSE)  |
| [Latte](https://microsoft.github.io/VirtualClient/docs/workloads/network) | Network latency | win-x64, win-arm64 | [MIT](https://github.com/microsoft/latte/blob/main/LICENSE)  |
| [LMbench](https://microsoft.github.io/VirtualClient/docs/workloads/lmbench) | Generic Memory | linux-x64, linux-arm64 | [GPL-2.0](https://github.com/intel/lmbench/blob/master/COPYING)  |
| [LZBench](https://github.com/inikep/lzbench ) | Compression/Streaming | linux-x64, linux-arm64, win-x64, win-arm64 | [None](https://github.com/inikep/lzbench)  |
| [Memcached](https://microsoft.github.io/VirtualClient/docs/workloads/memcached) | Memcached Performance | linux-x64, linux-arm64 | [**memcached**:BSD-3](https://github.com/memcached/memcached/blob/master/LICENSE)<br/>[**Memtier**:GPL-2.0](https://github.com/RedisLabs/memtier_benchmark/blob/master/COPYING)  |
| [MLPerf](https://microsoft.github.io/VirtualClient/docs/workloads/mlperf) | Machine learning | linux-x64 | [Custom](https://github.com/mlcommons/training/blob/master/LICENSE.md)  |
| [NAS Parallel](https://microsoft.github.io/VirtualClient/docs/workloads/nasparallel) | High Performance Compute (HPC) | linux-x64, linux-arm64 | [NASA-1.3](https://opensource.org/licenses/nasa1.3.php)  |
| [Network ICMP Ping](https://microsoft.github.io/VirtualClient/docs/workloads/network-ping) | Simple Network Ping | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT](https://github.com/microsoft/VirtualClient/blob/main/LICENSE)  |
| [NTttcp](https://microsoft.github.io/VirtualClient/docs/workloads/network) | Network bandwidth | linux-x64, linux-arm64, win-x64, win-arm64 | [MIT](https://github.com/microsoft/ntttcp/blob/main/LICENSE)  |
| [OpenFOAM](https://microsoft.github.io/VirtualClient/docs/workloads/openfoam) | Fluidmechanics | linux-x64, linux-arm64 | [Custom](https://github.com/OpenFOAM/OpenFOAM-10/blob/master/COPYING)  |
| [OpenSSL](https://microsoft.github.io/VirtualClient/docs/workloads/openssl) | Cryptography | linux-x64, linux-arm64, win-x64 | [Apache-2.0](https://github.com/openssl/openssl/blob/master/LICENSE.txt)  |
| [pbzip2](https://microsoft.github.io/VirtualClient/docs/workloads/compression) | pbzip2 compression | linux-x64, linux-arm64 | [BSD](http://compression.great-site.net/pbzip2/)  |
| [Prime95](https://microsoft.github.io/VirtualClient/docs/workloads/prime95) | Prime number search | linux-x64 | [Custom](https://www.mersenne.org/legal/)  |
| [Redis](https://microsoft.github.io/VirtualClient/docs/workloads/redis) | Redis Performance | linux-x64, linux-arm64 | [**Redis**:BSD-3](https://github.com/redis/redis/blob/unstable/COPYING)<br/>[**Memtier**:GPL-2.0](https://github.com/RedisLabs/memtier_benchmark/blob/master/COPYING)  |
| [SockPerf](https://microsoft.github.io/VirtualClient/docs/workloads/network) | Network latency | linux-x64, linux-arm64 | [Custom](https://github.com/Mellanox/sockperf/blob/sockperf_v2/copying)  |
| [SPECjvm](https://microsoft.github.io/VirtualClient/docs/workloads/specjvm) | Java Runtime | linux-x64, linux-arm64, win-x64, win-arm64 | [SPEC](https://www.spec.org/spec/docs/SPEC_General_License.pdf)  |
| [stress-ng](https://microsoft.github.io/VirtualClient/docs/workloads/stress-ng) | Fault Tolerance | linux-x64, linux-arm64 | [GPL-2.0](https://github.com/ColinIanKing/stress-ng/blob/master/COPYING)  |
| [SuperBench](https://microsoft.github.io/VirtualClient/docs/workloads/superbenchmark) | Machine learning | linux-x64 | [MIT](https://github.com/microsoft/superbenchmark/blob/main/LICENSE)  |


## Contributing

We welcome your contribution, and there are many ways to contribute to VirtualClient:

* [Just say Hi](https://github.com/microsoft/VirtualClient/discussions/categories/show-and-tell). It inspires us to know that there are fellow performance enthusiatics out there and VirtualClient made your work a little easier.
* [Feature Requests](https://github.com/microsoft/VirtualClient/issues/new/choose). It helps us to know what benchmarks people are using.
* [Submit bugs](https://github.com/microsoft/VirtualClient/issues/new/choose). We apologize for the bug and we will investigate it ASAP.
* Review [source code changes](https://github.com/microsoft/VirtualClient/pulls). You likely know more about one workload than us. Tell us your insights.
* Review the [documentation](https://github.com/microsoft/VirtualClient/tree/main/website/docs) and make pull requests for anything from typos to new content.
* We welcome you to directly work in the codebase. Please take a look at our [CONTRIBUTING.md](./CONTRIBUTING.md). [Start here](https://microsoft.github.io/VirtualClient/docs/category/developing/) and contact us if you have any questions.

Thank you and we look forward to your contribution.

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


## Telemetry Notice
Data Collection. 

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

#### VirtualClient does not collect your data by default
VirtualClient does not collect any of your benchmark data and upload to Microsoft. When benchmarking at scale, and leveraging VC's telemetry capabilities, users need to explicitly provide a connection string, that points to a user-owned Azure Data Explorer cluster. VirtualClient does host a Azure storage account to host the benchmark binaries or source. The only information VirtualClient team could infer from usage, is the download traces from Azure storage account.

#### About benchmark examples in source
VirtualClient has example benchmark outputs in source, for unit-testing purpose, to make sure our parsers work correctly.
Those runs might or might not be ran on Azure VMs. The results have also been randomly scrubbed. These examples do not represent Azure VM performance. They are in the source purely for unit testing purposes.


## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
