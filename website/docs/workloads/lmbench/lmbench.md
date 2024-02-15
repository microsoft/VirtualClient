# LMbench
LMbench (version 3) is a suite of simple, portable benchmarks ANSI/C microbenchmarks for UNIX/POSIX. In general, it measures two key 
features: component bandwidth and latency. LMbench is intended to provide system developers insights into basic performance and costs 
of key system operations.

* [LMbench Documentation](http://www.bitmover.com/lmbench/whatis_lmbench.html)
* [LMbench Manual](http://www.bitmover.com/lmbench/man_lmbench.html)

## System Requirements
The following section provides special considerations required for the system on which the LMbench workload will be run.

* Physical Memory = 16 GB minimum  
* Disk Space = At least 20 MB of free space on the OS disk

## What is Being Tested?
The following performance analysis tests are ran as part of the LMbench workload. Note that although LMbench runs benchmarks covering
various aspects of the system, the memory performance benchmarks are the ones that are most interesting for net impact analysis.

http://www.bitmover.com/lmbench/man_lmbench.html

| Bandwidth Benchmark   | Description                                               |
|-----------------------|-----------------------------------------------------------|
| Cached file read      | Measures times for reading and summing a file             |
| Memory copy (bcopy)   | Measures memory copy operation speeds                     |
| Memory read           | Measures memory read operation speeds                     |
| Memory write          | Measures memory write operation speeds                    |
| Pipe                  | Measures data movement times through named pipes          |
| TCP                   | Measures data movement times through TCP/IP sockets       |

| Latency Benchmark     | Description                                                    |
|-----------------------|----------------------------------------------------------------|
| Context switching     | Measures context switching time for processes on the system    |
| Networking: connection establishment, pipe, TCP, UDP, and RPC hot potato   | Measures inter-process connection latency via communications sockets |
| File system creates and deletes       | Measures file system create/delete performance              |
| Process creation                      | Measures the time the system takes to create new processes  |
| System call overhead                  | Measures the time it takes to make simple operating system calls |
| Memory read latency                   | Measures memory read latency       |

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the LMbench workload.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| ProcessorTimes-Mhz | 2494 | 2494 | 2494 | Mhz |
| ProcessorTimes-null call | 3.41 |3.41 |3.41 | microseconds |
| ProcessorTimes-null I/O | 3.58 |3.58 |3.58 | microseconds |
| ProcessorTimes-stat | 4.46 | 4.46 | 4.46 | microseconds |
| ProcessorTimes-open clos | 9.54 | 9.54 | 9.54 | microseconds |
| ProcessorTimes-slct TCP | 7.01 | 7.01 | 7.01 | microseconds |
| ProcessorTimes-sig inst | 3.58 | 7.01 | 7.01 | microseconds |
| ProcessorTimes-sig hndl | 9.62 |9.62 |9.62 | microseconds |
| ProcessorTimes-fork proc | 370 |370 |370 | microseconds |
| ProcessorTimes-exec proc | 946 |946 |946 | microseconds |
| ProcessorTimes-sh proc | 2281 |2281 |2281 | microseconds |
| BasicInt-intgr bit | 0.27 | 0.27 | 0.27 | nanoseconds |
| BasicInt-intgr add | 0.1 | 0.27 | 0.27 | nanoseconds |
| BasicInt-intgr mul | 0.01 | 0.01 | 0.01 | nanoseconds |
| BasicInt-intgr div | 7.23 | 7.23 | 7.23 | nanoseconds |
| BasicInt-intgr mod | 7.23 | 7.23 | 7.55 | nanoseconds |
| BasicFloat-float add | 2.41 | 2.41 | 2.41 | nanoseconds |
| BasicFloat-float mul | 2.41 | 2.41 | 2.41 | nanoseconds |
| BasicFloat-float div | 2.41 | 6.42 | 6.42 | nanoseconds |
| BasicFloat-float bogo | 6.83 | 6.83 | 6.83 | nanoseconds |
| BasicDouble-double add | 2.41 | 2.41 | 2.41 | nanoseconds |
| BasicDouble-double mul | 2.41 | 2.41 | 2.41 | nanoseconds |
| BasicDouble-double div | 9.25 | 9.25 | 9.25 | nanoseconds |
| BasicDouble-double bogo | 9.67 | 9.67 | 9.67 | nanoseconds |
| CommunicationBandwidth-AF UNIX | 4242.0 | 4633.0 | 4471.421052631579 | MB/s |
| CommunicationBandwidth-Bcopy (hand) | 3974.3 | 4541.9 | 4446.0999999999999 | MB/s |
| CommunicationBandwidth-Bcopy (libc) | 7753.3 | 8069.6 | 7936.668421052632 | MB/s |
| CommunicationBandwidth-File reread | 5077.3 | 5358.9 | 5291.463157894737 | MB/s |
| CommunicationBandwidth-Mem reread | 8005.0 | 8406.0 | 8251.473684210527 | MB/s |
| CommunicationBandwidth-Mem write | 6262.0 | 6702.0 | 6591.315789473684 | MB/s |
| CommunicationBandwidth-Mmap reread | 7987.9 | 8240.2 | 8100.952631578946 | MB/s |
| CommunicationBandwidth-Pipe | 2114.0 | 2461.0 | 2357.2631578947368 | MB/s |
| CommunicationBandwidth-TCP | 2920.0 | 3757.0 | 3350.157894736842 | MB/s |
| CommunicationLatency-2p/0K ctxsw | 10.5 | 13.4 | 11.742105263157895 | microseconds |
| CommunicationLatency-AF UNIX | 0.0 | 0.0 | 0.0 | microseconds |
| CommunicationLatency-Pipe | 0.0 | 0.0 | 0.0 | microseconds |
| CommunicationLatency-RPC/TCP | 0.0 | 0.0 | 0.0 | microseconds |
| CommunicationLatency-RPC/UDP | 0.0 | 0.0 | 0.0 | microseconds |
| CommunicationLatency-TCP | 35.5 | 41.5 | 37.52105263157895 | microseconds |
| CommunicationLatency-TCP conn | 22.0 | 66.0 | 38.89473684210526 | microseconds |
| CommunicationLatency-UDP | 30.1 | 35.4 | 32.71578947368421 | microseconds |
| ContextSwitching-16p/16K ctxsw | 11.3 | 14.8 | 13.173684210526315 | microseconds |
| ContextSwitching-16p/64K ctxsw | 12.3 | 15.8 | 13.942105263157897 | microseconds |
| ContextSwitching-2p/0K ctxsw | 10.5 | 13.4 | 11.742105263157895 | microseconds |
| ContextSwitching-2p/16K ctxsw | 9.66 | 13.9 | 11.792631578947369 | microseconds |
| ContextSwitching-2p/64K ctxsw | 10.5 | 14.4 | 11.715789473684211 | microseconds |
| ContextSwitching-8p/16K ctxsw | 11.1 | 14.2 | 12.88421052631579 | microseconds |
| ContextSwitching-8p/64K ctxsw | 11.4 | 15.2 | 13.605263157894737 | microseconds |
| FileVmLatency-0K File Create | 6.6518 | 15.9 | 8.161615789473684 | microseconds |
| FileVmLatency-0K File Delete | 4.6205 | 8.9458 | 6.006173684210525 | microseconds |
| FileVmLatency-100fd select | 0.0 | 0.0 | 0.0 | microseconds |
| FileVmLatency-10K File Create | 13.2 | 21.3 | 16.194736842105266 | microseconds |
| FileVmLatency-10K File Delete | 6.8272 | 22.1 | 9.763121052631576 | microseconds |
| FileVmLatency-Mmap Latency | 62500.0 | 322800.0 | 84189.47368421052 | microseconds |
| FileVmLatency-Page Fault | 0.0 | 0.0 | 0.0 | Count |
| FileVmLatency-Prot Fault | 0.0 | 0.0 | 0.0 | Count |
| MemoryLatency-L1 | 1.138 | 1.161 | 1.1445789473684208 | nanoseconds |
| MemoryLatency-L2 | 7.935 | 9.571 | 8.73215789473684 | nanoseconds |
| MemoryLatency-Main mem | 28.7 | 30.0 | 29.22631578947368 | nanoseconds |
| MemoryLatency-Mhz | -1.0 | 1801.0 | 932.5263157894736 | Mhz |
| MemoryLatency-Rand mem | 0.0 | 119.0 | 97.67894736842107 | nanoseconds |
