# LMbench Workload Metrics
The following document illustrates the type of results that are emitted by the LMbench workload and captured by the
Virtual Client for net impact analysis.

### System Metrics
* [Performance Counters](./PerformanceCounterMetrics.md)
* [Power/Temperature Measurements](./PowerMetrics.md)

### Workload-Specific Metrics
The following metrics are emitted by the LMbench workload itself.

| Execution Profile   | Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------------|-----------|-------------|---------------------|---------------------|---------------------|------|
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-Mhz | 2494 | 2494 | 2494 | Mhz |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-null call | 3.41 |3.41 |3.41 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-null I/O | 3.58 |3.58 |3.58 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-stat | 4.46 | 4.46 | 4.46 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-open clos | 9.54 | 9.54 | 9.54 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-slct TCP | 7.01 | 7.01 | 7.01 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-sig inst | 3.58 | 7.01 | 7.01 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-sig hndl | 9.62 |9.62 |9.62 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-fork proc | 370 |370 |370 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-exec proc | 946 |946 |946 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ProcessorTimes-sh proc | 2281 |2281 |2281 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicInt-intgr bit | 0.27 | 0.27 | 0.27 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicInt-intgr add | 0.1 | 0.27 | 0.27 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicInt-intgr mul | 0.01 | 0.01 | 0.01 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicInt-intgr div | 7.23 | 7.23 | 7.23 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicInt-intgr mod | 7.23 | 7.23 | 7.55 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicFloat-float add | 2.41 | 2.41 | 2.41 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicFloat-float mul | 2.41 | 2.41 | 2.41 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicFloat-float div | 2.41 | 6.42 | 6.42 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicFloat-float bogo | 6.83 | 6.83 | 6.83 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicDouble-double add | 2.41 | 2.41 | 2.41 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicDouble-double mul | 2.41 | 2.41 | 2.41 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicDouble-double div | 9.25 | 9.25 | 9.25 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | BasicDouble-double bogo | 9.67 | 9.67 | 9.67 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-AF UNIX | 4242.0 | 4633.0 | 4471.421052631579 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-Bcopy (hand) | 3974.3 | 4541.9 | 4446.0999999999999 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-Bcopy (libc) | 7753.3 | 8069.6 | 7936.668421052632 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-File reread | 5077.3 | 5358.9 | 5291.463157894737 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-Mem reread | 8005.0 | 8406.0 | 8251.473684210527 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-Mem write | 6262.0 | 6702.0 | 6591.315789473684 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-Mmap reread | 7987.9 | 8240.2 | 8100.952631578946 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-Pipe | 2114.0 | 2461.0 | 2357.2631578947368 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationBandwidth-TCP | 2920.0 | 3757.0 | 3350.157894736842 | MB/s |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-2p/0K ctxsw | 10.5 | 13.4 | 11.742105263157895 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-AF UNIX | 0.0 | 0.0 | 0.0 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-Pipe | 0.0 | 0.0 | 0.0 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-RPC/TCP | 0.0 | 0.0 | 0.0 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-RPC/UDP | 0.0 | 0.0 | 0.0 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-TCP | 35.5 | 41.5 | 37.52105263157895 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-TCP conn | 22.0 | 66.0 | 38.89473684210526 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | CommunicationLatency-UDP | 30.1 | 35.4 | 32.71578947368421 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-16p/16K ctxsw | 11.3 | 14.8 | 13.173684210526315 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-16p/64K ctxsw | 12.3 | 15.8 | 13.942105263157897 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-2p/0K ctxsw | 10.5 | 13.4 | 11.742105263157895 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-2p/16K ctxsw | 9.66 | 13.9 | 11.792631578947369 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-2p/64K ctxsw | 10.5 | 14.4 | 11.715789473684211 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-8p/16K ctxsw | 11.1 | 14.2 | 12.88421052631579 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | ContextSwitching-8p/64K ctxsw | 11.4 | 15.2 | 13.605263157894737 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-0K File Create | 6.6518 | 15.9 | 8.161615789473684 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-0K File Delete | 4.6205 | 8.9458 | 6.006173684210525 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-100fd select | 0.0 | 0.0 | 0.0 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-10K File Create | 13.2 | 21.3 | 16.194736842105266 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-10K File Delete | 6.8272 | 22.1 | 9.763121052631576 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-Mmap Latency | 62500.0 | 322800.0 | 84189.47368421052 | microseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-Page Fault | 0.0 | 0.0 | 0.0 | Count |
| PERF-MEM-LMBENCH-Linux.json | LMbench | FileVmLatency-Prot Fault | 0.0 | 0.0 | 0.0 | Count |
| PERF-MEM-LMBENCH-Linux.json | LMbench | MemoryLatency-L1 | 1.138 | 1.161 | 1.1445789473684208 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | MemoryLatency-L2 | 7.935 | 9.571 | 8.73215789473684 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | MemoryLatency-Main mem | 28.7 | 30.0 | 29.22631578947368 | nanoseconds |
| PERF-MEM-LMBENCH-Linux.json | LMbench | MemoryLatency-Mhz | -1.0 | 1801.0 | 932.5263157894736 | Mhz |
| PERF-MEM-LMBENCH-Linux.json | LMbench | MemoryLatency-Rand mem | 0.0 | 119.0 | 97.67894736842107 | nanoseconds |