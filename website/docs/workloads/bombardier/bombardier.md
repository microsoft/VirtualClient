# Bombardier HTTP Benchmarking
Bombardier is a fast, cross-platform HTTP(S) benchmarking tool written in Go. It uses the fasthttp library for high-performance
HTTP client operations and supports configurable concurrency, request duration, timeouts, and output in JSON format for automated
metric collection.

In Virtual Client, Bombardier serves as the HTTP client load generator for ASP.NET server workloads. It runs on a dedicated
client machine (or the same machine in single-VM mode) and sends requests to the ASP.NET Kestrel server, producing latency
and throughput statistics in JSON format that are parsed into standardized metrics.

* [Bombardier GitHub](https://github.com/codesenberg/bombardier)
* [Bombardier Releases](https://github.com/codesenberg/bombardier/releases)

## Deployment Modes
The Bombardier executor supports two deployment modes:

- **Multi-VM (Client-Server)** — The client and server run on separate machines connected via a layout file.
  This isolates load generation from server processing for accurate benchmarking.
- **Single-VM** — When no layout file is provided, Bombardier connects to the server via the loopback address
  (`127.0.0.1`). Both server and client actions run sequentially on the same machine.

## What is Being Measured?
Bombardier generates sustained HTTP request traffic against a target server endpoint and captures the following:

- **Latency** — average, standard deviation, maximum, and percentile distributions (P50, P75, P90, P95, P99) of response latency in microseconds.
- **Requests per second** — average, standard deviation, maximum, and percentile distributions (P50, P75, P90, P95, P99) of throughput.

Bombardier outputs results in JSON format, which is parsed by the `BombardierMetricsParser` to extract structured metrics.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Bombardier workload against an
ASP.NET server.

### Latency Metrics

| Metric Name       | Example Value       | Unit        | Description                                |
|--------------------|---------------------|-------------|--------------------------------------------|
| Latency Max        | 178703              | microsecond | HTTP response latency (maximum)            |
| Latency Average    | 8270.807963429836   | microsecond | HTTP response latency (average)            |
| Latency Stddev     | 6124.356473307014   | microsecond | HTTP response latency (standard deviation) |
| Latency P50        | 6058                | microsecond | HTTP response latency (50th percentile)    |
| Latency P75        | 10913               | microsecond | HTTP response latency (75th percentile)    |
| Latency P90        | 17949               | microsecond | HTTP response latency (90th percentile)    |
| Latency P95        | 23318               | microsecond | HTTP response latency (95th percentile)    |
| Latency P99        | 35856               | microsecond | HTTP response latency (99th percentile)    |

### Throughput Metrics

| Metric Name            | Example Value        | Unit     | Description                                        |
|------------------------|----------------------|----------|----------------------------------------------------|
| RequestPerSecond Max    | 67321.282458945348   | Reqs/sec | HTTP requests per second (maximum)                 |
| RequestPerSecond Average| 31211.609987720527   | Reqs/sec | HTTP requests per second (average)                 |
| RequestPerSecond Stddev | 6446.822354105378    | Reqs/sec | HTTP requests per second (standard deviation)      |
| RequestPerSecond P50    | 31049.462844         | Reqs/sec | HTTP requests per second (50th percentile)         |
| RequestPerSecond P75    | 35597.436614         | Reqs/sec | HTTP requests per second (75th percentile)         |
| RequestPerSecond P90    | 39826.205746         | Reqs/sec | HTTP requests per second (90th percentile)         |
| RequestPerSecond P95    | 41662.542962         | Reqs/sec | HTTP requests per second (95th percentile)         |
| RequestPerSecond P99    | 49625.656227         | Reqs/sec | HTTP requests per second (99th percentile)         |

## Profiles
The following profiles use Bombardier as the client load generator.

| Profile Name                 | Description                                                                                     | Server                 | Platforms                                    |
|------------------------------|-------------------------------------------------------------------------------------------------|------------------------|----------------------------------------------|
| PERF-ASPNETBENCH.json        | ASP.NET JSON serialization benchmark using Bombardier with 256 connections.                     | AspNetServerExecutor   | linux-x64, linux-arm64, win-x64, win-arm64   |

## Parameters
The following table describes the key parameters supported by the Bombardier executor.

| Parameter        | Description                                                                                       | Default       |
|------------------|--------------------------------------------------------------------------------------------------|---------------|
| PackageName      | The name of the Bombardier dependency package.                                                    | *required*    |
| CommandArguments | The Bombardier command-line arguments (duration, connections, timeout, URL, output format, etc.). | *required*    |
| TargetService    | The target service type: `server`, `rp` (reverse-proxy), or `apigw` (API gateway).               | auto-detected |
| Timeout          | Maximum time to wait for server availability.                                                     | 5 minutes     |
| WarmUp           | When `true`, the run is a warm-up pass and metrics are not captured.                              | `false`       |
| BindToCores      | When `true`, the Bombardier process is pinned to specific CPU cores.                              | `false`       |
| CoreAffinity     | CPU core affinity specification (e.g., `8-15`). Required when `BindToCores` is `true`.           | *none*        |

## Packaging and Setup
Virtual Client handles installation of Bombardier automatically through profile dependencies. The typical setup flow is:

1. VC installs Bombardier from the registered dependency package.
2. On Unix systems, VC makes the `bombardier` binary executable.
3. VC waits for the server (e.g., ASP.NET Kestrel) to come online.
4. Bombardier is invoked with the configured parameters against the target URL.

Example Bombardier command:
```
bombardier -d 15s -c 256 -t 10s --fasthttp --insecure -l http://localhost:9876/json --print r --format json
```
