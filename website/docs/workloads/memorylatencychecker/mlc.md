---
slug: /workloads/mlc
---

# Memory Latency Checker (MLC)
Intel Memory Latency Checker (MLC) is a tool used to measure memory latencies and bandwidth and how these memory aspects change with increasing load.

* [Intel Documentation](https://www.intel.com/content/www/us/en/developer/articles/tool/intelr-memory-latency-checker.html)

## Limitations
Intel MLC is designed primarily for physical hardware systems (vs. for virtual machines). The software will attempt to disable hardware prefetchers while 
capturing latency measurements. This prefetcher control is exposed through a model-specific registry (MSR) on the physical hardware system. Best results are
are produced as such when running on physical hardware systems. The software can be reliably used for virtual machine scenarios for bandwidth measurements.
When running MLC on virtual machines, the virtualization layer prevents access to MSRs and thus cannot modify settings for hardware prefetchers. So, latency measurements
may not be reliable for comparisons across systems depending upon the settings in the MSRs.

## What is Being Measured?
When Intel MLC is launched without any additional parameters, it automatically identifies the system topology and measures the following:

* A matrix of idle memory latencies for requests originating from each of the sockets and addressed to each of the available sockets (Unit: nano secs)

* Peak injection memory bandwidth measured (with all accesses to local memory) for requests having varying amounts of reads and writes (each core generating requests
  as fast as possible).

* A matrix of memory bandwidth values for requests originating from each of the sockets
  and addressed to each of the available sockets.

* Latencies at different bandwidth ranges/points.

* Latencies between caches in the processor.

Intel MLC also provides command line arguments for fine-grained control over the latencies and bandwidth measurements. The details of parameters are mentioned in the profile documentation.

* [Workload Profile Documentation](./mlc-profiles.md)

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the MLC workload. The number of Numa Nodes that were on the system was 
4 for the below example.

| Metric Name | Example Value (min) | Example Value (max) | Example Value (avg) | Unit |
|-------------|---------------------|---------------------|---------------------|------|
| Local Socket L2->L2 HIT  latency | 56.0 | 94.8 | 60.22073732718893 | nanoseconds |
| Local Socket L2->L2 HITM latency | 57.3 | 85.8 | 61.877419354838718 | nanoseconds |
| idle-latency_nodes_0_0 | 86.9 | 148.5 | 94.88018433179724 | nanoseconds |
| load_bandwidth_at_delay_00000 | 11414.6 | 17013.0 | 15006.365437788018 | MBps |
| load_bandwidth_at_delay_00002 | 12378.8 | 17104.3 | 15090.00552995392 | MBps |
| load_bandwidth_at_delay_00008 | 12865.7 | 17025.4 | 15026.367281105991 | MBps |
| load_bandwidth_at_delay_00015 | 12496.7 | 16962.0 | 14963.37788018433 | MBps |
| load_bandwidth_at_delay_00050 | 9066.9 | 11609.5 | 11347.43824884793 | MBps |
| load_bandwidth_at_delay_00100 | 5057.4 | 6661.9 | 6021.277880184331 | MBps |
| load_bandwidth_at_delay_00200 | 3137.7 | 3667.4 | 3513.3760368663599 | MBps |
| load_bandwidth_at_delay_00300 | 2289.7 | 2743.5 | 2664.607834101382 | MBps |
| load_bandwidth_at_delay_00400 | 1825.2 | 2257.0 | 2188.821198156682 | MBps |
| load_bandwidth_at_delay_00500 | 1495.1 | 1958.7 | 1893.6327188940095 | MBps |
| load_bandwidth_at_delay_00700 | 1096.1 | 1614.5 | 1546.0290322580646 | MBps |
| load_bandwidth_at_delay_01000 | 1076.3 | 1350.6 | 1289.9917050691245 | MBps |
| load_bandwidth_at_delay_01300 | 943.2 | 1209.3 | 1148.1198156682029 | MBps |
| load_bandwidth_at_delay_01700 | 808.7 | 1096.1 | 1034.7843317972352 | MBps |
| load_bandwidth_at_delay_02500 | 611.6 | 980.0 | 913.8018433179725 | MBps |
| load_bandwidth_at_delay_03500 | 609.2 | 905.8 | 848.7917050691245 | MBps |
| load_bandwidth_at_delay_05000 | 572.5 | 856.8 | 795.8792626728111 | MBps |
| load_bandwidth_at_delay_09000 | 583.5 | 801.2 | 735.5801843317973 | MBps |
| load_bandwidth_at_delay_20000 | 520.9 | 763.9 | 700.148387096774 | MBps |
| loaded_latency_at_delay_00000 | 90.27 | 166.66 | 100.97073732718894 | nanoseconds |
| loaded_latency_at_delay_00002 | 90.28 | 155.82 | 99.32543778801846 | nanoseconds |
| loaded_latency_at_delay_00008 | 90.42 | 153.86 | 99.54096774193546 | nanoseconds |
| loaded_latency_at_delay_00015 | 90.22 | 152.97 | 99.6316589861751 | nanoseconds |
| loaded_latency_at_delay_00050 | 90.22 | 167.73 | 99.12626728110596 | nanoseconds |
| loaded_latency_at_delay_00100 | 90.05 | 137.57 | 97.73838709677418 | nanoseconds |
| loaded_latency_at_delay_00200 | 89.54 | 138.51 | 97.65880184331798 | nanoseconds |
| loaded_latency_at_delay_00300 | 88.93 | 137.85 | 98.20576036866356 | nanoseconds |
| loaded_latency_at_delay_00400 | 88.78 | 135.51 | 97.2936866359447 | nanoseconds |
| loaded_latency_at_delay_00500 | 88.78 | 155.42 | 97.23244239631338 | nanoseconds |
| loaded_latency_at_delay_00700 | 88.51 | 195.6 | 98.01649769585254 | nanoseconds |
| loaded_latency_at_delay_01000 | 88.53 | 125.07 | 96.82207373271889 | nanoseconds |
| loaded_latency_at_delay_01300 | 88.29 | 133.44 | 96.77861751152074 | nanoseconds |
| loaded_latency_at_delay_01700 | 88.33 | 137.98 | 96.8858525345622 | nanoseconds |
| loaded_latency_at_delay_02500 | 88.08 | 161.44 | 97.4829953917051 | nanoseconds |
| loaded_latency_at_delay_03500 | 88.34 | 141.85 | 96.29304147465438 | nanoseconds |
| loaded_latency_at_delay_05000 | 87.7 | 135.44 | 96.10709677419354 | nanoseconds |
| loaded_latency_at_delay_09000 | 87.61 | 124.42 | 96.76253456221201 | nanoseconds |
| loaded_latency_at_delay_20000 | 87.42 | 129.93 | 96.20557603686638 | nanoseconds |
| memory_bandwidth_nodes_0_0 | 24155.2 | 31461.5 | 27718.97695852534 | MBps |
| peak_injection_bandwidth_at_1:1 Reads-Writes | 39615.9 | 52091.1 | 46210.8534562212 | MBps |
| peak_injection_bandwidth_at_2:1 Reads-Writes | 30999.4 | 47359.0 | 39572.31612903225 | MBps |
| peak_injection_bandwidth_at_3:1 Reads-Writes | 30875.7 | 38271.3 | 34344.0460829493 | MBps |
| peak_injection_bandwidth_at_ALL Reads | 24431.7 | 31511.1 | 27711.27972350231 | MBps |
| peak_injection_bandwidth_at_Stream-triad like | 23221.1 | 45475.9 | 33316.79677419354 | MBps |