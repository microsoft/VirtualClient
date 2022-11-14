# Redis Workload Metrics
The following document illustrates the type of results that are emitted by the Redis workload and captured by the
Virtual Client for net impact analysis.



### Workload-Specific Metrics

The following metrics are emitted by the Redis workload itself.

| Execution Profile           | Tool Name       | Metric Name                       | Example Value (avg) | Unit  |
|-----------------------------|-----------------|-----------------------------------|---------------------|-------|
|PERF-REDIS (linux-x64)       |RedisMemtier     |Throughput_1	                    |270454.59	          |requests/second|
|PERF-REDIS (linux-x64)       |RedisMemtier     |Throughput_2	                    |283481.85	          |requests/second|
|PERF-REDIS (linux-x64)       |RedisMemtier     |Throughput	                        |553936.44	          |requests/second|
|PERF-REDIS (linux-x64)       |RedisMemtier     |P50lat	                            |0.407	              |milliSeconds   |
|PERF-REDIS (linux-x64)       |RedisMemtier     |P90lat	                            |0.663	              |milliSeconds   |
|PERF-REDIS (linux-x64)       |RedisMemtier     |P95lat	                            |0.775	              |milliSeconds   |
|PERF-REDIS (linux-x64)       |RedisMemtier     |P99_9lat	                        |3.791	              |milliSeconds   |
|PERF-REDIS (linux-x64)       |RedisMemtier     |P99lat	                            |1.279                |milliSeconds   |
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_Requests/Sec           |303515.16	          |requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_verage_Latency	        |0.08	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_Min_Latency	        |0.048	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_P50_Latency	        |0.071	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_P95_Latency	        |0.151	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_P99_Latency	        |0.207	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_INLINE_Max_Latency	        |0.295	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_Requests/Sec	        |256820.5	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_Average_Latency	        |0.103	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_Min_Latency	            |0.056	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_P50_Latency	            |0.087	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_P95_Latency	            |0.159	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_P99_Latency	            |0.239	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |PING_MBULK_Max_Latency	            |2.311	milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_Requests/Sec	                |166933.34	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_Average_Latency	            |0.169	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_Min_Latency	                |0.064	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_P50_Latency	                |0.119	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_P95_Latency	                |0.239	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_P99_Latency	                |1.903	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SET_Max_Latency	                |4.183	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_Requests/Sec	                |250400	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_Average_Latency	            |0.097	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_P50_Latency	                |0.087	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_P95_Latency	                |0.191	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_P99_Latency	                |0.271	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_P99_Latency	                |0.271	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |GET_Max_Latency	                |0.487	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_Requests/Sec	                |244292.67	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_Average_Latency	            |0.103	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_Min_Latency	                |0.064	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_P50_Latency	                |0.095	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_P95_Latency	                |0.175	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_P99_Latency	                |0.287	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |INCR_Max_Latency	                |0.399	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPUSH_Requests/Sec	                |164196.72	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPUSH_Average_Latency	            |0.155	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPUSH_Min_Latency	                |0.072	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPUSH_P50_Latency	                |0.111	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPUSH_P95_Latency	                |0.255	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |RPUSH_Requests/Sec	                |227636.36	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPOP_Average_Latency	            |0.123	|milliSeconds|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |RPOP_Requests/Sec	                |244292.67	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SADD_Requests/Sec	                |294588.22	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |HSET_Requests/Sec	                |256820.5	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |SPOP_Requests/Sec	                |323096.78	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |ZADD_Requests/Sec	                |256820.5	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |ZPOPMIN_Requests/Sec	            |294588.22	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LPUSH (needed to benchmark LRANGE)_Requests/Sec	|270702.69	|requests/second|
|PERF-REDIS (linux-x64)       |RedisBenchmark   |LRANGE_100 (first 100 elements)_Requests/Sec	|48858.54	|requests/second|



