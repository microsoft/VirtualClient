# ASP.NET Benchmarks
The ASP.NET benchmarks measure the throughput and latency of ASP.NET Kestrel web applications under sustained HTTP load.
The workloads use a client-server architecture where the server runs an ASP.NET application and the client generates
HTTP requests using either [Bombardier](../bombardier/bombardier.md) or [Wrk](../wrk/wrk.md).

Two server workloads are supported:

- **TechEmpower JSON Serialization** — Based on the [ASP.NET Benchmarks](https://github.com/aspnet/benchmarks) project
  (derived from the [TechEmpower framework](https://www.techempower.com/benchmarks/)), the server exposes a `/json`
  endpoint that serializes a simple JSON object. Supports Bombardier and Wrk as client load generators.
- **OrchardCore CMS** — Runs the [OrchardCore](https://github.com/orchardcms/orchardcore) content management system
  with a Blog recipe, benchmarked with Wrk against the `/about` endpoint.

Both server configurations support **CPU core affinity** via the `BindToCores` and `CoreAffinity` parameters, allowing the
server process to be pinned to specific CPU cores for controlled performance measurement.

* [ASP.NET Benchmarks GitHub](https://github.com/aspnet/benchmarks)
* [OrchardCore GitHub](https://github.com/orchardcms/orchardcore)
* [TechEmpower Framework Benchmarks](https://www.techempower.com/benchmarks/)
* [Bombardier Documentation](../bombardier/bombardier.md)
* [Wrk/Wrk2 Documentation](../wrk/wrk.md)

## Deployment Modes
The ASP.NET benchmark workloads support two deployment modes:

- **Multi-VM (Client-Server)** — The server and client run on separate machines connected via an
  [environment layout](../../guides/0020-client-server.md) file. This is the recommended mode for production benchmarking
  as it isolates server and client resource consumption.
- **Single-VM** — When no layout file is provided, both server and client actions run sequentially on the same machine.
  The client connects to the server via the loopback address (`127.0.0.1`). This mode is useful for development,
  validation, and quick smoke testing.

CPU core affinity profiles (e.g., `PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json`) work in
both modes. In single-VM mode, core affinity is especially useful to prevent the server and client from contending
for the same CPU cores.

## What is Being Measured?
The client tools (Bombardier or Wrk) generate concurrent HTTP requests against the ASP.NET server and capture latency
percentile distributions and throughput statistics.

### TechEmpower JSON Serialization
The server exposes a `/json` endpoint that serializes a simple JSON object. This scenario measures raw HTTP request processing
performance of the Kestrel server with minimal application logic overhead.

### OrchardCore CMS
The server runs a full OrchardCore CMS application with a Blog recipe. The `/about` endpoint exercises the full CMS rendering
pipeline including routing, middleware, and content rendering.

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the ASP.NET benchmark workloads.

### Bombardier Metrics
When Bombardier is used as the client:

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
When Wrk is used as the client (e.g., `PERF-WEB-ASPNET-TEJSON-WRK.json`, `PERF-WEB-ASPNET-ORCHARD-WRK.json`):

| Name               | Example Value | Unit          | Description                                   |
|--------------------|---------------|---------------|-----------------------------------------------|
| latency_p50        | 1.427         | milliseconds  | HTTP response latency (50th percentile)       |
| latency_p75        | 1.982         | milliseconds  | HTTP response latency (75th percentile)       |
| latency_p90        | 2.683         | milliseconds  | HTTP response latency (90th percentile)       |
| latency_p99        | 3.960         | milliseconds  | HTTP response latency (99th percentile)       |
| latency_p99_9      | 6.930         | milliseconds  | HTTP response latency (99.9th percentile)     |
| latency_p100       | 9.770         | milliseconds  | HTTP response latency (100th percentile)      |
| requests/sec       | 16305.17      | requests/sec  | Aggregate throughput                          |
| transfers/sec      | 20.01         | megabytes/sec | Data transfer rate                            |

See the [Wrk/Wrk2 documentation](../wrk/wrk.md) and [Bombardier documentation](../bombardier/bombardier.md) for the complete list of client metrics.

## Profiles
The following profiles are available for the ASP.NET benchmark workloads. See the [profile details](./aspnet-benchmarks-profiles.md)
page for per-profile parameters, dependencies, and usage examples.

| Profile Name                             | Description                                                                        | Client Tool        | Server                        | Platforms                                  |
|------------------------------------------|------------------------------------------------------------------------------------|--------------------|-------------------------------|--------------------------------------------|
| PERF-WEB-ASPNET-TEJSON-WRK.json          | TechEmpower JSON serialization benchmark using Wrk with warm-up pass.              | WrkExecutor        | AspNetServerExecutor          | linux-x64, linux-arm64, win-x64, win-arm64 |
| PERF-WEB-ASPNET-TEJSON-WRK-AFFINITY.json | TechEmpower JSON serialization benchmark using Wrk with CPU core affinity.         | WrkExecutor        | AspNetServerExecutor          | linux-x64, linux-arm64, win-x64, win-arm64 |
| PERF-WEB-ASPNET-ORCHARD-WRK.json         | OrchardCore CMS benchmark using Wrk with warm-up pass.                             | WrkExecutor        | AspNetOrchardServerExecutor   | linux-x64, linux-arm64                     |

## Server Parameters
The following tables describe the key parameters supported by the ASP.NET server executors.

### AspNetServerExecutor

| Parameter                        | Description                                                                  | Default    |
|----------------------------------|-----------------------------------------------------------------------------|------------|
| PackageName                      | The name of the ASP.NET Benchmarks dependency package.                      | *required* |
| DotNetSdkPackageName             | The name of the .NET SDK dependency package.                                | `dotnetsdk`|
| TargetFramework                  | The .NET target framework (e.g., `net8.0`, `net9.0`).                       | *required* |
| ServerPort                       | The port the Kestrel server listens on.                                     | `9876`     |
| AspNetCoreThreadCount            | The ASPNETCORE thread count environment variable.                           | `1`        |
| DotNetSystemNetSocketsThreadCount| The DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT environment variable.            | `1`        |
| BindToCores                      | When `true`, the server process is pinned to specific CPU cores.            | `false`    |
| CoreAffinity                     | CPU core affinity specification (e.g., `0-7`). Required when `BindToCores` is `true`. | *none* |

### AspNetOrchardServerExecutor

| Parameter          | Description                                                                  | Default    |
|--------------------|-----------------------------------------------------------------------------|------------|
| PackageName        | The name of the OrchardCore dependency package.                             | *required* |
| DotNetSdkPackageName | The name of the .NET SDK dependency package.                              | `dotnetsdk`|
| TargetFramework    | The .NET target framework (e.g., `net9.0`).                                 | *required* |
| ServerPort         | The port the OrchardCore server listens on.                                 | `5014`     |
| BindToCores        | When `true`, the server process is pinned to specific CPU cores.            | `false`    |
| CoreAffinity       | CPU core affinity specification (e.g., `0-7`). Required when `BindToCores` is `true`. | *none* |

## Packaging and Setup
The following section covers how to create the custom Virtual Client dependency packages required to execute the workload
and toolset(s). This section is meant to provide guidance for users who would like to create their own packages with the
software for use with the Virtual Client. For example, users may want to bring in new versions of the software.
See the documentation on [Dependency Packages](https://microsoft.github.io/VirtualClient/docs/developing/0040-vc-packages/)
for more information on the concepts.

### TechEmpower JSON Serialization Setup
1. Virtual Client installs the .NET SDK via the `DotNetInstallation` dependency.
2. Virtual Client clones the [ASP.NET Benchmarks](https://github.com/aspnet/Benchmarks) GitHub repository.
3. The `src/Benchmarks` project is built using `dotnet build`.
4. The server is started using `dotnet run`:

```bash
dotnet <path_to_binary>/Benchmarks.dll \
  --nonInteractive true \
  --scenarios json \
  --urls http://*:9876 \
  --server Kestrel \
  --kestrelTransport Sockets \
  --protocol http \
  --header "Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7" \
  --header "Connection: keep-alive"
```
5. The client tool (Bombardier or Wrk) generates HTTP load against the server:

```bash
# Bombardier example
bombardier -d 15s -c 256 -t 2s --fasthttp --insecure -l http://<server_ip>:9876/json --print r --format json

# Wrk example
wrk --latency --threads 64 --connections 4096 --duration 15s --timeout 10s http://<server_ip>:9876/json
```

### OrchardCore CMS Setup
1. Virtual Client installs the .NET SDK via the `DotNetInstallation` dependency.
2. Virtual Client clones the [OrchardCore](https://github.com/OrchardCMS/OrchardCore) GitHub repository.
3. The `OrchardCore.Cms.Web` project is published using `dotnet publish`.
4. The server is started:

```bash
nohup <path_to_publish>/OrchardCore.Cms.Web --urls http://*:5014
```

5. Wrk generates HTTP load against the `/about` endpoint:

```bash
wrk -t 64 -c 128 -d 20s http://<server_ip>:5014/about
```
