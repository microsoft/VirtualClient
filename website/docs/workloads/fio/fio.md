# Flexible IO Tester (FIO)
FIO, or flexible I/O, is a third party tool that simulates a given I/O workload. It allows us to quickly define and run workloads, and reports a variety 
of metrics about each run. This allows us to compare results under different conditions, such as different hardware and firmware configurations, with 
workloads that reflect production workloads. 

The workload enables the ability to test various I/O scenarios that represent real-life usage patterns on computer systems. For example, it
enables testing sequential read and write operations as well as random read and write operations. It also enables the ability to test single-threaded
vs. multi-threaded I/O operations as well as the ability to control I/O queue depths and whether hardware caches should be used.

* [FIO Documentation](https://fio.readthedocs.io/en/latest/index.html)

## What is Being Measured?
The FIO workload measures disk I/O performance focusing on throughput, bandwidth, latencies and data integrity/reliability.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the FIO workload. This set of metrics 
will be captured for each one of the distinct scenarios that are part of the profile (e.g. Random Write 4k block size, Random Read 4k block size). 
It is a lot of data!!

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit | Description |
|-------------|---------------------|---------------------|---------------------|------|-------------|
| h000000_002 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency under 2 microseconds. | 
| h000000_004 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 2 and 4 microseconds.| 
| h000000_010 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 4 and 10 microseconds. | 
| h000000_020 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 10 and 20 microseconds. | 
| h000000_050 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 20 and 50 microseconds. | 
| h000000_100 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 50 and 100 microseconds. | 
| h000000_250 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 100 and 250 microseconds. | 
| h000000_500 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 250 and 500 microseconds. | 
| h000000_750 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 500 and 750 microseconds. | 
| h000001_000 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 750 and 1000 microseconds. | 
| h000002_000 | 0.0 | 0.0 | 0.0 |  | Histogram. The percentage of I/O operations with a latency between 1 and 2 milliseconds. | 
| h000004_000 | 0.0 | 0.01 | 0.0022222222222222224 |  | Histogram. The percentage of I/O operations with a latency between 2 and 4 milliseconds. | 
| h000010_000 | 0.0 | 0.01 | 0.0044444444444444448 |  | Histogram. The percentage of I/O operations with a latency between 4 and 10 milliseconds. | 
| h000020_000 | 0.0 | 0.086733 | 0.02304311111111111 |  | Histogram. The percentage of I/O operations with a latency between 10 and 20 milliseconds. | 
| h000050_000 | 0.0 | 0.153191 | 0.09191677777777778 |  | Histogram. The percentage of I/O operations with a latency between 20 and 50 milliseconds. | 
| h000100_000 | 0.0 | 8.380734 | 5.972690111111111 |  | Histogram. The percentage of I/O operations with a latency between 50 and 100 milliseconds. | 
| h000250_000 | 0.0 | 94.70487 | 82.81543544444445 |  | Histogram. The percentage of I/O operations with a latency between 100 and 250 milliseconds. | 
| h000500_000 | 0.01 | 0.052439 | 0.025846333333333337 |  | Histogram. The percentage of I/O operations with a latency between 250 and 500 milliseconds. | 
| h000750_000 | 0.0 | 0.017135 | 0.001903888888888889 |  | Histogram. The percentage of I/O operations with a latency between 500 and 750 milliseconds. | 
| h001000_000 | 0.0 | 0.178207 | 0.019800777777777779 |  | Histogram. The percentage of I/O operations with a latency between 750 and 1000 milliseconds. | 
| h002000_000 | 0.0 | 0.380404 | 0.04337822222222223 |  | Histogram. The percentage of I/O operations with a latency between 1000 and 2000 milliseconds. | 
| hgt002000_000 | 0.0 | 100.25703 | 11.145678444444444 |  | Histogram. The percentage of I/O operations with a latency greater than 2000 milliseconds. | 
| read_bandwidth | 15702.0 | 200667.0 | 58223.333333333336 | kilobytes/sec | The average I/O bandwidth for read operations. | 
| read_bandwidth_max | 18084.0 | 462848.0 | 92574.55555555556 | kilobytes/sec | The maximum I/O bandwidth for read operations. | 
| read_bandwidth_mean | 15713.569282 | 199244.071667 | 58125.17605344445 | kilobytes/sec | The mean I/O bandwidth for read operations. | 
| read_bandwidth_min | 2560.0 | 61242.0 | 30026.88888888889 | kilobytes/sec | The minimum I/O bandwidth for read operations. | 
| read_bandwidth_stdev | 546.745 | 65246.966031 | 8616.168045111111 | kilobytes/sec | The standard deviation for I/O bandwidth with read operations. | 
| read_bytes | 4824506368.0 | 61730717696.0 | 17896966371.555559 |  | The total number of bytes associated with read I/O operations. | 
| read_completionlatency_max | 262.410511 | 6124.186573 | 1333.7768344444444 | milliseconds | The maximum latency for the completion of I/O operations after having submitted them to the kernel for processing. | 
| read_completionlatency_mean | 125.07446451683899 | 2619.6481831650137 | 403.9226721458616 | milliseconds | The mean latency for the completion of I/O operations after having submitted them to the kernel for processing. | 
| read_completionlatency_min | 3.221285 | 408.38951099999999 | 53.09218088888888 | milliseconds | The minimum latency for the completion of I/O operations after having submitted them to the kernel for processing. | 
| read_completionlatency_p50 | 125.30483199999999 | 2634.022912 | 408.30384355555557 | milliseconds | The 50th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 50% of all I/O operations will be less than or equal to this value. | 
| read_completionlatency_p70 | 139.460608 | 2667.577344 | 422.2266026666666 | milliseconds | The 70th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 70% of all I/O operations will be less than or equal to this value. | 
| read_completionlatency_p90 | 158.33497599999999 | 2835.349504 | 456.4800853333333 | milliseconds | The 90th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 90% of all I/O operations will be less than or equal to this value. | 
| read_completionlatency_p99 | 162.52928 | 3036.6760959999999 | 484.44211199999998 | milliseconds | The 99th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 99% of all I/O operations will be less than or equal to this value. | 
| read_completionlatency_p99_99 | 204.47232 | 4043.3090559999998 | 1035.294037333333 | milliseconds | The 99.99th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 99.99% of all I/O operations will be less than or equal to this value. | 
| read_completionlatency_stdev | 23.898507489954999 | 202.775762405382 | 50.63621908119244 | milliseconds | The standard deviation for all completion latency measurements with read operations. | 
| read_iops | 194.259979 | 4079.71628 | 3599.6289853333335 |  | The average number of read operations per second. | 
| read_iops_max | 452.0 | 5092.0 | 4171.777777777777 |  | The maximum number of read operations per second. | 
| read_iops_mean | 194.525 | 4084.490818 | 3607.9053463333336 |  | The mean number of read operations per second. | 
| read_iops_min | 12.0 | 3827.0 | 2786.777777777778 |  | The minimum number of read operations per second. | 
| read_iops_stdev | 34.754818 | 220.536425 | 147.02484755555555 |  | The standard deviation for the number of read operations per second. | 
| read_ios | 58359.0 | 1223974.0 | 1080003.7777777778 |  | The total number of read input/output operations. | 
| read_ios_dropped | 0.0 | 0.0 | 0.0 |  | The total number of read dropped input/output operations. | 
| read_ios_short | 0.0 | 0.0 | 0.0 |  | The total number of short read input/output operations. | 
| read_latency_max | 262.417111 | 6124.1951739999999 | 1333.7924684444445 | milliseconds | The maximum latency for read operations. | 
| read_latency_mean | 125.31562595346499 | 2624.783958116883 | 404.7109877959043 | milliseconds | The mean latency for read operations. | 
| read_latency_min | 3.808972 | 433.77435599999998 | 65.96513266666665 | milliseconds | The minimum latency for read operations. | 
| read_latency_stdev | 23.907676450169999 | 202.91714088587 | 50.65698046006966 | milliseconds | The standard deviation of latency measurements for read operations. | 
| read_submissionlatency_max | 62.879929 | 2041.5335309999999 | 406.41720799999998 | milliseconds | The maximum latency for submitting I/Os to the kernel for processing. | 
| read_submissionlatency_mean | 0.24092667047 | 5.135486366895 | 0.7880644981962222 | milliseconds | The mean latency for submitting I/Os to the kernel for processing. | 
| read_submissionlatency_min | 0.0033 | 0.031902 | 0.006789111111111111 | milliseconds | The minimum latency for submitting I/Os to the kernel for processing. | 
| read_submissionlatency_stdev | 2.934443015558 | 26.560566922469 | 5.744825794949667 | milliseconds | The standard deviation of latencies for submitting I/Os to the kernel for processing. | 
| write_bandwidth | 15702.0 | 200667.0 | 58223.333333333336 | kilobytes/sec | The average I/O bandwidth for write operations. | 
| write_bandwidth_max | 18084.0 | 462848.0 | 92574.55555555556 | kilobytes/sec | The maximum I/O bandwidth for write operations. | 
| write_bandwidth_mean | 15713.569282 | 199244.071667 | 58125.17605344445 | kilobytes/sec | The mean I/O bandwidth for write operations. | 
| write_bandwidth_min | 2560.0 | 61242.0 | 30026.88888888889 | kilobytes/sec | The minimum I/O bandwidth for write operations. | 
| write_bandwidth_stdev | 546.745 | 65246.966031 | 8616.168045111111 | kilobytes/sec | The standard deviation for I/O bandwidth with write operations. | 
| write_bytes | 4824506368.0 | 61730717696.0 | 17896966371.555559 |  | The total number of bytes associated with write I/O operations. | 
| write_completionlatency_max | 262.410511 | 6124.186573 | 1333.7768344444444 | milliseconds | The maximum latency for the completion of I/O operations after having submitted them to the kernel for processing. | 
| write_completionlatency_mean | 125.07446451683899 | 2619.6481831650137 | 403.9226721458616 | milliseconds | The mean latency for the completion of I/O operations after having submitted them to the kernel for processing. | 
| write_completionlatency_min | 3.221285 | 408.38951099999999 | 53.09218088888888 | milliseconds | The minimum latency for the completion of I/O operations after having submitted them to the kernel for processing. | 
| write_completionlatency_p50 | 125.30483199999999 | 2634.022912 | 408.30384355555557 | milliseconds | The 50th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 50% of all I/O operations will be less than or equal to this value. | 
| write_completionlatency_p70 | 139.460608 | 2667.577344 | 422.2266026666666 | milliseconds | The 70th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 70% of all I/O operations will be less than or equal to this value. | 
| write_completionlatency_p90 | 158.33497599999999 | 2835.349504 | 456.4800853333333 | milliseconds | The 90th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 90% of all I/O operations will be less than or equal to this value. | 
| write_completionlatency_p99 | 162.52928 | 3036.6760959999999 | 484.44211199999998 | milliseconds | The 99th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 99% of all I/O operations will be less than or equal to this value. | 
| write_completionlatency_p99_99 | 204.47232 | 4043.3090559999998 | 1035.294037333333 | milliseconds | The 99.99th percentile latency for the completion of I/O operations after having submitted them to the kernel for processing. 99.99% of all I/O operations will be less than or equal to this value. | 
| write_completionlatency_stdev | 23.898507489954999 | 202.775762405382 | 50.63621908119244 | milliseconds | The standard deviation for all completion latency measurements with write operations. | 
| write_iops | 194.259979 | 4079.71628 | 3599.6289853333335 |  | The average number of write operations per second. | 
| write_iops_max | 452.0 | 5092.0 | 4171.777777777777 |  | The maximum number of write operations per second. | 
| write_iops_mean | 194.525 | 4084.490818 | 3607.9053463333336 |  | The mean number of write operations per second. | 
| write_iops_min | 12.0 | 3827.0 | 2786.777777777778 |  | The minimum number of write operations per second. | 
| write_iops_stdev | 34.754818 | 220.536425 | 147.02484755555555 |  | The standard deviation for the number of write operations per second. | 
| write_ios | 58359.0 | 1223974.0 | 1080003.7777777778 |  | The total number of write input/output operations. | 
| write_ios_dropped | 0.0 | 0.0 | 0.0 |  | The total number of write dropped input/output operations. | 
| write_ios_short | 0.0 | 0.0 | 0.0 |  | The total number of short write input/output operations. | 
| write_latency_max | 262.417111 | 6124.1951739999999 | 1333.7924684444445 | milliseconds | The maximum latency for write operations. | 
| write_latency_mean | 125.31562595346499 | 2624.783958116883 | 404.7109877959043 | milliseconds | The mean latency for write operations. | 
| write_latency_min | 3.808972 | 433.77435599999998 | 65.96513266666665 | milliseconds | The minimum latency for write operations. | 
| write_latency_stdev | 23.907676450169999 | 202.91714088587 | 50.65698046006966 | milliseconds | The standard deviation of latency measurements for write operations. | 
| write_submissionlatency_max | 62.879929 | 2041.5335309999999 | 406.41720799999998 | milliseconds | The maximum latency for submitting I/Os to the kernel for processing. | 
| write_submissionlatency_mean | 0.24092667047 | 5.135486366895 | 0.7880644981962222 | milliseconds | The mean latency for submitting I/Os to the kernel for processing. | 
| write_submissionlatency_min | 0.0033 | 0.031902 | 0.006789111111111111 | milliseconds | The minimum latency for submitting I/Os to the kernel for processing. | 
| write_submissionlatency_stdev | 2.934443015558 | 26.560566922469 | 5.744825794949667 | milliseconds | The standard deviation of latencies for submitting I/Os to the kernel for processing. | 

## Additional Resources
* [Linux Reviews - How to test disk I/O performance](https://linuxreviews.org/HOWTO_Test_Disk_I/O_Performance)
* [How fast are your disks?](https://arstechnica.com/gadgets/2020/02/how-fast-are-your-disks-find-out-the-open-source-way-with-fio/)  
* [I/O Performance with FIO (VMWare)](https://docs.vmware.com/en/vSphere/6.7/solutions/vSphere-6.7.e3be11fbbf5809350802f6883cda9d28/GUID-A23C6CC9C2D014B7EBB03F92A6141093.html)  
* [Benchmarking persistent disk performance (Google)](https://cloud.google.com/compute/docs/disks/benchmarking-pd-performance)