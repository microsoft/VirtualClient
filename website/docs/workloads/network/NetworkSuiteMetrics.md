# Network Workload Suite Metrics
The following document illustrates the type of results that are emitted by the Azure Networking benchmark workload suite and captured by the
Virtual Client for net impact analysis.

### System Metrics
* [Performance Counters](./PerformanceCounterMetrics.md)
* [Power/Temperature Measurements](./PowerMetrics.md)

### Workload-Specific Metrics
The following metrics are emitted by the suite of Networking workloads.

| Scenario | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|----------|-------------|---------------------|---------------------|---------------------|------|
| CPS Client | Cps | 10202.0 | 19591.0 | 12418.48717948718 | |
| CPS Client | RexmitConnPercentage | 0.0 | 3.5478 | 2.698990497737556 |  |
| CPS Client | RexmitPerConn | 0.0 | 1.0328 | 0.9713179487179486 |  |
| CPS Client | SynRttMean | 3462.0 | 20725.0 | 10515.586726998492 |  |
| CPS Client | SynRttMedian | 1688.0 | 5919.0 | 3624.135746606335 |  |
| CPS Client | SynRttP25 | 667.0 | 2179.0 | 1057.6681749622927 |  |
| CPS Client | SynRttP75 | 4749.0 | 12701.0 | 10201.94419306184 |  |
| CPS Client | SynRttP90 | 8886.0 | 44139.0 | 15424.107088989442 |  |
| CPS Client | SynRttP95 | 11781.0 | 72848.0 | 23776.671191553545 |  |
| CPS Client | SynRttP99 | 17330.0 | 497044.0 | 69282.47209653093 |  |
| CPS Client | SynRttP99_9 | 30938.0 | 3007013.0 | 1396051.3288084465 |  |
| CPS Client | SynRttP99_99 | 39198.0 | 3014102.0 | 2916708.707390649 |  |
| CPS Server | Cps | 10573.0 | 19756.0 | 12509.789552238806 |  |
| CPS Server | RexmitConnPercentage | 0.0 | 1.0227 | 0.6096656716417908 |  |
| CPS Server | RexmitPerConn | 0.0 | 1.024 | 0.9697847761194024 |  |
| CPS Server | SynRttMean | 694.0 | 11696.0 | 3653.4910447761196 |  |
| CPS Server | SynRttMedian | 423.0 | 1880.0 | 915.2 |  |
| CPS Server | SynRttP25 | 338.0 | 1580.0 | 667.0850746268657 |  |
| CPS Server | SynRttP75 | 573.0 | 3411.0 | 1933.9432835820897 |  |
| CPS Server | SynRttP90 | 898.0 | 36344.0 | 5342.064179104477 |  |
| CPS Server | SynRttP95 | 1342.0 | 68352.0 | 15316.823880597014 |  |
| CPS Server | SynRttP99 | 7213.0 | 165167.0 | 61413.89552238806 |  |
| CPS Server | SynRttP99_9 | 22961.0 | 1484531.0 | 89663.93134328358 |  |
| CPS Server | SynRttP99_99 | 29292.0 | 3186825.0 | 108853.4223880597 |  |
| CPS Client | Cps | 9720.0 | 15897.0 | 14383.082585278276 |  |
| CPS Client | RexmitConnPercentage | 6.4359 | 13.0922 | 7.698681687612204 |  |
| CPS Client | RexmitPerConn | 1.0 | 1.0296 | 1.0052050269299802 |  |
| CPS Client | SynRttMean | 15016.0 | 41122.0 | 32903.09515260323 |  |
| CPS Client | SynRttMedian | 9778.0 | 15000.0 | 12877.229802513464 |  |
| CPS Client | SynRttP25 | 1841.0 | 9048.0 | 5232.140035906643 |  |
| CPS Client | SynRttP75 | 16000.0 | 22011.0 | 19099.5368043088 |  |
| CPS Client | SynRttP90 | 23300.0 | 33322.0 | 27326.92459605027 |  |
| CPS Client | SynRttP95 | 32393.0 | 46958.0 | 38655.06283662478 |  |
| CPS Client | SynRttP99 | 59249.0 | 1031000.0 | 1018665.6876122083 |  |
| CPS Client | SynRttP99_9 | 79617.0 | 1054000.0 | 1042016.7612208258 |  |
| CPS Client | SynRttP99_99 | 91125.0 | 3049000.0 | 1374617.987432675 |  |
| CPS Server | Cps | 9812.0 | 15990.0 | 14502.646643109541 |  |
| CPS Server | RexmitConnPercentage | 0.0 | 2.7116 | 1.8959323321554767 |  |
| CPS Server | RexmitPerConn | 0.0 | 1.7592 | 1.3311212014134273 |  |
| CPS Server | SynRttMean | 10250.0 | 15660.0 | 13091.466431095407 |  |
| CPS Server | SynRttMedian | 8919.0 | 14372.0 | 12187.097173144877 |  |
| CPS Server | SynRttP25 | 1706.0 | 8587.0 | 4712.268551236749 |  |
| CPS Server | SynRttP75 | 14304.0 | 20912.0 | 17795.948763250883 |  |
| CPS Server | SynRttP90 | 20286.0 | 31713.0 | 24133.104240282686 |  |
| CPS Server | SynRttP95 | 25785.0 | 39049.0 | 30722.782685512368 |  |
| CPS Server | SynRttP99 | 47377.0 | 72409.0 | 56250.68727915194 |  |
| CPS Server | SynRttP99_9 | 74810.0 | 367477.0 | 86169.82155477032 |  |
| CPS Server | SynRttP99_99 | 91533.0 | 627165.0 | 114658.16077738516 |  |

| Scenario  | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| Latte Client | Latency-Average | 0.0 | 502.28 | 248.65472527472529 | microseconds |
| Latte Client | Latency-Max | 0.0 | 9998.0 | 6868.153846153846 | microseconds |
| Latte Client | Latency-Min | 0.0 | 153.0 | 100.75824175824175 | microseconds |
| Latte Client | Latency-P25 | 0.0 | 483.0 | 147.56043956043957 | microseconds |
| Latte Client | Latency-P50 | 0.0 | 535.0 | 223.16483516483516 | microseconds |
| Latte Client | Latency-P75 | 0.0 | 582.0 | 310.3296703296703 | microseconds |
| Latte Client | Latency-P90 | 0.0 | 1114.0 | 421.3956043956044 | microseconds |
| Latte Client | Latency-P99 | 0.0 | 1648.0 | 639.2307692307693 | microseconds |
| Latte Client | Latency-P99.9 | 0.0 | 2566.0 | 1207.2417582417582 | microseconds |
| Latte Client | Latency-P99.99 | 0.0 | 7760.0 | 3300.868131868132 | microseconds |
| Latte Client | Latency-P99.999 | 0.0 | 9998.0 | 6075.197802197802 | microseconds |
| Latte Client | ContextSwitches/sec | 0.0 | 15014.0 | 7100.780219780219 |  |
| Latte Client | Interrupts/sec | 0.0 | 15014.0 | 7100.780219780219 |  |
| Latte Client | SystemCalls/sec | 0.0 | 15025.0 | 9317.0 |  |
| Latte Server | N/A. No server-side metrics. | | | | |

| Scenario      | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|---------------|-------------|---------------------|---------------------|---------------------|------|
| NTttcp Client | AvgBytesPerCompl | 262144.0 | 262144.0 | 262144.0 |  |
| NTttcp Client | AvgCpuPercentage | 1.003 | 8.348 | 1.6551020408163267 |  |
| NTttcp Client | AvgFrameSize | 1416.278 | 1417.007 | 1416.9609489795924 |  |
| NTttcp Client | AvgPacketsPerInterrupt | 0.569 | 1.81 | 1.1293571428571432 |  |
| NTttcp Client | CyclesPerByte | 0.847 | 6.36 | 1.3787653061224492 |  |
| NTttcp Client | Errors | 0.0 | 0.0 | 0.0 |  |
| NTttcp Client | InterruptsPerSec | 3860.049 | 9881.386 | 5011.198642857144 |  |
| NTttcp Client | PacketsRetransmitted | 44.0 | 3701.0 | 824.4081632653062 |  |
| NTttcp Client | ThroughputMbps | 1275.48 | 1928.144 | 1748.5727857142854 |  |
| NTttcp Client | TotalBytesMB | 9123.25 | 13793.0 | 12508.461734693878 |  |
| NTttcp Server | AvgBytesPerCompl | 148758.33 | 228660.3 | 206218.26020408167 |  |
| NTttcp Server | AvgCpuPercentage | 2.113 | 11.194 | 4.108795918367347 |  |
| NTttcp Server | AvgFrameSize | 1416.677 | 1417.033 | 1416.9609081632655 |  |
| NTttcp Server | AvgPacketsPerInterrupt | 16.872 | 45.382 | 31.823214285714284 |  |
| NTttcp Server | CyclesPerByte | 1.699 | 8.856 | 3.4371326530612245 |  |
| NTttcp Server | Errors | 0.0 | 0.0 | 0.0 |  |
| NTttcp Server | InterruptsPerSec | 3549.873 | 9191.585 | 4922.264214285715 |  |
| NTttcp Server | PacketsRetransmitted | 0.0 | 1.0 | 0.10204081632653062 |  |
| NTttcp Server | ThroughputMbps | 1275.486 | 1928.187 | 1748.5868469387747 |  |
| NTttcp Server | TotalBytesMB | 9123.75 | 13794.0 | 12508.49867857143 |  |

| Test Name | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-----------|-------------|---------------------|---------------------|---------------------|------|
| SockPerf Client | Latency-Average | 211.387 | 372.585 | 335.3205522388059 |  |
| SockPerf Client | Latency-P25 | 118.546 | 335.709 | 297.53847761194029 |  |
| SockPerf Client | Latency-P50 | 205.239 | 362.025 | 328.1263582089553 |  |
| SockPerf Client | Latency-P75 | 278.214 | 396.483 | 360.8234776119402 |  |
| SockPerf Client | Latency-P90 | 333.078 | 429.849 | 394.0734626865671 |  |
| SockPerf Client | Latency-P99 | 408.152 | 529.951 | 472.29831343283578 |  |
| SockPerf Client | Latency-P99.9 | 785.848 | 2207.143 | 1455.0127462686565 |  |
| SockPerf Client | Latency-P99.99 | 4239.64 | 8768.027 | 6637.732552238807 |  |
| SockPerf Client | Latency-P99.999 | 9222.604 | 23002.865 | 15495.719104477614 |  |
| SockPerf Client | Latency-Stdev | 116.05 | 184.24 | 158.41534328358214 |  |
| SockPerf Server | N/A. No server-side metrics. | | | | |