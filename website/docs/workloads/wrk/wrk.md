# Wrk/Wrk2 HTTP Benchmarking
Wrk is a modern HTTP benchmarking tool capable of generating significant load when run on a single multi-core CPU. It combines a multithreaded
design with scalable event notification systems such as epoll and kqueue to produce high request throughput with low resource consumption.

Wrk2 is a variant of wrk that adds constant-throughput, correct-latency recording using a modified version of Gil Tene's wrk2 rate-limiting
approach. Unlike wrk (which measures only coordinated-omission-free throughput), wrk2 takes a target request rate and produces an
HdrHistogram-corrected latency distribution, making it suitable for latency-sensitive benchmarks.

In Virtual Client, wrk and wrk2 serve as the HTTP client load generators for Nginx and ASP.NET web server workloads.
They run on a dedicated client machine and send requests to a server machine running the target web server.

* [Wrk GitHub](https://github.com/wg/wrk)
* [Wrk2 GitHub](https://github.com/giltene/wrk2)
* [How NOT to Measure Latency — Gil Tene (video)](https://www.youtube.com/watch?v=lJ8ydIuPFeU)
* [On Coordinated Omission — ScyllaDB](https://www.scylladb.com/2021/04/22/on-coordinated-omission/)

## Deployment Modes
The Wrk/Wrk2 executors support two deployment modes when used with ASP.NET server workloads:

- **Multi-VM (Client-Server)** — The client and server run on separate machines connected via a layout file.
  This isolates load generation from server processing for accurate benchmarking.
- **Single-VM** — When no layout file is provided, wrk connects to the server via the loopback address
  (`127.0.0.1`). Both server and client actions run sequentially on the same machine.

Note: Nginx workloads (`PERF-WEB-NGINX-*.json`) require a multi-VM layout and do not support single-VM mode.

## What is Being Measured?
The Wrk/Wrk2 toolset generates sustained HTTP/HTTPS request traffic against a target web server and captures the following:

- **Latency percentile distribution** — full HdrHistogram percentiles (P50 through P100) of response latency. Values are normalized to milliseconds.
- **Requests per second** — the aggregate throughput achieved during the test run.
- **Transfer rate** — the data transfer rate achieved during the test run (megabytes/sec).

Wrk2 additionally captures an *uncorrected latency distribution* that records raw measured latency without accounting for coordinated omission.
Both tools optionally emit a detailed percentile spectrum for fine-grained latency analysis (controlled by the `EmitLatencySpectrum` parameter).

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the Wrk workload.
Latency values are normalized to milliseconds by the parser regardless of the unit reported by wrk (nanoseconds, microseconds, milliseconds, or seconds).

### Latency Distribution Metrics

| Tool Name | Metric Name            | Example Value | Unit         |
|-----------|------------------------|---------------|--------------|
| Wrk       | latency_p50            | 1.427         | milliseconds |
| Wrk       | latency_p75            | 1.982         | milliseconds |
| Wrk       | latency_p90            | 2.683         | milliseconds |
| Wrk       | latency_p99            | 3.960         | milliseconds |
| Wrk       | latency_p99_9          | 6.930         | milliseconds |
| Wrk       | latency_p99_99         | 8.990         | milliseconds |
| Wrk       | latency_p99_999        | 9.770         | milliseconds |
| Wrk       | latency_p100           | 9.770         | milliseconds |

When wrk2 is used, an additional set of uncorrected latency metrics is emitted:

| Tool Name | Metric Name                     | Example Value | Unit         |
|-----------|---------------------------------|---------------|--------------|
| Wrk2      | uncorrected_latency_p50         | 0.483         | milliseconds |
| Wrk2      | uncorrected_latency_p75         | 1.120         | milliseconds |
| Wrk2      | uncorrected_latency_p90         | 1.710         | milliseconds |
| Wrk2      | uncorrected_latency_p99         | 2.870         | milliseconds |
| Wrk2      | uncorrected_latency_p99_9       | 5.760         | milliseconds |
| Wrk2      | uncorrected_latency_p99_99      | 8.020         | milliseconds |
| Wrk2      | uncorrected_latency_p99_999     | 8.410         | milliseconds |
| Wrk2      | uncorrected_latency_p100        | 8.410         | milliseconds |

### Throughput Metrics

| Tool Name | Metric Name    | Example Value | Unit          |
|-----------|----------------|---------------|---------------|
| Wrk       | requests/sec   | 16305.17      | requests/sec  |
| Wrk       | transfers/sec  | 20.01         | megabytes/sec |

### Latency Spectrum Metrics (Optional)
When the `EmitLatencySpectrum` parameter is set to `true`, the parser emits fine-grained percentile spectrum data points.
These are useful for visualizing full HdrHistogram latency distributions.

| Tool Name | Metric Name                    | Example Value | Unit | Description                     |
|-----------|--------------------------------|---------------|------|---------------------------------|
| Wrk       | latency_spectrum_p0_000000     | 0.175         |      | TotalCount:1                    |
| Wrk       | latency_spectrum_p0_100000     | 0.566         |      | TotalCount:3954                 |
| Wrk       | latency_spectrum_p0_500000     | 1.427         |      | TotalCount:19773                |
| Wrk       | latency_spectrum_p0_900000     | 2.683         |      | TotalCount:35553                |
| Wrk       | latency_spectrum_p0_990000     | 3.960         |      | TotalCount:39130                |
| Wrk       | latency_spectrum_p0_999000     | 7.011         |      | TotalCount:39462                |
| Wrk       | latency_spectrum_p1_000000     | 9.767         |      | TotalCount:39500                |

### Error Metrics

| Tool Name | Metric Name              | Example Value | Unit |
|-----------|--------------------------|---------------|------|
| Wrk       | Non-2xx or 3xx responses | 58902         |      |

If socket errors are detected, the parser raises a `WorkloadException` with a `Socket Error` metric.

## Profiles
The following profiles are available for the Wrk/Wrk2 workloads.

| Profile Name              | Description                                                                                 | Client Tool | Server                     | Platforms            |
|---------------------------|---------------------------------------------------------------------------------------------|-------------|----------------------------|----------------------|
| PERF-WEB-NGINX-WRK.json  | Nginx web server benchmark using wrk. Tests 100 to 10K connections at multiple thread counts. | WrkExecutor | NginxServerExecutor        | linux-x64, linux-arm64 |
| PERF-WEB-NGINX-WRK2.json | Nginx web server benchmark using wrk2 with constant request rate and corrected latency.      | Wrk2Executor | NginxServerExecutor       | linux-x64            |
| PERF-WEB-NGINX-WRK-RP.json  | Nginx reverse-proxy benchmark using wrk. Uses a three-node layout (Client → Reverse Proxy → Server). | WrkExecutor | NginxServerExecutor (×2) | linux-x64, linux-arm64 |
| PERF-WEB-NGINX-WRK2-RP.json | Nginx reverse-proxy benchmark using wrk2. Uses a three-node layout (Client → Reverse Proxy → Server). | Wrk2Executor | NginxServerExecutor (×2) | linux-x64 |
| PERF-WEB-ASPNET-TEJSON-WRK.json | ASP.NET TechEmpower JSON serialization benchmark using wrk.                                | WrkExecutor | AspNetServerExecutor       | linux-x64, linux-arm64, win-x64, win-arm64 |
| PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json | ASP.NET TechEmpower JSON serialization benchmark using wrk with CPU core affinity. | WrkExecutor | AspNetServerExecutor | linux-x64, linux-arm64, win-x64, win-arm64 |
| PERF-WEB-ASPNET-ORCHARD-WRK.json | ASP.NET OrchardCore CMS benchmark using wrk.                                              | WrkExecutor | AspNetOrchardServerExecutor | linux-x64, linux-arm64 |

## Parameters
The following table describes the key parameters supported by the Wrk/Wrk2 executors.

| Parameter            | Description                                                                                    | Default         |
|----------------------|-----------------------------------------------------------------------------------------------|-----------------|
| PackageName          | The name of the wrk or wrk2 dependency package.                                               | *required*      |
| CommandArguments     | The wrk/wrk2 command-line arguments (threads, connections, duration, URL, etc.).               | *required*      |
| TargetService        | The target service type: `server`, `rp` (reverse-proxy), or `apigw` (API gateway).            | auto-detected   |
| TestDuration         | Duration of the test run (e.g., `00:02:30`).                                                  | profile-defined |
| Timeout              | Maximum time to wait for server availability.                                                  | 5 minutes       |
| WarmUp               | When `true`, the run is a warm-up pass and metrics are not captured.                           | `false`         |
| EmitLatencySpectrum  | When `true`, the fine-grained latency spectrum is emitted as additional metrics.               | `false`         |
| BindToCores          | When `true`, the wrk process is pinned to specific CPU cores.                                  | `false`         |
| CoreAffinity         | CPU core affinity specification (e.g., `0-3`, `0,2,4,6`). Required when `BindToCores` is `true`. | *none*       |

## Wrk Command Line Options
The following are the key command-line options for wrk and wrk2. These are referenced in the `CommandArguments`
parameter in Virtual Client profiles.

| Option             | Description                                                                                      |
|--------------------|--------------------------------------------------------------------------------------------------|
| `-t, --threads`  | Total number of threads to use.                                                                  |
| `-c, --connections` | Total number of HTTP connections to keep open (each thread handles N = connections/threads).   |
| `-d, --duration` | Duration of the test (e.g., `2s`, `2m`, `2h`).                                             |
| `-R, --rate`     | Total requests per second (wrk2 only). Enables constant-throughput, corrected-latency recording. |
| `-L, --latency`  | Print detailed latency statistics (HdrHistogram percentile distribution).                        |
| `--timeout`      | Record a timeout if a response is not received within this amount of time.                       |

## Translating a Profile to a Command
Virtual Client profiles define wrk parameters declaratively. The executor translates them into a wrk command line
at runtime. For example, the following profile action:

```json
{
    "Type": "WrkExecutor",
    "Parameters": {
        "PackageName": "wrk",
        "Scenario": "benchmark_measurement",
        "CommandArguments": "--latency --threads {ThreadCount} --connections {Connection} --duration {TestDuration.TotalSeconds}s --timeout 10s http://{serverip}:{ServerPort}/json",
        "ThreadCount": "64",
        "Connection": 4096,
        "TestDuration": "00:00:15",
        "ServerPort": 9876,
        "Role": "Client"
    }
}
```

Translates to the following wrk command (assuming server IP `10.0.0.5`):

```
wrk --latency --threads 64 --connections 4096 --duration 15s --timeout 10s http://10.0.0.5:9876/json
```

## Performance Notes
Generally, the more concurrent connections you configure, the higher the load on the server. At some point, increasing
connections further will result in diminishing returns in requests per second and increased latency, as the server becomes
saturated. This saturation point is useful for characterizing the maximum throughput capacity of the server under test.
