# Network Suite
The Networking workload suite is a set of 4 workloads that are the recommended benchmarks for the Azure Networking team. The workloads are each designed to test network performance
and reliability.

The workloads that are a part of the suite include:

* **CPS**  
  This workload that is used to measure network socket connection establishment efficiencies and reliability between a client and a server.

* **[Latte](https://github.com/microsoft/latte)**  
  This workload that is used to measure network communications latencies between a client and a server. This workload runs on Windows systems only.

* **NTttcp**  
  This workload is used to measure network communications bandwidth/throughput between a client and a server.

* **SockPerf**  
  This workload that is used to measure network communications latencies between a client and a server. This workload runs on Unix/Linux systems only.

### What is Being Tested?
The following performance analysis tests are ran as part of the network workload suite.

*Note:  
The Latte and SockPerf workload client measurements are the only ones that matter. Server-side measurements are not of concern.* 

| Workload              | Test     | Description                                               |
|-----------------------|----------|-----------------------------------------------------------|
| CPS Client | Cps |         |
| CPS Client | RexmitConnPercentage |         |
| CPS Client | RexmitPerConn |         |
| CPS Client | SynRttMean |         |
| CPS Client | SynRttMedian |         |
| CPS Client | SynRttP25 |         |
| CPS Client | SynRttP75 |         |
| CPS Client | SynRttP90 |         |
| CPS Client | SynRttP95 |         |
| CPS Client | SynRttP99 |         |
| CPS Client | SynRttP99_9 |         |
| CPS Client | SynRttP99_99 |         |
| CPS Server | Cps |         |
| CPS Server | RexmitConnPercentage |         |
| CPS Server | RexmitPerConn |         |
| CPS Server | SynRttMean |         |
| CPS Server | SynRttMedian |         |
| CPS Server | SynRttP25 |         |
| CPS Server | SynRttP75 |         |
| CPS Server | SynRttP90 |         |
| CPS Server | SynRttP95 |         |
| CPS Server | SynRttP99 |         |
| CPS Server | SynRttP99_9 |         |
| CPS Server | SynRttP99_99 |         |
| Latte Client | AvgCpuPercentage |         |
| Latte Client | InterruptPerSec |         |
| Latte Client | Latency |         |
| Latte Client | LatencyMax |         |
| Latte Client | LatencyMin |         |
| Latte Client | LatencyP25 |         |
| Latte Client | LatencyP50 |         |
| Latte Client | LatencyP75 |         |
| Latte Client | LatencyP90 |         |
| Latte Client | LatencyP99 |         |
| Latte Client | LatencyP99_9 |         |
| Latte Client | LatencyP99_99 |         |
| Latte Client | LatencyP99_999 |         |
| Latte Client | SysCallPerSec |         |
| Latte Client | AvgCpuPercentage |         |
| Latte Client | InterruptPerSec |         |
| Latte Client | Latency |         |
| Latte Client | LatencyMax |         |
| Latte Client | LatencyMin |         |
| Latte Client | LatencyP25 |         |
| Latte Client | LatencyP50 |         |
| Latte Client | LatencyP75 |         |
| Latte Client | LatencyP90 |         |
| Latte Client | LatencyP99 |         |
| Latte Client | LatencyP99_9 |         |
| Latte Client | LatencyP99_99 |         |
| Latte Client | LatencyP99_999 |         |
| Latte Client | SysCallPerSec |         |
| NTttcp Client | AvgBytesPerCompl |         |
| NTttcp Client | AvgCpuPercentage |         |
| NTttcp Client | AvgFrameSize |         |
| NTttcp Client | AvgPacketsPerInterrupt |         |
| NTttcp Client | CyclesPerByte |         |
| NTttcp Client | Errors |         |
| NTttcp Client | InterruptsPerSec |         |
| NTttcp Client | PacketsRetransmitted |         |
| NTttcp Client | ThroughputMbps |         |
| NTttcp Client | TotalBytesMB |         |
| NTttcp Server | AvgBytesPerCompl |         |
| NTttcp Server | AvgCpuPercentage |         |
| NTttcp Server | AvgFrameSize |         |
| NTttcp Server | AvgPacketsPerInterrupt |         |
| NTttcp Server | CyclesPerByte |         |
| NTttcp Server | Errors |         |
| NTttcp Server | InterruptsPerSec |         |
| NTttcp Server | PacketsRetransmitted |         |
| NTttcp Server | ThroughputMbps |         |
| NTttcp Server | TotalBytesMB |         |
| SockPerf Client | AvgLatency |         |
| SockPerf Client | DroppedMessages |         |
| SockPerf Client | MaxObservation |         |
| SockPerf Client | MinObservation |         |
| SockPerf Client | P25 |         |
| SockPerf Client | P50 |         |
| SockPerf Client | P75 |         |
| SockPerf Client | P90 |         |
| SockPerf Client | P99 |         |
| SockPerf Client | P99_9 |         |
| SockPerf Client | P99_99 |         |
| SockPerf Client | P99_999 |         |
| SockPerf Client | StdDev |         |

### Communications Protocols Test Scenarios
The following communications protocols and configurations are tested with each of the sets of tests noted above for
each workload.

* CPS
  * No specific protocol is important here.
* Latte
  * TCP communications
  * UDP communications
* NTttcp
  * TCP communications, single thread, 4K bytes buffer size
  * TCP communications, single thread, 64K bytes buffer size
  * TCP communications, single thread, 256K bytes buffer size
  * TCP communications, 32 threads, 4K bytes buffer size
  * TCP communications, 32 threads, 64K bytes buffer size
  * TCP communications, 32 threads, 256K bytes buffer size
  * TCP communications, single thread, 1400 byte buffer size
  * TCP communications, 32 threads, 1400 byte buffer size
* SockPerf
  * TCP communications
  * UDP communications

### Supported Platforms
* Linux x64
  * CPS
  * NTttcp
  * SockPerf

* Windows x64
  * CPS
  * Latte
  * NTttcp 