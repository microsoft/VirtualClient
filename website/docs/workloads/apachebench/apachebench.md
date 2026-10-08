---
slug: /workloads/apachebench
---

# ApacheBench HTTP Benchmarking

ApacheBench (`ab`) is an HTTP load-generation tool distributed with the Apache HTTP Server. It sends a configurable number of requests to a web server and reports request throughput, latency, failures, and transfer rate.

Virtual Client uses ApacheBench to benchmark an Apache HTTP Server running on the same system. The workload installs and starts the server, runs `ab` against `http://localhost:80/`, and converts the text output into structured metrics.

* [ApacheBench documentation](https://httpd.apache.org/docs/2.4/programs/ab.html)
* [Apache HTTP Server documentation](https://httpd.apache.org/docs/2.4/)

## Supported Platforms

| Operating system | Architecture | Apache HTTP Server source |
|------------------|--------------|---------------------------|
| Linux (Debian and Ubuntu) | x64, Arm64 | `apache2` operating-system package |
| Windows | x64 | Virtual Client `apache-httpd-2.4.57-v2.zip` dependency package |

The profile requires network access to install Linux packages or download the Windows dependency package. It does not support a disconnected run unless those dependencies are already available from the configured package sources.

## Workload Flow

The `ApacheBenchExecutor` performs the following operations:

1. Installs Apache HTTP Server through the profile dependencies.
2. On Windows, updates `httpd.conf` with the extracted package path, installs the bundled Visual C++ runtime when present, installs the Apache service, and starts it.
3. On Linux, opens TCP port 80 through `ufw` and starts the `apache2` service.
4. Runs ApacheBench against the local server with HTTP keep-alive enabled.
5. Parses and publishes the benchmark metrics.

The default command is:

```text
ab -k -n 50000 -c 50 http://localhost:80/
```

`-n` specifies the total request count, `-c` specifies concurrency, and `-k` enables HTTP keep-alive.

## Profile

| Profile | Description | Platforms |
|---------|-------------|-----------|
| `PERF-APACHEBENCH.json` | Runs ApacheBench against a local Apache HTTP Server with configurable request count and concurrency. | linux-x64, linux-arm64, win-x64 |

The profile installs these dependencies:

* **Windows:** `apache-httpd-2.4.57-v2.zip` from the Virtual Client `packages` container.
* **Linux:** `apache2` and `unzip` through the system package manager.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `NoOfRequests` | Total number of requests sent during each benchmark iteration. | `50000` |
| `NoOfConcurrentRequests` | Number of requests issued concurrently. | `50` |
| `PackageName` | Logical name of the extracted Windows Apache HTTP Server package. | `apachehttpserver` |
| `Scenario` | Scenario name attached to emitted telemetry. | `ExecuteApacheBenchBenchmark` |

Parameters can be overridden on the command line:

```bash
./VirtualClient \
  --profile=PERF-APACHEBENCH.json \
  --system=Demo \
  --timeout=60 \
  --parameters="NoOfRequests=100000,,,NoOfConcurrentRequests=100"
```

On Windows, use `VirtualClient.exe` instead of `./VirtualClient`.

## Workload Metrics

ApacheBench reports two `Time per request` values. Virtual Client emits the value measured across all concurrent requests because it represents the average effective request latency at the configured concurrency.

| Metric Name | Example Value | Unit | Relativity | Description |
|-------------|---------------|------|------------|-------------|
| Concurrency Level | 100 | number | Undefined | Configured number of concurrent requests. |
| Total requests | 100 | number | Undefined | Number of completed requests. |
| Total time | 0.578 | seconds | Lower is better | Total benchmark duration. |
| Total failed requests | 0 | number | Lower is better | Requests ApacheBench classified as failed. |
| Requests | 172.97 | number/sec | Higher is better | Completed request throughput. |
| Total time per request | 5.781 | milliseconds | Lower is better | Mean request time across all concurrent requests. |
| Total data transferred | 65006 | bytes | Lower is better | Total response bytes received. |
| Data transfer rate | 109.81 | kilobytes/sec | Higher is better | Average response data throughput. |

## Troubleshooting

### The benchmark cannot connect to localhost

Confirm that the Apache HTTP Server is running and listening on port 80. On Linux, check `systemctl status apache2`. On Windows, check the Apache service and review the `Apache24/logs` directory in the extracted package.

### Windows reports a missing DLL

The Windows package includes `vc_redist.x64.exe`, which the executor installs before Apache HTTP Server. A failure with `0xC0000135` generally indicates that the Visual C++ runtime was not installed successfully.

### Linux setup fails while configuring the firewall

The executor runs `ufw allow 80/tcp` with elevated permissions. Ensure `ufw` is available and that the Virtual Client process can run elevated commands.

### Results parsing fails

Retain the ApacheBench standard output in the Virtual Client logs. The parser requires the standard summary fields beginning with `Concurrency Level` and reports a workload-results parsing error when those fields are absent or incomplete.
