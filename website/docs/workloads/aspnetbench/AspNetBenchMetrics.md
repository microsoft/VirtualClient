# AspNetBench Workload Metrics
The following document illustrates the type of results that are emitted by the AspNetBench workload and captured by the
Virtual Client for net impact analysis.

[Bombardier output example](https://github.com/codesenberg/bombardier#examples)

  

### Workload-Specific Metrics
The following metrics are captured during the operations of the AspNetBench workload.

#### Metrics


| Name				       | Example            | Unit        | Description                                             |
|--------------------------|--------------------|-------------|---------------------------------------------------------|
| Latency Max			   | 178703				| microsecond | ASP.NET Web Request latency (max) |
| Latency Average		   | 8270.807963429836  | microsecond | ASP.NET Web Request latency (avg) |
| Latency Stddev		   | 6124.356473307014  | microsecond | ASP.NET Web Request latency (stv) |
| Latency P50			   | 6058				| microsecond | ASP.NET Web Request latency (P50) |
| Latency P75		   	   | 10913				| microsecond | ASP.NET Web Request latency (P75) |
| Latency P90	   		   | 17949				| microsecond | ASP.NET Web Request latency (P90) |
| Latency P95	   		   | 23318			    | microsecond | ASP.NET Web Request latency (P95) |
| Latency P99			   | 35856				| microsecond | ASP.NET Web Request latency (P99) |
| RequestPerSecond Max     | 61221.282458945345 | Reqs/sec	  | ASP.NET Web Request per second (max) |
| RequestPerSecond Average | 31211.609987720527 | Reqs/sec    | ASP.NET Web Request per second (avg) |
| RequestPerSecond Stddev  | 6446.822354105378  | Reqs/sec    | ASP.NET Web Request per second (stv) |
| RequestPerSecond P50     | 31049.462844       | Reqs/sec    | ASP.NET Web Request per second (P50) |
| RequestPerSecond P75     | 35597.436614       | Reqs/sec    | ASP.NET Web Request per second (P75) |
| RequestPerSecond P90     | 39826.205746       | Reqs/sec    | ASP.NET Web Request per second (P90) |
| RequestPerSecond P95     | 41662.542962       | Reqs/sec    | ASP.NET Web Request per second (P95) |
| RequestPerSecond P99     | 48600.556224       | Reqs/sec    | ASP.NET Web Request per second (P99) |