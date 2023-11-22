# Redis
Redis is an open source (BSD licensed), in-memory, high-performance, distributed memory object caching system used as a database, 
cache, message broker and streaming engine.

* [Official Redis Documentation](https://redis.io/docs/about/)
* [Redis Github Repo](https://github.com/redis/redis)

Two of the widely used tools for benchmarking performance of a Redis server include the Memtier and Redis benchmarks.
* [Memtier Benchmarking Tool](https://redis.com/blog/memtier_benchmark-a-high-throughput-benchmarking-tool-for-redis-memcached/)
* [Memtier Benchmark Repo](https://github.com/RedisLabs/memtier_benchmark)
* [Redis Benchmarking Tool](https://redis.io/docs/reference/optimization/benchmarks/)

## What is Being Measured?
Either the Memtier toolset or Redis benchmark is used to generate various traffic patterns against Redis instances. These toolsets performs GET and SET operations against 
the Redis server and provides throughput and latency percentile distributions.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Memtier workload against a
Redis server.

|ToolName       | ScenarioName          | Metric Name  | Example Value  | Unit |
|--------------|-------------------------|--------------|---------------|-------|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Throughput_Avg	| 96143.676666666681|	requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Throughput_Max	|99310.66	        | requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Throughput_Min	|93126.7	        |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Throughput_Stdev|	1756.3137622699699|	requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Throughput_P80	|98123.77	        |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Throughput_Sum	|1153724.12	        |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Hits/sec_Avg	|48070.774999999994	|             |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Hits/sec_Max	|49654.27	|                     |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Hits/sec_Min	|46562.29	|                      |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Hits/sec_Stdev	|878.156472669686   |	           |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Hits/sec_P80	|49060.82	        |              |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Hits/sec_Sum	|576849.29999999993	|               |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Misses/sec_Avg	|0	                |               |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Misses/sec_Min	|0	                |               |   
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Misses/sec_Max	|0	                |               |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Misses/sec_Stdev|	0	            |               |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Misses/sec_P80	|0	                |               |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Misses/sec_Sum	|0	                |               |
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-Avg_Avg	|265.929245	        |milliseconds|             
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-Avg_Min	|257.37339	        |milliseconds|         
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-Avg_Max	|274.49494	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-Avg_Stdev|	4.8649509314371953	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-Avg_P80	|268.37573	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P50_Avg	|258.4736666666667	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P50_Min	|252.927	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P50_Max	|266.239	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P50_Stdev|	3.9095334191634046|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P50_P80	 |260.095	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P90_Min	 |331.775	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P90_Avg	 |337.74833333333333|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P90_P80	 |339.967	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P90_Max	 |348.159	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P90_Stdev|	4.6894529413236388|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P95_Min	 |346.111	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P95_Avg	 |354.4736666666667	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P95_Max	 |364.543	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P95_Stdev|	4.6894529413236388|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P95_P80	 |356.351	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99_Avg	 |385.36433333333338|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99_Min	 |376.831	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99_Max	 |397.311	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99_Stdev|	5.3427117542394855|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99_P80	 |389.119	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99.9_Avg|	442.02566666666667|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99.9_Min|	411.647	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99.9_Max|	532.479	        |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99.9_Stdev	|32.01080706400402|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Latency-P99.9_P80	|452.607	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Bandwidth_Avg	 |99831.145833333328|	kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Bandwidth_Min	 |96698.49	        |   kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Bandwidth_Stdev	 |1823.6575402628735|	kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Bandwidth_Max	 |103119.55	        |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1||Bandwidth_P80	 |101887.18	        |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Throughput_Avg|	48070.774999999994|	requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|Bandwidth_Sum	 |1197973.75    	|kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Throughput_Min|	46562.29	    |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Throughput_Max|	49654.27	    |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Throughput_P80|	49060.82	    |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Throughput_Stdev|	878.156472669686	|requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Throughput_Sum	|576849.29999999993	|requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-Avg_Avg	|265.9334425	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-Avg_Min	|257.37776	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-Avg_P80	|268.38027	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-Avg_Max	|274.49896	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-Avg_Stdev|	4.8649389483855519|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P50_Avg	 |258.4736666666667	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P50_Min	 |252.927	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P50_Max	 |266.239	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P90_Avg	 |337.74833333333333|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P90_Stdev|	4.6894529413236388|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P90_Min	 |331.775	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P90_Max	 |348.159	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P50_Stdev|	3.9095334191634046|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P50_P80	 |260.095	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P90_P80	 |339.967	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P95_Avg	 |354.81500000000005|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P95_Min	 |346.111	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P95_Max	 |364.543	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P95_Stdev|	5.0252567430265076|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P95_P80	|356.351	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99_Avg	|386.047	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99_Min	|376.831	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99_Max	|397.311	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99_Stdev|	5.3208600808515767|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99_P80	 |391.167	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99.9_Avg|	442.87900000000008|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99.9_Min|	411.647	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99.9_Max|	532.479	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99.9_Stdev|	31.706816932640848|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Latency-P99.9_P80	|454.655	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Bandwidth_Avg	 |49797.129166666673|	kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Bandwidth_Stdev	 |909.68510072129129|	kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Bandwidth_Min	 |48234.48	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Bandwidth_Max	 |51437.47	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Bandwidth_P80	 |50822.73	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Throughput_Avg	 |48072.902499999989|	requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|GET_Bandwidth_Sum	 |597565.55	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Throughput_Min	 |46564.42	    |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Throughput_Max	 |49656.39	    |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Throughput_P80	 |49062.95	    |requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Throughput_Stdev	|878.15536932182385|	requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Throughput_Sum	 |576874.82999999984|	requests/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-Avg_Avg	 |265.92504749999995|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-Avg_Min	 |257.36902	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-Avg_Max	 |274.49091	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-Avg_P80	 |268.37118	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P50_Avg	 |258.4736666666667|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-Avg_Stdev|	4.8649615272924267|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P50_Min	 |252.927	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P50_Max	 |266.239	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P50_P80	 |260.095	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P90_Avg	 |337.74833333333333|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P50_Stdev|	3.9095334191634046|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P90_Min	 |331.775	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P90_Max	 |348.159	    |milli0seconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P90_Stdev|	4.6894529413236388|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P90_P80	 |339.967	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P95_Avg	 |354.30299999999994|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P95_Min	 |346.111	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P95_Max	 |364.543	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P95_Stdev|	4.8029857380591956|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P95_P80	 |356.351	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99_Stdev|	5.1850095039021333|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99_Min	 |374.783	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99_Avg	 |384.85233333333332|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99_P80	 |389.119	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99_Max	 |395.263	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99.9_Avg|	441.17233333333337|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99.9_Min|	409.599 	|milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99.9_Max|	536.575	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99.9_Stdev	|33.438038432632723|	milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Bandwidth_Avg	 |50034.015833333331|	kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Latency-P99.9_P80|	450.559	    |milliseconds|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Bandwidth_Min	 |48464.01	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Bandwidth_P80	 |51064.45	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Bandwidth_Stdev	 |913.97098802841549|	kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Bandwidth_Sum	 |600408.19	    |kilobytes/sec|
|Memtier-Redis	|memtier_16t_16c_1kb_r1:1|SET_Bandwidth_Max	 |51682.07	    |kilobytes/sec|
