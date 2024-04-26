# Kafka
Kafka is a distributed system consisting of servers and clients that communicate via a high-performance TCP network protocol. It can be deployed on bare-metal hardware, virtual machines, and containers in on-premise as well as cloud environments.

* [Official Kafka Documentation](https://kafka.apache.org/documentation/#)
* [Kafka Github Repo](https://github.com/apache/kafka)
* [Download Kafka](https://kafka.apache.org/downloads)

For Kafka benchmarking we use load generation tools that ship with Kafka
* kafka-producer-perf-test
* kafka-consumer-perf-test

## What is Being Measured?
These kafka-*-perf-test tools can be used to generate load for measuring read and/or write latency, stress testing the nodes on specific parameter such as message/record size.

## Prerequisite changes to Binary
Inside file kafka-run-class.bat located at \kafka_2.13-3.6.1\bin\windows, we need to make below changes to avoid getting - Error: "The input line is too long" on Windows.
Change
```bat
rem Classpath addition for release
for %%i in ("%BASE_DIR%\libs\*") do (
	call :concat "%%i"
)
```

to
```bat
rem Classpath addition for release
call :concat "%BASE_DIR%\libs\*;"
```

For reference [The input line is too long](https://github.com/kafka-dev/kafka/issues/61)

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Kafka workload.

|ToolName	        |ScenarioName	                    |MetricName	                |Example MetricValue	|MetricUnit |
|-------------------|-----------------------------------|---------------------------|-----------------------|-------|
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_NMsg_Per_Sec	        |916466.0924	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-3	                |Data_Consumed_In_Mb	    |476.86	                |megabytes   |
|Kafka-Benchmark	|Consumer-async-3	                |Mb_Per_Sec_Throughput	    |53.34	                |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-3	                |Data_Consumed_In_nMsg	    |5000239	            |operations  |
|Kafka-Benchmark	|Consumer-async-3	                |nMsg_Per_Sec_Throughput    |559310.8501	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_Time_In_MilliSec	    |5456	                |milliseconds    |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_Mb_Per_Sec	        |87.401	                |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-3	                |Data_Consumed_In_Mb	    |476.86	                |megabytes   |
|Kafka-Benchmark	|Consumer-async-3	                |Mb_Per_Sec_Throughput	    |55.4617	            |megabytes/sec   |
|Kafka-Benchmark	|Consumer-async-3	                |Data_Consumed_In_nMsg	    |5000239	            |operations  |
|Kafka-Benchmark	|Consumer-async-3	                |nMsg_Per_Sec_Throughput	|581558.3857	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_Time_In_MilliSec	    |5107	                |milliseconds    |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_NMsg_Per_Sec	        |979095.1635	        |operations/sec  |
|Kafka-Benchmark	|Consumer-async-3	                |Fetch_Mb_Per_Sec	        |93.3738	            |megabytes/sec   |
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
