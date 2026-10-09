---
slug: /workloads/ctstraffic
---

# ctsTraffic
The CTS Traffic workload is a highly scalable client/server networking performance and reliability testing tool developed by Microsoft. It measures network socket connection establishment efficiencies, data transfer throughput, and network communication reliability across client and server endpoints.

## What is Being Tested?
The following performance analysis scenarios are covered as part of the CTS Traffic workload:

* **TCP Connection Establishment & Throughput**
* Connection scaling and socket creation performance between client and server endpoints.
* Network data transfer rates and throughput (push and pull patterns) over single or parallel TCP streams.

* **Network Reliability & Error Analytics**
* Tracking socket-level connection errors, protocol errors, and network packet drop rates.
* In-depth statistical analysis on connection success rates and timing distributions.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the CTS Traffic workload.

| Scenario | Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|----------|-------------|---------------------|---------------------|---------------------|------|
| CtsTraffic-Server | Completed(TimeSlice-114.821)| 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-100.028) | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-105.028) | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-110.028) | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-40.052)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-45.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-50.029)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-55.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-60.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-65.030)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-70.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-75.029)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-80.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-85.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-90.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | InFlight(TimeSlice-95.028)  | 1 | 1 | 1 |  |
| CtsTraffic-Server | RecvBps(TimeSlice-100.028)  | 40,488,140 | 40,488,140 | 40,488,140 | bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-105.028)  | 40,029,388 | 40,029,388 | 40,029,388 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-40.052)   | 155,054,612 | 155,054,612 | 155,054,612 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-45.028)   | 498,513,491 | 498,513,491 | 498,513,491 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-50.029)   | 77,199,075 | 77,199,075 | 77,199,075 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-55.028)   | 60,934,452 | 60,934,452 | 60,934,452 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-60.028)   | 53,071,052 | 53,071,052 | 53,071,052 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-65.030)   | 73,947,457 | 73,947,457 | 73,947,457 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-70.028)   | 112,885,038 | 112,885,038 | 112,885,038 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-75.029)   | 165,419,101 | 165,419,101 | 165,419,101 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-80.028)   | 258,447,030 | 258,447,030 | 258,447,030 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-85.028)   | 160,340,377 | 160,340,377 | 160,340,377 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-90.028)   | 7,156,531 | 7,156,531 | 7,156,531 |  bytes/sec
| CtsTraffic-Server | RecvBps(TimeSlice-95.028)   | 15,715,532 | 15,715,532 | 15,715,532 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-100.028)  | 119,943,987 | 119,943,987 | 119,943,987 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-105.028)  | 119,930,880 | 119,930,880 | 119,930,880 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-110.028)  | 119,943,987 | 119,943,987 | 119,943,987 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-114.821)  | 118,191,777 | 118,191,777 | 118,191,777 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-40.052)   | 3,160,405 | 3,160,405 | 3,160,405 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-45.028)   | 42,290,212 | 42,290,212 | 42,290,212 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-50.029)   | 119,880,689 | 119,880,689 | 119,880,689 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-55.028)   | 119,941,761 | 119,941,761 | 119,941,761 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-60.028)   | 119,878,451 | 119,878,451 | 119,878,451 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-65.030)   | 119,738,805 | 119,738,805 | 119,738,805 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-70.028)   | 120,778,730 | 120,778,730 | 120,778,730 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-75.029)   | 119,841,375 | 119,841,375 | 119,841,375 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-80.028)   | 119,836,882 | 119,836,882 | 119,836,882 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-85.028)   | 119,839,129 | 119,839,129 | 119,839,129 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-90.028)   | 119,930,880 | 119,930,880 | 119,930,880 |  bytes/sec
| CtsTraffic-Server | SendBps(TimeSlice-95.028)   | 119,930,880 | 119,930,880 | 119,930,880 |  bytes/sec