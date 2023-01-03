# DiskSpd
DiskSpd a Microsoft toolset designed to simulate different I/O workload patterns. It allows us to quickly define and run workloads, and reports a variety 
of metrics about each run. This allows us to compare results under different conditions, such as different hardware and firmware configurations, with 
workloads that reflect production workloads. 

The workload enables the ability to test various I/O scenarios that represent real-life usage patterns on computer systems. For example, it
enables testing sequential read and write operations as well as random read and write operations. It also enables the ability to test single-threaded
vs. multi-threaded I/O operations as well as the ability to control I/O queue depths and whether hardware caches should be used.

* [DiskSpd Documentation](https://github.com/microsoft/diskspd/blob/master/README.md)

## What is Being Measured?
The DiskSpd workload measures disk I/O performance focusing on throughput, bandwidth and latencies.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the DiskSpd workload. This set of metrics 
will be captured for each one of the distinct scenarios that are part of the profile (e.g. Random Write 4k block size, Random Read 4k block size). 
It is a lot of data!!

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| avg. latency | 98.82 | 2642.103 | 595.8003536931818 | milliseconds |
| iops stdev | 1.89 | 17063.0 | 1804.595497159091 | operations/sec |
| latency stdev | 10.33 | 3735.78 | 425.85086221590907 | milliseconds |
| read IO operations | 932417.0 | 24483796.0 | 19563267.372093023 | operations |
| read avg. latency | 98.822 | 2589.675 | 591.2786279069768 | milliseconds |
| read iops | 3108.04 | 81608.97 | 65209.23595930233 | operations/sec |
| read iops stdev | 2.96 | 5394.54 | 1697.3215697674419 | operations/sec |
| read latency stdev | 12.35 | 3603.306 | 607.8916889534884 | milliseconds |
| read throughput | 312.29 | 3113.01 | 1243.1043895348835 | mebibytes/sec |
| read total bytes | 98238738432.0 | 979291340800.0 | 391056506701.3953 | bytes |
| total IO operations | 929378.0 | 24483796.0 | 19509977.224431818 | operations |
| total bytes | 89273675776.0 | 979363692544.0 | 392139310382.5455 | bytes |
| total iops | 3097.8 | 81608.97 | 65031.59069602272 | operations/sec |
| total throughput | 283.78 | 3113.22 | 1246.5455397727274 | mebibytes/sec |
| write IO operations | 929378.0 | 24480568.0 | 19459055.527777777 | operations |
| write avg. latency | 98.82 | 2642.103 | 600.1211138888888 | milliseconds |
| write iops | 3097.8 | 81601.41 | 64861.84077777777 | operations/sec |
| write iops stdev | 1.89 | 17063.0 | 1907.1016944444444 | operations/sec |
| write latency stdev | 10.33 | 3735.78 | 251.90073888888888 | milliseconds |
| write throughput | 283.78 | 3113.22 | 1249.83375 | mebibytes/sec |
| write total bytes | 89273675776.0 | 979363692544.0 | 393173989455.6445 | bytes |

## Resources
* [Azure Stack: DiskSpd Overview](https://docs.microsoft.com/en-us/azure-stack/hci/manage/diskspd-overview)  
* [DiskSpd Command Line Parameters](https://github.com/Microsoft/diskspd/wiki/Command-line-and-parameters)  
* [Server Storage I/O Benchmark Tools: Microsoft Diskspd (Part I)](https://storageioblog.com/server-storage-io-benchmarking-tools-microsoft-diskspd-part/)  
* [Server Storage I/O Benchmark Tools: Microsoft Diskspd (Part II)](https://storageioblog.com/microsoft-diskspd-part-ii-server-storage-io-benchmark-tools/)