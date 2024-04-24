# Memcached
Memcached is an open source (BSD licensed), high-performance, distributed memory object caching system. Memcached is an in-memory key-value store for small 
arbitrary data (strings, objects) from results of database calls, API calls, or page rendering. Memcached works with an in-memory dataset. It is a client-server
model workload in which Memcached acts as server. There are different tools that acts are clients.

One of the widely used is the memtier_benchmark produced by Redis Labs.
* [Memcached Performance](https://github.com/memcached/memcached/wiki/Performance)  
* [Memtier Benchmarking Tool](https://redis.com/blog/memtier_benchmark-a-high-throughput-benchmarking-tool-for-redis-memcached/)
* [Official Memcached Documentation](https://memcached.org/about)
* [Memcached Github Repo](https://github.com/memcached/memcached)
* [Memtier Benchmark Toolset](https://github.com/RedisLabs/memtier_benchmark)

## What is Being Measured?
The Memtier toolset is used to generate various traffic patterns against Memcached instances. It provides a robust set of customization and reporting 
capabilities all wrapped into a convenient and easy-to-use command-line interface. It performs GET and SET operations against a Memcached server 
and gives percentile latency distributions and throughput.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Memtier workload against a
Memcached server.

### Raw Metrics - Memtier Benchmark
The following table shows the list of metrics that are captured from the execution of the Memtier workload against a Memcached server. Each Memtier 
client will produce the following metrics. Certain profiles for Virtual Client capture aggregate metrics as well (see below).

| ScenarioName          | Metric Name  | Example Value  | Unit |
|-------------------------|------------|----------------|-------|
|memtier_16t_16c_1kb_r1:1|Bandwidth	 |99831.145833333328|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Throughput	| 96143.676666666681|	requests/sec|
|memtier_16t_16c_1kb_r1:1|Hits/sec	    |48070.774999999994	|             |
|memtier_16t_16c_1kb_r1:1|Misses/sec	|0	                |               |
|memtier_16t_16c_1kb_r1:1|Latency-Avg |265.929245	        |milliseconds|             
|memtier_16t_16c_1kb_r1:1|Latency-P50 |258.4736666666667	|milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P80 |299.226  	|milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P90  |331.775	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P95  |346.111	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99  |385.36433333333338|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99.9 |	442.02566666666667|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth	 |49797.129166666673|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput |	48070.774999999994|	requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Latency-Avg	|265.9334425	|milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P50	 |258.4736666666667	|milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P90	 |331.775	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P95	 |354.81500000000005|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99	|386.047	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99.9 |	442.87900000000008|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth	 |50034.015833333331|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput	 |48072.902499999989|	requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Latency-Avg	 |265.92504749999995|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P50	 |266.239	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P90	 |348.159	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P95	 |364.543	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99	 |395.263	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99.9 |	441.17233333333337|	milliseconds|

### Aggregated Metrics - Memtier Benchmark
The following tables shows the list of metrics that are captured from the execution of the Memtier workload against a Memcached server. The metrics
are the result of aggregating the raw metrics (see above) for each individual Memtier client into a single set.

| ScenarioName          | Metric Name  | Example Value  | Unit |
|-------------------------|--------------|---------------|-------|
|memtier_16t_16c_1kb_r1:1|Bandwidth Avg	 |99831.145833333328|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth Min	 |96698.49	        |   kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth Stddev	 |1823.6575402628735|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth Max	 |103119.55	        |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth P20	 |98381.842	        |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth P50	 |100443.297	        |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth P80	 |101887.18	        |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Bandwidth Total |1197973.75    	|kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|Throughput Avg	| 96143.676666666681|	requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput Max	|99310.66	        | requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput Min	|93126.7	        |requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput Stddev|	1756.3137622699699|	requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput P20	|94854.33	        |requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput P50	|95182.65	        |requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput P80	|98123.77	        |requests/sec|
|memtier_16t_16c_1kb_r1:1|Throughput Total	|1153724.12	        |requests/sec|
|memtier_16t_16c_1kb_r1:1|Hits/sec Avg	|48070.774999999994	|             |
|memtier_16t_16c_1kb_r1:1|Hits/sec Max	|49654.27	|                     |
|memtier_16t_16c_1kb_r1:1|Hits/sec Min	|46562.29	|                      |
|memtier_16t_16c_1kb_r1:1|Hits/sec Stddev	|878.156472669686   |	           |
|memtier_16t_16c_1kb_r1:1|Hits/sec Total	|576849.29999999993	|               |
|memtier_16t_16c_1kb_r1:1|Misses/sec Avg	|0	                |               |
|memtier_16t_16c_1kb_r1:1|Misses/sec Min	|0	                |               |   
|memtier_16t_16c_1kb_r1:1|Misses/sec Max	|0	                |               |
|memtier_16t_16c_1kb_r1:1|Misses/sec Stddev|	0	            |               |
|memtier_16t_16c_1kb_r1:1|Misses/sec Total	|0	                |               |
|memtier_16t_16c_1kb_r1:1|Latency-Avg Avg	|265.929245	        |milliseconds|             
|memtier_16t_16c_1kb_r1:1|Latency-Avg Min	|257.37339	        |milliseconds|         
|memtier_16t_16c_1kb_r1:1|Latency-Avg Max	|274.49494	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-Avg Stddev|	4.8649509314371953	|milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-Avg P80	|268.37573	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P50 Avg	|258.4736666666667	|milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P50 Min	|252.927	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P50 Max	|266.239	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P50 Stddev|	3.9095334191634046|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P50 P80	 |260.095	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P90 Min	 |331.775	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P90 Avg	 |337.74833333333333|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P90 P80	 |339.967	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P90 Max	 |348.159	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P90 Stddev|	4.6894529413236388|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P95 Min	 |346.111	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P95 Avg	 |354.4736666666667	|milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P95 Max	 |364.543	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P95 Stddev|	4.6894529413236388|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P95 P80	 |356.351	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99 Avg	 |385.36433333333338|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99 Min	 |376.831	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99 Max	 |397.311	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99 Stddev|	5.3427117542394855|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99 P80	 |389.119	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99.9 Avg|	442.02566666666667|	milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99.9 Min|	411.647	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99.9 Max|	532.479	        |milliseconds|
|memtier_16t_16c_1kb_r1:1|Latency-P99.9 Stddev	|32.01080706400402|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth Avg	 |49797.129166666673|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth Min	 |48234.48	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth Max	 |51437.47	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth Stddev	 |909.68510072129129|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth P20	 |47827.237	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth P50	 |48389.11	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth P80	 |50822.73	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Bandwidth Total	 |597565.55	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput Avg|	48070.774999999994|	requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput Min|	46562.29	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput Max|	49654.27	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput Stddev|	878.156472669686	|requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput P20|	47922.592	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput P50|	48103.772	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput P80|	49060.82	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Throughput Total	|576849.29999999993	|requests/sec|
|memtier_16t_16c_1kb_r1:1|GET-Latency-Avg Avg	|265.9334425	|milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-Avg Min	|257.37776	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-Avg Max	|274.49896	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-Avg Stddev|	4.8649389483855519|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P50 Avg	 |258.4736666666667	|milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P50 Min	 |252.927	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P50 Max	 |266.239	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P50 Stddev|	3.9095334191634046|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P80 Avg	 |301.428	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P80 Min	 |260.095	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P80 Max	 |339.967	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P80 Stddev|	4.3814413423246189|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P90 Avg	 |337.74833333333333|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P90 Min	 |331.775	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P90 Max	 |348.159	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P90 Stddev|	4.6894529413236388|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P95 Avg	 |354.81500000000005|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P95 Min	 |346.111	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P95 Max	 |364.543	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P95 Stddev|	5.0252567430265076|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99 Avg	|386.047	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99 Min	|376.831	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99 Max	|397.311	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99 Stddev|	5.3208600808515767|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99.9 Avg|	442.87900000000008|	milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99.9 Min|	411.647	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99.9 Max|	532.479	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|GET-Latency-P99.9 Stddev|	31.706816932640848|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth Avg	 |50034.015833333331|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth Min	 |48464.01	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth Max	 |51682.07	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth Stddev	 |913.97098802841549|	kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth P20	 |48003.542	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth P50	 |49993.77	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth P80	 |51064.45	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Bandwidth Total	 |600408.19	    |kilobytes/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput Avg	 |48072.902499999989|	requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput Min	 |46564.42	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput Max	 |49656.39	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput Stddev	|878.15536932182385|	requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput P20	 |46291.362	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput P50	 |48556.10	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput P80	 |49062.95	    |requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Throughput Total	 |576874.82999999984|	requests/sec|
|memtier_16t_16c_1kb_r1:1|SET-Latency-Avg Avg	 |265.92504749999995|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-Avg Min	 |257.36902	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-Avg Max	 |274.49091	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-Avg Stddev|	4.8649615272924267|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P50 Avg	 |258.4736666666667|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P50 Min	 |252.927	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P50 Max	 |266.239	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P50 Stddev|	3.9095334191634046|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P90 Avg	 |337.74833333333333|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P90 Min	 |331.775	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P90 Max	 |348.159	    |milli0seconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P90 Stddev|	4.6894529413236388|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P95 Avg	 |354.30299999999994|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P95 Min	 |346.111	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P95 Max	 |364.543	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P95 Stddev|	4.8029857380591956|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99 Avg	 |384.85233333333332|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99 Min	 |374.783	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99 Max	 |395.263	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99 Stddev|	5.1850095039021333|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99.9 Avg|	441.17233333333337|	milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99.9 Min|	409.599 	|milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99.9 Max|	536.575	    |milliseconds|
|memtier_16t_16c_1kb_r1:1|SET-Latency-P99.9 Stddev	|33.438038432632723|	milliseconds |