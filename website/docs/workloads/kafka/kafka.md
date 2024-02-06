# Kafka
Kafka is a distributed system consisting of servers and clients that communicate via a high-performance TCP network protocol. It can be deployed on bare-metal hardware, virtual machines, and containers in on-premise as well as cloud environments.

* [Official Kafka Documentation](https://kafka.apache.org/documentation/#)
* [Kafka Github Repo](https://github.com/apache/kafka)

For Kafka benchmarking we use load generation tools that ship with Kafka
* kafka-producer-perf-test
* kafka-consumer-perf-test

## What is Being Measured?
These kafka-*-perf-test tools can be used to generate load for measuring read and/or write latency, stress testing the nodes on specific parameter such as message/record size.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Kafka workload.

|ToolName	        |ScenarioName	                    |MetricName	                |Example MetricValue	|MetricUnit |
|-------------------|-----------------------------------|---------------------------|-----------------------|-------|
|Kafka-Benchmark	|Consumer-async-1	                |Fetch_NMsg_Per_Sec	        |916466.0924	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-1	                |Data_Consumed_In_Mb	    |476.86	                |megabytes   |
|Kafka-Benchmark	|Consumer-async-1	                |Mb_Per_Sec_Throughput	    |53.34	                |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-1	                |Data_Consumed_In_nMsg	    |5000239	            |operations  |
|Kafka-Benchmark	|Consumer-async-1	                |nMsg_Per_Sec_Throughput    |559310.8501	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-1	                |Fetch_Time_In_MilliSec	    |5456	                |milliseconds    |
|Kafka-Benchmark	|Consumer-async-1	                |Fetch_Mb_Per_Sec	        |87.401	                |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-2	                |Data_Consumed_In_Mb	    |476.86	                |megabytes   |
|Kafka-Benchmark	|Consumer-async-2	                |Mb_Per_Sec_Throughput	    |55.4617	            |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-2	                |Data_Consumed_In_nMsg	    |5000239	            |operations  |
|Kafka-Benchmark	|Consumer-async-2	                |nMsg_Per_Sec_Throughput	|581558.3857	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-2	                |Fetch_Time_In_MilliSec	    |5107	                |milliseconds    |
|Kafka-Benchmark	|Consumer-async-2	                |Fetch_NMsg_Per_Sec	        |979095.1635	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-2	                |Fetch_Mb_Per_Sec	        |93.3738	            |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-3	                |Data_Consumed_In_Mb	    |476.86	                |megabytes   |
|Kafka-Benchmark	|Consumer-async-3	                |Mb_Per_Sec_Throughput	    |52.8084	            |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-3	                |nMsg_Per_Sec_Throughput	|553736.3234	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-3	                |Data_Consumed_In_nMsg	    |5000239	            |operations  |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_Time_In_MilliSec	    |5458	                |milliseconds    |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_NMsg_Per_Sec	        |916130.2675	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_Mb_Per_Sec	        |87.369	                |megabytes/sec   |
|Kafka-Benchmark	|Consumer-sync	                    |Fetch_NMsg_Per_Sec	        |2366303.8334	        |operations/sec  |
|Kafka-Benchmark	|Consumer-sync	                    |Data_Consumed_In_Mb	    |476.8372	            |megabytes   |
|Kafka-Benchmark	|Consumer-sync	                    |Mb_Per_Sec_Throughput	    |69.7845	            |megabytes/sec   |
|Kafka-Benchmark	|Consumer-sync	                    |Data_Consumed_In_nMsg	    |5000000	            |operations  |
|Kafka-Benchmark	|Consumer-sync	                    |nMsg_Per_Sec_Throughput	|731743.0119	        |operations/sec  |
|Kafka-Benchmark	|Consumer-sync	                    |Fetch_Time_In_MilliSec	    |2113	                |milliseconds    |
|Kafka-Benchmark	|Consumer-sync	                    |Fetch_Mb_Per_Sec	        |225.6683	            |megabytes/sec   |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Latency-P99	            |1449	                |milliseconds    |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Latency-P99.9	            |1753	                |milliseconds    |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Total_Records_Sent	        |5000000	            |operations  |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Records_Per_Sec	        |111032.154912	        |operations/sec  |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Latency-Avg	            |74.24	                |milliseconds    |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Latency-Max	            |1764	                |milliseconds    |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Latency-P50	            |3	                    |milliseconds    |
|Kafka-Benchmark	|Producer-async-batchSize-8196-1	|Latency-P95	            |541	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Total_Records_Sent	        |5000000	            |operations  |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Latency-P99.9	            |234	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Records_Per_Sec	        |545494.217761	        |operations/sec  |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Latency-Max	            |545	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Latency-Avg	            |2.59	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Latency-P50	            |0	                    |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Latency-P95	            |15	                    |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-64000-sync	    |Latency-P99	            |18	                    |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Latency-Avg	            |1797.85	            |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Latency-P50	            |1895	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Records_Per_Sec	        |225591.048547	        |operations/sec  |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Latency-Max	            |2284	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Total_Records_Sent	        |5000000	            |operations  |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Latency-P95	            |2237	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Latency-P99	            |2273	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async	    |Latency-P99.9	            |2282	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Total_Records_Sent	        |5000000	            |operations  |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Latency-Max	            |2653	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Records_Per_Sec	        |107909.787418	        |operations/sec  |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Latency-Avg	            |107.52	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Latency-P50	            |3	                    |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Latency-P95	            |676	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Latency-P99	            |2148	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-2	|Latency-P99.9	            |2626	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Total_Records_Sent	        |5000000	            |operations  |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Latency-Avg	            |65.63	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Records_Per_Sec	        |105044.223618	        |operations/sec  |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Latency-P50	            |3	                    |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Latency-Max	            |1374	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Latency-P95	            |550	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Latency-P99	            |1080	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-async-3	|Latency-P99.9	            |1270	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Total_Records_Sent	        |5000000	            |operations  |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Records_Per_Sec	        |317561.130518	        |operations/sec  |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Latency-Avg	            |114.83	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Latency-Max	            |1216	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Latency-P50	            |3	                    |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Latency-P95	            |817	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Latency-P99	            |1150	                |milliseconds    |
|Kafka-Benchmark	|Producer-batchSize-8196-sync	    |Latency-P99.9	            |1195	                |milliseconds    |


