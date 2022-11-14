# Memcached Workload Metrics
The following document illustrates the type of results that are emitted by the Memcached workload and captured by the Virtual Client for net impact analysis.



### Workload-Specific Metrics

The following metrics are emitted by the Memcached workload itself.

| Execution Profile          | Tool Name        | Scenario Name   | Metric Name  | Example Value  | Unit           |
|----------------------------|------------------|-----------------|--------------|----------------|----------------|
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |throughput_1	 |220008.21       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |throughput_2	 |172007.2        |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |throughput_3	 |179746.96       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |throughput_4	 |170973.75       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |throughput	 |742736.12       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |p50lat	     |0.623           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |p90lat	     |1.263           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |p95lat	     |1.711           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |p99lat        |3.087           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_1c    |p99.9lat      |6.431           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |throughput_1	 |156702.04       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |throughput_2	 |153510.4        |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |throughput_3	 |161094.04       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |throughput_4	 |160117.99       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |throughput	 |631424.47       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |p50lat	     |1.143           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |p90lat	     |4.127           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |p95lat	     |4.479           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |p99lat        |5.791           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_2c    |p99.9lat      |12.991          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |throughput_1	 |178787.84       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |throughput_2	 |170148.35       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |throughput_3	 |163780.86       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |throughput_4	 |182411.81       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |throughput	 |695128.86       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |p50lat	     |3.327           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |p90lat	     |5.439           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |p95lat	     |6.047           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |p99lat        |8.575           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_4t_4c    |p99.9lat      |20.223          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |throughput_1	 |169774.05       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |throughput_2	 |184726.02       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |throughput_3	 |169058.23       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |throughput_4	 |171733.51       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |throughput	 |695291.81       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |p50lat	     |0.783           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |p90lat	     |10.815          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |p95lat	     |16.191          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |p99lat        |24.703          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_2c    |p99.9lat      |32.639          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |throughput_1	 |172328.66       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |throughput_2	 |253105.71       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |throughput_3	 |184098.16       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |throughput_4	 |198147.88       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |throughput	 |807680.41       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |p50lat	     |1.623           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |p90lat	     |17.663          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |p95lat	     |24.959          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |p99lat        |29.439          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_4c    |p99.9lat      |37.119          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |throughput_1	 |212758.09       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |throughput_2	 |189965.37       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |throughput_3	 |185578.3        |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |throughput_4	 |241675.54       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |throughput	 |829977.3        |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |p50lat	     |9.279           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |p90lat	     |25.855          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |p95lat	     |27.007          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |p99lat        |34.303          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_8t_8c    |p99.9lat      |41.727          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |throughput_1	 |198764.35       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |throughput_2	 |188875.28       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |throughput_3	 |215892.43       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |throughput_4	 |218265.97       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |throughput	 |821798.03       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |p50lat	     |8.383           |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |p90lat	     |24.959          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |p95lat	     |27.135          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |p99lat        |34.303          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_4c   |p99.9lat      |40.703          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |throughput_1	 |200478.59       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |throughput_2	 |207471.17       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |throughput_3	 |195936.23       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |throughput_4	 |211663.86       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |throughput	 |815549.85       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |p50lat	     |21.759          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |p90lat	     |33.535          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |p95lat	     |37.119          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |p99lat        |43.775          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_8c   |p99.9lat      |51.455          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |throughput_1	 |197953.99       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |throughput_2	 |211923.58       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |throughput_3	 |197491.77       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |throughput_4	 |208411.81       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |throughput	 |815781.15       |operations/sec  |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |p50lat	     |41.215          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |p90lat	     |56.063          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |p95lat	     |60.415          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |p99lat        |70.143          |milliSeconds    |
|PERF-MEMCACHED (linux-x64)  |MemcachedMemtier  |Memtier_16t_16c  |p99.9lat      |88.063          |milliSeconds    |



