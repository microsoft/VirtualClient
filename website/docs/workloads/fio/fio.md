---
id: fio
---

# FIO (Flexible IO Tester) Workload
FIO, or flexible I/O, is a third party tool that simulates a given I/O workload. It allows us to quickly define and run workloads, and reports a variety 
of metrics about each run. This allows us to compare results under different conditions, such as different hardware and firmware configurations, with 
workloads that reflect production workloads. 

The workload enables the ability to test various I/O scenarios that represent real-life usage patterns on computer systems. For example, it
enables testing sequential read and write operations as well as random read and write operations. It also enables the ability to test single-threaded
vs. multi-threaded I/O operations as well as the ability to control I/O queue depths and whether hardware caches should be used.

* [FIO Documentation](https://fio.readthedocs.io/en/latest/index.html)

-----------------------------------------------------------------------

### What is Being Tested?
FIO outputs several hundred metrics in a single run of a workload. This is an unrealistic amount of data to store, so we decided to focus on a set of metrics that measure throughput, IOPs, and latency. We can use these values to compare the performance between different versions of hardware and firmware, as well as track overall performance over time.

| Name                                  | Description                                                            |
|---------------------------------------|------------------------------------------------------------------------|
| Bandwidth                             | Average bandwidth rate measured in  KiB/sec                            |
| IOPS                                  | IOs/sec                                                                |
| Total IOs                             | Total number of IO performed                                           |
| Completion Latency Mean               | Mean time from submission to completion of IO request                  |
| Data Corruption Count                 | Number of data verification errors encountered                         |

-----------------------------------------------------------------------

### Supported Platforms

* Linux x64
* Linux arm64
* Windows x64
* Windows arm64


-----------------------------------------------------------------------

### Resources

* [Linux Reviews - How to test disk I/O performance](https://linuxreviews.org/HOWTO_Test_Disk_I/O_Performance)
* [How fast are your disks?](https://arstechnica.com/gadgets/2020/02/how-fast-are-your-disks-find-out-the-open-source-way-with-fio/)  
* [I/O Performance with FIO (VMWare)](https://docs.vmware.com/en/vSphere/6.7/solutions/vSphere-6.7.e3be11fbbf5809350802f6883cda9d28/GUID-A23C6CC9C2D014B7EBB03F92A6141093.html)  
* [Benchmarking persistent disk performance (Google)](https://cloud.google.com/compute/docs/disks/benchmarking-pd-performance)