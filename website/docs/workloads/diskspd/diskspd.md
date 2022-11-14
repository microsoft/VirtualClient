# DiskSpd
DiskSpd a Microsoft toolset designed to simulate different I/O workload patterns. It allows us to quickly define and run workloads, and reports a variety 
of metrics about each run. This allows us to compare results under different conditions, such as different hardware and firmware configurations, with 
workloads that reflect production workloads. 

The workload enables the ability to test various I/O scenarios that represent real-life usage patterns on computer systems. For example, it
enables testing sequential read and write operations as well as random read and write operations. It also enables the ability to test single-threaded
vs. multi-threaded I/O operations as well as the ability to control I/O queue depths and whether hardware caches should be used.

* [DiskSpd Documentation](https://github.com/microsoft/diskspd/blob/master/README.md)

-----------------------------------------------------------------------

### What is Being Tested?
The following metrics are captured from the results of the DiskSpd workload.

* Disk Operation Bandwidth
* Disk I/O Operations/sec
* Disk Operation Latencies

  | Name                                  | Description |
  |---------------------------------------|-------------|
| total bytes                           | Total number of bytes processed for all operations (read and write) during the DiskSpd test.               |
| total IO operations                   | Total number of disk I/O operations processed for all operations (read and write) during the DiskSpd test. |
| total throughput                      | The total throughput (in Mebibytes per second) for all operations (read and write) during the DiskSpd test.|
| total iops                            | The disk I/O operations per second (IOPS) during the DiskSpd test (read and write).        |
| avg. latency                          | The average latency for all disk operations (read and write) during the DiskSpd test.      |
| iops stdev                            | The standard deviation for I/O operations per second measurements during the DiskSpd test. |
| latency stdev                         | The standard deviation for disk operation latency during the DiskSpd test.                 |
| read total bytes                      | The total number of bytes read during the DiskSpd test.                        |
| read IO operations                    | The total number of disk read I/O operations process during the DiskSpd test.  |
| read throughput                       | The total read throughput (in Mebibytes per second) during the DiskSpd test.   |
| read iops                             | The disk read I/O operations per second (IOPS) during the DiskSpd test.        |
| read avg. latency                     | The average disk read latency during the DiskSpd test.                         |
| read iops stdev                       | The standard deviation for disk read I/O operations per second measurements during the DiskSpd test. |
| read latency stdev                    | The standard deviation for disk read operation latency during the DiskSpd test.                      |
| write total bytes                     | The total number of bytes written during the DiskSpd test.                     |
| write IO operations                   | The total number of disk write I/O operations process during the DiskSpd test. |
| write throughput                      | The total write throughput (in Mebibytes per second) during the DiskSpd test.  |
| write iops                            | The disk write I/O operations per second (IOPS) during the DiskSpd test.       |
| write avg. latency                    | The average disk write latency during the DiskSpd test.                        |
| write iops stdev                      | The standard deviation for disk write I/O operations per second measurements during the DiskSpd test. |
| write latency stdev                   | The standard deviation for disk write operation latency during the DiskSpd test.                      |
| read latency/operation(P50)           | The 50th percentile latency for disk read operations during the DiskSpd test                  |
| write latency/operation(P50)          | The 50th percentile latency for disk write operations during the DiskSpd test                 |
| total latency/operation(P50)          | The 50th percentile latency for all disk operations (read and write) during the DiskSpd test  |
| read latency/operation(P75)           | The 75th percentile latency for disk read operations during the DiskSpd test                  |
| write latency/operation(P75)          | The 75th percentile latency for disk write operations during the DiskSpd test                 |
| total latency/operation(P75)          | The 75th percentile latency for all disk operations (read and write) during the DiskSpd test  |
| read latency/operation(P90)           | The 90th percentile latency for disk read operations during the DiskSpd test                  |
| write latency/operation(P90)          | The 90th percentile latency for disk write operations during the DiskSpd test                 |
| total latency/operation(P90)          | The 90th percentile latency for all disk operations (read and write) during the DiskSpd test  |
| read latency/operation(P95)           | The 95th percentile latency for disk read operations during the DiskSpd test                  |
| write latency/operation(P95)          | The 95th percentile latency for disk write operations during the DiskSpd test                 |
| total latency/operation(P95)          | The 95th percentile latency for all disk operations (read and write) during the DiskSpd test  |
| read latency/operation(P99)           | The 99th percentile latency for disk read operations during the DiskSpd test                  |
| write latency/operation(P99)          | The 99th percentile latency for disk write operations during the DiskSpd test                 |
| total latency/operation(P99)          | The 99th percentile latency for all disk operations (read and write) during the DiskSpd test  |

-----------------------------------------------------------------------

### Supported Platforms
* Windows x64
* Windows arm64

-----------------------------------------------------------------------

### Command Line Examples
```
// Random Write
// 4GB test file, 4K block size, 1 thread, I/O depth = 1, 100% write, test runtime = 480 seconds, 15 second warmup
DiskSpd.exe -c4G -b4K -r4K -t1 -o1 -w100 -d480 -Suw -W15 -D -L -Rtext testfile.dat

// Random Write
// 250MB test file, 64K block size, 32 threads, I/O depth = 16, 100% write, test runtime = 480 seconds, 15 second warmup
DiskSpd.exe -c250M -b64K -r64K -t32 -o16 -w100 -d480 -Suw -W15 -D -L -Rtext testfile.dat

// Random Read
// 4GB test file, 4K block size, 1 thread, I/O depth = 1, 100% read, test runtime = 480 seconds, 15 second warmup
DiskSpd.exe -c4G -b4K -r4K -t1 -o1 -w0 -d480 -Suw -W15 -D -L -Rtext

// Random Read/Write: 4GB test file, 4K block size, 1 thread, I/O depth = 1, 30% write/70% read, test runtime = 480 seconds, 15 second warmup
DiskSpd.exe -c4G -b4K -r4K -t1 -o1 -w30 -d480 -Suw -W15 -D -L -Rtext testfile.dat

// Sequential Read/Write: 4GB test file, 4K block size, 1 thread, I/O depth = 1, 30% write/70% read, test runtime = 480 seconds, 15 second warmup
DiskSpd.exe -c4G -b4K -si4K -t1 -o1 -w30 -d480 -Suw -W15 -D -L -Rtext testfile.dat

```

-----------------------------------------------------------------------

### Resources
* [Azure Stack: DiskSpd Overview](https://docs.microsoft.com/en-us/azure-stack/hci/manage/diskspd-overview)  
* [DiskSpd Command Line Parameters](https://github.com/Microsoft/diskspd/wiki/Command-line-and-parameters)  
* [Server Storage I/O Benchmark Tools: Microsoft Diskspd (Part I)](https://storageioblog.com/server-storage-io-benchmarking-tools-microsoft-diskspd-part/)  
* [Server Storage I/O Benchmark Tools: Microsoft Diskspd (Part II)](https://storageioblog.com/microsoft-diskspd-part-ii-server-storage-io-benchmark-tools/)