# AspNetBenchmark
AspNetBenchmark is a benchmark developed by MSFT ASPNET team, based on open source benchmark TechEmpower.  
This workload has server and client part, on the same test machine. The server part is started as a ASPNET service. The client calls server using open source bombardier binaries.  
Bombardier binaries could be downloaded from Github release, or directly compile from source using "go build ."

* [AspNetBenchmarks Github](https://github.com/aspnet/benchmarks)
* [Bombardier Github](https://github.com/codesenberg/bombardier)
* [Bombardier Release](https://github.com/codesenberg/bombardier/releases/tag/v1.2.5)

## Workload Metrics
The following metrics are examples of those captured by the Virtual Client when running the AspNetBenchmark workload.

[Bombardier output example](https://github.com/codesenberg/bombardier#examples)

The following metrics are examples of those captured during the operations of the AspNetBench workload.

| Name                     | Example            | Unit        | Description                            |
|--------------------------|--------------------|-------------|----------------------------------------|
| Latency Max               | 178703                | microsecond | ASP.NET Web Request latency (max) |
| Latency Average           | 8270.807963429836  | microsecond | ASP.NET Web Request latency (avg) |
| Latency Stddev           | 6124.356473307014  | microsecond | ASP.NET Web Request latency (standard deviation) |
| Latency P50               | 6058                | microsecond | ASP.NET Web Request latency (P50) |
| Latency P75                  | 10913                | microsecond | ASP.NET Web Request latency (P75) |
| Latency P90                  | 17949                | microsecond | ASP.NET Web Request latency (P90) |
| Latency P95                  | 23318                | microsecond | ASP.NET Web Request latency (P95) |
| Latency P99               | 35856                | microsecond | ASP.NET Web Request latency (P99) |
| RequestPerSecond Max     | 61221.282458945345 | Reqs/sec      | ASP.NET Web Request per second (max) |
| RequestPerSecond Average | 31211.609987720527 | Reqs/sec    | ASP.NET Web Request per second (avg) |
| RequestPerSecond Stddev  | 6446.822354105378  | Reqs/sec    | ASP.NET Web Request per second (standard deviation) |
| RequestPerSecond P50     | 31049.462844       | Reqs/sec    | ASP.NET Web Request per second (P50) |
| RequestPerSecond P75     | 35597.436614       | Reqs/sec    | ASP.NET Web Request per second (P75) |
| RequestPerSecond P90     | 39826.205746       | Reqs/sec    | ASP.NET Web Request per second (P90) |
| RequestPerSecond P95     | 41662.542962       | Reqs/sec    | ASP.NET Web Request per second (P95) |
| RequestPerSecond P99     | 48600.556224       | Reqs/sec    | ASP.NET Web Request per second (P99) |

## Packaging and Setup
The following section covers how to create the custom Virtual Client dependency packages required to execute the workload and toolset(s). This section
is meant to provide guidance for users that would like to create their own packages with the software for use with the Virtual Client. For example, users
may want to bring in new versions of the software. See the documentation on '[Dependency Packages](https://microsoft.github.io/VirtualClient/docs/developing/0040-vc-packages/)' 
for more information on the concepts.

1. VC installs dotnet SDK
2. VC clones AspNetBenchmarks github repo
3. dotnet build src/benchmarks project in AspNetBenchmarks repo
4. Use dotnet to start server

```
dotnet <path_to_binary>\Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:5000 --server Kestrel --kestrelTransport Sockets --protocol http --header "Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7" --header "Connection: keep-alive" 
```

5. Use bombardier to start client
```
bombardier-windows-amd64.exe -d 15s -c 256 -t 2s --fasthttp --insecure -l http://localhost:5000/json --print r --format json
```


