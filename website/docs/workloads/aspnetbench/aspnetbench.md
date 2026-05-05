# ASP.NET Benchmarks
The ASP.NET benchmarks measure the throughput and latency of ASP.NET Kestrel web applications under sustained HTTP load.
The workloads use a client-server architecture where the server runs an ASP.NET application and the client generates
HTTP requests using various workload generators including Bombardier and Wrk.

Two server workloads are supported:

* **TechEmpower JSON Serialization**  
  Based on the [ASP.NET Benchmarks](https://github.com/aspnet/benchmarks) project
  (derived from the [TechEmpower framework](https://www.techempower.com/benchmarks/)), the server exposes a `/json`
  endpoint that serializes a simple JSON object. Supports Bombardier and Wrk as client load generators.
* **OrchardCore CMS**  
  Runs the [OrchardCore](https://github.com/orchardcms/orchardcore) content management system
  with a Blog recipe, benchmarked with Wrk against the `/about` endpoint.

Both server configurations support **CPU core affinity** via the `BindToCores` and `CoreAffinity` parameters, allowing the
server process to be pinned to specific CPU cores for controlled performance measurement.

* [ASP.NET Benchmarks GitHub](https://github.com/aspnet/benchmarks)
* [OrchardCore GitHub](https://github.com/orchardcms/orchardcore)
* [TechEmpower Framework Benchmarks](https://www.techempower.com/benchmarks/)
* [Bombardier Documentation](../bombardier/bombardier.md)
* [Wrk/Wrk2 Documentation](../wrk/wrk.md)

## What is Being Measured?
The client tools (Bombardier, Wrk or Wrk2) generate concurrent HTTP requests against the ASP.NET server and capture latency
percentile distributions and throughput statistics.

### TechEmpower JSON Serialization
The server exposes a `/json` endpoint that serializes a simple JSON object. This scenario measures raw HTTP request processing
performance of the Kestrel server with minimal application logic overhead.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the ASP.NET benchmark workloads.

### Bombardier Metrics
The following metrics are emitted when the 'Bombardier' workload generator is used.

| Name                   | Example Value      | Unit        | Description                                         |
|------------------------|--------------------|-------------|-----------------------------------------------------|
| Latency Average        | 133.3698688        | milliseconds| Average HTTP response latency                       |
| Latency Max            | 7123.06304         | milliseconds| Maximum HTTP response latency                       |
| Latency P50            | 83.39392           | milliseconds| HTTP response latency (50th percentile)             |
| Latency P75            | 160.14336          | milliseconds| HTTP response latency (75th percentile)             |
| Latency P90            | 286.4128           | milliseconds| HTTP response latency (90th percentile)             |
| Latency P95            | 367.4112           | milliseconds| HTTP response latency (95th percentile)             |
| Latency P99            | 637.534208         | milliseconds| HTTP response latency (99th percentile)             |
| RequestPerSecond Avg   | 32768.449018       | Reqs/sec    | ASP.NET Web Requests per second (average)           |
| RequestPerSecond Stddev| 6446.822354105378  | Reqs/sec    | ASP.NET Web Requests per second (standard deviation)|
| RequestPerSecond P50   | 31049.462844       | Reqs/sec    | ASP.NET Web Requests per second (P50)               |
| RequestPerSecond P75   | 35597.436614       | Reqs/sec    | ASP.NET Web Requests per second (P75)               |
| RequestPerSecond P90   | 39826.205746       | Reqs/sec    | ASP.NET Web Requests per second (P90)               |
| RequestPerSecond P95   | 41662.542962       | Reqs/sec    | ASP.NET Web Requests per second (P95)               |
| RequestPerSecond P99   | 48600.556224       | Reqs/sec    | ASP.NET Web Requests per second (P99)               |

### Wrk Metrics
The following metrics are emitted when the 'Wrk' workload generator is used.

| Name               | Example Value | Unit          | Description                                   |
|--------------------|---------------|---------------|-----------------------------------------------|
| latency_p50        | 1.427         | milliseconds  | HTTP response latency (50th percentile)       |
| latency_p75        | 1.982         | milliseconds  | HTTP response latency (75th percentile)       |
| latency_p90        | 2.683         | milliseconds  | HTTP response latency (90th percentile)       |
| latency_p99        | 3.960         | milliseconds  | HTTP response latency (99th percentile)       |
| latency_p99_9      | 6.930         | milliseconds  | HTTP response latency (99.9th percentile)     |
| latency_p100       | 9.770         | milliseconds  | HTTP response latency (100th percentile)      |
| requests/sec       | 16305.17      | requests/sec  | Aggregate throughput                          |
| transfers/sec      | 20.01         | megabytes/sec | Data transfer rate                          |
